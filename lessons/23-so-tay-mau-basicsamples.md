# Bài 23 – Sổ tay các mẫu BasicSamples còn lại

**Mục tiêu:** đi qua các mẫu trong `Modules/BasicSamples` của Serene mà các bài trước chưa dùng tới, mỗi mẫu rút
ra một kỹ thuật ngắn. Bài này dùng như một cuốn sổ tra: khi gặp yêu cầu tương tự, mở đúng mẫu ra chép.

Tham khảo (3.13.7): [Serene/Serene.Web/Modules/BasicSamples/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples).
Chạy Serene, mở menu **Basic Samples** để xem từng mẫu chạy thật; mỗi trang mẫu có link tới file nguồn.

Mẫu đã có trong bài khác: `PopulateLinkedData`, `ReadOnlyDialog` (bài 19), `SerialAutoNumber` (bài 18),
`CancellableBulkAction`, `ConditionalFormatting`, `InlineImageInGrid`, `QuickFilterCustomization` (bài 13),
`ProductExcelImport` (bài 12), `StoredProcedureGrid` (bài 16), `VSGalleryQA` (bài 10), `GroupingAndSummariesInGrid`,
`TreeGrid`, `DragDropInTreeGrid`, `InitialValuesForQuickFilters`, `RemovingAddButton`, `ViewWithoutID`,
`WrappedHeaders` (bài 20), `ChartInDialog` (bài 24).

Gần như mọi mẫu đều theo một khuôn: **kế thừa** grid hoặc dialog có sẵn của Northwind, override một hai hàm, và
grid con trỏ tới dialog con bằng `getDialogType()`:

```ts
@Serenity.Decorators.registerClass()
export class CloneableEntityGrid extends Northwind.ProductGrid {
    protected getDialogType() { return CloneableEntityDialog; }
    constructor(container: JQuery) { super(container); }
}
```

---

## 1. Dialog

### 1.1. Hộp thoại thông báo (`DialogBoxes`)

```ts
Q.alert("Có lỗi!");
Q.warning("Cẩn thận!");
Q.information("Đã xong", () => { /* sau khi bấm OK */ });

Q.confirm("Xóa phim này?",
    () => Q.notifySuccess("Bấm Yes"),          // chỉ chạy khi bấm Yes
    {
        title: 'Xác nhận',
        onNo: () => Q.notifyInfo("Bấm No"),
        onCancel: () => Q.notifyError("Đóng bằng [x]")
    });

Q.alert("<b>HTML</b> trong thông báo", { htmlEncode: false });   // mặc định nội dung bị encode
```

`Q.notifySuccess/Info/Warning/Error` là thông báo nhỏ góc màn hình (toastr), không chặn thao tác.

### 1.2. Nhân bản bản ghi (`CloneableEntityDialog`)

`EntityDialog` có sẵn nút Clone nhưng `updateInterface` mặc định ẩn nó. Bật lại và sửa dữ liệu được nhân bản:

```ts
protected updateInterface() {
    super.updateInterface();
    this.cloneButton.toggle(this.isEditMode());
}

protected getCloningEntity() {
    var clone = super.getCloningEntity();          // đã bỏ Id và cờ IsActive/IsDeleted
    clone.ProductName = clone.ProductName + ' (Clone)';
    clone.ProductImage = null;                      // không chép file ảnh
    clone.UnitsInStock = 0;
    return clone;
}
```

### 1.3. Giá trị mặc định khi tạo mới (`DefaultValuesInNewDialog`)

Override `addButtonClick()` của grid để nút New mở dialog đã điền sẵn:

```ts
protected addButtonClick() {
    this.editItem(<Northwind.OrderRow>{
        CustomerID: 'ANTON',
        RequiredDate: Q.formatDate(new Date(), 'yyyy-MM-dd'),
        EmployeeID: Northwind.EmployeeRow.getLookup().items
            .filter(x => x.FullName === 'Robert King')[0].EmployeeID
    });
}
```

Muốn tự tạo dialog thay vì `editItem`, nhớ gọi `this.initDialog(dlg)` để grid tự refresh khi dialog lưu xong:

```ts
var dlg = new Northwind.OrderDialog();
this.initDialog(dlg);
dlg.loadEntityAndOpenDialog(<Northwind.OrderRow>{ CustomerID: 'GOURL', DetailList: [ ... ] });
```

Mẫu còn cho thấy có thể điền sẵn cả **bảng con** (`DetailList`) của master–detail (bài 08).

Giá trị mặc định đơn giản hơn thì đặt ngay trong form C#: `[DefaultValue("now")]` cho ngày giờ, `[DefaultValue("today")]`
cho ngày, `[DefaultValue(1)]` cho số. Đặt ở **form**, không đặt trên Row.

### 1.4. Lấy Id vừa thêm (`GetInsertedRecordId`)

```ts
protected onSaveSuccess(response: Serenity.SaveResponse) {
    super.onSaveSuccess(response);
    if (this.isNew())
        Q.notifySuccess("Id mới: " + response.EntityId);
}
```

Ở phía server, `SaveResponse.EntityId` cũng là Id mới sau `Create` (bài 06).

### 1.5. Dialog dạng panel (`EntityDialogAsPanel`)

`@Serenity.Decorators.panel()` biến dialog thành một khung chiếm cả trang thay vì cửa sổ nổi. Hợp với form dài.
Grid có thể ép mọi dialog mở dạng panel hoặc không bằng `openDialogsAsPanel` (bài 19).

### 1.6. Dialog trên điện thoại (`ResponsiveDialog`, `MultiColumnResponsiveDialog`)

`@Serenity.Decorators.responsive()` cho dialog chiếm toàn màn hình trên thiết bị nhỏ. Mẫu nhiều cột chỉ dùng CSS
(`site.basicsamples.less`), không cần thêm TypeScript: đặt các field cạnh nhau bằng class CSS trên form
(`[HalfWidth]`, `[OneThirdWidth]`, `[QuarterWidth]` của Serenity, bài 04).

### 1.7. Form của bảng khác trong một tab (`OtherFormInTab`, `OtherFormInTabOneBar`)

Dialog đơn hàng có thêm tab "Khách hàng" để sửa thông tin khách đang chọn:

```ts
this.customerPropertyGrid = new Serenity.PropertyGrid(this.byId("CustomerPropertyGrid"), {
    idPrefix: this.idPrefix + "_Customer_",           // tránh trùng id với form chính
    items: Q.getForm(Northwind.CustomerForm.formKey).filter(x => x.name != 'CustomerID'),
    useCategories: true
});
this.customerValidator = this.byId("CustomerForm").validate(Q.validateOptions({}));
```

- Nạp dữ liệu: gọi `CustomerService.Retrieve` khi đổi khách, rồi `customerPropertyGrid.load(response.Entity)`.
- Lưu: `customerPropertyGrid.save(c)` rồi `CustomerService.Update({ EntityId, Entity: c })`.
- `OtherFormInTab` có toolbar riêng trong tab. `OtherFormInTabOneBar` dùng chung nút Save của dialog: lưu khách
  trước, xong mới lưu đơn hàng.

Đây là cách dùng `PropertyGrid` trực tiếp, không qua `EntityDialog`. Cũng dùng được khi muốn một form nhập tham số
trên trang thường.

## 2. Editor

### 2.1. Đổi chữ hiển thị trong lookup (`ChangingLookupText`)

```ts
@Serenity.Decorators.registerEditor()
export class ChangingLookupTextEditor extends Serenity.LookupEditorBase<Serenity.LookupEditorOptions, Northwind.ProductRow> {
    protected getLookupKey() { return Northwind.ProductRow.lookupKey; }

    protected getItemText(item: Northwind.ProductRow, lookup) {
        return super.getItemText(item, lookup) + ' ($' + Q.formatNumber(item.UnitPrice, '#,##0.00') + ')';
    }
}
```

Các field dùng trong `getItemText` (ở đây `UnitPrice`) phải có `[LookupInclude]` trên Row, nếu không chúng là
`undefined` ở client (bài 05). Mẫu còn có: điền `UnitPrice` khi chọn sản phẩm (`changeSelect2`) và
`addValidationRule` kiểm tra chiết khấu không vượt tổng tiền.

### 2.2. Lọc lookup theo nhiều giá trị (`LookupFilterByMultipleValues`)

Override `getItems` để chỉ hiện một phần lookup:

```ts
protected getItems(lookup: Q.Lookup<Northwind.CategoryRow>) {
    return super.getItems(lookup).filter(x =>
        x.CategoryName === 'Produce' || x.CategoryName === 'Seafood');
}
```

Grid của mẫu lọc dữ liệu tương ứng bằng `onViewSubmit` + `Serenity.Criteria` (mục 3.2).

### 2.3. Lookup trong bảng con lọc theo giá trị của form cha (`FilteredLookupInDetail`)

Form đơn hàng có ô chọn Category (`[OneWay, NotMapped]`: không lưu). Khi đổi Category, dialog sản phẩm trong bảng
con chỉ hiện sản phẩm của Category đó. Giá trị được chuyển qua ba lớp:

```
FilteredLookupInDetailDialog   form.CategoryID.change → this.form.DetailList.categoryID = ...
 └─ FilteredLookupDetailEditor  initEntityDialog(...) → dialog.categoryID = this.categoryID
     └─ FilteredLookupOrderDetailDialog
           constructor:      form.ProductID.cascadeField = ProductRow.Fields.CategoryID
           beforeLoadEntity: form.ProductID.cascadeValue = this.categoryID
```

Đây là cascade (bài 05, 13) nhưng đặt giá trị bằng code thay vì `CascadeFrom`. `[OneWay]` nghĩa là field chỉ nạp
vào form, không được gửi lên khi lưu.

### 2.4. Select với giá trị viết cứng (`SelectWithHardcodedValues`)

```ts
@Serenity.Decorators.registerEditor()
export class HardcodedValuesEditor extends Serenity.Select2Editor<any, any> {
    constructor(container: JQuery) {
        super(container, null);
        this.addOption("key1", "Text 1");
        this.addItem({ id: "key4", text: "Text 4", disabled: true });
    }
}
```

Sau khi build và chạy T4, dùng trên form C# bằng `[HardcodedValuesEditor]`. Nếu danh sách là cố định và có ý
nghĩa nghiệp vụ, enum (bài 05) thường hợp hơn.

### 2.5. Chữ tĩnh trong form (`StaticTextBlock`)

Serene có editor `StaticTextBlock` (`Modules/Common/Helpers/StaticTextBlock.ts`) để chèn đoạn hướng dẫn vào giữa
form:

```csharp
[DisplayName("Lưu ý"), StaticTextBlock(Text = "Năm phát hành phải từ 1900 trở đi")]
public String Note1 { get; set; }

[StaticTextBlock(Text = "Site.Dashboard.ContentDescription", IsLocalText = true, IsHtml = true)]
public String Note2 { get; set; }

[StaticTextBlock(IsHtml = true)]                 // không có Text: hiện giá trị của chính field
public String DisplayFieldValue { get; set; }
```

### 2.6. Dialog chỉ có form, không gắn Row

`HardcodedValuesDialog` và `StaticTextBlockDialog` kế thừa `Serenity.PropertyDialog`: có form nhưng không gọi
service. Ghi đè `loadInitialEntity()` để nạp giá trị ban đầu, dùng giá trị trong nút OK của mình.

## 3. Grid

### 3.1. Link và nút trong ô (`CustomLinksInGrid`, `InlineActionButtons`)

Hai bước: formatter trả về thẻ có class riêng, rồi bắt click trong `onClick`:

```ts
protected getColumns() {
    var columns = super.getColumns();
    columns.unshift({
        field: 'Delete Row', name: '', width: 24, minWidth: 24, maxWidth: 24,
        format: ctx => '<a class="inline-action delete-row" title="delete"><i class="fa fa-trash-o text-red"></i></a>'
    });
    return columns;
}

protected onClick(e: JQueryEventObject, row: number, cell: number) {
    super.onClick(e, row, cell);
    if (e.isDefaultPrevented())
        return;

    var item = this.itemAt(row);
    var target = $(e.target);
    if (target.parent().hasClass('inline-action'))       // click trúng thẻ <i> bên trong
        target = target.parent();

    if (target.hasClass('delete-row')) {
        e.preventDefault();                              // không mở dialog như click thường
        Q.confirm('Xóa?', () => Northwind.CustomerService.Delete({ EntityId: item.ID }, () => this.refresh()));
    }
}
```

Trong formatter phải dùng `Q.htmlEncode(ctx.value)` cho dữ liệu người dùng nhập, tránh chèn HTML.

### 3.2. Grid luôn lọc theo một điều kiện (`GridFilteredByCriteria`)

```ts
protected onViewSubmit() {
    if (!super.onViewSubmit())
        return false;

    var request = this.view.params as Serenity.ListRequest;
    request.Criteria = Serenity.Criteria.and(request.Criteria,
        [['UnitsInStock'], '>', 10],
        [['CategoryName'], '!=', 'Condiments'],
        [['Discontinued'], '=', 0]);
    return true;
}
```

`[['Tên']]` là tên field, giá trị đứng sau là tham số. Đây là Criteria phía client, tương ứng Criteria C# ở bài 06.
Điều kiện này chỉ để tiện hiển thị; nếu là quy tắc **bảo mật** thì phải lọc ở server (`ApplyFilters` trong
ListHandler, bài 06, hoặc behavior, bài 14).

### 3.3. Chọn dòng và bôi đen chữ (`EnablingRowSelection`)

```ts
protected getSlickOptions() {
    var opt = super.getSlickOptions();
    opt.enableTextSelectionOnCells = true;      // cho phép bôi đen để copy
    opt.selectedCellCssClass = "slick-row-selected";
    opt.enableCellNavigation = true;
    return opt;
}

protected createSlickGrid() {
    var grid = super.createSlickGrid();
    grid.setSelectionModel(new Slick.RowSelectionModel());
    return grid;
}
```

Khác với cột checkbox (bài 13): đây là chọn dòng bằng click và phím mũi tên của SlickGrid, lấy bằng
`this.slickGrid.getSelectedRows()`.

## 4. Menu sinh từ dữ liệu (`DynamicNavigation`)

Một class cài `INavigationItemSource` được Serene tự tìm và gọi khi dựng menu:

```csharp
public class DynamicNavigationSample : INavigationItemSource
{
    public List<NavigationItemAttribute> GetItems()
    {
        var items = new List<NavigationItemAttribute>
        {
            new NavigationMenuAttribute(7970, "Basic Samples/Dynamic Navigation", "icon-paper-plane")
        };

        using (var connection = SqlConnections.NewByKey("Northwind"))
            foreach (var category in connection.List<CategoryRow>())
                items.Add(new NavigationLinkAttribute(7970,
                    path: "Basic Samples/Dynamic Navigation/" + category.CategoryName.Replace("/", "//"),
                    url: "~/Northwind/Product?cat=" + category.CategoryID,
                    permission: Northwind.PermissionKeys.General,
                    icon: "icon-folder-alt"));

        return items;
    }
}
```

- Số `7970` là thứ tự sắp xếp, giống `[NavigationLink(7970, ...)]` ở bài 07.
- Dấu `/` trong tên phải viết `//`, vì `/` là dấu tách cấp menu.
- Link `?cat=` mở grid sản phẩm đã lọc sẵn (bài 20, mục 6).
- Menu được cache theo từng user (`NavigationModel`, `TwoLevelCache` với group key của bảng `UserPermissions`,
  không có hạn), nên thêm Category mới **không** hiện ngay. Muốn cập nhật, gọi
  `TwoLevelCache.ExpireGroupItems(UserPermissionRow.Fields.GenerationKey)` sau khi lưu Category, hoặc restart app.

## 5. Bài tập

1. Thêm nút Clone cho `MovieDialog`, nhân bản thì thêm hậu tố " (bản sao)" vào `Title` và xóa ảnh.
2. Nút New của `MovieGrid` mở dialog có sẵn `Kind = Film` và `Year` là năm hiện tại.
3. Thêm cột nút xóa nhanh ở đầu `MovieGrid` theo mục 3.1.
4. Tạo menu "Phim theo thể loại" sinh từ bảng Genre bằng `INavigationItemSource`, mỗi mục mở `MovieGrid` đã lọc
   theo thể loại (cần đọc tham số URL như `ProductGrid`).
5. Thêm một `StaticTextBlock` hướng dẫn ở đầu `MovieForm`.
6. Ở dự án thật: những mẫu nào trong bài này đã được dùng? Tìm `getCloningEntity`, `addButtonClick`,
   `INavigationItemSource`, `inline-action`.

## 6. Ghi chú của mình

-
