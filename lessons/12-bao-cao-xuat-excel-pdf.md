# Bài 12 – Báo cáo, xuất Excel/PDF và nhập từ Excel

**Mục tiêu:** thêm nút xuất Excel và PDF cho grid Movie, làm một báo cáo in được (PDF) và một báo cáo chỉ có
dữ liệu (Excel). Biết cách nhập dữ liệu từ file Excel.

Tham khảo (Serene 3.13.7):
- [Modules/Common/Reporting/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Reporting): `ReportController`, `ReportHelper.ts`, `DynamicDataReport`, `ExcelReportGenerator`
- [Modules/Common/Helpers/ExcelExportHelper.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/ExcelExportHelper.ts), [PdfExportHelper.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Reporting/PdfExportHelper.ts)
- [Northwind/Order/OrderDetailReport.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderDetailReport.cs) và `.cshtml`, [Northwind/Reports/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Reports)
- [BasicSamples/Grids/ProductExcelImport/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Grids/ProductExcelImport)
- Đã phân tích một phần trong [walkthroughs/03](../walkthroughs/03-phan-tich-OrderGrid-OrderDialog.md)

---

## 1. Bốn cách "xuất" trong Serene

| Cách | Chạy ở đâu | Thư viện | Ví dụ trong Serene |
|---|---|---|---|
| Xuất grid ra **Excel** | Server | EPPlus 4.5 | Nút Excel trên grid Order, Customer |
| Xuất grid ra **PDF** | Trình duyệt | jsPDF + AutoTable | Nút PDF trên grid Order |
| **Báo cáo HTML/PDF** theo mẫu (hóa đơn...) | Server | Razor `.cshtml` + **wkhtmltopdf** | Nút Invoice trên dialog Order |
| **Báo cáo dữ liệu** ra Excel | Server | EPPlus | *Northwind → Reports → Sales By Category* |

## 2. Xuất grid ra Excel

Cần hai phần: một action `ListExcel` ở server và một nút trên grid.

**Server** (`MovieEndpoint.cs`), chép từ `OrderEndpoint.cs`:

```csharp
using Serenity.Reporting;
using Serenity.Web;
using System;

// Không cần attribute riêng: [ServiceAuthorize(typeof(MyRow))] ở cấp class đã yêu cầu ReadPermission
public FileContentResult ListExcel(IDbConnection connection, ListRequest request)
{
    var data = List(connection, request).Entities;                         // ① chạy lại List, có filter
    var report = new DynamicDataReport(data, request.IncludeColumns,       // ② cột lấy theo grid
        typeof(Columns.MovieColumns));                                     //    nhãn, định dạng lấy từ Columns
    var bytes = new ReportRepository().Render(report);                     // ③ EPPlus tạo .xlsx
    return ExcelContentResult.Create(bytes, "MovieList_" +
        DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
}
```

**Client** (`MovieGrid.ts`):

```ts
protected getButtons() {
    var buttons = super.getButtons();

    buttons.push(Common.ExcelExportHelper.createToolButton({
        grid: this,
        service: MovieService.baseUrl + '/ListExcel',
        onViewSubmit: () => this.onViewSubmit(),
        separator: true
    }));

    buttons.push(Common.PdfExportHelper.createToolButton({
        grid: this,
        onViewSubmit: () => this.onViewSubmit()
    }));

    return buttons;
}
```

`ExcelExportHelper` làm gì khi bấm nút:
1. Chép **đúng request đang dùng** của grid (bộ lọc, quick search, sắp xếp).
2. Đặt `Take = 0` để lấy **tất cả** dòng, không chỉ trang hiện tại.
3. `IncludeColumns` = các cột **đang hiển thị** trên grid. Ẩn cột nào thì file Excel không có cột đó.
4. `Q.postToService(..., target: '_blank')` để trình duyệt tải file về.

> Sau khi thêm action `ListExcel`, phải **build và chạy T4** để `MovieService` có thêm phương thức. Nếu quên,
> TypeScript vẫn biên dịch vì ở đây chỉ ghép chuỗi `baseUrl + '/ListExcel'`, nhưng nên giữ typing đồng bộ.

## 3. Báo cáo có mẫu, in ra PDF (ví dụ hóa đơn)

Gồm ba phần.

**a. Lớp báo cáo** (`OrderDetailReport.cs`):

```csharp
[Report("Northwind.OrderDetail")]                                 // ① key của báo cáo
[ReportDesign(MVC.Views.Northwind.Order.OrderDetailReport)]       // ② file .cshtml (MVC.tt sinh hằng số)
[RequiredPermission(PermissionKeys.General)]                      // ③ quyền
public class OrderDetailReport : IReport, ICustomizeHtmlToPdf
{
    public Int32 OrderID { get; set; }                            // ④ tham số, client gửi lên

    public object GetData()                                       // ⑤ lấy dữ liệu cho view
    {
        var data = new OrderDetailReportData();
        using (var connection = SqlConnections.NewFor<OrderRow>())
        {
            data.Order = connection.TryById<OrderRow>(this.OrderID, q => q
                .SelectTableFields()
                .Select(o.EmployeeFullName)
                .Select(o.ShipViaCompanyName)) ?? new OrderRow();
            data.Details = connection.List<OrderDetailRow>(q => q
                .SelectTableFields()
                .Select(od.ProductName)
                .Select(od.LineTotal)
                .Where(od.OrderID == this.OrderID));
            // ...
        }
        return data;
    }

    public void Customize(IHtmlToPdfOptions options)              // ⑥ chỉnh lề, khổ giấy PDF
    {
        // options.MarginsAll = "2cm";
    }
}
```

**b. View Razor** (`OrderDetailReport.cshtml`): HTML bình thường, `@model` là object trả về từ `GetData()`.

**c. Nút gọi báo cáo** (`OrderDialog.ts`):

```ts
buttons.push(Serene.Common.ReportHelper.createToolButton({
    title: 'Invoice',
    cssClass: 'export-pdf-button',
    reportKey: 'Northwind.OrderDetail',
    getParams: () => ({ OrderID: this.get_entityId() })   // → OrderDetailReport.OrderID
}));
```

### Báo cáo chạy thế nào

```
POST /Report/Render  { key: "Northwind.OrderDetail", ext: "pdf", opt: "{\"OrderID\":10248}" }
   │  ReportController: tìm báo cáo theo key, kiểm tra RequiredPermission
   │  JSON opt → new OrderDetailReport { OrderID = 10248 }
   ├─ ext = html → GetData() → render .cshtml → trả HTML
   └─ ext = pdf  → gọi wkhtmltopdf.exe, cho nó mở URL /Report/Render?...&print=1 (kèm cookie đăng nhập
                   và cookie ngôn ngữ) → nhận file PDF → trả về
```

> ⚠️ **Lỗi PDF hay gặp khi deploy:** wkhtmltopdf là một chương trình riêng, nó **gọi ngược HTTP vào chính
> website** để lấy HTML. Nếu server không tự gọi được địa chỉ công khai của nó (do firewall, proxy, DNS nội bộ
> hoặc HTTPS tự ký), PDF sẽ trắng hoặc báo lỗi. Cách xử lý: khai báo địa chỉ nội bộ trong `Web.config`
>
> ```xml
> <add key="EnvironmentSettings" value="{ SiteExternalUrl: 'http://localhost/TenSite/' }" />
> ```
>
> `wkhtmltopdf.exe` được chép vào `bin/` từ NuGet package `wkhtmltopdf-x86-win32`. Khi publish, kiểm tra file
> này có trong `bin` trên server.

## 4. Báo cáo chỉ có dữ liệu ra Excel (`IDataOnlyReport`)

```csharp
[Report, RequiredPermission(PermissionKeys.General)]
[Category("Northwind/Orders"), DisplayName("Sales By Category")]     // vị trí trong cây báo cáo
public class SalesByDetailReport : IReport, IDataOnlyReport
{
    public object GetData()
    {
        using (var connection = SqlConnections.NewFor<SalesByCategoryRow>())
            return connection.List<SalesByCategoryRow>();
    }

    public List<ReportColumn> GetColumnList()
    {
        return ReportColumnConverter.ObjectTypeToList(typeof(Item));     // cột Excel
    }

    [BasedOnRow(typeof(SalesByCategoryRow), CheckNames = true)]
    public class Item
    {
        public String CategoryName { get; set; }
        public String ProductName { get; set; }
        public Decimal ProductSales { get; set; }
    }
}
```

- `[Report]` không có key thì key là **tên đầy đủ của class** (`Serene.Northwind.SalesByDetailReport`).
- Báo cáo có `IDataOnlyReport` luôn xuất ra `.xlsx`.
- **Trang danh sách báo cáo** (`ReportsPage.cs`) chỉ cần gọi `new ReportRepository().GetReportTree("Northwind")`:
  mọi báo cáo có `[Category("Northwind/...")]` mà user có quyền sẽ tự hiện. Bấm vào một báo cáo, Serene mở
  `ReportDialog`: các property của lớp báo cáo hiện thành ô nhập tham số, kèm nút xem, tải PDF hoặc tải Excel
  (báo cáo `IDataOnlyReport` chỉ có nút Excel).

## 5. Nhập dữ liệu từ Excel

Mẫu *Basic Samples → Grids → Product Excel Import* làm theo trình tự:

1. Dialog có một `[FileUploadEditor]`. Người dùng chọn file `.xlsx`, file được lưu vào `temporary/` (bài 11).
2. Bấm nút Import, client gọi endpoint `ExcelImport` với tên file tạm.
3. Server đọc file bằng EPPlus, duyệt từng dòng, tra khóa ngoại theo tên, rồi gọi **chính Repository**
   để lưu, nhờ vậy validation và cache vẫn hoạt động như khi nhập tay.

```csharp
UploadHelper.CheckFileNameSecurity(request.FileName);
if (!request.FileName.StartsWith("temporary/"))
    throw new ArgumentOutOfRangeException("filename");          // chỉ cho đọc file tạm

ExcelPackage ep = new ExcelPackage();
using (var fs = new FileStream(UploadHelper.DbFilePath(request.FileName), FileMode.Open, FileAccess.Read))
    ep.Load(fs);

var worksheet = ep.Workbook.Worksheets[1];                      // EPPlus 4: sheet đánh số từ 1
for (var row = 2; row <= worksheet.Dimension.End.Row; row++)    // bỏ dòng tiêu đề
{
    var productName = Convert.ToString(worksheet.Cells[row, 1].Value ?? "");
    // ... tìm Supplier, Category theo tên, lỗi thì ghi vào response.ErrorList rồi continue
    new ProductRepository().Create(uow, new SaveRequest<MyRow> { Entity = product });
}
```

Toàn bộ chạy trong một `IUnitOfWork`, nên nếu có exception không bắt thì **cả file bị rollback**.

## 6. Bài tập

1. Thêm `ListExcel` và hai nút Excel/PDF cho grid Movie. Lọc theo Kind rồi xuất: file Excel có đúng dữ liệu
   đã lọc không? Ẩn cột Runtime rồi xuất lại.
2. Tạo báo cáo `MovieDB.MovieCard` (HTML/PDF) in thông tin một phim kèm danh sách thể loại. Thêm nút "In"
   vào `MovieDialog`.
3. Tạo báo cáo `IDataOnlyReport` "Số phim theo thể loại", `[Category("MovieDB")]`, và một trang
   `MovieDB/Reports` liệt kê báo cáo của module.
4. (Nâng cao) Viết chức năng nhập phim từ Excel theo mẫu ProductExcelImport.
5. Ở dự án thật: tìm các lớp có `[Report]`. Báo cáo PDF có chạy trên server production không? `SiteExternalUrl`
   đang đặt thế nào?

## 7. Ghi chú của mình

-
