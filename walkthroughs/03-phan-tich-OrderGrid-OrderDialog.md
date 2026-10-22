# Phân tích code 03 – `OrderGrid.ts` và `OrderDialog.ts`: tùy biến giao diện bằng TypeScript

**File gốc (Serene 3.13.7):**
- [Northwind/Order/OrderGrid.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderGrid.ts)
- [Northwind/Order/OrderDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderDialog.ts)
- Phía server liên quan: [OrderListRequest.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderListRequest.cs), [OrderRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderRepository.cs)

**Liên quan bài:** 04 (tùy biến UI), 06 (List handler có tham số)

Grid đơn hàng là ví dụ tốt nhất về **override** trong `EntityGrid`: thêm bộ lọc có tham số riêng gửi lên
server, thêm nút Excel và PDF, thêm cột icon có xử lý click, và điền sẵn dữ liệu khi bấm "New".

> Quy tắc chung: muốn đổi hành vi thì **override phương thức `protected`** của lớp cha, gọi `super.xxx()`,
> rồi chỉnh kết quả.

---

## 1. Khai báo lớp

```ts
namespace Serene.Northwind {

    import fld = OrderRow.Fields;                       // ①

    @Serenity.Decorators.registerClass()                // ②
    @Serenity.Decorators.filterable()                   // ③
    export class OrderGrid extends Serenity.EntityGrid<OrderRow, any> {
        protected getColumnsKey() { return "Northwind.Order"; }
        protected getDialogType() { return <any>OrderDialog; }
        protected getIdProperty() { return OrderRow.idProperty; }
        protected getLocalTextPrefix() { return OrderRow.localTextPrefix; }
        protected getService() { return OrderService.baseUrl; }

        protected shippingStateFilter: Serenity.EnumEditor;   // ④
```

| # | Giải thích |
|---|---|
| ① | Đặt alias `fld` cho `OrderRow.Fields` để viết `fld.ShippingState` thay vì chuỗi `"ShippingState"`. |
| ② | **Bắt buộc** với mọi widget. Nó đăng ký class để Serenity tìm được theo tên, ví dụ khi `LookupEditor` tìm dialog. |
| ③ | Bật **bộ lọc nâng cao** (nút filter, cho phép lọc theo nhiều điều kiện). |
| ④ | Giữ tham chiếu tới editor của quick filter "Shipping State" để điều khiển từ bên ngoài (xem ⑪). |

## 2. Quick filter tùy biến, gửi tham số riêng lên server

```ts
protected getQuickFilters() {
    var filters = super.getQuickFilters();               // ⑤ các filter từ [QuickFilter] trong Columns

    filters.push({
        type: Serenity.LookupEditor,                     // ⑥ editor dùng cho filter
        options: {
            lookupKey: ProductRow.lookupKey
        },
        field: 'ProductID',
        title: 'Contains Product in Details',
        handler: w => {                                  // ⑦
            (this.view.params as OrderListRequest).ProductID = Q.toId(w.value);
        },
        cssClass: 'hidden-xs'                            // ⑧
    });

    return filters;
}
```

| # | Giải thích |
|---|---|
| ⑤ | Lấy danh sách filter mặc định, tức là các field có `[QuickFilter]` trong `OrderColumns.cs`, rồi thêm filter mới vào. |
| ⑥ | Dropdown chọn sản phẩm, lấy từ lookup `Northwind.Product`. |
| ⑦ | **Điểm then chốt.** `ProductID` **không phải cột của bảng Orders**, nên không dùng được bộ lọc mặc định. Vì vậy `handler` tự đặt giá trị vào `view.params`, là object request sẽ gửi lên `List`. Kiểu `OrderListRequest` (TS) do T4 sinh từ `OrderListRequest.cs`. |
| ⑧ | Ẩn filter này trên màn hình điện thoại (class Bootstrap). |

**Phía server** nhận và xử lý `ProductID`:

```csharp
public class OrderListRequest : ListRequest { public int? ProductID { get; set; } }

private class MyListHandler : ListRequestHandler<MyRow, OrderListRequest>
{
    protected override void ApplyFilters(SqlQuery query)
    {
        base.ApplyFilters(query);
        if (Request.ProductID != null)
        {
            var od = Entities.OrderDetailRow.Fields.As("od");
            query.Where(Criteria.Exists(query.SubQuery()
                .Select("1").From(od)
                .Where(od.OrderID == fld.OrderID & od.ProductID == Request.ProductID.Value)
                .ToString()));
        }
    }
}
```

Câu SQL tương đương là `WHERE EXISTS (SELECT 1 FROM [Order Details] od WHERE od.OrderID = T0.OrderID AND od.ProductID = @p)`.
Kết quả là lọc ra **các đơn hàng có chứa sản phẩm đó**.

> **Mẫu dùng lại được:** muốn lọc theo điều kiện phức tạp thì tạo `XxxListRequest`, đặt tham số qua
> `view.params` ở client, rồi xử lý trong `ApplyFilters` ở server.

## 3. Lấy tham chiếu tới một quick filter

```ts
protected createQuickFilters() {
    super.createQuickFilters();
    this.shippingStateFilter = this.findQuickFilter(Serenity.EnumEditor, fld.ShippingState);   // ⑨
}

public set_shippingState(value: number): void {
    this.shippingStateFilter.value = value == null ? '' : value.toString();                    // ⑩
}
```

⑨ Sau khi các filter đã được tạo, tìm editor của filter `ShippingState`.
⑩ Hàm public để code khác (một trang hoặc dialog khác nhúng `OrderGrid`) đặt sẵn bộ lọc trạng thái giao
hàng, ví dụ `grid.set_shippingState(0)` để chỉ hiện đơn chưa giao.

## 4. Thêm nút trên toolbar

```ts
protected getButtons() {
    var buttons = super.getButtons();                  // [New Order], [Refresh], [Column picker]...

    buttons.push(Common.ExcelExportHelper.createToolButton({
        grid: this,
        service: OrderService.baseUrl + '/ListExcel',  // ⑪
        onViewSubmit: () => this.onViewSubmit(),
        separator: true
    }));

    buttons.push(Common.PdfExportHelper.createToolButton({
        grid: this,
        onViewSubmit: () => this.onViewSubmit()        // ⑫
    }));

    return buttons;
}
```

⑪ Nút Excel gọi action `ListExcel` trong `OrderEndpoint.cs`, cùng điều kiện lọc với grid.
⑫ Truyền `onViewSubmit` để khi xuất file vẫn dùng **cùng bộ lọc** (gồm cả `ProductID` ở ⑦).

## 5. Thêm cột icon và xử lý click

```ts
protected getColumns() {
    var columns = super.getColumns();                  // cột từ OrderColumns.cs

    columns.splice(1, 0, {                             // ⑬ chèn vào vị trí thứ 2
        field: 'Print Invoice',
        name: '',
        format: ctx => '<a class="inline-action print-invoice" title="invoice">' +
            '<i class="fa fa-file-pdf-o text-red"></i></a>',
        width: 24, minWidth: 24, maxWidth: 24
    });

    return columns;
}

protected onClick(e: JQueryEventObject, row: number, cell: number) {
    super.onClick(e, row, cell);
    if (e.isDefaultPrevented())                        // ⑭
        return;

    var item = this.itemAt(row);                       // ⑮ dữ liệu của dòng được click
    var target = $(e.target);
    if (target.parent().hasClass('inline-action'))     // click trúng thẻ <i> thì lấy thẻ <a> cha
        target = target.parent();

    if (target.hasClass('inline-action')) {
        e.preventDefault();
        if (target.hasClass('print-invoice')) {
            Serene.Common.ReportHelper.execute({
                reportKey: 'Northwind.OrderDetail',
                params: { OrderID: item.OrderID }
            });
        }
    }
}
```

| # | Giải thích |
|---|---|
| ⑬ | Cột này **chỉ có ở client**, không có trong `OrderColumns.cs`. `format` trả về HTML của ô. Đây là formatter SlickGrid. |
| ⑭ | Lớp cha có thể đã xử lý click (ví dụ click vào EditLink để mở dialog). Nếu đã xử lý thì thôi. |
| ⑮ | `itemAt(row)` trả về object `OrderRow` của dòng đó. Có nó thì làm gì tiếp cũng được. |

Mẫu **cột hành động** này (class `inline-action` + `onClick`) dùng lại rất nhiều: nút xóa nhanh, nút
duyệt, nút in...

## 6. Điền sẵn dữ liệu khi bấm "New"

```ts
protected addButtonClick() {
    var eq = this.view.params.EqualityFilter;          // ⑯
    this.editItem(<OrderRow>{
        CustomerID: eq ? eq.CustomerID : null
    });
}
```

⑯ Khi grid đang lọc theo một khách hàng (ví dụ grid đơn hàng nằm trong tab của `CustomerDialog`), bấm
"New Order" sẽ **điền sẵn khách hàng đó** vào đơn mới.

## 7. `OrderDialog.ts`: nút trên dialog và bật/tắt theo trạng thái

```ts
@Serenity.Decorators.registerClass()
@Serenity.Decorators.panel()                           // ⑰
export class OrderDialog extends Serenity.EntityDialog<OrderRow, any> {
    // ... getFormKey, getService ...
    protected form = new OrderForm(this.idPrefix);

    getToolbarButtons() {
        var buttons = super.getToolbarButtons();       // [Save] [Apply] [Delete] ...
        buttons.push(Serene.Common.ReportHelper.createToolButton({
            title: 'Invoice',
            cssClass: 'export-pdf-button',
            reportKey: 'Northwind.OrderDetail',
            getParams: () => ({ OrderID: this.get_entityId() })   // ⑱
        }));
        return buttons;
    }

    protected updateInterface() {                      // ⑲
        super.updateInterface();
        this.toolbar.findButton('export-pdf-button').toggle(this.isEditMode());
    }
}
```

| # | Giải thích |
|---|---|
| ⑰ | `@panel()`: dialog mở dạng **panel toàn trang** thay vì cửa sổ nổi, hợp với form dài có bảng con. |
| ⑱ | `get_entityId()` trả về id của bản ghi đang mở. |
| ⑲ | `updateInterface()` được gọi mỗi khi trạng thái dialog đổi (mở mới, nạp bản ghi, sau khi lưu). Đây là chỗ đúng để **ẩn/hiện, bật/tắt** nút và field. Nút Invoice chỉ hiện khi đơn đã được lưu (`isEditMode()`). |

## 8. Bảng tra nhanh các phương thức hay override

| Lớp | Phương thức | Dùng để |
|---|---|---|
| Grid | `getButtons()` | Thêm, bớt nút trên toolbar |
| Grid | `getColumns()` | Thêm cột chỉ có ở client, đổi formatter |
| Grid | `getQuickFilters()` / `createQuickFilters()` | Thêm filter tùy biến, giữ tham chiếu filter |
| Grid | `onViewSubmit()` | Gửi thêm tham số lên `List` mỗi lần tải dữ liệu |
| Grid | `onClick()` | Xử lý click trên ô |
| Grid | `addButtonClick()` | Điền sẵn dữ liệu khi tạo mới |
| Grid | `getDefaultSortBy()` | Sắp xếp mặc định |
| Dialog | `getToolbarButtons()` | Thêm nút trên dialog |
| Dialog | `updateInterface()` | Ẩn/hiện, bật/tắt theo trạng thái |
| Dialog | `afterLoadEntity()` | Chạy logic sau khi nạp xong dữ liệu |
| Dialog | constructor + `form.X.change(...)` | Bắt sự kiện thay đổi field |

## 9. Tự kiểm tra

1. Vì sao filter "Contains Product in Details" không thể khai báo bằng `[QuickFilter]` trong `OrderColumns.cs`?
2. Bỏ dòng `onViewSubmit: () => this.onViewSubmit()` ở nút Excel thì file Excel sẽ khác gì?
3. Thêm vào `MovieGrid.ts` một cột icon "★". Click vào đó thì hiện `Q.notifySuccess(item.Title)`.
