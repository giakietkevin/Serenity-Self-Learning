# Bài 22 – Tài khoản người dùng: đăng ký, quên mật khẩu, gửi email và Recaptcha

**Mục tiêu:** hiểu các trang trong module `Membership` của Serene (đăng nhập, đăng ký, kích hoạt, quên và đặt
lại mật khẩu, đổi mật khẩu), cách chúng tạo link có hạn dùng mà không cần bảng lưu token, cách cấu hình gửi email
thật, và cách thêm Recaptcha vào form.

Tham khảo (3.13.7):
- Serene: [Membership/Account/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Membership/Account) (`AccountPage.cs`, `SignUp/`, `ForgotPassword/`, `ResetPassword/`, `ChangePassword/`, `EnvironmentSettings.cs`), [Common/Helpers/EmailHelper.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/EmailHelper.cs), [Administration/User/UserRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/UserRepository.cs) (`ValidatePassword`)
- Serenity: [Serenity.Web/Security/RecaptchaValidation.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Security/RecaptchaValidation.cs), [Serenity.Scripts/CoreLib/UI/Editors/Recaptcha.ts](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Scripts/CoreLib/UI/Editors/Recaptcha.ts)
- Guide: `howto/how_to_use_recaptcha.md`

Bài 21 đã nói phần lõi: `WebSecurityHelper.Authenticate`, `AuthenticationService.Validate`, cookie Forms
Authentication. Bài này là phần giao diện và các luồng xung quanh.

---

## 1. Các trang trong `AccountController`

`AccountController` là MVC controller thường (không phải `ServiceEndpoint`), `[RoutePrefix("Account")]`. Mỗi
chức năng có một action `GET` trả về trang và một action `POST` nhận JSON:

| URL | GET | POST | Ghi chú |
|---|---|---|---|
| `Account/Login` | Trang đăng nhập | Kiểm tra mật khẩu, tạo cookie | Bài 21 |
| `Account/SignUp` | Form đăng ký | Tạo user `IsActive = 0`, gửi mail kích hoạt | |
| `Account/Activate?t=...` | Kích hoạt từ link trong mail | | Đặt `IsActive = 1` |
| `Account/ForgotPassword` | Form nhập email | Gửi mail chứa link đặt lại | |
| `Account/ResetPassword?t=...` | Form mật khẩu mới | Lưu mật khẩu mới | |
| `Account/ChangePassword` | Form đổi mật khẩu (`[Authorize]`) | Kiểm tra mật khẩu cũ, lưu mới | Cần đăng nhập |
| `Account/Signout` | Xóa session và cookie | | |

Phía client, mỗi trang có một panel TypeScript kế thừa `Serenity.PropertyPanel` (`LoginPanel`, `SignUpPanel`,
`ForgotPasswordPanel`...). Panel dùng form C# (`SignUpForm.cs` có `[FormScript("Membership.SignUp")]`), tự
`validateForm()`, rồi gọi `Q.serviceCall({ url: Q.resolveUrl('~/Account/SignUp'), ... })`.

Vì POST dùng `[HttpPost, JsonFilter]` và trả về `Result<ServiceResponse>`, lỗi được trả về giống service thường
(bài 18): ném `ValidationError` là client hiện đúng thông báo.

```csharp
[HttpPost, JsonFilter]
public Result<ServiceResponse> SignUp(SignUpRequest request)
{
    return this.UseConnection("Default", connection => { ... });   // chỉ connection
}

[HttpPost, JsonFilter]
public Result<ServiceResponse> ResetPassword(ResetPasswordRequest request)
{
    return this.InTransaction("Default", uow => { ... });          // có transaction
}
```

`UseConnection`, `InTransaction`, `ExecuteMethod` là cách viết của controller MVC thường. Chúng làm đúng việc mà
`ServiceEndpoint` tự làm cho tham số `IDbConnection`/`IUnitOfWork`.

> `AccountController.UseAdminLTELoginBox = false` chọn giữa hai kiểu giao diện trang đăng nhập (`AccountLogin.cshtml`
> và `AccountLogin.AdminLTE.cshtml`). Đổi thành `true` để dùng hộp đăng nhập kiểu AdminLTE.

## 2. Đăng ký và kích hoạt

Luồng `SignUp` (rút gọn từ code thật):

1. Kiểm tra email, mật khẩu (`UserRepository.ValidatePassword`: tối thiểu 5 ký tự), tên hiển thị.
2. Email đã dùng làm `Username` hoặc `Email` của user khác → `ValidationError("EmailInUse", ...)`.
3. Băm mật khẩu: `UserRepository.GenerateHash(password, ref salt)`.
4. Thêm `UserRow` với `Username = Email`, `Source = "sign"`, **`IsActive = 0`**.
5. Tạo token kích hoạt (mục 3), ghép thành link `.../Account/Activate?t=...`.
6. Render nội dung mail từ view Razor `AccountActivateEmail.cshtml` bằng `TemplateHelper.RenderTemplate(...)`.
7. `Common.EmailHelper.Send(subject, body, email)`, rồi `uow.Commit()`.

Khi user bấm link, `Activate(t)` giải mã token, kiểm tra hạn, kiểm tra user vẫn `IsActive == 0`, rồi
`UpdateById(new UserRow { UserId = ..., IsActive = 1 })` và `BatchGenerationUpdater.OnCommit(uow, UserRow.Fields.GenerationKey)`
để xóa cache user (bài 10, bài 21). Cuối cùng chuyển về `~/Account/Login?activated=email`.

`AuthenticationService` chấp nhận cả `Source = "site"` lẫn `Source = "sign"` khi so mật khẩu, và từ chối user
có `IsActive != 1`. Vì vậy user tự đăng ký nhưng chưa bấm link thì chưa đăng nhập được.

> User tự đăng ký **không có role nào**, nên chỉ thấy những trang không yêu cầu quyền. Muốn gán role mặc định,
> thêm một dòng `UserRoles` ngay trong `SignUp` (cùng `uow`).

## 3. Link có hạn dùng mà không cần bảng token

Cả kích hoạt lẫn đặt lại mật khẩu đều dùng cùng một cách:

```csharp
byte[] bytes;
using (var ms = new MemoryStream())
using (var bw = new BinaryWriter(ms))
{
    bw.Write(DateTime.UtcNow.AddHours(3).ToBinary());   // hết hạn sau 3 giờ
    bw.Write(user.UserId.Value);
    bw.Flush();
    bytes = ms.ToArray();
}
var token = Convert.ToBase64String(MachineKey.Protect(bytes, "ResetPassword"));
```

- `MachineKey.Protect` mã hóa và ký dữ liệu bằng `<machineKey>` của ứng dụng. Người dùng không đọc hay sửa được
  token. Chuỗi `"ResetPassword"`/`"Activate"` là *purpose*: token kích hoạt không dùng được để đặt lại mật khẩu.
- Khi nhận lại, `MachineKey.Unprotect(...)` giải mã, đọc thời điểm hết hạn và `UserId`. Sai hoặc hết hạn thì báo
  `InvalidResetToken` / `InvalidActivateToken`.
- Không có bảng lưu token nên token **dùng lại được** cho tới khi hết hạn. Nếu cần "chỉ dùng một lần", thêm một
  giá trị như `PasswordHash` cũ vào token rồi so sánh khi nhận lại.
- Nhiều server hoặc đổi `<machineKey>` thì link cũ không mở được nữa. Đây là lý do phải cố định `<machineKey>`
  khi có nhiều server (bài 15).

**Địa chỉ trong link** lấy từ `Config.Get<EnvironmentSettings>().SiteExternalUrl`, nếu không có thì lấy từ URL của
request hiện tại. Sau reverse proxy hoặc khi chạy dưới IIS bằng tên máy nội bộ, link trong mail sẽ sai, khi đó
khai báo trong `Web.config`:

```xml
<add key="EnvironmentSettings" value="{ SiteExternalUrl: 'https://movies.congty.vn/' }" />
```

Không có `[SettingKey]` nên key chính là tên class `EnvironmentSettings` (bài 15).

## 4. Quên, đặt lại và đổi mật khẩu

**ForgotPassword:** tìm user theo `Email`. Không có thì báo `CantFindUserWithEmail`. Có thì tạo token (mục 3)
và gửi mail chứa link `Account/ResetPassword?t=...`.

> Thông báo "không tìm thấy email" cho người ngoài biết email nào có tài khoản. Với hệ thống public, có thể đổi
> thành luôn báo "đã gửi mail nếu email tồn tại".

**ResetPassword:** kiểm tra token, kiểm tra `ConfirmPassword == NewPassword`, `ValidatePassword`, rồi tạo salt và
hash mới:

```csharp
var salt = Membership.GeneratePassword(5, 1);
var hash = SiteMembershipProvider.ComputeSHA512(request.NewPassword + salt);
uow.Connection.UpdateById(new UserRow { UserId = user.UserId.Value, PasswordSalt = salt, PasswordHash = hash });
BatchGenerationUpdater.OnCommit(uow, UserRow.Fields.GenerationKey);
```

**ChangePassword:** có `[ServiceAuthorize]` (phải đăng nhập). Kiểm tra mật khẩu cũ bằng chính
`IAuthenticationService.Validate` (bài 21), sau đó giống ResetPassword.

`UserRepository.CheckPublicDemo(userId)` chặn sửa user admin khi `appSettings` có `IsPublicDemo = 1`. Dự án
thật không bật cờ này.

**Muốn luật mật khẩu chặt hơn** (độ dài, có số, có chữ hoa): sửa một chỗ duy nhất là
`UserRepository.ValidatePassword`. Hàm này được gọi khi tạo user ở màn hình quản trị, đăng ký, đặt lại và đổi mật
khẩu.

## 5. Gửi email

`Common.EmailHelper.Send` dùng `System.Net.Mail.SmtpClient` mặc định, nên mọi cấu hình nằm ở `Web.config`:

```xml
<system.net>
  <mailSettings>
    <smtp deliveryMethod="SpecifiedPickupDirectory" from="no-reply@mysite.com">
    </smtp>
  </mailSettings>
</system.net>
```

Mặc định là `SpecifiedPickupDirectory`: mail **không được gửi**, mà được ghi thành file `.eml` trong
`App_Data/Mail` (helper tự tạo thư mục). Khi phát triển, mở file đó để lấy link kích hoạt/đặt lại mật khẩu.

Gửi thật qua SMTP:

```xml
<smtp deliveryMethod="Network" from="no-reply@congty.vn">
  <network host="smtp.congty.vn" port="587" enableSsl="true"
           userName="no-reply@congty.vn" password="..." />
</smtp>
```

Đừng commit mật khẩu SMTP vào git: dùng file config riêng của server hoặc config transform khi publish (bài 15).

Gửi mail trong một request đang có transaction thì nên dời ra sau khi commit (`uow.OnCommit`, bài 16), để mail
không được gửi khi dữ liệu bị rollback. Code `SignUp` của Serene gửi trước `Commit()`: nếu gửi lỗi thì user không
được tạo, đây cũng là một lựa chọn hợp lý.

Nội dung mail là một view Razor bình thường (`AccountResetPasswordEmail.cshtml`) có model riêng, render thành chuỗi
bằng `TemplateHelper.RenderTemplate(viewPath, model)`. Có thể dùng cách này cho mọi mail của hệ thống.

## 6. Recaptcha

Serenity 3.13.7 có sẵn đủ ba phần:

| Phần | Lớp |
|---|---|
| Cấu hình | `RecaptchaSettings` với `[SettingKey("Recaptcha")]`: `SiteKey`, `SecretKey` |
| Editor phía client | `Serenity.Recaptcha`, gắn bằng `[Recaptcha]` trên property của form. Editor tự nạp script của Google và thêm rule bắt buộc |
| Kiểm tra phía server | `Serenity.Web.RecaptchaValidation.Validate(token)`: gọi `siteverify` của Google, sai thì ném `ValidationError` |

Thêm vào trang đăng nhập:

**B1.** Đăng ký site ở `https://www.google.com/recaptcha/admin`, chọn reCAPTCHA v2 ("I'm not a robot"). Thêm vào
`Web.config`:

```xml
<add key="Recaptcha" value="{ SiteKey: '...', SecretKey: '...' }" />
```

**B2.** Trong Serene 3.13.7, `LoginRequest.cs` vừa là request vừa là form (`[FormScript("Membership.Login")]`),
khác với Guide cũ có `LoginForm.cs` riêng. Chỉ cần thêm một property (editor lấy `SiteKey` từ config):

```csharp
[DisplayName(""), Recaptcha]
public string Recaptcha { get; set; }
```

`LoginPanel.ts` gửi `this.getSaveEntity()`, tức mọi field của form, nên không cần sửa TypeScript. Build và chạy T4.

**B3.** Trong action `Login`, trước khi kiểm tra mật khẩu:

```csharp
Serenity.Web.RecaptchaValidation.Validate(request.Recaptcha);
```

Làm tương tự cho `SignUp` và `ForgotPassword`, hai trang hay bị spam nhất. Riêng `SignUpPanel.ts` tự liệt kê từng
field khi gọi service, nên phải thêm `Recaptcha: this.form.Recaptcha.value` vào request, và thêm property vào cả
`SignUpForm.cs` lẫn `SignUpRequest.cs`.

> Server phải gọi ra được `https://www.google.com`. Trên .NET Framework 4.5 có thể phải bật TLS 1.2
> (`ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;` trong `Application_Start`), nếu không
> `Validate` sẽ báo lỗi kết nối.

## 7. Bài tập

1. Đăng ký một tài khoản mới ở `/Account/SignUp`. Mở `App_Data/Mail`, lấy link trong file `.eml` để kích hoạt.
   Thử đăng nhập trước và sau khi kích hoạt.
2. Sửa `UserRepository.ValidatePassword` để bắt buộc ít nhất 8 ký tự và có một chữ số. Thử ở màn hình User,
   ChangePassword và ResetPassword.
3. Trong `SignUp`, gán role "Khách" cho user mới (thêm `UserRoleRow` bằng `uow.Connection.Insert`).
4. Đổi thời hạn link đặt lại mật khẩu từ 3 giờ thành 30 phút. Giải thích vì sao link cũ không cần xóa ở đâu cả.
5. Thêm Recaptcha cho trang đăng nhập theo mục 6. Dùng cặp key thử nghiệm trong Guide để chạy trên máy.
6. Ở dự án thật: hệ thống có cho tự đăng ký không? Mail được gửi bằng SMTP nào, cấu hình ở đâu?

## 8. Ghi chú của mình

-
