# Bài 10 – Cache: LocalCache, TwoLevelCache và làm mới cache

**Mục tiêu:** hiểu vì sao sửa dữ liệu thẳng trong SQL thì dropdown vẫn hiện dữ liệu cũ. Biết cách cache một
kết quả tốn thời gian và làm mới nó đúng lúc.

Tham khảo:
- [Serenity.Core/Caching/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Core/Caching): `LocalCache`, `TwoLevelCache`, `DistributedCache`
- [BatchGenerationUpdater.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers/Helpers/BatchGenerationUpdater.cs)
- Code mẫu trong Serene: [DashboardPage.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Dashboard/DashboardPage.cs), [UserRetrieveService.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/Authentication/UserRetrieveService.cs), [PermissionService.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/Authentication/PermissionService.cs)
- Guide: `caching/`

---

## 1. Ba loại cache

| Loại | Lớp | Nằm ở đâu | Khi nào dùng |
|---|---|---|---|
| Local | `LocalCache` | RAM của **một** tiến trình IIS | Dữ liệu chỉ đọc, không cần đồng bộ giữa nhiều server |
| Distributed | `DistributedCache` | Redis hoặc Couchbase. Mặc định là `DistributedCacheEmulator`, thực chất cũng chỉ là RAM | Web farm có nhiều server |
| Hai tầng | `TwoLevelCache` | Local + Distributed, kèm **group key** để làm mới | **Dùng nhiều nhất**: cache dữ liệu lấy từ một bảng |

Serene chạy một server nên distributed cache chỉ là bản giả lập (`CommonInitialization` đăng ký
`DistributedCacheEmulator` nếu chưa có provider nào). Bạn vẫn nên dùng `TwoLevelCache` vì nó có cơ chế
**group key** rất tiện, và sau này chuyển sang Redis không phải sửa code.

## 2. `LocalCache`: cache đơn giản nhất

```csharp
// Modules/BasicSamples/Grids/VSGalleryQA/VSGalleryQAEndpoint.cs
return LocalCache.Get("VSGalleryCachedThreads", TimeSpan.FromMinutes(5), () =>
{
    // gọi API bên ngoài, tốn vài giây...
    return threads;
});
```

- Tham số 1 là **cache key**, phải là duy nhất trong toàn app.
- Tham số 2 là thời gian sống. `TimeSpan.Zero` nghĩa là **không bao giờ hết hạn** (chỉ mất khi app khởi
  động lại hoặc gọi `LocalCache.Remove(key)`).
- Tham số 3 là hàm nạp (`loader`), chỉ chạy khi trong cache chưa có.

## 3. `TwoLevelCache` và **group key**

Vấn đề của `LocalCache`: nếu cache danh sách nhân viên trong 1 ngày, thì khi có người thêm nhân viên mới,
cache vẫn giữ danh sách cũ. `TwoLevelCache` giải quyết bằng **group key**: một "số phiên bản" gắn với
nhóm dữ liệu. Khi số phiên bản đổi, mọi mục trong nhóm coi như hết hạn.

```csharp
// Modules/Common/Dashboard/DashboardPage.cs
var cachedModel = TwoLevelCache.GetLocalStoreOnly("DashboardPageModel", TimeSpan.FromMinutes(5),
    OrderRow.Fields.GenerationKey, () =>                    // ← group key: bảng Orders
    {
        var model = new DashboardPageModel();
        using (var connection = SqlConnections.NewFor<OrderRow>())
        {
            model.OpenOrders = connection.Count<OrderRow>(o.ShippingState == (int)OrderShippingState.NotShipped);
            // ...
        }
        return model;
    });
```

- `OrderRow.Fields.GenerationKey` có giá trị mặc định là `"<ConnectionKey>.<TableName>"`, ví dụ
  `"Northwind.Orders"`. Với MovieRow là `"Default.[mov].[Movie]"`.
- Mục cache này hết hạn khi **đủ 5 phút** hoặc khi **bảng Orders có thay đổi**, tùy điều kiện nào đến trước.
- `GetLocalStoreOnly`: chỉ lưu ở cache local (vẫn dùng group key). Hợp với object không cần serialize.
  `TwoLevelCache.Get(...)` thì lưu cả hai tầng.

Các hàm chính:

| Hàm | Ý nghĩa |
|---|---|
| `TwoLevelCache.Get(key, expiration, groupKey, loader)` | Đọc, nếu chưa có thì nạp và lưu cả hai tầng |
| `TwoLevelCache.GetLocalStoreOnly(key, expiration, groupKey, loader)` | Giống trên nhưng chỉ lưu local |
| `TwoLevelCache.ExpireGroupItems(groupKey)` | **Làm hết hạn cả nhóm** (đổi phiên bản) |
| `TwoLevelCache.Remove(key)` | Xóa một mục |

> Phiên bản của nhóm cũng được cache local khoảng **5 giây** (`GenerationCacheExpiration`). Vì vậy trong web
> farm, server khác có thể dùng dữ liệu cũ thêm vài giây.

## 4. Ai gọi `ExpireGroupItems`? Phần lớn là **tự động**

Trong `SaveRequestHandler`, `DeleteRequestHandler` và `UndeleteRequestHandler` của Serenity 3.13.7 có:

```csharp
protected virtual void InvalidateCacheOnCommit()
{
    BatchGenerationUpdater.OnCommit(this.UnitOfWork, Row.GetFields().GenerationKey);
    var attr = typeof(TRow).GetCustomAttribute<TwoLevelCachedAttribute>(false);
    if (attr != null)
        foreach (var key in attr.GenerationKeys)
            BatchGenerationUpdater.OnCommit(this.UnitOfWork, key);
}
```

Nghĩa là: **mỗi lần lưu hoặc xóa qua service**, group key của bảng đó tự hết hạn **sau khi transaction
commit thành công**. `BatchGenerationUpdater` gom lại để nếu trong một transaction có lưu 100 dòng thì chỉ đổi
phiên bản một lần.

`[TwoLevelCached("key1", "key2")]` trên Row dùng khi lưu bảng này thì cần làm hết hạn thêm các nhóm khác.

### Lookup script cũng dùng cơ chế này

`[LookupScript]` (bài 05) được cache phía server theo **group key của Row**. Thêm Genre qua dialog thì
lookup `MovieDB.Genre` tự được làm mới. Có thể đặt thêm thời gian sống:

```csharp
[LookupScript("MovieDB.Genre", Expiration = 60 * 60)]   // giây
```

Phía client, nếu cần lấy lại lookup trong cùng trang thì dùng `Q.reloadLookup('MovieDB.Genre')`.

## 5. Khi nào **phải tự** làm mới cache

Cơ chế tự động chỉ chạy khi dữ liệu đi qua request handler. Các trường hợp sau phải tự làm:

**a. Tự viết `SqlUpdate`, `SqlInsert`, `SqlDelete`** trong Repository hoặc endpoint riêng:

```csharp
new SqlUpdate(MyRow.Fields.TableName)
    .Set(fld.Kind, (int)MovieKind.Film)
    .Where(new Criteria(fld.Kind).IsNull())
    .Execute(uow.Connection, ExpectedRows.Ignore);

BatchGenerationUpdater.OnCommit(uow, fld.GenerationKey);   // ← làm mới khi commit
```

Serene làm đúng như vậy trong `UserRoleRepository`, `RolePermissionRepository` và `AccountPage.ChangePassword`.

**b. Sửa dữ liệu trực tiếp trong SQL Server Management Studio** hoặc qua job, app khác. Serenity không
biết. Cách xử lý: khởi động lại app pool, hoặc làm một nút/endpoint admin gọi `TwoLevelCache.ExpireGroupItems(...)`.

**c. Cache theo key riêng** như `UserRetrieveService` cache user theo Id:

```csharp
return TwoLevelCache.Get<UserDefinition>("UserByID_" + id, TimeSpan.Zero, TimeSpan.FromDays(1),
    fld.GenerationKey, () => { /* đọc DB */ });

// Và có hàm xóa riêng khi cần:
public static void RemoveCachedUser(int? userId, string username)
{
    if (userId != null) TwoLevelCache.Remove("UserByID_" + userId);
    ...
}
```

## 6. Những chỗ Serene đang cache (nên biết để khỏi bất ngờ)

| Dữ liệu | Code | Group key |
|---|---|---|
| Thông tin user đăng nhập | `UserRetrieveService` | `UserRow` |
| Quyền của user, role | `PermissionService` | `UserPermissionRow`, `RolePermissionRow`, `UserRoleRow` |
| Menu bên trái theo user | `NavigationModel` | `UserPermissionRow` |
| Số liệu Dashboard | `DashboardPage` | `OrderRow`, thêm 5 phút |
| Mọi lookup script | `[LookupScript]` | Row tương ứng |

Ví dụ: gán quyền cho user bằng câu SQL `INSERT INTO UserPermissions ...` thì user **vẫn chưa có quyền** cho
tới khi cache hết hạn. Gán qua giao diện thì có ngay.

## 7. Dùng Redis (khi có nhiều server)

Serenity 3.13.7 có sẵn package `Serenity.Caching.Redis` và `Serenity.Caching.Couchbase`. Chỉ cần đăng ký
provider trong `SiteInitialization.ApplicationStart` **trước** khi dùng cache. Một server thì chưa cần.

## 8. Bài tập

1. Mở `DashboardPage.cs`. Thêm một đơn hàng qua giao diện rồi về Dashboard: số liệu có đổi ngay không? Thêm
   bằng câu SQL trong SSMS thì sao? Giải thích.
2. Viết endpoint `MovieController.Stats` trả về số phim theo từng `Kind`, cache bằng
   `TwoLevelCache.GetLocalStoreOnly` với group key `MovieRow.Fields.GenerationKey`, thời gian 10 phút.
3. Viết một `SqlUpdate` đổi tên thể loại, **quên** gọi `BatchGenerationUpdater`. Mở dialog Movie xem dropdown
   Genre. Sau đó thêm dòng `OnCommit` và thử lại.
4. Ở dự án thật: tìm `TwoLevelCache` và `LocalCache` trong source. Dữ liệu nào đang được cache, và group key
   có đúng bảng không?

## 9. Ghi chú của mình

-
