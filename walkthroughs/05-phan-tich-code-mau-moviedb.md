# Phân tích code 05 – Bộ code mẫu MovieDB: lần theo một tính năng qua mọi file

**Code:** thư mục [samples/MovieDB](../samples/MovieDB) trong repo này

**Liên quan bài:** 02 tới 08 (đây là bản ghép tất cả lại)

Thay vì đọc từng file một, bài này **lần theo 4 tính năng** của module Movie qua mọi file mà tính năng đó
đi qua. Mục đích là để thấy các file nối với nhau thế nào.

---

## Tính năng 1: Cột `Kind` (enum) – từ DB tới dropdown

```
Migration ──▶ MovieKind.cs ──▶ MovieRow.Kind ──▶ MovieForm.Kind ──▶ (T4) ──▶ dropdown trên dialog
                                    │
                                    └────────────▶ MovieColumns.Kind [QuickFilter] ──▶ filter trên grid
```

**1. DB** (`DefaultDB_20201022_100000_MovieTables.cs`)

```csharp
.WithColumn("Kind").AsInt32().NotNullable().WithDefaultValue(1)
```

DB chỉ biết số nguyên. `WithDefaultValue(1)` giúp các bản ghi cũ (nếu thêm cột vào bảng đã có dữ liệu)
không bị NULL.

**2. Enum** (`MovieKind.cs`)

```csharp
[EnumKey("MovieDB.MovieKind")]
public enum MovieKind { [Description("Film")] Film = 1, ... }
```

`EnumKey` là tên enum phía client. **Giá trị số phải khớp với dữ liệu trong DB.**

**3. Row** (`MovieRow.cs`)

```csharp
[DisplayName("Kind"), NotNull, DefaultValue(MovieKind.Film)]
public MovieKind? Kind
{
    get { return (MovieKind?)Fields.Kind[this]; }     // Int32? → MovieKind?
    set { Fields.Kind[this] = (Int32?)value; }        // MovieKind? → Int32?
}
...
public Int32Field Kind;
```

Property có kiểu enum để code C# dễ đọc (`Row.Kind == MovieKind.Film`), còn field vẫn là `Int32Field` vì
DB lưu số. `DefaultValue(MovieKind.Film)` làm form mới có sẵn giá trị "Film".

**4. Form và Columns**

```csharp
// MovieForm.cs
[HalfWidth]
public MovieKind Kind { get; set; }        // kiểu enum → Serenity tự chọn EnumEditor (dropdown)

// MovieColumns.cs
[Width(110), QuickFilter]
public MovieKind Kind { get; set; }        // grid hiện chữ "Film" thay vì số 1, có filter phía trên
```

**5. T4.** Sau khi build và chạy T4, file `Imports/ServerTypings/MovieDB.MovieKind.ts` được sinh ra. Thiếu
bước này thì dialog **không mở được** và Console báo `Can't find ... MovieKind enum type!`.

**6. Dùng trong nghiệp vụ** (`MovieRepository.cs` → `MyListHandler`)

```csharp
if (!Authorization.HasPermission(PermissionKeys.General))
    query.Where(fld.Kind == (int)MovieKind.Film);   // so sánh trên field phải ép về int
```

**7. Dùng trên client** (`MovieDialog.ts`)

```ts
Serenity.EditorUtils.setReadOnly(this.form.Kind, this.isEditMode());   // tạo rồi thì không cho đổi loại
```

## Tính năng 2: Thể loại `GenreId` – khóa ngoại, view field, lookup

```
Migration (FK) ──▶ GenreRow [LookupScript] ──▶ MovieRow.GenreId [ForeignKey, LeftJoin("g"), LookupEditor]
                                                     │
                                                     ├──▶ MovieRow.GenreName [Expression("g.Name")]
                                                     ├──▶ MovieForm.GenreId          → dropdown + nút thêm nhanh
                                                     └──▶ MovieColumns.GenreName [QuickFilter] → cột + filter
```

**Điểm nối quan trọng:**

| Từ | Tới | Bằng gì |
|---|---|---|
| `MovieRow.GenreId` | bảng `mov.Genre` | `ForeignKey(typeof(GenreRow))` lấy tên bảng và khóa từ `GenreRow` |
| `MovieRow.GenreName` | cột `Genre.Name` | alias `"g"` trong `LeftJoin("g")` và `Expression("g.Name")` phải **giống nhau** |
| Dropdown trên form | dữ liệu Genre | `LookupEditor(typeof(GenreRow))` → `[LookupScript("MovieDB.Genre")]` |
| Nút "thêm nhanh" | `GenreDialog.ts` | quy ước tên: lookup key `MovieDB.Genre` + `Dialog` |
| Filter trên grid | `GenreId` | `QuickFilter` trên `GenreName`. Serenity tự hiểu cần lọc theo `GenreId` |

**SQL** khi mở grid có cột `GenreName`:

```sql
SELECT T0.MovieId, T0.Title, T0.Year, T0.ReleaseDate, T0.Kind, g.Name AS GenreName, T0.Runtime, T0.Description
FROM [mov].[Movie] T0
LEFT JOIN [mov].[Genre] g ON (g.GenreId = T0.GenreId)
WHERE T0.Kind = 1          -- chỉ khi user không có quyền MovieDB:General (MyListHandler)
ORDER BY T0.Year DESC      -- getDefaultSortBy() trong MovieGrid.ts
```

## Tính năng 3: Lưu một bộ phim – đi qua Repository

Người dùng nhập Title = " Inception ", Year = 2010, ReleaseDate = 16/07/2010 rồi bấm Save:

```
MovieDialog.ts
  ├─ client validation: Runtime trong khoảng 1..1000 (addValidationRule)
  └─ POST /Services/MovieDB/Movie/Create  {Entity: {Title: " Inception ", Year: 2010, ...}}

MovieEndpoint.cs  [AuthorizeCreate(typeof(MyRow))] → kiểm tra quyền MovieDB:Movie:Modify
  └─ MovieRepository.Create → MySaveHandler.Process(uow, request, Create)

MySaveHandler
  ① ValidateRequest
       Year 2010 nằm trong [1888, năm nay + 5]                       ✓
       ReleaseDate.Year == Year                                      ✓
       Title "Inception" chưa tồn tại (Connection.Exists)            ✓
  ② SetInternalFields
       Title = "Inception"  (TrimToNull)
       InsertDate = DateTime.Now, InsertUserId = user hiện tại
  ③ ExecuteSave → INSERT INTO [mov].[Movie] ...
  ④ LinkingSetRelationBehavior → INSERT các dòng MovieGenres theo GenreList
  COMMIT → trả về {EntityId: 15}
```

Vì sao `InsertDate` và `InsertUserId` trong `MovieRow` có `Insertable(false), Updatable(false)`? Để **client
không gửi lên được** (người dùng không tự đặt ngày tạo). Gán trong `SetInternalFields` thì vẫn được, vì
code server chạy sau bước kiểm tra field editable.

Vì sao kiểm tra trùng tên lại dùng `Row.IsAssigned(fld.Title)`? Khi Update, client có thể chỉ gửi vài
field. Nếu Title không được gửi thì không cần kiểm tra.

## Tính năng 4: Chọn nhiều thể loại – `GenreList`

```csharp
[DisplayName("Genres"), NotMapped]
[LookupEditor(typeof(GenreRow), Multiple = true)]
[LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")]
public List<Int32> GenreList { ... }
public ListField<Int32> GenreList;
```

| Thành phần | Vai trò |
|---|---|
| `NotMapped` | Bảng Movie không có cột này. Thiếu nó thì sẽ gặp lỗi SQL "Invalid column name GenreList" |
| `LookupEditor(..., Multiple = true)` | Dropdown chọn nhiều, giá trị là mảng id `[1, 3]` |
| `LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")` | Bảng nối, cột trỏ về Movie, cột chứa giá trị |
| `MovieGenresRow.cs` | Chỉ cần Row. Behavior dùng nó để đọc và ghi bảng nối |

Khi lưu `GenreList = [1, 3]` cho phim 15 mà trong DB đang có `[1, 2]`, behavior sẽ: giữ 1, **xóa** dòng
(15, 2), **thêm** dòng (15, 3).

> Trong bộ mẫu có cả `GenreId` (một thể loại chính) lẫn `GenreList` (nhiều thể loại) để minh họa hai kỹ
> thuật. Dự án thật thường chỉ chọn một trong hai.

## Bảng tổng hợp: file nào phụ trách điều gì

| File | Phụ trách | Đổi khi nào |
|---|---|---|
| Migration | Cấu trúc DB | Thêm hoặc sửa cột, bảng (luôn tạo migration mới) |
| `MovieRow.cs` | Ánh xạ bảng, join, editor mặc định, quyền | Thêm field, đổi nhãn, thêm FK |
| `MovieRepository.cs` | Nghiệp vụ, validation, lọc | Thêm luật nghiệp vụ |
| `MovieEndpoint.cs` | API, quyền theo action | Thêm action mới (ví dụ `ListExcel`) |
| `MovieForm.cs` | Bố cục dialog | Đổi thứ tự, tab, editor riêng cho form |
| `MovieColumns.cs` | Cột grid | Đổi cột, độ rộng, filter |
| `MovieGrid.ts` / `MovieDialog.ts` | Hành vi giao diện | Nút, sự kiện, ẩn/hiện động |
| `MovieDBPermissionKeys.cs` | Danh sách quyền | Thêm quyền mới |
| `MovieDBNavigation.cs` | Menu | Thêm trang, đổi thứ tự |
| `site.moviedb.less` | Kích thước dialog | Dialog bị chật hoặc có thanh cuộn |

## Bài tập

1. Thêm field `Rating` (decimal 3,1, từ 0 đến 10) cho Movie và đi đủ mọi bước: migration → Row → Form →
   Columns → validation trong Repository → T4.
2. Thêm quick search theo `GenreName` vào `getQuickSearchFields()`. Có chạy không? Vì sao? (Gợi ý: phải
   gắn `[QuickSearch]` lên field đó trong Row.)
3. Viết `MovieListRequest` có thêm `MinYear`, kèm một ô nhập ở grid để lọc phim từ năm đó trở đi (làm
   theo mẫu `OrderListRequest` trong phân tích 03).
