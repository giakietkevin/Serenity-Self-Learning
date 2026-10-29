# Bài 19 – Widget và vòng đời Dialog phía client

**Mục tiêu:** hiểu cây kế thừa `Widget` → `TemplatedWidget` → `TemplatedDialog` → `EntityDialog`, biết **thứ tự
các hàm được gọi** khi mở, nạp và lưu một dialog. Từ đó biết override hàm nào cho đúng, dùng template HTML riêng
và đặt một grid con vào tab của dialog.

Tham khảo (3.13.7):
- [Serenity.Scripts/CoreLib/UI/Dialogs/EntityDialog.ts](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Scripts/CoreLib/UI/Dialogs/EntityDialog.ts), thư mục [UI/Widgets/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Scripts/CoreLib/UI/Widgets)
- Serene: [Northwind/Customer/CustomerDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Customer/CustomerDialog.ts), [CustomerOrdersGrid.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Customer/CustomerOrdersGrid.ts), [Northwind.CustomerDialog.Template.html](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Customer/Northwind.CustomerDialog.Template.html), [BasicSamples/Dialogs/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Dialogs)
- Guide: `widgets/`, `tutorials/movies/listing_movies_in_person_dialog.md`

Bài 04 và phân tích 03 đã giới thiệu `afterLoadEntity`, `getToolbarButtons`, `updateInterface`. Bài này giải
thích chúng nằm ở đâu trong cả quy trình.

---

## 1. Cây kế thừa

```
Widget<TOptions>                  có this.element (jQuery), this.options, this.uniqueName
 └─ TemplatedWidget               + nạp HTML từ template, this.idPrefix, this.byId('...')
     └─ TemplatedDialog           + mở/đóng dialog hoặc panel, toolbar, validator, tabs
         ├─ PropertyDialog        + form (PropertyGrid) nhưng không gọi service. Nút OK/Cancel
         └─ EntityDialog          + form gắn với một Row: Retrieve, Create/Update, Delete, Undelete, Clone
             └─ MovieDialog       code của mình
```

Editor (`StringEditor`, `LookupEditor`...) và grid (`EntityGrid`) cũng là `Widget`. Vì vậy mọi editor đều có
`element`, `addValidationRule`, `getGridField()` (thẻ `.field` bao quanh editor, dùng để ẩn/hiện cả dòng).

| Lớp | Dùng khi |
|---|---|
| `Widget` | Viết editor, formatter có giao diện, control nhỏ |
| `TemplatedDialog` | Dialog tự do không gắn Row: hộp chọn tham số, màn hình xác nhận |
| `PropertyDialog` | Dialog có form để nhập rồi xử lý ở client, ví dụ form tham số của `ReportDialog` (bài 12) |
| `EntityDialog` | Sửa một bản ghi |

## 2. Dialog lấy HTML từ đâu

`TemplatedWidget.getTemplate()` tìm template theo **tên class**, bỏ phần root namespace (`Serene.`):

1. Thẻ `<script id="Template_MovieDB_MovieDialog">` trên trang, nếu có.
2. File `*.Template.html` trong `Modules/`. Server gom các file này lại. File
   `Modules/MovieDB/Movie/MovieDialog.Template.html` có tên `MovieDB.MovieDialog` (tự thêm tên module nếu
   thiếu).
3. Không có thì tìm tiếp theo **lớp cha**. Cuối cùng là `Views/Templates/EntityDialog.Template.html`, template
   mặc định có toolbar, form và `PropertyGrid`.

Trong template, `~_` được thay bằng `idPrefix` của dialog để id không trùng khi mở nhiều dialog cùng lúc. Trong
code thì dùng `this.byId('OrdersGrid')` thay vì `$('#...')`.

Template của `CustomerDialog` (rút gọn), thêm 2 tab so với mặc định:

```html
<div id="~_Tabs" class="s-DialogContent">
    <ul>
        <li><a href="#~_TabInfo"><span>{{text:"Db.Northwind.Customer.EntitySingular"}}</span></a></li>
        <li><a href="#~_TabOrders"><span>{{text:"Db.Northwind.Order.EntityPlural"}}</span></a></li>
    </ul>
    <div id="~_TabInfo" class="tab-pane s-TabInfo">
        <div id="~_Toolbar" class="s-DialogToolbar"></div>
        <div class="s-Form">
            <form id="~_Form" action="">
                <div class="fieldset ui-widget ui-widget-content ui-corner-all">
                    <div id="~_PropertyGrid"></div>
                    <div class="clear"></div>
                </div>
            </form>
        </div>
    </div>
    <div id="~_TabOrders" class="tab-pane s-TabOrders">
        <div id="~_OrdersGrid"></div>
    </div>
</div>
```

> Giữ nguyên các id `~_Toolbar`, `~_Form`, `~_PropertyGrid`. `EntityDialog` tìm đúng các id này để tạo toolbar
> và form. `{{text:"..."}}` là local text key (bài 09).

## 3. Vòng đời của EntityDialog

### 3.1. Khởi tạo (constructor)

`new MovieDialog()` chạy constructor của các lớp cha theo thứ tự:

1. `TemplatedWidget`: tạo element, nạp template.
2. `TemplatedDialog`: `initValidator`, `initTabs` (và tự gọi `arrange()` mỗi khi đổi tab), `initToolbar`
   (gọi **`getToolbarButtons()`**).
3. `EntityDialog`: `initPropertyGrid` (gọi **`getPropertyItems()`**, tạo các editor), `initLocalizationGrid`.

Xong bước này các editor đã tồn tại nhưng **chưa có dữ liệu**.

Vì vậy trong constructor của mình chỉ nên **gắn sự kiện**, không đọc giá trị:

```ts
constructor() {
    super();
    this.form.Kind.changeSelect2(e => this.onKindChanged());   // chỉ chạy khi user đổi
}
```

`change` chạy cả khi dialog nạp dữ liệu vào editor, còn `changeSelect2` chỉ chạy khi user chọn
(`PopulateLinkedDataDialog` có giải thích).

### 3.2. Nạp dữ liệu

Ba cách mở dialog, và grid gọi cách nào:

| Hàm | Khi nào | Có gọi Retrieve không |
|---|---|---|
| `loadNewAndOpenDialog()` | Bấm *New* không truyền gì | Không |
| `loadEntityAndOpenDialog(entity)` | `grid.editItem({ GenreId: 3 })`, tức là New có điền sẵn | Không |
| `loadByIdAndOpenDialog(id)` | Bấm vào một dòng trên grid | Có, gọi `.../Retrieve` |

Cả ba đều đi qua `loadResponse`. Code thật trong 3.13.7:

```ts
protected loadResponse(data: any): void {
    data = data || {};
    this.onLoadingData(data);          // ① có response từ server, chưa đụng vào form
    var entity = data.Entity || new Object();
    this.beforeLoadEntity(entity);     // ② sắp đưa entity vào form
    this.loadEntity(entity);           // ③ gán entityId, propertyGrid.load(entity) → editor có giá trị
    this.set_entity(entity);
    this.afterLoadEntity();            // ④ gọi updateInterface() rồi updateTitle()
}
```

Sau đó dialog mới được mở (`dialogOpen` → `onDialogOpen` → `arrange`).

| Muốn làm | Override |
|---|---|
| Sửa dữ liệu trả về trước khi hiện | `onLoadingData(data)` hoặc `beforeLoadEntity(entity)` |
| Truyền giá trị cho grid con, bật/tắt tab theo bản ghi | `loadEntity(entity)` (gọi `super` trước) |
| Logic cần giá trị đã có trên form, ví dụ điền số tự động | `afterLoadEntity()` |
| Ẩn/hiện, bật/tắt nút và field | `updateInterface()` |
| Đổi tiêu đề | `getEntityTitle()` hoặc `updateTitle()` |
| Đổi request Retrieve, ví dụ thêm `IncludeColumns` | `getLoadByIdRequest(id)` |

`updateInterface()` còn được gọi lại khi trạng thái thay đổi (bấm *Edit* ở chế độ xem, sau Undelete...), nên
đặt mọi logic ẩn/hiện ở đây thay vì rải trong nhiều hàm.

### 3.3. Lưu

```
Bấm Save
 └─ save()
     ├─ validate form (rule của editor + addValidationRule)
     ├─ validateBeforeSave()         ← return false để chặn lưu
     └─ save_submitHandler()
         └─ getSaveOptions()
             └─ getSaveRequest()
                 └─ getSaveEntity()  ← propertyGrid.save(entity): đọc giá trị từ editor
         └─ Q.serviceCall(.../Create hoặc .../Update)
              └─ thành công: onSaveSuccess(response)
                             phát sự kiện 'ondatachange' → grid tự refresh
```

| Muốn làm | Override |
|---|---|
| Kiểm tra ở client trước khi gửi | `validateBeforeSave()` |
| Thêm hoặc sửa field gửi lên mà không có trên form | `getSaveEntity()` |
| Thêm property vào request (request riêng) | `getSaveRequest()` |
| Sau khi lưu: lấy Id vừa tạo, reload lookup, mở dialog khác | `onSaveSuccess(response)`. Id mới ở `response.EntityId` |

```ts
protected getSaveEntity() {
    let entity = super.getSaveEntity();
    entity.Title = Q.trimToNull(entity.Title);
    return entity;
}

protected onSaveSuccess(response: Serenity.SaveResponse) {
    super.onSaveSuccess(response);
    if (this.isNew())                                   // vẫn đúng: lúc này chưa nạp lại
        Q.notifySuccess('Đã tạo phim #' + response.EntityId);
    Q.reloadLookup('MovieDB.Movie');                    // bài 10
}
```

### 3.4. Các hàm kiểm tra trạng thái

| Hàm | `true` khi |
|---|---|
| `isNew()` | Chưa có `entityId` (thêm mới) |
| `isEditMode()` | Đã có `entityId` và không phải chế độ clone |
| `isCloneMode()` | Đang nhân bản từ bản ghi khác |
| `isDeleted()` | Bản ghi có cờ IsActive/IsDeleted đang ở trạng thái xóa (xóa mềm) |
| `isNewOrDeleted()` | `isNew()` hoặc `isDeleted()` |
| `readOnly` | Dialog đang khóa toàn bộ |

## 4. Thao tác với editor và field

```ts
this.form.Year.value = 2020;                                     // đọc, gán giá trị
this.form.Runtime.getGridField().toggle(this.isEditMode());      // ẩn/hiện cả dòng (nhãn + editor)
Serenity.EditorUtils.setReadOnly(this.form.Kind, true);          // khóa một editor
Serenity.EditorUtils.setRequired(this.form.ReleaseDate, true);   // bắt buộc nhập
this.deleteButton.hide();                                        // nút có sẵn: saveAndCloseButton,
                                                                 // applyChangesButton, deleteButton, cloneButton...
this.toolbar.findButton('invoice-button').toggle(this.isEditMode()); // nút tự thêm, tìm theo cssClass
```

`this.deleteButton` và `findButton` chỉ dùng được **sau** khi toolbar đã tạo, tức từ `updateInterface()` trở
đi. Trong `getToolbarButtons()` thì sửa mảng nút (thêm, `splice` để bỏ), như `ReadOnlyDialog`.

## 5. Grid con trong một tab: danh sách phim trong dialog Thể loại

Đây là bài "Listing Movies in Person Dialog" của tutorial, làm theo mẫu `CustomerOrdersGrid`.

**B1. Grid con** (`Modules/MovieDB/Genre/GenreMovieGrid.ts`):

```ts
namespace Serene.MovieDB {

    @Serenity.Decorators.registerClass()
    export class GenreMovieGrid extends MovieGrid {

        constructor(container: JQuery) {
            super(container);
        }

        protected getColumns() {                                   // bỏ cột thừa
            return super.getColumns().filter(x => x.field !== MovieRow.Fields.GenreName);
        }

        protected getInitialTitle() { return null; }               // không hiện tiêu đề trong tab

        protected getGridCanLoad() {                               // chưa có GenreId thì không gọi List
            return super.getGridCanLoad() && !!this.genreId;
        }

        protected addButtonClick() {                               // New: điền sẵn thể loại
            this.editItem(<MovieRow>{ GenreId: this.genreId });
        }

        protected initEntityDialog(itemType, dialog) {             // dialog phim mở lệch so với dialog cha
            super.initEntityDialog(itemType, dialog);
            Serenity.SubDialogHelper.cascade(dialog, this.element.closest('.ui-dialog'));
        }

        private _genreId: number;
        get genreId() { return this._genreId; }
        set genreId(value: number) {
            if (this._genreId !== value) {
                this._genreId = value;
                this.setEquality(MovieRow.Fields.GenreId, value);  // thêm "GenreId = value" vào ListRequest
                this.refresh();
            }
        }
    }
}
```

**B2. Template** `Modules/MovieDB/Genre/GenreDialog.Template.html`: chép template ở mục 2, đổi tab thứ hai thành
`~_TabMovies` chứa `<div id="~_MoviesGrid"></div>`.

**B3. Dialog** (`GenreDialog.ts`):

```ts
private moviesGrid: GenreMovieGrid;

constructor() {
    super();
    this.moviesGrid = new GenreMovieGrid(this.byId('MoviesGrid'));
    this.moviesGrid.openDialogsAsPanel = false;    // nếu MovieDialog có @panel()
}

protected loadEntity(entity: GenreRow) {
    super.loadEntity(entity);
    Serenity.TabsExtensions.setDisabled(this.tabs, 'Movies', this.isNewOrDeleted()); // chưa lưu thì khóa tab
    this.moviesGrid.genreId = entity.GenreId;
}
```

Tên tab trong `setDisabled` là phần sau `Tab` của id (`~_TabMovies` → `'Movies'`).

## 6. Dialog không gắn Row

```ts
@Serenity.Decorators.registerClass()
export class ChonNamDialog extends Serenity.TemplatedDialog<any> {
    constructor() {
        super();
        this.dialogTitle = 'Chọn năm';
    }

    protected getTemplate() {                      // template viết thẳng trong code, không cần file
        return `<div class="s-Form"><input id="~_Year" type="number" class="form-control" /></div>`;
    }

    protected getDialogButtons() {
        return [{
            text: 'OK',
            click: () => {
                Q.notifyInfo('Năm: ' + this.byId('Year').val());
                this.dialogClose();
            }
        }, { text: 'Hủy', click: () => this.dialogClose() }];
    }
}

// mở: new ChonNamDialog().dialogOpen();
```

Decorator thường gặp trên dialog: `@Serenity.Decorators.panel()` (mở dạng panel chiếm cả trang),
`@Serenity.Decorators.responsive()`, `@Serenity.Decorators.maximizable()`.

## 7. Bài tập

1. Thêm `console.log` vào constructor, `onLoadingData`, `beforeLoadEntity`, `loadEntity`, `afterLoadEntity`,
   `updateInterface`, `onDialogOpen`, `validateBeforeSave`, `onSaveSuccess` của `MovieDialog`. Mở dialog bằng
   *New*, bằng click một dòng, rồi lưu. Ghi lại thứ tự in ra.
2. Trong `MovieDialog`, ẩn `Runtime` khi `Kind` là TV Series. Phải xử lý ở cả `afterLoadEntity` (lúc mở) và sự
   kiện thay đổi của `Kind`.
3. Làm tab "Phim" trong `GenreDialog` theo mục 5.
4. Viết `ChonNamDialog` và mở nó từ một nút trên `MovieGrid`. Khi bấm OK thì lọc grid theo năm đã chọn.
5. Ở dự án thật: tìm các file `*.Template.html`. Dialog nào có nhiều tab, tab đó chứa gì?

## 8. Ghi chú của mình

-
