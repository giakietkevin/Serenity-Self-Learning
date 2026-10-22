# Bài 15 – Cấu hình, bundling và triển khai lên IIS

**Mục tiêu:** hiểu các mục cấu hình trong `Web.config` của Serene 3.13.7, bật gộp/nén script khi chạy thật,
publish lên IIS và biết các lỗi hay gặp sau khi deploy.

Tham khảo (Serene 3.13.7):
- [Web.config](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Web.config), [Web.Release.config](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Web.Release.config)
- [App_Start/SiteInitialization.Migrations.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/App_Start/SiteInitialization.Migrations.cs), [SiteInitialization.ExceptionLog.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/App_Start/SiteInitialization.ExceptionLog.cs)
- [Scripts/site/ScriptBundles.json](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Scripts/site/ScriptBundles.json), [Content/site/CssBundles.json](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Content/site/CssBundles.json)
- Guide: `howto/how_to_enable_script_bundling.md`, `configuration_system/`

---

## 1. Các mục cấu hình trong `appSettings`

```xml
<appSettings>
  <add key="LocalTextPackages" value="{ Site: [ 'Controls.', 'Db.', ... ], Login: [ ... ] }" />
  <add key="Logging" value="{ File: '~\\App_Data\\Log\\App_{0}_{1}.log', FlushTimeout: 0, Level: 'Off' }" />
  <add key="CDNSettings" value="{ Enabled: false, Url: '', ... }" />
  <add key="CssBundling" value="{ Enabled: false, Minimize: true, UseMinCss: true }" />
  <add key="ScriptBundling" value="{ Enabled: false, Minimize: true, UseMinJS: true, NoMinimize: [...] }" />
  <add key="UploadSettings" value="{ Path: '~\\App_Data\\upload\\' }" />
</appSettings>
```

| Key | Ý nghĩa | Bài liên quan |
|---|---|---|
| `LocalTextPackages` | Tiền tố key bản dịch được gửi xuống client | 09 |
| `Logging` | Ghi log ra file. `Level: 'Off'` là tắt. Đổi thành `'Info'` hoặc `'Error'` khi cần dò lỗi | |
| `CssBundling`, `ScriptBundling` | Gộp và nén CSS/JS | mục 3 |
| `UploadSettings` | Thư mục gốc lưu file upload | 11 |
| `EnvironmentSettings` | **Không có sẵn**, tự thêm khi cần: `{ SiteExternalUrl: '...' }`, dùng cho báo cáo PDF và link trong email | 12 |

**Cách Serenity đọc cấu hình:** mỗi key là một chuỗi JSON (cho phép viết kiểu JS, không cần ngoặc kép cho
tên thuộc tính). Trong code, `Config.Get<UploadSettings>()` đọc key **trùng tên class** (hoặc tên trong
`[SettingKey("...")]`). Tự thêm cấu hình cho module của mình:

```csharp
public class MovieDBSettings
{
    public int MaxGalleryImages { get; set; }
}

// Web.config: <add key="MovieDBSettings" value="{ MaxGalleryImages: 10 }" />
var settings = Config.Get<MovieDBSettings>();
```

## 2. Chuỗi kết nối và migration khi lên server thật

```xml
<connectionStrings>
  <add name="Default" connectionString="Data Source=(LocalDb)\MSSqlLocalDB; Initial Catalog=Serene_Default_v1; Integrated Security=True" providerName="System.Data.SqlClient" />
  <add name="Northwind" connectionString="... Initial Catalog=Serene_Northwind_v1; ..." providerName="System.Data.SqlClient" />
</connectionStrings>
```

Khi app khởi động, `SiteInitialization.ApplicationStart` chạy hai bước cho **mỗi** connection trong
`databaseKeys`:

1. `EnsureDatabase`: nếu database **chưa tồn tại thì tạo mới** (`CREATE DATABASE`). Hàm này **không** có kiểm
   tra tên.
2. `RunMigrations`: chỉ chạy nếu chuỗi kết nối chứa `Serene_<Key>_v1` (bài 02). Không khớp thì bỏ qua và đặt
   `SkippedMigrations = true`.

Hệ quả khi đưa lên production:

| Tình huống | Điều xảy ra |
|---|---|
| Tên DB production là `Serene_Default_v1` | Migration chạy tự động mỗi lần app khởi động. Tiện, nhưng mọi migration mới sẽ chạy thẳng vào production ngay khi deploy |
| Tên DB khác, ví dụ `CongTy_Prod` | Migration **không** chạy. Phải chạy script SQL bằng tay, hoặc sửa điều kiện an toàn |
| Gõ sai tên DB trong chuỗi kết nối | `EnsureDatabase` **tạo một database rỗng** với tên sai, nếu tài khoản SQL có quyền tạo DB |
| Tài khoản SQL không có quyền `CREATE DATABASE` | Khởi động lỗi nếu DB chưa có. DB đã có thì vẫn chạy bình thường |

> Hãy hỏi team xem dự án thật đang làm theo cách nào. Nhiều dự án xóa `EnsureDatabase` và tự quản lý migration.

Muốn chuỗi kết nối khác nhau giữa máy dev và server, dùng **Web.config transform** trong
`Web.Release.config`:

```xml
<connectionStrings>
  <add name="Default" connectionString="Data Source=SQLPROD; Initial Catalog=Serene_Default_v1; User Id=serene; Password=***"
       xdt:Transform="SetAttributes" xdt:Locator="Match(name)" />
</connectionStrings>
<system.web>
  <compilation xdt:Transform="RemoveAttributes(debug)" />    <!-- Serene đã có sẵn dòng này -->
</system.web>
```

## 3. Gộp và nén script (bundling)

Ở chế độ dev, `_LayoutHead.cshtml` nạp **hàng chục file JS và CSS riêng lẻ** (hơn 3MB). Khi chạy thật nên gộp lại:

```xml
<add key="CssBundling" value="{ Enabled: true, Minimize: true, UseMinCss: true }" />
<add key="ScriptBundling" value="{ Enabled: true, Minimize: true, UseMinJS: true, NoMinimize: ['~/Scripts/ckeditor/ckeditor.js'] }" />
```

- Danh sách file của từng bundle nằm ở `Scripts/site/ScriptBundles.json` và `Content/site/CssBundles.json`.
  `_LayoutHead.cshtml` gọi `@Html.ScriptBundle("Base")`, `@Html.ScriptBundle("Site")`...
- Bật bundling thì mỗi bundle thành một file, được nén và có tham số phiên bản để trình duyệt tự tải lại khi
  nội dung đổi.
- Thêm thư viện JS của bên thứ ba thì **thêm vào `ScriptBundles.json`**, đừng chỉ thêm thẻ `<script>` vào layout.
- Nếu bật bundling mà có lỗi JS lạ, tạm tắt `Minimize` để xem lỗi có phải do nén hay không.

## 4. Build trước khi publish: những bước dễ quên

| Bước | Vì sao |
|---|---|
| **Transform All T4 Templates** rồi build lại | Để `Imports/ServerTypings` và `Imports/ClientTypes` khớp với C# mới nhất |
| Kiểm tra **TypeScript không lỗi** | Trong `Serene.Web.csproj`, bước biên dịch TS có `ContinueOnError="true"`. Build vẫn "thành công" dù TS lỗi, và `Scripts/site/Serene.Web.js` không được cập nhật |
| File `.cs` mới đã **Include In Project** | Project kiểu cũ (.NET Framework) chỉ biên dịch file có trong `.csproj` |
| File `.ts`, `.cshtml`, `.json` mới có trong `.csproj` dạng `Content` | Publish chỉ chép file có trong project. Thiếu `.cshtml` thì trang lỗi 500 trên server |
| `wkhtmltopdf.exe` có trong `bin` | Cần cho báo cáo PDF (bài 12) |

## 5. Publish và cấu hình IIS

1. Visual Studio: chuột phải `Serene.Web` → **Publish** → *Folder* (hoặc Web Deploy), cấu hình **Release**.
2. Trên server: cài **.NET Framework 4.5.1 trở lên** (project nhắm `v4.5.1`), bật IIS với **ASP.NET 4.x**.
3. Tạo **Application Pool**: .NET CLR v4.0, *Integrated* pipeline.
4. Tạo Site hoặc Application trỏ vào thư mục publish.
5. **Cấp quyền ghi** cho tài khoản app pool (`IIS AppPool\<TênPool>`) vào:
   - `App_Data` (file bản dịch sửa trên giao diện, log, và `upload` nếu để mặc định)
   - thư mục upload nếu đặt bên ngoài
6. Nếu SQL Server dùng *Integrated Security*: cấp quyền DB cho tài khoản app pool, hoặc chuyển sang tài khoản SQL.

## 6. Xem lỗi trên server

- Serene ghi exception vào bảng `Exceptions` trong DB Default (StackExchange.Exceptional, cấu hình trong
  `SiteInitialization.ExceptionLog.cs`).
- Xem tại **`/errorlog.axd`**. Trong `Web.config` chỉ user tên `admin` được vào:

  ```xml
  <location path="errorlog.axd">
    <system.web><authorization><allow users="admin" /><deny users="*" /></authorization></system.web>
  </location>
  ```

- `<customErrors mode="Off" />` cho người dùng thấy chi tiết lỗi. Trên production nên đổi thành `RemoteOnly`
  trong `Web.Release.config`.

## 7. Bảng tra lỗi sau khi deploy

| Hiện tượng | Kiểm tra |
|---|---|
| Trang trắng, F12 báo `Serene is not defined` / `... is not a constructor` | `Serene.Web.js` cũ hoặc thiếu. Build lại, xem lỗi TypeScript |
| Dialog không mở: `Can't find ... enum type!` hoặc thiếu form | Chưa chạy T4 trước khi build |
| Lỗi 500 ngay khi khởi động | Chuỗi kết nối, quyền SQL, migration lỗi. Xem Event Viewer và `/errorlog.axd` |
| Dropdown thiếu dữ liệu mới | Sửa dữ liệu trực tiếp trong SQL, cache chưa hết hạn (bài 10). Recycle app pool |
| Upload lỗi *Access denied* | Quyền ghi thư mục upload (bài 11) |
| PDF trắng hoặc timeout | `SiteExternalUrl`, firewall, `wkhtmltopdf.exe` (bài 12) |
| Chữ hiện dạng key `Db.MovieDB.Movie.Title` | Thiếu file `Scripts/site/texts/*.json` trên server hoặc key không nằm trong `LocalTextPackages` (bài 09) |
| Đăng nhập xong bị đá ra liên tục khi có nhiều server | Mỗi server tự sinh `machineKey` khác nhau. Khai báo `<machineKey>` giống nhau cho các server |

## 8. Bài tập

1. Thêm `MovieDBSettings` vào `Web.config` và đọc nó trong `MovieRepository` để giới hạn số ảnh gallery.
2. Bật `ScriptBundling` và `CssBundling`, mở F12 → *Network* so sánh số request và dung lượng trước và sau.
3. Publish Serene ra một thư mục, dựng trên IIS của máy mình với app pool riêng. Thử upload ảnh và xuất PDF.
4. Cố ý gây lỗi (ví dụ chia cho 0 trong một endpoint), rồi xem lỗi ở `/errorlog.axd`.
5. Ở dự án thật: hỏi team quy trình deploy hiện tại. Migration chạy tự động hay bằng script? `Web.Release.config`
   đang thay những gì?

## 9. Ghi chú của mình

-
