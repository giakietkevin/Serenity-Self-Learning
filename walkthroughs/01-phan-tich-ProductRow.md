# Phân tích code 01 – `ProductRow.cs`: một entity "đầy đủ đồ chơi"

**File gốc:** [Serene.Web/Modules/Northwind/Product/ProductRow.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Product/ProductRow.cs) (Serene 3.13.7)

**Liên quan bài:** 01 (Row), 05 (khóa ngoại, lookup), 07 (quyền)

`ProductRow` đáng đọc kỹ vì một file này gom gần hết các kỹ thuật của Row: lookup, upload ảnh, khóa ngoại
tới 2 bảng, view field dùng `Origin`, log thay đổi và đa ngôn ngữ.

---

## 1. Phần khai báo class

```csharp
[ConnectionKey("Northwind"), Module("Northwind"), TableName("Products")]   // ①
[DisplayName("Products"), InstanceName("Product")]                         // ②
[ReadPermission(PermissionKeys.General)]                                   // ③
[ModifyPermission(PermissionKeys.General)]
[LookupScript]                                                             // ④
[CaptureLog(typeof(ProductLogRow))]                                        // ⑤
[LocalizationRow(typeof(ProductLangRow))]                                  // ⑥
public sealed class ProductRow : Row, IIdRow, INameRow                     // ⑦
```

| # | Giải thích |
|---|---|
| ① | Dùng connection `Northwind` trong `Web.config`, thuộc module `Northwind`, ánh xạ tới bảng `Products`. `Module` quyết định `LocalTextPrefix` = `Northwind.Product` và lookup key mặc định. |
| ② | Tiêu đề trang là "Products", nút "New Product", tiêu đề dialog "Edit Product". |
| ③ | `PermissionKeys.General` = `"Northwind:General"` (xem `NorthwindPermissionKeys.cs`). Ai có quyền này thì được đọc và sửa. |
| ④ | `[LookupScript]` **không có tham số**, nên key tự sinh là `Northwind.Product`. Nhờ vậy client gọi được `ProductRow.getLookup()`, như trong `OrderDetailDialog.ts`. |
| ⑤ | **Ghi lịch sử thay đổi.** Mỗi lần thêm, sửa hay xóa, một phiên bản của bản ghi được ghi vào bảng `ProductLog` (`ProductLogRow`), kèm loại thao tác (`OperationType`), người sửa (`ChangingUserId`) và khoảng hiệu lực (`ValidFrom`/`ValidUntil`). Đây là tính năng có sẵn, không cần viết code. |
| ⑥ | Hỗ trợ **dịch dữ liệu** (tên sản phẩm theo ngôn ngữ) qua bảng `ProductLangRow`. |
| ⑦ | `sealed`: Row không được kế thừa. `IIdRow` cho biết field khóa, `INameRow` cho biết field tên. |

## 2. Field thường và các attribute hay gặp

```csharp
[DisplayName("Product Id"), Identity, LookupInclude]                       // ⑧
public Int32? ProductID { ... }

[DisplayName("Product Name"), Size(40), NotNull, QuickSearch, LookupInclude]
public String ProductName { ... }

[DisplayName("Product Image"), Size(100)]
[ImageUploadEditor(FilenameFormat = "ProductImage/~", CopyToHistory = true)]   // ⑨
public String ProductImage { ... }

[DisplayName("Discontinued"), NotNull]                                     // ⑩
public Boolean? Discontinued { ... }

[DisplayName("Unit Price"), Scale(4), LookupInclude]                       // ⑪
public Decimal? UnitPrice { ... }

[DisplayName("Units In Stock"), NotNull, DefaultValue(0), LookupInclude]   // ⑫
public Int16? UnitsInStock { ... }
```

| # | Giải thích |
|---|---|
| ⑧ | `Identity`: cột tự tăng, không cho nhập. `LookupInclude`: **đưa thêm field này vào lookup script**. Mặc định lookup chỉ chứa Id và Name. |
| ⑨ | Field kiểu chuỗi nhưng editor là **upload ảnh**. DB chỉ lưu đường dẫn file. `FilenameFormat = "ProductImage/~"` nghĩa là lưu file vào thư mục `ProductImage/`, còn `~` là tên file tự sinh. `CopyToHistory` giữ lại file cũ khi đổi ảnh. |
| ⑩ | Mọi property đều có kiểu **nullable** (`Boolean?`, `Int32?`), kể cả cột NOT NULL. Lý do: Serenity cần phân biệt "chưa gán" với "gán giá trị", ví dụ khi Update chỉ gửi một vài field. |
| ⑪ | `Scale(4)` là số chữ số thập phân. `LookupInclude` giúp client lấy được đơn giá. `OrderDetailDialog.ts` dùng nó để tự điền giá khi chọn sản phẩm: `ProductRow.getLookup().itemById[id].UnitPrice`. |
| ⑫ | `DefaultValue(0)`: giá trị mặc định khi thêm mới, áp dụng cả trên form lẫn ở server nếu client không gửi lên. |

## 3. Khóa ngoại tới 2 bảng

```csharp
[DisplayName("Supplier"), ForeignKey(typeof(SupplierRow)), LeftJoin("sup")]   // ⑬
[LookupEditor(typeof(SupplierRow), InplaceAdd = true)]                       // ⑭
public Int32? SupplierID { ... }

[DisplayName("Category"), ForeignKey(typeof(CategoryRow)), LeftJoin("cat"), LookupInclude]
[LookupEditor(typeof(CategoryRow), InplaceAdd = true)]
public Int32? CategoryID { ... }
```

| # | Giải thích |
|---|---|
| ⑬ | `ForeignKey(typeof(SupplierRow))` đọc tên bảng và khóa từ `SupplierRow`, an toàn hơn viết chuỗi. `LeftJoin("sup")` tạo alias `sup`, và các view field bên dưới sẽ dùng alias này. |
| ⑭ | Dropdown chọn nhà cung cấp, lấy từ lookup của `SupplierRow`. `InplaceAdd` thêm nút tạo nhanh, mở `SupplierDialog`. |

## 4. View field dùng `Origin` (cách viết gọn của `Expression`)

```csharp
[Origin("sup"), DisplayName("Supplier"), LookupInclude]       // ⑮
public String SupplierCompanyName { ... }

[Origin("sup")]                                               // ⑯
public String SupplierContactName { ... }
// ... SupplierAddress, SupplierCity, SupplierCountry, SupplierPhone...

[Origin("cat"), DisplayName("Category")]
public String CategoryName { ... }
```

| # | Giải thích |
|---|---|
| ⑮ | `Origin("sup")` nghĩa là field này lấy từ join `sup`. Serenity tự suy ra cột: bỏ tiền tố `Supplier` (lấy từ tên field khóa ngoại `SupplierID`) khỏi tên property, được `CompanyName`. Kết quả **tương đương** `Expression("sup.CompanyName")`. |
| ⑯ | Với `Origin`, Serenity còn **lấy luôn `DisplayName` và `Size`** từ property gốc trong `SupplierRow`, nên không cần khai báo lại. Viết `Expression` thì không được hưởng điều này. |

**SQL sinh ra** khi grid hiện cột `SupplierCompanyName` và `CategoryName`:

```sql
SELECT T0.ProductID, T0.ProductName, ..., sup.CompanyName AS SupplierCompanyName, cat.CategoryName AS CategoryName
FROM Products T0
LEFT JOIN Suppliers sup ON (sup.SupplierID = T0.SupplierID)
LEFT JOIN Categories cat ON (cat.CategoryID = T0.CategoryID)
```

Join nào không cần (không có field nào của nó được chọn) thì **sẽ không xuất hiện** trong SQL.

## 5. Phần cuối: interface và RowFields

```csharp
IIdField IIdRow.IdField { get { return Fields.ProductID; } }
StringField INameRow.NameField { get { return Fields.ProductName; } }

public static readonly RowFields Fields = new RowFields().Init();    // ⑰

public ProductRow() : base(Fields) { }

public class RowFields : RowFieldsBase                               // ⑱
{
    public Int32Field ProductID;
    public StringField ProductName;
    // ...
    public Int16Field UnitsInStock;           // Int16? ↔ Int16Field
    public BooleanField Discontinued;         // Boolean? ↔ BooleanField
    public StringField SupplierCompanyName;   // view field cũng phải khai báo
    public StreamField CategoryPicture;       // Stream ↔ StreamField
}
```

| # | Giải thích |
|---|---|
| ⑰ | `Fields` là **static, tạo một lần** cho cả class. `Init()` đọc attribute của mọi property để dựng metadata (tên cột, kích thước, join...). |
| ⑱ | Mỗi property phải có **một field cùng tên, đúng kiểu**: `Int32Field`, `Int16Field`, `StringField`, `DecimalField`, `BooleanField`, `DateTimeField`, `StreamField`, `ListField<T>`, `RowListField<T>`. Sai tên hoặc thiếu thì app lỗi ngay khi khởi động. |

## 6. Rút ra

- Row **khai báo** (declarative). Hầu hết hành vi như join, lookup, upload, log hay đa ngôn ngữ đều bật
  bằng attribute, không cần viết code.
- Dùng `ForeignKey(typeof(...))` + `LeftJoin("alias")` + `Origin("alias")` cho view field: ngắn gọn và tự
  kế thừa nhãn và kích thước.
- Muốn client đọc thêm thông tin từ lookup thì thêm `LookupInclude`.

## 7. Tự kiểm tra

1. Nếu bỏ `LookupInclude` trên `UnitPrice` thì chỗ nào trong Serene sẽ hỏng?
2. Viết lại `SupplierCountry` bằng `Expression(...)` thay vì `Origin`.
3. Tìm `ProductLogRow.cs` và đoán xem bảng log có những cột nào.
