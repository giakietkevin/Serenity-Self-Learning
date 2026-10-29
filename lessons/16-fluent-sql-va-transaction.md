# Bài 16 – Fluent SQL, connection và transaction

**Mục tiêu:** tự viết truy vấn bằng `SqlQuery` (join, group by, phân trang), đọc kết quả theo nhiều cách, viết
`SqlInsert`/`SqlUpdate`/`SqlDelete`, và hiểu connection, `UnitOfWork` để không bị ghi dở dữ liệu hay treo
transaction.

Tham khảo (3.13.7):
- [Serenity.Data/FluentSql/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data/FluentSql): `SqlQuery.cs`, `SqlQuery_Join.cs`, `SqlInsert.cs`, `SqlUpdate.cs`
- [Serenity.Data/Connections/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data/Connections): `SqlConnections.cs`, `UnitOfWork.cs`
- [Serenity.Data.Entity/Extensions/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Data.Entity/Extensions): `EntityConnectionExtensions.cs`, `EntitySqlQueryExtensions.cs`
- Ví dụ trong Serene: [BasicSamplesEndpoint.OrdersByShipper](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/BasicSamplesEndpoint.cs), [NotesBehavior.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Note/NotesBehavior.cs), [CustomerGrossSalesRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Grids/StoredProcedureGrid/CustomerGrossSalesRepository.cs) (gọi stored procedure)
- Guide: `fluent_sql/`, `connections/connections_and_transactions.md`

Bài 06 đã có Criteria (điều kiện WHERE) và các hàm ngắn như `connection.List<MovieRow>(...)`. Bài này đi
xuống một tầng.

---

## 1. Ba tầng truy cập dữ liệu

| Tầng | Ví dụ | Dùng khi |
|---|---|---|
| Request handler (bài 06) | `new MovieRepository().List(connection, request)` | Muốn đủ phân quyền, behavior, cache, audit |
| Hàm tiện ích trên connection | `connection.List<MovieRow>(fld.Year > 2000)`, `TryById`, `Count`, `Exists`, `InsertAndGetID`, `UpdateById`, `DeleteById` | Đọc/ghi Row nhanh, không cần các thứ trên |
| `SqlQuery` tự viết | `new SqlQuery().From(fld).Select(...).GroupBy(...)` | Báo cáo, thống kê, truy vấn kết quả không phải là một Row |

> Hai tầng dưới **bỏ qua** behavior và kiểm tra quyền. Ví dụ `connection.List<MovieRow>()` không áp dụng
> `MultiTenantBehavior` (bài 14), và `UpdateById` không xóa cache (bài 10). Từ endpoint gọi ra ngoài thì nên đi
> qua Repository.

## 2. SqlQuery: viết SELECT bằng C#

```csharp
var m = MovieRow.Fields;               // alias mặc định T0
var g = GenreRow.Fields.As("g");       // alias riêng cho bảng join

var query = new SqlQuery()
    .From(m)                                         // FROM [mov].[Movie] T0
    .LeftJoin(g, g.GenreId == m.GenreId)             // LEFT JOIN [mov].[Genre] g ON (g.GenreId = T0.GenreId)
    .Select(m.MovieId)
    .Select(m.Title)
    .Select(g.Name, "GenreName")                     // đặt tên cột kết quả
    .Where(m.Year >= 2000 & m.Kind == (int)MovieKind.Film)
    .OrderBy(m.Year, desc: true)
    .OrderBy(m.Title);

string sql = query.ToString();      // xem SQL sinh ra để debug
```

Các hàm chính (đều trả về chính `SqlQuery` nên viết nối được):

| Hàm | SQL |
|---|---|
| `From(fields)` | `FROM bảng alias` |
| `LeftJoin(alias, criteria)`, `InnerJoin(...)`, `RightJoin(...)` | `... JOIN ... ON ...` |
| `Select(field)`, `Select(field, "TênCột")`, `Select("biểu thức", "TênCột")`, `Select(fld1, fld2, ...)` | Cột trong SELECT |
| `Where(criteria)` | Gọi nhiều lần thì nối bằng `AND` |
| `GroupBy(field)`, `GroupBy("biểu thức")`, `Having(criteria)` | `GROUP BY`, `HAVING` |
| `OrderBy(field, desc: true)`, `OrderByFirst(...)` | `ORDER BY` |
| `Distinct(true)` | `SELECT DISTINCT` |
| `Skip(n)`, `Take(n)` | Phân trang. SQL Server cần có `OrderBy` khi dùng `Skip` |
| `SubQuery()` | Tạo truy vấn con dùng chung tham số với truy vấn cha (bài 06, mục 3) |

Hàm tổng hợp viết sẵn trong lớp `Sql`: `Sql.Count()`, `Sql.Sum(...)`, `Sql.Avg(...)`, `Sql.Min(...)`,
`Sql.Max(...)`, `Sql.Coalesce(...)`, `Sql.Case(...)`.

**Tham số và SQL injection:** giá trị trong Criteria (`m.Year >= 2000`, `m.Title.Contains(text)`) được chuyển
thành tham số `@p1`, `@p2`. Còn chuỗi truyền vào `Where("...")` hay `Select("...")` được ghép thẳng vào SQL,
nên **không bao giờ** ghép dữ liệu người dùng nhập vào chuỗi đó.

## 3. Đọc kết quả

### 3.1. Vào một class tự định nghĩa (Dapper)

Serenity có sẵn Dapper. `connection.Query<T>(sqlQuery)` map cột theo tên property:

```csharp
public class MovieCountByGenre
{
    public string GenreName { get; set; }
    public int MovieCount { get; set; }
}

var g = GenreRow.Fields.As("g");
var list = connection.Query<MovieCountByGenre>(new SqlQuery()
        .From(m)
        .LeftJoin(g, g.GenreId == m.GenreId)
        .Select(g.Name, "GenreName")
        .Select(Sql.Count(), "MovieCount")
        .GroupBy(g.Name)
        .OrderBy(g.Name))
    .ToList();
```

### 3.2. Một giá trị

```csharp
var maxYear = connection.Query<int?>(new SqlQuery()
    .From(m)
    .Select(Sql.Max(m.Year.Expression)))
    .FirstOrDefault();
```

`GetNextNumberHelper` (bài 18) dùng đúng cách này với `Sql.Max`.

### 3.3. Kiểu `dynamic`

`connection.Query(sqlQuery)` (không có `<T>`) trả về các dòng `dynamic`, như `OrdersByShipper` và
`NotesBehavior`. Viết nhanh nhưng không có kiểm tra kiểu, gõ sai tên cột chỉ lỗi lúc chạy.

### 3.4. Vào Row bằng `ForEach`

```csharp
var row = new MovieRow();
new SqlQuery()
    .From(row)                       // From một Row: kết quả mỗi dòng được đọc vào chính row đó
    .Select(m.MovieId, m.Title)
    .Where(m.Year < 1950)
    .ForEach(connection, () =>
    {
        // row.MovieId, row.Title là của dòng hiện tại
    });
```

Cách này không tạo list, hợp khi duyệt rất nhiều dòng.

### 3.5. SQL viết tay và stored procedure

```csharp
// SQL thuần, tham số đặt tên
var items = connection.Query<MovieRow>(
    "SELECT MovieId, Title FROM mov.Movie WHERE Year BETWEEN @from AND @to",
    new { from = 2000, to = 2010 });

// Stored procedure (mẫu StoredProcedureGrid)
var data = connection.Query<CustomerGrossSalesRow>("CustomerGrossSales",
    param: new { startDate = request.StartDate, endDate = request.EndDate },
    commandType: System.Data.CommandType.StoredProcedure);
```

## 4. INSERT, UPDATE, DELETE

```csharp
// INSERT và lấy Id vừa tạo
var newId = new SqlInsert(m.TableName)
    .Set(m.Title, "Inception")
    .Set(m.Year, 2010)
    .Set(m.Kind, (int)MovieKind.Film)
    .ExecuteAndGetID(uow.Connection);

// Hoặc từ một Row
var id = uow.Connection.InsertAndGetID(new MovieRow { Title = "Tenet", Year = 2020, Kind = MovieKind.Film });

// UPDATE nhiều dòng
new SqlUpdate(m.TableName)
    .Set(m.Kind, (int)MovieKind.TvSeries)
    .Where(m.MovieId.In(ids))
    .Execute(uow.Connection, ExpectedRows.Ignore);

// DELETE
new SqlDelete(m.TableName)
    .Where(m.Year < 1900)
    .Execute(uow.Connection, ExpectedRows.Ignore);
```

Nhắc lại hai điểm từ bài 06 và bài 10:
- `Execute()` mặc định là `ExpectedRows.One`, khác 1 dòng thì ném lỗi.
- Các lệnh này **không** xóa cache. Sau khi ghi, gọi `BatchGenerationUpdater.OnCommit(uow, m.GenerationKey)`.

## 5. Connection

```csharp
using (var connection = SqlConnections.NewFor<MovieRow>())   // theo [ConnectionKey] của Row
{ ... }

using (var connection = SqlConnections.NewByKey("Default"))   // theo tên trong <connectionStrings>
{ ... }
```

- Connection được mở khi chạy lệnh đầu tiên (hoặc gọi `connection.EnsureOpen()`), và đóng khi `Dispose`.
  Luôn dùng `using`.
- Trong endpoint, repository, handler, behavior thì **đừng mở connection mới**. Dùng connection có sẵn:
  tham số `connection`, `uow.Connection`, hoặc `Connection` / `UnitOfWork.Connection` trong handler.
- Mở connection mới chỉ khi dữ liệu nằm ở database khác (Default và Northwind là hai DB). `NotesBehavior` làm
  vậy để đọc tên user từ DB Default trong khi đang ở DB Northwind.

## 6. Transaction và UnitOfWork

`UnitOfWork` bọc một transaction:

```csharp
using (var connection = SqlConnections.NewFor<MovieRow>())
using (var uow = new UnitOfWork(connection))     // mở connection và BEGIN TRANSACTION
{
    uow.Connection.InsertAndGetID(...);
    new SqlUpdate(...).Execute(uow.Connection, ExpectedRows.Ignore);

    uow.Commit();                                // COMMIT
}                                                // chưa Commit mà Dispose → ROLLBACK
```

Trong endpoint thì **không** tự tạo như trên. Khai báo tham số `IUnitOfWork uow` là framework tạo, commit
và rollback giúp (bài 18, mục 3).

### Chạy việc sau khi commit

`IUnitOfWork` có hai sự kiện `OnCommit` và `OnRollback`:

```csharp
uow.OnCommit += () =>
{
    // chỉ chạy khi dữ liệu đã thật sự được lưu
    EmailHelper.Send(...);          // gửi mail, gọi API ngoài, xóa file cũ...
};
```

Nếu gửi mail ngay trong `AfterSave` mà sau đó có lỗi làm rollback, người nhận sẽ nhận mail về một bản ghi không
tồn tại. `BatchGenerationUpdater.OnCommit` và `ImageUploadBehavior` (xóa file cũ) cũng dựa vào sự kiện này.

### Cái bẫy: mở connection thứ hai trong lúc đang có transaction

```csharp
public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request)
{
    // ...đã INSERT vào mov.Movie bằng uow.Connection, transaction chưa commit
    using (var cnn = SqlConnections.NewFor<MovieRow>())        // ❌ connection khác, ngoài transaction
        var count = cnn.Count<MovieRow>();                      // có thể bị chờ khóa cho tới timeout
}
```

Connection thứ hai không thấy dữ liệu chưa commit và có thể bị khóa bởi chính transaction đang mở. Hãy dùng
`uow.Connection`.

## 7. Bài tập

1. Viết truy vấn "số phim theo thể loại" ở mục 3.1. In `query.ToString()` ra log để xem SQL.
2. Viết truy vấn "5 phim dài nhất của mỗi loại" dùng `Take(5)` và `OrderBy(m.Runtime, desc: true)` cho từng
   `Kind`.
3. Viết một endpoint `IUnitOfWork` tạo một thể loại mới rồi gán cho nhiều phim bằng `SqlInsert` +
   `SqlUpdate`. Cố ý ném `ValidationError` ở cuối, kiểm tra trong DB rằng không có gì được lưu.
4. Thêm `uow.OnCommit` ghi một dòng log, kiểm tra nó chỉ chạy khi không có lỗi.
5. Ở dự án thật: tìm các chỗ dùng `new SqlQuery()` và `SqlConnections.New`. Có chỗ nào mở connection mới trong
   lúc đã có `uow` không?

## 8. Ghi chú của mình

-
