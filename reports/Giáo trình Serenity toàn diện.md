# Học Serenity 10.6 từng bước, vững từ gốc

Serenity là một nền tảng mã nguồn mở (giấy phép MIT) gồm ASP.NET Core và TypeScript, dùng để xây các ứng dụng nghiệp vụ nặng về dữ liệu: nhiều bảng, nhiều form, nhiều trang danh sách và báo cáo. Bạn mô tả **entity (Row) bằng C# có gắn attribute**, rồi framework sinh ra service CRUD, grid, dialog, lookup, phân quyền và bản địa hóa ([Serenity Docs](https://serenity.is/docs/)). Tính đến tháng 10/2026, phiên bản hiện hành là **Serenity 10.6.0 (phát hành 2026-10-11), chạy trên .NET 10**, đi kèm template miễn phí Serene.Templates 10.6.0 ([NuGet Serenity.Net.Web](https://packages.nuget.org/packages/Serenity.Net.Web/); [NuGet Serene.Templates](https://packages.nuget.org/packages/Serene.Templates)). Với người mới học, có ba điều quan trọng nhất. Thứ nhất, phần lớn hướng dẫn trên mạng đã lỗi thời. Những bài dùng `namespace`, Saltarelle, jQuery hay `@Decorators` đều thuộc các thế hệ trước v9. Thứ hai, **từ 10.6 phân quyền chuyển sang "fail-closed"**: Row nào không khai báo quyền thì bị từ chối truy cập ([Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)). Thứ ba, con đường học hiệu quả nhất là đi theo tutorial Movie chính thức, sau đó đọc mã nguồn Northwind nằm trong monorepo `serenity-is/Serenity`, không đọc repo Serene cũ đã bị lưu trữ (archived) ([serenity-is/Serenity](https://github.com/serenity-is/Serenity)). Giáo trình này chia kiến thức thành Phần 0 (bức tranh lớn) và 17 bài, đi từ cơ bản đến nâng cao. Mỗi bài có mục tiêu, giải thích khái niệm, code thật trích từ tài liệu và mã nguồn chính thức, các bẫy thường gặp và bài tập tự kiểm tra.

> **Cách đọc giáo trình.** Thầy dùng hai nhãn để bạn biết mức độ chắc chắn của từng thông tin. **[Suy luận]** đánh dấu điều thầy suy ra từ code, quy ước hoặc kinh nghiệm chung, không có trang tài liệu chính thức xác nhận. **[Khoảng trống]** đánh dấu chỗ nhóm nghiên cứu chưa đọc được nguồn gốc, nên bạn cần tự kiểm chứng trên project của mình. Phần không mang nhãn có trích dẫn nguồn đi kèm. Khi thấy hai nguồn mâu thuẫn, thầy nói rõ là mâu thuẫn chứ không tự chọn một bên.

---

## Phần 0 — Bức tranh lớn: Serenity là "khung", Serene là "căn nhà mẫu"

### 0.1 Serenity giải quyết vấn đề gì

Một ứng dụng nghiệp vụ điển hình có hàng chục bảng. Mỗi bảng lại cần cùng một bộ thứ: API danh sách có phân trang, lọc và sắp xếp; API thêm, sửa, xóa có kiểm tra hợp lệ; một trang grid; một dialog form; dropdown tra cứu; phân quyền; đa ngôn ngữ; cache. Serenity tồn tại để bạn **không phải viết lại bộ khung đó cho từng bảng**. Theo tài liệu, nó "giảm boilerplate và xử lý các mối quan tâm cắt ngang như caching, localization, validation và access control", và đã là mã nguồn mở từ năm 2015 ([Serenity Docs](https://serenity.is/docs/)). README trên GitHub mô tả nó là nền tảng cho "data-centric business applications with a service-based architecture" ([GitHub serenity-is/Serenity](https://github.com/serenity-is/Serenity)).

Bạn nên tách bạch ba khái niệm ngay từ đầu. **Serenity** là framework, phát hành dưới dạng các gói NuGet (`Serenity.Net.Core`, `Serenity.Net.Services`, `Serenity.Net.Web`, `Serenity.Extensions`) và các gói npm (`@serenity-is/corelib`, `@serenity-is/domwise`, `@serenity-is/sleekgrid`) ([Serenity Docs](https://serenity.is/docs/); [Frontend overview](https://serenity.is/docs/framework/ui/readme)). **Serene** và **StartSharp** là hai *template ứng dụng* xây sẵn trên framework đó. **Sergen** là công cụ sinh mã (scaffolding), cài sẵn trong template dưới dạng .NET local tool ([Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)).

### 0.2 Serene hay StartSharp: học bằng Serene là đủ

| | Serene | StartSharp |
|---|---|---|
| Giấy phép | MIT, miễn phí, dùng thương mại được | Thương mại, tính theo seat. Không dùng được cho app mã nguồn mở |
| Gói bán | — | Freelancer (1 seat), Business (3), Enterprise (5). Mỗi lần mua có 12 tháng cập nhật và hỗ trợ, không tự gia hạn |
| Nội dung | Module demo Northwind, Basic Samples, quản lý User/Role, Localization | Toàn bộ Serene, cộng thêm theme cao cấp, Data Audit Log, Background Task, Batch Mail Queue, 2FA, Login As User, Implicit Permissions, Email Client, Dynamic Data Explorer… |
| Lệnh tạo | `dotnet new serene` | `dotnet new startsharp` (cần nguồn gói premium) |

Nguồn: [StartSharp intro](https://serenity.is/docs/startsharp/README); [Serene tour](https://serenity.is/docs/serene_tour/README); [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new). Trang tài liệu không công bố giá StartSharp **[Khoảng trống]**. Theo thầy, **Serene hoàn toàn đủ để học và để làm dự án thật**. StartSharp chủ yếu đáng tiền ở phần giao diện chau chuốt và các module làm sẵn **[Suy luận]**. Suốt giáo trình, thầy sẽ đánh dấu những tính năng chỉ có ở StartSharp để bạn không mất công tìm chúng trong Serene.

### 0.3 Kiến trúc: một project, tổ chức theo tính năng

Một solution Serene chỉ có **một project ASP.NET Core** tên `<Tên>.Web`. Mã nguồn được chia theo tính năng: mỗi entity có một thư mục `Modules/<Module>/<Entity>/`, bên trong chứa Row, Columns, Form, Endpoint, các handler, Page, cùng các file TSX của grid và dialog ([Movie tutorial ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui)). Hãy hình dung các tầng như sau:

| Tầng | Thành phần | Bạn viết gì |
|---|---|---|
| Dữ liệu | `XRow.cs` (entity), migration FluentMigrator | Ánh xạ bảng/cột bằng attribute |
| Dịch vụ | `XEndpoint.cs`, `XListHandler`/`XSaveHandler`…, behaviors | Logic nghiệp vụ trong các hook |
| Mô tả giao diện (C#) | `XForm.cs`, `XColumns.cs`, Lookup | Danh sách field và attribute hiển thị |
| Cầu nối sinh mã | `Modules/ServerTypes/*.ts` (Sergen sinh khi build) | Không sửa tay |
| Giao diện (TS) | `XGrid.tsx`, `XDialog.tsx`, `XPage.ts(x)` | Override các hook của grid/dialog |
| Trang & menu | `XPage.cs` (controller), `[NavigationLink]` | Route và quyền trang |

Các framework thông thường bắt bạn viết DTO, controller và component riêng cho từng bảng. Serenity làm khác: **attribute trên C# là nguồn sự thật duy nhất**. Form và Columns kế thừa metadata từ Row qua `[BasedOnRow]`, và Sergen biến chúng thành kiểu TypeScript để phía client gọi service an toàn kiểu ([Forms & Editors](https://serenity.is/docs/framework/forms-editors); [Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)).

### 0.4 Một request đi từ trình duyệt xuống database như thế nào

Hiểu được luồng này thì bạn sẽ gỡ lỗi nhanh gấp đôi. Thầy vẽ lại theo tài liệu Service Endpoints và release notes:

```
[Trình duyệt]  XGrid (EntityGrid) cần dữ liệu
      │  XService.List(...)  → serviceCall POST JSON
      ▼
POST /Services/{Module}/{Entity}/List          (ví dụ /Services/Northwind/Product/List)
      │
[ServiceEndpoint controller]  [Route("Services/Northwind/Product/[action]")]
      │  [ServiceAuthorize]  → chưa đủ quyền thì trả lỗi NotAuthorized (không redirect)
      │  [JsonRequest]       → deserialize body thành ListRequest (async từ 10.1)
      │  [ConnectionKey]     → ISqlConnections.NewByKey(...)
      │      đọc (List/Retrieve) → IDbConnection
      │      ghi (Create/Update/Delete) → IUnitOfWork (commit khi thành công, rollback khi lỗi)
      ▼
[Request handler]  ListRequestHandlerAsync<ProductRow>
      │  hook: ValidateRequest → PrepareQuery → ApplyFilters → ... + behaviors
      │  10.6: Row không có permission → bị từ chối (fail-closed)
      ▼
[SqlQuery / Fluent SQL]  → SQL có tham số, theo dialect (SQL Server, Postgres, SQLite...)
      ▼
[Database] → ListResponse<ProductRow> { Entities, TotalCount, ... } → JSON → grid vẽ dữ liệu
```

Nguồn: [Service Endpoints](https://serenity.is/docs/services/service_endpoints); [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0); [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0). Trong sơ đồ này, **endpoint chỉ là lớp vỏ mỏng** chuyển request cho handler, mọi logic nằm ở handler và behavior. Khi bị lỗi "không có dữ liệu", bạn kiểm tra lần lượt ba trạm: quyền ở endpoint, hook trong handler, và SQL sinh ra.

### 0.5 Bản đồ thời gian: những gì bạn không được làm theo nữa

Lời dặn quan trọng nhất của Phần 0 là **luôn kiểm tra một hướng dẫn được viết cho phiên bản nào** trước khi làm theo.

| Phiên bản | Thời điểm | Mốc chính |
|---|---|---|
| 1.x–5.x | 2015–2022 | Thời Saltarelle (C#→JS), T4, ASP.NET MVC, rồi chuyển sang ASP.NET Core dùng `namespace` |
| ~6.1 | 2022 | ES modules trở thành mặc định. Từ 6.1.8 không còn migration guide riêng |
| (không có 7.x) | — | — |
| 8.0 | 11/2023 | .NET 8, Bootstrap 5.3, loại bỏ tàn dư Saltarelle và asset cũ |
| 9.0 | 09/2025 | Bỏ jQuery UI/plugin khỏi Assets, `[Symbol.typeInfo]` thay decorator, ClosedXML thay EPPlus, gộp `Serenity.Net.Data/Entity` vào `Serenity.Net.Services` |
| 10.0/10.1 | 2025–01/2026 | .NET 10, VS 2026, ES2022, `.slnx`, bỏ namespace `Q` |
| 10.6 | 10/2026 | Phân quyền fail-closed, bắt buộc đăng ký `IImageProcessor`, nhiều thay đổi phá vỡ API truy vấn |

Nguồn: [Versions](https://serenity.is/docs/versions); [Migration index](https://serenity.is/docs/migration/README); [8.0.0](https://serenity.is/docs/release-notes/8.0.0); [9.0.0](https://serenity.is/docs/release-notes/9.0.0); [10.1.0](https://serenity.is/docs/release-notes/10.1.0); [10.6.0](https://serenity.is/docs/release-notes/10.6.0). Các nguồn chưa thống nhất về ngày của 10.0.0. CHANGELOG ghi 10.0.0 là "First .NET 10 version" và đặt ngày 2025-11-24 ([changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)). Trang Versions lại gán ngày 2025-11-24 cho 9.2.0, và mục lục release notes trên docs bắt đầu từ 10.1.0 **[Khoảng trống]**. Nếu bạn còn dùng VS 2022 hoặc .NET 8, tài liệu khuyên ở lại nhánh **9.2.x** ([10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)).

---

## Chặng 1 — Nền móng: cài đặt, tutorial Movie và Row

### Bài 1. Cài đặt và giải phẫu một project Serene

**Mục tiêu.** Tạo và chạy được một project Serene 10.6, đăng nhập vào, và biết mỗi thư mục chính dùng để làm gì.

**Khái niệm.** Bạn cần bốn thứ. Một là **.NET 10 SDK** (nếu dùng Visual Studio thì phải là VS 2026 bản 18.8 trở lên). Hai là **Node.js LTS** (ví dụ trong tài liệu dùng node 24.10.0 và npm 11.1.1). Ba là **TypeScript 6.0.3+**, được tham chiếu qua `Microsoft.TypeScript.MSBuild`. Bốn là một trình soạn thảo: VS 2026, hoặc CLI kết hợp VS Code ([Getting Started](https://serenity.is/docs/getting_started/README)). Trên Windows, nếu Visual Studio dùng bản Node cũ đi kèm, bạn vào Tools > Options > Projects and Solutions > External Web Tools và đưa `C:\Program Files\nodejs` lên đầu danh sách (cùng nguồn). Về phía frontend, template dùng esbuild thông qua `tsbuild`, nhắm ES2022 và ES modules ([10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)).

**Thực hành từng lệnh.**

```bash
dotnet new install Serene.Templates          # hoặc ghim phiên bản: dotnet new install Serene.Templates@10.6.0
dotnet new serene -n HelloSerenity           # tên template viết thường; -D false để bỏ module demo
cd HelloSerenity/HelloSerenity.Web
npm i
dotnet build
dotnet run                                   # mở http://localhost:5000 (cổng in ra trong console)
```

Đăng nhập bằng **admin / serenity**. Ở lần chạy đầu, Serene tự tạo database trong SQL LocalDB và chạy migration lúc khởi động ([Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new); [Starting Serene](https://serenity.is/docs/getting_started/starting_serene)). `dotnet new` không tạo file solution. Nếu muốn mở bằng Visual Studio, bạn lưu file solution ở thư mục gốc, không đặt trong thư mục `.Web` (cùng nguồn). Từ 10.1, solution dùng định dạng `.slnx` thay cho `.sln` ([10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)).

**Giải phẫu project.** `Initialization/Startup.cs` đăng ký các dịch vụ của Serenity: `AddServiceHandlers()`, `AddDynamicScripts()`, `AddUploadStorage()`, `AddReporting()`, cùng các service quyền và người dùng nằm trong `AppServices`. Đến cuối pipeline, nó gọi `IDataMigrations.Initialize()` để chạy migration ([Application Startup](https://serenity.is/docs/framework/initialization)). `Migrations/DefaultDB` chứa các lớp FluentMigrator, còn `Modules/Common/AppServices/DataMigrations.cs` là dịch vụ chạy chúng ([Movie tutorial ch.1](https://serenity.is/docs/tutorials/movies/01-creating-movie-table)). `App_Data/texts` chứa các file JSON bản địa hóa ([Application Startup](https://serenity.is/docs/framework/initialization)). `Modules/Common/bundles/*.bundle(.css|.ts)` là nơi đặt global bundle từ 10.1 ([10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)). `Modules/ServerTypes/` chứa TypeScript do Sergen sinh ra từ C# ([Serene.Web.csproj](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Serene.Web.csproj)). Chuỗi kết nối nằm trong `appsettings.json`, mục `Data`:

```json
"Data": {
  "Default": {
    "ConnectionString": "Server=(localdb)\\MsSqlLocalDB;Database=Serene_Default_v1;Integrated Security=true;Encrypt=false;TrustServerCertificate=true",
    "ProviderName": "System.Data.SqlClient"
  },
  "Northwind": { "ConnectionString": "...Serene_Northwind_v1...", "ProviderName": "System.Data.SqlClient" }
}
```

([SER appsettings.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/appsettings.json))

Chuyện gì xảy ra khi bạn chạy `dotnet build`? File `.csproj` của Serene có các target MSBuild sau. `TransformMvcClientTypes` chạy `dotnet sergen mvct` để sinh helper C# từ TS. `TransformServerTypings` (AfterBuild) sinh TS từ C#. `TransformRunTSBuild` chạy `node ./tsbuild.js`, tức esbuild, để đóng gói các file `*Page.ts(x)` vào `wwwroot/esm` ([Serene.Web.csproj](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Serene.Web.csproj)). Vì vậy tutorial chỉ cần bảo "rebuild" là đủ.

**Bẫy thường gặp.** Trên Linux và macOS không có LocalDB. `Startup.RegisterDataProviders()` mặc định có đăng ký `Microsoft.Data.Sqlite` ([SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs)), nên về lý thuyết bạn có thể trỏ `ConnectionString` và `ProviderName` sang SQLite hoặc một SQL Server khác. Tuy nhiên các trang đã đọc không có hướng dẫn chính thức từng bước cho trường hợp này **[Khoảng trống]**. Bẫy thứ hai: nếu sửa TS mà trình duyệt không thấy thay đổi, hãy xem log ứng dụng. Ở môi trường development, watcher của tsbuild được khởi động qua `StartNodeScripts` trong `appsettings.Development.json` ([ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)). Bẫy thứ ba là trang Troubleshooting cũ nói rằng tên solution có dấu chấm sẽ gây lỗi "Template.LoginPanel is not found". Đây là thông tin cũ, chưa được xác minh trên 10.x, nhưng tốt nhất bạn cứ tránh đặt tên có dấu chấm ([Troubleshooting](https://serenity.is/docs/faq/troubleshooting)).

**Bài tập.** Tạo project `HelloSerenity` hai lần, một lần mặc định và một lần với `-D false`, rồi so sánh menu của hai bản. Sau đó dùng SQL Server Management Studio hoặc Azure Data Studio mở database `..._Default_v1` và tìm bảng `VersionInfo`. **Tự kiểm tra:** Bạn có giải thích được tại sao không cần chạy `npm run build` bằng tay không? Migration được gọi ở dòng nào trong `Startup`?

> **Ghi chú về cấu trúc thư mục.** Không có trang chính thức nào liệt kê đầy đủ cây thư mục của một project Serene 10.x. Vai trò của `Imports/ClientTypes`, `Imports/MVC` và `wwwroot/esm` được ghép lại từ file `.csproj`, các trang Sergen và quy ước lâu năm **[Suy luận]**. Bạn nên tạo một project mới và tự đối chiếu.

---

### Bài 2. Đi một vòng tutorial Movie: từ bảng trống đến CRUD hoàn chỉnh

**Mục tiêu.** Nắm được vòng lặp làm việc cốt lõi của Serenity: **viết migration → chạy Sergen → tùy biến bằng attribute → build**.

**Khái niệm.** Tutorial Movie chính thức có 13 chương (0–12), xây một ứng dụng nhỏ kiểu IMDB tên `MovieTutorial` ([Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction)). Ngày ghi trong tên migration (2022-11) cho thấy tutorial được viết từ thời 6.x. Tuy vậy, nó đã được cập nhật sang `.tsx`, `@serenity-is/extensions` và `getLookupAsync`, nên vẫn dùng tốt làm lộ trình học hiện nay **[Suy luận]**. Bảng dưới cho biết mỗi chương dạy khái niệm gì:

| Chương | Nội dung | Khái niệm |
|---|---|---|
| 1 | Tạo bảng Movie | Migration |
| 2 | Sinh mã bằng Sergen | Scaffolding |
| 3–5 | Tùy biến UI, menu, quick search | Giao diện điều khiển bằng attribute |
| 6 | Trường Kind (enum) | Enum |
| 7–9 | Thể loại, nhiều thể loại, lọc | Lookup, quan hệ nhiều-nhiều |
| 10–11 | Diễn viên (cast) | Master-detail |
| 12 | Ảnh chính và thư viện ảnh | Upload |

**Bước 1: migration.** Tạo file `Migrations/DefaultDB/DefaultDB_20221114_1505_MovieTable.cs`, đặt tên theo mẫu `{DB}_yyyyMMdd_HHmm_{Mô tả}`. Lớp này mang `[DefaultDB, MigrationKey(20221114_1505)]` và kế thừa `AutoReversingMigration` ([Movie tutorial ch.1](https://serenity.is/docs/tutorials/movies/01-creating-movie-table)). Đoạn dưới được thầy *tái dựng* từ mô tả cột trong ghi chú, không phải bản sao nguyên văn:

```csharp
[DefaultDB, MigrationKey(20221114_1505)]
public class DefaultDB_20221114_1505_MovieTable : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("Movie")
            .WithColumn("MovieId").AsInt32().IdentityKey(this)
            .WithColumn("Title").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()
            .WithColumn("Storyline").AsString(int.MaxValue).Nullable()
            .WithColumn("Year").AsInt32().Nullable()
            .WithColumn("ReleaseDate").AsDateTime().Nullable()
            .WithColumn("Runtime").AsInt32().Nullable();
    }
}
```

Nhấn F5. Bảng `[dbo].[Movie]` sẽ xuất hiện, và `VersionInfo` có thêm một dòng `20221114150500`. Quy tắc vàng ở đây: **không bao giờ sửa một migration đã chạy, hãy thêm migration mới** (cùng nguồn).

**Bước 2: Sergen.** Trong thư mục `.Web`, chạy `dotnet sergen g` rồi lần lượt chọn: connection `Default`, bảng `dbo.Movie` (bấm Space để chọn, Enter để xác nhận), module `MovieDB`, identifier `Movie`, quyền mặc định `Administration:General`, và sinh "All" ([Movie tutorial ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)). Rebuild và chạy lại. Menu MovieDB > Movie xuất hiện tại `/MovieDB/Movie` với đầy đủ CRUD mà bạn chưa viết dòng code nào. Thư mục `Modules/MovieDB/Movie/` giờ chứa `MovieRow.cs`, `MovieColumns.cs`, `MovieForm.cs`, `MovieEndpoint.cs`, các handler, `MoviePage.cs`/`MoviePage.tsx`, `MovieGrid.tsx` và `MovieDialog.tsx` ([Movie tutorial ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui)).

**Bước 3: tùy biến bằng attribute.** Chương 3 cho thấy mỗi loại thay đổi thuộc về một file khác nhau. Đổi `[DisplayName("Runtime (mins)")]` ở Row thì nhãn thay đổi ở mọi nơi. Thêm `[DisplayName("Runtime in Minutes"), Width(150), AlignRight]` trong `MovieColumns.cs` thì chỉ grid thay đổi. Thêm `[TextAreaEditor(Rows = 3)]` trong `MovieForm.cs` thì chỉ dialog thay đổi. CSS riêng cho dialog được viết vào `MovieDialog.css` với selector `.s-MovieDB-MovieDialog .caption`, rồi import từ `MovieDialog.tsx` ([Movie tutorial ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui)).

**Bước 4: enum.** Chương 6 thêm migration `MovieKind` (cột Int32 `Kind`, not null, default 1). Sau đó tạo enum có `[EnumKey("MovieDB.MovieKind")]` gồm Film=1, TvSeries=2, MiniSeries=3, mỗi giá trị kèm `[Description]`. Trên Row thêm property `MovieKind? Kind` với `[DisplayName("Kind"), NotNull, DefaultValue(MovieKind.Film)]` và field `EnumField<MovieKind> Kind`. Cuối cùng thêm `Kind` vào Form và Columns, dialog sẽ tự hiện dropdown ([Movie tutorial ch.6](https://serenity.is/docs/tutorials/movies/06-adding-movie-kind-field)).

```csharp
[EnumKey("MovieDB.MovieKind")]
public enum MovieKind
{
    [Description("Film")] Film = 1,
    [Description("TV Series")] TvSeries = 2,
    [Description("Mini Series")] MiniSeries = 3
}
```

(Đoạn này cũng được tái dựng từ mô tả.)

**Bẫy thường gặp.** Nếu `dotnet sergen g` báo lỗi, hãy chạy `dotnet restore` và `dotnet tool restore` trong thư mục `.Web` ([Movie tutorial ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)). Quyền mặc định `Administration:General` **chỉ admin có**, nên user thường sẽ không thấy trang vừa sinh (cùng nguồn). Tutorial dùng `[DisplayName]`, trong khi mã Northwind 10.6 dùng `[DisplayText]`. Bạn sẽ gặp cả hai, cứ hiểu chúng cùng vai trò là nhãn hiển thị ([NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs)). Ghi chú nghiên cứu chưa đọc chi tiết các chương 4, 5, 7, 9, 11 và 12 **[Khoảng trống]**, nên bạn hãy đọc trực tiếp chúng trên docs.

**Bài tập.** Làm hết chương 0 đến 6. Sau đó tự thêm trường `Rating` (decimal, 0–10) theo đúng vòng lặp: migration mới, sửa Row, Columns, Form, rồi build. **Không chạy lại Sergen** cho bảng Movie. **Tự kiểm tra:** Muốn đổi nhãn chỉ trong grid thì sửa file nào? Muốn đổi ở mọi nơi thì sửa file nào?

---

### Bài 3. Row: trái tim của mọi tính năng

**Mục tiêu.** Đọc hiểu và tự viết được một Row: field, property, khóa, join, view field, expression, cùng các attribute quyền mà 10.6 bắt buộc.

**Khái niệm cốt lõi.** Một Row có dạng `sealed class XRow : Row<XRow.RowFields>`. Bên trong nó có một lớp lồng `RowFields : RowFieldsBase` chứa các field có kiểu cụ thể (`StringField`, `Int32Field`, `DecimalField`, `EnumField<T>`, `ListField<T>`, `RowListField<T>`…). **Row không được dùng auto-property.** Mỗi property phải viết dạng `get => fields.Name[this]; set => fields.Name[this] = value;`. Cách viết này cho phép dựng query mà không cần reflection, đồng thời theo dõi được field nào đã được gán. Mọi property đều nullable, kể cả cột NOT NULL, theo nguyên tắc "We never use zero in place of null" vì left join có thể trả về null. Kiểu và tên của field phải khớp với property, nếu không quá trình khởi tạo sẽ lỗi ([Entities docs](https://serenity.is/docs/framework/data-access/entities)). Template mới dùng `<Nullable>annotations</Nullable>` từ 10.5.2, nên field được khai báo kèm `= null!` ([CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)).

**Code thật (Northwind, 10.6.0, đã rút gọn):**

```csharp
[NorthwindConnection, NorthwindModule, TableName("Products")]
[DisplayText("Products"), InstanceName("Product")]
[ReadPermission(PermissionKeys.General)]
[ModifyPermission(PermissionKeys.General)]
[LookupScript]
public sealed class ProductRow : Row<ProductRow.RowFields>, IIdRow, INameRow, IConcurrencyVersionRow
{
    [DisplayText("Product Id"), Identity, LookupInclude, IdProperty]
    public int? ProductID { get => fields.ProductID[this]; set => fields.ProductID[this] = value; }

    [DisplayText("Product Name"), Size(40), NotNull, QuickSearch, LookupInclude, NameProperty]
    public string? ProductName { get => fields.ProductName[this]; set => fields.ProductName[this] = value; }

    [DisplayText("Supplier"), NotNull, ForeignKey(typeof(SupplierRow)), LeftJoin("sup")]
    [LookupEditor(typeof(SupplierRow), InplaceAdd = true, DialogType = "Demo.Northwind.SupplierDialog", Async = true)]
    public int? SupplierID { get => fields.SupplierID[this]; set => fields.SupplierID[this] = value; }

    [Origin("sup"), DisplayText("Supplier"), LookupInclude]
    public string? SupplierCompanyName { get => fields.SupplierCompanyName[this]; set => fields.SupplierCompanyName[this] = value; }

    [DisplayText("Row Version"), Insertable(false), Updatable(false)]
    public int? RowVersion { get => fields.RowVersion[this]; set => fields.RowVersion[this] = value; }
    Field IConcurrencyVersionRow.ConcurrencyVersionField => fields.RowVersion;

    public class RowFields : RowFieldsBase
    {
        public Int32Field ProductID = null!;
        public StringField ProductName = null!;
        public Int32Field SupplierID = null!;
        public StringField SupplierCompanyName = null!;
        public Int32Field RowVersion = null!;
    }
}
```

([NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs))

Đọc theo từng nhóm sẽ dễ hơn. **Ánh xạ bảng:** `[TableName]`, cùng attribute connection là một lớp con của `ConnectionKeyAttribute` (`NorthwindConnectionAttribute` có `const string Key = "Northwind"`) ([NorthwindConnectionAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Shared/NorthwindConnectionAttribute.cs)). Kiểu cũ hơn là `[ConnectionKey("Default"), Module("Administration")]`, và bạn vẫn sẽ gặp nó ([Service Endpoints](https://serenity.is/docs/services/service_endpoints)). **Khóa và tên:** `Identity`, `IdProperty`, `NameProperty`, `InstanceName`. **Cột:** `Size`, `Scale`, `NotNull`. **Tìm kiếm:** `QuickSearch`. **Lookup:** `LookupInclude` ([Mapping docs](https://serenity.is/docs/framework/data-access/mapping)).

**Join và view field.** Chỉ `[ForeignKey]` thôi thì *chỉ là metadata*, nó không làm thay đổi query. Phải thêm `[LeftJoin("alias")]` thì join mới thực sự được tạo. Field lấy từ bảng được join dùng `[Origin("alias", "Prop")]` hoặc `[Expression("alias.[Col]")]`. Alias phải là duy nhất và không được trùng từ khóa SQL ([Mapping docs](https://serenity.is/docs/framework/data-access/mapping)). Northwind 10.6 dùng một cách viết an toàn khi refactor:

```csharp
const string jCustomer = nameof(jCustomer);

[DisplayText("Customer"), Size(5), NotNull, NameProperty]
[ForeignKey(typeof(CustomerRow), nameof(CustomerRow.CustomerID)), LeftJoin(jCustomer)]
public string? CustomerID { get => fields.CustomerID[this]; set => fields.CustomerID[this] = value; }

[Origin(jCustomer, nameof(CustomerRow.CompanyName)), DisplayText("Customer"), QuickSearch]
public string? CustomerCompanyName { get => fields.CustomerCompanyName[this]; set => fields.CustomerCompanyName[this] = value; }

[DisplayText("Shipping State"), Case($"T0.[{nameof(ShippedDate)}] IS NULL",
    (int)OrderShippingState.NotShipped, (int)OrderShippingState.Shipped)]
public OrderShippingState? ShippingState { get => fields.ShippingState[this]; set => fields.ShippingState[this] = value; }
```

([NW OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs))

**Expression và dialect.** `T0` là alias của bảng chính. Toán tử `+` để nối chuỗi chỉ chạy trên SQL Server. Muốn chạy được trên nhiều loại database, hãy dùng `CONCAT`, `[Concat]`, hoặc viết biến thể theo dialect: `[Expression("(T0.FirstName || ' ' || T0.LastName)", Dialect = nameof(ServerType.Sqlite))]`. Join có thể khai báo ở cấp lớp, ví dụ `[LeftJoin("cd", "CustomerDetails", "cd.[CustomerID] = T0.[CustomerID]")]`. `[NotMapped]` dùng cho property không có cột trong database, chẳng hạn danh sách detail. `[ClientSide]` là alias cũ đã lỗi thời ([Mapping docs](https://serenity.is/docs/framework/data-access/mapping)).

**Interface đánh dấu.** `IIdRow`, `INameRow`, `IInsertLogRow`, `IUpdateLogRow`, `IDeleteLogRow`, `IIsActiveRow`, `IIsActiveDeletedRow`. Behavior sẽ dựa vào các interface này để tự động làm việc của nó ([FW Entity/Contracts](https://github.com/serenity-is/Serenity/tree/master/src/services/Entity/Contracts)).

**Bẫy thường gặp (rất quan trọng trên 10.6).** **Row không có `[ReadPermission]`/`[ModifyPermission]` sẽ bị từ chối truy cập**, và permission key không được để trống. Nếu muốn ai cũng xem được, dùng `SpecialPermissionKeys.Public` (`"*"`). Nếu chỉ cần đã đăng nhập, dùng `LoggedIn` (`"?"`) ([CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)). Từ 10.6, **field có tên trông nhạy cảm** (secret, token, api key, 2FA) và các field hash/salt của `IPasswordRow` tự động bị loại khỏi select, lọc, sắp xếp và ghi từ client. Một field tên `ApiSecret` vì thế sẽ "biến mất" khỏi kết quả nếu bạn không cấu hình lại ([10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)). Các API đã lỗi thời: lớp lồng `RowTemplate` không còn được khuyến nghị từ 6.8.1 ([Entities docs](https://serenity.is/docs/framework/data-access/entities)), và nên dùng extension `GetIdField()` thay cho property `IdField` ([CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)). `[GenerateFields]` với partial property là tính năng của `Serenity.Pro.Coder`, chỉ có trong StartSharp ([Entities docs](https://serenity.is/docs/framework/data-access/entities)).

**Bài tập.** Mở `OrderRow.cs` trong monorepo. Tìm một field kiểu view (`Origin`), một field tính toán (`Case`) và một danh sách detail (`MasterDetailRelation`). Sau đó viết `PersonRow` cho tutorial Movie với `FullName` ghép từ FirstName và LastName bằng `[Concat]`, đánh dấu `QuickSearch` và `NameProperty`. **Tự kiểm tra:** Vì sao `int? ProductID` lại nullable dù cột là identity? Nếu xóa `[LeftJoin]` nhưng giữ `[Origin]` thì sẽ xảy ra gì?

---

## Chặng 2 — Dữ liệu và dịch vụ

### Bài 4. Fluent SQL và Criteria: viết truy vấn mà không ghép chuỗi

**Mục tiêu.** Dùng được `SqlQuery`, `SqlInsert/Update/Delete`, `Criteria` và các helper của connection. Hiểu rõ chúng **không** chạy behavior hay kiểm tra quyền.

**Khái niệm.** `SqlQuery` là bộ dựng câu SELECT kiểu fluent. Các phương thức có thể gọi theo bất kỳ thứ tự nào: `From`, `Select`, `OrderBy`, `GroupBy`, `Having`, `Skip`, `Take`, `Distinct`. Phân trang được sinh theo dialect: SQL Server 2012+ dùng `OFFSET ... FETCH NEXT`, SQLite dùng `LIMIT ... OFFSET` ([Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql)). **Criteria** thường được tạo từ field của Row thông qua toán tử nạp chồng: `.Where(fld.Age > 18 & fld.Country == "US")` sinh ra `WHERE Age > @p1 AND Country = @p2`, có tham số đầy đủ ([Query Extensions docs](https://serenity.is/docs/framework/data-access/query-extensions)). Kiểm tra null phải dùng `IsNull()`/`IsNotNull()`, không dùng `== null`. Ngoài ra còn `Like`, `StartsWith`, `Contains`, `In/NotIn` (giá trị hoặc subquery) và `Criteria.Exists`. **Không bao giờ đưa chuỗi do client gửi lên vào constructor `new Criteria(...)`** ([Criteria docs](https://serenity.is/docs/framework/data-access/criteria)).

**Code thật:**

```csharp
// Helper trên connection
var shippers = connection.List<ShipperRow>(q => q.SelectTableFields().OrderBy(ShipperRow.Fields.CompanyName));

// Thống kê theo tháng, chạy bằng Dapper
var monthExpr = new DatePartAttribute(DateParts.Month, fld.OrderDate.Expression)
    .ToString(connection.GetDialect());

var byMonth = connection.Query(
    new SqlQuery()
        .From(fld)
        .Select(monthExpr, "Month")
        .Select(Sql.Count(), "Count")
        .Select(fld.ShipVia, "ShipVia")
        .GroupBy(monthExpr)
        .GroupBy(fld.ShipVia)
        .Where(fld.OrderDate.IsNotNull() & fld.ShipVia.IsNotNull()));
```

([BasicSamplesEndpoint.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.basicsamples/Modules/BasicSamplesEndpoint.cs))

```csharp
// Subquery EXISTS bên trong list handler
query.Where(Criteria.Exists(
    query.SubQueryFrom(OrderDetailRow.Fields, out var detailFields)
        .Select("1")
        .Where(detailFields.OrderID == MyRow.Fields.OrderID &
               detailFields.ProductID == Request.ProductID.Value))
    .ToString()));
```

([NW OrderListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/RequestHandlers/OrderListHandler.cs))

**Ghi dữ liệu.** `new SqlInsert("T").Set(...).Execute(conn)`. `SqlUpdate` có `Set/SetNull/Inc/Dec`, và `.Execute(conn, ExpectedRows.One)` sẽ ném lỗi nếu số dòng bị ảnh hưởng không đúng như mong đợi, nên câu lệnh thiếu WHERE sẽ thất bại chứ không âm thầm cập nhật cả bảng ([SQL Data Manipulation docs](https://serenity.is/docs/framework/data-access/sql-data-manipulation)). Các helper `ById`, `TryById`, `List<TRow>`, `Count`, `Insert`, `UpdateById`, `DeleteById` chỉ ghi những field đã được gán. **Chúng không chạy behavior và không validate theo service** ([Entity CRUD docs](https://serenity.is/docs/framework/data-access/entity-crud)).

**Bẫy và thay đổi của 10.6.** Từ 10.6, `StartsWith`, `EndsWith` và `Contains` tự escape `%` và `_`. `Like`/`NotLike` vẫn là pattern thô, và có thêm `LikeEscaped`. `QueryWithParams.Params` giờ chỉ đọc. `SqlUpdate.Where(params string[])` và API `IChainable.With` đã bị xóa. `IFilterableQuery.Where` nhận `ICriteria?`. SQL sinh ra được cache, và có thêm `Freeze`/`FreezeParams`, `ListProjected`/`QueryProjected` ([CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)). Code mẫu cũ nào dùng `.With(...)` sẽ không còn biên dịch được. Trang docs chỉ liệt kê một phần các dialect (Firebird, Postgres, Sqlite, SqlServer), trong khi mã nguồn còn có MySql, Oracle, Oracle12c và SqlServer2008 ([Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql)). Chữ ký của `ParamCriteria`/`Alias` trong ghi chú là diễn giải lại, bạn nên đối chiếu trang gốc trước khi chép **[Khoảng trống]**.

**Bài tập.** Trong một endpoint thử nghiệm, viết truy vấn đếm số phim theo `Kind`, dùng `GroupBy` và `Sql.Count()`. In `query.ToString()` ra log để xem SQL sinh ra. **Tự kiểm tra:** Vì sao `connection.Insert(row)` không tự điền `InsertUserId` còn Save service thì có?

---

### Bài 5. Service, handler, endpoint, behavior và validation

**Mục tiêu.** Biết logic nghiệp vụ nên đặt ở đâu. Override đúng hook. Viết được behavior dùng chung và ném lỗi validation đúng chuẩn.

**Khái niệm: handler async là mặc định.** CRUD do các handler generic đảm nhận: `ListRequestHandlerAsync<TRow>`, `RetrieveRequestHandlerAsync<TRow>`, `SaveRequestHandlerAsync<TRow>`, `DeleteRequestHandlerAsync<TRow>`, cùng `UndeleteRequestHandlerAsync`. Mỗi handler được bọc trong một interface riêng của project để đăng ký DI. Code mà Sergen sinh trên 10.6 chỉ có vài dòng:

```csharp
public interface IProductListHandler : IListHandlerAsync<MyRow> { }

public class ProductListHandler(IRequestContext context) :
    ListRequestHandlerAsync<MyRow>(context), IProductListHandler
{
}
```

([NW ProductListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/RequestHandlers/ProductListHandler.cs))

Về handler đồng bộ (`SaveRequestHandler<TRow>`…), có một mâu thuẫn. Docs nói chúng được giữ lại để tương thích và "marked obsolete" ([Save handler docs](https://serenity.is/docs/services/save_request_handler)). Nhưng mã nguồn 10.6 không gắn `[Obsolete]` cho chúng ([FW SaveRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandler.cs)). Lời khuyên của thầy: **xem chúng là đồ cũ, code mới luôn dùng async**.

**Endpoint.** Endpoint là một controller kế thừa `ServiceEndpoint`. Action ghi nhận `IUnitOfWork` và mang `[HttpPost]`. Action đọc nhận `IDbConnection`. Handler được inject theo từng action bằng `[FromServices]`:

```csharp
[Route("Services/Northwind/Product/[action]")]
[ConnectionKey(typeof(MyRow)), ServiceAuthorize(typeof(MyRow))]
public class ProductEndpoint : ServiceEndpoint
{
    [HttpPost, AuthorizeCreate(typeof(MyRow))]
    public Task<SaveResponse> Create(IUnitOfWork uow, SaveRequest<MyRow> request,
        [FromServices] IProductSaveHandler handler, CancellationToken cancellationToken = default)
        => handler.CreateAsync(uow, request, cancellationToken);

    public Task<ListResponse<MyRow>> List(IDbConnection connection, ListRequest request,
        [FromServices] IProductListHandler handler, CancellationToken cancellationToken = default)
        => handler.ListAsync(connection, request, cancellationToken);
    // Update, Delete, Retrieve tương tự
}
```

([NW ProductEndpoint.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductEndpoint.cs)). `AuthorizeCreate/Update/Delete` đọc permission từ attribute của Row ([Service Endpoints](https://serenity.is/docs/services/service_endpoints)). `services.AddServiceHandlers()` trong Startup tự đăng ký mọi handler và behavior ([SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs)). Nếu đọc bài viết cũ có lớp `XRepository` gọi `new MyListHandler().Process(...)`, bạn hãy hiểu nó tương đương với "handler + `[FromServices]`" ngày nay **[Suy luận]**.

**Thứ tự các hook của Save:** `ValidateRequestAsync` → `SetInternalFieldsAsync` → `BeforeSaveAsync` → (INSERT/UPDATE) → `AfterSaveAsync` → `OnReturnAsync`. Ngoài ra còn `GetEditableFields(HashSet<Field>)`. Trong handler bạn truy cập được `Row`, `Old`, `Request`, `UnitOfWork`, `Connection`, `StateBag`, `IsCreate`/`IsUpdate` ([Save handler docs](https://serenity.is/docs/services/save_request_handler); [FW SaveRequestHandlerBase.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandlerBase.cs)). List có các hook `PrepareQueryAsync`, `ApplyFiltersAsync`, `OnBeforeExecuteQueryAsync`, `OnAfterExecuteQueryAsync` ([List handler docs](https://serenity.is/docs/services/list_request_handler)). Một handler thật trong Serene:

```csharp
public class UserSaveHandler(IRequestContext context, IOptions<EnvironmentSettings> environmentOptions)
    : SaveRequestHandlerAsync<MyRow>(context), IUserSaveHandler
{
    protected override void GetEditableFields(HashSet<Field> editable)
    {
        base.GetEditableFields(editable);
        if (!Permissions.HasPermission(PermissionKeys.Security))
        {
            editable.Remove(MyRow.Fields.Source);
            editable.Remove(MyRow.Fields.IsActive);
        }
    }

    protected override async Task SetInternalFieldsAsync(CancellationToken cancellationToken = default)
    {
        await base.SetInternalFieldsAsync(cancellationToken).ConfigureAwait(false);
        if (IsCreate) { Row.Source = "site"; Row.IsActive = Row.IsActive ?? 1; }
    }

    protected override async Task AfterSaveAsync(CancellationToken cancellationToken = default)
    {
        await base.AfterSaveAsync(cancellationToken).ConfigureAwait(false);
        Cache.InvalidateOnCommit(UnitOfWork, MyRow.Fields);
    }
}
```

([SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs)). Nguyên tắc chung: **luôn gọi `base` trước** rồi mới thêm logic của bạn.

**Request tùy biến.** Khi cần thêm tham số lọc, dùng dạng ba tham số generic `ListRequestHandlerAsync<MyRow, OrderListRequest, ListResponse<MyRow>>` với `public class OrderListRequest : ListRequest { public int? ProductID { get; set; } }` ([NW OrderListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/RequestHandlers/OrderListHandler.cs)). Phía `ListRequest` có `Skip`, `Take`, `Sort`, `ContainsText`, `Criteria`, `EqualityFilter`, `ColumnSelection`, `IncludeColumns`… Từ 10.6 có thêm `IncludeMore` để bỏ qua `COUNT(*)`, và giới hạn trang phía server qua `RequestHandlerSettings.DefaultPageSize/MaxPageSize` ([List handler docs](https://serenity.is/docs/services/list_request_handler); [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)).

**Behavior: logic cắt ngang viết một lần, áp cho nhiều bảng.** Behavior là singleton. Behavior ngầm định implement `IImplicitBehavior.ActivateFor(IRow row)`, còn muốn gắn tường minh thì dùng `[AddBehavior]`. Vì là singleton, **không được lưu state theo request trong field của behavior**, hãy dùng `handler.StateBag`. Tên hook của behavior có tiền tố `On`: `OnValidateRequestAsync`, `OnSetInternalFieldsAsync`, `OnPrepareQueryAsync`, `OnBeforeSaveAsync`. Các lớp nền async gồm `BaseSaveBehaviorAsync`, `BaseListBehaviorAsync`… Các lớp nền đồng bộ được ghi là obsolete ([Behaviors docs](https://serenity.is/docs/services/behaviors)). Behavior có sẵn: log insert/update (`LoggingRow`), `[CaptureLog]`, `[LocalizationRow]`, `[MasterDetailRelation]`, `[LinkingSetRelation]`, `[Unique]`/`[UniqueConstraint]`, `[UpdatableExtension]`, `IDisplayOrderRow` ([Built-in Behaviors docs](https://serenity.is/docs/services/built-in-behaviors)).

**Validation.** Thứ tự kiểm tra khi Save: `ValidatePermissions` → `ValidateEditable` → `ValidateRequired` → `ValidateIsActive` → `ValidateFieldValues` (`ICustomValidator`) → `OnValidateRequest` của các behavior. Một field được coi là bắt buộc nếu có `[NotNull]` và không có `[TrimToEmpty]` ([Validation docs](https://serenity.is/docs/services/validation)). Để báo lỗi gắn với một field cụ thể, ném `ValidationError(errorCode, fieldName, message)`. Endpoint sẽ chuyển nó thành `ServiceError` với HTTP 400, và client tô đỏ đúng field đó:

```csharp
if (!UserHelper.IsValidUsername(username))
    throw new ValidationError("InvalidUsername", "Username",
        "Usernames should start with letters, only contain letters and numbers!");
```

([SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs)). Từ 10.6, lỗi vi phạm ràng buộc SQL (unique, FK…) được chuyển thành thông báo validation đã bản địa hóa trên SQL Server, PostgreSQL, MySQL, Oracle, Firebird và SQLite ([CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)).

**Bẫy.** Đừng đặt logic trong endpoint. Endpoint mỏng giúp bạn tái dùng handler từ job nền bằng cách inject `IXxxHandler` và tự cung cấp `UnitOfWork` **[Suy luận]**. Danh sách hook của Retrieve và Delete chưa được ghi chú đọc kỹ **[Khoảng trống]**, xem tại [Retrieve handler](https://serenity.is/docs/services/retrieve_request_handler) và [Delete handler](https://serenity.is/docs/services/delete_request_handler).

**Bài tập.** Trong `MovieSaveHandler`, override `ValidateRequestAsync` để ném lỗi nếu `ReleaseDate` lớn hơn hôm nay. Sau đó viết một behavior `TrimTitleBehavior` áp cho mọi Row có field `Title`. **Tự kiểm tra:** Vì sao lưu một `List<>` trong field của behavior là bug ngầm?

---

### Bài 6. Migration và kết nối database

**Mục tiêu.** Viết migration chạy được trên nhiều database, hiểu cơ chế tự chạy lúc khởi động, và biết cách đổi sang PostgreSQL hoặc database khác.

**Khái niệm.** Serene chạy FluentMigrator ngay lúc khởi động qua `IDataMigrations.Initialize()`. Với mỗi connection key, nó gọi `EnsureDatabase` (tự tạo database nếu chưa có) rồi `RunMigrations`. Processor được chọn theo `Dialect.ServerType` (SQL Server, Postgres, MySQL, Oracle, SQLite, Firebird). Tag của migration là tên key cộng "DB" (ví dụ `NorthwindDB`), và migration không có tag chỉ chạy cho key Default ([SER DataMigrations.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/AppServices/DataMigrations.cs)). Attribute tag database được viết như sau: `public class NorthwindDBAttribute : TargetDBAttribute { public NorthwindDBAttribute() : base(NorthwindConnectionAttribute.Key) { } }` ([NorthwindDBAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDBAttribute.cs)).

**Code thật:** một migration mới trong 10.6, có cả `Up` và `Down`:

```csharp
[NorthwindDB, MigrationKey(20261008_1200)]
public class NorthwindDB_20261008_1200_ProductConcurrencyVersion : Migration
{
    public override void Up()
    {
        Alter.Table("Products")
            .AddColumn("RowVersion").AsInt32().NotNullable().WithDefaultValue(0)
            .AddConcurrencyVersionTrigger(this, idField: "ProductID");
    }

    public override void Down()
    {
        this.DropConcurrencyVersionTrigger("Products", "RowVersion");
        Delete.Column("RowVersion").FromTable("Products");
    }
}
```

([NW ...ProductConcurrencyVersion.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDB_20261008_1200_ProductConcurrencyVersion.cs)). Khi tạo khóa chính tự tăng, dùng helper `IdentityKey(this)` của Serenity thay cho `.Identity().PrimaryKey()` để migration chạy được trên nhiều dialect ([NW Initial migration](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDB_20141123_1551_Initial.cs)). Ví dụ Oracle cần sequence, và theo thầy đó chính là lý do helper này tồn tại **[Suy luận]**. Từ 10.6 có thêm các helper `AsUserIdType`/`UserIdForeignKey` ([CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)).

**Kết nối.** Mỗi mục trong `Data` có `ConnectionString`, `ProviderName` và có thể có thêm `Dialect`. Nếu không ghi `Dialect`, framework tự suy ra từ provider ([SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections)). Provider được đăng ký trong `Startup.RegisterDataProviders()`. Mặc định chỉ có SqlClient và Sqlite, còn Npgsql, MySql, Firebird đang được comment sẵn ([SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs)). Để chuyển sang PostgreSQL, bạn thêm gói Npgsql, bỏ comment dòng đăng ký, rồi đặt `"ProviderName": "Npgsql"` **[Suy luận từ code; chưa có trang hướng dẫn chính thức]**. Repo có integration test migration cho SqlServer, Postgres, MySql, Oracle, Firebird và Sqlite, đây là bằng chứng hỗ trợ đa database ([Serene tests/sqlintegration](https://github.com/serenity-is/Serenity/tree/master/serene/tests/Serene.Tests/sqlintegration)).

**Bẫy.** Docs **không có trang nào dành riêng cho migration database**. Các mục "Migration" trong docs là hướng dẫn nâng cấp phiên bản. Toàn bộ kiến thức ở bài này được rút ra từ mã nguồn **[Khoảng trống]**. Có hai lỗi kinh điển: sửa migration đã chạy, và đặt `MigrationKey` không khớp với tên file. Nếu bạn cấu hình `UserEntityOptions` để đổi kiểu user ID sang long, Guid hay string, việc đó phải làm **trước** khi chạy migration ban đầu ([10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)).

**Bài tập.** Viết migration thêm bảng `Genre` (`GenreId` dùng `IdentityKey`, `Name` kiểu string(100) unique) kèm `Down()`. Tạo project thứ hai, đổi connection Default sang SQLite và kiểm tra migration chạy được **[thử nghiệm — có thể cần chỉnh thêm]**. **Tự kiểm tra:** Tại sao migration không có tag lại không chạy cho Northwind?

---

## Chặng 3 — Giao diện

### Bài 7. Form và Columns: mô tả giao diện bằng C#

**Mục tiêu.** Điều khiển layout của dialog và cột của grid bằng attribute. Hiểu cơ chế kế thừa metadata qua `BasedOnRow`.

**Khái niệm.** Lớp `[FormScript]` và lớp `[ColumnsScript]` là các lớp C# thường. Mỗi public property của chúng là một field trên form hoặc một cột trên grid. Khi gắn `[BasedOnRow(typeof(XRow), CheckNames = true)]`, chúng kế thừa nhãn, editor, formatter và cờ bắt buộc từ property cùng tên của Row. Server chuyển chúng thành `PropertyItem[]` dạng JSON, rồi `PropertyGrid` (dialog) và `DataGrid` (grid) dựng giao diện từ đó ([Forms & Editors](https://serenity.is/docs/framework/forms-editors); [Grid Columns](https://serenity.is/docs/framework/grid-columns)). `CheckNames = true` bắt lỗi gõ sai tên property. `[SkipNameCheck]` dùng để miễn kiểm tra cho một property.

**Code thật: Form có tab và category:**

```csharp
[FormScript("Northwind.Order")]
[BasedOnRow(typeof(OrderRow), CheckNames = true)]
public class OrderForm
{
    [Tab("General")]
    [CategoryName("Order")]
    public string? CustomerID { get; set; }
    [DefaultValue("now")]
    public DateTime OrderDate { get; set; }
    public int? EmployeeID { get; set; }

    [CategoryName("Order Details")]
    [OrderDetailsEditor]                       // editor master-detail tùy biến
    public List<OrderDetailRow>? DetailList { get; set; }

    [Tab("Shipping")]
    [CategoryName("Info")]
    public DateTime ShippedDate { get; set; }
    public decimal Freight { get; set; }
}
```

([OrderForm.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderForm.cs)). Các attribute độ rộng gồm `[HalfWidth]` (`col-sm-6`), `[FullWidth]`, `OneThirdWidth`, `QuarterWidth`… Ngoài ra có `[Required]`, `[OneWay]`, `[HideOnInsert]`/`[HideOnUpdate]` ([Forms & Editors](https://serenity.is/docs/framework/forms-editors)).

**Code thật: Columns:**

```csharp
[ColumnsScript("Northwind.Order")]
[BasedOnRow(typeof(OrderRow), CheckNames = true)]
public class OrderColumns
{
    [EditLink, AlignRight, SortOrder(1, descending: true), Width(70), PinToStart]
    public string? OrderID { get; set; }

    [EditLink, Width(200), QuickFilter, PinToStart]
    public string? CustomerCompanyName { get; set; }

    [FilterOnly, QuickFilter]                  // chỉ dùng để lọc, không hiện thành cột
    public OrderShippingState ShippingState { get; set; }

    [Width(100), AsyncLookupEditor(typeof(Lookups.OrderShipCityLookup))]
    [QuickFilter(CssClass = "hidden-xs"), QuickFilterOption("CascadeFrom", "ShipCountry")]
    public string? ShipCity { get; set; }

    [FreightFormatter]                         // formatter TS tùy biến
    public decimal? Freight { get; set; }
}
```

([OrderColumns.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderColumns.cs)). Docs mô tả cách các attribute ánh xạ sang thuộc tính của `PropertyItem`: `Width`, `FixedWidth`, `AlignCenter/Right`, `EditLink`, `Sortable`, `PinToStart/End`, `FormatterType`, `DisplayFormat`, `SummaryType`, `FilteringType`, `QuickFilter`, `NotFilterable` ([Grid Columns](https://serenity.is/docs/framework/grid-columns)). Ý nghĩa của `[FilterOnly]`, `[Unbound]`, `[SortOrder]`, `[QuickFilterOption]`, `[HeaderCssClass]` và `[CategoryName]` được suy ra từ mã thật, vì các trang docs đã đọc không liệt kê chúng **[Suy luận]**.

**Lời khuyên thiết kế.** Hãy đặt attribute editor và formatter ở Row, hoặc ở một lớp `[AnnotationType(typeof(OrderRow))]` để giữ entity sạch các phụ thuộc giao diện ([OrderRowAnnotations.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRowAnnotations.cs)). Chỉ override ở Form hoặc Columns khi một màn hình cụ thể cần khác đi.

**Bẫy.** Giá trị mặc định kiểu "hôm nay" (`[DefaultValue("today")]`/`"now"`) đặt ở **Form**, không đặt ở Row ([FAQ](https://serenity.is/docs/faq/frequently_asked_questions)). Ở phía client, `getColumns`/`getColumnsData` đồng bộ đã lỗi thời, hãy dùng `getColumnsDataAsync` ([Grid Columns](https://serenity.is/docs/framework/grid-columns)).

**Bài tập.** Chia form Movie thành hai tab "Thông tin" và "Nội dung". Đặt `Year` và `Runtime` thành `[HalfWidth]`. Trong Columns, ghim `Title` sang trái, thêm `QuickFilter` cho `Kind`, và sắp xếp mặc định theo `ReleaseDate` giảm dần. **Tự kiểm tra:** Nếu bạn gõ sai tên property trong Form trong khi `CheckNames = true`, lỗi sẽ xuất hiện lúc nào?

---

### Bài 8. Grid, Dialog, trang và menu trong TypeScript hiện đại

**Mục tiêu.** Viết và tùy biến được `EntityGrid`/`EntityDialog` theo phong cách 10.x (ES modules, `[Symbol.typeInfo]`, JSX). Hiểu chuỗi sinh mã và cách đăng ký trang.

**Khái niệm: đăng ký kiểu bằng `[Symbol.typeInfo]`.** Từ 9.0, đăng ký bằng decorator (`@Decorators.registerClass`…) đã bị **deprecated**. Nó vẫn chạy nhưng không còn được khuyến nghị. Có hai lý do: thông tin decorator không được xuất vào file `.d.ts`, và esbuild có bug khi kết hợp decorator với property initializer. Cách mới là `static [Symbol.typeInfo] = this.registerClass("MyApp.MyModule.")`. Namespace kết thúc bằng dấu chấm sẽ được tự nối thêm tên lớp. Một kiểu không được dùng đồng thời cả hai cách ([changelog-2025.md (9.0.0)](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md); [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)).

**Bộ khung tối thiểu: 4 override cho grid, 3 override cho dialog** (theo template của Sergen):

```ts
export class XGrid extends EntityGrid<XRow> {
    static override [Symbol.typeInfo] = this.registerClass("MyApp.MyModule.");
    protected override getColumnsKey() { return XColumns.columnsKey; }
    protected override getDialogType() { return XDialog; }
    protected override getRowDefinition() { return XRow; }
    protected override getService() { return XService.baseUrl; }
}

export class XDialog extends EntityDialog<XRow, any> {
    static override [Symbol.typeInfo] = this.registerClass("MyApp.MyModule.");
    protected override getFormKey() { return XForm.formKey; }
    protected override getRowDefinition() { return XRow; }
    protected override getService() { return XService.baseUrl; }
    protected form = new XForm(this);
}
```

([GridModular.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/GridModular.scriban); [DialogModular.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/DialogModular.scriban))

**Tùy biến grid: học theo thứ tự người dùng nhìn thấy.** Đầu tiên là cột (`createColumns`), rồi toolbar (`getButtons`), bộ lọc (`getQuickFilters`), hành động trên dòng (`onClick`), và cuối cùng là giá trị mặc định khi thêm mới (`addButtonClick`). Grid Order trong Northwind làm đủ cả năm việc:

```tsx
export class OrderGrid<P = {}> extends EntityGrid<OrderRow, P> {
    static override[Symbol.typeInfo] = this.registerClass(nsDemoNorthwind, [Attributes.advancedFiltering]);
    protected override getColumnsKey() { return OrderColumns.columnsKey; }
    protected override getDialogType() { return OrderDialog as any; }
    protected override getRowDefinition() { return OrderRow; }
    protected override getService() { return OrderService.baseUrl; }

    protected override getQuickFilters() {
        const filters = super.getQuickFilters();
        filters.push({
            type: LookupEditor,
            options: { lookupKey: ProductRow.lookupKey, async: true },
            field: 'ProductID',
            title: 'Contains Product in Details',
            handler: w => { (this.view.params as OrderListRequest).ProductID = toId(w.value); },
            cssClass: 'hidden-xs'
        });
        return filters;
    }

    protected override getButtons(): ToolButton[] {
        const buttons = super.getButtons();
        buttons.push(ExcelExportHelper.createToolButton({ grid: this, service: OrderService.baseUrl + '/ListExcel', separator: true }));
        buttons.push(PdfExportHelper.createToolButton({ grid: this }));
        return buttons;
    }

    protected override createColumns() {
        const columns = new OrderColumns(super.createColumns());
        columns.PrintInvoice.format = () => <a class="inline-action" data-action="print-invoice" title="invoice">
            <i class={faIcon("file-pdf", "red")}></i></a>;
        return columns.valueOf();
    }

    protected override onClick(e: Event, row: number, cell: number) {
        super.onClick(e, row, cell);
        if (Fluent.isDefaultPrevented(e)) return;
        const item = this.itemAt(row);
        const action = (e.target as HTMLElement)?.closest(".inline-action")?.getAttribute("data-action");
        if (action == "print-invoice") {
            e.preventDefault();
            ReportHelper.execute({ reportKey: 'Northwind.OrderDetail', params: { OrderID: item.OrderID } });
        }
    }

    protected override addButtonClick() {
        const eq = this.view.params.EqualityFilter;
        this.editItem({ CustomerID: eq ? eq.CustomerID : null });
    }
}
```

([OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx)). Tùy chọn `[Attributes.advancedFiltering]` bật giao diện lọc nâng cao. Quick search được tùy biến qua `getQuickSearchFields()`. Để lọc một grid con theo grid cha, dùng `this.setEquality('CustomerID', value); this.refresh();` ([Filtering](https://serenity.is/docs/grids/filtering); [CustomerOrdersGrid.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerOrdersGrid.ts)).

**Vòng đời dialog.** Các hook chính: `getToolbarButtons`, `updateInterface` (ẩn hoặc hiện theo chế độ), `afterLoadEntity`, `validateBeforeSave`, `onSaveSuccess`, cùng `isEditMode()`/`isNewOrDeleted()` ([EntityDialog](https://serenity.is/docs/dialogs/entitydialog)). Với form đã có kiểu, `this.form.CustomerID` trả về đúng một `CustomerEditor`. Để bắt sự kiện đổi giá trị và thêm luật validate, xem `OrderDetailDialog`: `this.form.ProductID.changeSelect2(async e => {...})` và `this.form.Discount.addValidationRule(this.uniqueName, e => {...})` ([OrderDetailDialog.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/OrderDetail/OrderDetailDialog.ts)). Muốn tự dựng layout bằng JSX thì override `renderContents()` ([CustomerDialog.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerDialog.tsx)). Từ 8.x, mọi `EntityDialog` mặc định mở dạng **panel**, còn `GridEditorDialog`/`PropertyDialog` mở dạng modal ([changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)).

**Chuỗi sinh mã và trang.** Mỗi lần build: Row/Form/Columns viết bằng C# được Sergen `servertypings` chuyển thành `Modules/ServerTypes/*.ts`. Trong đó service proxy có `baseUrl = 'Northwind/Order'` và các hàm `List`/`Create`… đã có kiểu. Tiếp theo `tsbuild` chạy esbuild để tạo `wwwroot/esm` ([Sergen Commands](https://serenity.is/docs/sergen/sergen_commands); [Northwind.OrderService.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderService.ts)). Một trang gồm ba phần. Thứ nhất là controller `[PageAuthorize(typeof(XRow))]` với `[Route("Module/X")]` trả về `this.GridPage<XRow>(ESM.XPage)`. Thứ hai là module `XPage.ts` với `export default () => gridPageInit(XGrid);`. Thứ ba là một `[assembly: NavigationLink(...)]` để thêm vào menu ([CustomerPage.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerPage.cs); [CustomerPage.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerPage.ts); [Navigation](https://serenity.is/docs/framework/navigation)):

```csharp
[assembly: NavigationMenu(7000, "Northwind", icon: "fa-anchor")]
[assembly: NavigationLink(7200, "Northwind/Orders", typeof(Northwind.OrderPage), icon: "fa-shopping-cart")]
```

([NorthwindNavigation.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Shared/NorthwindNavigation.cs)). Người dùng không có quyền của trang sẽ không thấy mục menu tương ứng.

**JSX, DomWise và SleekGrid.** Giao diện được viết bằng JSX của **DomWise** (`@serenity-is/domwise`). Thư viện này biên dịch JSX thẳng thành DOM thật, không có virtual DOM, và tích hợp sẵn signals ([JSX with DomWise](https://serenity.is/docs/framework/ui/jsx)). `tsconfig.json` của Serene đặt `"jsxImportSource": "@serenity-is/domwise"` và `"target": "ES2022"` ([tsconfig.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/tsconfig.json)). Grid nền là **SleekGrid**, bản viết lại SlickGrid bằng TypeScript, dùng layout CSS Grid và hỗ trợ ghim cột ([Frontend overview](https://serenity.is/docs/framework/ui/readme); [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)). jQuery không còn là phụ thuộc. Với thao tác DOM theo kiểu mệnh lệnh, dùng `Fluent`.

**Bẫy.** Trong ES modules, lớp không còn là biến toàn cục. Gõ `MyProject.Module.XDialog` trong console sẽ ra `undefined`. File module không có `import` hoặc `export` sẽ báo lỗi "global script file" ([ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)). **Không sửa tay `Modules/ServerTypes`** vì nó bị ghi đè mỗi lần build **[Suy luận từ cơ chế build]**. Thay vì override `getColumns()` như trước, giờ override `createColumns()` ([changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)).

**Bài tập.** Trong `MovieGrid`, thêm nút toolbar "Xuất Excel" (cần endpoint `ListExcel`, xem Bài 15). Thêm một cột unbound chứa icon bấm được để mở trang IMDB tìm theo tiêu đề. Trong `MovieDialog`, ẩn một nút khi đang ở chế độ thêm mới. **Tự kiểm tra:** Bốn method grid bắt buộc phải override là gì? `afterLoadEntity` khác `onSaveSuccess` ở điểm nào?

---

### Bài 9. Editor, formatter và lookup

**Mục tiêu.** Chọn đúng editor có sẵn. Viết được formatter và editor riêng. Biết khi nào dùng lookup script, khi nào dùng `ServiceLookupEditor`.

**Editor có sẵn.** Phía C# có các attribute: `LookupEditor`, `AsyncLookupEditor`, `ServiceLookupEditor`, `EnumEditor`, `DateEditor`, `DateTimeEditor`, `DecimalEditor`, `IntegerEditor`, `TextAreaEditor`, `HtmlContentEditor`, `EmailAddressEditor`, `MaskedEditor`, `BooleanEditor`, `CheckLookupEditor`, `RadioButtonEditor`, `TimeEditor`, `UrlEditor`… ([EditorTypes folder](https://github.com/serenity-is/Serenity/tree/master/src/core/ComponentModel/PropertyGrid/EditorTypes)). Lookup editor có các tùy chọn `InplaceAdd`, `DialogType`, `CascadeFrom`/`CascadeField`/`CascadeValue`, `FilterField`/`FilterValue`, `Multiple`, `Async` ([LookupEditorBaseAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/src/core/ComponentModel/PropertyGrid/EditorTypes/LookupEditorBaseAttribute.cs)). Ví dụ dropdown thành phố phụ thuộc quốc gia: `[AsyncLookupEditor(typeof(Lookups.CustomerCityLookup), CascadeFrom = "Country", AutoComplete = true)]` ([CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs)).

**Lookup script và ServiceLookupEditor.** `[LookupScript]` trên Row tạo ra một script được cache phía server và tải toàn bộ về client. Mặc định script chỉ chứa Id và Name, muốn thêm field thì gắn `[LookupInclude]`. Lookup thừa hưởng `ReadPermission` của Row. Cache mặc định là vô hạn. Phía client nên dùng `getLookupAsync`, vì `getLookup` đồng bộ đã lỗi thời. Sau khi dữ liệu đổi thì gọi `reloadLookup('Key')`. Theo docs, `ServiceLookupEditor` "never loads all the records" và "does not cache anything by default", và **docs khuyên dùng nó cho ứng dụng mới**. Lookup script chỉ hợp với bảng nhỏ, ít thay đổi, và **không được chứa dữ liệu nhạy cảm** ([Lookups](https://serenity.is/docs/framework/dynamic-scripts/lookups)). Từ 10.6, `ServiceLookupEditor` dựa vào `[ListRequestCapabilities]` và có thể tự suy ra endpoint từ metadata của Row ([CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)). Lookup tùy biến (danh sách thành phố không trùng lặp):

```csharp
[LookupScript, NorthwindModule]
public class OrderShipCityLookup : RowLookupScript<OrderRow>
{
    public OrderShipCityLookup(ISqlConnections sqlConnections) : base(sqlConnections)
    {
        IdField = TextField = OrderRow.Fields.ShipCity.PropertyName;
    }
    protected override void PrepareQuery(SqlQuery query)
    {
        var fld = OrderRow.Fields;
        query.Distinct(true).Select(fld.ShipCountry).Select(fld.ShipCity)
             .Where(fld.ShipCountry != "" & fld.ShipCountry.IsNotNull() & fld.ShipCity != "" & fld.ShipCity.IsNotNull());
    }
    protected override void ApplyOrder(SqlQuery query) { }
}
```

([OrderShipCityLookup.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderShipCityLookup.cs))

**Formatter tùy biến (phải trả về DOM, không trả về chuỗi HTML):**

```tsx
export class FreightFormatter implements Formatter {
    static [Symbol.typeInfo] = formatterTypeInfo(nsDemoNorthwind); static { registerType(this); }
    format(ctx: FormatterContext): FormatterResult {
        if (ctx.value == null) return "";
        return <>{ctx.value} <i class={[faIcon("balance-scale", "secondary"), "text-opacity-75"]}></i></>;
    }
}
```

([FreightFormatter.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/FreightFormatter.tsx)). Đây là **thay đổi phá vỡ từ 9.x**: `gridDefaults.enableHtmlRendering` mặc định là false, nên chuỗi mà formatter trả về được hiển thị dưới dạng *văn bản*, không được hiểu là HTML ([changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)). Formatter cũ trả về `"<b>" + x + "</b>"` sẽ hiện nguyên các thẻ ra màn hình. Nếu formatter cần lookup, hãy tải lookup bất đồng bộ rồi gọi `ctx.grid?.invalidate()` khi tải xong, như trong `GenreListFormatter` ở chương 8 của Movie ([Movie ch.8](https://serenity.is/docs/tutorials/movies/08-multiple-genres-per-movie)).

**Editor tùy biến.** Editor là một `Widget` implement các interface giá trị, được đăng ký bằng `this.registerEditor(ns, [IStringValue...])`. Sau khi build, `sergen clienttypes` sinh ra attribute C# tương ứng trong `Imports/ClientTypes`, nhờ đó bạn gắn được `[PhoneEditor]` lên Row ([Custom Editors](https://serenity.is/docs/editors/custom-editors); [Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)):

```ts
export class CustomerEditor<P extends LookupEditorOptions = LookupEditorOptions> extends LookupEditorBase<P, CustomerRow> {
    static override[Symbol.typeInfo] = this.registerEditor(nsDemoNorthwind);
    constructor(props: EditorProps<P>) { super({ async: true, ...props }); }
    protected override getLookupKey() { return 'Northwind.Customer'; }
    protected override getItemText(item, lookup) { return super.getItemText(item, lookup) + ' [' + item.CustomerID + ']'; }
}
```

([CustomerEditor.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerEditor.ts))

**Bẫy: cascade dropdown trống.** Nếu dropdown thứ hai trống, kiểm tra ba điều. `CascadeField` phải khớp *chính xác* tên property phía client (`CountryId` khác `CountryID`), nên dùng `nameof()`. `CascadeFrom` phải khớp id của editor nguồn. Field dùng để cascade phải được gửi kèm lookup qua `[LookupInclude]` ([Troubleshooting](https://serenity.is/docs/faq/troubleshooting); [How-to: cascaded editors](https://serenity.is/docs/howto/how_to_setup_cascaded_editors)).

**Bài tập.** Làm chương 7 và 8 của Movie (Genre, sau đó chuyển sang nhiều Genre). Viết `RuntimeFormatter` hiển thị "2h 15m" thay vì 135. Đổi editor Genre sang `ServiceLookupEditor` rồi so sánh tab Network của trình duyệt giữa hai cách. **Tự kiểm tra:** Một bảng 200.000 khách hàng thì nên dùng lookup script hay `ServiceLookupEditor`? Vì sao?

---

### Bài 10. Master-detail, quan hệ nhiều-nhiều và chỉnh sửa trong grid

**Mục tiêu.** Xây được form "đơn hàng có các dòng chi tiết", lưu trong một transaction. Dùng được `LinkingSetRelation` cho quan hệ nhiều-nhiều.

**Quan hệ nhiều-nhiều (Movie ch.8).** Trên Row Movie, thêm property `List<int> GenreList` với `[LookupEditor(typeof(GenreRow), Multiple = true)]`, `[NotMapped]` và `[LinkingSetRelation(typeof(MovieGenresRow), MovieId, GenreId)]`, cùng field `ListField<int>`. Bảng nối `MovieGenres` được sinh bằng Sergen ở chế độ "Row & Services only" ([Movie ch.8](https://serenity.is/docs/tutorials/movies/08-multiple-genres-per-movie)).

**Master-detail.** Có bốn mảnh ghép. **(1)** Trên Row cha, khai báo `[MasterDetailRelation(foreignKey: ...)]` cùng `[NotMapped] List<DetailRow>`. Behavior sẽ lo retrieve, save và delete trong *một* transaction ([Movie ch.10](https://serenity.is/docs/tutorials/movies/10-keeping-record-of-the-cast); [Built-in Behaviors](https://serenity.is/docs/services/built-in-behaviors)):

```csharp
[DisplayText("Details"), MasterDetailRelation(foreignKey: nameof(OrderDetailRow.OrderID)), NotMapped]
[MinSelectLevel(SelectLevel.Explicit)]
public List<OrderDetailRow>? DetailList { get => fields.DetailList[this]; set => fields.DetailList[this] = value; }
```

([NW OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs)). **(2)** Một lớp editor kế thừa `GridEditorBase<TDetailRow>` từ `@serenity-is/extensions`. **(3)** Một dialog chi tiết kế thừa `GridEditorDialog`. **(4)** Gắn attribute editor đó lên property trong Form, rồi truyền khóa của bản ghi cha trong `afterLoadEntity` của dialog cha (như `OrderDialog` ở Bài 8).

```ts
export class OrderDetailsEditor<P = {}> extends GridEditorBase<OrderDetailRow, P> {
    static override[Symbol.typeInfo] = this.registerEditor(nsDemoNorthwind);
    protected override getColumnsKey() { return OrderDetailColumns.columnsKey; }
    protected override getDialogType() { return OrderDetailDialog; }
    protected override getRowDefinition() { return OrderDetailRow; }
    protected override getService() { return OrderDetailService.baseUrl; }

    protected override async validateEntity(row: OrderDetailRow, id: any) {
        row.ProductID = toId(row.ProductID);
        const sameProduct = this.view.getItems().find(x => x.ProductID === row.ProductID);
        if (sameProduct && this.itemId(sameProduct) !== id) { alertDialog('This product is already in order details!'); return false; }
        if (this.connectedMode) return true;
        const lookup = await ProductRow.getLookupAsync();
        row.ProductName = lookup.itemById[row.ProductID].ProductName;
        row.LineTotal = (row.Quantity || 0) * (row.UnitPrice || 0) - (row.Discount || 0);
        return true;
    }
    protected override getNewEntity() { return { OrderID: this.orderId }; }

    public set orderId(value: number) {
        if (this._orderId !== toId(value)) {
            this.setEquality(OrderDetailRow.Fields.OrderID, this._orderId = toId(value));
            this.connectedMode = this._orderId != null;   // đơn hàng đã tồn tại → connected mode
            this.refresh();
        }
    }
}
```

([OrderDetailsEditor.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/OrderDetail/OrderDetailsEditor.ts)). Lưu ý có hai chế độ hoạt động. Ở **chế độ in-memory**, các dòng chi tiết được giữ trong bộ nhớ và lưu cùng bản ghi cha. Ở **connected mode**, mỗi dòng chi tiết được lưu ngay qua service riêng của nó ([GridEditorBase.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/extensions/Modules/GridEditor/GridEditorBase.tsx)). Một kiểu master-detail khác là đặt grid liên quan trong một tab, không nằm trong form. `CustomerOrdersGrid` là ví dụ của kiểu này ([CustomerOrdersGrid.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerOrdersGrid.ts)). Chỉnh sửa trực tiếp trên grid có thể làm theo cách của `ProductGrid`: formatter trả về `<input>`, các thay đổi được gom vào `pendingChanges`, rồi nút "Save Changes" gọi `Update` cho từng dòng ([ProductGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductGrid.tsx)).

**Bẫy.** Cập nhật chi tiết in-memory mà bị nhân đôi dòng: hãy bỏ `[IdProperty]` khỏi dialog chi tiết, vì `GridEditorDialog` dùng một id giả `__id`. View field trống khi mở lại bản ghi chi tiết: thêm `[MinSelectLevel(SelectLevel.List)]` ([Troubleshooting](https://serenity.is/docs/faq/troubleshooting)). Từ 10.6, `DeltaLister`, thành phần dùng để so sánh danh sách chi tiết cũ và mới, mặc định chạy ở chế độ strict: id mới không hợp lệ sẽ ném lỗi. Muốn quay về hành vi dễ dãi cũ, truyền `DeltaOptions.IgnoreInvalidNewId` ([10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)).

**Bài tập.** Làm chương 10 của Movie (Cast: Person, MovieCast, `MovieCastEditor`, `MovieCastEditDialog`). Sau đó tự làm Order/OrderLine cho một mini-project, có tính `LineTotal` ở phía client và kiểm tra lại tổng ở `BeforeSaveAsync` phía server. **Tự kiểm tra:** Vì sao phải tính lại tổng ở server dù client đã tính rồi?

---

## Chặng 4 — Nâng cao: bảo mật, đa ngôn ngữ, hiệu năng, vận hành

### Bài 11. Phân quyền — và cú chuyển "fail-closed" của 10.6

**Mục tiêu.** Hiểu hệ thống quyền từ attribute đến lúc kiểm tra. Biết bảo vệ đủ ba lớp: Row, endpoint và trang. Tránh được cái bẫy lớn nhất khi lên 10.6.

**Khái niệm.** Permission key là chuỗi tự do theo dạng `Area:Feature`. Key được dùng trong attribute ở Row, trang và endpoint, được phát hiện bằng reflection (`IPermissionKeyLister`) để hiện trong dialog cấp quyền, và được kiểm tra ở server bởi `IPermissionService`. Serene implement service này bằng `BasePermissionService<TUserPermissionRow, TUserRoleRow>`, có cache và tra theo role ([Authorization docs](https://serenity.is/docs/framework/authorization)). Các attribute ở cấp Row: `ReadPermission`, `ModifyPermission`, `InsertPermission`, `UpdatePermission`, `DeletePermission`, `ServiceLookupPermission`. Ở cấp field: `FieldReadPermission`, `FieldInsertPermission`, `FieldUpdatePermission`, `FieldModifyPermission`. Lookup có quyền riêng qua `[LookupScript(Permission = ...)]`. `PageAuthorize` ném lỗi khi chưa đăng nhập, còn `ServiceAuthorize` trả về lỗi service thân thiện với AJAX. Thứ tự dự phòng: `AuthorizeCreate` tìm Insert, rồi Modify, rồi Read. `AuthorizeUpdate` tìm Update, rồi Modify, rồi Read. `AuthorizeDelete` tìm Delete, rồi Modify, rồi Read (cùng nguồn).

```csharp
[NestedPermissionKeys]
[DisplayName("Administration")]
public class PermissionKeys
{
    [Description("User, Role Management and Permissions")]
    public const string Security = "Administration:Security";
}
```

**Key đặc biệt:** `"*"` (`SpecialPermissionKeys.Public`) cho phép mọi người, kể cả người chưa đăng nhập. `"?"` (`LoggedIn`) cho phép mọi người đã đăng nhập. `"DENY"` cấm tất cả, kể cả super admin. Biểu thức logic như `"(A & B) | C"` chỉ hoạt động khi bạn bọc service bằng `LogicOperatorPermissionService` ([Authorization docs](https://serenity.is/docs/framework/authorization)). Dấu hai chấm tạo cấu trúc cây trong dialog cấp quyền. Không tạo key kết thúc bằng `:`. Key không được dùng ở attribute nào thì phải đăng ký bằng `[assembly: RegisterPermissionKey("...")]` mới hiện trong dialog ([How to register permissions](https://serenity.is/docs/howto/how_to_register_permissions_in_serene)).

**Thay đổi lớn của 10.6 (breaking).** Đây là điểm thầy muốn bạn nhớ nhất. **Row không có attribute quyền không còn mặc định công khai nữa, nó bị từ chối.** Key null hoặc rỗng sẽ ném `ArgumentException`, và `DENY` không bao giờ được cấp. Khi người dùng bị từ chối Save, Delete hoặc Undelete, server trả về `EntityNotFound` thay vì `AccessDenied` để không lộ việc bản ghi có tồn tại hay không. Riêng Create vẫn trả về `AccessDenied`. Quyền đọc field giờ được áp cả khi sắp xếp, lọc bằng equality, quick search và trong criteria. Undelete có quyền riêng, tách khỏi delete ([Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)). Hệ quả thực tế: một tutorial cũ bảo bạn "tạo Row, chưa cần quan tâm quyền" sẽ cho ra một trang **trống trơn hoặc báo không tìm thấy bản ghi** trên 10.6. Lỗi này rất khó hiểu với người mới, vì thông báo không hề nhắc đến chữ "quyền".

**Phía client chỉ để hiển thị.** Dùng `await Authorization.hasPermissionAsync("Orders:View&Orders:Approve")` để ẩn hoặc hiện nút. Docs nói rõ: "Client-side permission checks are **not security**" ([Client authorization](https://serenity.is/docs/framework/authorization-client)).

**Bẫy.** Phải bảo vệ đủ **ba lớp**: attribute trên Row, `[ServiceAuthorize]` trên endpoint, `[PageAuthorize]` trên trang. Nếu đổi key ở Row mà quên endpoint, bạn sẽ gặp lỗi kèm danh sách trống. Nếu trang không hiện trong menu, hãy kiểm tra `PageAuthorize`. Thêm hằng số vào `PermissionKeys` không làm key hiện trong dialog, vì lớp đó chỉ phục vụ IntelliSense ([Troubleshooting](https://serenity.is/docs/faq/troubleshooting)). Một số tính năng chỉ có trong StartSharp, gồm Implicit Permissions (`[ImplicitPermission]`), Login As User, giao diện Account Elevation và 2FA ([Implicitly granted permissions](https://serenity.is/docs/startsharp/features/implicitly-granted-permissions); [Authorization docs](https://serenity.is/docs/framework/authorization)). Trong ví dụ phân quyền chi tiết, docs viết `[InsertPermission("Administration:User:Delete")]`, nhiều khả năng là lỗi đánh máy của `DeletePermission` (cùng nguồn).

**Bài tập.** Tạo các key `MovieDB:Movie:View` và `MovieDB:Movie:Modify`. Gắn chúng vào `MovieRow` (Read/Modify), endpoint và trang. Tạo role "Biên tập viên" chỉ có quyền View rồi đăng nhập bằng user thuộc role đó. Bạn phải thấy được danh sách, không lưu được, và khi thử lưu bằng cách gọi API trực tiếp thì nhận về `EntityNotFound`. Sau đó xóa hẳn `[ReadPermission]` khỏi Row để tận mắt thấy fail-closed hoạt động. **Tự kiểm tra:** Vì sao 10.6 trả về `EntityNotFound` chứ không trả `AccessDenied` khi Save bị từ chối?

---

### Bài 12. Bản địa hóa (localization)

**Mục tiêu.** Dịch được giao diện và nhãn của Row, đồng thời biết đưa bản dịch lên production cho đúng cách.

**Khái niệm.** Mọi văn bản đi qua `ITextLocalizer`, được hỗ trợ bởi các từ điển của `ILocalTextRegistry`. Thứ tự tra: ngôn ngữ hiện tại, rồi fallback tường minh, rồi ngôn ngữ trung tính, cuối cùng là invariant (ví dụ en-UK → en-US → en → invariant). Nhãn trên Row sinh ra key dạng `Db.<Prefix>.<Property>`. Văn bản trong code được khai báo bằng lớp `[NestedLocalTexts]`:

```csharp
[NestedLocalTexts]
public static partial class Texts
{
    public static class Site { public static LocalText WelcomeMessage = "Welcome to Serenity"; }
}
```

Văn bản trong file JSON đặt theo tên `{Prefix}.{LanguageID}.json`, ví dụ `site.texts.de.json`. File có thể nhúng trong `texts/resources` (nạp bởi `AddJsonResourceTexts`) hoặc nằm trên đĩa ở `App_Data/texts` (nạp bởi `AddJsonTexts`). Trang Administration > Translations ghi vào `App_Data/texts` ([Localization docs](https://serenity.is/docs/framework/localization)).

**Bẫy.** **Trước khi publish, chuyển các bản dịch từ `App_Data/texts` sang `texts/resources`** (cùng nguồn). Nếu chạy nhiều instance hoặc container, bản dịch sửa trên giao diện chỉ nằm trên một máy và sẽ mất khi deploy lại. Vì vậy hãy coi trang Translations là công cụ lúc phát triển **[Suy luận]**. Docs ghi rõ không khuyến nghị đổi key của enum qua `EnumKeyAttribute`. Hàm static `InitializeLocalTexts` cũ đã được thay bằng service `ILocalTextInitializer`. Ghi chú nghiên cứu chưa đọc trang [localization phía client](https://serenity.is/docs/framework/localization-client). Các tên hàm `localText`/`tryGetText` mà thầy biết chỉ là trí nhớ, chưa kiểm chứng **[Khoảng trống]**.

**Bài tập.** Tạo file `site.texts.vi.json` dịch nhãn của Movie sang tiếng Việt, chuyển ngôn ngữ và kiểm tra. Sau đó thêm `Texts.Site.MovieSaved` và dùng nó trong `ValidationError` thông qua overload có `ITextLocalizer`. **Tự kiểm tra:** Key của nhãn `Title` trên `MovieRow` trông như thế nào?

---

### Bài 13. Caching hai tầng

**Mục tiêu.** Hiểu `ITwoLevelCache`, cơ chế vô hiệu hóa theo nhóm, và cấu hình đúng khi chạy nhiều server (web farm).

**Khái niệm.** `ITwoLevelCache` gồm `IMemoryCache` (cache cục bộ) và `IDistributedCache` (cache phân tán). Khi đọc, nó tìm ở cache cục bộ trước, rồi đến cache phân tán, cuối cùng mới vào database. Mỗi giá trị đi kèm một số "generation" ngẫu nhiên. `ExpireGroupItems(groupKey)` làm mất hiệu lực cả nhóm bằng cách đổi số generation. **Các handler Create, Update, Delete và Undelete tự gọi nó** với khóa `ConnectionKey.TableName` ([Caching docs](https://serenity.is/docs/framework/caching)).

```csharp
// cache.Get(key, localExpiration, distributedExpiration, groupKey, loader)
// cache.GetLocalStoreOnly(key, expiration, groupKey, loader)
// cache.ExpireGroupItems(MyRow.Fields.GenerationKey)
```

Dashboard của Serene cache số liệu trong 5 phút bằng `GetLocalStoreOnly(..., o.GenerationKey, ...)` ([DashboardPage.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/Dashboard/DashboardPage.cs)). Trong handler, gọi `Cache.InvalidateOnCommit(UnitOfWork, MyRow.Fields)` để cache chỉ bị xóa khi transaction đã commit thành công, đúng như `UserSaveHandler` làm ở Bài 5.

**Bẫy.** Serenity không kèm sẵn cache phân tán nào. Serene và StartSharp mặc định dùng `DistributedMemoryCache`. Khi chạy web farm, bạn **phải đăng ký Redis hoặc SQL Server cache trước `AddServiceHandlers`/`AddCaching`**. Số generation được cache cục bộ 5 giây, nên các server khác có thể trả dữ liệu cũ trong tối đa 5 giây ([Caching docs](https://serenity.is/docs/framework/caching)). Throttler đăng nhập (50 lần sai mỗi 30 phút cho mỗi username) cũng cần cache phân tán thì mới đếm chung được giữa các server ([Authorization docs](https://serenity.is/docs/framework/authorization)). Cơ chế vô hiệu hóa cache của lookup chưa được nghiên cứu sâu **[Khoảng trống]**.

**Bài tập.** Viết một endpoint "thống kê phim" cache 10 phút với group key là `MovieRow.Fields.GenerationKey`. Thêm một phim mới và xác nhận số liệu được làm mới ngay. **Tự kiểm tra:** Vì sao bạn không cần tự gọi `ExpireGroupItems` sau khi Save?

---

### Bài 14. Upload file và ảnh

**Mục tiêu.** Thêm được trường ảnh vào form, hiểu luồng upload hai pha, và nắm hai yêu cầu bắt buộc của 10.6.

**Khái niệm: hai pha.** Ở pha thứ nhất, client gửi file lên `/File/TemporaryUpload`. `IUploadProcessor` kiểm tra tên file, có thể quét virus qua `IUploadAVScanner`, validate, rồi ghi vào thư mục tạm và tạo ảnh scale và thumbnail qua `IImageProcessor`. Ở pha thứ hai, khi bản ghi được Save, `FileUploadBehavior` chuyển file sang đường dẫn vĩnh viễn. File cũ bị xóa khi commit, hoặc được lưu trữ nếu bật `CopyToHistory`. Nếu rollback thì file mới bị xóa ([Uploads docs](https://serenity.is/docs/services/upload)).

```csharp
[DisplayName("User Image"), Size(100)]
[ImageUploadEditor(FilenameFormat = "UserImage/~", CopyToHistory = true)]
public string? UserImage { get => fields.UserImage[this]; set => fields.UserImage[this] = value; }
```

Các editor có sẵn: `ImageUploadEditor`, `FileUploadEditor`, `MultipleImageUploadEditor`, `MultipleFileUploadEditor` (hai editor "Multiple" lưu dữ liệu dạng mảng JSON). Tùy chọn gồm `MaxSize`, `AllowedExtensions`, `ThumbSizes`, `OverwriteOption`… File vĩnh viễn mặc định nằm ở `App_Data/upload/` và được phục vụ tại `/upload/` (cùng nguồn).

**Hai yêu cầu bắt buộc từ 10.6.** Thứ nhất, **ImageSharp đã bị gỡ bỏ**, nên bạn phải đăng ký một `IImageProcessor` thật *trước* `AddUploadStorage()`. Mặc định framework đăng ký `ThrowingImageProcessor`, một placeholder chỉ ném lỗi. Serene và StartSharp có sẵn một implementation dùng SkiaSharp ([Uploads docs](https://serenity.is/docs/services/upload); [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)). Thứ hai, **đặt attribute editor ở Row chứ không ở Form**, nếu không behavior sẽ không chạy và file nằm mãi trong thư mục tạm ([Uploads docs](https://serenity.is/docs/services/upload)).

**Bẫy bảo mật.** Kiểm tra quyền đọc theo từng file (`[FileReadPermission]`, `PathPermissions`) chỉ có trong `SecureUploadFileResponder` của gói premium Serenity.Pro.Extensions. **Với responder mặc định của Serene, ai biết URL đều đọc được file** (cùng nguồn). Với tài liệu nhạy cảm, bạn nên viết một `IUploadFileResponder` riêng, hoặc lưu file ngoài đường dẫn công khai và phục vụ qua một endpoint có kiểm tra quyền **[Suy luận]**. Trang [AV scan](https://serenity.is/docs/av) chưa được đọc **[Khoảng trống]**.

**Bài tập.** Làm chương 12 của Movie: thêm `PrimaryImage` (một ảnh) và `GalleryImages` (nhiều ảnh). Kiểm tra trong `Startup` xem `IImageProcessor` đã được đăng ký trước `AddUploadStorage()` chưa. **Tự kiểm tra:** Khi người dùng bấm Hủy sau lúc đã upload, file nằm ở đâu và ai dọn nó?

---

### Bài 15. Báo cáo và xuất Excel/PDF

**Mục tiêu.** Xuất được grid ra Excel, viết được một báo cáo `IReport`, và cấu hình PDF chạy được trong container.

**Khái niệm.** `IReport` có một phương thức là `GetData()`, và các public property của nó là tham số báo cáo. `IDataOnlyReport` xuất ra Excel hoặc CSV. `[Report]` dùng để đăng ký báo cáo, còn `[RequiredPermission]` để giới hạn ai được xem. `IReportFactory` hỗ trợ DI qua constructor ([Reporting docs](https://serenity.is/docs/services/reporting)):

```csharp
[Report, RequiredPermission(PermissionKeys.General)]
[Category("Northwind/Orders"), DisplayName("Customer Gross Sales")]
public class CustomerGrossSalesReport(ISqlConnections sqlConnections, ITextLocalizer localizer, IServiceProvider serviceProvider) : IReport, IDataOnlyReport
{
    [DisplayName("Start Date")] public DateTime? StartDate { get; set; }
    public object GetData() { /* truy vấn qua SqlConnections */ }
}
```

Để xuất grid ra Excel, endpoint thêm một action `ListExcel` dùng `IExcelExporter`, và grid thêm nút `ExcelExportHelper.createToolButton(...)` như ví dụ OrderGrid ở Bài 8 ([NW ProductEndpoint.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductEndpoint.cs); [OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx)). Từ 9.0, thư viện Excel đã đổi từ EPPlus sang **ClosedXML** ([9.0.0 notes](https://serenity.is/docs/release-notes/9.0.0)). PDF được tạo qua `IHtmlToPdfConverter`. Bản miễn phí là `WKHtmlToPdfConverter`, nó tìm file thực thi theo thứ tự: cấu hình `WKHtmlToPdf:ExecutablePath`, thư mục assembly, content root, `App_Data/Reporting`, `bin`, rồi PATH. Bản premium là `PuppeteerHtmlToPdfConverter`. `[UseChromeHtmlToPdf]` đã lỗi thời ([Reporting docs](https://serenity.is/docs/services/reporting)).

**Bẫy.** Bộ chuyển PDF gọi ngược lại endpoint render của chính website. Vì vậy **khi chạy sau reverse proxy hoặc trong container, phải đặt `EnvironmentSettings:SiteInternalUrl`** (cùng nguồn). Nếu bạn bật `DefaultPageSize`/`MaxPageSize` của 10.6, file xuất có thể bị cắt cụt. Hãy dùng `SuppressPagingLimits()` cho các tác vụ xuất và job nền ([10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)). Lỗi xuất PDF là chủ đề được hỏi nhiều trên GitHub Discussions ([Discussions](https://github.com/serenity-is/Serenity/discussions)). Chi tiết endpoint `ListExcel` trong Serene chưa được đối chiếu với trang docs **[Khoảng trống]**.

**Bài tập.** Thêm nút xuất Excel cho grid Movie. Viết báo cáo "Phim theo năm" bằng `IDataOnlyReport` với tham số `FromYear`/`ToYear`. **Tự kiểm tra:** Trong Docker, PDF bị lỗi trong khi Excel vẫn chạy. Bạn kiểm tra hai cấu hình nào trước?

---

### Bài 16. Multi-tenancy: phân tách dữ liệu theo từng khách hàng

**Mục tiêu.** Hiểu mẫu chính thức "marker interface + implicit behavior", và áp dụng được nó cho mọi loại bảo mật mức dòng.

**Khái niệm.** Tutorial chính thức có 14 bước, xây tiếp trên Movie. Mọi bảng, kể cả Users, có thêm cột `TenantId`. `TenantId` được đưa vào user definition và claims. Sau đó dùng một interface đánh dấu và một behavior ngầm định ([Multi-tenancy intro](https://serenity.is/docs/tutorials/multi_tenancy/multi_tenancy)):

```csharp
public interface IMultiTenantRow
{
    Int32Field TenantIdField { get; }
}
```

`MultiTenantBehavior : IImplicitBehavior` implement các interface behavior async cho Retrieve, List, Save và Delete. `ActivateFor` trả về false nếu Row không implement `IMultiTenantRow`. Trong `OnPrepareQueryAsync`, nó thêm `query.Where(tenantIdField == handler.Context.User.GetTenantId())`, trừ khi user có quyền `Tenants`. Trong `OnSetInternalFieldsAsync`, nó gán TenantId khi tạo mới. Trong `OnValidateRequestAsync`, nó chặn việc đổi TenantId. Khi xóa, nó chặn xóa bản ghi của tenant khác ([Using Serenity service behaviors](https://serenity.is/docs/tutorials/multi_tenancy/using_serenity_service_behaviors)). Lookup cần một lớp riêng là `MultiTenantRowLookupScript<TRow>`. Lớp này đặt `Expiration = TimeSpan.FromDays(-1)` để tắt cache của script manager, vì cache đó được tính theo script chứ không theo tenant. Thay vào đó nó cache theo từng tenant bằng `twoLevelCache.GetLocalStoreOnly($"MultiTenantLookup:{ScriptName}:{tenantId}", ...)`. Cách dùng: `[LookupScript(LookupType = typeof(MultiTenantRowLookupScript<>))]` ([Handling lookup scripts](https://serenity.is/docs/tutorials/multi_tenancy/handling_lookup_scripts)).

**Góc nhìn phản biện.** Chính tác giả tutorial **khuyên dùng database riêng cho từng khách hàng** thay vì chung bảng. Cách chung bảng rẻ hơn khi vận hành, nhưng khó sao lưu hoặc di chuyển dữ liệu của riêng một tenant và có thể ảnh hưởng hiệu năng ([Multi-tenancy intro](https://serenity.is/docs/tutorials/multi_tenancy/multi_tenancy)). StartSharp không có mục multi-tenancy trong danh sách tính năng ([StartSharp](https://serenity.is/docs/startsharp/)).

**Bẫy.** Behavior là singleton, nên phải an toàn đa luồng. Lookup tùy biến nào override `PrepareQuery` mà không gọi `base` thì phải tự gọi `AddTenantFilter`. **Lookup không lọc tenant sẽ làm lộ dữ liệu của toàn bộ các tenant** (cùng nguồn). Nếu user ID của bạn không phải `Int32` (10.6 cho phép long, Guid hoặc string), hãy điều chỉnh kiểu của `TenantIdField` cho phù hợp **[Suy luận]**. Code thêm TenantId vào claims và hàm `GetTenantId()` nằm ở các bước đầu của tutorial mà ghi chú chưa đọc **[Khoảng trống]**.

**Bài tập.** Áp dụng đúng mẫu này cho bài toán "mỗi user chỉ thấy phim do mình tạo". Tạo interface `IOwnedRow` với `InsertUserId`, và một behavior lọc theo user hiện tại, trừ khi user có quyền `MovieDB:Movie:All`. **Tự kiểm tra:** Vì sao lọc ở behavior an toàn hơn lọc bằng `setEquality` ở grid?

---

### Bài 17. Nâng cấp phiên bản và triển khai production

**Mục tiêu.** Nâng cấp an toàn theo release notes và triển khai một ứng dụng Serenity lên môi trường thật.

**Quy trình nâng cấp chính thức.** Bước một, cập nhật các gói `Serenity.*` (và `Serenity.Pro.*` nếu có). Bước hai, chạy `dotnet tool update sergen`, vì phiên bản sergen phải khớp với Serenity. Bước ba, cập nhật `@serenity-is/corelib` và `@serenity-is/tsbuild` rồi chạy `npm install`. Bước bốn, chạy `dotnet sergen restore`. Bước năm, đọc migration guide và release notes theo thứ tự. **Từ 6.1.8 không còn migration guide riêng**, nên release notes chính là hướng dẫn nâng cấp ([Migration intro](https://serenity.is/docs/migration/README)). Khách hàng premium có thêm công cụ [Stargen](https://serenity.is/docs/migration/stargen). Danh sách việc cần làm khi lên 10.6: thêm attribute quyền cho các Row trước đây công khai ngầm; bật lại các field có tên nhạy cảm nếu thực sự cần; đăng ký `IImageProcessor` trước `AddUploadStorage()`; cấu hình `UserEntityOptions` trước migration đầu tiên và cập nhật code dùng `LoggingRow`, vì user ID giờ có kiểu `object`/`Field`; sửa code truy vấn tùy biến (`Params` chỉ đọc, `ICriteria`, bỏ `IChainable/With`); với StartSharp, thay các lớp `ConnectionKeys`; xử lý `DeltaLister` strict; chạy lại Sergen; rà soát các list endpoint trước khi bật giới hạn trang ([Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)). Khi nâng cấp từ 9.x trở xuống, bạn cũng cần lưu ý: lên .NET 9+ thì thêm `BeforeTargets="ResolveProjectStaticWebAssets"` vào target `RunTSBuild` ([9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)), và từ 9.1+ thì `DataGrid.getPersistanceKey()` không còn nữa ([Discussions](https://github.com/serenity-is/Serenity/discussions)).

**Triển khai.** Docs hiện tại **không có trang triển khai riêng**, nên việc này thực chất là publish một ứng dụng ASP.NET Core thông thường ([Docs index](https://serenity.is/docs/)). Có vài điểm riêng của Serenity cần nhớ. Một là bật bundling CSS và script (`AddCssAndScriptBundling()`). Minifier mặc định là `EsBuildMinifier`, được "downloaded on demand", nên build agent không có mạng sẽ gặp vấn đề ([Script bundling](https://serenity.is/docs/framework/script-bundling)). Hai là chuyển bản dịch vào `texts/resources` (Bài 12). Ba là dùng cache phân tán khi chạy farm (Bài 13). Bốn là đảm bảo `UploadSettings:Path` trỏ tới nơi lưu trữ bền vững (Bài 14). Năm là đặt `SiteInternalUrl` và `SiteExternalUrl` (Bài 15).

> **Mâu thuẫn cần biết.** Trang Script bundling vẫn hướng dẫn bật bundling trong `appsettings.bundles.json` ([Script bundling](https://serenity.is/docs/framework/script-bundling)), trong khi release notes 10.1 nói file này **đã deprecated** và chuyển sang `buildGlobalBundles` cùng import maps ([10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)). Bạn hãy mở một project 10.6 mới tạo để xem template thực sự dùng cách nào **[Khoảng trống]**.

**Kinh nghiệm ASP.NET Core chung, áp dụng cho Serenity [Suy luận, không có trang chính thức].** Chạy `npm ci` và tsbuild trước khi `dotnet publish -c Release` để `wwwroot/esm` được cập nhật. Trên Linux, chạy Kestrel sau nginx và cấu hình forwarded headers. Trên Docker, dùng image runtime `mcr.microsoft.com/dotnet/aspnet` và cài thêm các phụ thuộc native nếu cần xuất PDF. Mount volume bền vững cho `App_Data/upload` và cho Data Protection key ring, vì cookie đăng nhập và token reset mật khẩu sẽ hỏng nếu key không được chia sẻ hoặc không bền ([Security](https://serenity.is/docs/framework/security)). Đưa connection string vào biến môi trường (`Data__Default__ConnectionString`). Cân nhắc chạy migration một cách có chủ đích thay vì để tự chạy lúc khởi động. Lời khuyên cũ cho lỗi "table not found sau khi publish" chỉ tới một cơ chế an toàn trong `RunMigrations` chặn migration chạy trên database lạ. Bạn cần kiểm tra lại vị trí của nó trong template hiện tại ([Troubleshooting](https://serenity.is/docs/faq/troubleshooting)).

**Bài tập.** Nâng một project từ 10.5.x lên 10.6.0 chỉ bằng cách đọc release notes, và ghi lại mọi lỗi gặp phải. Đóng gói ứng dụng Movie vào Docker với SQL Server container và volume cho upload. **Tự kiểm tra:** Kể tên năm thứ phải "bền vững" giữa các lần deploy.

---

## Lộ trình học và ba mini-project

Không có lộ trình học chính thức nào kèm ước lượng thời gian. Lộ trình dưới đây là tổng hợp của thầy, ghép từ các tài nguyên chính thức **[Suy luận]**. Kiến thức nền nên có trước khi bắt đầu: C# ở mức khá (attribute, generic, async, nullable, `nameof`), ASP.NET Core cơ bản (DI, `appsettings`, middleware), SQL thành thạo (join, khóa), và TypeScript cơ bản (ES modules, kế thừa lớp, JSX). jQuery thì không cần nữa ([Getting Started](https://serenity.is/docs/getting_started/README); [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)).

| Giai đoạn | Thời lượng ước tính | Học | Mini-project |
|---|---|---|---|
| 0. Nền tảng | ~1 tuần (nếu cần) | Ôn C#/DI/SQL/TS. Cài đặt, đi tour Serene ([Serene tour](https://serenity.is/docs/serene_tour/README)) | — |
| 1. Cơ bản | 1–2 tuần | Bài 1–3, 7–8. Tutorial Movie chương 0–6 | **A. "Thư viện cá nhân"**: Book, Author, Genre. Tự viết migration, chạy Sergen, thêm lookup, quick filter, formatter, mặc định "today". Kèm bài tập phân quyền 3 lớp với role không phải admin |
| 2. Trung cấp | 2–4 tuần | Bài 4–6, 9–10, 12–15. Movie chương 7–12, tutorial Courses ([Courses](https://serenity.is/docs/tutorials/courses/00-introduction)), đọc Northwind và BasicSamples | **B. "Kho và đơn hàng"**: Customer, Product, Category, Order/OrderDetail (in-memory editor), cascade Country→City, tổng tiền tính lại ở server, xuất Excel (ClosedXML), ảnh sản phẩm (cần `IImageProcessor`), bản dịch tiếng Việt |
| 3. Nâng cao | 4+ tuần | Bài 11, 13, 16–17. Tutorial Multi-tenancy, behavior, DomWise, optimistic concurrency, `IncludeMore` | **C. "Helpdesk/CRM đa tenant"**: phân tách tenant, quy trình ticket, dashboard, PDF, `IConcurrencyVersionRow`, bảo mật mức dòng, triển khai Docker. Thêm một bài nâng cấp một phiên bản minor chỉ bằng release notes |

Hãy rèn các thói quen sau suốt quá trình học. Mở sẵn demo [demo.serenity.is](https://demo.serenity.is) (admin/serenity) cùng mã nguồn `serene/` và `common-features/` *ở đúng tag* trùng với phiên bản NuGet bạn dùng. Kiểm tra ngày viết của mọi đoạn code trước khi chép. Ghi lại phiên bản Serenity trong README của dự án. Commit code ngay sau khi Sergen sinh, để lần sinh lại sau còn có diff mà so ([Serenity README](https://github.com/serenity-is/Serenity); [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)).

## Tài nguyên và mức độ tin cậy: tin nguồn nào, dè chừng nguồn nào

Tài nguyên cộng đồng **ít và phần lớn đã cũ**, nên thứ tự ưu tiên rất quan trọng. Chính README chính thức cũng cảnh báo rằng video do cộng đồng làm là "unofficial and may be outdated" ([serenity-is/Serenity](https://github.com/serenity-is/Serenity)).

| Mức tin cậy | Tài nguyên | Ghi chú |
|---|---|---|
| ★★★★★ | Mã nguồn monorepo [serenity-is/Serenity](https://github.com/serenity-is/Serenity): `serene/`, `common-features/` (Northwind, BasicSamples), `src/` | Nguồn sự thật. Đọc ở tag trùng phiên bản bạn dùng |
| ★★★★★ | Release notes trên [blog](https://serenity.is/blog/) và [docs](https://serenity.is/docs/release-notes/10.6.0), [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md) | Hướng dẫn nâng cấp thực tế từ v6 đến v10 |
| ★★★★☆ | Docs hiện hành: Backend/Frontend Framework, Services, Grids, Dialogs, tutorial Movie/Courses/Multi-tenancy ([docs](https://serenity.is/docs/)) | Tốt, nhưng có trang chưa liệt kê đủ (ví dụ danh sách dialect) |
| ★★★★☆ | Bài blog khái niệm: [Switching to ES Modules](https://serenity.is/blog/2022/10/15/switching-to-es-modules), [Introducing DomWise](https://serenity.is/blog/2026/07/24/domwise) | Giải thích vì sao framework đổi hướng |
| ★★★☆☆ | [GitHub Discussions](https://github.com/serenity-is/Serenity/discussions) | Đang hoạt động, nhưng 17/25 chủ đề ở trang đầu chưa có trả lời. Khi hỏi, ghi kèm phiên bản và một repro tối giản |
| ★★☆☆☆ | **Trang docs lỗi thời**: [Troubleshooting](https://serenity.is/docs/faq/troubleshooting) (Saltarelle, T4, `web.config`, Select2 3.5.1), [FAQ](https://serenity.is/docs/faq/frequently_asked_questions) (`JsDate.Today`, "script project"), [Code Generator (Sergen)](https://serenity.is/docs/sergen/code_generator_sergen) (`Serenity.CodeGenerator.config`, GenerateSSImports, KDiff3, TFS) | Phần *khái niệm* (cascade, quyền, master-detail) vẫn đúng. Phần *công cụ* đã lỗi thời. Cấu hình hiện đại nằm trong `sergen.json` (`{"Extends": "defaults@9.0.0", ...}`) ([sergen.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/sergen.json)) |
| ★☆☆☆☆ | **Repo archived** [serenity-is/Serene](https://github.com/serenity-is/Serene) và common-features (archived 10/2024, commit cuối khoảng thời 8.6) | Không dùng. Đọc `serene/` trong monorepo thay thế |
| ★☆☆☆☆ | StackOverflow, blog và video bên thứ ba trước 2023 | Mặc định coi là lỗi thời, trừ khi viết theo kiểu module và từ 2023 trở đi. Ghi chú không truy cập được tag [serenity-platform] **[Khoảng trống]** |

Các repo phụ: [sleekgrid](https://github.com/serenity-is/sleekgrid) (subtree chỉ đọc), [devtools](https://github.com/serenity-is/devtools) (Chrome extension cho dev), [docs](https://github.com/serenity-is/docs) (mã nguồn của trang tài liệu) ([GitHub serenity-is org](https://github.com/serenity-is)).

### Bảng tra nhanh: cách cũ và cách mới

| Đừng làm theo (cũ) | Hãy làm (10.x) | Nguồn |
|---|---|---|
| `namespace MyApp.Module { }`, biến toàn cục `Serenity.*`, `Q.*` | ES modules: `import { EntityGrid } from "@serenity-is/corelib"` | [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules); [10.1.0](https://serenity.is/docs/release-notes/10.1.0) |
| `@Decorators.registerClass/registerEditor`, `@Decorators.option` | `static [Symbol.typeInfo] = this.registerClass(...)`, truyền tùy chọn qua props | [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md) |
| `getTemplate()`, `TemplatedWidget/Dialog` | `renderContents()` với JSX (DomWise), `Widget`/`BaseDialog` | [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md) |
| `jsx-dom`, jQuery `$()` | `@serenity-is/domwise`, `Fluent`/DOM gốc | [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md) |
| Override `getColumns()` | Override `createColumns()` | [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md) |
| Formatter trả về chuỗi HTML | Trả về DOM/JSX | [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md) |
| `getLookup` (đồng bộ) | `getLookupAsync`, hoặc `ServiceLookupEditor` | [Lookups](https://serenity.is/docs/framework/dynamic-scripts/lookups) |
| Handler/behavior đồng bộ, lớp `XRepository` | Handler `...Async` + interface + `[FromServices]` | [Save handler](https://serenity.is/docs/services/save_request_handler); [Behaviors](https://serenity.is/docs/services/behaviors) |
| `RowTemplate`, property `IdField` | Property trực tiếp, `GetIdField()` | [Entities](https://serenity.is/docs/framework/data-access/entities); [CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md) |
| Row không khai báo quyền (trước đây là công khai) | Luôn có `[ReadPermission]`/`[ModifyPermission]`, hoặc `"*"`/`"?"` một cách tường minh | [10.6.0](https://serenity.is/docs/release-notes/10.6.0) |
| `.With(...)`, `SqlUpdate.Where(params string[])`, sửa `Params` | Criteria, `ICriteria`, `Freeze` | [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md) |
| ImageSharp, `UploadProcessor`, `ThumbnailGenerator` | `IImageProcessor` (SkiaSharp), `IUploadProcessor` | [Uploads](https://serenity.is/docs/services/upload) |
| EPPlus, `[UseChromeHtmlToPdf]` | ClosedXML, `[UseWKHtmlToPdf]`/Puppeteer (Pro) | [9.0.0](https://serenity.is/docs/release-notes/9.0.0); [Reporting](https://serenity.is/docs/services/reporting) |
| `.sln`, `appsettings.bundles.json`, `ScriptSkip` | `.slnx`, `buildGlobalBundles`/import maps, `TransformIgnore` | [10.1.0](https://serenity.is/docs/release-notes/10.1.0) |

## Kết luận

Toàn bộ 17 bài thực chất xoay quanh một mô hình duy nhất. Học Serenity là học cách **khai báo**: Row mô tả dữ liệu, Form và Columns mô tả cách hiển thị, attribute quyền mô tả ai được làm gì. Code bạn tự viết chỉ là phần "khác biệt": hook trong handler, behavior, override trong grid và dialog. Ai coi Serenity như một bộ sinh CRUD rồi viết đè lên thì sẽ vật lộn mãi. Ai hiểu luồng Row → handler/behavior → ServerTypes → Grid/Dialog thì làm một bảng mới chỉ mất vài phút, và một quy tắc nghiệp vụ dùng chung cũng chỉ cần một behavior. Mẫu multi-tenancy ở Bài 16 thể hiện điều này rõ nhất. Bảo mật mức dòng, tự động gán dữ liệu hay kiểm toán đều có thể viết thành một interface đánh dấu cộng với một `IImplicitBehavior`.

Với những ai học vào lúc này, có hai điều thực tế. Thứ nhất, 10.6 đã đảo ngược mặc định bảo mật: trước đây "quên khai báo" nghĩa là lộ dữ liệu, giờ nghĩa là trang trống. Lỗi dễ thấy hơn và an toàn hơn, nhưng thông báo `EntityNotFound` sẽ đánh lừa người không biết điều này. Thứ hai, do tài liệu cộng đồng mỏng và có cả trang docs chính thức đã cũ, kỹ năng quan trọng nhất không nằm ở việc thuộc API. Nó nằm ở việc **đọc mã nguồn Northwind ở đúng tag phiên bản và đọc release notes trước mỗi lần nâng cấp**. Bạn rèn được thói quen đó từ mini-project A thì đến mini-project C sẽ tự gỡ được phần lớn vướng mắc mà không cần chờ ai trả lời trên Discussions.
