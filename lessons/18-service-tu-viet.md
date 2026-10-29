# Bài 18 – Tự viết service không phải CRUD

**Mục tiêu:** viết được một action riêng trong Endpoint (ngoài Create, Update, Delete, Retrieve, List), hiểu
framework mở connection và transaction cho mình thế nào, gọi nó từ TypeScript và xử lý lỗi đúng cách.

Tham khảo (3.13.7):
- [Serenity.Web/Mvc/ServiceEndpoint.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Mvc/ServiceEndpoint.cs), [JsonFilter.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Mvc/JsonFilter.cs), [EndpointExtensions.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Mvc/EndpointExtensions.cs)
- [Serenity.Scripts/CoreLib/Q/Q.Services.ts](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Scripts/CoreLib/Q/Q.Services.ts)
- Ví dụ trong Serene: [CustomerEndpoint.GetNextNumber](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Customer/CustomerEndpoint.cs), [GetNextNumberHelper.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Helpers/GetNextNumberHelper.cs), [BasicSamplesEndpoint.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/BasicSamplesEndpoint.cs), [SerialAutoNumberDialog.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Dialogs/SerialAutoNumber/SerialAutoNumberDialog.ts)
- Guide: `services/service_endpoints.md`

---

## 1. Khi nào cần service riêng

Năm action CRUD đủ cho việc thêm, sửa, xóa từng dòng. Những việc sau cần action riêng:

| Việc | Ví dụ trong Serene |
|---|---|
| Lấy số liệu tổng hợp cho biểu đồ, dashboard | `BasicSamples/OrdersByShipper` |
| Sinh mã tự động | `Northwind/Customer/GetNextNumber` |
| Thao tác hàng loạt trên nhiều dòng | `BasicSamples/OrderBulkAction` |
| Nhập file | `ProductExcelImport/ExcelImport` (bài 12) |
| Đổi mật khẩu, đăng ký, quên mật khẩu | `Membership/Account/*` |

## 2. Ba phần của một service

**a. Request và Response** là class C# bình thường, kế thừa `ServiceRequest` và `ServiceResponse`:

```csharp
// Common/Helpers/GetNextNumberRequest.cs
public class GetNextNumberRequest : ServiceRequest
{
    public string Prefix { get; set; }
    public int Length { get; set; }
}

public class GetNextNumberResponse : ServiceResponse
{
    public long Number { get; set; }
    public string Serial { get; set; }
}
```

`ServiceResponse` có sẵn property `Error` (kiểu `ServiceError`, gồm `Code`, `Message`, `Arguments`, `Details`).
Bạn không tự gán `Error`, framework gán khi có exception (mục 4).

**b. Action trong Endpoint:**

```csharp
public GetNextNumberResponse GetNextNumber(IDbConnection connection, GetNextNumberRequest request)
{
    return new MyRepository().GetNextNumber(connection, request);
}
```

**c. Code xử lý** đặt trong Repository hoặc một helper, giống CRUD.

Sau đó **build và chạy T4**. `ServerTypings.tt` sinh ra:

```ts
// Imports/ServerTypings/Northwind.CustomerService.ts (tự sinh)
export declare function GetNextNumber(request: GetNextNumberRequest,
    onSuccess?: (response: GetNextNumberResponse) => void, opt?: Q.ServiceOptions<any>): JQueryXHR;

export declare const enum Methods {
    GetNextNumber = "Northwind/Customer/GetNextNumber",
    // ...
}
```

## 3. Framework làm gì khi một request tới

Đây là phần lý thuyết quan trọng nhất của bài. Trong bản ASP.NET MVC, `ServiceEndpoint` dùng một
`ServiceEndpointActionInvoker` riêng. Khi có request `POST /Services/Northwind/Customer/GetNextNumber`:

1. **Route:** `[RoutePrefix("Services/Northwind/Customer"), Route("{action}")]` chọn action theo tên method.
2. **Phân quyền:** chạy `[ServiceAuthorize]` ở class và ở method (nếu có). Thiếu quyền thì dừng tại đây.
3. **Đọc request:** `JsonFilter` đọc body JSON và chuyển thành object request. Action chỉ được có **một**
   tham số không phải interface, hoặc tham số đó phải tên là `request`.
4. **Connection:** đọc `[ConnectionKey(...)]` của class rồi gọi `SqlConnections.NewByKey(...)`.
   - Tham số `IUnitOfWork uow`: mở connection, **mở transaction**, chạy action, **Commit** nếu không có
     exception. Có exception thì transaction bị dispose, tức là rollback.
   - Tham số `IDbConnection connection`: chỉ mở connection, **không có transaction**.
5. **Kết quả:** giá trị trả về được bọc thành JSON.

Từ đó rút ra quy tắc chọn tham số:

| Action | Dùng |
|---|---|
| Chỉ đọc dữ liệu | `IDbConnection connection` |
| Có ghi (INSERT, UPDATE, DELETE), nhất là ghi nhiều bảng | `IUnitOfWork uow` và đánh dấu `[HttpPost]` |

> ⚠️ Nếu dùng `IDbConnection` cho action có ghi dữ liệu, mỗi lệnh SQL tự commit riêng. Lỗi giữa chừng sẽ để
> lại dữ liệu ghi dở.

## 4. Lỗi được trả về client thế nào

Khi action ném exception, `ServiceEndpoint` bắt lại và gọi `ConvertToResponse`:

| Loại exception | HTTP status | `Error.Message` client nhận được |
|---|---|---|
| `ValidationError` | 400 | **Luôn là message bạn viết** |
| Exception khác (`NullReferenceException`, lỗi SQL...) | 500 | Message thật **chỉ khi `customErrors` tắt**. Nếu bật (ví dụ `RemoteOnly` khi truy cập từ máy khác), client chỉ thấy *"An error occurred while processing your request."* |

Exception cũng được ghi log (bảng `Exceptions`, xem ở `/errorlog.axd`, bài 15).

**Quy tắc:** lỗi nào muốn người dùng đọc được thì ném `ValidationError`:

```csharp
throw new ValidationError("Phim này đang được dùng, không thể đổi loại!");
throw new ValidationError("InvalidYear", "Year", "Năm không hợp lệ");   // có tên field → client tô đỏ ô đó
```

`request.CheckNotNull()` (dùng trong `OrderBulkAction`) ném lỗi nếu client gửi request rỗng.

## 5. Gọi service từ TypeScript

### 5.1. Cách nên dùng: hàm sinh sẵn

```ts
Serene.Northwind.CustomerService.GetNextNumber({
    Prefix: 'C',
    Length: 5
}, response => {
    this.form.CustomerID.value = response.Serial;
});
```

TypeScript kiểm tra được tên và kiểu của `Prefix`, `Length`, `response.Serial`. Đổi tên property ở C# mà quên
sửa TS thì build sẽ báo lỗi (sau khi chạy T4).

### 5.2. `Q.serviceCall` và `Q.serviceRequest`

Hàm sinh sẵn thực chất gọi `Q.serviceRequest(baseUrl + '/' + tên, request, onSuccess, opt)`, và hàm đó gọi
`Q.serviceCall`. Dùng trực tiếp khi không có typing, ví dụ gọi service của module khác:

```ts
Q.serviceCall<Serenity.ServiceResponse>({
    service: 'MovieDB/Movie/BulkChangeKind',   // không có "/" ở đầu → tự thêm "~/services/"
    request: { MovieIds: [1, 2, 3], Kind: MovieKind.TvSeries },
    blockUI: true,                             // mặc định true: che màn hình khi đang chờ
    onSuccess: response => Q.notifySuccess('Đã cập nhật'),
    onError: response => {                     // chỉ khai báo khi muốn tự xử lý lỗi
        Q.alert('Lỗi: ' + response.Error.Message);
    },
    onCleanup: () => { /* luôn chạy, kể cả khi lỗi */ }
});
```

Trong `Q.Services.ts`, khi lỗi mà bạn **không** khai báo `onError`, Serenity tự hiện hộp thoại báo lỗi
(`ErrorHandling.showServiceError`). Lỗi `NotLoggedIn` thì chuyển về trang đăng nhập. Vì vậy phần lớn trường hợp
chỉ cần viết `onSuccess`.

> Request được gửi bằng **POST với body JSON**. Muốn thử bằng Postman thì phải gửi kèm cookie đăng nhập,
> header `Content-Type: application/json`, và body là JSON của request.

## 6. Ví dụ hoàn chỉnh cho MovieDB: đổi loại phim hàng loạt

**Request** (`Modules/MovieDB/Movie/MovieBulkChangeKindRequest.cs`):

```csharp
public class MovieBulkChangeKindRequest : ServiceRequest
{
    public List<int> MovieIds { get; set; }
    public MovieKind? Kind { get; set; }
}
```

**Repository:**

```csharp
public ServiceResponse BulkChangeKind(IUnitOfWork uow, MovieBulkChangeKindRequest request)
{
    request.CheckNotNull();
    if (request.MovieIds.IsEmptyOrNull())
        throw new ValidationError("Chưa chọn phim nào!");
    if (request.Kind == null)
        throw new ValidationError("Chưa chọn loại phim!");

    new SqlUpdate(fld.TableName)
        .Set(fld.Kind, (int)request.Kind.Value)
        .Where(fld.MovieId.In(request.MovieIds))
        .Execute(uow.Connection, ExpectedRows.Ignore);     // nhiều dòng → Ignore (bài 06)

    BatchGenerationUpdater.OnCommit(uow, fld.GenerationKey); // SqlUpdate không tự xóa cache (bài 10)
    return new ServiceResponse();
}
```

**Endpoint:**

```csharp
[HttpPost, ServiceAuthorize(PermissionKeys.Movie.Modify)]   // thêm quyền riêng cho action này
public ServiceResponse BulkChangeKind(IUnitOfWork uow, MovieBulkChangeKindRequest request)
{
    return new MyRepository().BulkChangeKind(uow, request);
}
```

`[ServiceAuthorize(typeof(MyRow))]` ở class vẫn áp dụng, nên user cần **cả** quyền đọc và quyền Modify.

**Client** (`MovieGrid.ts`, dùng cột checkbox của bài 13):

```ts
buttons.push({
    title: 'Chuyển thành TV Series',
    cssClass: 'edit-button',
    onClick: () => {
        let ids = this.rowSelection.getSelectedKeys().map(x => parseInt(x, 10));
        if (!ids.length) {
            Q.notifyWarning('Hãy chọn ít nhất một phim');
            return;
        }
        Q.confirm('Đổi ' + ids.length + ' phim thành TV Series?', () => {
            MovieService.BulkChangeKind({ MovieIds: ids, Kind: MovieKind.TvSeries }, () => {
                this.rowSelection.resetCheckedAndRefresh();
                Q.notifySuccess('Đã cập nhật');
            });
        });
    }
});
```

Nhớ build và chạy T4 để có `MovieService.BulkChangeKind` và `MovieBulkChangeKindRequest` ở phía TS.

## 7. Service không cần connection

Action không có tham số `IDbConnection`/`IUnitOfWork` thì framework không mở kết nối. Khi đó class vẫn cần
`[ConnectionKey]` chỉ nếu có action khác dùng connection. Ví dụ: một action trả về cấu hình
`Config.Get<MovieDBSettings>()` (bài 15) cho client.

## 8. Lỗi hay gặp

| Hiện tượng | Nguyên nhân |
|---|---|
| `JsonFilter requires an action method with only one parameter, or a parameter with name 'request'` | Action có hai tham số kiểu class. Gộp lại thành một class request |
| `ArgumentNullException: connectionKey` | Action có `IDbConnection`/`IUnitOfWork` nhưng class thiếu `[ConnectionKey]` |
| Client báo *"An error occurred while processing your request."* | Exception thường bị giấu vì `customErrors` bật. Dùng `ValidationError` hoặc xem `/errorlog.axd` |
| `MovieService.BulkChangeKind is not a function` | Chưa chạy T4 sau khi thêm action |
| Dữ liệu ghi dở khi có lỗi | Dùng `IDbConnection` thay vì `IUnitOfWork` cho action có ghi |

## 9. Bài tập

1. Viết `MovieEndpoint.GetStats` (chỉ đọc) trả về số phim theo từng `Kind`. Hiện kết quả bằng `Q.alert` khi
   bấm một nút trên `MovieGrid`. Gợi ý: xem `OrdersByShipper` để biết cách `GroupBy`; bài 16 sẽ nói kỹ về
   `SqlQuery`.
2. Làm ví dụ `BulkChangeKind` ở mục 6. Thử đăng nhập bằng user không có quyền Modify và gọi service bằng F12.
3. Cố ý ném `new Exception("abc")` trong action, xem client nhận gì khi `customErrors` là `Off` và khi là `On`.
4. Ở dự án thật: tìm các Endpoint có action ngoài 5 action CRUD. Action đó dùng `IDbConnection` hay
   `IUnitOfWork`, và có đúng với quy tắc ở mục 3 không?

## 10. Ghi chú của mình

-
