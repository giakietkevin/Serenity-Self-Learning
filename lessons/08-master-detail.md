# Bài 08 – Master–detail và quan hệ nhiều–nhiều

**Mục tiêu:** làm được hai kiểu quan hệ hay gặp nhất trong dự án:

1. **Nhiều–nhiều** (Movie ↔ Genre): chọn nhiều giá trị trong một dropdown, dùng `LinkingSetRelation`.
2. **Master–detail** (Order → OrderDetail, Movie → Cast): bảng con được sửa ngay trong dialog cha và lưu
   cùng một lần, dùng `MasterDetailRelation` + `GridEditorBase`.

Tham khảo (ví dụ chuẩn trong Serene 3.13.7):
- [Northwind/Order/OrderRow.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderRow.cs) (`DetailList`)
- [Northwind/OrderDetail/OrderDetailsEditor.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/OrderDetail/OrderDetailsEditor.ts), [OrderDetailDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/OrderDetail/OrderDetailDialog.ts)
- [Common/Helpers/GridEditorBase.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/GridEditorBase.ts)
- Guide: `allowing_multiple_genre_selection.md`, `the_cast_and_characters_they_played.md`

---

## Phần A – Nhiều–nhiều với LinkingSetRelation

### A1. Bảng nối

```csharp
[Migration(20201022140000)]
public class DefaultDB_20201022_140000_MovieGenres : AutoReversingMigration
{
    public override void Up()
    {
        Create.Table("MovieGenres").InSchema("mov")
            .WithColumn("MovieGenreId").AsInt32().Identity().PrimaryKey().NotNullable()
            .WithColumn("MovieId").AsInt32().NotNullable()
                .ForeignKey("FK_MovieGenres_MovieId", "mov", "Movie", "MovieId")
            .WithColumn("GenreId").AsInt32().NotNullable()
                .ForeignKey("FK_MovieGenres_GenreId", "mov", "Genre", "GenreId");
    }
}
```

Dùng Sergen sinh `MovieGenresRow` với **chỉ Row** (bỏ tick Service và UI), vì ta không cần trang riêng
cho bảng này.

### A2. Field danh sách trong MovieRow

```csharp
[DisplayName("Genres"), NotMapped]
[LookupEditor(typeof(GenreRow), Multiple = true)]
[LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")]
public List<Int32> GenreList
{
    get { return Fields.GenreList[this]; }
    set { Fields.GenreList[this] = value; }
}

// trong RowFields
public ListField<Int32> GenreList;
```

- `NotMapped` cho biết bảng `Movie` **không có cột này**. Nếu thiếu, Serenity sẽ SELECT cột đó và gặp lỗi SQL.
- `LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")` nghĩa là bảng nối là
  `MovieGenres`, `MovieId` trỏ về bản ghi hiện tại, còn `GenreId` là giá trị được chọn.
- Không cần viết thêm code trong Repository. **`LinkingSetRelationBehavior`** tự đọc bảng nối khi
  Retrieve, tự insert/delete các dòng nối khi Save, và tự xóa khi Delete.
- Thêm `GenreList` vào `MovieForm`. Trên grid, có thể thêm `GenreList` vào `MovieColumns` kèm một
  formatter để hiện tên.

## Phần B – Master–detail: sửa bảng con ngay trong dialog cha

Lấy ví dụ Order và OrderDetail có sẵn trong Serene. Cấu trúc gồm 6 mảnh:

```
OrderRow.DetailList        ← [MasterDetailRelation] : server tự lưu/đọc bảng con
OrderForm.DetailList       ← [OrderDetailsEditor]   : hiển thị grid con trong form
OrderDetailsEditor.ts      ← kế thừa GridEditorBase  : grid con sửa trong bộ nhớ, chưa gọi server
OrderDetailDialog.ts       ← kế thừa GridEditorDialog: dialog sửa 1 dòng con
OrderDetailColumns.cs      ← các cột của grid con
OrderDetailForm.cs         ← các field của dialog con
```

### B1. Server: MasterDetailRelation trên Row cha

```csharp
[DisplayName("Details"), MasterDetailRelation(foreignKey: "OrderID"), NotMapped]
public List<OrderDetailRow> DetailList
{
    get { return Fields.DetailList[this]; }
    set { Fields.DetailList[this] = value; }
}

// trong RowFields
public RowListField<OrderDetailRow> DetailList;
```

- `foreignKey: "OrderID"` là tên field trong **bảng con** trỏ về bảng cha.
- **`MasterDetailRelationBehavior`** tự làm mọi việc: Retrieve thì đọc các dòng con, Save thì so sánh danh
  sách mới với danh sách cũ rồi insert/update/delete từng dòng, Delete thì xóa các dòng con. Tất cả chạy
  **trong cùng transaction** với bản ghi cha.
- Nếu grid con hiện **view field** (ví dụ `ProductName` lấy từ join), hãy khai báo
  `IncludeColumns = "ProductName"`. Nếu không, khi mở dialog cột đó sẽ trống:

```csharp
[MasterDetailRelation(foreignKey: "MovieId", IncludeColumns = "PersonFullname")]
```

Trong Row con, field khóa ngoại về bảng cha thường để `Updatable(false)`, như trong `OrderDetailRow`:

```csharp
[DisplayName("Order Id"), PrimaryKey, ForeignKey(typeof(OrderRow)), LeftJoin("o"), Updatable(false)]
public Int32? OrderID { ... }
```

### B2. Client: grid con trong form

```ts
// OrderDetailsEditor.ts
namespace Serene.Northwind {
    @Serenity.Decorators.registerClass()
    export class OrderDetailsEditor extends Common.GridEditorBase<OrderDetailRow> {
        protected getColumnsKey() { return "Northwind.OrderDetail"; }
        protected getDialogType() { return OrderDetailDialog; }
        protected getLocalTextPrefix() { return OrderDetailRow.localTextPrefix; }

        constructor(container: JQuery) {
            super(container);
        }

        // Chạy mỗi khi bấm OK trên dialog con, trước khi dòng được đưa vào grid
        validateEntity(row, id) {
            row.ProductID = Q.toId(row.ProductID);

            var sameProduct = Q.tryFirst(this.view.getItems(), x => x.ProductID === row.ProductID);
            if (sameProduct && this.id(sameProduct) !== id) {
                Q.alert('This product is already in order details!');
                return false;
            }

            // Tự điền view field cho grid hiển thị (vì chưa lưu xuống server)
            row.ProductName = ProductRow.getLookup().itemById[row.ProductID].ProductName;
            row.LineTotal = (row.Quantity || 0) * (row.UnitPrice || 0) - (row.Discount || 0);
            return true;
        }
    }
}
```

```ts
// OrderDetailDialog.ts
export class OrderDetailDialog extends Common.GridEditorDialog<OrderDetailRow> {
    protected getFormKey() { return OrderDetailForm.formKey; }
    protected getLocalTextPrefix() { return OrderDetailRow.localTextPrefix; }
    protected form: OrderDetailForm;

    constructor() {
        super();
        this.form = new OrderDetailForm(this.idPrefix);

        // Chọn sản phẩm thì tự điền đơn giá (lấy từ lookup, cần [LookupInclude] trên UnitPrice)
        this.form.ProductID.changeSelect2(e => {
            var productID = Q.toId(this.form.ProductID.value);
            if (productID != null)
                this.form.UnitPrice.value = ProductRow.getLookup().itemById[productID].UnitPrice;
        });
    }
}
```

```csharp
// OrderForm.cs
[Category("Order Details")]
[OrderDetailsEditor]
public List<Entities.OrderDetailRow> DetailList { get; set; }
```

`[OrderDetailsEditor]` là attribute C# **được T4 sinh tự động** từ class TypeScript
`OrderDetailsEditor`. File sinh ra nằm ở `Imports/ClientTypes/Northwind.OrderDetailsEditorAttribute.cs`.
Thứ tự đúng là: viết TS → build → chạy T4 → lúc đó mới dùng được attribute trong Form → build lại.

### B3. Luồng dữ liệu

```
Mở OrderDialog  ──Retrieve──▶ server trả Order + DetailList (nhờ MasterDetailRelation)
Thêm/sửa/xóa dòng con       → chỉ thay đổi trong bộ nhớ (GridEditorBase), CHƯA gọi server
Bấm Save trên OrderDialog ──Update {Entity: {..., DetailList:[...]}}──▶
        SaveHandler(Order) → UPDATE Orders
        MasterDetailRelationBehavior → so sánh DetailList cũ/mới → INSERT/UPDATE/DELETE Order Details
        (cùng 1 transaction: lỗi ở đâu thì rollback toàn bộ)
```

Các dòng mới có id tạm dạng `` `1 ``, `` `2 `` (do `GridEditorBase.getNextId()` tạo). Server biết đó là
dòng mới cần INSERT.

## Phần C – Khi nào dùng cách nào

| Tình huống | Cách |
|---|---|
| Chọn nhiều giá trị từ một danh mục, bảng nối chỉ có 2 khóa | `LinkingSetRelation` + `LookupEditor(Multiple = true)` |
| Bảng con có thêm dữ liệu riêng (số lượng, giá, vai diễn...) và lưu cùng bản ghi cha | `MasterDetailRelation` + `GridEditorBase` |
| Bảng con rất lớn, hoặc cần sửa độc lập với cha | Một grid con bình thường (`EntityGrid`) đặt trong tab của dialog cha, lọc theo id cha. Mỗi dòng được lưu riêng qua service của nó |

## Bài tập

1. Làm phần A: cho `Movie` chọn nhiều `Genre`. Kiểm tra bảng `mov.MovieGenres` sau khi lưu.
2. Đọc kỹ toàn bộ Order/OrderDetail trong Serene (6 mảnh ở phần B). Vẽ lại luồng dữ liệu bằng tay.
3. Làm master–detail `Movie → MovieCast (MovieId, PersonId, Character)`: tạo bảng `Person` và
   `MovieCast`, viết `MovieCastEditor` và `MovieCastEditDialog`, rồi dùng `IncludeColumns` để hiện tên
   diễn viên.
4. Ở dự án thật: tìm một chỗ dùng `MasterDetailRelation` hoặc `LinkingSetRelation`, rồi chỉ ra đủ các mảnh
   tương ứng.

## Ghi chú của mình

-

---

**Hết lộ trình cơ bản.** Sau bài này có thể học tiếp theo nhu cầu dự án: Localization (đa ngôn ngữ),
Multi-tenancy (tutorial `multi_tenancy/` trong Guide), upload file (`ImageUploadEditor`), Report/Excel export,
và tự viết Service Behavior.
