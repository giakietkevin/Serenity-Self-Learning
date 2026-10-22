# Bài 09 – Đa ngôn ngữ (Localization)

**Mục tiêu:** hiểu cách Serenity 3.13.7 dịch giao diện (nhãn, nút, thông báo) và dịch **dữ liệu** (tên sản
phẩm theo từng ngôn ngữ). Tự thêm bản dịch tiếng Việt cho module MovieDB.

Tham khảo:
- [Serenity.Web/Common/CommonInitialization.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Common/CommonInitialization.cs): nơi nạp bản dịch lúc khởi động
- [Serene.Web/Modules/Texts.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Texts.cs), [Global.asax.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Global.asax.cs)
- [Scripts/site/texts/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Scripts/site/texts): file JSON bản dịch (đã có sẵn `vi-VN`)
- Guide: `localization/`, `serene_tour/localization.md`

---

## 1. Ý tưởng chính: mọi chữ trên giao diện là một **key**

Serenity không viết cứng chữ "Product Name" lên giao diện. Nó dùng một **local text key**, ví dụ
`Db.Northwind.Product.ProductName`, rồi tra key đó theo ngôn ngữ đang chọn.

```
Key                                   en (invariant)      vi-VN
Db.Northwind.Product.ProductName  →   "Product Name"   →  "Tên sản phẩm"
Dialogs.OkButton                  →   "OK"             →  "Đồng ý"
Enums.MovieDB.MovieKind.Film      →   "Film"           →  "Phim lẻ"
```

Nếu không tìm thấy bản dịch, Serenity trả về **chính cái key**. Đây là cố ý: nhìn thấy chữ
`Db.MovieDB.Movie.Title` trên màn hình là biết ngay key nào đang thiếu bản dịch.

## 2. Ngôn ngữ hiện tại được chọn thế nào

Có hai nơi quyết định, theo thứ tự ưu tiên:

**a. Cookie `LanguagePreference`** (khi người dùng chọn ngôn ngữ ở menu cài đặt góc phải). Code trong
`Global.asax.cs`:

```csharp
protected void Application_BeginRequest(object sender, EventArgs e)
{
    var cookie = Request.Cookies["LanguagePreference"];
    if (cookie != null && !string.IsNullOrEmpty(cookie.Value))
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(cookie.Value);
            Thread.CurrentThread.CurrentUICulture = culture;     // ← ngôn ngữ của request này
        }
        catch (CultureNotFoundException) { }
    }
}
```

**b. `Web.config`** khi chưa có cookie:

```xml
<globalization culture="auto:en-US" uiCulture="auto:en-US" />
```

`auto:en-US` nghĩa là lấy theo ngôn ngữ trình duyệt, nếu không xác định được thì dùng `en-US`. Muốn mặc định
tiếng Việt cho mọi người thì đặt `uiCulture="vi-VN"`.

Danh sách ngôn ngữ trong dropdown lấy từ bảng `Languages` (trang *Administration → Languages*). Migration
khởi tạo của Serene đã có sẵn `vi-VN`.

## 3. Bản dịch đến từ đâu

Khi app khởi động, `CommonInitialization.InitializeLocalTexts()` nạp bản dịch theo thứ tự sau. **Nguồn
nạp sau ghi đè nguồn nạp trước** nếu trùng key và trùng ngôn ngữ:

```csharp
NestedLocalTextRegistration.Initialize(...);   // ① class [NestedLocalTexts] (Texts.cs)
NestedPermissionKeyRegistration...;            //    tên quyền
EnumLocalTextRegistration.Initialize(...);     // ② [Description] trên enum
EntityLocalTexts.Initialize();                 // ③ [DisplayName] trên Row
JsonLocalTextRegistration.AddFromFilesInFolder("~/Scripts/serenity/texts/");  // ④ bản dịch của Serenity
JsonLocalTextRegistration.AddFromFilesInFolder("~/Scripts/site/texts/");      // ⑤ bản dịch của app (bạn)
JsonLocalTextRegistration.AddFromFilesInFolder("~/App_Data/texts/");          // ⑥ bản dịch sửa trên giao diện
```

| # | Nguồn | Key sinh ra | Thường dùng để |
|---|---|---|---|
| ① | `Texts.cs` với `[NestedLocalTexts]` | theo tên class lồng nhau, ví dụ `Validation.NorthwindPhone` | Chữ mặc định (tiếng Anh) dùng trong code C# |
| ② | `[Description]` trên giá trị enum | `Enums.<EnumKey>.<Tên giá trị>` | Chữ của dropdown enum |
| ③ | `[DisplayName]` và `[InstanceName]` trên Row | `Db.<Module>.<Row>.<Property>`, `Db.<Module>.<Row>.EntityPlural` / `EntitySingular` | Nhãn field, tiêu đề trang |
| ④⑤ | File `*.json` | key ghi trong file | Bản dịch cho từng ngôn ngữ |
| ⑥ | `App_Data/texts/user.texts.<lang>.json` | key ghi trong file | Bản dịch sửa trên trang *Translations* |

①②③ là ngôn ngữ **invariant** (mặc định, thường là tiếng Anh), đóng vai trò bản dự phòng cuối cùng.

### Quy tắc đặt tên file JSON

`<tên bất kỳ>.<mã ngôn ngữ>.json`. Phần sau dấu chấm cuối cùng là mã ngôn ngữ, `invariant` nghĩa là ngôn
ngữ mặc định. Thư mục con cũng được quét, nên Serene tách theo module: `Scripts/site/texts/northwind/northwind.vi-VN.json`.

```json
{
  "Db.Northwind.Category.CategoryName": "Tên nhóm hàng",
  "Db.Northwind.Category.EntityPlural": "Nhóm hàng"
}
```

### Thứ tự dự phòng khi tra key

Ví dụ đang chọn `vi-VN`: tìm trong `vi-VN` → nếu không có thì `vi` → nếu không có thì invariant → nếu vẫn
không có thì trả về key.

## 4. Dùng bản dịch trong code

**C# (server):**

```csharp
// Cách 1: dùng class Texts (có intellisense)
throw new ValidationError(String.Format(Texts.Validation.DeleteForeignKeyError, fk.TableName));

// Cách 2: dùng key dạng chuỗi
var title = LocalText.Get("Db.MovieDB.Movie.EntityPlural");

// TryGet trả về null nếu không có bản dịch, thay vì trả về key
var s = LocalText.TryGet("Site.MovieDB.Hint") ?? "Mặc định";

// Chữ của enum
var text = MovieKind.Film.GetText();     // cần using Serenity;
```

Khai báo key mới cho module của mình theo mẫu `NorthwindTexts.cs` (dùng `partial`):

```csharp
namespace Serene
{
    public static partial class Texts
    {
        public static partial class Validation
        {
            public static LocalText MovieTitleExists = "Đã có phim với tên này!";
        }
    }
}
```

**TypeScript (client):**

```ts
Q.text('Dialogs.OkButton')                         // → "OK" hoặc "Đồng ý"
Q.format(Q.text('Site.RolePermissionDialog.DialogTitle'), roleName)
Q.tryGetText('Site.MovieDB.Hint')                  // null nếu không có
Texts.Validation.NorthwindPhone                    // typing do T4 sinh trong Imports/ServerTypings/Texts.ts
```

> ⚠️ **Client chỉ nhận những key nằm trong "gói" (package)**. Gói `Site` được khai báo trong `Web.config`:
>
> ```xml
> <add key="LocalTextPackages" value="{ Site: [ 'Controls.', 'Db.', 'Dialogs.', 'Enums.', 'Forms.',
>      'Permission.', 'Site.', 'Validation.' ], ... }" />
> ```
>
> Key bắt đầu bằng một trong các tiền tố này mới được gửi xuống trình duyệt (qua
> `@Html.LocalTextScript("Site")` trong `_LayoutHead.cshtml`). Nếu đặt key là `MovieDB.Hint` thì
> `Q.text` sẽ không thấy. Hãy đặt key dưới `Site.` hoặc thêm tiền tố vào danh sách.

## 5. Dịch ngay trên giao diện: trang *Translations*

*Administration → Translations*: chọn ngôn ngữ nguồn và ngôn ngữ đích, gõ bản dịch, rồi bấm *Save Changes*.
Serene ghi vào `App_Data/texts/user.texts.<lang>.json` và nạp lại ngay.

> Trước khi deploy, nên **chép các bản dịch này sang `Scripts/site/texts/`** để đưa vào source control.
> Thư mục `App_Data` thường không được commit, và khi deploy có thể bị ghi đè.

## 6. Dịch **dữ liệu** (data localization)

Dịch nhãn thì chưa đủ nếu tên sản phẩm cũng cần có bản tiếng Việt và tiếng Anh. Serene làm việc này cho
Product và Category bằng một **bảng phụ** chứa bản dịch.

**B1. Bảng phụ** (`NorthwindDB_20160216_154900_DataLocalization.cs`):

```csharp
Create.Table("ProductLang")
    .WithColumn("ID").AsInt32().PrimaryKey().Identity().NotNullable()
    .WithColumn("ProductID").AsInt32().NotNullable()      // trỏ về bản ghi gốc
    .WithColumn("LanguageID").AsInt32().NotNullable()     // Id trong bảng Languages
    .WithColumn("ProductName").AsString(40).Nullable();   // cột cần dịch, CÙNG TÊN với cột gốc
```

**B2. Row của bảng phụ** phải implement `ILocalizationRow` và cho biết field ngôn ngữ:

```csharp
public sealed class ProductLangRow : Row, IIdRow, INameRow, ILocalizationRow
{
    // Id, ProductId, LanguageId, ProductName ...
    public Field CultureIdField
    {
        get { return Fields.LanguageId; }
    }
}
```

**B3. Gắn vào Row gốc:**

```csharp
[LocalizationRow(typeof(ProductLangRow))]
public sealed class ProductRow : Row, IIdRow, INameRow
```

**B4. Client:** `Modules/Common/ScriptInitialization.ts` đã gán sẵn danh sách ngôn ngữ cho mọi dialog:

```ts
Serenity.EntityDialog.defaultLanguageList = LanguageList.getValue;   // mọi ngôn ngữ trừ 'en'
```

Kết quả: dialog Product có thêm nút **Localization**. Bấm vào sẽ hiện một form để nhập bản dịch cho các
field "có thể dịch".

Cơ chế bên dưới:
- Field nào trong `ProductRow` có **field cùng tên** trong `ProductLangRow` thì tự được đánh dấu
  localizable (`LocalizablePropertyProcessor`). Có thể ép bằng `[Localizable(true)]` hoặc tắt bằng
  `[Localizable(false)]` trên form.
- Khi lưu, client gửi kèm `Localizations: { "<LanguageID>": { ProductName: "..." } }`.
  `LocalizationBehavior` (behavior **implicit**, tự kích hoạt khi Row có `[LocalizationRow]`) sẽ thêm hoặc
  sửa các dòng trong `ProductLang` ở `OnAfterSave`.
- Khi mở dialog ở chế độ dịch, client gọi Retrieve với `IncludeColumns: ['Localizations']` để lấy các bản
  dịch đã có.

> Trong Serene 3.13.7, `ProductRepository` vẫn còn đoạn `AfterSave` tự gọi `LocalizationRowHandler`. Đó là
> code từ trước khi có behavior. Khi viết module mới, chỉ cần các bước B1–B4.

## 7. Bài tập

1. Đổi ngôn ngữ sang Tiếng Việt trên menu cài đặt. Mở F12 → *Application → Cookies* để thấy
   `LanguagePreference = vi-VN`.
2. Tạo `Scripts/site/texts/moviedb/moviedb.vi-VN.json` dịch các nhãn của Movie (`Db.MovieDB.Movie.Title`,
   `Db.MovieDB.Movie.EntityPlural`, ...) và các giá trị enum (`Enums.MovieDB.MovieKind.Film`, ...). Khởi
   động lại app rồi kiểm tra.
3. Đổi thông báo lỗi "Đã có phim với tên này!" trong `MovieRepository` thành `Texts.Validation.MovieTitleExists`,
   rồi thêm bản tiếng Anh vào `site.texts.invariant.json`.
4. (Nâng cao) Làm data localization cho `GenreRow`: tạo bảng `mov.GenreLang`, `GenreLangRow`, gắn
   `[LocalizationRow]`, rồi thử nhập tên thể loại bằng tiếng Anh và tiếng Việt.
5. Ở dự án thật: tìm thư mục `Scripts/site/texts` và trang Translations. Dự án đang dịch theo cách nào?

## 8. Ghi chú của mình

-
