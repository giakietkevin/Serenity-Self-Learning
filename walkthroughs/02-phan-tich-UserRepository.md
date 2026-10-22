# Phân tích code 02 – `UserRepository.cs`: nghiệp vụ thật trong Save/Delete handler

**File gốc:** [Serene.Web/Modules/Administration/User/UserRepository.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Administration/User/UserRepository.cs) (Serene 3.13.7)

**Liên quan bài:** 06 (Request Handler), 07 (quyền)

Đây là repository có **nhiều nghiệp vụ nhất** trong Serene: kiểm tra username, kiểm tra độ dài mật khẩu,
băm mật khẩu, giới hạn field theo quyền, và xóa dữ liệu liên quan. Đọc kỹ file này thì bạn sẽ biết nên
đặt từng loại logic vào đâu.

---

## 1. Hàm kiểm tra dùng chung (static)

```csharp
public static string ValidatePassword(string username, string password, bool isNewUser)
{
    password = password.TrimToNull();                           // ①

    if (password == null || password.Length < 5)
        throw new ValidationError("PasswordLength", "Password",  // ②
            String.Format(Texts.Validation.MinRequiredPasswordLength, 5));  // ③

    return password;
}
```

| # | Giải thích |
|---|---|
| ① | `TrimToNull()` là extension của Serenity: cắt khoảng trắng, và trả `null` nếu chuỗi rỗng. Rất hay dùng. |
| ② | `ValidationError(errorCode, field, message)`. Tham số `"Password"` giúp client **tô đỏ đúng ô** Password. |
| ③ | `Texts.Validation...` là chuỗi **đa ngôn ngữ**, không phải chuỗi cứng (xem `Modules/Texts.cs`). |

Hàm để `public static` để chỗ khác dùng lại được, ví dụ trang đổi mật khẩu hay trang đăng ký.

## 2. `MySaveHandler`: thứ tự các bước

### 2.1 Giới hạn field được sửa theo quyền

```csharp
protected override void GetEditableFields(HashSet<Field> editable)
{
    base.GetEditableFields(editable);

    if (!Authorization.HasPermission(Administration.PermissionKeys.Security))
    {
        editable.Remove(fld.Source);       // ④
        editable.Remove(fld.IsActive);
    }
}
```

④ `GetEditableFields` quyết định **field nào client được phép gửi lên**. User không có quyền `Security`
thì không đổi được `IsActive` (khóa hay mở tài khoản), **kể cả khi tự gọi API**. Đây là ví dụ phân quyền
theo field bằng code, thay vì dùng attribute.

### 2.2 `ValidateRequest`: phân nhánh theo Create và Update

```csharp
protected override void ValidateRequest()
{
    base.ValidateRequest();                                              // ⑤

    if (IsUpdate)
    {
        CheckPublicDemo(Row.UserId);

        if (Row.IsAssigned(fld.Password) && !Row.Password.IsEmptyOrNull())   // ⑥
            password = Row.Password = ValidatePassword(Old.Username, Row.Password, false);

        if (Row.Username != Old.Username)                                // ⑦
            Row.Username = MySaveHandler.ValidateUsername(this.Connection, Row.Username, Old.UserId.Value);

        if (Row.DisplayName != Old.DisplayName)
            Row.DisplayName = ValidateDisplayName(this.Connection, Row.DisplayName, Old.UserId.Value);
    }

    if (IsCreate)                                                        // ⑧
    {
        this.Row.Username = ValidateUsername(this.Connection, this.Row.Username, null);
        this.Row.DisplayName = ValidateDisplayName(this.Connection, this.Row.DisplayName, null);
        password = ValidatePassword(Row.Username, Row.Password, true);
    }
}
```

| # | Giải thích |
|---|---|
| ⑤ | **Luôn gọi `base`** trước. Base kiểm tra NotNull, kích thước và quyền. |
| ⑥ | `Row.IsAssigned(field)`: client **có gửi** field này lên không. Khi Update, client có thể không gửi Password (người dùng không đổi mật khẩu), nên phải kiểm tra trước. Mật khẩu đã kiểm tra được lưu vào biến `password` của handler để bước sau dùng. |
| ⑦ | Chỉ kiểm tra trùng username **khi username thật sự đổi**. `Old` là bản ghi trong DB trước khi sửa. Truyền `existingUserId` để không báo trùng với chính nó. |
| ⑧ | Khi tạo mới thì kiểm tra hết. Lúc này không có `Old` (nó là `null`). **Đừng dùng `Old` khi `IsCreate`.** |

### 2.3 Kiểm tra trùng bằng truy vấn

```csharp
var existing = GetUser(connection,
    new Criteria(fld.Username) == username |
    new Criteria(fld.Username) == username.Replace('I', 'İ'));        // ⑨

if (existing != null && existingUserId != existing.UserId)
    throw new ValidationError("UniqueViolation", "Username",
        "A user with same name exists. Please choose another!");
```

⑨ Điều kiện OR viết bằng `|`. Dòng `Replace('I', 'İ')` xử lý một trường hợp riêng của tiếng Thổ Nhĩ Kỳ
(tác giả Serenity người Thổ). `GetUser` dùng `new SqlQuery().From(row).Select(...).Where(filter).GetFirst(connection)`,
tức là truy vấn thủ công và chỉ chọn vài cột.

### 2.4 `SetInternalFields`: gán field hệ thống

```csharp
protected override void SetInternalFields()
{
    base.SetInternalFields();

    if (IsCreate)
    {
        Row.Source = "site";                     // ⑩
        Row.IsActive = Row.IsActive ?? 1;
    }

    if (IsCreate || !Row.Password.IsEmptyOrNull())
    {
        string salt = null;
        Row.PasswordHash = GenerateHash(password, ref salt);   // ⑪
        Row.PasswordSalt = salt;
    }
}
```

| # | Giải thích |
|---|---|
| ⑩ | Field người dùng không nhập: nguồn tạo, trạng thái mặc định. |
| ⑪ | **Không bao giờ lưu mật khẩu thô.** Ở đây sinh salt rồi băm SHA512, chỉ lưu `PasswordHash` và `PasswordSalt`. Field `Password` trong `UserRow` là `NotMapped`, tức là chỉ để nhận dữ liệu từ form. |

### 2.5 `AfterSave`: làm mới cache

```csharp
protected override void AfterSave()
{
    base.AfterSave();
    BatchGenerationUpdater.OnCommit(this.UnitOfWork, fld.GenerationKey);   // ⑫
}
```

⑫ Sau khi transaction **commit thành công**, đánh dấu cache của bảng Users là cũ (dùng cho lookup và cho
thông tin user đang đăng nhập). `OnCommit` nghĩa là nếu transaction rollback thì không làm gì.

## 3. `MyDeleteHandler`: dọn dữ liệu con trước khi xóa

```csharp
protected override void OnBeforeDelete()
{
    base.OnBeforeDelete();

    new SqlDelete(UserPreferenceRow.Fields.TableName)
        .Where(UserPreferenceRow.Fields.UserId == Row.UserId.Value)
        .Execute(Connection, ExpectedRows.Ignore);                 // ⑬

    new SqlDelete(Entities.UserRoleRow.Fields.TableName)
        .Where(Entities.UserRoleRow.Fields.UserId == Row.UserId.Value)
        .Execute(Connection, ExpectedRows.Ignore);

    new SqlDelete(Entities.UserPermissionRow.Fields.TableName)
        .Where(Entities.UserPermissionRow.Fields.UserId == Row.UserId.Value)
        .Execute(Connection, ExpectedRows.Ignore);
}
```

⑬ Phải xóa các bảng con có khóa ngoại tới Users trước, nếu không DB sẽ báo lỗi FK. **`ExpectedRows.Ignore`
là bắt buộc** ở đây, vì mặc định `Execute` chỉ chấp nhận đúng 1 dòng bị ảnh hưởng, mà user có thể có 0 hoặc
nhiều role. Tất cả chạy trong **cùng transaction** với lệnh xóa user.

## 4. Sơ đồ tổng kết

```
Create/Update request
  │
  ├─ GetEditableFields   → loại bỏ field user không được sửa (theo quyền)
  ├─ ValidateRequest     → kiểm tra username, display name, mật khẩu (dùng Old khi Update)
  ├─ SetInternalFields   → Source, IsActive, băm mật khẩu
  ├─ (INSERT / UPDATE)
  └─ AfterSave           → làm mới cache khi commit

Delete request
  ├─ ValidateRequest     → chặn xóa trên bản demo
  ├─ OnBeforeDelete      → xóa UserPreferences, UserRoles, UserPermissions
  └─ (DELETE Users)
```

## 5. Tự kiểm tra

1. Vì sao trong nhánh `IsUpdate` phải kiểm tra `Row.IsAssigned(fld.Password)`?
2. Nếu quên `ExpectedRows.Ignore` ở ⑬ thì xóa một user có 3 role sẽ xảy ra chuyện gì?
3. Thêm luật: username không được chứa chữ "admin" (trừ user `admin` có sẵn). Bạn sẽ thêm code vào hàm nào?
