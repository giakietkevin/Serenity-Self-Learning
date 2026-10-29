# Bài 21 – Kiến trúc nền: Service Locator, khởi động ứng dụng và xác thực

**Mục tiêu:** hiểu điều gì xảy ra khi app khởi động, `Dependency` (service locator) hoạt động thế nào, và
`Authorization.HasPermission(...)` thật sự gọi tới đâu. Từ đó biết chỗ để thay đổi cách đăng nhập, cách tính quyền,
hoặc chạy code với quyền của user khác (job nền, import).

Tham khảo (3.13.7):
- Serene: [App_Start/SiteInitialization.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/App_Start/SiteInitialization.cs), [Global.asax.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Global.asax.cs), [Administration/User/Authentication/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/Authentication), [Membership/Account/AccountPage.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Membership/Account/AccountPage.cs)
- Serenity: [Serenity.Web/Common/CommonInitialization.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Common/CommonInitialization.cs), [Serenity.Core/Authorization/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Core/Authorization), [Serenity.Core/Dependency/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Core/Dependency), [Serenity.Web/Security/WebSecurityHelper.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Security/WebSecurityHelper.cs)
- Guide: `service_locator/`, `authentication/`

---

## 1. Thứ tự khởi động

```
IIS nhận request đầu tiên
 └─ Global.Application_Start                         (Global.asax.cs)
     ├─ đăng ký route, filter MVC, Razor view engine
     └─ SiteInitialization.ApplicationStart()        (App_Start/SiteInitialization.cs)
         ├─ CommonInitialization.Run()               (Serenity.Web)
         │   ├─ InitializeServiceLocator             tạo MunqContainer, gán cho Dependency
         │   ├─ InitializeSelfAssemblies             tìm các assembly có tham chiếu Serenity
         │   ├─ InitializeAnnotatedTypes
         │   ├─ InitializeCaching                    ILocalCache, IDistributedCache (bài 10)
         │   ├─ InitializeConfigurationSystem        Config.Get<T> (bài 15)
         │   ├─ InitializeLogging
         │   ├─ InitializeLocalTexts                 nạp bản dịch (bài 09)
         │   ├─ InitializeDynamicScripts             lookup, form, columns, template script
         │   ├─ InitializeRequestContext
         │   └─ InitializeRequestBehaviors           quét IImplicitBehavior (bài 14)
         ├─ đăng ký 4 service xác thực/phân quyền   (mục 3)
         ├─ InitializeExceptionLog                   /errorlog.axd (bài 15)
         └─ EnsureDatabase + RunMigrations           (bài 02, 15)
```

Mỗi request sau đó: `Application_BeginRequest` đặt ngôn ngữ từ cookie `LanguagePreference` (bài 09), rồi ASP.NET
đọc cookie Forms Authentication để biết user là ai.

Lỗi ở bước nào trong `ApplicationStart` thì app không khởi động được, và lỗi được ghi log (`ex.Log()`) trước khi
ném ra. Thấy lỗi 500 ngay trang đầu sau khi deploy thì xem ở đây trước.

## 2. Dependency: service locator

Serenity 3.x không dùng constructor injection. Mọi service dùng chung được đăng ký vào một container, và lấy ra
qua lớp static `Dependency`:

```csharp
var registrar = Dependency.Resolve<IDependencyRegistrar>();
registrar.RegisterInstance<IPermissionService>(new MyPermissionService());   // một instance dùng chung
registrar.Register<IMyService, MyService>();                                   // tạo mới mỗi lần Resolve

var perm = Dependency.Resolve<IPermissionService>();      // không có thì ném lỗi
var dir  = Dependency.TryResolve<IDirectoryService>();    // không có thì trả về null
```

- Container mặc định là `MunqContainer`.
- `RegisterInstance` sau sẽ **thay** cái trước cho cùng interface. Vì vậy `CommonInitialization` dùng
  `if (Dependency.TryResolve<ILocalCache>() == null)`: nếu bạn đã đăng ký từ trước thì nó không ghi đè. Ví dụ
  muốn dùng Redis cho `IDistributedCache`, đăng ký trước khi gọi `CommonInitialization.Run()`, hoặc đăng ký lại
  sau đó.
- Service đăng ký bằng `RegisterInstance` được dùng chung bởi mọi request và mọi thread, nên phải thread-safe,
  giống behavior ở bài 14.

Những interface hay gặp:

| Interface | Ai đăng ký | Dùng bởi |
|---|---|---|
| `IAuthorizationService` | `SiteInitialization` | `Authorization.Username`, `IsLoggedIn` |
| `IAuthenticationService` | `SiteInitialization` | Kiểm tra username/password khi đăng nhập |
| `IPermissionService` | `SiteInitialization` | `Authorization.HasPermission` |
| `IUserRetrieveService` | `SiteInitialization` | `Authorization.UserDefinition`, `UserId` |
| `IDirectoryService` | `SiteInitialization`, chỉ khi có cấu hình `LDAP`/`ActiveDirectory` | Đăng nhập bằng AD |
| `ILocalCache`, `IDistributedCache` | `CommonInitialization` | Cache (bài 10) |
| `IImplicitBehaviorRegistry` | `CommonInitialization` | Service behavior (bài 14) |
| `ILogger` | `CommonInitialization` | `ex.Log()`, `Log.Info(...)` |

## 3. Bốn service xác thực và phân quyền

```csharp
// SiteInitialization.ApplicationStart()
registrar.RegisterInstance<IAuthorizationService>(new Administration.AuthorizationService());
registrar.RegisterInstance<IAuthenticationService>(new Administration.AuthenticationService());
registrar.RegisterInstance<IPermissionService>(
    new LogicOperatorPermissionService(new Administration.PermissionService()));
registrar.RegisterInstance<IUserRetrieveService>(new Administration.UserRetrieveService());
```

Lớp static `Authorization` mà bạn dùng ở khắp nơi (bài 07) chỉ là lớp gọi qua bốn service này:

| Gọi | Thực chất |
|---|---|
| `Authorization.Username` | `IAuthorizationService.Username` → Serene đọc `HttpContext.Current.User.Identity.Name` (từ cookie đăng nhập) |
| `Authorization.IsLoggedIn` | `Username` khác rỗng |
| `Authorization.UserDefinition` | `IUserRetrieveService.ByUsername(Username)` → `UserDefinition`, có cache |
| `Authorization.UserId` | `UserDefinition.Id` |
| `Authorization.HasPermission(p)` | `"*"` → luôn `true`; `""` hoặc `"?"` → đã đăng nhập là đủ; còn lại → `IPermissionService.HasPermission(p)` |

### 3.1. Đăng nhập (IAuthenticationService)

```
LoginPanel.ts → Q.serviceCall('Account/Login')
 └─ AccountController.Login                     (Membership/Account/AccountPage.cs)
     └─ WebSecurityHelper.Authenticate(ref username, password, persist)
         ├─ IAuthenticationService.Validate(ref username, password)
         │    Serene: AuthenticationService
         │     ├─ tìm user bằng IUserRetrieveService.ByUsername
         │     ├─ user không active → false
         │     ├─ quá 50 lần sai trong 30 phút → false (Throttler)
         │     ├─ Source = "site": so mật khẩu băm với PasswordHash + PasswordSalt
         │     └─ Source = "ldap": hỏi IDirectoryService
         └─ đúng thì SetAuthenticationTicket → tạo cookie Forms Authentication
```

Cookie được cấu hình trong `Web.config`:

```xml
<authentication mode="Forms">
  <forms loginUrl="~/Account/Login" timeout="30" path="/" slidingExpiration="true" />
</authentication>
```

`timeout="30"` và `slidingExpiration="true"`: không thao tác 30 phút thì phải đăng nhập lại. Khi có nhiều server,
cần `<machineKey>` giống nhau để cookie của server này đọc được ở server kia (bài 15).

Muốn đăng nhập theo cách khác (kiểm tra qua API của công ty, mật khẩu kiểu cũ...), viết lại
`AuthenticationService.Validate`. Phần tạo cookie giữ nguyên.

### 3.2. Thông tin user hiện tại (IUserRetrieveService)

`UserRetrieveService.ByUsername` đọc `UserRow` rồi tạo `UserDefinition`, cache bằng `TwoLevelCache` với group
key là bảng Users (bài 10). Muốn có thêm thông tin, ví dụ `TenantId` (bài 14) hay mã phòng ban:

1. Thêm property vào `UserDefinition.cs`.
2. Gán nó trong `UserRetrieveService.GetFirst`.
3. Đọc ở bất kỳ đâu: `((UserDefinition)Authorization.UserDefinition).DepartmentId`.

Lưu user qua màn hình User Management thì cache tự hết hạn nhờ group key (bài 10). Sửa user bằng tay trong SQL
thì cache **không** biết, user có thể vẫn đăng nhập được dù đã bị khóa cho tới khi cache hết hạn. Khi đó gọi
`UserRetrieveService.RemoveCachedUser(userId, username)` (Serene dùng hàm này trong `AuthenticationService` và
lúc đăng ký tài khoản) hoặc recycle app pool.

### 3.3. Tính quyền (IPermissionService)

`Administration.PermissionService.HasPermission` của Serene:

1. Username là **`admin`** → luôn `true`. Đây là lý do tài khoản admin thấy mọi menu dù chưa gán quyền.
2. Có dòng trong `UserPermissions` cho quyền này → dùng giá trị `Granted` của dòng đó (có thể là **từ chối**,
   kể cả khi role có quyền).
3. Không có → `true` nếu một trong các role của user có quyền (`RolePermissions`).

Ba bảng đều được cache theo user/role, và quyền ngầm (`ImplicitPermission`, bài 07) được cộng thêm.

`LogicOperatorPermissionService` bọc bên ngoài để cho phép viết biểu thức:

```csharp
[ReadPermission("MovieDB:Movie:View|MovieDB:General")]       // có một trong hai
[ReadPermission("MovieDB:General&Administration:General")]   // có cả hai
[ReadPermission("!MovieDB:Restricted")]                       // không có quyền này
```

## 4. Chạy code với quyền khác: Impersonate và TransientGrant

Code chạy ngoài request (job nền, scheduled task) không có cookie, nên `Authorization.Username` là `null` và mọi
`ValidatePermission` đều thất bại. Code import đôi khi cần tạm có quyền mà user không có. Serenity 3.13.7 có sẵn
hai lớp bọc cho việc này, nhưng Serene **chưa đăng ký** chúng. Thêm vào `SiteInitialization`:

```csharp
registrar.RegisterInstance<IAuthorizationService>(
    new ImpersonatingAuthorizationService(new Administration.AuthorizationService()));
registrar.RegisterInstance<IPermissionService>(
    new TransientGrantingPermissionService(
        new LogicOperatorPermissionService(new Administration.PermissionService())));
```

Dùng:

```csharp
var impersonator = (IImpersonator)Dependency.Resolve<IAuthorizationService>();
impersonator.Impersonate("admin");                    // từ đây Authorization.Username = "admin"
try
{
    new MovieRepository().Create(uow, request);       // đi qua đủ validation, behavior như user thật
}
finally
{
    impersonator.UndoImpersonate();                   // luôn trả lại, kể cả khi lỗi
}

var grantor = (ITransientGrantor)Dependency.Resolve<IPermissionService>();
grantor.Grant(PermissionKeys.Movie.Modify);           // tạm cho thêm một quyền
try { /* ... */ }
finally { grantor.UndoGrant(); }
```

Trạng thái được lưu theo request (hoặc theo thread nếu không có request), nên không ảnh hưởng user khác. Hãy giữ
`try/finally` thật chặt quanh đoạn cần quyền.

## 5. Phía client

Client không tự tính quyền. Khi trang tải, server gửi `UserDefinition` rút gọn qua dynamic script
(`ScriptUserDefinition.cs`, gồm `Username`, `DisplayName`, `IsAdmin` và `Permissions`, được gán trong
`UserEndpoint.GetUserData`). `Q.Authorization` ở TypeScript
đọc từ đó:

```ts
Q.Authorization.isLoggedIn
Q.Authorization.username
Q.Authorization.hasPermission('MovieDB:Movie:Modify')
```

Kết quả này chỉ để **ẩn/hiện giao diện**. Kiểm tra thật luôn ở server (bài 07, bài 14).

## 6. Bài tập

1. Đặt breakpoint trong `SiteInitialization.ApplicationStart` và trong `PermissionService.HasPermission`, chạy
   app và mở một trang. Mỗi lần mở trang `HasPermission` được gọi bao nhiêu lần?
2. Tạo user `test` thuộc một role có quyền `MovieDB:Movie:View`. Thêm một dòng `UserPermissions` với
   `Granted = 0` cho quyền đó. User còn xem được phim không? Giải thích bằng mục 3.3.
3. Thêm `PhoneNumber` vào `UserDefinition` và hiển thị nó trong một `Q.alert` (gợi ý: phải thêm vào
   `ScriptUserDefinition` và gán trong `UserEndpoint.GetUserData` mới xuống được client, rồi đọc bằng
   `Q.Authorization.userDefinition`).
4. Đăng ký `ImpersonatingAuthorizationService` và viết một action gọi `MovieRepository.Create` dưới quyền admin.
   `InsertUserId` của phim mới là ai?
5. Ở dự án thật: mở `SiteInitialization.cs`. Dự án có thay service nào trong 4 service ở mục 3 không? Có đăng ký
   thêm gì vào `Dependency` không?

## 7. Ghi chú của mình

-
