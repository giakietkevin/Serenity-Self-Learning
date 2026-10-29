# Bài 17 – Row chuyên sâu: Field, FieldFlags, theo dõi gán giá trị và SelectLevel

**Mục tiêu:** hiểu bên trong một Row: Field là gì, các cờ `FieldFlags` đến từ đâu và ảnh hưởng tới lưu,
đọc dữ liệu ra sao. Biết vì sao gửi thiếu field lên `Update` không làm mất dữ liệu, vì sao có field không bao
giờ được trả về client, và chọn đúng attribute cho từng trường hợp.

Tham khảo (3.13.7):
- [Serenity.Data.Entity/Row/Row.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data.Entity/Row/Row.cs), [JsonRowConverter.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data.Entity/Row/JsonRowConverter.cs)
- [Serenity.Data/Mapping/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data/Mapping): `FieldFlags.cs`, `SelectLevel.cs` và các attribute
- [SaveRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers/Save/SaveRequestHandler.cs) (`GetEditableFields`, `ValidateEditableFields`, `ClearNonTableAssignments`)
- Guide: `entities/entities_row.md`, `entities/mapping_attributes.md`, `entities/fieldflags_enumeration.md`

Phân tích 01 (`ProductRow`) đã nói về `ForeignKey`, `LeftJoin`, `Origin`. Bài này nói phần "bên dưới".

---

## 1. Row và Field

```csharp
public sealed class MovieRow : Row, IIdRow, INameRow
{
    public String Title
    {
        get { return Fields.Title[this]; }       // đọc giá trị của dòng này qua Field
        set { Fields.Title[this] = value; }
    }

    public static readonly RowFields Fields = new RowFields().Init();

    public class RowFields : RowFieldsBase
    {
        public StringField Title;                  // một Field = metadata của một cột
        // ...
    }
}
```

- **`Fields` là static**, tạo một lần cho mỗi loại Row. Mỗi `Field` giữ metadata: tên, `Expression` (SQL để
  đọc cột), `Size`, `Flags`, join, quyền...
- **Mỗi instance Row** chỉ giữ giá trị. Property `Title` chỉ là cách viết tiện của `Fields.Title[row]`.
- Vì vậy `fld.Title` vừa dùng làm điều kiện SQL (`fld.Title == "x"`), vừa dùng để đọc/ghi giá trị
  (`fld.Title[row] = "x"`, cách behavior ở bài 14 làm).

Các loại Field thường gặp:

| Field | Kiểu C# | Ghi chú |
|---|---|---|
| `StringField` | `String` | |
| `Int16Field`, `Int32Field`, `Int64Field` | `Int16?`, `Int32?`, `Int64?` | Enum cũng dùng `Int32Field` (bài 05) |
| `DecimalField`, `DoubleField`, `SingleField` | `Decimal?`... | Số tiền dùng `Decimal` + `[Scale(2)]` |
| `BooleanField` | `Boolean?` | |
| `DateTimeField`, `DateTimeOffsetField`, `TimeSpanField` | | `[DateTimeKind(...)]` chỉ định giờ địa phương hay UTC |
| `GuidField`, `ByteArrayField` | | |
| `ListField<T>` | `List<T>` | Ví dụ `GenreList` (bài 08), luôn là `NotMapped` |
| `RowListField<TRow>` | `List<TRow>` | Bảng con master–detail (bài 08) |
| `CustomClassField<T>`, `JsonField<T>` | | Lưu một object, thường kèm xử lý riêng |

## 2. FieldFlags

Mỗi Field có một tập cờ `FieldFlags`. Bản 3.13.7:

| Cờ | Nghĩa | Được bật bởi |
|---|---|---|
| `Insertable` | Được gán khi Create | Mặc định. Tắt bằng `[Insertable(false)]` |
| `Updatable` | Được gán khi Update | Mặc định. Tắt bằng `[Updatable(false)]` |
| `Trim` / `TrimToEmpty` | Tự cắt khoảng trắng chuỗi / đổi null thành chuỗi rỗng | `Trim` mặc định |
| `NotNull` | Bắt buộc có giá trị | `[NotNull]` (còn `[Required]` chỉ bắt nhập trên form, không đổi cờ) |
| `PrimaryKey`, `AutoIncrement` | Khóa chính, tự tăng | `[PrimaryKey]`, `[AutoIncrement]`, hoặc `[Identity]` (gồm cả hai và `NotNull`) |
| `Foreign` | Lấy từ bảng join | **Tự động** khi `Expression` dùng alias join, ví dụ `g.Name` |
| `Calculated` | Biểu thức tính toán | **Tự động** khi `Expression` không phải một cột đơn, ví dụ `T0.A + T0.B` |
| `NotMapped` | Không có cột trong DB | `[NotMapped]` |
| `Reflective` | Field phản chiếu field khác | Ít dùng |
| `DenyFiltering` | Không cho lọc theo field này từ client | `[SetFieldFlags(FieldFlags.DenyFiltering)]` |
| `Unique` | Không trùng giá trị | `[Unique]` (bài 14) |

Tổ hợp hay gặp: `Default = Insertable | Updatable | Trim`, `Required = Default | NotNull`,
`Identity = PrimaryKey | AutoIncrement | NotNull`.

Muốn bật/tắt cờ trực tiếp: `[SetFieldFlags(add, remove)]`, ví dụ
`[SetFieldFlags(FieldFlags.Updatable, FieldFlags.None)]`.

### Table field và view field

Một field là **table field** khi nó là cột của chính bảng: không `Foreign`, không `Calculated`, không
`NotMapped`. Những field còn lại là **view field**:

- Được đọc ra (`SELECT`) nhưng **không bao giờ được ghi**. `SaveRequestHandler.ClearNonTableAssignments()` bỏ
  chúng khỏi lệnh `INSERT`/`UPDATE`.
- `SelectTableFields()` (dùng trong `connection.TryById<Row>(id, q => q.SelectTableFields())`) chỉ lấy table field.

Ví dụ field tính toán cho Movie:

```csharp
[DisplayName("Title (Year)"), Expression("CONCAT(T0.[Title], ' (', T0.[Year], ')')")]
public String TitleWithYear
{
    get { return Fields.TitleWithYear[this]; }
    set { Fields.TitleWithYear[this] = value; }
}
```

`Expression` này chứa hàm và nhiều cột, nên Serenity tự bật cờ `Calculated`: hiện được trên grid, lọc được,
nhưng không lưu.

## 3. Row nhớ field nào đã được gán

Đây là cơ chế quan trọng nhất của bài.

```csharp
var row = new MovieRow();
row.TrackAssignments = true;      // bật chế độ theo dõi
row.Title = "Inception";

row.IsAssigned(MovieRow.Fields.Title);   // true
row.IsAssigned(MovieRow.Fields.Year);    // false: chưa gán, khác với "gán null"
row.ClearAssignment(MovieRow.Fields.Title);
```

Khi request JSON tới server, `JsonRowConverter` tạo Row với `TrackAssignments = true` rồi gán **chỉ những
property có trong JSON**. Từ đó:

1. **Update từng phần.** `SaveRequestHandler` khi Update chỉ ghi các field đã được gán. Client gửi
   `{ EntityId: 5, Entity: { Year: 2011 } }` thì chỉ cột `Year` bị đổi, `Title` và các cột khác giữ nguyên.
   `ProductGrid.ts` của Serene dựa vào điều này để sửa ngay trên grid: `pendingChanges` chỉ chứa các field user
   vừa sửa, và chỉ những field đó được gửi lên:

   ```ts
   var entity = Q.deepClone(self.pendingChanges[key]);   // ví dụ { UnitPrice: 20, UnitsInStock: 5 }
   entity.ProductID = key;
   Q.serviceRequest('Northwind/Product/Update', {
       EntityId: key,
       Entity: entity
   }, response => { ... });
   ```

2. **Kiểm tra bắt buộc khi Update** chỉ áp dụng cho field được gán (`ValidateRequiredIfModified`). Khi Create thì
   mọi field `NotNull` đều phải có.
3. **Field không được sửa:** nếu client gửi giá trị khác null cho một table field không `Insertable` (khi Create)
   hoặc không `Updatable` (khi Update), handler ném lỗi *read only*. Gửi `null` hoặc gửi một view field thì bị bỏ
   qua. Đây là lý do `[Insertable(false), Updatable(false)]` là cách chặn client sửa `TenantId` ở bài 14.
4. **Trả JSON về:** Row đang theo dõi thì chỉ field đã gán được serialize. Row không theo dõi thì mọi field khác
   null được serialize.

Trong `SetInternalFields` hay behavior, gán `Row.InsertDate = DateTime.Now` là field đó được tính là đã gán và
sẽ được lưu. Muốn **bỏ** một giá trị client gửi lên: `Row.ClearAssignment(fld.X)`.

### Quyền theo field

`[ReadPermission]`, `[InsertPermission]`, `[UpdatePermission]`, `[ModifyPermission]` đặt trên **property** của
Row: user thiếu quyền đọc thì List/Retrieve không trả field đó, thiếu quyền ghi thì field bị coi là không sửa được
(rơi vào trường hợp 3 ở trên).

```csharp
[DisplayName("Budget"), ReadPermission("MovieDB:Finance")]
public Decimal? Budget { ... }
```

Trong `RowFieldsBase`, nếu field không khai báo quyền ghi riêng thì quyền Insert/Update **lấy theo quyền đọc**,
nên chỉ cần `ReadPermission` là user không có `MovieDB:Finance` vừa không thấy vừa không sửa được `Budget`.
Muốn áp cho mọi field của một Row thì đặt `[FieldReadPermission(...)]` ở class (trừ Id, field tên và field
`LookupInclude`).

## 4. SelectLevel: field nào được SELECT

List và Retrieve không phải lúc nào cũng lấy mọi cột. Quyết định dựa vào `ColumnSelection` của request và
`[MinSelectLevel]` của field.

**`ColumnSelection` trong request:**

| Giá trị | Mặc định của | Lấy |
|---|---|---|
| `List` | `ListRequest` | Field ở mức `List` (mặc định gồm các table field) |
| `Details` | `RetrieveRequest` | Field ở mức `List` và `Details` (mặc định gồm cả view field) |
| `KeyOnly` | | Chỉ khóa chính |

Thêm vào đó, `IncludeColumns` lấy thêm các field được nêu tên, `ExcludeColumns` bỏ bớt. Grid gửi
`IncludeColumns` là các cột đang hiện (bài 13).

**`[MinSelectLevel(...)]` trên field:**

| SelectLevel | Field được lấy khi |
|---|---|
| `Auto` (mặc định) | Table field coi như `List`, view field coi như `Details` |
| `Always` | Luôn luôn, kể cả khi bị `ExcludeColumns` |
| `List` | `ColumnSelection` là `List` hoặc `Details`, hoặc có trong `IncludeColumns` |
| `Details` | `ColumnSelection` là `Details`, hoặc có trong `IncludeColumns` |
| `Explicit` | Chỉ khi có trong `IncludeColumns` |
| `Never` | Không bao giờ |

Ví dụ trong Serene:

```csharp
// UserRow: mật khẩu băm không bao giờ rời khỏi server
[DisplayName("Password Hash"), Size(86), NotNull, Insertable(false), Updatable(false),
 MinSelectLevel(SelectLevel.Never)]
public String PasswordHash { ... }

// CustomerRow: cột dài chỉ lấy khi mở dialog, không lấy cho grid
[MinSelectLevel(SelectLevel.Details)]

// OrderDetailRow: view field nhưng grid luôn cần
[Origin("p"), MinSelectLevel(SelectLevel.List)]
```

Dùng `Details` cho cột dài (mô tả, nội dung HTML) để grid nhẹ hơn, và `Never` cho dữ liệu nhạy cảm.

## 5. Bảng tra các mapping attribute

| Attribute | Đặt ở | Ý nghĩa |
|---|---|---|
| `[ConnectionKey("Default")]` | Class | Row thuộc database nào |
| `[TableName("[mov].[Movie]")]` | Class | Tên bảng |
| `[Column("tên_cột")]` | Property | Tên cột khác tên property |
| `[Expression("...")]` | Property | SQL để đọc field. Có thể có nhiều cái theo dialect |
| `[Origin("alias")]`, `[Origin("alias", "Cột")]` | Property | Lấy cột từ một join, kế thừa `DisplayName`, `Size` (phân tích 01) |
| `[ForeignKey(...)]` + `[LeftJoin("alias")]` / `[InnerJoin]` | Property | Khóa ngoại và join (bài 05) |
| `[OuterApply("alias", "SELECT ...")]` | Property hoặc class | `OUTER APPLY` với một truy vấn con |
| `[Size(n)]`, `[Scale(n)]` | Property | Độ dài chuỗi, số chữ số thập phân. Dùng cho validation và editor |
| `[NotNull]`, `[PrimaryKey]`, `[Identity]`, `[AutoIncrement]` | Property | Xem mục 2 |
| `[Insertable(false)]`, `[Updatable(false)]` | Property | Client không được gán |
| `[NotMapped]` | Property | Không có cột DB |
| `[MinSelectLevel(...)]` | Property | Mục 4 |
| `[QuickSearch]`, `[LookupInclude]`, `[NameProperty]` | Property | Tìm nhanh, đưa vào lookup, field làm tên hiển thị |
| `[TextualField("...")]` | Property | Field chữ đi kèm một khóa ngoại, dùng khi hiển thị |
| `[DateTimeKind(DateTimeKind.Utc)]` | Property | Cách hiểu giá trị ngày giờ |
| `[Unique]`, `[UniqueConstraint(...)]` | Property / class | Bài 14 |
| `[LinkingSetRelation]`, `[MasterDetailRelation]` | Property | Bài 08 |
| `[TwoLevelCached]` | Class | Bài 10 |
| `[ReadPermission]`, `[InsertPermission]`, `[UpdatePermission]`, `[DeletePermission]`, `[ModifyPermission]` | Class hoặc property | Bài 07 và mục 3 |

## 6. Bài tập

1. Thêm field `TitleWithYear` (mục 2) vào `MovieRow`, hiện trên grid. Thử gửi giá trị cho nó trong một lệnh
   `Update` bằng `Q.serviceCall` từ F12. Giá trị có được lưu không?
2. Từ F12 gửi `MovieService.Update({ EntityId: 1, Entity: { Year: 1999 } })`. Kiểm tra trong DB: `Title` có bị
   xóa không? Giải thích bằng mục 3.
3. Gắn `[Insertable(false), Updatable(false)]` cho `InsertDate`, rồi thử gửi giá trị cho nó khi Create. Đọc
   thông báo lỗi.
4. Đặt `[MinSelectLevel(SelectLevel.Details)]` cho `Storyline`. Mở F12 → *Network*, so sánh response của
   `List` và `Retrieve`.
5. Ở dự án thật: tìm các field có `MinSelectLevel`, `SetFieldFlags`, `Insertable(false)`. Vì sao chúng được
   đặt như vậy?

## 7. Ghi chú của mình

-
