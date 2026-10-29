# Serenity Self-Learning

Repo tự học **Serenity Platform (serenity.is) phiên bản 3.13.7** trên **.NET Framework 4.5 / ASP.NET MVC 5**,
đúng với phiên bản đang dùng ở dự án. Mỗi bài giảng là một file Markdown trong `lessons/` để sau này
đọc lại và review.

> Lưu ý: tài liệu trên serenity.is hiện tại viết cho .NET Core (bản 6.x trở lên), cấu trúc file khác
> nhiều so với 3.13.7. Khi tra cứu, hãy dùng các link "đóng băng" bên dưới, đã trỏ đúng commit của bản cũ.

## Nguồn tham khảo đúng phiên bản

| Nguồn | Commit | Ghi chú |
|---|---|---|
| Serenity (framework) | [`7cffedf` – ":up: 3.13.7", 21/07/2020](https://github.com/serenity-is/Serenity/tree/7cffedf30d14032ea0c3415f00ec36703ad482a5) | Source code framework, `CHANGELOG.md` |
| Serene (template mẫu) | [`4769edd` – ":up: 3.13.7.0", 05/08/2020](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c) | Project mẫu: `Serene/Serene.Web` là bản .NET Framework, `Serene/Serene.Core` là bản .NET Core |
| Serenity Guide (tài liệu cũ) | [`6d94504`, 05/08/2020](https://github.com/volkanceylan/Serenity-Guide/tree/6d945041646b74c8a95663e940c57508cca29ddc) | Tài liệu gốc thời 3.x, xem `SUMMARY.md` |

Nếu muốn xem offline, có thể clone rồi checkout về đúng commit:

```bash
git clone https://github.com/serenity-is/Serene && cd Serene
git checkout 4769edde155c4bf35f180136dd20dcf03a50310c
```

## Lộ trình

Xem [lessons/00-lo-trinh.md](lessons/00-lo-trinh.md).

| # | Bài | Trạng thái |
|---|---|---|
| 00a | [Khởi tạo dự án 3.13.7 trong Visual Studio](lessons/00a-khoi-tao-du-an.md) | ✅ |
| 01 | [Kiến trúc và cấu trúc một module](lessons/01-kien-truc-va-cau-truc-module.md) | ✅ |
| 02 | [Tạo bảng bằng FluentMigrator](lessons/02-fluentmigrator.md) | ✅ |
| 03 | [Sinh code bằng Sergen](lessons/03-sergen.md) | ✅ |
| 04 | [Tùy biến Grid, Form, Columns](lessons/04-tuy-bien-grid-form-columns.md) | ✅ |
| 05 | [Enum, Lookup, Editor và khóa ngoại](lessons/05-lookup-editor-khoa-ngoai.md) | ✅ |
| 06 | [Nghiệp vụ trong Repository (Request Handler)](lessons/06-nghiep-vu-trong-repository.md) | ✅ |
| 07 | [Phân quyền (Permission) và Navigation](lessons/07-phan-quyen-va-navigation.md) | ✅ |
| 08 | [Master–detail và quan hệ nhiều–nhiều](lessons/08-master-detail.md) | ✅ |
| 09 | [Đa ngôn ngữ (Localization)](lessons/09-da-ngon-ngu-localization.md) | ✅ |
| 10 | [Cache: LocalCache, TwoLevelCache](lessons/10-cache.md) | ✅ |
| 11 | [Upload file và ảnh](lessons/11-upload-file-va-anh.md) | ✅ |
| 12 | [Báo cáo, xuất Excel/PDF và nhập từ Excel](lessons/12-bao-cao-xuat-excel-pdf.md) | ✅ |
| 13 | [Tự viết Formatter, Editor và Filter](lessons/13-formatter-editor-filter-tu-viet.md) | ✅ |
| 14 | [Service Behavior và Multi-tenancy](lessons/14-service-behavior-va-multi-tenancy.md) | ✅ |
| 15 | [Cấu hình, bundling và triển khai lên IIS](lessons/15-cau-hinh-bundling-trien-khai-iis.md) | ✅ |
| 16 | [Fluent SQL, connection và transaction](lessons/16-fluent-sql-va-transaction.md) | ✅ |
| 17 | [Row chuyên sâu](lessons/17-row-chuyen-sau.md) | ✅ |
| 18 | [Tự viết service không phải CRUD](lessons/18-service-tu-viet.md) | ✅ |
| 19 | [Widget và vòng đời Dialog](lessons/19-widget-va-vong-doi-dialog.md) | ✅ |
| 20 | [Grid nâng cao](lessons/20-grid-nang-cao.md) | ✅ |
| 21 | [Kiến trúc nền và xác thực](lessons/21-kien-truc-nen-va-xac-thuc.md) | ✅ |

## Code mẫu và phân tích code

- [samples/](samples/README.md): bộ code đầy đủ của module **MovieDB** (ví dụ xuyên suốt bài 02 tới 08), gồm
  migration, Row, Repository, Endpoint, Form, Columns, Grid, Dialog, quyền và menu.
- [walkthroughs/](walkthroughs/README.md): phân tích từng đoạn code thật trong Serene 3.13.7 (`ProductRow`,
  `UserRepository`, `OrderGrid`/`OrderDialog`, master–detail Order) và của bộ code mẫu.
