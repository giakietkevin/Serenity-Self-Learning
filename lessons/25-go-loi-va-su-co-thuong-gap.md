# Bài 25 – Gỡ lỗi và các sự cố thường gặp

**Mục tiêu:** có một cách tìm lỗi theo từng tầng (trình duyệt, service, SQL), biết bật log SQL, đặt breakpoint
vào **bên trong** mã nguồn Serenity, và tra nhanh nguyên nhân của các lỗi hay gặp nhất khi làm với Serenity 3.13.7.

Tham khảo (3.13.7):
- Guide: `howto/how_to_debugging_with_serenity_sources.md`, `faq/troubleshooting.md`, `faq/frequently_asked_questions.md`
- Serenity: [Serenity.Core/Logging/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Core/Logging) (`Log.cs`, `FileLogger.cs`, `LoggingLevel.cs`), [Serenity.Data/SqlHelpers/SqlHelper.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data/SqlHelpers/SqlHelper.cs)
- Serene: [App_Start/SiteInitialization.Migrations.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/App_Start/SiteInitialization.Migrations.cs), [Web.config](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Web.config)

Phần lỗi khi deploy lên IIS đã có ở bài 15. Bảng lỗi riêng của từng chủ đề có ở cuối bài 18, 24. Bài này gom lại
và thêm phần "cách tìm".

---

## 1. Tìm lỗi theo tầng

Một thao tác trên màn hình Serenity đi qua bốn tầng. Đi từ ngoài vào trong, dừng ở tầng đầu tiên thấy sai:

```
Trình duyệt (TypeScript)  →  Request JSON  →  Endpoint / Repository / Handler (C#)  →  SQL
     F12 Console              F12 Network        Breakpoint, /errorlog.axd               Log SQL, SQL Profiler
```

**Bước 1. F12 → Console.** Lỗi đỏ ở đây là lỗi phía client: thiếu script, gọi hàm chưa có (chưa chạy T4), sai
tên field. Nhờ `sourceMap: true` trong `tsconfig.json`, lỗi chỉ thẳng tới dòng trong file `.ts`, và đặt breakpoint
được ngay trong tab *Sources*.

**Bước 2. F12 → Network.** Tìm request tới `/Services/...`:
- *Payload*: client gửi gì. Thiếu field, sai tên, gửi chuỗi thay vì số?
- *Response*: server trả gì. Có `Error.Code`, `Error.Message` không? HTTP 400 là `ValidationError` (lỗi bạn chủ
  động ném), 500 là exception khác (bài 18).
- Không thấy request nào: lỗi nằm ở client trước khi gửi (validate form, `onViewSubmit` trả `false`,
  `getGridCanLoad()` trả `false`).

**Bước 3. Server.** Chạy bằng F5 trong Visual Studio, đặt breakpoint trong Endpoint/Repository. Hoặc xem
`/errorlog.axd` (bài 15): có đủ stack trace, kể cả lỗi bị giấu khỏi client vì `customErrors`.

**Bước 4. SQL.** Bật log SQL (mục 2), hoặc dùng SQL Server Profiler / Extended Events để xem câu lệnh thật. Chép
câu SQL ra SSMS chạy thử.

Mẹo nhỏ trong console:

```js
Q.getLookup('MovieDB.Genre').items           // lookup đang có những gì ở client
Q.getForm('MovieDB.Movie')                   // form có những field nào, editor gì
Q.Authorization.userDefinition                // user và quyền hiện tại (bài 21)
Serene.MovieDB.MovieService.List({}, r => console.log(r))   // gọi thử service
```

## 2. Log ra file, kể cả từng câu SQL

`Web.config` của Serene có sẵn:

```xml
<add key="Logging" value="{ File: '~\\App_Data\\Log\\App_{0}_{1}.log', FlushTimeout: 0, Level: 'Off' }" />
```

`CommonInitialization.InitializeLogging` đăng ký `FileLogger` đọc cấu hình này. Các mức (`LoggingLevel`), từ chi
tiết nhất: `Verbose`, `Debug`, `Info`, `Warn`, `Error`, `Fatal`, `Off`.

Đặt **`Level: 'Debug'`** thì `SqlHelper` ghi **mọi câu SQL** được chạy cùng tham số vào
`App_Data/Log/App_<ngày>_<số>.log`. Rất hữu ích khi muốn biết `List` thực sự chạy câu gì (join nào, `WHERE` nào,
behavior nào đã thêm điều kiện). Nhớ tắt lại sau khi dò xong, vì file log lớn rất nhanh.

Tự ghi log từ code của mình:

```csharp
Log.Info("Bắt đầu import " + file, GetType());
if (Log.IsDebugEnabled)
    Log.Debug("Dòng " + i + ": " + JSON.Stringify(row), GetType());
ex.Log();                                     // ghi exception vào errorlog (bài 15)
```

Muốn xem SQL của một `SqlQuery` mà không bật log: `query.ToString()` (bài 16).

## 3. Debug vào mã nguồn Serenity

Khi cần biết **vì sao** framework làm vậy (ví dụ handler bỏ qua field của mình), bước vào code của Serenity.

**Cách 1: Source server (khuyên dùng).** Gói NuGet Serenity có kèm file `.pdb` trỏ tới mã nguồn trên GitHub.

1. Visual Studio → *Tools* → *Options* → *Debugging* → *General*:
   - Bật **Enable source server support**.
   - Tắt **Enable Just My Code**.
2. Đặt breakpoint trong code của mình, ví dụ `MovieRepository.MyListHandler.ApplyFilters`.
3. F5, mở trang, khi dừng ở breakpoint bấm F11 (*Step Into*) vào `base.ApplyFilters(...)`. Visual Studio tải file
   nguồn tương ứng từ GitHub.
4. Cửa sổ *Call Stack* cho thấy toàn bộ đường đi từ `ServiceEndpoint` → `ListRequestHandler.Process` → hàm của bạn.

Nếu Visual Studio báo không tìm thấy symbol: *Debug* → *Windows* → *Modules*, tìm `Serenity.Services.dll`, chuột
phải → *Load Symbols*.

**Cách 2: Thêm Serenity làm git submodule** và tham chiếu project thay vì gói NuGet. Chỉ dành cho người đã quen
git và cấu trúc solution: mất khả năng cập nhật bằng NuGet, và chính Guide khuyên người mới không làm.

**Cách 3: Đọc mã nguồn đúng phiên bản.** Các bài trong khóa đều trỏ tới commit
`7cffedf3` của Serenity (bản 3.13.7). Đọc trực tiếp trên GitHub thường đủ để hiểu, nhanh hơn debug.

Phía client: `Scripts/serenity/Serenity.CoreLib.js` không bị nén khi bundling tắt, nên đặt breakpoint thẳng vào
đó trong F12 → *Sources* được.

## 4. Bảng tra sự cố thường gặp

### 4.1. Khởi tạo và build

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| Hàm, form, service mới không có ở TypeScript (`... is not a function`, `Property 'X' does not exist`) | Chưa build C# rồi chạy T4 (*Build* → *Transform All T4 Templates*). Thứ tự luôn là: build → T4 → build lại |
| Enum không có ở phía TypeScript sau khi chạy T4 | Enum chỉ được chuyển sang khi được dùng trong Row hoặc request/response. Thêm `[ScriptInclude]` lên enum |
| Không thấy attribute editor/formatter tự viết ở C# | Chưa build TypeScript rồi chạy T4 (bài 13) |
| Sau khi tạo project, trang login báo không tìm thấy template | Guide ghi nhận lỗi này khi tên solution có dấu chấm (`Cty.QuanLy`). Đặt tên không có dấu chấm |
| Sergen báo lỗi KDiff3 khi sinh lại code | Sergen dùng KDiff3 để trộn code cũ và mới. Cài KDiff3, hoặc sửa đường dẫn trong `Serenity.CodeGenerator.config` (bài 03). Tốt nhất chỉ sinh code một lần, sau đó thêm field bằng tay |

### 4.2. Dữ liệu và SQL

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| `Incorrect syntax near 'OFFSET'` khi đổi trang | SQL Server 2008 trở về trước không có `OFFSET/FETCH`. Đặt dialect trong `SiteInitialization`: `SqlConnections.GetConnectionString("Default").Dialect = SqlServer2008Dialect.Instance;` |
| `Table not found` (ví dụ bảng Users) sau khi publish | Migration bị bỏ qua: `RunMigrations` chỉ chạy khi chuỗi kết nối chứa tên DB dạng `<Namespace>_<Key>_v1` (ví dụ `MovieLab_Default_v1`). Đổi tên DB hoặc bỏ đoạn kiểm tra này (bài 02, 15) |
| Bảng con master–detail: mở lại bản ghi thì các cột view (tên sản phẩm...) trống | Handler chỉ nạp table field của bảng con. Thêm `[MinSelectLevel(SelectLevel.List)]` cho các view field đó (bài 08, 17) |
| Upload, ghi chú báo `NotImplementedException` | Một số behavior cũ cần khóa chính kiểu số tự tăng |
| `ExpectedRows` / "Query affected N rows while 1 expected" | `Execute()` mặc định mong đúng 1 dòng. Dùng `ExpectedRows.Ignore` khi ghi nhiều dòng (bài 06, 16) |
| Request bị treo tới timeout | Mở connection thứ hai trong lúc đang có transaction (bài 16, mục 6) |

### 4.3. Form, editor, grid

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| Dropdown cascade thứ hai luôn trống | `CascadeFrom` là **id editor trên form** (tên property của form), `CascadeField` là **tên field trong Row của lookup**. Sai hoa thường (`CountryID` khác `CountryId`) là trống. Dùng `nameof(...)`. Field đó còn phải có `[LookupInclude]` (bài 05, 13) |
| Tab trong template không hiện hoặc trống | Đặt `div` của tab này lồng bên trong `div` của tab khác. Các tab phải ngang hàng (bài 19) |
| Bảng con in-memory (grid editor) bị **nhân đôi dòng** khi lưu | Dialog con đã override `getIdProperty()`. `GridEditorDialog` dùng Id giả `"__id"` để phân biệt dòng; đổi nó thành Id thật (đang null) thì mọi dòng đều bị coi là dòng mới (bài 08) |
| Lookup đã sửa nhưng dropdown chưa cập nhật | Cache lookup ở client. `Q.reloadLookup('Module.Key')`, và kiểm tra cache phía server (bài 10) |
| `DecimalEditor` không cho nhập số âm | Đặt `[DecimalEditor(MinValue = "-999999999.99", MaxValue = "999999999.99")]`, cùng số chữ số |
| Muốn ngày mặc định là hôm nay khi tạo mới | `[DefaultValue("today")]` (ngày) hoặc `[DefaultValue("now")]` (ngày giờ) **trên form**, không đặt trên Row |
| Sau khi cập nhật Select2 thì dropdown lỗi | Serenity 3.13.7 dùng Select2 **3.5.1** (`packages.config`). Không nâng gói `Select2.js`. Nếu đã nâng: `Update-Package Select2.js -Version 3.5.1` |

### 4.4. Phân quyền và menu

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| Trang không hiện trong menu | Quyền của menu lấy từ `[PageAuthorize]` trên action `Index` của `XyzPage.cs`. User phải có quyền đó (bài 07) |
| Quyền mới không hiện trong cây phân quyền | Thêm vào class `PermissionKeys` có `[NestedPermissionKeys]` (bài 07). Chỉ khai báo hằng ở chỗ khác thì không tự đăng ký |
| Đổi quyền trên Row xong thì grid lỗi, không có dữ liệu | Endpoint còn `[ServiceAuthorize("QuyềnCũ")]`. Sửa cho khớp với quyền trên Row |
| User `admin` thấy hết nhưng user khác không thấy | `PermissionService` luôn trả `true` cho username `admin` (bài 21). Thử bằng một user thường |
| Muốn tách quyền thêm và quyền sửa | Dùng `[InsertPermission]` và `[UpdatePermission]` thay vì `[ModifyPermission]`. Thứ tự tìm: Insert → Modify → Read (khi thêm), Update → Modify → Read (khi sửa), Delete → Modify → Read (khi xóa) |

### 4.5. Đa ngôn ngữ

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| Bản dịch làm trên màn hình Translations bị mất sau khi publish | Chúng được lưu trong `App_Data/texts/`. Chép thư mục này lên server, hoặc chuyển nội dung vào `Scripts/site/texts/` (bài 09) |
| `Q.text('MyModule.Abc')` ở client trả về chính key | Chỉ các key có tiền tố trong `LocalTextPackages` (`Controls.`, `Db.`, `Dialogs.`, `Enums.`, `Forms.`, `Permission.`, `Site.`, `Validation.`) được gửi xuống client. Đổi key theo một tiền tố đó, hoặc thêm tiền tố mới vào `Web.config` |

### 4.6. Publish

| Hiện tượng | Nguyên nhân và cách xử lý |
|---|---|
| Thiếu CSS, ảnh, script sau khi publish | File chưa được thêm vào project hoặc *Build Action* không phải *Content* |
| Lỗi đọc file, upload | User của Application Pool (`IIS_IUSRS`) thiếu quyền trên thư mục (bài 11, 15) |
| `FieldAccessException: Cannot set a constant field` | Hosting chạy ứng dụng ở *Medium Trust*. Serenity khởi tạo Field bằng reflection nên cần *Full Trust* (`<trust level="Full" />`) hoặc đổi hosting |

## 5. Khi hỏi người khác

Khi đăng câu hỏi lên diễn đàn, GitHub issue hay hỏi đồng nghiệp, gửi kèm:

1. Phiên bản: Serenity 3.13.7, .NET Framework 4.5, SQL Server bao nhiêu.
2. Request và response lấy từ F12 → *Network*.
3. Stack trace trong `/errorlog.axd`.
4. Đoạn code liên quan (Row, Repository, Grid/Dialog), đã bỏ thông tin nhạy cảm.
5. Câu SQL lấy từ log (mục 2) nếu lỗi liên quan dữ liệu.

## 6. Bài tập

1. Đặt `Level: 'Debug'`, mở trang Movie, đọc file log và tìm câu `SELECT` của `List`. Thêm một quick filter rồi so
   sánh câu SQL.
2. Bật source server và tắt *Just My Code*. Đặt breakpoint trong `MovieRepository.MySaveHandler`, bước vào
   `SaveRequestHandler.Process` và tìm chỗ gọi `ValidateRequest`, `SetInternalFields`, `BeforeSave`.
3. Cố ý gây ba lỗi: gõ sai `CascadeField`, quên chạy T4 sau khi thêm một action, đặt `getIdProperty()` trong một
   dialog bảng con. Ghi lại triệu chứng của từng lỗi.
4. Trong console, gọi `Serene.MovieDB.MovieService.List({ Take: 5 }, r => console.log(r))` và đọc kết quả.
5. Ở dự án thật: lần gần nhất bạn mất nhiều thời gian sửa lỗi là lỗi gì? Thêm nó vào bảng mục 4 trong phần ghi chú.

## 7. Ghi chú của mình

-
