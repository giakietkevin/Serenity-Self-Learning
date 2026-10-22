# Bài 11 – Upload file và ảnh

**Mục tiêu:** thêm ảnh đại diện và bộ sưu tập ảnh cho Movie. Hiểu đường đi của một file từ lúc chọn trên
trình duyệt tới lúc nằm trong thư mục `App_Data/upload`, và các lỗi hay gặp khi deploy.

Tham khảo:
- [ImageUploadEditorAttribute.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Core/ComponentModel/Upload/ImageUploadEditorAttribute.cs): toàn bộ tùy chọn
- [ImageUploadBehavior.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Upload/ImageUploadBehavior.cs), [MultipleImageUploadBehavior.cs](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.Web/Upload/MultipleImageUploadBehavior.cs)
- [Serene: Modules/Common/File/FilePage.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/File/FilePage.cs)
- Guide: `tutorials/movies/adding_primary_and_gallery_images.md`

---

## 1. Bốn editor upload

| Attribute | Lưu trong DB | Dùng cho |
|---|---|---|
| `[ImageUploadEditor]` | Một đường dẫn: `Movie/PrimaryImage/00000/00000001_6l55nk6v2tiyi.jpg` | Một ảnh |
| `[MultipleImageUploadEditor]` | Chuỗi JSON: `[{"Filename":"...","OriginalName":"a.jpg"}, ...]` | Nhiều ảnh |
| `[FileUploadEditor]` | Như `ImageUploadEditor` nhưng cho phép file không phải ảnh (`AllowNonImage = true`) | Một file bất kỳ |
| `[MultipleFileUploadEditor]` | JSON như trên | Nhiều file |

DB **chỉ lưu đường dẫn**, file thật nằm trên đĩa. Vì vậy cột chỉ cần kiểu chuỗi.

## 2. Làm theo: ảnh cho Movie

**B1. Migration** (bài 02): luôn tạo migration mới, không sửa migration cũ.

```csharp
[Migration(20201101100000)]
public class DefaultDB_20201101_100000_MovieImages : Migration
{
    public override void Up()
    {
        Alter.Table("Movie").InSchema("mov")
            .AddColumn("PrimaryImage").AsString(100).Nullable()
            .AddColumn("GalleryImages").AsString(int.MaxValue).Nullable();
    }

    public override void Down() { }
}
```

**B2. Row:**

```csharp
[DisplayName("Primary Image"), Size(100)]
[ImageUploadEditor(FilenameFormat = "Movie/PrimaryImage/~")]
public String PrimaryImage
{
    get { return Fields.PrimaryImage[this]; }
    set { Fields.PrimaryImage[this] = value; }
}

[DisplayName("Gallery Images")]
[MultipleImageUploadEditor(FilenameFormat = "Movie/GalleryImages/~")]
public String GalleryImages
{
    get { return Fields.GalleryImages[this]; }
    set { Fields.GalleryImages[this] = value; }
}

// trong RowFields:
public StringField PrimaryImage;
public StringField GalleryImages;
```

**B3. Form:** thêm `public String PrimaryImage { get; set; }` và `public String GalleryImages { get; set; }`
vào `MovieForm.cs`.

**B4.** Build → Transform All T4 → Build, rồi mở dialog Movie. Không cần viết thêm dòng code nào để lưu file.

## 3. `FilenameFormat` và dấu `~`

`~` là viết tắt của `{1:00000}/{0:00000000}_{2}`:

| Tham số | Giá trị | Ví dụ |
|---|---|---|
| `{0}` | Id của bản ghi | `00000015` |
| `{1}` | Id / 1000, để mỗi thư mục không quá 1000 file | `00000` |
| `{2}` | Chuỗi ngẫu nhiên, đổi mỗi lần upload, tránh trình duyệt cache ảnh cũ | `6l55nk6v2tiyi` |
| `{3}` | Ngày giờ hiện tại | |
| `{4}` | Tên file gốc | |

Ví dụ phim Id 15: `App_Data/upload/Movie/PrimaryImage/00000/00000015_6l55nk6v2tiyi.jpg`.

Không ghi `FilenameFormat` thì mặc định là `<Tên Row>/<Tên field>/~`.

## 4. Đường đi của một file

```
① Người dùng chọn file trong dialog
     │  POST /File/TemporaryUpload            (FilePage.cs, Serene)
     ▼
② Server lưu vào App_Data/upload/temporary/abc123.jpg, kèm abc123.orig chứa tên gốc
     │  trả về { TemporaryFile: "temporary/abc123.jpg", IsImage, Width, Height }
     ▼
③ Editor giữ giá trị "temporary/abc123.jpg". Người dùng bấm Save.
     │  POST /Services/MovieDB/Movie/Create  { PrimaryImage: "temporary/abc123.jpg", ... }
     ▼
④ ImageUploadBehavior (implicit, tự chạy khi field có [ImageUploadEditor])
     - Kiểm tra giá trị phải bắt đầu bằng "temporary/" (chống giả mạo đường dẫn)
     - Kiểm tra kích thước, loại ảnh, tạo thumbnail nếu có cấu hình
     - Create: chờ tới OnAfterSave (cần Id cho {0}), rồi chép file sang Movie/PrimaryImage/00000/00000015_xxx.jpg
       và cập nhật lại cột
     - Update: chép file ngay ở OnBeforeSave, ghi nhận file cũ để xóa
     ▼
⑤ Transaction commit → xóa file cũ (nếu đổi ảnh). Rollback → file mới bị dọn.
     ▼
⑥ Hiển thị: GET /upload/Movie/PrimaryImage/00000/00000015_xxx.jpg   (FilePage.Read)
```

Ý nghĩa thực tế:
- **Không thể** gán `Row.PrimaryImage = "C:\\anh.jpg"` trong code rồi Save. Behavior sẽ báo lỗi
  *"For security reasons, only temporary files can be used in uploads!"*. Muốn gán file bằng code thì phải
  chép file vào thư mục `temporary/` trước, hoặc tự xử lý với `DisableDefaultBehavior = true`.
- Xóa bản ghi thì file cũng bị xóa (`OnAfterDelete`), trừ khi Row dùng xóa mềm (`IIsActiveDeletedRow`,
  `IIsDeletedRow`).
- `CopyToHistory = true` (như `ProductRow`, `UserRow`): trước khi xóa file cũ, chép một bản vào
  `upload/history/yyyyMM/`.

## 5. Các tùy chọn hay dùng

```csharp
[ImageUploadEditor(
    FilenameFormat = "Movie/PrimaryImage/~",
    MaxSize = 2 * 1024 * 1024,     // byte: tối đa 2MB
    MinWidth = 200, MaxWidth = 4000,
    ScaleWidth = 800, ScaleHeight = 600,   // tự thu nhỏ ảnh
    ThumbSizes = "128x96",                 // tạo thêm ảnh nhỏ: ..._t128x96.jpg
    OriginalNameProperty = "PrimaryImageName",  // lưu tên file gốc vào field khác
    CopyToHistory = true)]
```

```csharp
[FileUploadEditor(FilenameFormat = "Movie/Script/~", OriginalNameProperty = "ScriptFileName")]
public String ScriptFile { ... }
```

## 6. Cấu hình và triển khai

**Thư mục gốc** khai báo trong `Web.config`:

```xml
<add key="UploadSettings" value="{ Path: '~\\App_Data\\upload\\' }" />
```

Có thể trỏ ra ngoài thư mục web, ví dụ `D:\\SereneUpload\\`, để deploy lại không mất file.

**Giới hạn dung lượng** (Serene để sẵn khoảng 50MB):

```xml
<httpRuntime targetFramework="4.5" maxRequestLength="51200" />      <!-- KB, cho ASP.NET -->
<requestLimits maxAllowedContentLength="52428800" />                <!-- byte, cho IIS -->
```

Muốn cho upload file lớn hơn thì phải **sửa cả hai**, cùng với `MaxSize` trên attribute.

**Lỗi hay gặp trên IIS:**

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| Upload báo lỗi *Access to the path ... is denied* | Tài khoản app pool (ví dụ `IIS AppPool\Serene`) không có quyền **ghi** vào thư mục upload |
| Ảnh lưu được nhưng không hiện (404) | Thiếu dòng `<add name="SkipStaticFileForUploadFolder" path="/upload/*" .../>` trong `Web.config`, nên IIS tìm file tĩnh thay vì gọi `FilePage.Read` |
| Deploy xong mất hết ảnh | Thư mục upload nằm trong thư mục web và bị ghi đè khi publish |

## 7. Bài tập

1. Làm đủ mục 2 cho Movie. Upload thử rồi mở `App_Data/upload` xem cấu trúc thư mục.
2. Mở F12 → *Network*: tìm request `TemporaryUpload` và request `Create`. Giá trị `PrimaryImage` gửi lên là gì?
3. Đổi ảnh khác rồi Save. File cũ còn không? Bật `CopyToHistory` rồi thử lại.
4. Đặt `MaxSize` 100KB, upload ảnh lớn hơn và đọc thông báo lỗi.
5. Ở dự án thật: tìm `UploadSettings` trong `Web.config` của môi trường production. Thư mục đó nằm ở đâu và
   có được backup không?

## 8. Ghi chú của mình

-
