# Bài 03 – Sinh code bằng Sergen

**Mục tiêu:** dùng Sergen (Serenity Code Generator) để sinh trọn bộ file cho một module từ bảng
`mov.Movie`, biết từng file sinh ra nằm ở đâu, và biết những việc phải làm sau khi sinh code.

> Sergen của bản .NET Framework là **ứng dụng cửa sổ (WPF)**, mở bằng Package Manager Console. Tutorial
> trong Guide 2020 lại hướng dẫn `dotnet sergen g` (bản console dành cho .NET Core). Lệnh đó **không dùng
> cho dự án 3.13.7 .NET Framework**.

Tham khảo:
- [Serenity.CodeGenerator.Net45/MainWindow.xaml](https://github.com/serenity-is/Serenity/blob/7cffedf30d14032ea0c3415f00ec36703ad482a5/Serenity.CodeGenerator.Net45/MainWindow.xaml)
- Guide: `tutorials/movies/generating_code_for_movie_table.md`

---

## 1. Mở Sergen

1. Trong Visual Studio: *Tools → NuGet Package Manager → Package Manager Console*.
2. Ở ô *Default project*, chọn project Web (ví dụ `Serene.Web`).
3. Gõ:

   ```
   sergen
   ```

4. Một cửa sổ **Serenity Code Generator** hiện ra. Cấu hình của nó được lưu trong file
   `Serenity.CodeGenerator.config` ở thư mục solution, nên lần sau mở lại không phải nhập lại.

> Nếu gõ `sergen` mà báo không nhận lệnh thì kiểm tra project Web đã có package `Serenity.CodeGenerator`
> chưa, sau đó *Restore NuGet Packages* rồi mở lại Visual Studio.

## 2. Các bước trong cửa sổ Sergen

| Ô / Nút | Nhập gì | Ý nghĩa |
|---|---|---|
| **Connection** (+ *New Connection*) | Chọn hoặc thêm connection tới DB chứa bảng | Sergen tự kết nối DB để đọc cấu trúc bảng |
| **Web Project File** | Đường dẫn `...Web.csproj` | Nơi Sergen chép file sinh ra vào, và thêm chúng vào `.csproj` |
| **Root Namespace** | `Serene` (hoặc namespace dự án) | Tiền tố namespace cho code sinh ra |
| Danh sách bảng | Tick vào `mov.Movie` | Có thể tick nhiều bảng cùng lúc |
| **Connection Key** | `Default` | Phải trùng tên connection trong `Web.config`. Nó sẽ thành `[ConnectionKey("Default")]` |
| **Module** | `MovieDB` | Tên module, quyết định thư mục, namespace và URL |
| **Identifier** | `Movie` | Tên entity, sinh ra `MovieRow`, `MovieGrid`... |
| **Permission Key** | `Administration:General` | Quyền đọc/sửa mặc định. Sẽ đổi ở bài 07 |
| **Generation Options** | Row / Service / UI | Bỏ UI nếu chỉ cần Row + Service. Chỉ cần entity thì chỉ tick Row |

Cuối cùng bấm **Generate Code for Checked Entities**.

**Quy tắc đặt tên:** Module và Identifier phải viết **PascalCase**, tức là bắt đầu bằng chữ hoa
(`MovieDB`, `OrderDetail`). Nếu tên bảng có dấu gạch dưới hoặc ký tự lạ, hãy đổi Identifier thành một tên
C# hợp lệ.

## 3. Sergen sinh ra những gì

```
Modules/MovieDB/
├── MovieDBNavigation.cs     # Thêm 1 dòng [assembly: NavigationLink(...)] cho menu
└── Movie/
    ├── MovieRow.cs          # Entity
    ├── MovieRepository.cs   # Handler CRUD
    ├── MovieEndpoint.cs     # Service: /Services/MovieDB/Movie/...
    ├── MovieForm.cs         # Field trên dialog
    ├── MovieColumns.cs      # Cột trên grid
    ├── MoviePage.cs         # Trang /MovieDB/Movie
    ├── MovieIndex.cshtml    # View chứa grid
    ├── MovieGrid.ts
    └── MovieDialog.ts
Content/site/site.moviedb.less   # CSS cho dialog (.s-MovieDB-MovieDialog), được @import vào site.less
```

Đây đúng là bộ file đã học ở bài 01. Sergen chỉ giúp bạn khỏi phải chép tay khoảng 10 file.

**Sergen tự suy ra:**

- Cột identity hoặc primary key sẽ thành `[Identity]`/`[PrimaryKey]` và `IIdRow`.
- Cột chuỗi đầu tiên (`Title`) sẽ thành name field (`INameRow`) và có `[QuickSearch]`.
- `NOT NULL` thành `[NotNull]`, độ dài nvarchar thành `[Size(n)]`.
- Khóa ngoại ở DB thành `[ForeignKey]` và `[LeftJoin]`, kèm các field view lấy từ bảng kia.

## 4. Sau khi sinh code: bắt buộc làm

1. Nếu Visual Studio hỏi *reload project* thì chọn **Reload All**.
2. **Rebuild Solution**.
3. Chạy T4: *Build → Transform All T4 Templates*, hoặc chuột phải `Imports/ServerTypings/ServerTypings.tt`
   rồi chọn *Run Custom Tool*. Bước này sinh ra `MovieRow`, `MovieForm` và `MovieService` cho phía
   TypeScript. Sau đó **build lại một lần nữa** để biên dịch TypeScript.
4. Chạy app (F5), đăng nhập `admin` / `serenity` (mặc định của Serene). Trên menu trái sẽ có mục
   **MovieDB → Movie**.
5. Thử thêm, sửa và xóa một bộ phim. Lúc này CRUD đã chạy được mà chưa phải viết dòng code nào.

> Ghi nhớ: **build → T4 → build** mỗi khi sửa class C# mà TypeScript có dùng tới (Row, Form, Columns,
> Endpoint, enum, Request/Response).

## 5. Khi nào KHÔNG dùng lại Sergen

Sergen chỉ dùng **một lần cho mỗi bảng**, ở bước khởi tạo. Sau khi đã tùy biến code (bài 04 trở đi), nếu
chạy lại Sergen cho cùng bảng thì code của bạn có thể bị ghi đè. Khi bảng có thêm cột, hãy **thêm tay**
property vào Row, Form và Columns (xem bài 05).

Sergen có tích hợp KDiff3 để merge khi file đã tồn tại. Đường dẫn KDiff3 cấu hình trong *Generation
Options*. Dù vậy, cách an toàn nhất vẫn là thêm tay.

## 6. Lỗi hay gặp

| Lỗi | Cách xử lý |
|---|---|
| Không thấy menu mới | Kiểm tra quyền của user (Permission Key) và dòng `NavigationLink` trong `MovieDBNavigation.cs` |
| Mở trang báo lỗi JS `MovieGrid is not defined`... | Chưa chạy T4 hoặc chưa build lại TypeScript |
| Grid trống, F12 thấy request `List` lỗi 500 | Connection Key sai, hoặc tên bảng/schema sai trong `[TableName]` |
| File sinh ra không có trong Solution Explorer | Web Project File trỏ sai `.csproj`. Hãy *Show All Files* rồi *Include In Project* |

## 7. Bài tập

1. Trên project Serene thử nghiệm, sinh code cho `mov.Movie` (module `MovieDB`, identifier `Movie`).
2. Mở từng file sinh ra, đối chiếu với bài 01. File nào khác với `Category` ở Northwind?
3. Tạo thêm bảng `mov.Genre (GenreId identity, Name nvarchar(100) not null)` bằng migration, rồi sinh code
   cho nó. Bảng này sẽ dùng ở bài 05.
4. Ở dự án thật: tìm file `Serenity.CodeGenerator.config` trong solution và xem team đang cấu hình
   connection nào.

## 8. Ghi chú của mình

-
