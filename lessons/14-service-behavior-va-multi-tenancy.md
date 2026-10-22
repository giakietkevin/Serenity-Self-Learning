# Bài 14 – Service Behavior và Multi-tenancy

**Mục tiêu:** hiểu **service behavior**, cơ chế đứng sau `LinkingSetRelation`, `MasterDetailRelation`,
upload, localization... Tự viết một behavior dùng chung cho nhiều bảng, và áp dụng để làm **multi-tenancy**:
nhiều công ty (tenant) dùng chung một app và một database nhưng không thấy dữ liệu của nhau.

Tham khảo:
- [Serenity.Services/RequestHandlers/](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Services/RequestHandlers): `ISaveBehavior`, `IListBehavior`, `BaseSaveBehavior`..., thư mục `IntegratedFeatures/`
- Guide: `tutorials/multi_tenancy/` (13 bài), source đầy đủ ở repo [volkanceylan/MultiTenancy](https://github.com/volkanceylan/MultiTenancy)

---

## 1. Service behavior là gì

Ở bài 06, muốn thêm logic thì override `ValidateRequest`, `SetInternalFields`... **trong từng Repository**.
Nếu cùng một logic phải có ở 20 bảng (ví dụ lọc theo công ty), viết lại 20 lần thì vừa dài vừa dễ quên.

Behavior là một class **cắm vào** request handler. Handler gọi behavior ở đúng các bước mà bạn đã biết:

| Handler | Interface | Các "móc" (hook) |
|---|---|---|
| Save | `ISaveBehavior` | `OnPrepareQuery`, `OnValidateRequest`, `OnSetInternalFields`, `OnBeforeSave`, `OnAfterSave`, `OnAudit`, `OnReturn` |
| Delete | `IDeleteBehavior` | `OnPrepareQuery`, `OnValidateRequest`, `OnBeforeDelete`, `OnAfterDelete`, `OnAudit`, `OnReturn` |
| List | `IListBehavior` | `OnValidateRequest`, `OnPrepareQuery`, `OnApplyFilters`, `OnBeforeExecuteQuery`, `OnAfterExecuteQuery`, `OnReturn` |
| Retrieve | `IRetrieveBehavior` | `OnValidateRequest`, `OnPrepareQuery`, `OnBeforeExecuteQuery`, `OnAfterExecuteQuery`, `OnReturn` |

Có sẵn lớp cơ sở với các hàm rỗng (`virtual`) để chỉ override cái cần: `BaseSaveBehavior`,
`BaseDeleteBehavior`, `BaseSaveDeleteBehavior`, `BaseListBehavior`, `BaseRetrieveBehavior`.

### Behavior "implicit": tự bật theo điều kiện

Behavior implement `IImplicitBehavior` có hàm `ActivateFor(Row row)`. Serenity gọi hàm này **một lần cho mỗi
loại Row và loại handler**. Trả về `true` thì behavior được gắn vào handler đó.

> ⚠️ Một instance behavior được **dùng chung cho mọi request** của loại Row đó. Chỉ lưu những thứ cố định
> (như tham chiếu tới field) vào biến của class. **Không** lưu dữ liệu của request vào biến class.

### Các behavior có sẵn trong 3.13.7 (bạn đã dùng mà có thể không để ý)

| Behavior | Bật khi | Làm gì |
|---|---|---|
| `LinkingSetRelationBehavior` | Field có `[LinkingSetRelation]` | Đọc/ghi bảng nối nhiều–nhiều (bài 08) |
| `MasterDetailRelationBehavior` | Field có `[MasterDetailRelation]` | Đọc/ghi bảng con (bài 08) |
| `ImageUploadBehavior`, `MultipleImageUploadBehavior` | Field có editor upload | Chép file tạm, xóa file cũ (bài 11) |
| `LocalizationBehavior` | Row có `[LocalizationRow]` | Lưu bản dịch dữ liệu (bài 09) |
| `CaptureLogBehavior` | Row có `[CaptureLog]` | Ghi lịch sử thay đổi (walkthrough 01) |
| `InsertUpdateLogBehavior` | Row implement `IInsertLogRow` / `IUpdateLogRow` | **Tự gán** `InsertDate`, `InsertUserId`, `UpdateDate`, `UpdateUserId` |
| `UniqueFieldSaveBehavior` | Field có `[Unique]` | Kiểm tra trùng giá trị trước khi lưu |
| `UniqueConstraintSaveBehavior` | Row có `[UniqueConstraint(...)]` | Kiểm tra trùng theo nhiều cột |
| `UpdatableExtensionBehavior` | Row có `[UpdatableExtension]` | Lưu sang bảng mở rộng 1–1 |

Hai behavior rút gọn được code của bộ mẫu MovieDB:

```csharp
// Thay cho đoạn kiểm tra trùng tên tự viết trong MovieRepository.ValidateRequest
[DisplayName("Title"), Size(200), NotNull, QuickSearch, Unique]
public String Title { ... }

// Thay cho việc tự gán InsertDate, InsertUserId trong SetInternalFields:
// cho MovieRow implement IInsertLogRow
public sealed class MovieRow : Row, IIdRow, INameRow, IInsertLogRow
{
    // ...
    IIdField IInsertLogRow.InsertUserIdField { get { return Fields.InsertUserId; } }
    DateTimeField IInsertLogRow.InsertDateField { get { return Fields.InsertDate; } }
}
```

Serene dùng cách thứ hai cho `UserRow` thông qua lớp cơ sở `LoggingRow` (`Modules/Administration/User/LoggingRow.cs`).

## 2. Tự viết behavior: ví dụ "ghi chú lần sửa cuối"

Muốn mọi Row có cột `LastNote` tự được gán chuỗi `"Sửa bởi <user> lúc <giờ>"`:

```csharp
public interface ILastNoteRow
{
    StringField LastNoteField { get; }
}

public class LastNoteBehavior : BaseSaveBehavior, IImplicitBehavior
{
    private StringField fld;                         // cố định theo loại Row → được phép lưu

    public bool ActivateFor(Row row)
    {
        var r = row as ILastNoteRow;
        if (r == null)
            return false;
        fld = r.LastNoteField;
        return true;
    }

    public override void OnSetInternalFields(ISaveRequestHandler handler)
    {
        fld[handler.Row] = "Sửa bởi " + Authorization.Username + " lúc " + DateTime.Now.ToString("g");
    }
}
```

Không cần đăng ký ở đâu: Serenity tự quét các class implement `IImplicitBehavior` trong assembly của app.

## 3. Multi-tenancy bằng behavior

### 3.1. Dữ liệu

- Bảng `Tenants (TenantId, TenantName)` ở DefaultDB.
- Thêm cột `TenantId INT NOT NULL DEFAULT 1` vào `Users`, `Roles` và **mọi bảng nghiệp vụ** cần tách.
- Các bảng như `UserRoles`, `UserPermissions` không cần, vì đã gắn với User/Role.

### 3.2. Biết user hiện tại thuộc tenant nào

Thêm `TenantId` vào `UserDefinition` (`Modules/Administration/User/Authentication/UserDefinition.cs`) và gán
trong `UserRetrieveService.GetFirst`. Vì `UserDefinition` được cache (bài 10), đọc nó ở mọi request gần như
không tốn gì:

```csharp
var tenantId = ((UserDefinition)Authorization.UserDefinition).TenantId;
```

Thêm quyền `Administration:Tenants` cho người quản trị toàn hệ thống (được thấy mọi tenant).

### 3.3. Interface và behavior

```csharp
public interface IMultiTenantRow
{
    Int32Field TenantIdField { get; }
}

public class MultiTenantBehavior : IImplicitBehavior,
    ISaveBehavior, IDeleteBehavior, IListBehavior, IRetrieveBehavior
{
    private Int32Field fldTenantId;

    public bool ActivateFor(Row row)
    {
        var mt = row as IMultiTenantRow;
        if (mt == null)
            return false;
        fldTenantId = mt.TenantIdField;
        return true;
    }

    // ① List và Retrieve: chỉ thấy dữ liệu của tenant mình
    public void OnPrepareQuery(IListRequestHandler handler, SqlQuery query)
    {
        if (!Authorization.HasPermission(PermissionKeys.Tenants))
            query.Where(fldTenantId == ((UserDefinition)Authorization.UserDefinition).TenantId);
    }

    public void OnPrepareQuery(IRetrieveRequestHandler handler, SqlQuery query)
    {
        if (!Authorization.HasPermission(PermissionKeys.Tenants))
            query.Where(fldTenantId == ((UserDefinition)Authorization.UserDefinition).TenantId);
    }

    // ② Tạo mới: TenantId lấy từ user, không tin client
    public void OnSetInternalFields(ISaveRequestHandler handler)
    {
        if (handler.IsCreate)
            fldTenantId[handler.Row] = ((UserDefinition)Authorization.UserDefinition).TenantId;
    }

    // ③ Sửa/xóa bản ghi của tenant khác: phải có quyền Tenants
    public void OnValidateRequest(ISaveRequestHandler handler)
    {
        if (handler.IsUpdate &&
            fldTenantId[handler.Old] != ((UserDefinition)Authorization.UserDefinition).TenantId)
            Authorization.ValidatePermission(PermissionKeys.Tenants);
    }

    public void OnValidateRequest(IDeleteRequestHandler handler)
    {
        if (fldTenantId[handler.Row] != ((UserDefinition)Authorization.UserDefinition).TenantId)
            Authorization.ValidatePermission(PermissionKeys.Tenants);
    }

    // ... các hàm còn lại của 4 interface để trống
}
```

### 3.4. Áp dụng cho một bảng

```csharp
public sealed class MovieRow : Row, IIdRow, INameRow, IMultiTenantRow
{
    [Insertable(false), Updatable(false)]      // client không gửi được TenantId
    public Int32? TenantId
    {
        get { return Fields.TenantId[this]; }
        set { Fields.TenantId[this] = value; }
    }

    public Int32Field TenantIdField
    {
        get { return Fields.TenantId; }
    }
    // RowFields: public Int32Field TenantId;
}
```

Không phải sửa Repository nào.

### 3.5. Đừng quên lookup script

Lookup (dropdown) **không đi qua List service**, nên behavior không lọc được. Lookup lại được cache **một
bản chung cho mọi user** (bài 10), nên tenant A sẽ thấy dropdown của tenant B. Cách xử lý trong tutorial: tạo
lớp lookup riêng, tắt cache chung và cache theo từng tenant.

```csharp
public class MultiTenantRowLookupScript<TRow> : RowLookupScript<TRow>
    where TRow : Row, IMultiTenantRow, new()
{
    public MultiTenantRowLookupScript()
    {
        Expiration = TimeSpan.FromDays(-1);          // tắt cache chung của dynamic script
    }

    protected override void PrepareQuery(SqlQuery query)
    {
        base.PrepareQuery(query);
        AddTenantFilter(query);
    }

    protected void AddTenantFilter(SqlQuery query)
    {
        var r = new TRow();
        query.Where(r.TenantIdField == ((UserDefinition)Authorization.UserDefinition).TenantId);
    }

    public override string GetScript()               // cache riêng theo TenantId
    {
        return TwoLevelCache.GetLocalStoreOnly(
            "MultiTenantLookup:" + this.ScriptName + ":" +
                ((UserDefinition)Authorization.UserDefinition).TenantId,
            TimeSpan.FromHours(1),
            new TRow().GetFields().GenerationKey,
            () => base.GetScript());
    }
}
```

```csharp
[LookupScript("MovieDB.Genre", LookupType = typeof(MultiTenantRowLookupScript<>))]
public sealed class GenreRow : Row, IIdRow, INameRow, IMultiTenantRow
```

### 3.6. Danh sách kiểm tra bảo mật

Tutorial nhấn mạnh: **giấu trên giao diện chưa phải là bảo mật**. Người dùng có thể gọi service bằng F12:

```js
Q.serviceCall({ service: 'Administration/User/Update',
    request: { EntityId: 2, Entity: { UserId: 2, TenantId: 1 } } });
```

| Lỗ hổng | Chặn bằng |
|---|---|
| Tự đổi `TenantId` của mình | `[ReadPermission(PermissionKeys.Tenants)]` trên field `TenantId` của UserRow, hoặc `Insertable(false), Updatable(false)` |
| Mở bản ghi tenant khác bằng Id (`loadByIdAndOpenDialog(1)`) | Lọc trong **Retrieve**, không chỉ List |
| Sửa/xóa bản ghi tenant khác | Kiểm tra `Old.TenantId` / `Row.TenantId` khi Update, Delete |
| Dropdown lộ dữ liệu tenant khác | Lookup theo tenant (mục 3.5) |
| Quyền và Role dùng chung | Thêm `TenantId` cho Roles, ẩn quyền `Administration:Tenants` khỏi tenant thường |

> Tác giả Serenity cũng ghi trong tutorial rằng ông **thích mô hình mỗi khách hàng một database** hơn.
> Multi-tenancy chung database tiết kiệm chi phí quản lý nhưng khó sao lưu riêng từng khách và mọi truy vấn
> đều phải lọc. Hãy cân nhắc trước khi chọn.

## 4. Bài tập

1. Gắn `[Unique]` cho `MovieRow.Title`, xóa đoạn kiểm tra trùng tự viết trong `MovieRepository`. Thử tạo hai
   phim trùng tên và đọc thông báo lỗi.
2. Cho `MovieRow` implement `IInsertLogRow`, xóa phần gán `InsertDate`/`InsertUserId` trong `SetInternalFields`.
3. Viết `LastNoteBehavior` (mục 2) và áp dụng cho Movie và Genre.
4. (Nâng cao) Làm multi-tenancy cho MovieDB theo mục 3. Tạo 2 tenant, 2 user, kiểm tra bằng cả giao diện và
   lệnh `Q.serviceCall` trong F12.
5. Ở dự án thật: tìm các class có `IImplicitBehavior`. Dự án có dùng `[Unique]`, `IInsertLogRow` không?

## 5. Ghi chú của mình

-
