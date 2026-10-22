# Bài 04 – Tùy biến Grid, Form và Columns

**Mục tiêu:** biết chỉnh giao diện **ở đúng chỗ**: tiêu đề, độ rộng, thứ tự, editor, tab, kích thước
dialog, quick search. Phần lớn các chỉnh sửa này chỉ cần thêm attribute trong C#.

Ví dụ tiếp tục với module `MovieDB/Movie` đã sinh ở bài 03.

Tham khảo:
- Guide: `tutorials/movies/customizing_movie_interface.md`, `customizing_quick_search.md`
- Serene: [Northwind/Order/OrderForm.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/OrderForm.cs) (Tab/Category)

---

## 1. Nguyên tắc "sửa ở đâu"

Có ba nơi khai báo thông tin cho một field:

```
            MovieRow.cs   ← nguồn chung: tiêu đề, kiểu, NotNull, editor mặc định
             /       \
   MovieForm.cs     MovieColumns.cs
   (dialog)          (grid)
```

- Khai báo ở **Row** thì có hiệu lực **ở mọi nơi**, cả grid lẫn form.
- Khai báo ở **Form** hay **Columns** thì **ghi đè** cho riêng dialog hoặc riêng grid.
- Ưu tiên sửa ở Row, trừ khi bạn thật sự muốn grid và form khác nhau.

## 2. Đổi tiêu đề field

Muốn đổi ở mọi nơi thì sửa trong Row:

```csharp
[DisplayName("Runtime (mins)")]
public Int32? Runtime
{
    get { return Fields.Runtime[this]; }
    set { Fields.Runtime[this] = value; }
}
```

Muốn chỉ cột trên grid hiển thị khác thì sửa trong Columns:

```csharp
[DisplayName("Runtime in Minutes"), Width(150), AlignRight]
public Int32 Runtime { get; set; }
```

**Tiêu đề trang và dialog** lấy từ attribute ở cấp class của Row:

```csharp
[DisplayName("Movies"), InstanceName("Movie")]   // DisplayName: số nhiều, dùng làm tiêu đề trang
public sealed class MovieRow : Row, IIdRow, INameRow // InstanceName: số ít, dùng cho "New Movie", "Edit Movie"
```

## 3. Columns: độ rộng, căn lề, ẩn hiện, thứ tự

```csharp
[ColumnsScript("MovieDB.Movie")]
[BasedOnRow(typeof(Entities.MovieRow), CheckNames = true)]
public class MovieColumns
{
    [EditLink, DisplayName("Db.Shared.RecordId"), AlignRight]
    public Int32 MovieId { get; set; }

    [EditLink, Width(300)]              // bấm vào thì mở dialog
    public String Title { get; set; }

    [Width(80), AlignCenter]
    public Int32 Year { get; set; }

    [DisplayFormat("dd/MM/yyyy")]
    public DateTime ReleaseDate { get; set; }

    [Hidden]                            // có trong grid nhưng ẩn, người dùng có thể bật lại
    public String Description { get; set; }
}
```

| Attribute | Tác dụng |
|---|---|
| `Width(n)` | Độ rộng cột (px). Nếu không khai báo, Serenity tự tính theo kiểu và độ dài field |
| `AlignRight`, `AlignCenter` | Căn lề |
| `EditLink` | Biến ô thành link mở dialog |
| `DisplayFormat("...")` | Định dạng số hoặc ngày, ví dụ `"#,##0.00"`, `"dd/MM/yyyy"` |
| `Hidden` | Ẩn mặc định, người dùng vẫn bật lại được qua *column picker* |
| `Sortable(false)` | Không cho sắp xếp theo cột này |
| `QuickFilter` | Thêm bộ lọc nhanh phía trên grid (xem bài 05) |

**Thứ tự cột chính là thứ tự property.** Muốn bỏ hẳn một cột thì xóa property đó.

## 4. Form: editor, layout, tab

```csharp
[FormScript("MovieDB.Movie")]
[BasedOnRow(typeof(Entities.MovieRow), CheckNames = true)]
public class MovieForm
{
    [Tab("Thông tin chung")]
    [Category("Cơ bản")]
    public String Title { get; set; }

    [TextAreaEditor(Rows = 3)]
    public String Description { get; set; }

    [HalfWidth] public Int32 Year { get; set; }
    [HalfWidth] public DateTime ReleaseDate { get; set; }

    [Tab("Nội dung")]
    [TextAreaEditor(Rows = 8)]
    public String Storyline { get; set; }

    [Placeholder("phút"), Hint("Thời lượng tính bằng phút")]
    public Int32 Runtime { get; set; }
}
```

| Attribute | Tác dụng |
|---|---|
| `Tab("...")` | Bắt đầu một tab mới. Các field phía dưới thuộc tab này |
| `Category("...")` | Bắt đầu một nhóm (khung có tiêu đề) |
| `HalfWidth`, `OneThirdWidth`, `FullWidth`... | Chia cột trên form |
| `TextAreaEditor(Rows = n)` | Ô nhập nhiều dòng |
| `HtmlContentEditor` | Ô soạn thảo rich text |
| `ReadOnly(true)` | Chỉ hiển thị, không cho sửa |
| `HideOnInsert`, `HideOnUpdate` | Ẩn khi thêm mới hoặc khi sửa |
| `Required(true)` | Bắt buộc nhập trên form, kể cả khi Row không `NotNull` |
| `DefaultValue(...)` | Giá trị mặc định khi thêm mới, ví dụ `[DefaultValue("now")]` cho ngày |
| `Placeholder`, `Hint` | Chữ gợi ý trong ô, tooltip |

Ví dụ thật trong Serene là `OrderForm.cs`, dùng `Tab` và `Category` cùng `[DefaultValue("now")]` cho
`OrderDate`.

## 5. Kích thước dialog bằng LESS

Sergen sinh `Content/site/site.moviedb.less`:

```less
.s-MovieDB-MovieDialog {
    > .size { width: 650px; height: 500px; }   // kích thước dialog trên desktop
    .caption { width: 150px; }                 // độ rộng nhãn field
}
```

Tên class là `s-<Module>-<Tên>Dialog`. File `.less` được biên dịch thành `site.css` **khi build**
(target `CompileSiteLess` trong `.csproj`), nên sửa xong phải build lại. Trên điện thoại, dialog tự chuyển
sang chế độ responsive, nên `height` chỉ có tác dụng trên desktop.

## 6. Quick search

Ô tìm nhanh trên grid tìm theo các field có `[QuickSearch]` trong Row:

```csharp
[DisplayName("Title"), Size(200), NotNull, QuickSearch]
public String Title { ... }

[DisplayName("Description"), Size(1000), QuickSearch]                    // tìm "chứa"
public String Description { ... }

[DisplayName("Year"), QuickSearch(SearchType.Equals, numericOnly: 1)]   // chỉ khớp số chính xác
public Int32? Year { ... }
```

`SearchType` có `Contains` (mặc định), `StartsWith` và `Equals`.

Muốn có dropdown cho người dùng chọn field cần tìm thì override trong `MovieGrid.ts`:

```ts
protected getQuickSearchFields(): Serenity.QuickSearchField[] {
    let fld = MovieRow.Fields;
    return [
        { name: "", title: "tất cả" },
        { name: fld.Description, title: "mô tả" },
        { name: fld.Year, title: "năm" }
    ];
}
```

`MovieRow.Fields` có được là nhờ T4 sinh ra (build → T4 → build). Dùng cách này thay vì gõ chuỗi
`"Description"` sẽ tránh được lỗi gõ sai tên.

## 7. Tùy biến bằng TypeScript

Những gì attribute không làm được thì viết trong `Grid.ts` / `Dialog.ts`:

```ts
// MovieGrid.ts – thêm nút trên toolbar
protected getButtons(): Serenity.ToolButton[] {
    var buttons = super.getButtons();
    buttons.push({
        title: 'Xuất Excel',
        cssClass: 'export-xlsx-button',
        onClick: () => { /* ... */ }
    });
    return buttons;
}

// MovieDialog.ts – ẩn/hiện field theo giá trị field khác
protected afterLoadEntity() {
    super.afterLoadEntity();
    Serenity.EditorUtils.setReadOnly(this.form.Runtime, this.isEditMode());
}
```

Ví dụ thật: `OrderDialog.ts` thêm nút *Invoice* trong `getToolbarButtons()`, rồi bật/tắt nút trong
`updateInterface()`.

## 8. Bài tập

1. Đổi `DisplayName` của Row thành `Movies` / `Movie`, rồi xem tiêu đề trang và nút "New" thay đổi.
2. Trên grid: cho cột `Title` rộng 300px, `Year` căn giữa, và ẩn `Description` bằng `[Hidden]`.
3. Trên form: chia thành 2 tab, đặt `Year` và `ReleaseDate` trên cùng một hàng bằng `HalfWidth`, cho
   `Storyline` dùng `TextAreaEditor(Rows = 8)`.
4. Thêm `QuickSearch` cho `Description`, sau đó thêm dropdown chọn field tìm kiếm trong `MovieGrid.ts`.
5. Ở dự án thật: tìm một Form có dùng `Tab`/`Category` và một Grid có override `getButtons()`, rồi đọc
   hiểu chúng.

## 9. Ghi chú của mình

-
