# Bài 07 – Phân quyền (Permission) và Menu (Navigation)

**Mục tiêu:** hiểu permission key được khai báo, gán và kiểm tra ở những đâu. Tự tạo được bộ quyền cho
module mới (Xem / Sửa / Xóa), và sắp xếp menu bên trái.

Tham khảo:
- [Northwind/NorthwindPermissionKeys.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/NorthwindPermissionKeys.cs)
- [Northwind/NorthwindNavigation.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/NorthwindNavigation.cs)
- Guide: `authentication/`, `tutorials/movies/handling_movie_navigation.md`

---

## 1. Permission key là gì

Trong Serenity, **quyền chỉ là một chuỗi**, ví dụ `"Administration:General"` hay
`"Northwind:Customer:View"`. Quyền được gán cho **Role** hoặc gán thẳng cho **User**, trên trang
*Administration → Roles / Users → Edit Permissions*.

User `admin` của Serene luôn có mọi quyền. Vì vậy khi test phân quyền, hãy **tạo một user thường**.

## 2. Khai báo permission key

Nên gom tất cả key của một module vào một class, như Serene đang làm:

```csharp
using Serenity.Extensibility;
using System.ComponentModel;

namespace Serene.MovieDB
{
    [NestedPermissionKeys]
    [DisplayName("Movie Database")]
    public class PermissionKeys
    {
        [DisplayName("Movies")]
        public class Movie
        {
            public const string View = "MovieDB:Movie:View";

            [Description("Create/Update"), ImplicitPermission(View)]
            public const string Modify = "MovieDB:Movie:Modify";

            [ImplicitPermission(View)]
            public const string Delete = "MovieDB:Movie:Delete";
        }

        [Description("[General]")]
        public const string General = "MovieDB:General";
    }
}
```

- `[NestedPermissionKeys]` giúp các key này **tự hiện trong cây quyền** trên dialog phân quyền, nhóm theo
  `DisplayName`.
- `ImplicitPermission(View)` nghĩa là ai có quyền `Modify` thì **tự động có** quyền `View`.
- Dấu `:` trong key tạo thành cây phân cấp trên giao diện.

## 3. Gắn quyền vào entity

```csharp
[ReadPermission(PermissionKeys.Movie.View)]       // xem (List, Retrieve, lookup)
[ModifyPermission(PermissionKeys.Movie.Modify)]   // mặc định cho Create/Update/Delete
[DeletePermission(PermissionKeys.Movie.Delete)]   // ghi đè riêng cho Delete
public sealed class MovieRow : Row, IIdRow, INameRow
```

Bản 3.13.7 có các attribute cấp class: `ReadPermission`, `ModifyPermission`, `InsertPermission`,
`UpdatePermission`, `DeletePermission`. Cái nào không khai báo thì lấy theo `ModifyPermission`.

Từ các attribute này, quyền được áp dụng **tự động** ở nhiều lớp:

| Lớp | Cơ chế | Hiệu quả |
|---|---|---|
| Trang | `[PageAuthorize(typeof(MovieRow))]` trong `MoviePage.cs` | Không có `ReadPermission` thì không vào được trang |
| Service | `[ServiceAuthorize(typeof(MyRow))]`, `[AuthorizeCreate]`, `[AuthorizeUpdate]`, `[AuthorizeDelete]` trong Endpoint | Chặn gọi API trực tiếp |
| Lookup | Lookup script kiểm tra `ReadPermission` của Row | User không có quyền thì không tải được dropdown |
| Giao diện | Grid và Dialog ẩn hoặc vô hiệu hóa nút New, Save, Delete | Người dùng không thấy nút mình không được bấm |

> Ẩn nút trên giao diện **không phải là bảo mật**. Bảo mật thật nằm ở Endpoint và Repository.

### Quyền theo từng field

```csharp
[ReadPermission("MovieDB:Movie:Budget")]           // không có quyền thì không thấy field
[ModifyPermission("MovieDB:Movie:Budget")]
public Decimal? Budget { ... }
```

Bản 3.13.7 có các attribute này ở cấp field. Field bị chặn sẽ không được trả về và không cho lưu.

## 4. Kiểm tra quyền trong code

```csharp
// C# – trong Repository / Endpoint
if (!Authorization.HasPermission(PermissionKeys.Movie.Modify))
    throw new ValidationError("Bạn không có quyền sửa phim!");

Authorization.ValidatePermission(PermissionKeys.Movie.Delete);   // ném lỗi nếu không có
var userId = Convert.ToInt32(Authorization.UserId);
var user = (UserDefinition)Authorization.UserDefinition;         // thông tin user hiện tại
```

```ts
// TypeScript – trong Grid/Dialog
if (Q.Authorization.hasPermission("MovieDB:Movie:Modify")) {
    // hiện nút
}
```

## 5. Navigation – menu bên trái

Menu được khai báo bằng **assembly attribute**. Sergen sinh một dòng trong
`Modules/<Module>/<Module>Navigation.cs`:

```csharp
using Serenity.Navigation;
using MyPages = Serene.MovieDB.Pages;

[assembly: NavigationLink(int.MaxValue, "MovieDB/Movie", typeof(MyPages.MovieController), icon: null)]
```

Sửa lại cho gọn và có thứ tự, theo mẫu `NorthwindNavigation.cs`:

```csharp
[assembly: NavigationMenu(2000, "Movie Database", icon: "fa-film")]
[assembly: NavigationLink(2100, "Movie Database/Movies", typeof(MyPages.MovieController), icon: "fa-video-camera")]
[assembly: NavigationLink(2200, "Movie Database/Genres", typeof(MyPages.GenreController), icon: "fa-tags")]
```

- Số đầu tiên là **thứ tự hiển thị**. Dashboard là `1000`, Northwind là `7000`, nên chọn số trước hoặc
  sau các mốc đó.
- `"Menu/Link"`: dấu `/` tách menu cha và link con.
- `NavigationMenu` khai báo menu cha để đặt được thứ tự và icon. Nếu không khai báo, Serenity tự tạo menu
  cha nhưng bạn không kiểm soát được nó.
- Icon dùng class của Font Awesome 4.7 (`fa-...`) hoặc Simple Line Icons (`icon-...`). Danh sách icon có ở
  trang *Theme Samples → UI Elements → Icons* trong Serene.
- Link có `typeof(Controller)` thì **tự ẩn khi user không có quyền** (lấy từ `PageAuthorize`).
- Link không có controller thì dùng `url:` và `permission:`, ví dụ dòng Dashboard trong
  `Modules/Common/Navigation/NavigationItems.cs`.

## 6. Bài tập

1. Tạo `MovieDBPermissionKeys.cs` có `View`, `Modify`, `Delete` như mục 2. Gắn vào `MovieRow` và
   `GenreRow`, sau đó build lại.
2. Tạo role `MovieViewer` chỉ có quyền `MovieDB:Movie:View`, rồi tạo user `viewer` gán vào role đó. Đăng
   nhập bằng `viewer`: kiểm tra nút New và Save đã ẩn chưa. Mở F12 và thử gọi
   `/Services/MovieDB/Movie/Create` để thấy server chặn.
3. Sắp xếp menu: tạo `NavigationMenu` "Movie Database" nằm ngay dưới Dashboard, có 2 link Movies và
   Genres với icon riêng.
4. Ở dự án thật: tìm file PermissionKeys của 1 module, đối chiếu với cây quyền trên trang Roles.

## 7. Ghi chú của mình

-
