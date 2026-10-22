# Code mẫu: module MovieDB (Serenity 3.13.7, .NET Framework)

Đây là **bộ code hoàn chỉnh** của ví dụ xuyên suốt trong các bài 02 tới 08: quản lý phim gồm Movie, Genre
và quan hệ nhiều–nhiều MovieGenres. Mọi file đều viết theo đúng mẫu Sergen 3.13.7 (bản .NET Framework,
ASP.NET MVC 5) và các API có trong source 3.13.7.

> ⚠️ Bộ code này chưa được biên dịch thử trong Visual Studio. Nếu gặp lỗi khi chép vào project, hãy đọc
> thông báo lỗi rồi đối chiếu với bài học tương ứng. Tự sửa lỗi cũng là một cách học tốt.

Phần phân tích chi tiết của bộ code này nằm ở [walkthroughs/05-phan-tich-code-mau-moviedb.md](../walkthroughs/05-phan-tich-code-mau-moviedb.md).

## Cấu trúc

```
samples/MovieDB/
├── Migrations/DefaultDB/
│   ├── DefaultDB_20201022_100000_MovieTables.cs    # Bài 02: schema mov + 3 bảng
│   └── DefaultDB_20201022_100100_SampleGenres.cs   # Bài 02: dữ liệu mẫu
├── Modules/MovieDB/
│   ├── MovieDBPermissionKeys.cs                    # Bài 07
│   ├── MovieDBNavigation.cs                        # Bài 07
│   ├── Movie/
│   │   ├── MovieKind.cs                            # Bài 05: enum
│   │   ├── MovieRow.cs                             # Bài 01, 05, 08: entity
│   │   ├── MovieRepository.cs                      # Bài 06: nghiệp vụ
│   │   ├── MovieEndpoint.cs                        # Bài 01: API
│   │   ├── MovieForm.cs                            # Bài 04: form
│   │   ├── MovieColumns.cs                         # Bài 04: cột grid
│   │   ├── MoviePage.cs + MovieIndex.cshtml        # Bài 01: trang
│   │   ├── MovieGrid.ts                            # Bài 04: grid
│   │   └── MovieDialog.ts                          # Bài 04: dialog
│   ├── Genre/
│   │   ├── GenreRow.cs                             # Bài 05: LookupScript
│   │   └── GenreFiles.md                           # các file còn lại (giống mẫu Sergen)
│   └── MovieGenres/
│       └── MovieGenresRow.cs                       # Bài 08: bảng nối
└── Content/site/site.moviedb.less                  # Bài 04: kích thước dialog
```

## Cách dùng trên một project Serene 3.13.7 thử nghiệm

1. Tạo project mới từ template Serene 3.13.7 (VSIX), đặt tên **Serene** để khỏi phải đổi namespace. Nếu
   đặt tên khác thì find & replace `Serene.` thành `TenProject.` trong các file mẫu.
2. Chép `Migrations/DefaultDB/*.cs` vào `Serene.Web/Migrations/DefaultDB/`.
3. Chép thư mục `Modules/MovieDB` vào `Serene.Web/Modules/`.
4. Tạo phần còn lại cho Genre theo `Genre/GenreFiles.md`, hoặc chạy Sergen cho bảng `mov.Genre` sau khi
   migration đã chạy, rồi thay `GenreRow.cs` bằng file mẫu.
5. Chép `site.moviedb.less` vào `Content/site/`, sau đó thêm dòng `@import "site.moviedb.less";` vào
   `site.less`.
6. Trong Solution Explorer: *Show All Files* → chuột phải từng file mới → *Include In Project*.
7. **Build → Transform All T4 Templates → Build**, rồi F5.
8. Đăng nhập `admin` / `serenity`, vào *Administration → Roles* gán quyền `MovieDB:...` (admin đã có sẵn
   mọi quyền), rồi mở *Movie Database → Movies*.
