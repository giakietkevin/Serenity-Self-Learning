# Bài 24 – Dùng thư viện JavaScript bên ngoài: biểu đồ, trang dashboard và bọc plugin thành editor

**Mục tiêu:** biết đưa một thư viện JS/CSS bên ngoài vào dự án (chỉ cho một trang hay cho toàn site), gọi nó
từ TypeScript, vẽ biểu đồ từ dữ liệu của một service, làm trang dashboard riêng, và bọc một plugin jQuery thành
editor dùng được trên form như các editor có sẵn.

Tham khảo (3.13.7):
- Serene: [BasicSamples/Dialogs/ChartInDialog/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/Dialogs/ChartInDialog), [BasicSamplesEndpoint.OrdersByShipper](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/BasicSamples/BasicSamplesEndpoint.cs), [Common/Dashboard/](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Common/Dashboard), [Views/Shared/_Layout.cshtml](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Views/Shared/_Layout.cshtml), [Scripts/site/ScriptBundles.json](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Scripts/site/ScriptBundles.json), [Northwind/Note/NotesEditor.ts](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Modules/Northwind/Note/NotesEditor.ts)
- Guide: `howto/how_to_use_a_third_party_plugin_with_serenity.md`

---

## 1. Đưa file JS/CSS vào trang

Serene 3.13.7 có sẵn khá nhiều thư viện trong `Scripts/`: `morris` + `raphael` (biểu đồ dùng ở Dashboard),
`chartjs` (Chart.js), `flot`, `sparkline`, `fullcalendar`, `ckeditor`, `jspdf`... Thư viện mới thì chép file vào
`Scripts/<tên>/` và `Content/<tên>/` (hoặc cài bằng NuGet nếu gói đó chép file vào project).

Có ba chỗ để nạp, tùy phạm vi:

| Phạm vi | Cách | Ví dụ trong Serene |
|---|---|---|
| Chỉ một trang | Khai báo trong `@section Head { ... }` của view `.cshtml` | `ChartInDialog/Index.cshtml` |
| Một nhóm trang, có bundling | Thêm một bundle mới vào `ScriptBundles.json` / `CssBundles.json`, rồi `@Html.ScriptBundle("Tên")` trong `@section Head` | Bundle `Pages/Dashboard` |
| Mọi trang | Thêm vào bundle `Site` (hoặc `Base`) trong `ScriptBundles.json` | `jquery.autoNumeric.js`, `vue.js` |

`_Layout.cshtml` gọi `_LayoutHead` (nạp bundle `Base`, `Site`) rồi `@RenderSection("Head", false)`, nên script
khai báo trong `@section Head` luôn chạy **sau** jQuery và Serenity.

Trang mẫu ChartInDialog:

```cshtml
@section Head {
<link rel="stylesheet" href="~/Scripts/morris/morris.css">
<script src="~/Scripts/raphael/raphael-min.js"></script>
<script src="~/Scripts/morris/morris.min.js"></script>
}
```

Trang Dashboard dùng bundle:

```cshtml
@section Head {
@Html.StyleBundle("Pages/Dashboard")
@Html.ScriptBundle("Pages/Dashboard")
}
```

```json
// Scripts/site/ScriptBundles.json
"Pages/Dashboard": [
  "~/Scripts/raphael/raphael-min.js",
  "~/Scripts/morris/morris.min.js",
  "~/Scripts/sparkline/jquery.sparkline.min.js",
  ...
]
```

Khi bundling tắt (`ScriptBundling: { Enabled: false }`, mặc định khi phát triển), `ScriptBundle(...)` in ra từng
thẻ `<script>`. Khi bật, nó gộp và nén lại thành một file (bài 15). Vì vậy dùng bundle thì không phải sửa view khi
chuyển sang production.

> Thứ tự trong bundle là thứ tự nạp. Thư viện phụ thuộc (ví dụ `raphael` trước `morris`) phải đứng trước.

## 2. Gọi thư viện từ TypeScript

TypeScript không biết biến toàn cục `Morris` hay `Chart`. Có hai cách:

**Nhanh:** khai báo kiểu `any` ở đầu file, như `ChartInDialog.ts`:

```ts
declare var Morris: any;
```

**Có gợi ý code:** viết một file khai báo trong `Scripts/typings/<tên>/<tên>.d.ts`. `tsconfig.json` của Serene
đã `include` thư mục `./Scripts/typings/`. Ví dụ cho một plugin jQuery:

```ts
// Scripts/typings/bsmultiselect/bsmultiselect.d.ts
interface JQuery {
    multiselect(options?: BSMultiSelectOptions | string): JQuery;
}

interface BSMultiSelectOptions {
    includeSelectAllOption?: boolean;
    selectAllText?: string;
}
```

Mọi file `.ts` trong `Modules/` được biên dịch chung vào `Scripts/site/Serene.Web.js` (thuộc bundle `Site`),
nên không cần `import`.

## 3. Biểu đồ trong dialog: `ChartInDialog`

Ba phần: service trả dữ liệu, dialog chứa biểu đồ, và trang mở dialog.

**Service** (`BasicSamplesEndpoint.OrdersByShipper`): đếm đơn hàng theo tháng và theo hãng vận chuyển bằng
`SqlQuery` + `GroupBy` (bài 16), rồi trả về đúng dạng mà Morris cần:

```csharp
public class OrdersByShipperResponse : ServiceResponse
{
    public List<Dictionary<string, object>> Values { get; set; }   // mỗi phần tử: { Month: "Jan", s1: 10, s2: 7, ... }
    public List<string> ShipperKeys { get; set; }                  // ["s1", "s2", "s3"]
    public List<string> ShipperLabels { get; set; }                // ["Speedy Express", ...]
}
```

> Nguyên tắc: tính toán và gom nhóm ở **server**, trả về dữ liệu đã sẵn hình dạng của thư viện biểu đồ. Phía
> client chỉ vẽ.

**Dialog:**

```ts
declare var Morris: any;

@Serenity.Decorators.registerClass()
@Serenity.Decorators.resizable()
@Serenity.Decorators.maximizable()
export class ChartInDialog extends Serenity.TemplatedDialog<any> {

    private areaChart: any;

    protected onDialogOpen() {
        super.onDialogOpen();
        BasicSamplesService.OrdersByShipper({}, response => {
            this.areaChart = new Morris.Area({
                element: this.idPrefix + 'Chart',          // id của div trong template
                resize: true, parseTime: false,
                data: response.Values,
                xkey: 'Month',
                ykeys: response.ShipperKeys, labels: response.ShipperLabels, hideHover: 'auto'
            });
        });
    }

    protected arrange() {                               // gọi khi đổi kích thước, phóng to dialog
        super.arrange();
        this.areaChart && this.areaChart.redraw();
    }

    protected getTemplate() {
        return "<div id='~_Chart'></div>";
    }
}
```

Ba điểm rút ra:
- Vẽ trong `onDialogOpen`, **không** vẽ trong constructor. Lúc constructor chạy, dialog chưa hiện nên chiều rộng
  của div là 0 và thư viện vẽ sai kích thước.
- Dùng `this.idPrefix + 'Chart'` (hoặc `this.byId('Chart')`) để id không trùng khi mở hai dialog cùng lúc
  (bài 19).
- Override `arrange()` để vẽ lại khi dialog đổi kích thước.

**Trang:** `Index.cshtml` nạp script ở `@section Head`, có một nút, và gọi
`Serene.BasicSamples.ChartInDialog.initializePage()` để gắn sự kiện click mở dialog.

Có thể đặt biểu đồ vào một tab của `EntityDialog` theo cùng cách: vẽ khi tab được chọn lần đầu (hoặc trong
`arrange()`), vì tab ẩn cũng có chiều rộng 0.

## 4. Trang dashboard riêng

`Common/Dashboard` là một trang MVC thường, không có grid:

```csharp
[RoutePrefix("Dashboard"), Route("{action=index}")]
public class DashboardController : Controller
{
    [Authorize, HttpGet, Route("~/")]
    public ActionResult Index()
    {
        var cachedModel = TwoLevelCache.GetLocalStoreOnly("DashboardPageModel", TimeSpan.FromMinutes(5),
            OrderRow.Fields.GenerationKey, () =>
            {
                var model = new DashboardPageModel();
                using (var connection = SqlConnections.NewFor<OrderRow>())
                {
                    model.OpenOrders = connection.Count<OrderRow>(o.ShippingState == (int)OrderShippingState.NotShipped);
                    // ...
                }
                return model;
            });
        return View(MVC.Views.Common.Dashboard.DashboardIndex, cachedModel);
    }
}
```

- Số liệu tổng hợp được tính ở server và **cache** 5 phút, tự hết hạn khi bảng Order thay đổi (bài 10). Trang
  dashboard được mở rất thường xuyên nên nên cache như vậy.
- View `DashboardIndex.cshtml` nhận model bằng `@model Serene.Common.DashboardPageModel` và in ra bằng
  `@Model.OpenOrders`.
- Các biểu đồ trong Dashboard mặc định của Serene là **dữ liệu mẫu** của AdminLTE (`Scripts/adminlte/pages/dashboard.js`),
  không lấy từ DB. Muốn biểu đồ thật, làm như mục 3: thêm một service trả dữ liệu, rồi vẽ khi trang tải xong.

Trang dashboard của riêng module: thêm controller + view như trên (xem lại phần Page ở bài 01), thêm
`[NavigationLink]` để có menu (bài 07), và `[PageAuthorize]` để giới hạn quyền.

## 5. Bọc plugin jQuery thành editor

Muốn một plugin (ô chọn nhiều, ô nhập màu, slider...) dùng được trên form như `[LookupEditor]`, form phải biết
cách **đưa giá trị vào** và **lấy giá trị ra**. Editor cài hai interface `ISetEditValue` và `IGetEditValue`, cách
`NotesEditor` của Serene làm:

```ts
namespace Serene.Common {

    @Serenity.Decorators.element("<select/>")                                   // plugin cần thẻ <select>
    @Serenity.Decorators.registerEditor([Serenity.IGetEditValue, Serenity.ISetEditValue])
    export class BSMultiSelectEditor extends Serenity.Widget<BSMultiSelectEditorOptions>
        implements Serenity.IGetEditValue, Serenity.ISetEditValue {

        constructor(element: JQuery, opt: BSMultiSelectEditorOptions) {
            super(element, opt);

            element.attr('multiple', 'multiple');
            let lookup = Q.getLookup<any>(this.options.lookupKey);
            for (let item of lookup.items)
                Q.addOption(element, item[lookup.idField], item[lookup.textField] || '');

            element.attr('name', this.uniqueName + '[]').multiselect();
        }

        public setEditValue(source: any, property: Serenity.PropertyItem) {   // entity → editor
            this.element.val(source[property.name] || []).multiselect('refresh');
        }

        public getEditValue(property: Serenity.PropertyItem, target: any) {   // editor → entity
            target[property.name] = this.element.val() || [];
        }
    }

    export interface BSMultiSelectEditorOptions {
        lookupKey: string;
    }
}
```

Build và chạy T4 để có attribute phía C#, rồi dùng thay cho editor cũ:

```csharp
[BSMultiSelectEditor(LookupKey = "MovieDB.Genre"), NotMapped]
[LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")]
public List<Int32> GenreList { ... }
```

Tên attribute là tên class TypeScript (không có namespace), thuộc tính `LookupKey` tương ứng option `lookupKey`.

- Nếu chỉ cần một giá trị chuỗi, đơn giản hơn là cài `IStringValue` (`get value` / `set value`), như editor
  `Recaptcha` của Serenity (bài 22).
- Editor kế thừa `Widget` nên có sẵn `addValidationRule`, `getGridField()`, `idPrefix` (bài 19).
- Phần editor tự viết cơ bản đã có ở bài 13. Bài này thêm phần "plugin bên ngoài".

## 6. Lỗi hay gặp

| Hiện tượng | Nguyên nhân |
|---|---|
| `Morris is not defined` | Script chưa được nạp ở trang đó, hoặc nạp sau file gọi nó. Mở F12 → *Network* xem file có tải được không |
| Biểu đồ rộng 0 hoặc méo | Vẽ khi dialog/tab còn ẩn. Vẽ trong `onDialogOpen`, khi chọn tab, và vẽ lại trong `arrange()` |
| Chạy được khi phát triển, lỗi sau khi publish | File JS/CSS mới không được đưa vào gói publish (*Build Action* phải là *Content*), hoặc bundle có đường dẫn sai mà chỉ lộ ra khi bật bundling (bài 15, bài 25) |
| Mở dialog lần hai biểu đồ hiện hai lần | Vẽ vào một `id` cố định thay vì `idPrefix` |
| Editor mới không lưu giá trị | Thiếu `getEditValue`/`setEditValue`, hoặc thiếu interface trong `registerEditor([...])` |
| Không thấy attribute `[BSMultiSelectEditor]` ở C# | Chưa build TypeScript và chạy T4 |

## 7. Bài tập

1. Viết service `MovieEndpoint.CountByYear` trả về số phim theo từng năm (bài 16, 18). Vẽ thành biểu đồ cột bằng
   Morris (`Morris.Bar`) trong một `TemplatedDialog`, mở từ một nút trên `MovieGrid`.
2. Làm lại bài 1 bằng Chart.js (`Scripts/chartjs/Chart.min.js`), nạp bằng một bundle mới `Pages/MovieStats`.
3. Tạo trang `MovieDB/Dashboard` hiện tổng số phim, số phim mỗi loại, có cache theo `MovieRow.Fields.GenerationKey`.
4. Đặt biểu đồ của bài 1 vào một tab mới của `GenreDialog`, chỉ hiện phim thuộc thể loại đang mở.
5. Bọc một plugin bất kỳ (ví dụ `bootstrap-slider` có sẵn trong `Scripts/`) thành editor cho field `Runtime`.
6. Ở dự án thật: mở `ScriptBundles.json` và `_LayoutHead.cshtml`. Dự án đã thêm những thư viện nào, nạp ở đâu?

## 8. Ghi chú của mình

-
