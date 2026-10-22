# Serenity Platform — Client-side / UI Layer (Forms, Columns, Grids, Dialogs, Editors, Formatters, Lookups, Codegen, Pages, Theming)

> Version baseline: Serenity **10.6.0 (released 2026-10-11)**, targeting .NET 10 (10.0.0 was "First .NET 10 version", 2025-11-24; 9.2.x is the last .NET 8 line) — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md); [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md).
> Source of real code below: the `Serenity.Demo.Northwind` feature project and the Serene template, which now live **inside the Serenity repo** (`common-features/src/demo.northwind`, `serene/src/Serene.Web`). The standalone `serenity-is/Serene` repo's last commit is from Oct 2024 (v8.6 era) and is outdated — prefer the in-repo copy. (Verified by cloning both repos on 2026-10-22.)
> Base URL for code citations: `https://github.com/serenity-is/Serenity/blob/master/` (abbreviated as **GH/** below only in prose; full links given in citations).

---

## 1. How are Form and Columns classes defined in C# and how do they map to UI?

### Takeaway
A Form class (`[FormScript]`) and a Columns class (`[ColumnsScript]`) are plain C# classes whose public properties list the fields of a dialog form / grid columns. With `[BasedOnRow(typeof(XRow), CheckNames = true)]` they inherit display names, editor/formatter types, required flags, lookups etc. from the matching Row properties, and the server serializes them into `PropertyItem[]` JSON (a dynamic "form script" / "columns script") that `PropertyGrid` (dialogs) and `DataGrid` (grids) render on the client.

### Cited Findings

**Form scripts**
- `[FormScript]` marks a class so a form script is generated; the script produces a `PropertyItemsData` JSON structure. `[BasedOnRow(typeof(OrderRow))]` makes form properties inherit attributes (editor type, required, insertable/updatable) from row fields with matching names; `CheckNames = true` validates every property matches a row field, `[SkipNameCheck]` exempts one — [Forms & Editors](https://serenity.is/docs/framework/forms-editors)
- `[Tab]` places a field in a tab and sets `PropertyItem.category`-related info; `[HalfWidth]` = `col-sm-6`, `[FullWidth]` = `col-sm-12`, plus `OneThirdWidth`, `TwoThirdWidth`, `QuarterWidth`, `ThreeQuarterWidth`, all derived from `FormWidthAttribute` (supports per-device widths and `JustThis`/`UntilNext` scope) — [Forms & Editors](https://serenity.is/docs/framework/forms-editors)
- `EditorTypeAttribute` sets the editor type key (e.g. `"Lookup"`, `"Date"`); concrete editor attributes derive from it through `CustomEditorAttribute`. `[Required]` → `required`; `[OneWay]` sends value to server but doesn't load it back; `[HideOnInsert]`/`[HideOnUpdate]` control `visible`; `[LookupEditor(typeof(CustomerRow))]` creates a dropdown from a lookup script. Client loads form data with `getFormDataAsync("Key")` — [Forms & Editors](https://serenity.is/docs/framework/forms-editors)
- Real example — Northwind `OrderForm` uses `[Tab]`, `[CategoryName]` (section header inside a tab), `[DefaultValue("now")]` and a custom detail editor attribute `[OrderDetailsEditor]` on a `List<OrderDetailRow>` property — [OrderForm.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderForm.cs):

```csharp
namespace Serenity.Demo.Northwind.Forms;

[FormScript("Northwind.Order")]
[BasedOnRow(typeof(OrderRow), CheckNames = true)]
public class OrderForm
{
    [Tab("General")]
    [CategoryName("Order")]
    public string? CustomerID { get; set; }
    [DefaultValue("now")]
    public DateTime OrderDate { get; set; }
    public DateTime RequiredDate { get; set; }
    public int? EmployeeID { get; set; }

    [CategoryName("Order Details")]
    [OrderDetailsEditor]                       // custom GridEditorBase editor (master-detail)
    public List<OrderDetailRow>? DetailList { get; set; }

    [Tab("Shipping")]
    [CategoryName("Info")]
    public DateTime ShippedDate { get; set; }
    public int ShipVia { get; set; }
    public decimal Freight { get; set; }

    [CategoryName("Ship To")]
    public string? ShipName { get; set; }
    // ... ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry
}
```

- `CustomerForm` shows `[HalfWidth]` for two-column layouts and `List<int>` for a multi-select (`Representatives`) — [CustomerForm.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerForm.cs)
- Property types in the Form class are mostly irrelevant for UI; the editor is taken from attributes on the form property or (via `BasedOnRow`) from the row/annotation. Example: `OrderRowAnnotations` (an `[AnnotationType(typeof(OrderRow))]` class) puts `[CustomerEditor]`, `[AsyncLookupEditor(typeof(EmployeeRow))]`, `[AsyncLookupEditor(typeof(ShipperRow))]` on row properties, so UI-only dependencies stay out of the entity — [OrderRowAnnotations.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRowAnnotations.cs)
- The generated TS form typing then shows the resolved editor per field: `CustomerID: CustomerEditor; OrderDate: DateEditor; EmployeeID: LookupEditor; DetailList: OrderDetailsEditor; Freight: DecimalEditor; ShipName: StringEditor ...` — [Northwind.OrderForm.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderForm.ts)

**Columns scripts**
- Each public property in a columns class defines one column. Without a key, the script key is the type's full name; with `[ColumnsScript("Northwind.Order")]` the script is registered as `Columns.Northwind.Order` — [Grid Columns](https://serenity.is/docs/framework/grid-columns)
- Attribute → PropertyItem mapping: `Width` → `width` (px); `FixedWidth` → non-resizable width; `AlignCenter`/`AlignRight` → `alignment`; `EditLink` → `editLink` (renders link that opens edit dialog); `Sortable` → `sortable`; `ShowSelection` → `showSelection`; `PinToStart`/`PinToEnd` → `pinnedPosition`; `Focusable`/`Tabbable` → `tabIndex`; `FormatterType` → `formatterType`; `FormatterOption` → `formatterParams`; `DisplayFormat` → `format`; `SummaryType` → `summaryType` (Sum, Avg); `FilteringType` → `filteringType`; `FilteringOption` → `filteringParams`; `QuickFilter` → `quickFilter`; `NotFilterable` → disables filtering — [Grid Columns](https://serenity.is/docs/framework/grid-columns)
- Client: `getColumnsDataAsync("Northwind.Order")` returns columns data (`data.items` is `PropertyItem[]`); synchronous `getColumns`/`getColumnsData` are deprecated. Grids load columns automatically through `getColumnsKey()` — [Grid Columns](https://serenity.is/docs/framework/grid-columns)
- Real example — [OrderColumns.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderColumns.cs):

```csharp
[ColumnsScript("Northwind.Order")]
[BasedOnRow(typeof(OrderRow), CheckNames = true)]
public class OrderColumns
{
    [EditLink, AlignRight, SortOrder(1, descending: true), Width(70), PinToStart]
    public string? OrderID { get; set; }

    [DisplayText(""), AlignCenter, Unbound, FixedWidth(15), PinToStart]
    public string? PrintInvoice { get; set; }          // unbound column, content set in TS createColumns()

    [EditLink, Width(200), QuickFilter, PinToStart]
    public string? CustomerCompanyName { get; set; }

    [EditLink, QuickFilter(CssClass = "hidden-xs")]     // quick filter hidden on phones
    public DateTime? OrderDate { get; set; }

    [Width(140), EmployeeFormatter(GenderProperty = "EmployeeGender"), QuickFilter(CssClass = "hidden-xs")]
    public string? EmployeeFullName { get; set; }

    [FilterOnly, QuickFilter]                           // only appears as a filter, not a column
    public OrderShippingState ShippingState { get; set; }

    [Width(140), ShipperFormatter, QuickFilter(CssClass = "hidden-xs"), QuickFilterOption("multiple", true)]
    public string? ShipViaCompanyName { get; set; }

    [Width(100), QuickFilter(CssClass = "hidden-xs"), AsyncLookupEditor(typeof(Lookups.OrderShipCountryLookup)), CountryWithFlagFormatter]
    public string? ShipCountry { get; set; }

    [Width(100), AsyncLookupEditor(typeof(Lookups.OrderShipCityLookup))]
    [QuickFilter(CssClass = "hidden-xs"), QuickFilterOption("CascadeFrom", "ShipCountry")]   // cascaded quick filter
    public string? ShipCity { get; set; }

    [FreightFormatter]                                  // custom TS formatter (see §3)
    public decimal? Freight { get; set; }
}
```

- `ProductColumns` shows `[DisplayText(TextKey = "Db.Shared.RecordId")]` (localized title), `[HeaderCssClass("align-center")]`, `[EditLink(ItemType = "Demo.Northwind.Supplier")]` (link opens a *different* entity's dialog) — [ProductColumns.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductColumns.cs)
- Generated TS typing: `OrderColumns extends ColumnsBase<OrderRow>` with `static readonly columnsKey = 'Northwind.Order'` and an interface of `Column<OrderRow>` per property, letting TS code do `new OrderColumns(super.createColumns()).PrintInvoice.format = ...` — [Northwind.OrderColumns.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderColumns.ts)

### Inferences
- `[SortOrder(1, descending: true)]`, `[Hidden]`, `[FilterOnly]`, `[Unbound]`, `[QuickFilterOption]`, `[HeaderCssClass]`, `[CategoryName]`, `[DefaultValue]` are confirmed in real code but not listed on the Grid Columns / Forms docs page fetched; their semantics above are inferred from usage (e.g. `FilterOnly` = filter but not visible column).
- Prefer putting editor/formatter attributes on the Row (or an `[AnnotationType]` class) so Form and Columns inherit them; override in Form/Columns only for view-specific differences.

### Gaps
- The fetched Forms docs page did not document `Category`/`CategoryName`, `Hidden`, `ReadOnly`, `Placeholder`, `Hint`, or the editor-inference rules (e.g. `DateTime` → DateEditor, FK with `[LookupEditor]`) — these are inferred from generated typings/real code, not explicitly sourced.

---

## 2. How do EntityGrid and EntityDialog work in modern Serenity TypeScript?

### Takeaway
Modern Serenity (9.0+) uses ES modules + a **static `[Symbol.typeInfo]` registration** (`static override [Symbol.typeInfo] = this.registerClass(nsX)`) instead of `@Decorators.registerClass`. An `EntityGrid<TRow>` needs four overrides (`getColumnsKey`, `getDialogType`, `getRowDefinition`, `getService`); an `EntityDialog<TRow>` needs three (`getFormKey`, `getRowDefinition`, `getService`) plus a typed `form = new XForm(this)`. Customization is done by overriding protected virtual methods (`createColumns`, `getButtons`, `getQuickFilters`, `onClick`, `getToolbarButtons`, `updateInterface`, `afterLoadEntity`, `onSaveSuccess`, etc.).

### Cited Findings

**Registration (deprecation of decorators)**
- 9.0.0: "Type registration via decorators (e.g., `Decorators.registerClass`, `Decorators.registerEditor`, etc.) is now deprecated (it still works but is not recommended)". Use `static [Symbol.typeInfo] = this.registerClass("MyProject.MyModule.MyType")`. Reason: decorator info is not emitted in `.d.ts` files, and an esbuild bug with decorators + property initializers. Attributes/interfaces go in the 2nd arg: `this.registerClass("…", [new PanelAttribute(true)])`. Namespace ending with a dot auto-appends class name. Sergen/Pro.Coder generate `Modules/ServerTypes/Namespaces.ts` constants (`nsMyModule`) — [changelog-2025.md (9.0.0)](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- If you need third-party decorators: `static [Symbol.typeInfo] = classTypeInfo("…"); static { registerType(this); }` — same source.
- A new `Attributes` namespace works like `@Decorators` for `[Symbol.typeInfo]` registration, e.g. `[Attributes.panel()]` instead of `[new PanelAttribute()]`; later (9.1) "No need to call Attributes.factoryFunction()… `[Attributes.someAttr]` is enough" — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- `@Decorators.option` deprecated (not JSX-compatible; use constructor props) — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)

**What sergen generates for a new table (templates)**
- [GridModular.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/GridModular.scriban) / [DialogModular.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/DialogModular.scriban):

```ts
export class XGrid extends EntityGrid<XRow> {
    static override [Symbol.typeInfo] = this.registerClass("MyApp.MyModule.");
    protected override getColumnsKey() { return XColumns.columnsKey; }
    protected override getDialogType() { return XDialog; }
    protected override getRowDefinition() { return XRow; }
    protected override getService() { return XService.baseUrl; }
}

export class XDialog extends EntityDialog<XRow, any> {
    static override [Symbol.typeInfo] = this.registerClass("MyApp.MyModule.");
    protected override getFormKey() { return XForm.formKey; }
    protected override getRowDefinition() { return XRow; }
    protected override getService() { return XService.baseUrl; }
    protected form = new XForm(this);
}
```
- Sergen generates `.tsx` (not `.ts`) for Dialog, Grid and Page files (since 2024) — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)

**EntityGrid behavior**
- List request goes to `~/Services/<service>/List`; `getButtons()` builds Add, Refresh, Column Picker and Filter; Add is disabled with `disabled: () => !this.hasInsertPermission() || this.readOnly`; `addButtonClick()` calls `editItem` with an empty object; `editItem` creates dialog via `createEntityDialog()`, calls `dialog.load(entityOrId, …)`, then `dialog.dialogOpen(this.openDialogsAsPanel ?? DataGrid.defaultOptions.openDialogsAsPanel)` — [EntityGrid CRUD](https://serenity.is/docs/grids/entitygrid-crud)
- `getColumns()` is deprecated → override `createColumns()`; current visible columns via `.columns` / `.sleekGrid.getColumns()`, all via `.allColumns` — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- `DataGrid` protected hooks in corelib source: `getInitialTitle()`, `createToolbarExtensions()`, `getQuickSearchFields(): QuickSearchField[]` (default `null`), `createQuickSearchInput()`, `getItemCssClass(item, index)`, `getIncludeColumns(include)`, `onViewSubmit()`, `getDefaultSortBy()`, `usePager()` (default `false`), `getViewOptions()`, `getSlickOptions()`, `addQuickFilter()`, `addDateRangeFilter()`, `dateRangeQuickFilter()`, `addDateTimeRangeFilter()`, `addBooleanFilter()` — [datagrid.tsx](https://github.com/serenity-is/Serenity/blob/master/packages/corelib/src/ui/datagrid/datagrid.tsx)
- Since 8.x, all dialogs deriving from `EntityDialog` are **panels by default**; `GridEditorDialog`/`PropertyDialog` are modal by default — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)

**Real EntityGrid with customizations** — [OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx):

```tsx
import { Attributes, EntityGrid, EnumEditor, Fluent, LookupEditor, ToolButton, faIcon, toId } from "@serenity-is/corelib";
import { ExcelExportHelper, PdfExportHelper, ReportHelper } from "@serenity-is/extensions";
import { OrderColumns, OrderListRequest, OrderRow, OrderService, ProductRow } from "../ServerTypes/Demo";
import { nsDemoNorthwind } from "../ServerTypes/Namespaces";
import { OrderDialog } from "./OrderDialog";
import "./OrderGrid.css";                                        // CSS imported as ES module

export class OrderGrid<P = {}> extends EntityGrid<OrderRow, P> {
    static override[Symbol.typeInfo] = this.registerClass(nsDemoNorthwind, [Attributes.advancedFiltering]);

    protected override getColumnsKey() { return OrderColumns.columnsKey; }
    protected override getDialogType() { return OrderDialog as any; }
    protected override getRowDefinition() { return OrderRow; }
    protected override getService() { return OrderService.baseUrl; }

    protected override getQuickFilters() {                      // add a custom quick filter
        const filters = super.getQuickFilters();
        filters.push({
            type: LookupEditor,
            options: { lookupKey: ProductRow.lookupKey, async: true },
            field: 'ProductID',
            title: 'Contains Product in Details',
            handler: w => { (this.view.params as OrderListRequest).ProductID = toId(w.value); },
            cssClass: 'hidden-xs'
        });
        return filters;
    }

    protected override getButtons(): ToolButton[] {              // add toolbar buttons
        const buttons = super.getButtons();
        buttons.push(ExcelExportHelper.createToolButton({ grid: this, service: OrderService.baseUrl + '/ListExcel', separator: true }));
        buttons.push(PdfExportHelper.createToolButton({ grid: this }));
        return buttons;
    }

    protected override createColumns() {                        // customize column rendering with JSX
        const columns = new OrderColumns(super.createColumns());
        columns.PrintInvoice.format = () => <a class="inline-action" data-action="print-invoice" title="invoice">
            <i class={faIcon("file-pdf", "red")}></i></a>;
        return columns.valueOf();
    }

    protected override onClick(e: Event, row: number, cell: number) {   // inline action handler
        super.onClick(e, row, cell);
        if (Fluent.isDefaultPrevented(e)) return;
        const item = this.itemAt(row);
        const action = (e.target as HTMLElement)?.closest(".inline-action")?.getAttribute("data-action");
        if (action == "print-invoice") {
            e.preventDefault();
            ReportHelper.execute({ reportKey: 'Northwind.OrderDetail', params: { OrderID: item.OrderID } });
        }
    }

    protected override addButtonClick() {                       // prefill new entity
        const eq = this.view.params.EqualityFilter;
        this.editItem({ CustomerID: eq ? eq.CustomerID : null });
    }
}
```
- `createQuickFilters()` + `this.findQuickFilter(EnumEditor, OrderRow.Fields.ShippingState)` retrieves a quick filter widget for later programmatic setting — same file.

**Real EntityDialog** — [OrderDialog.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderDialog.ts):

```ts
export class OrderDialog<P = {}> extends EntityDialog<OrderRow, P> {
    static override[Symbol.typeInfo] = this.registerClass(nsDemoNorthwind);
    protected override getFormKey() { return OrderForm.formKey; }
    protected override getRowDefinition() { return OrderRow; }
    protected override getService() { return OrderService.baseUrl; }
    protected form = new OrderForm(this);                       // typed access: this.form.CustomerID is a CustomerEditor

    protected override getToolbarButtons() {
        const buttons = super.getToolbarButtons();
        buttons.push(ReportHelper.createToolButton({
            title: 'Invoice', cssClass: 'export-pdf-button', reportKey: 'Northwind.OrderDetail',
            getParams: () => ({ OrderID: this.entityId })
        }));
        return buttons;
    }
    protected override updateInterface() {                      // show/hide per mode
        super.updateInterface();
        this.toolbar.findButton('export-pdf-button').toggle(this.isEditMode());
    }
    protected override afterLoadEntity() {                      // wire master-detail
        super.afterLoadEntity();
        this.form.DetailList.orderId = this.entityId;
    }
}
```
- EntityDialog docs: `getToolbarButtons()` builds standard buttons; override `validateBeforeSave()` for custom validation; `onSaveSuccess()` handles the save response; `getSaveRequest()` builds the request; `load(entityOrId, done, fail)` — `null` = new, string/number → `loadById()`, object loaded directly; modes edit/insert/view/deleted checked via `isEditMode()`, `isViewMode()`, `isDeleted()`; Save disabled when `!hasSavePermission()` or `this.readOnly`; `doDelete`/`undelete` with confirmation; `hasUndeletePermission()`; optimistic concurrency via `concurrencyVersionProperty` — [EntityDialog and CRUD Workflows](https://serenity.is/docs/dialogs/entitydialog)
- 10.6 added `undeletePermission` on row definitions with `getUndeletePermission`/`hasUndeletePermission` on EntityGrid/EntityDialog — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Custom dialog layout with JSX via `renderContents()` and `useIdPrefix()` — `CustomerDialog` renders tabs (Info / Notes / Orders), embeds a `<CustomerOrdersGrid ref={grid => …} />` widget directly in JSX, overrides `loadEntity` to disable the Orders tab for new records (`TabsExtensions.setDisabled(this.tabs, 'Orders', this.isNewOrDeleted())`), and calls `reloadLookup('Northwind.Customer')` in `onSaveSuccess` — [CustomerDialog.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerDialog.tsx)
- Editor change event + validation rule in a dialog constructor: `this.form.ProductID.changeSelect2(async e => { … this.form.UnitPrice.value = (await ProductRow.getLookupAsync()).itemById[productID].UnitPrice; })` and `this.form.Discount.addValidationRule(this.uniqueName, e => { … return "Discount can't be higher than total price!"; })` — [OrderDetailDialog.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/OrderDetail/OrderDetailDialog.ts)

**Quick search / filters**
- Quick filters are toolbar editors created from `quickFilter: true` columns or `getQuickFilters()`; definition supports `field`, `type`, `title`, `handler`, `options`, `separator`, `cssClass`, `loadState`, `saveState`, `displayText`; `QuickFilterBar` renders them. Quick search: `createQuickSearchInput()`; override `getQuickSearchFields()` returning `{ name, title }` entries. Advanced filters: `FilterPanel` in a `FilterDialog`, `FilterDisplayBar`, `FilterStore`; operators come from `filteringType` (`StringFiltering`, `DateFiltering`, `LookupFiltering`), custom types in `FilteringTypeRegistry`. Flow: filter change → `view.params` updated → `populate()` builds `ListRequest` with criteria → server filters — [Filtering and Quick Search](https://serenity.is/docs/grids/filtering)
- Advanced filter UI is enabled per grid by `[Attributes.advancedFiltering]` in registration (see OrderGrid above) — [OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx)
- Filtering a child grid: `this.setEquality('CustomerID', value); this.refresh();` and `getGridCanLoad()` override to avoid loading until parent ID known — [CustomerOrdersGrid.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerOrdersGrid.ts)
- Reading querystring into a quick filter's initial value: `category.init = e => { EditorUtils.setValue(e.element.tryGetWidget(), q["cat"]); }` — [ProductGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductGrid.tsx)

### Inferences
- Learning path: start from the 4+3 required overrides, then learn the hook methods in the order a user perceives them: columns (`createColumns`) → toolbar (`getButtons`) → filters (`getQuickFilters`, `getQuickSearchFields`) → row actions (`onClick`) → dialog lifecycle (`loadEntity`/`afterLoadEntity`/`updateInterface`/`onSaveSuccess`).
- Subclassing grids is idiomatic: `CustomerOrdersGrid extends OrderGrid` and only changes dialog type, removes a column, and adds equality filtering.

### Gaps
- Docs page did not cover `getDefaultSortBy`/`getInitialTitle`/`gridPageInit` explicitly; their existence is cited from source only.

---

## 3. Editors, formatters, lookup scripts — and writing custom ones

### Takeaway
Editors are `Widget`s registered with `registerEditor` and referenced from C# via an editor attribute (built-in like `[LookupEditor]`, `[DateEditor]`, `[ImageUploadEditor]`, or a sergen-generated attribute for your TS editor). Formatters are classes implementing `Formatter.format(ctx)` (returning DOM via JSX), registered with `formatterTypeInfo` + `registerType`. Lookups are server-side cached dynamic scripts (`[LookupScript]`, `RowLookupScript<T>`); for large/changing tables the docs now recommend `ServiceLookupEditor`.

### Cited Findings

**Built-in editors**
- corelib editors folder contains: BooleanEditor, CheckTreeEditor, Combobox(Editor), DateEditor, DateTimeEditor, DateYearEditor, DecimalEditor, EmailAddressEditor, EmailEditor, EnumEditor, HtmlContentEditor (+ Tiptap variant), Int64Editor, IntegerEditor, LookupEditor, MaskedEditor, PasswordEditor, RadioButtonEditor, Recaptcha, Select2 (embedded TS port), SelectEditor, ServiceLookupEditor, StringEditor, TextAreaEditor, TimeEditor, upload editors, UrlEditor — [corelib/src/ui/editors](https://github.com/serenity-is/Serenity/tree/master/packages/corelib/src/ui/editors)
- C# editor attributes: AsyncLookupEditor, BooleanEditor, CheckLookupEditor, DateEditor, DateTimeEditor, DateYearEditor, DecimalEditor, DistinctValuesEditor, EmailAddressEditor, EnumEditor, HtmlContentEditor, HtmlNoteContentEditor, HtmlReportContentEditor, Int64Editor, IntegerEditor, LookupEditor, MaskedEditor, PasswordEditor, RadioButtonEditor, Recaptcha, ServiceLookupEditor, StringEditor, TextAreaEditor, TimeEditor, TimeSpanEditor, UrlEditor — [EditorTypes folder](https://github.com/serenity-is/Serenity/tree/master/src/core/ComponentModel/PropertyGrid/EditorTypes)
- `LookupEditorBaseAttribute` options: `LookupKey`, `AutoComplete`, `InplaceAdd`, `InplaceAddPermission`, `DialogType`, `CascadeFrom`, `CascadeField`, `CascadeValue`, `FilterField`, `FilterValue`, `MinimumResultsForSearch`, `Multiple`, `Delimited`, `OpenDialogAsPanel`, `Async` — [LookupEditorBaseAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/src/core/ComponentModel/PropertyGrid/EditorTypes/LookupEditorBaseAttribute.cs)
- Real usages on rows: `[AsyncLookupEditor(typeof(Lookups.CustomerCityLookup), CascadeFrom = "Country", AutoComplete = true)]` (cascaded City by Country) — [CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs); `[LookupEditor(typeof(SupplierRow), InplaceAdd = true, DialogType = "Demo.Northwind.SupplierDialog", Async = true)]` and `[ServiceLookupEditor(typeof(CategoryRow), InplaceAdd = true, DialogType = "Demo.Northwind.CategoryDialog")]`; `[ImageUploadEditor(FilenameFormat = "ProductImage/~", CopyToHistory = true)]` — [ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs)
- Upload editors (`ImageUploadEditor`, `MultipleImageUploadEditor`) derive from `BaseUploadEditorAttribute` with options `UploadIntent`, `AllowNonImage`, `MaxSize`/`MinSize`, `MaxWidth`/`MaxHeight`/`MinWidth`/`MinHeight`, `ScaleWidth`/`ScaleHeight`/`ScaleQuality`/`ScaleMode`, `ThumbWidth`/`ThumbHeight`/`ThumbSizes`/`ThumbMode`, `JsonEncodeValue`, `OriginalNameProperty`, `FilenameFormat` — [BaseUploadEditorAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/src/core/ComponentModel/Upload/BaseUploadEditorAttribute.cs)
- 10.6 breaking: ImageSharp removed; image validation/resizing requires an `IImageProcessor` registration (Serene/StartSharp ship a SkiaSharp implementation) — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- 10.6: `ServiceLookupEditor` uses `[ListRequestCapabilities]`, can infer lookup endpoints from row metadata — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)

**Lookup scripts**
- `[LookupScript]` defines a lookup served from the `DynJS` endpoint; default key like `Lookup.Administration.Role`, or explicit `[LookupScript("MyModule.MyLookup")]`. Custom lookups derive from `RowLookupScript<TRow>`, override `PrepareQuery`, set `IdField`. Items include only Id + Name by default; add `[LookupInclude]` for more fields. Row `ReadPermission` carries over; `Permission = "*"`/`"?"`. Default cache "Infinite"; invalidate via `ExpireGroupItems` or `IDynamicScriptManager.Changed(...)`. Client: prefer `getLookupAsync`; `getLookup` is obsolete (sync XHR); `reloadLookup('Key')`. `ServiceLookupEditor` "never loads all the records", "does not cache anything by default"; docs recommend it for new apps; lookups suit small, rarely-changing tables; don't put sensitive data in lookups — [Lookups](https://serenity.is/docs/framework/dynamic-scripts/lookups)
- Real custom lookup (distinct ship cities) — [OrderShipCityLookup.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderShipCityLookup.cs):

```csharp
[LookupScript, NorthwindModule]
public class OrderShipCityLookup : RowLookupScript<OrderRow>
{
    public OrderShipCityLookup(ISqlConnections sqlConnections) : base(sqlConnections)
    {
        IdField = TextField = OrderRow.Fields.ShipCity.PropertyName;
    }
    protected override void PrepareQuery(SqlQuery query)
    {
        var fld = OrderRow.Fields;
        query.Distinct(true).Select(fld.ShipCountry).Select(fld.ShipCity)
             .Where(fld.ShipCountry != "" & fld.ShipCountry.IsNotNull() & fld.ShipCity != "" & fld.ShipCity.IsNotNull());
    }
    protected override void ApplyOrder(SqlQuery query) { }
}
```
- Non-async lookups can be preloaded in a page view: `@Html.Script("dynamic://Lookup.Northwind.Customer")` — [OrderIndex.cshtml](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Areas/Serenity.Demo.Northwind/Order/OrderIndex.cshtml)

**Custom editors**
- A custom editor is a `Widget` (or `EditorWidget`) implementing value interfaces (`IDoubleValue`, `IStringValue`, `IGetEditValue`/`ISetEditValue`, `IReadOnly`) that `PropertyGrid` uses to load/save values; register with `static override[Symbol.typeInfo] = this.registerEditor(nsX, [IDoubleValue, IReadOnly]);` override `createDefaultElement()`; constructor takes `props: EditorProps<P>` (covers `initialValue`, `maxLength`, `placeholder`, `required`, `readOnly`); server side `[CustomEditor("MyApp.MyEditor")]` — [Custom Editors](https://serenity.is/docs/editors/custom-editors)
- `sergen clienttypes` collects editor/formatter registrations from `.ts` files and generates C# attribute files in `Imports/ClientTypes` (so `[PhoneEditor]`, `[CustomerEditor]`, `[FreightFormatter]` become usable in C#) — [Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)
- Real custom editor extending StringEditor with validation & formatting — [PhoneEditor.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Shared/PhoneEditor.ts):

```ts
export interface PhoneEditorOptions { multiple?: boolean; }

export class PhoneEditor<P extends PhoneEditorOptions = PhoneEditorOptions> extends StringEditor<P> {
    static override [Symbol.typeInfo] = this.registerEditor(nsDemoNorthwind);

    constructor(props: WidgetProps<P>) {
        super(props);
        this.addValidationRule(this.uniqueName, e => {
            const value = this.get_value()?.trim();
            if (!value) return null;
            return PhoneEditor.validate(value, this.props?.multiple);   // returns error text or null
        });
        (this.domNode as HTMLInputElement).addEventListener('change', () => this.formatValue());
    }
    override get_value() { return this.getFormattedValue(); }
    override set_value(value: string) { (this.domNode as HTMLInputElement).value = value; }
    // ...
}
```
- Custom lookup editor subclass (fixed lookup key, custom item text, async default) — [CustomerEditor.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerEditor.ts):

```ts
export class CustomerEditor<P extends LookupEditorOptions = LookupEditorOptions> extends LookupEditorBase<P, CustomerRow> {
    static override[Symbol.typeInfo] = this.registerEditor(nsDemoNorthwind);
    constructor(props: EditorProps<P>) { super({ async: true, ...props }); }
    protected override getLookupKey() { return 'Northwind.Customer'; }
    protected override getItemText(item, lookup) { return super.getItemText(item, lookup) + ' [' + item.CustomerID + ']'; }
}
```

**Formatters**
- Real custom formatter — [FreightFormatter.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/FreightFormatter.tsx):

```tsx
import { Formatter, faIcon, formatterTypeInfo, registerType } from "@serenity-is/corelib";
import { FormatterContext, FormatterResult } from "@serenity-is/sleekgrid";

export class FreightFormatter implements Formatter {
    static [Symbol.typeInfo] = formatterTypeInfo(nsDemoNorthwind); static { registerType(this); }
    format(ctx: FormatterContext): FormatterResult {
        if (ctx.value == null) return "";
        return <>{ctx.value} <i class={[faIcon("balance-scale", "secondary"), "text-opacity-75"]}></i></>;
    }
}
```
- Alternative: `class MyFormatter extends FormatterBase implements Formatter { static [Symbol.typeInfo] = this.registerFormatter("…"); }` — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- **Breaking (9.x):** `gridDefaults.enableHtmlRendering` is now false by default — strings returned from formatters are treated as **text**, not HTML; convert formatters to return HTML elements (domwise). `ctx.escape()` then returns strings as-is. Use the `formatterContext` helper instead of constructing contexts — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- Formatter that lazily loads a lookup and calls `ctx.grid?.invalidate()` when loaded — [EmployeeListFormatter.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/EmployeeListFormatter.tsx)
- Docs list of built-in formatter types: [Formatter Types](https://serenity.is/docs/grids/formatter_types) (Date, Number, Enum, etc. referenced via `FormatterTypeAttribute` — [Grid Columns](https://serenity.is/docs/framework/grid-columns))

### Inferences
- Workflow to add a custom editor: write TS class with `registerEditor` → build (sergen `clienttypes`/`mvct` generates `[XEditor]` C# attribute in `Imports/ClientTypes`) → put `[XEditor]` on Row/Form property → `servertypings` regenerates the form typing so `this.form.Field` is typed as `XEditor`.

### Gaps
- Exact rules of `IGetEditValue`/`ISetEditValue` (`getEditValue(property, target)`/`setEditValue(source, property)`) not fetched from docs; implemented by `GridEditorBase` per source (see §6).

---

## 4. TypeScript code generation (sergen / ServerTypes / tsbuild) and page registration

### Takeaway
On every `dotnet build`, MSBuild targets run `dotnet sergen` to (a) generate C# helpers from TS (`Imports/ClientTypes`, `Imports/MVC` incl. `ESM` entry-point constants) and (b) after compilation, generate TS from C# into `Modules/ServerTypes/` (Row/Form/Columns/Service/enums/texts/`Namespaces.ts`), then run `node ./tsbuild.js` (esbuild) to bundle every `*Page.ts(x)` entry into `wwwroot/esm`. A page = an MVC controller with `[PageAuthorize]` + `[Route]` returning `this.GridPage<TRow>(ESM.XPage)`, an ES module `XPage.ts` exporting `default () => gridPageInit(XGrid)`, and an assembly-level `[NavigationLink]`.

### Cited Findings
- Sergen commands: `generate`/`g` (table code generator; R/S/U/C = Row, Repo+Svc, UI, Custom; dots in module names become `/`), `restore`/`r`, `clienttypes`/`c` (TS editor/formatter registrations → C# attributes in `Imports/ClientTypes`), `servertypings`/`s` (C# → TS for columns, forms, rows, services, enums, permissions, request/response), `mvc`/`m` (view path helpers in `Imports/MVC`), `transform`/`t` (runs clienttypes + mvc + servertypings) — [Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)
- Serene template build targets: `TransformMvcClientTypes` (after ResolveProjectReferences) runs `dotnet tool restore` + `$(DotNetSergen) mvct …` and re-includes `Imports\**\*.cs`; `TransformServerTypings` (AfterBuild) runs `$(DotNetSergen) servertypings …`; `TransformRunTSBuild` (AfterBuild, depends on RestoreNodeTypes;NpmInstall;TransformServerTypings) runs `node ./tsbuild.js` unless `SkipTSBuild` — [Serene.Web.csproj](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Serene.Web.csproj)
- `RestoreNodeTypes` copies NuGet-packaged TS libs into `node_modules/.dotnet/<package>` so `@serenity-is/corelib` etc. resolve from NuGet — [Serenity.Net.Web.targets](https://github.com/serenity-is/Serenity/blob/master/src/web/Serenity.Net.Web.targets)
- Current template: `net10.0`, `<Using Include="Serene.MVC.ESM" Alias="ESM" />`, packages Serenity.Corelib/Net.Web/Extensions/Assets 10.6.0 — [Serene.Web.csproj](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Serene.Web.csproj); `sergen.json` = `{"Extends": "defaults@9.0.0", "RootNamespace": "Serene", …}` — [sergen.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/sergen.json)
- `tsbuild.js`: `import { build } from "@serenity-is/tsbuild"; await build({});` — [tsbuild.js](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/tsbuild.js); feature project variant comments "wwwroot/esm/**/*.js: ESM entry points for own pages" and builds a `dist/index.js` bundle with `external: ['@serenity-is/*']` — [demo.northwind/tsbuild.js](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/tsbuild.js). `Modules/**/*.mts` added to default entry points; `npmCopy` copies `~/npm/` bundle entries from node_modules to wwwroot/npm — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- `tsconfig.json`: `"module": "ESNext"`, `"moduleResolution": "bundler"`, `"jsx": "react-jsx"`, `"jsxImportSource": "@serenity-is/domwise"`, `"target": "ES2022"`, `"noEmit": true` (esbuild emits), `paths` mapping `@serenity-is/*` to `node_modules/.dotnet/serenity.*/dist/index`, `react`→`preact/compat` — [tsconfig.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/tsconfig.json)
- Generated service proxy (`OrderService`): `baseUrl = 'Northwind/Order'`, typed `Create/Update/Delete/Retrieve/List` functions calling `serviceRequest(baseUrl + '/' + x, …)`, plus a `Methods` const — [Northwind.OrderService.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderService.ts); generated row typing: interface with optional fields + `abstract class OrderRow { static readonly idProperty, nameProperty, localTextPrefix, …Permission; static readonly Fields = fieldsProxy<OrderRow>(); }` — [Northwind.OrderRow.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderRow.ts); generated form: `class OrderForm extends PrefixedContext { static readonly formKey = 'Northwind.Order'; … initFormType(OrderForm, ['CustomerID', CustomerEditor, …]) }` — [Northwind.OrderForm.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/ServerTypes/Demo/Northwind.OrderForm.ts)
- Built-in corelib/SleekGrid types are included in codegen (fewer errors if npm install fails) — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)

**Pages & navigation**
- Page controller (sergen template): `[PageAuthorize(typeof(XRow))] public class XPage : Controller { [Route("Module/X")] public ActionResult Index() => this.GridPage<XRow>("…module path…"); }` and page TS: `export default () => gridPageInit(XGrid);` — [PageModular.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/PageModular.scriban), [PageModularTS.scriban](https://github.com/serenity-is/Serenity/blob/master/src/codegenerator/Templates/PageModularTS.scriban)
- Real: [CustomerPage.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerPage.cs) (`return this.GridPage<CustomerRow>(ESM.CustomerPage);`) + [CustomerPage.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerPage.ts) (`export default () => gridPageInit(CustomerGrid);`). Custom view variant: `OrderPage` returns `View(MVC.Views.Order.OrderIndex)`; the cshtml has `<div id="GridDiv"></div>` and `@Html.ModulePageInit(ESM.OrderPage)`; `OrderPage.ts` reads querystring and calls `orderGrid.set_shippingState(...)` — [OrderPage.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderPage.cs), [OrderIndex.cshtml](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Areas/Serenity.Demo.Northwind/Order/OrderIndex.cshtml), [OrderPage.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderPage.ts)
- `GridPage<TRow>(this Controller, string module, options, layout, pageTitle)` returns a `ModulePageResult` — [ModulePageExtensions.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/extensions/Modules/ModulePage/ModulePageExtensions.cs); `gridPageInit` / `panelPageInit` live in corelib — [layout.ts](https://github.com/serenity-is/Serenity/blob/master/packages/corelib/src/compat/layout.ts)
- Navigation attributes (assembly-level): `NavigationMenu` (no link; parent), `NavigationLink` (with `typeof(Page)` the URL and permission are resolved from the controller's `[Route]` and `[PageAuthorize]`), `NavigationSection`, `NavigationGroup`; `/` separates hierarchy; `order` sorts; `icon` is a Font Awesome class; users without the permission don't see the item — [Navigation](https://serenity.is/docs/framework/navigation)
- Real: `[assembly: NavigationMenu(7000, "Northwind", icon: "fa-anchor")]`, `[assembly: NavigationLink(7200, "Northwind/Orders", typeof(Northwind.OrderPage), icon: "fa-shopping-cart")]` — [NorthwindNavigation.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Shared/NorthwindNavigation.cs)
- 10.6 breaking: permission checks fail closed — rows without explicit permissions are denied; use `SpecialPermissionKeys.Public` (`*`) or `LoggedIn` (`?`) — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Global client init in `Modules/Common/ScriptInit.ts`: `Config.rootNamespaces.push('Serene')`, `gridDefaults.sanitizer = DOMPurify.sanitize`, flatpickr localization, `window.onerror = ErrorHandling.runtimeErrorHandler` — [ScriptInit.ts](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/ScriptInit.ts)

### Inferences
- Mental model of a feature: `XRow.cs` → `XColumns.cs` + `XForm.cs` (+ `XLookup.cs`) → [build] → `ServerTypes/*.ts` → `XGrid.tsx`/`XDialog.tsx`/`XPage.ts` → [tsbuild] → `wwwroot/esm/.../XPage.js` loaded by `XPage.cs` → `[NavigationLink]` for menu.
- The "Pro.Coder" source-generator alternative (StartSharp) is mentioned in the changelog alongside Sergen but not detailed here.

### Gaps
- `sergen.json` full option schema (e.g. `ServerTypings.LocalTexts`, relative path preference) not documented in fetched pages; seen in old Serene `sergen.json` (`"ServerTypings": { "LocalTexts": true }`) only.

---

## 5. SleekGrid and the modern DOM/JSX (DomWise) approach vs older jQuery

### Takeaway
SleekGrid (`@serenity-is/sleekgrid`) is Serenity's TypeScript rewrite/fork of SlickGrid (virtualized data grid), now released in lockstep with Serenity, with a CSS-Grid-based layout, pinned columns/frozen rows and a safer formatter pipeline. UI markup is now written with **DomWise** JSX (`@serenity-is/domwise`, successor of jsx-dom): JSX compiles to real DOM nodes (no virtual DOM) with built-in Preact signals. jQuery, jQuery UI, jQuery Validate, Select2-jQuery, autoNumeric have been removed as dependencies; `Fluent` replaces jQuery for imperative DOM work.

### Cited Findings
- `@serenity-is/domwise` is "our new library to create DOM elements with full TypeScript support… based on jsx-dom with integrated signal support via @preact/signals-core"; remove `jsx-dom` from package.json and set `jsxImportSource` to `@serenity-is/domwise`; attributes are lowercased (`readonly`, `for`) with aliases (`tabIndex`, `htmlFor`, `maxLength`); event handlers stay camelCase (`onClick`) — [changelog-2025.md (9.x)](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- DomWise "compiles JSX directly into real DOM nodes"; no virtual DOM, diffing or reconciliation — deliberate because widgets mutate DOM imperatively; "Signal children create reactive text nodes that update in place"; `ref` via `createRef` or callback (class components → instance) — [JSX with DomWise](https://serenity.is/docs/framework/ui/jsx)
- domwise is exported from corelib (no duplication across feature projects) — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md); 10.6 extends DomWise signals with conditional `Show` rendering, iterable/selection bindings, `withLifecycleRoot`, auto-disposal with widget lifecycles — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Related docs: [Signals and Reactivity](https://serenity.is/docs/framework/ui/signals), [Fluent](https://serenity.is/docs/framework/ui/fluent), [Components and Hooks](https://serenity.is/docs/framework/ui/components), [Type Registration](https://serenity.is/docs/framework/ui/type-registration)
- History: jsx-dom integration added 2023 (`renderContents()` instead of `getTemplate()`, `useIdPrefix()`) — [changelog-2023.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2023.md); 2024 "removing the jQuery dependency, along with… Select2, jQuery Validate, autoNumeric… converted to TypeScript without jQuery dependencies and embedded into Serenity Corelib"; widgets usable directly in JSX without wrapping; `TemplatedWidget`/`TemplatedDialog`/`TemplatedPanel` deprecated → `Widget`/`BaseDialog`/`BasePanel` + `renderContents()` — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md); 9.0 removed jquery-ui, jquery.colorbox, jquery.cookie, jquery.event.drag, jquery.maskedinput, jspdf from Serenity.Assets — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)
- `Fluent.toggle` now uses the `hidden` attribute instead of `style.display = none` — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md); `Fluent.isDefaultPrevented` helper handles both jQuery and native events — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)
- SleekGrid: `Grid` renamed to `SleekGrid`; layout rewritten on CSS Grid; supports dynamic pinned columns (start/end) and frozen rows (top/bottom) responsively reduced on small viewports; `EnhancedLayout` (StartSharp) replaces `FrozenLayout`; touch-friendly column dragging; DOMParser-based sanitizer and corelib `sanitizeHtml`; `ISleekGrid`/`IRemoteView` interfaces; redesigned event system — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md); `styleNonce` option for strict CSP — same source (9.2.0)
- SleekGrid API reference — [@serenity-is/sleekgrid API](https://serenity.is/docs/api/js/sleekgrid/README); DataGrid architecture — [DataGrid Architecture](https://serenity.is/docs/grids/architecture)
- Access the underlying grid from a DataGrid: `this.sleekGrid.getCellFromEvent(e)`; delegated events on `this.slickContainer.on('change', 'input.edit, …', handler)` — [ProductGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductGrid.tsx)

**Deprecated approaches to flag**
- `namespace MyApp.MyModule { … }` global-script style, `Q.*` namespace, Saltarelle (C#→JS) — replaced by ES modules (`import … from "@serenity-is/corelib"`) — inferred from the 2023 plan to move `Q` functions to base library and from all current samples using ES imports ([changelog-2023.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2023.md); Saltarelle references in [changelog-2017.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2017.md)).
- `@Decorators.registerClass/registerEditor/registerFormatter`, `@Decorators.option` → `static [Symbol.typeInfo]` (§2).
- `getTemplate()`/`TemplatedWidget` → `renderContents()` JSX.
- `getColumns()` override → `createColumns()`.
- `jsx-dom` → `@serenity-is/domwise`.
- Returning HTML strings from formatters → return DOM nodes (HTML rendering disabled by default).
- `getLookup` (sync) → `getLookupAsync`.

### Inferences
- When copying old tutorials (pre-2024) expect to translate: `$(...)`/jQuery → `Fluent`/native DOM; `this.element.find(...)` → `this.byId(...)`/`this.domNode.querySelector`; decorators → static typeInfo.

### Gaps
- Exact SleekGrid vs SlickGrid API diff list not compiled; see API reference.

---

## 6. Master-detail editing, inline editing, cascaded editors, filters

### Takeaway
Master-detail in-form editing uses `GridEditorBase<TDetailRow>` (from `@serenity-is/extensions`) as an editor (implements `IGetEditValue/ISetEditValue`) bound to a `List<DetailRow>` property with `[MasterDetailRelation]` on the row; details open in a `GridEditorDialog`. It works in-memory (saved with the master) or in "connected mode" (each detail saved immediately via its own service). Inline editing is either SleekGrid cell editing or formatter-rendered inputs (Product grid sample). Cascades use `CascadeFrom`/`CascadeField` on lookup editors and `QuickFilterOption("CascadeFrom", …)` on quick filters.

### Cited Findings
- Row side: `[DisplayText("Details"), MasterDetailRelation(foreignKey: nameof(OrderDetailRow.OrderID)), NotMapped] DetailList` — [OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs)
- `GridEditorBase` is registered `this.registerEditor(nsExtensions, [IGetEditValue, ISetEditValue])`; `save()` in connected mode merges with existing item, calls `validateEntity`; `getGridCanLoad()` returns true only in connected mode; `connectedModeChanged()` toggles refresh button and quick search — [GridEditorBase.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/extensions/Modules/GridEditor/GridEditorBase.tsx)
- Real detail editor — [OrderDetailsEditor.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/OrderDetail/OrderDetailsEditor.ts):

```ts
export class OrderDetailsEditor<P = {}> extends GridEditorBase<OrderDetailRow, P> {
    static override[Symbol.typeInfo] = this.registerEditor(nsDemoNorthwind);
    protected override getColumnsKey() { return OrderDetailColumns.columnsKey; }
    protected override getDialogType() { return OrderDetailDialog; }        // extends GridEditorDialog
    protected override getRowDefinition() { return OrderDetailRow; }
    protected override getService() { return OrderDetailService.baseUrl; }

    protected override async validateEntity(row: OrderDetailRow, id: any) {
        row.ProductID = toId(row.ProductID);
        const sameProduct = this.view.getItems().find(x => x.ProductID === row.ProductID);
        if (sameProduct && this.itemId(sameProduct) !== id) { alertDialog('This product is already in order details!'); return false; }
        if (this.connectedMode) return true;
        const lookup = await ProductRow.getLookupAsync();                  // fill display fields in memory
        row.ProductName = lookup.itemById[row.ProductID].ProductName;
        row.LineTotal = (row.Quantity || 0) * (row.UnitPrice || 0) - (row.Discount || 0);
        return true;
    }
    protected override getNewEntity() { return { OrderID: this.orderId }; }

    public set orderId(value: number) {                                     // set from OrderDialog.afterLoadEntity
        if (this._orderId !== toId(value)) {
            this.setEquality(OrderDetailRow.Fields.OrderID, this._orderId = toId(value));
            this.connectedMode = this._orderId != null;                    // existing order => connected mode
            this.refresh();
        }
    }
}
```
- Tutorial: [Managing Enrollments with a Master–Detail](https://serenity.is/docs/tutorials/courses/05-managing-enrollments-master-detail); cell editing docs: [Cell Editing](https://serenity.is/docs/grids/cell-editing)
- Related-grid-in-tab master-detail (not in-form): `CustomerOrdersGrid` + `SubDialogHelper.cascade(dialog, this.domNode.closest('.ui-dialog'))`, disables Add until `customerID` set — [CustomerOrdersGrid.ts](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerOrdersGrid.ts)
- Inline editing sample (Product grid): formatters return `<input class="edit numeric" data-field=…>` / `<select>` elements; `pendingChanges` dictionary; a "Save Changes" toolbar button calls `serviceRequest(ProductService.Methods.Update, { EntityId, Entity })` per modified row — [ProductGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductGrid.tsx)
- Cascaded editors: `CascadeFrom`, `CascadeField`, `CascadeValue`, `FilterField`, `FilterValue` on lookup editor attributes — [LookupEditorBaseAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/src/core/ComponentModel/PropertyGrid/EditorTypes/LookupEditorBaseAttribute.cs); usage `CascadeFrom = "Country"` — [CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs); corelib has `cascadedwidgetlink.ts` — [editors folder](https://github.com/serenity-is/Serenity/tree/master/packages/corelib/src/ui/editors)
- Custom list request field for server filtering: `(this.view.params as OrderListRequest).ProductID = …` with a C# `OrderListRequest : ListRequest` — [OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx), [OrderListRequest.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderListRequest.cs)
- 10.6: `DeltaLister` (used for detail list diffs) now strict — unknown new ids throw — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)

### Inferences
- Master-detail checklist: `[MasterDetailRelation]` + `[NotMapped] List<DetailRow>` on master row → detail editor class (`GridEditorBase`) + detail dialog (`GridEditorDialog`) → custom editor attribute on Form property → set foreign key in `afterLoadEntity`.

### Gaps
- SleekGrid built-in cell editing (`editable: true`, `editor` per column) details not fetched beyond the docs link.

---

## 7. Theming/CSS, dashboards, responsive behavior

### Takeaway
Serenity UI is Bootstrap + CSS variables; every widget gets auto `s-<FullName>` / `s-<ShortName>` CSS classes so you scope styles by importing a co-located `.css` file from the widget module. Premium themes (Azure, Cosmos, Glassy, light/dark) are StartSharp features; Serene uses the common theme. Dashboards are ordinary pages (controller + view/TSX + Chart.js etc.); responsiveness comes from Bootstrap grid classes (`hidden-xs`, `col-sm-*` widths) and SleekGrid's responsive pinning.

### Cited Findings
- Apps are "styled with Bootstrap and a set of CSS variables"; StartSharp premium themes Azure, Cosmos, Glassy define variables like `--s-card-title`, `--s-category-title`, `--s-sidebar-band-bg`; custom theme = override under `:root.theme-mycustomred`; each widget gets `s-` classes from `getCssClass()` (e.g. `s-MySamples-MyCoolWidget s-MyCoolWidget`); create a CSS file beside the widget, import it from the module, target the `s-` class; scope grid styles e.g. `.s-OrderGrid .slick-header-column`; header has Light/Dark/System selector — [Theming and CSS Customization](https://serenity.is/docs/framework/theming)
- Real: `import "./OrderGrid.css";`, `import "./CustomerDialog.css";` — [OrderGrid.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderGrid.tsx), [CustomerDialog.tsx](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerDialog.tsx)
- `common-style.css`/`common-theme.css` in Serenity.Extensions rewritten to align with the premium theme and include SlickGrid, Select2, file upload styles (remove `slick.grid.css`, `select2.css` etc. from `appsettings.bundles.json`) — [changelog-2024.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2024.md)
- Responsive: `[HalfWidth]` → `col-sm-6` (form) — [Forms & Editors](https://serenity.is/docs/framework/forms-editors); `QuickFilter(CssClass = "hidden-xs")` hides filters on phones — [OrderColumns.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderColumns.cs); SleekGrid temporarily reduces pinned columns/frozen rows on small viewports — [changelog-2025.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md); RTL support in SleekGrid — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Dashboard (Serene): `DashboardPage` controller `[Route("Dashboard/[action]")]`, `[PageAuthorize, HttpGet, Route("~/")]`, builds a `DashboardPageModel` (e.g. open/closed orders counts from Northwind) cached 5 min via `ITwoLevelCache.GetLocalStoreOnly(..., o.GenerationKey, ...)`, rendered by `DashboardIndex.cshtml` — [DashboardPage.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/Dashboard/DashboardPage.cs); template depends on chart.js, flatpickr, daterangepicker, jvectormap, jquery-sparkline/knob — [package.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/package.json). StartSharp's dashboard was rewritten with JSX functional components (2023) — [changelog-2023.md](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2023.md); 10.6 adds WIP `Serenity.Pro.Dashboards` (Enterprise, not yet released) — [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Strict CSP: inline style attributes replaced; Tiptap replaces CKEditor (opt-in, CKEditor to be removed) — [changelog-2025.md (9.2.0)](https://github.com/serenity-is/Serenity/blob/master/docs/changelog/changelog-2025.md)

### Inferences
- For Serene (free) users, theme customization = override Bootstrap/CSS variables in `site.css` or per-widget CSS; premium theme switching is StartSharp-only.

### Gaps
- The theming docs page did not state the Bootstrap major version or document `appsettings.bundles.json`/`site.css` specifics; not verified here.
