# Bài 06 – Viết nghiệp vụ trong Repository (Request Handler)

**Mục tiêu:** biết **override đúng hàm** trong Save, List và Delete handler để thêm validation, tự gán
giá trị, lọc dữ liệu và xử lý sau khi lưu. Đây là nơi bạn sẽ viết nhiều code nghiệp vụ nhất trong dự án.

Tham khảo (đúng commit 3.13.7):
- [SaveRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers/Save/SaveRequestHandler.cs)
- [ListRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers/List/ListRequestHandler.cs)
- Ví dụ thật: [Administration/User/UserRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/UserRepository.cs), [Northwind/Order/OrderRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderRepository.cs)

---

## 1. Thứ tự chạy của SaveRequestHandler

Đây là code thật của `Process()` trong bản 3.13.7, đã rút gọn:

```csharp
Row = request.Entity;
// Create hay Update? Nếu Id null thì là Create
if (requestType == SaveRequestType.Update)
{
    Old = new TRow();
    LoadOldEntity();          // đọc bản ghi cũ từ DB vào Old
}

ValidateRequest();            // ① kiểm tra dữ liệu, quyền, ném lỗi
SetInternalFields();          // ② tự gán các field người dùng không nhập
BeforeSave();                 // ③ ngay trước INSERT/UPDATE
ClearNonTableAssignments();   //    bỏ các view field (Expression) khỏi lệnh lưu
ExecuteSave();                //    INSERT hoặc UPDATE
AfterSave();                  // ④ sau khi lưu: lưu bảng con, ghi log, gọi việc khác
PerformAuditing();
OnReturn();                   // ⑤ chỉnh response trả về
```

Các thuộc tính hay dùng bên trong handler:

| Thuộc tính | Ý nghĩa |
|---|---|
| `Row` | Dữ liệu client gửi lên. Sau `SetInternalFields` là dữ liệu sẽ được lưu |
| `Old` | Bản ghi trước khi sửa, chỉ có khi Update |
| `IsCreate`, `IsUpdate` | Đang thêm mới hay đang sửa |
| `Connection`, `UnitOfWork` | Kết nối DB, transaction hiện tại. Mọi thứ trong handler chạy chung transaction |
| `Request`, `Response` | Request và response gốc |

**Luôn gọi `base.XXX()`**, trừ khi bạn chắc chắn muốn bỏ hành vi mặc định.

## 2. Ví dụ Save handler

```csharp
private class MySaveHandler : SaveRequestHandler<MyRow>
{
    protected override void ValidateRequest()
    {
        base.ValidateRequest();

        if (Row.ReleaseDate != null && Row.Year != null &&
            Row.ReleaseDate.Value.Year != Row.Year.Value)
        {
            throw new ValidationError("YearMismatch", "Year",
                "Năm phát hành không khớp với ngày phát hành!");
        }

        // Kiểm tra trùng tên (khi thêm mới, hoặc khi đổi tên)
        if (IsCreate || Row.Title != Old.Title)
        {
            if (Connection.Exists<MyRow>(fld.Title == Row.Title))
                throw new ValidationError("UniqueViolation", "Title", "Tên phim đã tồn tại!");
        }
    }

    protected override void SetInternalFields()
    {
        base.SetInternalFields();

        if (IsCreate)
            Row.InsertDate = DateTime.Now;

        Row.UpdateDate = DateTime.Now;
        Row.Title = Row.Title?.Trim();
    }

    protected override void AfterSave()
    {
        base.AfterSave();

        // Row.MovieId đã có giá trị (kể cả khi vừa INSERT)
        if (IsUpdate && Row.Kind != Old.Kind)
        {
            // ví dụ: ghi lịch sử, cập nhật bảng khác bằng UnitOfWork.Connection
        }
    }
}
```

- `ValidationError(errorCode, field, message)`: tham số thứ hai là tên field, nhờ vậy client tô đỏ đúng ô.
- Ném exception thì **transaction rollback**, không có gì được lưu.
- `UserRepository` của Serene là ví dụ đầy đủ nhất: kiểm tra username trùng, băm mật khẩu trong
  `SetInternalFields`, và làm mới cache trong `AfterSave`.

## 3. Ví dụ List handler

```csharp
private class MyListHandler : ListRequestHandler<MyRow>
{
    protected override void PrepareQuery(SqlQuery query)
    {
        base.PrepareQuery(query);
        // Luôn select thêm field, kể cả khi grid không hiện
        query.Select(fld.Kind);
    }

    protected override void ApplyFilters(SqlQuery query)
    {
        base.ApplyFilters(query);

        // Ví dụ: user không có quyền Admin thì chỉ thấy phim từ năm 2000
        if (!Authorization.HasPermission("Administration:General"))
            query.Where(fld.Year >= 2000);
    }
}
```

Muốn client gửi thêm tham số lọc thì tạo request riêng, như `OrderListRequest` trong Serene:

```csharp
public class OrderListRequest : ListRequest
{
    public int? ProductID { get; set; }
}

private class MyListHandler : ListRequestHandler<MyRow, OrderListRequest>
{
    protected override void ApplyFilters(SqlQuery query)
    {
        base.ApplyFilters(query);
        if (Request.ProductID != null)
        {
            var od = Entities.OrderDetailRow.Fields.As("od");
            query.Where(Criteria.Exists(
                query.SubQuery()
                    .Select("1")
                    .From(od)
                    .Where(od.OrderID == fld.OrderID & od.ProductID == Request.ProductID.Value)
                    .ToString()));
        }
    }
}
```

Nhớ sửa luôn chữ ký của hàm `List` trong Repository và Endpoint để nhận `OrderListRequest`.

Ở phía client, gửi tham số trong `Grid.ts`:

```ts
protected onViewSubmit() {
    if (!super.onViewSubmit()) return false;
    var request = this.view.params as OrderListRequest;
    request.ProductID = this.productID;
    return true;
}
```

## 4. Criteria: viết điều kiện WHERE bằng C#

```csharp
var fld = MovieRow.Fields;

query.Where(fld.Year >= 2000 & fld.Kind == (int)MovieKind.Film);   // AND
query.Where(new Criteria(fld.Title).Contains("Lord") |
            new Criteria(fld.Title).StartsWith("The"));            // OR
query.Where(new Criteria(fld.GenreId).IsNull());
query.Where(new Criteria(fld.MovieId).In(1, 2, 3));
query.Where(new Criteria(fld.Title).Like("%ring%"));
```

Dùng `&` và `|` (một ký tự), **không phải** `&&` và `||`. Phép so sánh (`==`, `>=`...) viết thẳng trên field được, còn `Contains`, `StartsWith`, `Like`, `In`, `IsNull` thì phải bọc field trong `new Criteria(...)`.

## 5. Truy vấn trực tiếp ngoài handler

```csharp
// Lấy 1 dòng
var movie = connection.TryById<MovieRow>(movieId);
var first = connection.TryFirst<MovieRow>(fld.Title == "Inception");

// Lấy danh sách
var list = connection.List<MovieRow>(fld.Year == 2010);

// Kiểm tra tồn tại / đếm
bool exists = connection.Exists<MovieRow>(fld.GenreId == genreId);
int count = connection.Count<MovieRow>(fld.Kind == 1);

// Cập nhật, xóa hàng loạt
new SqlUpdate(fld.TableName).Set(fld.Kind, 2).Where(fld.Year < 1950)
    .Execute(connection, ExpectedRows.Ignore);
new SqlDelete(fld.TableName).Where(fld.Year < 1900)
    .Execute(connection, ExpectedRows.Ignore);
```

> ⚠️ `Execute()` của `SqlUpdate`/`SqlDelete` mặc định là `ExpectedRows.One`: nếu số dòng bị ảnh hưởng
> **khác 1** thì nó ném lỗi. Khi cập nhật hay xóa nhiều dòng (hoặc có thể 0 dòng), luôn truyền
> `ExpectedRows.Ignore`.

`UserRepository.MyDeleteHandler` dùng `SqlDelete` để xóa các bảng liên quan trong `OnBeforeDelete()`.

## 6. Delete handler

```csharp
private class MyDeleteHandler : DeleteRequestHandler<MyRow>
{
    protected override void ValidateRequest()
    {
        base.ValidateRequest();
        if (Connection.Exists<MovieCastRow>(MovieCastRow.Fields.MovieId == Row.MovieId.Value))
            throw new ValidationError("Phim đang có diễn viên, không thể xóa!");
    }

    protected override void OnBeforeDelete()
    {
        base.OnBeforeDelete();
        // xóa dữ liệu con trước khi xóa bản ghi chính
    }
}
```

## 7. Sơ đồ "logic này đặt ở đâu"

| Logic | Đặt ở |
|---|---|
| Kiểm tra dữ liệu, ném lỗi cho người dùng | `ValidateRequest` |
| Gán ngày tạo, người tạo, trạng thái mặc định | `SetInternalFields` |
| Tính toán field trước khi lưu | `BeforeSave` |
| Lưu bảng con, cập nhật bảng khác, gửi mail | `AfterSave` |
| Lọc dữ liệu theo user hoặc theo tham số | `ListHandler.ApplyFilters` |
| Kiểm tra trước khi xóa | `DeleteHandler.ValidateRequest` / `OnBeforeDelete` |
| Kiểm tra ngay trên giao diện (cho nhanh) | `Dialog.ts` → `addValidationRule`. **Vẫn phải kiểm tra lại ở server** |

## 8. Bài tập

1. Thêm validation: `Year` phải nằm trong khoảng 1888 đến năm hiện tại + 5.
2. Thêm 2 cột `InsertDate`, `InsertUserId`. Gán chúng trong `SetInternalFields` (user lấy từ
   `Convert.ToInt32(Authorization.UserId)`, vì `UserId` có kiểu string), và không cho sửa trên form.
3. Viết `ApplyFilters` sao cho user không có quyền `Administration:General` chỉ thấy phim `Kind = Film`.
4. Ở dự án thật: tìm 3 Repository có override handler. Với mỗi cái, ghi lại hàm nào được override và để
   làm gì.

## 9. Ghi chú của mình

-
