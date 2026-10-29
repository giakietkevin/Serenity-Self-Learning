# Bài 20 – Grid nâng cao: vòng đời, lưu cài đặt, nhóm dòng, cây và sửa trên grid

**Mục tiêu:** biết grid gọi những hàm nào khi tải dữ liệu, lưu lại độ rộng cột, sắp xếp và bộ lọc của từng
user, chọn cột hiển thị, nhóm dòng kèm dòng tổng, hiện dữ liệu dạng cây, và hiểu cách sửa dữ liệu ngay trên grid
cùng giới hạn của nó.

Tham khảo (3.13.7):
- [Serenity.Scripts/CoreLib/UI/DataGrid/DataGrid.ts](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Scripts/CoreLib/UI/DataGrid/DataGrid.ts)
- Serene: [BasicSamples/Grids/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Grids) (`GroupingAndSummariesInGrid`, `TreeGrid`, `InitialValuesForQuickFilters`, `ViewWithoutID`, `RemovingAddButton`), [Northwind/Product/ProductGrid.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Product/ProductGrid.ts) (sửa trên grid), [Common/UserPreference/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/UserPreference)
- Guide: `grids/`, `grids/persisting_settings.md`

Các bài trước đã có: `getColumns`, `getButtons`, quick filter (bài 04, 13), formatter (bài 13), cột checkbox và
`getItemCssClass` (bài 13).

---

## 1. Vòng đời một lần tải dữ liệu

```
refresh() hoặc đổi trang, sắp xếp, bộ lọc
 └─ onViewSubmit()             ← chuẩn bị ListRequest: quick filter, quick search, sort, IncludeColumns
      return false thì không gọi service
 └─ POST .../List
 └─ onViewProcessData(response) ← nhận ListResponse, sửa Entities trước khi hiện
 └─ SlickGrid vẽ lại: getItemMetadata / getItemCssClass, formatter từng ô
```

| Hàm | Override khi |
|---|---|
| `getGridCanLoad()` | Chưa đủ điều kiện để tải, ví dụ grid con chưa có Id cha (bài 19) |
| `onViewSubmit()` | Gán thêm tham số vào request (bài 13) |
| `onViewProcessData(response)` | Thêm cột tính ở client, đánh số dòng, gán Id giả cho view không có khóa (`ViewWithoutID`) |
| `getDefaultSortBy()` | Sắp xếp mặc định, ví dụ `[MovieRow.Fields.Year + ' DESC']` |
| `usePager()` | `false` để tắt phân trang (tải hết) |
| `getViewOptions()` | Đổi số dòng mỗi trang: `opt.rowsPerPage = 50` |
| `getSlickOptions()` | Tùy chọn của SlickGrid, ví dụ `showFooterRow`, `rowHeight` |
| `createSlickGrid()` | Đăng ký plugin của SlickGrid |
| `onClick(e, row, cell)` | Bắt click trên ô (phân tích 03) |

```ts
protected getDefaultSortBy() {
    return [MovieRow.Fields.Year + ' DESC', MovieRow.Fields.Title];
}

protected getViewOptions() {
    let opt = super.getViewOptions();
    opt.rowsPerPage = 50;
    return opt;
}
```

## 2. Lưu cài đặt grid cho từng user

Grid có sẵn cơ chế lưu: độ rộng, thứ tự, ẩn/hiện cột, cột sắp xếp, bộ lọc nâng cao, quick filter, quick search.
Nhưng nó **chỉ chạy khi có nơi để lưu**. Trong 3.13.7, `DataGrid.getPersistanceStorage()` trả về
`DataGrid.defaultPersistanceStorage`, và Serene **không gán** giá trị này, nên mặc định không có gì được lưu.

Nơi lưu là bất kỳ object nào có `getItem(key)` và `setItem(key, value)`. Có hai lựa chọn có sẵn:

| Nơi lưu | Cách bật | Đặc điểm |
|---|---|---|
| `window.localStorage` | `Serenity.DataGrid.defaultPersistanceStorage = window.localStorage;` | Lưu trong trình duyệt. Đổi máy hoặc xóa dữ liệu trình duyệt là mất. Không gọi server |
| `Common.UserPreferenceStorage` | `Serenity.DataGrid.defaultPersistanceStorage = new Common.UserPreferenceStorage();` | Lưu vào bảng `UserPreferences` trong DB theo user. Đi theo user ở mọi máy. Mỗi lần đọc là một request **đồng bộ** (`async: false`) |

Đặt dòng đó trong `Modules/Common/ScriptInitialization.ts` để áp dụng cho mọi grid. Muốn chỉ một grid:

```ts
protected getPersistanceStorage() {
    return window.localStorage;
}

protected gridPersistanceFlags() {
    return { quickFilters: false, quickSearch: false };   // không nhớ bộ lọc, chỉ nhớ cột và sắp xếp
}
```

Key lưu có dạng `GridSettings:<2 đoạn đầu URL>:<tên đầy đủ của class grid>`, nên cùng một grid ở hai trang khác
nhau được lưu riêng.

> Sau khi bật, user đổi cột xong lần sau mở lại sẽ thấy đúng như cũ. Khi bạn **thêm cột mới** ở `Columns.cs`, cột
> đó có thể không hiện với user đã có cài đặt cũ. Đây là hành vi đúng, không phải lỗi.

## 3. Cho user chọn cột

```ts
protected getButtons() {
    let buttons = super.getButtons();
    buttons.push(Serenity.ColumnPickerDialog.createToolButton(this));
    return buttons;
}
```

Nút này mở `ColumnPickerDialog` để kéo thả, bật tắt cột. Kết hợp với mục 2 thì lựa chọn được nhớ lại.

## 4. Nhóm dòng và dòng tổng

Mẫu `GroupingAndSummariesInGrid`:

```ts
protected createSlickGrid() {
    var grid = super.createSlickGrid();
    grid.registerPlugin(new Slick.Data.GroupItemMetadataProvider());   // bắt buộc khi nhóm

    this.view.setSummaryOptions({
        aggregators: [
            new Slick.Aggregators.Avg(fld.UnitPrice),
            new Slick.Aggregators.Sum(fld.UnitsInStock),
            new Slick.Aggregators.Max(fld.UnitsOnOrder)
        ]
    });
    return grid;
}

protected getSlickOptions() {
    var opt = super.getSlickOptions();
    opt.showFooterRow = true;            // dòng tổng ở cuối grid
    return opt;
}

protected usePager() { return false; }    // xem lưu ý bên dưới

// nhóm theo một hoặc nhiều field
this.view.setGrouping([{
    getter: fld.CategoryName,
    formatter: x => 'Category: ' + x.value + ' (' + x.count + ' items)'
}]);
this.view.setGrouping([]);               // bỏ nhóm
```

Cột nào cần hiện tổng theo nhóm thì gán `groupTotalsFormatter` trong `getColumns()`.

> ⚠️ Nhóm và tính tổng chạy **ở trình duyệt, trên các dòng đã tải về**. Có phân trang thì tổng chỉ là tổng của
> trang hiện tại. Vì vậy mẫu tắt pager. Với bảng lớn, hãy tính tổng ở server (bài 16) và trả về qua một service
> riêng (bài 18) hoặc một property thêm trong `ListResponse`.

## 5. Dạng cây

Bảng có cột cha–con (ví dụ danh mục có `ParentId`):

```ts
private treeMixin: Serenity.TreeGridMixin<CategoryRow>;

constructor(container: JQuery) {
    super(container);
    this.treeMixin = new Serenity.TreeGridMixin({
        grid: this,
        toggleField: CategoryRow.Fields.Name,        // cột có nút mở/đóng
        getParentId: x => x.ParentId,
        initialCollapse: () => true
    });
}

protected usePager() { return false; }               // cần đủ cả cha lẫn con
```

Mẫu `DragDropInTreeGrid` có thêm kéo thả để đổi cha.

## 6. Giá trị ban đầu cho quick filter

```ts
protected getQuickFilters() {
    let filters = super.getQuickFilters();
    Q.first(filters, x => x.field == MovieRow.Fields.Kind).init = w => {
        (w as Serenity.EnumEditor).value = MovieKind.Film.toString();
    };
    return filters;
}
```

`ProductGrid` dùng cách này để đọc tham số trên URL (`?cat=3`) và lọc sẵn. Mở trang từ một link
`/Northwind/Product?cat=3` là grid đã lọc theo danh mục.

## 7. Sửa trực tiếp trên grid

`ProductGrid.ts` có một mẫu sửa giá, tồn kho ngay trên grid. Cách nó làm:

1. Formatter trả về `<input class="edit" data-field="UnitPrice" value="...">` thay vì chữ.
2. Bắt sự kiện `change` của các input đó (`this.slickContainer.on('change', '.edit:input', ...)`), kiểm tra
   giá trị, lưu vào `pendingChanges[id][field]`.
3. Nút *Save Changes* gửi từng dòng lên `Update`, mỗi dòng chỉ chứa các field đã đổi. Nhờ cơ chế theo dõi gán giá
   trị (bài 17, mục 3), các cột khác không bị ảnh hưởng.
4. `onViewProcessData` xóa `pendingChanges` mỗi khi tải lại.

Chính comment trong code Serene ghi rõ đây **chỉ là mẫu tham khảo, Serenity không hỗ trợ và không khuyến khích**:
không dùng được editor của Serenity (LookupEditor, DateEditor), không có validation như form, khó dùng trên
điện thoại. Nếu cần nhập nhiều dòng, nên dùng grid editor trong dialog (master–detail, bài 08), hoặc nhập từ Excel
(bài 12).

## 8. Các chỉnh nhỏ hay gặp

| Muốn | Cách |
|---|---|
| Bỏ nút *New* | `getButtons()` rồi `splice` nút có `cssClass == "add-button"` (`RemovingAddButton`) |
| Không mở dialog khi click dòng | Override `editItem()` và không gọi `super`, hoặc bỏ `getDialogType()` |
| Grid chỉ xem, không có Id | Gán Id giả trong `onViewProcessData` và `getIdProperty()` trả về field giả (`ViewWithoutID`) |
| Tiêu đề cột dài xuống dòng | CSS cho `.slick-header-column`, xem mẫu `WrappedHeaders` |
| Đổi chữ trên nút New | `getAddButtonCaption()` |

## 9. Bài tập

1. Bật `defaultPersistanceStorage = window.localStorage` trong `ScriptInitialization.ts`. Đổi độ rộng và thứ tự
   cột của `MovieGrid`, tải lại trang. Mở F12 → *Application* → *Local Storage* xem key `GridSettings:...`.
2. Đổi sang `new Common.UserPreferenceStorage()`, rồi đăng nhập ở trình duyệt khác. Cài đặt có theo user không?
   Xem bảng `UserPreferences` trong DB.
3. Thêm nút chọn cột (mục 3) cho `MovieGrid`.
4. Làm grid phim nhóm theo `Kind`, có dòng tổng `Runtime`. Thử bật lại pager và giải thích vì sao tổng sai.
5. Đặt sắp xếp mặc định là năm giảm dần và 50 dòng mỗi trang.
6. Ở dự án thật: grid có nhớ cài đặt của user không? Nếu có thì lưu ở đâu?

## 10. Ghi chú của mình

-
