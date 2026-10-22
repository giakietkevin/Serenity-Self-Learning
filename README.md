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
| 01 | [Kiến trúc và cấu trúc một module](lessons/01-kien-truc-va-cau-truc-module.md) | ✅ |
| 02 | [Tạo bảng bằng FluentMigrator](lessons/02-fluentmigrator.md) | ✅ |
| 03 | [Sinh code bằng Sergen](lessons/03-sergen.md) | ✅ |
| 04 | [Tùy biến Grid, Form, Columns](lessons/04-tuy-bien-grid-form-columns.md) | ✅ |
| 05 | [Enum, Lookup, Editor và khóa ngoại](lessons/05-lookup-editor-khoa-ngoai.md) | ✅ |
| 06 | [Nghiệp vụ trong Repository (Request Handler)](lessons/06-nghiep-vu-trong-repository.md) | ✅ |
| 07 | [Phân quyền (Permission) và Navigation](lessons/07-phan-quyen-va-navigation.md) | ✅ |
| 08 | [Master–detail và quan hệ nhiều–nhiều](lessons/08-master-detail.md) | ✅ |

## Code mẫu và phân tích code

- [samples/](samples/README.md): bộ code đầy đủ của module **MovieDB** (ví dụ xuyên suốt bài 02 tới 08), gồm
  migration, Row, Repository, Endpoint, Form, Columns, Grid, Dialog, quyền và menu.
- [walkthroughs/](walkthroughs/README.md): phân tích từng đoạn code thật trong Serene 3.13.7 (`ProductRow`,
  `UserRepository`, `OrderGrid`/`OrderDialog`, master–detail Order) và của bộ code mẫu.
