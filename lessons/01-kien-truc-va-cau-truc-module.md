# Bài 01 – Kiến trúc Serenity 3.13.7 và cấu trúc một module

**Mục tiêu:** sau bài này bạn nhìn vào một thư mục module (ví dụ `Modules/Northwind/Category`) và biết
mỗi file làm gì, request đi qua những file nào, và khi cần sửa một thứ thì mở file nào.

Code mẫu trong bài lấy từ Serene 3.13.7, bản .NET Framework:
[Serene.Web/Modules/Northwind/Category](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Category)

---

## 1. Serenity là gì, Serene là gì

- **Serenity** là framework, phát hành dưới dạng NuGet package (`Serenity.Core`, `Serenity.Data`,
  `Serenity.Services`, `Serenity.Web`, `Serenity.Scripts`...). Bạn ít khi sửa code này.
- **Serene** là *template project* tạo ra từ VSIX trong Visual Studio. Project của bạn ở công ty gần như
  chắc chắn được sinh ra từ Serene, rồi đổi tên namespace (ví dụ `Serene.` thành `TenDuAn.`).
- **Sergen** là công cụ sinh code. Nó đọc một bảng trong database rồi sinh ra cả bộ file của module.

Với bản 3.13.7 trên .NET Framework 4.5:

- Web chạy bằng **ASP.NET MVC 5** (`System.Web.Mvc`), cấu hình nằm trong `Web.config`, khởi động trong
  `Global.asax.cs` và `App_Start/SiteInitialization.cs`.
- Client viết bằng **TypeScript theo kiểu `namespace`**, không dùng ES module. Mọi file `.ts` được biên dịch
  chung thành `Scripts/site/<TenProject>.Web.js`.
- UI dựa trên **jQuery, jQuery UI và SlickGrid**, giao diện là AdminLTE.

> Đây là điểm khác lớn nhất so với docs hiện tại trên serenity.is (.NET Core, ES module, `Serenity.Pro`...).
> Nếu thấy docs nói tới `appsettings.json`, `Startup.cs` hay `import { ... } from "@serenity-is/corelib"`,
> thì đó là bản mới và không áp dụng cho dự án của mình.

## 2. Cấu trúc thư mục project Web

```
Serene.Web/
├── App_Start/                 # Khởi động: SiteInitialization, chạy Migrations, RouteConfig
├── Content/                   # CSS, LESS, theme AdminLTE
├── Imports/
│   ├── ServerTypings/         # File .ts SINH TỰ ĐỘNG từ C# (Row, Form, Columns, Service...)
│   │   └── ServerTypings.tt   # T4 template sinh ra các file ở trên
│   └── MVC/MVC.tt             # Sinh hằng số đường dẫn View (MVC.Views....)
├── Migrations/
│   ├── DefaultDB/             # Migration cho DB "Default" (user, role, ...)
│   └── NorthwindDB/           # Migration cho DB "Northwind"
├── Modules/                   # ★ Nơi viết code nghiệp vụ, mỗi module một thư mục
│   ├── Administration/        # User, Role, Permission, Language, Translation
│   ├── Common/                # Dashboard, Navigation, phần dùng chung
│   └── Northwind/             # Module mẫu, có Category, Customer, Order...
├── Scripts/                   # Thư viện JS và file site.js đã biên dịch
├── Views/                     # Layout chung (_Layout.cshtml...)
├── Web.config                 # Connection string, appSettings
└── tsconfig.json
```

Connection string nằm trong `Web.config`. Tên của nó (`Default`, `Northwind`) chính là giá trị trong
`[ConnectionKey("...")]` trên Row:

```xml
<add name="Northwind" connectionString="Data Source=(LocalDb)\MSSqlLocalDB; Initial Catalog=Serene_Northwind_v1; ..." />
```

## 3. Giải phẫu một module: `Northwind/Category`

```
Modules/Northwind/Category/
├── CategoryRow.cs          # Server: entity, ánh xạ bảng ↔ class
├── CategoryRepository.cs   # Server: xử lý nghiệp vụ Create/Update/Delete/Retrieve/List
├── CategoryEndpoint.cs     # Server: API (controller) nhận request JSON
├── CategoryForm.cs         # Server: định nghĩa các field trên form sửa
├── CategoryColumns.cs      # Server: định nghĩa các cột trên grid
├── CategoryPage.cs         # Server: controller MVC trả về trang danh sách
├── CategoryIndex.cshtml    # View: trang chứa grid
├── CategoryGrid.ts         # Client: lưới dữ liệu
└── CategoryDialog.ts       # Client: hộp thoại thêm/sửa
```

Các đoạn code bên dưới đã được rút gọn để dễ đọc (bỏ `using`, viết `=>` thay cho `{ return ...; }`). Code đầy đủ xem ở link đầu bài.

Ghi nhớ nhanh: **Row → Repository → Endpoint** là phía server/dữ liệu, **Form + Columns** là "khai báo UI
bằng C#", còn **Grid + Dialog** là phía client.

### 3.1 Row – entity

```csharp
[ConnectionKey("Northwind"), Module("Northwind"), TableName("Categories")]
[DisplayName("Categories"), InstanceName("Category")]
[ReadPermission(PermissionKeys.General)]
[ModifyPermission(PermissionKeys.General)]
[LookupScript]
public sealed class CategoryRow : Row, IIdRow, INameRow
{
    [DisplayName("Category Id"), Identity]
    public Int32? CategoryID
    {
        get { return Fields.CategoryID[this]; }
        set { Fields.CategoryID[this] = value; }
    }

    [DisplayName("Category Name"), Size(15), NotNull, QuickSearch]
    public String CategoryName { get {...} set {...} }
    ...
    public static readonly RowFields Fields = new RowFields().Init();

    public class RowFields : RowFieldsBase
    {
        public Int32Field CategoryID;
        public StringField CategoryName;
        ...
    }
}
```

Những điểm cần nhớ:

- **Mỗi property có 2 phần.** Property C# (`CategoryName`) dùng khi đọc hoặc ghi giá trị, còn field
  (`Fields.CategoryName`) dùng khi viết query, ví dụ `fld.CategoryName == "abc"`. Ở 3.x phải viết đủ cả
  hai, getter/setter đi qua `Fields.X[this]`. Bản mới đã rút gọn chỗ này.
- **Attribute cấp class:** `ConnectionKey` cho biết dùng DB nào, `TableName` là bảng nào, `ReadPermission`
  và `ModifyPermission` là quyền cần có, `LookupScript` cho phép dùng bảng này làm dropdown (học ở bài 05).
- **Attribute cấp field:** `Identity`/`PrimaryKey`, `NotNull`, `Size`, `QuickSearch` (ô tìm nhanh trên
  grid sẽ tìm theo field này), `DisplayName` (nhãn hiển thị).
- `IIdRow` và `INameRow` cho framework biết field nào là khóa và field nào là "tên" của bản ghi. Tên
  được hiện trên tiêu đề dialog và trong dropdown.

### 3.2 Repository – nghiệp vụ

```csharp
public class CategoryRepository
{
    public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request)
        => new MySaveHandler().Process(uow, request, SaveRequestType.Create);
    ...
    private class MySaveHandler : SaveRequestHandler<MyRow, SaveRequest<MyRow>, SaveResponse> { }
    private class MyDeleteHandler : DeleteRequestHandler<MyRow> { }
    private class MyRetrieveHandler : RetrieveRequestHandler<MyRow> { }
    private class MyListHandler : ListRequestHandler<MyRow> { }
}
```

- Mỗi thao tác có một **Handler** riêng (Save, Delete, Retrieve, List). Mặc định handler rỗng mà vẫn
  chạy đủ CRUD, vì framework tự đọc attribute trên Row để biết cần làm gì.
- Khi cần thêm nghiệp vụ, bạn **override** trong handler. Ví dụ `CategoryRepository` override `AfterSave()`
  để lưu bản dịch. Các hàm hay dùng nhất là `ValidateRequest`, `SetInternalFields`, `BeforeSave`,
  `AfterSave` (trong SaveHandler) và `PrepareQuery`, `ApplyFilters` (trong ListHandler). Chúng ta sẽ học
  ở bài 06.
- `IUnitOfWork uow` là một transaction. Create, Update và Delete chạy trong transaction, còn Retrieve và
  List chỉ cần `IDbConnection`.

### 3.3 Endpoint – API

```csharp
[RoutePrefix("Services/Northwind/Category"), Route("{action}")]
[ConnectionKey(typeof(MyRow)), ServiceAuthorize(typeof(MyRow))]
public class CategoryController : ServiceEndpoint
{
    [HttpPost, AuthorizeCreate(typeof(MyRow))]
    public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request)
        => new MyRepository().Create(uow, request);
    ...
    public ListResponse<MyRow> List(IDbConnection connection, ListRequest request)
        => new MyRepository().List(connection, request);
}
```

- Đây là một controller MVC. URL của nó là `/Services/Northwind/Category/List`, `/Create`...
- Endpoint **không chứa nghiệp vụ**, nó chỉ kiểm tra quyền rồi gọi Repository.
- Framework tự mở connection hoặc transaction rồi truyền vào tham số `uow` hay `connection`.
- File này nằm trong namespace `...Endpoints`. Có một file khác cũng tên `CategoryController` nhưng nằm
  trong namespace `...Pages` (mục 3.5), nên hai cái không đụng nhau.

### 3.4 Form và Columns – khai báo UI bằng C#

```csharp
[FormScript("Northwind.Category")]
[BasedOnRow(typeof(Entities.CategoryRow), CheckNames = true)]
public class CategoryForm
{
    public String CategoryName { get; set; }
    public String Description { get; set; }
}

[ColumnsScript("Northwind.Category")]
[BasedOnRow(typeof(Entities.CategoryRow), CheckNames = true)]
public class CategoryColumns
{
    [EditLink, DisplayName("Db.Shared.RecordId"), AlignRight]
    public Int32 CategoryID { get; set; }
    [EditLink, Width(250)]
    public String CategoryName { get; set; }
    [Width(450)]
    public String Description { get; set; }
}
```

- **Thứ tự property chính là thứ tự field trên form và thứ tự cột trên grid.**
- `BasedOnRow` lấy sẵn nhãn, kiểu dữ liệu và validation từ Row, nên ở đây chỉ cần khai báo thêm phần khác
  biệt như `Width`, `EditLink` (bấm vào thì mở dialog), `Hidden`, `Category("Tab A")`...
- `CheckNames = true` sẽ báo lỗi nếu bạn gõ sai tên property so với Row.
- Muốn bỏ một cột hay một field khỏi UI thì chỉ cần xóa property khỏi file này, không cần đụng tới TypeScript.

### 3.5 Page và View – trang danh sách

```csharp
[RoutePrefix("Northwind/Category"), Route("{action=index}")]
[PageAuthorize(typeof(Entities.CategoryRow))]
public class CategoryController : Controller
{
    public ActionResult Index() => View(MVC.Views.Northwind.Category.CategoryIndex);
}
```

```html
<div id="GridDiv"></div>
<script type="text/javascript">
    jQuery(function () {
        new Serene.Northwind.CategoryGrid($('#GridDiv'), {}).init();
        Q.initFullHeightGridPage($('#GridDiv'));
    });
</script>
```

Trang này chỉ tạo một `div` rồi gắn `CategoryGrid` vào đó. `MVC.Views....` là hằng số do `Imports/MVC/MVC.tt`
sinh ra.

### 3.6 Grid và Dialog – client TypeScript

```ts
namespace Serene.Northwind {
    @Serenity.Decorators.registerClass()
    export class CategoryGrid extends Serenity.EntityGrid<CategoryRow, any> {
        protected getColumnsKey() { return "Northwind.Category"; }   // ↔ [ColumnsScript]
        protected getDialogType() { return <any>CategoryDialog; }
        protected getIdProperty() { return CategoryRow.idProperty; }
        protected getLocalTextPrefix() { return CategoryRow.localTextPrefix; }
        protected getService() { return CategoryService.baseUrl; }   // ↔ Endpoint
    }

    @Serenity.Decorators.registerClass()
    export class CategoryDialog extends Serenity.EntityDialog<CategoryRow, any> {
        protected getFormKey() { return CategoryForm.formKey; }       // ↔ [FormScript]
        ...
        protected form = new CategoryForm(this.idPrefix);
    }
}
```

- `CategoryRow`, `CategoryForm` và `CategoryService` ở phía TypeScript **không do bạn viết**. Chúng được
  sinh ra trong `Imports/ServerTypings/` từ các class C#.
- **Sau khi sửa Row, Form, Columns hay Endpoint**, hãy build project rồi chạy lại T4: chuột phải vào
  `ServerTypings.tt` và chọn *Run Custom Tool*, hoặc chọn *Build → Transform All T4 Templates*. Nếu không
  chạy lại, TypeScript sẽ không thấy field mới. Đây là lỗi người mới hay gặp nhất.
- Muốn đổi hành vi UI (thêm nút, ẩn/hiện field, xử lý sự kiện) thì override trong `Grid.ts` và `Dialog.ts`.

## 4. Luồng một request

Ví dụ khi người dùng mở trang Category rồi bấm Lưu trên dialog:

```
Trình duyệt                                   Server (ASP.NET MVC 5)
───────────                                   ──────────────────────
GET /Northwind/Category  ───────────────────▶ Pages.CategoryController.Index()
                         ◀─────────────────── CategoryIndex.cshtml (div + script)
CategoryGrid.init()
  ├─ tải định nghĩa cột "Northwind.Category" ◀─ CategoryColumns (ColumnsScript)
  └─ POST /Services/Northwind/Category/List ─▶ Endpoints.CategoryController.List()
                                                 └─ CategoryRepository.List()
                                                      └─ ListRequestHandler → SQL SELECT
Bấm vào 1 dòng → CategoryDialog
  ├─ tải định nghĩa form "Northwind.Category" ◀─ CategoryForm (FormScript)
  └─ POST .../Retrieve ─────────────────────▶ RetrieveRequestHandler → SELECT 1 dòng
Bấm Lưu
  └─ POST .../Update  {Entity:{...}} ────────▶ AuthorizeUpdate → Repository.Update()
                                                 └─ SaveRequestHandler → UPDATE (trong transaction)
```

## 5. Khi cần sửa X thì mở file nào?

| Muốn... | Mở file |
|---|---|
| Thêm cột mới từ DB vào module | Migration, rồi `Row.cs`, rồi `Form.cs`/`Columns.cs`, rồi build và chạy T4 |
| Đổi độ rộng, thứ tự, ẩn cột trên grid | `Columns.cs` |
| Đổi thứ tự, nhóm tab, ẩn field trên form | `Form.cs` |
| Thêm validation hay logic khi lưu | `Repository.cs` → `MySaveHandler` |
| Lọc dữ liệu trên danh sách theo điều kiện | `Repository.cs` → `MyListHandler`, hoặc `Grid.ts` |
| Thêm nút, sự kiện, ẩn/hiện động trên UI | `Grid.ts` / `Dialog.ts` |
| Đổi quyền truy cập | Attribute trên `Row.cs` và `PermissionKeys` |
| Thêm menu bên trái | `Modules/<Module>/<Module>Navigation.cs` |

## 6. Bài tập

1. Mở project của dự án, chọn **một module bất kỳ**, rồi liệt kê các file trong thư mục đó. Với mỗi file,
   ghi 1 dòng xem nó thuộc loại nào (Row, Repository, Endpoint, Form, Columns, Page, Grid, Dialog).
   Có file nào không thuộc loại nào thì ghi lại.
2. Trong Row của module đó, tìm `ConnectionKey`, `TableName`, field có `QuickSearch`, và quyền đọc/sửa.
   Sau đó tìm connection string tương ứng trong `Web.config`.
3. Mở `Columns.cs`, đổi thứ tự 2 cột rồi build và chạy lại để xem grid thay đổi. Sau đó hoàn tác.
4. Mở DevTools (F12), tab Network, rồi mở trang danh sách của module. Tìm request `.../List` và xem
   body request (`Take`, `Skip`, `Sort`...) cùng response (`Entities`, `TotalCount`).

## 7. Ghi chú của mình

_(Viết lại những chỗ còn chưa hiểu hoặc khác biệt so với dự án thật ở đây.)_

-
