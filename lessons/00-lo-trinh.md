# Bài 00 – Lộ trình học Serenity 3.13.7

Lộ trình bám theo tutorial **Movie Database** trong Serenity Guide cũ (bản 2020). Đây là tutorial chính thức,
nó đi từ tạo bảng cho tới master–detail. Mỗi bài sẽ có phần lý thuyết ngắn, phần đọc code mẫu trong Serene
và một bài tập để tự làm trong project của mình.

| # | Bài | Nội dung chính | Tham khảo trong Guide cũ |
|---|---|---|---|
| 01 | Kiến trúc và cấu trúc một module | Các lớp Row, Repository, Endpoint, Form, Columns, Grid, Dialog và luồng request | `serene_tour/`, `entities/`, `services/` |
| 02 | Tạo bảng bằng FluentMigrator | Viết migration, đặt tên, chạy migration lúc app khởi động | `tutorials/movies/creating_movie_table.md` |
| 03 | Sinh code bằng Sergen | Dùng Sergen để sinh code module từ bảng, hiểu từng file được sinh ra | `tutorials/movies/generating_code_for_movie_table.md`, `sergen/` |
| 04 | Tùy biến Grid, Form, Columns | Đổi độ rộng cột, thứ tự field, Category/Tab trên form, QuickSearch | `customizing_movie_interface.md`, `customizing_quick_search.md` |
| 05 | Lookup, Editor và khóa ngoại | `[LookupScript]`, `[LookupEditor]`, `ForeignKey`/`LeftJoin`, enum | `adding_a_movie_kind_field.md`, `adding_movie_genres.md` |
| 06 | Nghiệp vụ trong Repository | Override `SetInternalFields`, `ValidateRequest`, `AfterSave`, `PrepareQuery` | `services/` |
| 07 | Phân quyền và Navigation | `PermissionKeys`, `[ReadPermission]`, `NavigationItem` | `authentication/`, `handling_movie_navigation.md` |
| 08 | Master–detail | Bảng con trong dialog (`MasterDetailRelation`, grid editor) | `the_cast_and_characters_they_played.md` |

Sau bài 08, có thể học tiếp theo nhu cầu dự án: Localization, Multi-tenancy, upload file và Report.

## Cách học mỗi bài

1. Đọc file bài giảng trong `lessons/`.
2. Mở code mẫu tương ứng trong Serene 3.13.7, đã có link trong bài.
3. Làm phần **Bài tập** ở cuối bài, trên project thật hoặc một project Serene tạo mới.
4. Ghi lại những gì còn vướng vào mục **Ghi chú của mình** ở cuối file. Lần sau mở lại sẽ nhớ nhanh hơn.
