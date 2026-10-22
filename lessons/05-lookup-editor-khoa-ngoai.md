# Bài 05 – Enum, Lookup, Editor và khóa ngoại

**Mục tiêu:** thêm field mới vào entity bằng tay, dùng enum cho danh sách cố định, dùng
**LookupScript + LookupEditor** cho danh sách lấy từ DB, và lấy dữ liệu từ bảng khác bằng
**ForeignKey + LeftJoin + Expression**.

Tham khảo:
- Guide: `tutorials/movies/adding_a_movie_kind_field.md`, `adding_movie_genres.md`
- Serene: [Northwind/Product/ProductRow.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Product/ProductRow.cs), có `SupplierID` và `CategoryID` là ví dụ chuẩn

---

## 1. Thêm field bằng tay (không dùng lại Sergen)

Một field mới cần **4 bước**. Thiếu bước nào cũng gây lỗi:

1. **Migration**: thêm cột vào DB (bài 02).
2. **Row**: thêm property **và** khai báo trong `RowFields`.
3. **Form / Columns**: thêm property nếu muốn hiện trên UI.
4. **Build → T4 → build**.

```csharp
// 1. Property
[DisplayName("Kind"), NotNull, DefaultValue(MovieKind.Film)]
public MovieKind? Kind
{
    get { return (MovieKind?)Fields.Kind[this]; }
    set { Fields.Kind[this] = (Int32?)value; }
}

// 2. Khai báo field, đừng quên chỗ này
public class RowFields : RowFieldsBase
{
    // ...
    public Int32Field Kind;
}
```

Nếu quên khai báo trong `RowFields` thì `Fields.Kind` sẽ là `null`, và app báo
`NullReferenceException` ngay khi khởi động.

## 2. Enum cho danh sách cố định

Khi danh sách giá trị **ít và không đổi**, ví dụ loại phim hay trạng thái đơn hàng, ta dùng enum:

```csharp
// Modules/MovieDB/Movie/MovieKind.cs
using Serenity.ComponentModel;
using System.ComponentModel;

namespace Serene.MovieDB
{
    [EnumKey("MovieDB.MovieKind")]
    public enum MovieKind
    {
        [Description("Film")]        Film = 1,
        [Description("TV Series")]   TvSeries = 2,
        [Description("Mini Series")] MiniSeries = 3
    }
}
```

- DB lưu kiểu `int`. Property trong Row có kiểu `MovieKind?`, còn field là `Int32Field`, nên cần ép kiểu
  như ở mục 1.
- Trên form, enum **tự động** hiện thành dropdown. Không cần thêm editor.
- `[Description]` là chữ hiển thị trên dropdown.
- **Bắt buộc chạy T4.** Nếu không, mở dialog sẽ không thấy gì xảy ra, và F12 hiện lỗi
  `Can't find Serene.MovieDB.MovieKind enum type!`

> Gặp lỗi kiểu "bấm mà không có gì xảy ra" thì **luôn mở F12 → Console trước tiên**.

## 3. Khóa ngoại: ForeignKey + LeftJoin + Expression

Giả sử `Movie` có cột `GenreId` trỏ tới bảng `mov.Genre`:

```csharp
[DisplayName("Genre"), ForeignKey("[mov].Genre", "GenreId"), LeftJoin("g")]
public Int32? GenreId
{
    get { return Fields.GenreId[this]; }
    set { Fields.GenreId[this] = value; }
}

[DisplayName("Genre"), Expression("g.Name")]
public String GenreName
{
    get { return Fields.GenreName[this]; }
    set { Fields.GenreName[this] = value; }
}

// trong RowFields
public Int32Field GenreId;
public StringField GenreName;
```

Nếu đã có `GenreRow` thì nên viết `ForeignKey(typeof(GenreRow))` thay cho chuỗi tên bảng, giống
`ProductRow` trong Serene:

```csharp
[DisplayName("Category"), ForeignKey(typeof(CategoryRow)), LeftJoin("cat")]
```

Cách hiểu: **Row trong Serenity giống một SQL view**.

- `LeftJoin("g")` nghĩa là khi cần lấy dữ liệu từ Genre, join với alias `g`.
- `Expression("g.Name")` nghĩa là `GenreName` **không phải cột của bảng Movie**, nó được tính từ join.
  Field kiểu này gọi là **view field**. Nó chỉ dùng để đọc, khi lưu sẽ bị bỏ qua.
- Join **chỉ được thực hiện khi cần**, tức là khi grid có hiện cột `GenreName`:

```sql
SELECT T0.MovieId, T0.Title, T0.GenreId, g.Name AS GenreName
FROM mov.Movie T0
LEFT JOIN mov.Genre g ON (g.GenreId = T0.GenreId)
```

Trên grid nên hiện `GenreName`, còn trên form thì dùng `GenreId` để chọn.

## 4. LookupScript: đưa danh sách từ DB xuống client

Muốn `GenreId` hiện dropdown thì client phải có danh sách Genre. Ta đánh dấu `GenreRow` bằng
`[LookupScript]`:

```csharp
[ConnectionKey("Default"), Module("MovieDB"), TableName("[mov].[Genre]")]
[DisplayName("Genres"), InstanceName("Genre")]
[ReadPermission("Administration:General")]
[ModifyPermission("Administration:General")]
[LookupScript("MovieDB.Genre")]
public sealed class GenreRow : Row, IIdRow, INameRow
```

- Lookup script là một **file JS động, được cache** và dùng chung cho mọi user. Nó không nhận tham số,
  nên đừng dùng nó như một web service.
- Chỉ dùng cho bảng **nhỏ, ít thay đổi** (danh mục, loại, khu vực...). Bảng lớn hàng chục nghìn dòng thì
  dùng `ServiceLookupEditor` hoặc `AsyncLookupEditor`.
- Muốn kiểm tra thì sau khi đăng nhập, mở F12 Console và gõ `Q.getLookup('MovieDB.Genre')`.
- `[LookupScript]` không có tham số thì key mặc định là `<Module>.<Tên Row bỏ chữ Row>`, ví dụ `Northwind.Category`.
- Muốn đưa thêm cột vào lookup (để dùng ở client) thì gắn `[LookupInclude]` lên field, như `UnitPrice`
  trong `ProductRow`.
- Lookup script được tự làm mới khi bảng gốc thay đổi qua Serenity. Nếu bạn sửa DB trực tiếp bằng SQL thì
  cache có thể chưa cập nhật.

## 5. LookupEditor: dropdown trên form

```csharp
[DisplayName("Genre"), ForeignKey(typeof(GenreRow)), LeftJoin("g")]
[LookupEditor(typeof(GenreRow), InplaceAdd = true)]
public Int32? GenreId { ... }
```

- `LookupEditor(typeof(GenreRow))` đọc key từ `[LookupScript]` của `GenreRow`. Nếu `GenreRow` không có
  `[LookupScript]` thì app **báo lỗi ngay khi khởi động**.
- `InplaceAdd = true` thêm nút để tạo nhanh Genre mới ngay trong dropdown. Nó tự tìm `GenreDialog` theo
  quy ước tên. Nếu tên không khớp thì chỉ định thêm `DialogType = "MovieDB.Genre"`.
- Nên đặt editor ở **Row** để dùng chung. Chỉ đặt ở Form khi editor đó dành riêng cho một form.

Các editor liên quan:

| Editor | Dùng khi |
|---|---|
| `LookupEditor(..., Multiple = true)` | Chọn nhiều giá trị (kết hợp `LinkingSetRelation`, học ở bài 08) |
| `LookupEditor(..., CascadeFrom = "CountryId", CascadeField = "CountryId")` | Dropdown phụ thuộc, ví dụ chọn Quốc gia rồi mới tới Thành phố |
| `LookupEditor(..., FilterField = "IsActive", FilterValue = 1)` | Lọc sẵn danh sách |
| `CheckLookupEditor` | Danh sách checkbox |
| `ServiceLookupEditor` | Lấy dữ liệu qua service (bảng lớn, có phân trang) |

## 6. Hiện trên grid và lọc nhanh

```csharp
// MovieColumns.cs
[Width(100), QuickFilter]
public String GenreName { get; set; }

[Width(100), QuickFilter]
public MovieKind Kind { get; set; }
```

`QuickFilter` trên `GenreName` sẽ **tự lọc theo `GenreId`** và dùng lookup editor của `GenreId`, vì
Serenity hiểu được quan hệ giữa hai field này.

## 7. Bài tập

1. Thêm cột `Kind` (bài 02), tạo enum `MovieKind`, thêm vào Row, Form và Columns. Thử **quên** chạy T4 để
   tự thấy lỗi trong Console, sau đó sửa.
2. Thêm `GenreId` vào `Movie` có khóa ngoại tới `Genre`, gắn `[LookupScript]` cho `GenreRow`, rồi dùng
   `LookupEditor(typeof(GenreRow), InplaceAdd = true)`.
3. Hiện `GenreName` trên grid có `QuickFilter`. Mở F12 → Network, xem request `List` gửi điều kiện lọc gì.
4. Ở dự án thật: tìm 3 field có `ForeignKey` + `LeftJoin`. Với mỗi field, viết ra câu SQL mà Serenity sẽ
   sinh ra.

## 8. Ghi chú của mình

-
