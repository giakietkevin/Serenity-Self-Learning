# Bài 13 – Tự viết Formatter, Editor và Filter

**Mục tiêu:** khi các attribute có sẵn không đủ, tự viết được: formatter để hiển thị ô trên grid, editor để
nhập liệu trên form, bộ lọc riêng (custom ListRequest), cột checkbox chọn nhiều dòng và tô màu dòng theo
điều kiện.

Tham khảo (Serene 3.13.7, đều có trong menu *Basic Samples*):
- Formatter: [Northwind/Order/FreightFormatter.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Order/FreightFormatter.ts), [Northwind/Employee/EmployeeFormatter.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Employee/EmployeeFormatter.ts), [BasicSamples/Grids/InlineImageInGrid/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Grids/InlineImageInGrid)
- Editor: [Northwind/Shared/PhoneEditor.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Shared/PhoneEditor.ts), [BasicSamples/Editors/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Editors)
- Grid: [BasicSamples/Grids/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Grids) (`ConditionalFormatting`, `CancellableBulkAction`, `QuickFilterCustomization`)
- Guide: `tutorials/movies/allowing_multiple_genre_selection.md`, `filtering_with_multiple_genre_list.md`, `howto/how_to_setup_cascaded_editors.md`

---

## 0. Quy tắc chung: viết TS → build → T4 → dùng trong C#

Formatter và editor được viết bằng **TypeScript**. Sau khi build và chạy T4 (`ClientTypes.tt`), Serenity sinh
ra một **attribute C#** tương ứng trong `Imports/ClientTypes/`. Ví dụ class `EmployeeFormatter` sinh ra
`EmployeeFormatterAttribute`, và mỗi `@Serenity.Decorators.option()` thành một property của attribute:

```csharp
// Imports/ClientTypes/Northwind.EmployeeFormatterAttribute.cs (tự sinh, không sửa tay)
public const string Key = "Serene.Northwind.EmployeeFormatter";
public String GenderProperty
{
    get { return GetOption<String>("genderProperty"); }
    set { SetOption("genderProperty", value); }
}
```

Nhờ vậy trong `OrderColumns.cs` mới viết được `[EmployeeFormatter(GenderProperty = "EmployeeGender")]`.

## 1. Formatter: quyết định ô trên grid hiển thị gì

### 1.1. Formatter đơn giản

```ts
namespace Serene.Northwind {
    @Serenity.Decorators.registerFormatter()
    export class FreightFormatter implements Slick.Formatter {
        format(ctx: Slick.FormatterContext) {
            return "<span class='freight-symbol'>" + Q.htmlEncode(ctx.value) + '</span>';
        }
    }
}
```

- `ctx.value` là giá trị của ô, `ctx.item` là **cả dòng** dữ liệu.
- `format` trả về **HTML**. Luôn dùng `Q.htmlEncode` cho dữ liệu người dùng nhập để tránh lỗi XSS.

### 1.2. Formatter có tùy chọn và cần thêm field

```ts
@Serenity.Decorators.registerFormatter([Serenity.ISlickFormatter, Serenity.IInitializeColumn])
export class EmployeeFormatter implements Slick.Formatter {
    format(ctx: Slick.FormatterContext) {
        var text = Q.htmlEncode(ctx.value);
        if (!this.genderProperty)
            return text;
        var gender = ctx.item[this.genderProperty];          // đọc field khác trong cùng dòng
        return "<span class='" + ((gender === Gender.Female) ?
            'employee-symbol female' : 'employee-symbol male') + "'>" + text + '</span>';
    }

    @Serenity.Decorators.option()
    public genderProperty: string;

    public initializeColumn(column: Slick.Column) {
        column.referencedFields = column.referencedFields || [];
        if (this.genderProperty)
            column.referencedFields.push(this.genderProperty);   // ← bắt server trả về field này
    }
}
```

> **`referencedFields` quan trọng:** grid chỉ yêu cầu server trả về các cột đang hiển thị. Nếu formatter
> cần `EmployeeGender` mà cột đó không hiển thị, `ctx.item.EmployeeGender` sẽ là `undefined`.
> `initializeColumn` thêm field vào danh sách cần lấy.

### 1.3. Ví dụ cho MovieDB: hiện tên thể loại từ danh sách Id

`GenreList` là `List<Int32>`, grid sẽ hiện `1,3`. Formatter đổi sang tên bằng lookup:

```ts
namespace Serene.MovieDB {
    @Serenity.Decorators.registerFormatter()
    export class GenreListFormatter implements Slick.Formatter {
        format(ctx: Slick.FormatterContext) {
            let idList = ctx.value as number[];
            if (!idList || !idList.length)
                return "";

            let byId = GenreRow.getLookup().itemById;
            return idList.map(x => {
                let g = byId[x];
                return g ? Q.htmlEncode(g.Name) : x.toString();
            }).join(", ");
        }
    }
}
```

```csharp
// MovieColumns.cs (sau khi build + T4)
[Width(200), GenreListFormatter, QuickFilter]
public List<Int32> GenreList { get; set; }
```

`LinkingSetRelationBehavior` tự nạp `GenreList` cho từng dòng khi cột này được hiển thị trên grid.

## 2. Editor: ô nhập liệu riêng

### 2.1. Kế thừa editor có sẵn (cách hay dùng nhất)

`PhoneEditor` kế thừa `StringEditor`, thêm kiểm tra và tự định dạng số điện thoại:

```ts
@Serenity.Decorators.registerEditor()
export class PhoneEditor extends Serenity.StringEditor {
    constructor(input: JQuery) {
        super(input);

        this.addValidationRule(this.uniqueName, e => {          // kiểm tra khi Save
            var value = Q.trimToNull(this.get_value());
            if (value == null) return null;
            return PhoneEditor.validate(value, this.multiple);  // trả về chuỗi lỗi hoặc null
        });

        input.bind('blur', e => {                               // rời ô thì định dạng lại
            if (this.element.hasClass('valid'))
                this.formatValue();
        });
    }

    @Serenity.Decorators.option()
    public multiple: boolean;                                   // → [PhoneEditor(Multiple = true)]

    get_value() { return this.getFormattedValue(); }            // giá trị gửi lên server
    set_value(value: string) { this.element.val(value); }       // giá trị nạp từ server
}
```

Dùng: `[PhoneEditor]` trên field trong Row hoặc Form.

### 2.2. Dropdown với giá trị cố định

```ts
@Serenity.Decorators.registerEditor()
export class HardcodedValuesEditor extends Serenity.Select2Editor<any, any> {
    constructor(container: JQuery) {
        super(container, null);
        this.addOption("key1", "Text 1");
        this.addOption("key2", "Text 2");
        this.addItem({ id: "key4", text: "Text 4", disabled: true });
    }
}
```

Nếu danh sách cố định và có ý nghĩa nghiệp vụ, **dùng enum** (bài 05) sẽ gọn hơn. Editor kiểu này hợp với
danh sách tính toán ở client.

### 2.3. Editor phụ thuộc nhau (cascade) không cần viết code

Chọn quốc gia xong thì dropdown thành phố chỉ hiện thành phố của quốc gia đó:

```csharp
// CustomerRow.cs trong Serene
[DisplayName("City"), Size(15)]
[LookupEditor(typeof(Lookups.CustomerCityLookup), CascadeFrom = "Country", AutoComplete = true)]
public String City { ... }
```

| Tùy chọn `LookupEditor` | Ý nghĩa |
|---|---|
| `CascadeFrom = "Country"` | Tên **field trên form** làm cha |
| `CascadeField = "Country"` | Tên field **trong lookup** để so sánh. Mặc định giống `CascadeFrom` |
| `FilterField`, `FilterValue` | Lọc cố định, ví dụ chỉ hiện sản phẩm còn bán: `FilterField = "Discontinued", FilterValue = false` |

> Field dùng để cascade/filter phải **có trong lookup script**, tức là có `[LookupInclude]` ở Row nguồn (bài 05).

## 3. Bộ lọc riêng: custom `ListRequest`

Khi bộ lọc không phải là "cột = giá trị", ví dụ "phim từ năm X trở đi" hoặc "phim có ít nhất một thể loại
trong danh sách", làm theo 4 bước.

**B1. Request mới** (`MovieListRequest.cs`):

```csharp
public class MovieListRequest : ListRequest
{
    public List<int> Genres { get; set; }
    public int? MinYear { get; set; }
}
```

**B2. Đổi kiểu request** trong Repository và Endpoint:

```csharp
// MovieRepository.cs
public ListResponse<MyRow> List(IDbConnection connection, MovieListRequest request)
{
    return new MyListHandler().Process(connection, request);
}

private class MyListHandler : ListRequestHandler<MyRow, MovieListRequest>
{
    protected override void ApplyFilters(SqlQuery query)
    {
        base.ApplyFilters(query);

        if (Request.MinYear != null)
            query.Where(fld.Year >= Request.MinYear.Value);

        if (!Request.Genres.IsEmptyOrNull())
        {
            var mg = Entities.MovieGenresRow.Fields.As("mg");      // alias riêng, tránh trùng t0
            query.Where(Criteria.Exists(
                query.SubQuery()
                    .From(mg)
                    .Select("1")
                    .Where(mg.MovieId == fld.MovieId &&
                           mg.GenreId.In(Request.Genres))
                    .ToString()));
        }
    }
}

// MovieEndpoint.cs
public ListResponse<MyRow> List(IDbConnection connection, MovieListRequest request) { ... }
```

SQL sinh ra:

```sql
SELECT ... FROM [mov].[Movie] T0
WHERE T0.Year >= 2000
  AND EXISTS (SELECT 1 FROM [mov].[MovieGenres] mg
              WHERE mg.MovieId = T0.MovieId AND mg.GenreId IN (1, 3))
```

**B3. Build + T4** để client có `MovieListRequest`.

**B4. Client gán giá trị vào request** (`MovieGrid.ts`). Hai cách:

```ts
// Cách a: chặn quick filter có sẵn và chuyển giá trị sang request
protected getQuickFilters() {
    let items = super.getQuickFilters();
    let genreFilter = Q.first(items, x => x.field == MovieRow.Fields.GenreList);
    genreFilter.handler = h => {
        let request = h.request as MovieListRequest;
        let values = (h.widget as Serenity.LookupEditor).values;
        request.Genres = values.map(x => parseInt(x, 10));
        h.handled = true;                 // ← không để quick filter tự thêm Criteria
    };
    return items;
}

// Cách b: tự thêm ô nhập rồi gán trong onViewSubmit
protected onViewSubmit() {
    if (!super.onViewSubmit())
        return false;
    let request = this.view.params as MovieListRequest;
    request.MinYear = this.minYearEditor.value;
    return true;
}
```

> Trong 3.13.7, `LinkingSetRelation` đã **tự xử lý** quick filter bằng một giá trị trên `GenreList`
> (`HandleEqualityFilter = true` mặc định). Custom ListRequest vẫn là kỹ thuật nên biết, vì nó áp dụng cho
> mọi bộ lọc phức tạp.

## 4. Hai kỹ thuật grid hay dùng

### 4.1. Cột checkbox chọn nhiều dòng (`GridRowSelectionMixin`)

```ts
export class MovieGrid extends Serenity.EntityGrid<MovieRow, any> {
    private rowSelection: Serenity.GridRowSelectionMixin;

    protected createToolbarExtensions() {
        super.createToolbarExtensions();
        this.rowSelection = new Serenity.GridRowSelectionMixin(this);
    }

    protected getColumns() {
        var columns = super.getColumns();
        columns.splice(0, 0, Serenity.GridRowSelectionMixin.createSelectColumn(() => this.rowSelection));
        return columns;
    }

    protected getButtons() {
        var buttons = super.getButtons();
        buttons.push({
            title: 'Đánh dấu là Film',
            cssClass: 'send-button',
            onClick: () => {
                var ids = this.rowSelection.getSelectedKeys();    // mảng Id đã chọn
                // gọi service riêng với ids, xong thì:
                // this.rowSelection.resetCheckedAndRefresh();
            }
        });
        return buttons;
    }
}
```

Muốn hiện thanh tiến trình và cho hủy khi xử lý nhiều dòng, xem `CancellableBulkAction` và
`Common/Helpers/BulkServiceAction.ts`.

### 4.2. Tô màu dòng theo dữ liệu (`getItemCssClass`)

```ts
protected getItemCssClass(item: MovieRow, index: number): string {
    if (item.Kind == MovieKind.TvSeries) return "tv-series";
    if (item.Year < 1950) return "old-movie";
    return null;
}
```

```less
.s-MovieDB-MovieGrid .tv-series .slick-cell { background-color: #eef7ff; }
```

Field dùng trong `getItemCssClass` cũng phải được trả về, tức là cột đó phải hiển thị, hoặc thêm vào
`referencedFields`/`IncludeColumns`.

## 5. Bài tập

1. Viết `GenreListFormatter` và gắn vào cột `GenreList`.
2. Viết `RuntimeFormatter` hiển thị `135` thành `2h 15m`, có option `showMinutesOnly`.
3. Viết `MovieTitleEditor` kế thừa `StringEditor`, tự viết hoa chữ đầu khi rời ô.
4. Làm bộ lọc `MinYear` với một ô nhập trên toolbar.
5. Thêm cột checkbox và nút "Xóa các phim đã chọn" (gọi `MovieService.Delete` lần lượt, hoặc viết endpoint
   xóa nhiều).
6. Ở dự án thật: tìm các file `*Formatter.ts` và `*Editor.ts`. Formatter nào dùng `referencedFields`?

## 6. Ghi chú của mình

-
