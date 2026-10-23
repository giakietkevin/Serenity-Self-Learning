# Bài 00 – Lộ trình học Serenity 3.13.7

Lộ trình bám theo tutorial **Movie Database** trong Serenity Guide cũ (bản 2020). Đây là tutorial chính thức,
nó đi từ tạo bảng cho tới master–detail. Mỗi bài sẽ có phần lý thuyết ngắn, phần đọc code mẫu trong Serene
và một bài tập để tự làm trong project của mình.

| # | Bài | Nội dung chính | Tham khảo trong Guide cũ |
|---|---|---|---|
| 00a | Khởi tạo dự án 3.13.7 trong Visual Studio | Cài VSIX Serene (ASP.NET MVC), nâng package lên 3.13.7, chạy lần đầu | `getting_started/` |
| 01 | Kiến trúc và cấu trúc một module | Các lớp Row, Repository, Endpoint, Form, Columns, Grid, Dialog và luồng request | `serene_tour/`, `entities/`, `services/` |
| 02 | Tạo bảng bằng FluentMigrator | Viết migration, đặt tên, chạy migration lúc app khởi động | `tutorials/movies/creating_movie_table.md` |
| 03 | Sinh code bằng Sergen | Dùng Sergen để sinh code module từ bảng, hiểu từng file được sinh ra | `tutorials/movies/generating_code_for_movie_table.md`, `sergen/` |
| 04 | Tùy biến Grid, Form, Columns | Đổi độ rộng cột, thứ tự field, Category/Tab trên form, QuickSearch | `customizing_movie_interface.md`, `customizing_quick_search.md` |
| 05 | Lookup, Editor và khóa ngoại | `[LookupScript]`, `[LookupEditor]`, `ForeignKey`/`LeftJoin`, enum | `adding_a_movie_kind_field.md`, `adding_movie_genres.md` |
| 06 | Nghiệp vụ trong Repository | Override `SetInternalFields`, `ValidateRequest`, `AfterSave`, `PrepareQuery` | `services/` |
| 07 | Phân quyền và Navigation | `PermissionKeys`, `[ReadPermission]`, `NavigationItem` | `authentication/`, `handling_movie_navigation.md` |
| 08 | Master–detail | Bảng con trong dialog (`MasterDetailRelation`, grid editor) | `the_cast_and_characters_they_played.md` |

### Phần nâng cao (bổ sung sau khi rà soát lại theo Serenity Guide bản 3.x)

| # | Bài | Nội dung chính | Tham khảo trong Guide cũ |
|---|---|---|---|
| 09 | Đa ngôn ngữ (Localization) | Local text key, file JSON bản dịch, trang Translations, dịch dữ liệu bằng `[LocalizationRow]` | `localization/`, `serene_tour/localization.md` |
| 10 | Cache | `LocalCache`, `TwoLevelCache`, group key, khi nào phải tự làm mới cache | `caching/` |
| 11 | Upload file và ảnh | `ImageUploadEditor`, `MultipleImageUploadEditor`, đường đi của file, cấu hình khi deploy | `adding_primary_and_gallery_images.md` |
| 12 | Báo cáo, xuất Excel/PDF, nhập Excel | `ListExcel`, `[Report]`, wkhtmltopdf, `IDataOnlyReport`, import bằng EPPlus | Code mẫu trong Serene |
| 13 | Tự viết Formatter, Editor, Filter | Formatter/editor TypeScript, cascade, custom `ListRequest`, cột checkbox | `allowing_multiple_genre_selection.md`, `filtering_with_multiple_genre_list.md` |
| 14 | Service Behavior và Multi-tenancy | Behavior có sẵn (`[Unique]`, `IInsertLogRow`...), tự viết behavior, tách dữ liệu theo tenant | `tutorials/multi_tenancy/` |
| 15 | Cấu hình, bundling, triển khai IIS | `appSettings`, migration trên production, bundling, publish, đọc lỗi | `howto/how_to_enable_script_bundling.md` |

Thứ tự gợi ý: 09 → 13 dùng hằng ngày nên học trước; 14 và 15 cần khi bắt đầu đụng tới kiến trúc và deploy.

Những phần **không** đưa vào khóa vì ít dùng hoặc thuộc bản trả phí: đăng nhập LDAP/Active Directory
(`howto/how_to_authenticate_with_active_directory_or_ldap.md`), các database khác SQL Server
(PostgreSQL, MySQL, Oracle) và các tính năng của StartSharp.

## Cách học mỗi bài

1. Đọc file bài giảng trong `lessons/`.
2. Mở code mẫu tương ứng trong Serene 3.13.7, đã có link trong bài.
3. Làm phần **Bài tập** ở cuối bài, trên project thật hoặc một project Serene tạo mới.
4. Ghi lại những gì còn vướng vào mục **Ghi chú của mình** ở cuối file. Lần sau mở lại sẽ nhớ nhanh hơn.
