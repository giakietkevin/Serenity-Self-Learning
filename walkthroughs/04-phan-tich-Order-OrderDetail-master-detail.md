# Phân tích code 04 – Order ↔ OrderDetail: master–detail từ đầu đến cuối

**File gốc (Serene 3.13.7):**
- Server: [OrderRow.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderRow.cs), [OrderDetailRow.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/OrderDetail/OrderDetailRow.cs), [OrderForm.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderForm.cs)
- Client: [OrderDetailsEditor.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/OrderDetail/OrderDetailsEditor.ts), [OrderDetailDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/OrderDetail/OrderDetailDialog.ts)
- Nền tảng: [Common/Helpers/GridEditorBase.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/GridEditorBase.ts), [GridEditorDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/GridEditorDialog.ts)
- Framework: [MasterDetailRelationBehavior.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers/IntegratedFeatures/MasterDetail/MasterDetailRelationBehavior.cs)

**Liên quan bài:** 08

Bài 08 đã giới thiệu 6 mảnh của master–detail. File này đi **sâu vào cơ chế**: grid con giữ dữ liệu trong
bộ nhớ ra sao, dữ liệu được gửi lên server thế nào, và server so sánh danh sách cũ với mới như thế nào.

---

## 1. Server – khai báo quan hệ

```csharp
// OrderRow.cs
[DisplayName("Details"), MasterDetailRelation(foreignKey: "OrderID"), NotMapped]   // ①
public List<OrderDetailRow> DetailList
{
    get { return Fields.DetailList[this]; }
    set { Fields.DetailList[this] = value; }
}
// ...
public RowListField<OrderDetailRow> DetailList;                                     // ②
```

```csharp
// OrderDetailRow.cs
[DisplayName("ID"), Identity]
public Int32? DetailID { ... }                                                      // ③

[DisplayName("Order Id"), PrimaryKey, ForeignKey(typeof(OrderRow)), LeftJoin("o"), Updatable(false)]
public Int32? OrderID { ... }                                                       // ④

[DisplayName("Product"), PrimaryKey, ForeignKey(typeof(ProductRow)), LeftJoin("p")]
[LookupEditor(typeof(ProductRow))]
public Int32? ProductID { ... }
```

| # | Giải thích |
|---|---|
| ① | `foreignKey: "OrderID"` là **tên field trong bảng con** trỏ về bảng cha. `NotMapped` vì bảng Orders không có cột DetailList. |
| ② | Kiểu field cho danh sách Row con là `RowListField<T>`. Danh sách số thì dùng `ListField<T>`. |
| ③ | Bảng con **phải có id riêng** (`IIdRow`). Server dựa vào id này để biết dòng nào thêm, sửa hay xóa. |
| ④ | `Updatable(false)`: không cho đổi đơn hàng cha của một dòng chi tiết. Behavior tự gán `OrderID` khi lưu. |

Trong `OrderDetailRow`, các view field như `ProductName` (`[Origin("p")]`) và `LineTotal` có thêm
`MinSelectLevel(SelectLevel.List)`. Nhờ vậy chúng luôn được chọn khi behavior đọc danh sách con, nên
`OrderRow` không cần khai báo `IncludeColumns`.

## 2. Server – Behavior làm gì khi Save

Code thật của `MasterDetailRelationBehavior.DetailListSave` (rút gọn):

```csharp
private void DetailListSave(IUnitOfWork uow, object masterId, IList oldList, IList newList)
{
    if (oldList.Count == 0) {                     // ⑤ đơn mới, hoặc chưa có dòng nào
        foreach (Row entity in newList)
            SaveDetail(uow, entity, masterId, null);   // INSERT hết
        return;
    }

    if (newList.Count == 0) {                     // ⑥ người dùng xóa hết dòng
        foreach (Row entity in oldList)
            DeleteDetail(uow, rowIdField.AsObject(entity));
        return;
    }

    // ⑦ Lập từ điển theo id: oldById, newById
    foreach (Row item in oldList)                 // ⑧ có trong cũ nhưng không có trong mới → DELETE
        if (!newById.ContainsKey(idStr)) DeleteDetail(uow, id);

    foreach (Row item in newList)                 // ⑨ có id và có trong cũ → UPDATE
        if (oldById.TryGetValue(idStr, out old)) SaveDetail(uow, item, masterId, id);

    foreach (Row item in newList)                 // ⑩ không có id, hoặc id lạ → INSERT
        if (string.IsNullOrEmpty(idStr) || !oldById.ContainsKey(idStr))
            SaveDetail(uow, item, masterId, null);
}
```

- `oldList` do behavior **tự đọc từ DB** (các dòng con hiện có của đơn này). `newList` là `DetailList`
  client gửi lên.
- `SaveDetail` gọi **SaveRequestHandler của OrderDetailRow**, nên mọi validation và quyền của bảng con vẫn
  được áp dụng.
- Tất cả chạy trong **cùng `IUnitOfWork`** với việc lưu Order. Lỗi ở bất kỳ dòng nào thì rollback cả đơn.
- Khi Retrieve, behavior đọc `DetailList` theo `OrderID`. Khi Delete Order, nó xóa các dòng con trước.

## 3. Client – `GridEditorBase`: grid chạy trong bộ nhớ

`GridEditorBase<T>` (trong `Modules/Common/Helpers`) kế thừa `EntityGrid` nhưng **không bao giờ gọi
service**. Những đoạn quan trọng:

```ts
@Serenity.Decorators.registerClass([Serenity.IGetEditValue, Serenity.ISetEditValue])   // ⑪
@Serenity.Decorators.editor()                                                          // ⑫
@Serenity.Decorators.element("<div/>")
export class GridEditorBase<TEntity> extends Serenity.EntityGrid<TEntity, any>
    implements Serenity.IGetEditValue, Serenity.ISetEditValue {

    protected getIdProperty() { return "__id"; }                                       // ⑬

    protected getNextId() { return "`" + this.nextId++; }

    protected save(opt, callback) {                                                    // ⑭
        var request = opt.request as Serenity.SaveRequest<TEntity>;
        var row = Q.deepClone(request.Entity);
        var id = this.id(row);
        if (id == null)
            (row as any)[this.getIdProperty()] = this.getNextId();

        if (!this.validateEntity(row, id))                                             // ⑮
            return;

        var items = this.view.getItems().slice();
        if (id == null) items.push(row);
        else {
            var index = Q.indexOf(items, x => this.id(x) === id);
            items[index] = Q.deepClone({} as TEntity, items[index], row);
        }
        this.setEntities(items);
        callback({});
    }

    public get value(): TEntity[] {                                                    // ⑯
        var p = this.getIdProperty();
        return this.view.getItems().map(x => {
            var y = Q.deepClone(x);
            var id = y[p];
            if (id && id.toString().charAt(0) == '`')
                delete y[p];
            return y;
        });
    }

    protected getGridCanLoad() { return false; }   // ⑰ không tự gọi List
    protected usePager() { return false; }
}
```

| # | Giải thích |
|---|---|
| ⑪ ⑫ | Đánh dấu class này là **một editor** (giống ô text, dropdown), có `getEditValue`/`setEditValue`. Nhờ vậy form đọc và ghi giá trị của nó như mọi field khác. |
| ⑬ | Dùng `__id` (chỉ tồn tại ở client) làm khóa trong grid, giá trị dạng `` `1 ``, `` `2 ``. Nhờ vậy dòng mới chưa có `DetailID` vẫn phân biệt được. |
| ⑭ | Khi bấm OK trên dialog con, thay vì gọi `/Services/.../Create`, nó **chỉ thêm hoặc sửa trong mảng**, rồi gọi `callback({})` để dialog con đóng như thể đã lưu thành công. |
| ⑮ | Móc (hook) cho lớp con kiểm tra và bổ sung dữ liệu. `OrderDetailsEditor` override hàm này. |
| ⑯ | Khi form lấy giá trị để gửi lên server, `__id` bị **xóa đi**. Server chỉ thấy `DetailID` thật (dòng cũ có, dòng mới không có), khớp với logic ⑨ và ⑩. |
| ⑰ | Không tự tải dữ liệu, không phân trang. Dữ liệu đến từ `setEditValue` khi dialog cha nạp Order. |

`GridEditorDialog<T>` là bản tương ứng cho dialog con: `saveHandler` và `deleteHandler` **không gọi
service** mà gọi `onSave`/`onDelete` do grid gán vào.

## 4. Client – lớp cụ thể cho Order

```ts
export class OrderDetailsEditor extends Common.GridEditorBase<OrderDetailRow> {
    protected getColumnsKey() { return "Northwind.OrderDetail"; }   // cột từ OrderDetailColumns.cs
    protected getDialogType() { return OrderDetailDialog; }

    validateEntity(row, id) {
        row.ProductID = Q.toId(row.ProductID);                         // ⑱

        var sameProduct = Q.tryFirst(this.view.getItems(), x => x.ProductID === row.ProductID);
        if (sameProduct && this.id(sameProduct) !== id) {              // ⑲
            Q.alert('This product is already in order details!');
            return false;
        }

        row.ProductName = ProductRow.getLookup().itemById[row.ProductID].ProductName;   // ⑳
        row.LineTotal = (row.Quantity || 0) * (row.UnitPrice || 0) - (row.Discount || 0);
        return true;
    }
}
```

| # | Giải thích |
|---|---|
| ⑱ | Giá trị từ dropdown có thể là chuỗi. `Q.toId` chuyển về số. |
| ⑲ | Không cho thêm trùng sản phẩm. `this.id(sameProduct) !== id` loại trừ chính dòng đang sửa. |
| ⑳ | Vì dữ liệu **chưa qua server**, các view field (`ProductName` từ join, `LineTotal` tính toán) phải được tự điền ở client để grid hiển thị đúng. Sau khi lưu và mở lại, server sẽ trả giá trị thật. |

Cuối cùng, nối editor vào form:

```csharp
// OrderForm.cs
[Category("Order Details")]
[OrderDetailsEditor]          // attribute C# do T4 sinh từ class TS (Imports/ClientTypes/...)
public List<Entities.OrderDetailRow> DetailList { get; set; }
```

## 5. Toàn cảnh một lần "Save"

```
[Client]                                                     [Server]
OrderDialog.save()
  form → Entity {OrderID: 10248, CustomerID: ..., 
                 DetailList: [ {DetailID: 1, ProductID: 11, Quantity: 12},   ← dòng cũ (có DetailID)
                               {ProductID: 42, Quantity: 10} ]}              ← dòng mới (__id đã bị xóa)
  POST /Services/Northwind/Order/Update ───────────────────▶ OrderRepository.Update
                                                              SaveRequestHandler<OrderRow>
                                                                UPDATE Orders ...
                                                              MasterDetailRelationBehavior
                                                                oldList (từ DB) = [1, 2]
                                                                newList         = [1, (mới)]
                                                                → DELETE detail 2
                                                                → UPDATE detail 1
                                                                → INSERT detail mới (OrderID = 10248)
                                                              COMMIT
```

## 6. Tự kiểm tra

1. Nếu bảng con **không có cột Identity riêng** (chỉ có khóa ghép OrderID + ProductID) thì có dùng
   `MasterDetailRelation` được không? Hãy xem lại vai trò của `rowIdField` ở mục 2.
2. Vì sao phải tự điền `ProductName` trong `validateEntity`, trong khi server đã có `Origin`/`Expression`?
3. `OrderRow.DetailList` **không khai báo** `IncludeColumns`, vậy mà `ProductName` vẫn hiện khi mở lại
   đơn. Mở `OrderDetailRow.cs`, tìm `MinSelectLevel(SelectLevel.List)` trên `ProductName` và `LineTotal`,
   rồi giải thích vì sao. (Gợi ý: behavior đọc bảng con bằng một truy vấn kiểu List, mà
   `MinSelectLevel(List)` buộc field được chọn trong mọi truy vấn List. `IncludeColumns` là cách thứ hai
   để làm điều tương tự.)
