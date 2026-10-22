# Serenity Backend: Entities (Rows), Fluent SQL, Criteria, Connections, Migrations, Services & Behaviors

> Research snapshot: 2026-10-22. Latest Serenity release found = **10.6.0 (2026-10-11)** per [CHANGELOG.md](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md). Code examples below are copied from the `serenity-is/Serenity` repo `master` branch (commit de416c2, 2026-10-22), which now contains the Serene template (`/serene`) and the Northwind demo (`/common-features/src/demo.northwind`). Note: the standalone `serenity-is/Serene` repo's last commit is 2024-10-19, so it is stale; use the `serene/` folder inside the Serenity repo instead ([repo](https://github.com/serenity-is/Serenity/tree/master/serene)).
>
> Base URLs used below:
> - NW = https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind
> - SER = https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web
> - FW = https://github.com/serenity-is/Serenity/blob/master/src

## 1. How is an entity Row defined (RowFieldsBase, field types, attributes)?

### Takeaway
A Row is a `sealed class XRow : Row<XRow.RowFields>`. It has nullable properties that read and write through typed field objects (`StringField`, `Int32Field`, ...) declared in a nested `RowFields : RowFieldsBase` class. Class and property attributes supply the table mapping, connection, permissions, keys and UI metadata. In 10.x the connection is usually declared with a feature-specific subclass of `ConnectionKeyAttribute` (e.g. `[NorthwindConnection]`), and permission checks fail closed.

### Cited Findings
- A row derives from `Row<TFields>`, where `TFields` is a nested class derived from `RowFieldsBase`. The `Row` suffix is a convention. With no `[TableName]`, the table name is the class name minus `Row` — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- Auto-properties are not allowed. Each property must use `get => fields.Name[this]; set => fields.Name[this] = value;`. This enables zero-reflection query building, assignment tracking and optional `INotifyPropertyChanged` — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- Properties are always nullable, even for NOT NULL columns, because "We never use zero in place of null" (a left join can yield null) — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- The field type must match the property type, and the field name must match the property name, or initialization fails. One `RowFields` instance is shared by all rows of that type, like a static. Fields are auto-created by name matching. Manual init in the RowFields constructor is "not recommended" — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- With `<Nullable>enable|annotations</Nullable>`, fields are declared `= null!`. Since 10.5.2, new templates use `<Nullable>annotations</Nullable>` by default — [Entities docs](https://serenity.is/docs/framework/data-access/entities); [CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- StartSharp's `Serenity.Pro.Coder` offers `[GenerateFields]` with C# 13 partial properties (`public partial string? Name { get; set; }`). The generator emits the backing code. This is a StartSharp (paid) feature — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- **Deprecated:** the nested `RowTemplate` class has been "not recommended since 6.8.1". The Newtonsoft `JsonRowConverter` is "compat only". Rows serialize via `RowJsonConverter`, which emits only assigned fields — [Entities docs](https://serenity.is/docs/framework/data-access/entities)
- **Deprecated:** use the `GetIdField()` extension instead of the `IdField` property (it throws if not configured) — [CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Field type pages exist for each field class (StringField, Int32Field, Int16Field, Int64Field, DecimalField, BooleanField, DateTimeField, EnumField<T>, ListField<T>, RowListField<T>, etc.) — [Field Types docs](https://serenity.is/docs/framework/data-access/field-types). The real Northwind code uses `Int32Field, StringField, BooleanField, DecimalField, Int16Field, DateTimeField, EnumField<Gender>, RowListField<OrderDetailRow>, ListField<int>` — [NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs), [NW OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs), [NW CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs)

**Real example (10.6.0), ProductRow** — [NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs):
```csharp
namespace Serenity.Demo.Northwind;

[NorthwindConnection, NorthwindModule, TableName("Products")]
[DisplayText("Products"), InstanceName("Product")]
[ReadPermission(PermissionKeys.General)]
[ModifyPermission(PermissionKeys.General)]
[LookupScript]
[CaptureLog(typeof(ProductLogRow))]
[LocalizationRow(typeof(ProductLangRow), LocalizeListByDefault = true)]
public sealed class ProductRow : Row<ProductRow.RowFields>, IIdRow, INameRow, IConcurrencyVersionRow
{
    [DisplayText("Product Id"), Identity, LookupInclude, IdProperty]
    public int? ProductID { get => fields.ProductID[this]; set => fields.ProductID[this] = value; }

    [DisplayText("Product Name"), Size(40), NotNull, QuickSearch, LookupInclude, NameProperty]
    public string? ProductName { get => fields.ProductName[this]; set => fields.ProductName[this] = value; }

    [DisplayText("Discontinued"), NotNull, DefaultValue(false)]
    public bool? Discontinued { get => fields.Discontinued[this]; set => fields.Discontinued[this] = value; }

    [DisplayText("Supplier"), NotNull, ForeignKey(typeof(SupplierRow)), LeftJoin("sup")]
    [LookupEditor(typeof(SupplierRow), InplaceAdd = true, DialogType = "Demo.Northwind.SupplierDialog", Async = true)]
    public int? SupplierID { get => fields.SupplierID[this]; set => fields.SupplierID[this] = value; }

    [DisplayText("Unit Price"), Scale(4), LookupInclude, SummaryType(SummaryType.Avg)]
    public decimal? UnitPrice { get => fields.UnitPrice[this]; set => fields.UnitPrice[this] = value; }

    [Origin("sup"), DisplayText("Supplier"), LookupInclude]
    public string? SupplierCompanyName { get => fields.SupplierCompanyName[this]; set => fields.SupplierCompanyName[this] = value; }

    [DisplayText("Row Version"), Insertable(false), Updatable(false)]
    public int? RowVersion { get => fields.RowVersion[this]; set => fields.RowVersion[this] = value; }

    Field IConcurrencyVersionRow.ConcurrencyVersionField => fields.RowVersion;

    public class RowFields : RowFieldsBase
    {
        public Int32Field ProductID = null!;
        public StringField ProductName = null!;
        public BooleanField Discontinued = null!;
        public Int32Field SupplierID = null!;
        public DecimalField UnitPrice = null!;
        public Int32Field RowVersion = null!;
        public StringField SupplierCompanyName = null!;
        // ... (abridged)
    }
}
```
- The connection attribute is a subclass of `ConnectionKeyAttribute`: `public class NorthwindConnectionAttribute() : ConnectionKeyAttribute(Key) { public const string Key = "Northwind"; }` — [NW NorthwindConnectionAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Shared/NorthwindConnectionAttribute.cs). Serene's own rows use `DefaultConnectionAttribute.Key` ("Default") — [SER DataMigrations.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/AppServices/DataMigrations.cs). Older style: `[ConnectionKey("Default"), Module("Administration"), TableName("Users")]` — [Service Endpoints docs](https://serenity.is/docs/services/service_endpoints)
- **10.6.0 change:** feature-specific connection and module attributes (DefaultConnection, MeetingConnection, ...) were introduced. Pro features' `ConnectionKeys` classes were removed (breaking change for StartSharp) — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- **10.6.0 breaking change:** "Permission checks now fail closed: rows without explicit permissions are denied by request handlers, and permission keys cannot be empty. Use `SpecialPermissionKeys.Public` (`*`) or `SpecialPermissionKey.LoggedIn` (`?`)" — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md). Rows declare `[ReadPermission]`, `[ModifyPermission]` and optionally `[DeletePermission]`, `[InsertPermission]`, `[UpdatePermission]` (CustomerRow uses Read/Modify/Delete) — [NW CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs)
- **10.6.0:** fields with potentially sensitive names are automatically excluded from selection, filtering, sorting and client writes unless explicitly configured (`[Filterable]`/`[NotFilterable]` override filtering) — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- **10.6.0:** optimistic concurrency via `IConcurrencyVersionRow` (shown above) — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md); [NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs)
- Key/name attributes: `Identity`, `PrimaryKey`, `IdProperty`, `NameProperty`, `InstanceName`. Column attributes: `Size`, `Scale`, `NotNull`. Search: `QuickSearch`, `SearchType`. Lookups: `TextualField`, `LookupInclude`, `TwoLevelCached`. Select levels: `MinSelectLevel`, `SelectLevel` — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- Older docs and tutorials use `[DisplayName]`. Current Northwind code uses `[DisplayText(...)]` on properties and the class, with `[InstanceName]` for the singular name — [NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs)
- A field is "required" for save validation when it has `[NotNull]` and is not `[TrimToEmpty]` — [Validation docs](https://serenity.is/docs/services/validation)
- Row marker interfaces in source: `IIdRow`, `INameRow`, `IInsertLogRow : IInsertDateRow, IInsertUserIdRow`, `IUpdateLogRow`, `IDeleteLogRow`, `IIsActiveRow`, `IIsActiveDeletedRow : IIsActiveRow` — [FW services/Entity/Contracts](https://github.com/serenity-is/Serenity/tree/master/src/services/Entity/Contracts)
- **10.6.0 breaking change:** `LoggingRow` user ID properties and fields are now `object` and `Field` (were `int`/`Int32Field`). `UserEntityOptions` supports int/long/Guid/string user IDs. Logging rows use UTC for InsertDate/UpdateDate — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)

### Inferences
- For a learner on 10.x: always add explicit `[ReadPermission]`/`[ModifyPermission]` (or `"*"`). Otherwise the generated CRUD services will deny access because of fail-closed permissions.
- Sergen (`dotnet sergen g`) generates Row/Columns/Form/Endpoint/handlers. The generated Row "belongs to you" (no regeneration directives) per [CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md).

### Gaps
- I did not retrieve the full list of field types and flags from the Field Types or Field Flags pages (only their URLs): https://serenity.is/docs/framework/data-access/field-types, https://serenity.is/docs/framework/data-access/field-flags

## 2. How do joins, expression fields, view fields and lookups work?

### Takeaway
A `[ForeignKey]` plus `[LeftJoin("alias")]` on the FK property creates a join. View fields from the joined table use `[Origin("alias", "Prop")]` (or `[Expression("alias.[Col]")]`). Computed fields use `[Expression]`, `[Concat]`, `[Case]` and similar attributes, with optional per-dialect variants. Lookups are cached client-side scripts declared with `[LookupScript]` on the row, or with a custom `RowLookupScript<TRow>` class.

### Cited Findings
- `[TableName]` and `[Column]` override names. `SqlSettings.AutoQuotedIdentifiers` (default false, but Serene/StartSharp set it to true) brackets identifiers — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- `[Expression("(T0.[Firstname] + ' ' + T0.[Lastname])")]` defines a computed field. Prefix main-table columns with `T0`. The `+` operator is SQL Server specific, so use `CONCAT` or the `[Concat]` attribute for portability. `Case`, `DateDiff`, `DatePart` and `SqlNow` attribute helpers also exist — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- `[ForeignKey]` only records metadata and does not change queries alone. `[LeftJoin("c")]` joins using the FK metadata. Chained joins work. Class-level `[LeftJoin("cd", "CustomerDetails", "cd.[CustomerId] = T0.[Id]")]`, `[InnerJoin(...)]` and `OuterApply` exist. Aliases must be unique and must not be reserved words — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- `[NotMapped]` excludes a property from the DB. It is used for master-detail lists. `[ClientSide]` is an obsolete alias — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- Dialect-specific mapping: `[Expression("(T0.FirstName || ' ' || T0.LastName)", Dialect = nameof(ServerType.Sqlite))]`. The longest matching dialect name wins, and `!`/`NegateDialect` inverts the match. The dialect comes from the row's ConnectionKey, else `SqlSettings.DefaultDialect`. Expressions are fixed when RowFields is first created — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)
- `[ForeignRow]` exposes a joined row as a row-valued property. `UserIdFieldType`/`UserIdJoinKey` configure user-table joins — [Mapping docs](https://serenity.is/docs/framework/data-access/mapping)

Real join, view-field and `Case` examples from OrderRow — [NW OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs):
```csharp
const string jCustomer = nameof(jCustomer);

[DisplayText("Customer"), Size(5), NotNull, NameProperty]
[ForeignKey(typeof(CustomerRow), nameof(CustomerRow.CustomerID)), LeftJoin(jCustomer)]
public string? CustomerID { get => fields.CustomerID[this]; set => fields.CustomerID[this] = value; }

[Origin(jCustomer, nameof(CustomerRow.CompanyName)), DisplayText("Customer"), QuickSearch]
public string? CustomerCompanyName { get => fields.CustomerCompanyName[this]; set => fields.CustomerCompanyName[this] = value; }

[DisplayText("Employee"), ForeignKey(typeof(EmployeeRow)), LeftJoin(jEmployee)]
[TextualField(nameof(EmployeeFullName))]
public int? EmployeeID { get => fields.EmployeeID[this]; set => fields.EmployeeID[this] = value; }

[DisplayText("Shipping State"), Case($"T0.[{nameof(ShippedDate)}] IS NULL",
    (int)OrderShippingState.NotShipped, (int)OrderShippingState.Shipped)]
public OrderShippingState? ShippingState { get => fields.ShippingState[this]; set => fields.ShippingState[this] = value; }

[DisplayText("Details"), MasterDetailRelation(foreignKey: nameof(OrderDetailRow.OrderID)), NotMapped]
[MinSelectLevel(SelectLevel.Explicit)]
public List<OrderDetailRow>? DetailList { get => fields.DetailList[this]; set => fields.DetailList[this] = value; }
```
Class-level join plus updatable extension (CustomerRow) — [NW CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs):
```csharp
[LeftJoin("cd", "CustomerDetails", "cd.[CustomerID] = T0.[CustomerID]", RowType = typeof(CustomerDetailsRow), TitlePrefix = "")]
[UpdatableExtension("cd", typeof(CustomerDetailsRow), CascadeDelete = true)]
[LookupScript(typeof(Lookups.CustomerLookup))]
public sealed class CustomerRow : Row<CustomerRow.RowFields>, IIdRow, INameRow
```
**Lookups:** `[LookupScript]` on a row makes an automatic lookup. `[LookupInclude]` marks the fields sent to the client (ProductRow above). A custom lookup derives from `RowLookupScript<TRow>` — [NW OrderShipCityLookup.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderShipCityLookup.cs):
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
        query.Distinct(true)
            .Select(fld.ShipCountry)
            .Select(fld.ShipCity)
            .Where(fld.ShipCountry != "" & fld.ShipCountry.IsNotNull() &
                   fld.ShipCity != "" & fld.ShipCity.IsNotNull());
    }

    protected override void ApplyOrder(SqlQuery query) { }
}
```
- **Newer alternative to lookup scripts:** `ServiceLookupEditor(typeof(CategoryRow), ...)` calls the List service instead of a cached script — [NW ProductRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductRow.cs). In 10.6.0, `[ListRequestCapabilities]` lets list handlers declare supported operations. `ServiceLookupEditor` uses these and can infer endpoints from row metadata — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)

### Inferences
- The `const string jX = nameof(jX)` alias pattern plus `nameof(...)` in `Origin` is the modern, refactor-safe style. Hand-written `[Expression("sup.[CompanyName]")]` is the older equivalent.
- `[MinSelectLevel(SelectLevel.Explicit)]` on a detail list means it is not loaded unless requested.

### Gaps
- Details of the lookup cache (`TwoLevelCached`, cache invalidation via `Cache.InvalidateOnCommit`) were not researched in depth. `Cache.InvalidateOnCommit(UnitOfWork, MyRow.Fields)` appears in [SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs).

## 3. How do Fluent SQL (SqlQuery/SqlInsert/SqlUpdate/SqlDelete), Criteria and dialects work?

### Takeaway
`SqlQuery` is a fluent SELECT builder. Methods can be called in any order, and paging is rendered per dialect. `SqlInsert`/`SqlUpdate`/`SqlDelete` build DML with `ExpectedRows` safety checks. `Criteria` objects (most often row Fields with overloaded operators) produce parameterized WHERE clauses. None of these builders run behaviors or permission checks, and none must ever receive raw user strings.

### Cited Findings
- `SqlQuery` methods: `From(string|Alias)` (a second call creates a CROSS JOIN), `Select(expr[, alias])` (~11 overloads), `SelectMany`, `OrderBy(expr, desc)`, `OrderByFirst`, `GroupBy`, `Having`, `Skip`, `Take`, `Distinct(bool)`, `Dialect(ISqlDialect)`. `ToString()` produces SQL without validation — [Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql)
- Paging output: SQL Server 2012 gives `ORDER BY PersonId OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY`; SQLite gives `ORDER BY PersonId LIMIT 50 OFFSET 100`. Pre-2012 SQL Server needs ORDER BY for Skip (it uses ROW_NUMBER) — [Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql)
- Fallback dialect: `SqlSettings.DefaultDialect = SqliteDialect.Instance;`. A connection string's ProviderName/Dialect takes precedence — [Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql); [SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections)
- Dialect classes in source: Firebird, MySql, Oracle, Oracle12c, Postgres, SqlServer2000/2005/2008/2012, Sqlite — [FW core dialects](https://github.com/serenity-is/Serenity/tree/master/src) (verified by listing source files). The docs page lists only Firebird, Postgres, Sqlite and SqlServer2000/2005/2012 — [Fluent SQL docs](https://serenity.is/docs/framework/data-access/fluent-sql) (the docs list is incomplete compared with the source)
- Query extensions: `Where(ICriteria)` gives a parameterized clause (`.Where(fld.Age > 18 & fld.Country == "US")` produces `WHERE Age > @p1 AND Country = @p2`). `Where(string)` is for trusted fragments only. `WhereEqual(field, value, emitIsNull)`. `From(row, out fields)` sets the INTO row and returns aliased fields. `SubQueryFrom`, `WithSelf` — [Query Extensions docs](https://serenity.is/docs/framework/data-access/query-extensions)
- Joins: the `Alias` class (`p["Firstname"]` produces `p.Firstname`, static `Alias.T0..T9`, `WithNoLock()`). `SqlQuery.LeftJoin/RightJoin/InnerJoin(alias, onCriteria)`. Typed `LeftJoinVia/InnerJoinVia` build ON from FK metadata — [Joins & Aliases docs](https://serenity.is/docs/framework/data-access/joins-aliases)
- Criteria: `&`, `|`, `!`, comparison operators, `IsNull()/IsNotNull()` (not `== null`), `Like/NotLike/StartsWith/EndsWith/Contains/NotContains`, `In/NotIn` (values or subquery), `ParamCriteria("@p")` + `SetParam`, `ConstantCriteria`. Never pass client strings to the `new Criteria(...)` constructor — [Criteria docs](https://serenity.is/docs/framework/data-access/criteria)
- **10.6.0:** `StartsWith/EndsWith/Contains` escape literal text by default. `LikeEscaped`/`NotLikeEscaped` were added. `Like`/`NotLike` remain raw patterns. An `Exists(BaseCriteria)` overload was added — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- **10.6.0 breaking changes:** `IFilterableQuery.Where` takes `ICriteria?`. `SqlUpdate.Where(params string[])` and legacy `IChainable.With` were removed. `QueryWithParams.Params` is read-only. Generated SQL is cached, and `Freeze`/`FreezeParams` were added. New `ListProjected`/`QueryProjected` helpers with `Sql.Expr`, `JoinVia` — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- DML: `new SqlInsert("T").Set(...)`, `.Execute(conn)`, `.IdentityColumn("PersonId")` + `ExecuteAndGetID(conn)` (integer identity only), `ExecuteUpsert`. `SqlUpdate` with `Set/SetTo/SetNull/Inc/Dec`, `Where`, `WhereEqual`, `.Execute(conn, ExpectedRows.One)`. `SqlDelete` with `.Where(new Criteria("PersonId") == 5)`. `ExpectedRows.One` (default, throws otherwise), `ZeroOrOne`, `Ignore`. A missing WHERE therefore throws. The builders perform no validation, permission checks or behaviors — [SQL Data Manipulation docs](https://serenity.is/docs/framework/data-access/sql-data-manipulation)
- Entity helpers on connections (`EntityConnectionExtensions`): `ById`, `TryById`, `First/TryFirst`, `Single/TrySingle`, `List<TRow>`, `Count`, `Insert`, `InsertAndGetID`, `UpdateById`, `Update`, `DeleteById`, `Delete`. Writes use only assigned fields. They do **not** run behaviors or service validation. `IRowOperationInterceptor` can intercept them — [Entity CRUD docs](https://serenity.is/docs/framework/data-access/entity-crud)

Real SqlQuery usage executed with Dapper's `connection.Query` — [BasicSamplesEndpoint.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.basicsamples/Modules/BasicSamplesEndpoint.cs):
```csharp
var shippers = connection.List<ShipperRow>(q => q.SelectTableFields().OrderBy(ShipperRow.Fields.CompanyName));

var monthExpr = new DatePartAttribute(DateParts.Month, fld.OrderDate.Expression)
    .ToString(connection.GetDialect());

var byMonth = connection.Query(
    new SqlQuery()
        .From(fld)
        .Select(monthExpr, "Month")
        .Select(Sql.Count(), "Count")
        .Select(fld.ShipVia, "ShipVia")
        .GroupBy(monthExpr)
        .GroupBy(fld.ShipVia)
        .Where(fld.OrderDate.IsNotNull() & fld.ShipVia.IsNotNull()));
```
Subquery with `Criteria.Exists` inside a list handler — [NW OrderListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/RequestHandlers/OrderListHandler.cs):
```csharp
query.Where(Criteria.Exists(
    query.SubQueryFrom(OrderDetailRow.Fields, out var detailFields)
        .Select("1")
        .Where(detailFields.OrderID == MyRow.Fields.OrderID &
               detailFields.ProductID == Request.ProductID.Value))
    .ToString()));
```

### Inferences
- Criteria sent from the client in `ListRequest.Criteria` are a JSON criteria tree that the handler validates (it "cannot contain arbitrary SQL" per the [List handler docs](https://serenity.is/docs/services/list_request_handler)). This is distinct from server-side `new Criteria("raw")`.

### Gaps
- The docs pages were summarized by a fetch tool that limits verbatim quoting, so signatures for `ParamCriteria`/`Alias` are paraphrased. Check the original pages before copying code.
- The client-side JSON criteria format was not researched.

## 4. How do services work: requests/responses, handlers, hooks, behaviors?

### Takeaway
CRUD is done by generic request handlers. In 10.x the **async** ones are the default: `ListRequestHandlerAsync<TRow>`, `RetrieveRequestHandlerAsync<TRow>`, `SaveRequestHandlerAsync<TRow>`, `DeleteRequestHandlerAsync<TRow>`, plus `UndeleteRequestHandlerAsync`. Each is wrapped by a project interface (`IProductListHandler : IListHandlerAsync<ProductRow>`) for DI. You customize by overriding protected virtual hooks and calling base first. Cross-cutting logic lives in singleton behaviors, activated implicitly via `IImplicitBehavior.ActivateFor` or attached with attributes.

### Cited Findings
- Generated handlers in 10.6.0 are one-liners with primary constructors — [NW ProductListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/RequestHandlers/ProductListHandler.cs):
```csharp
public interface IProductListHandler : IListHandlerAsync<MyRow> { }

public class ProductListHandler(IRequestContext context) :
    ListRequestHandlerAsync<MyRow>(context), IProductListHandler
{
}
// likewise: ISaveHandlerAsync<MyRow>/SaveRequestHandlerAsync<MyRow>,
// IDeleteHandlerAsync<MyRow>/DeleteRequestHandlerAsync<MyRow>, IRetrieveHandlerAsync<MyRow>/RetrieveRequestHandlerAsync<MyRow>
```
- A custom request type uses the 3-generic form: `ListRequestHandlerAsync<MyRow, OrderListRequest, ListResponse<MyRow>>` with `public class OrderListRequest : ListRequest { public int? ProductID { get; set; } }` — [NW OrderListHandler.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/RequestHandlers/OrderListHandler.cs), [OrderListRequest.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderListRequest.cs)
- **Sync vs async / deprecation:** the docs say the sync `SaveRequestHandler<TRow>` and `ISaveHandler<TRow>` are "kept for backward compatibility and marked obsolete", and that new code (including Sergen output) should use async — [Save handler docs](https://serenity.is/docs/services/save_request_handler). **Conflict:** in the 10.6.0 source, `SaveRequestHandler<TRow>` and the other sync handlers carry no `[Obsolete]` attribute (grep of `src/services/RequestHandlers` found only one unrelated `[Obsolete]`) — [FW SaveRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandler.cs). Treat sync as legacy/soft-deprecated. Sync behavior base classes (`BaseSaveBehavior` etc.) are documented as obsolete — [Behaviors docs](https://serenity.is/docs/services/behaviors)
- ListRequest: `Skip`, `Take` (0 = all, subject to server limits), `IncludeMore`, `Sort`, `ContainsText`, `ContainsField` (must be `[QuickSearch]`), `Criteria`, `IncludeDeleted` (needs undelete permission), `ExcludeTotalCount`, `EqualityFilter`, `ColumnSelection` (List/KeyOnly/Details), `IncludeColumns`, `ExcludeColumns`, `DistinctFields`, `ExportColumns`. ListResponse: `Entities`, `TotalCount`, `Take`, `More` — [List handler docs](https://serenity.is/docs/services/list_request_handler)
- **10.6.0:** `IncludeMore`/`More`, `RequestHandlerSettings.DefaultPageSize/MaxPageSize` (config section `RequestHandlerSettings`), `ExcludeTotalCountByDefault`, `ListRequest.SuppressPagingLimits()`, and validation of negative skip/take — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- List hooks (async): `ValidateRequestAsync`, `PrepareQueryAsync(SqlQuery)`, `ApplyFiltersAsync(SqlQuery)`, `OnBeforeExecuteQueryAsync`, `OnAfterExecuteQueryAsync`, `OnReturnAsync` (+ `ExecuteQueryAsync`) — [List handler docs](https://serenity.is/docs/services/list_request_handler); [FW ListRequestHandlerAsync.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/List/ListRequestHandlerAsync.cs)
- Save hooks (async, in order): `ValidateRequestAsync` → `SetInternalFieldsAsync` → `BeforeSaveAsync` → (INSERT/UPDATE) → `AfterSaveAsync` → `OnReturnAsync`. Also `LoadOldEntityAsync`, `PrepareQueryAsync`, `ExecuteSaveAsync`, `PerformAuditingAsync`, `HandleDisplayOrderAsync`, and sync `GetEditableFields(HashSet<Field>)`. The sync equivalents are `ValidateRequest`, `SetInternalFields`, `BeforeSave`, `AfterSave`, `OnReturn` — [Save handler docs](https://serenity.is/docs/services/save_request_handler); [FW SaveRequestHandlerAsync.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandlerAsync.cs); [FW SaveRequestHandler.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandler.cs)
- Save handler properties: `Context`, `Connection` (from `UnitOfWork.Connection`), `UnitOfWork`, `Old`, `Row`, `Request`, `Response`, `StateBag`, plus `IsCreate`/`IsUpdate` — [FW SaveRequestHandlerBase.cs](https://github.com/serenity-is/Serenity/blob/master/src/services/RequestHandlers/Save/SaveRequestHandlerBase.cs); [Save handler docs](https://serenity.is/docs/services/save_request_handler)
- Older names `OnBeforeSave`/`OnValidateRequest` are the **behavior** hook names (e.g. `OnValidateRequestAsync`, `OnSetInternalFieldsAsync`, `OnPrepareQueryAsync`, `OnBeforeSaveAsync`/`OnAfterSaveAsync`), not handler names — [Behaviors docs](https://serenity.is/docs/services/behaviors); [Save handler docs](https://serenity.is/docs/services/save_request_handler)

Real overridden save handler — [SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs) (abridged):
```csharp
public class UserSaveHandler(IRequestContext context, IOptions<EnvironmentSettings> environmentOptions)
    : SaveRequestHandlerAsync<MyRow>(context), IUserSaveHandler
{
    protected override void GetEditableFields(HashSet<Field> editable)
    {
        base.GetEditableFields(editable);
        if (!Permissions.HasPermission(PermissionKeys.Security))
        {
            var fld = MyRow.Fields;
            editable.Remove(fld.Source);
            editable.Remove(fld.IsActive);
        }
    }

    protected override async Task ValidateRequestAsync(CancellationToken cancellationToken = default)
    {
        await base.ValidateRequestAsync(cancellationToken).ConfigureAwait(false);
        if (IsUpdate && Row.Username != Old.Username) { /* ... */ }
        if (IsCreate) { /* ... */ }
    }

    protected override async Task SetInternalFieldsAsync(CancellationToken cancellationToken = default)
    {
        await base.SetInternalFieldsAsync(cancellationToken).ConfigureAwait(false);
        if (IsCreate) { Row.Source = "site"; Row.IsActive = Row.IsActive ?? 1; }
    }

    protected override async Task AfterSaveAsync(CancellationToken cancellationToken = default)
    {
        await base.AfterSaveAsync(cancellationToken).ConfigureAwait(false);
        Cache.InvalidateOnCommit(UnitOfWork, MyRow.Fields);
    }
}
```
**Behaviors:**
- Interfaces come in pairs: `ISaveBehaviorAsync/ISaveBehaviorSync` (marker `ISaveBehavior`), and the same for List, Retrieve, Delete and Undelete. There are also `IListMapFieldExpressionBehavior`, `IFieldBehavior` (with `Target`), and exception behaviors (`ISaveExceptionBehavior`... sync `OnException`) — [Behaviors docs](https://serenity.is/docs/services/behaviors)
- `IImplicitBehavior { bool ActivateFor(IRow row); }` is called once per handler/row type. Behaviors are singletons, so don't store per-request state in fields (use `handler.StateBag`). `[AddBehavior]` attaches explicitly. Async base classes: `BaseSaveBehaviorAsync`, `BaseSaveDeleteBehaviorAsync`, `BaseListBehaviorAsync`, `BaseDeleteBehaviorAsync`, `BaseRetrieveBehaviorAsync`, `BaseUndeleteBehaviorAsync`. `AddServiceHandlers()` calls `AddServiceBehaviors()`, and implicit behaviors are auto-discovered by the type source. Constructor DI is supported — [Behaviors docs](https://serenity.is/docs/services/behaviors)
- **10.6.0 fix:** scoped handler/behavior dependencies now resolve from the current request scope instead of the root provider — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Built-in behaviors — [Built-in Behaviors docs](https://serenity.is/docs/services/built-in-behaviors):
  - Insert/Update log: implement `ILoggingRow` / `IInsertLogRow` / `IUpdateLogRow`, or derive from `LoggingRow<TFields>`. This auto-fills user and date fields.
  - Capture log: `[CaptureLog(typeof(ProductLogRow))]` gives an audit history table.
  - Localization: `[LocalizationRow(typeof(ProductLangRow), LocalizeListByDefault = true)]`
  - Master-detail: `[MasterDetailRelation(nameof(Detail.MasterId))]` on a `List<DetailRow>` `[NotMapped]` property.
  - Linking set: `[LinkingSetRelation(typeof(CustomerRepresentativesRow), "CustomerId", "EmployeeId")]` on a `List<int>`. Real usage is in [NW CustomerRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Customer/CustomerRow.cs).
  - Unique: `[Unique]` on a field, `[UniqueConstraint(nameof(A), nameof(B))]` on a row.
  - Updatable extension: `[UpdatableExtension("cd", typeof(CustomerDetailsRow), CascadeDelete = true)]`
  - Display order: implement `IDisplayOrderRow`. Parent validation: `IParentIdRow`. SQL exception humanization: implicit.
- Soft delete uses `IIsActiveDeletedRow` / `IDeleteLogRow` (interfaces in [FW Entity/Contracts](https://github.com/serenity-is/Serenity/tree/master/src/services/Entity/Contracts)), with an `UndeleteRequestHandler` ([Undelete docs](https://serenity.is/docs/services/undelete_request_handler)). The built-in behaviors page does not describe them.
- Multi-tenancy is a documented custom-behavior pattern: `ActivateFor` checks `row is IMultiTenantRow`, then overrides `OnSetInternalFieldsAsync` and `OnValidateRequestAsync` — [Behaviors docs](https://serenity.is/docs/services/behaviors)
- `[DataAuditLog]` appears on OrderRow — [NW OrderRow.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Order/OrderRow.cs)

### Inferences
- The "Repository" pattern from Serenity ≤ 5 (`XRepository` classes calling `new MyListHandler().Process(...)`) has been replaced by DI-injected handler interfaces. A remnant is `UserPermissionRepository.cs` in Serene — [SER UserPermissionRepository.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/UserPermission/UserPermissionRepository.cs). Learners reading old tutorials should map "Repository" to "handler + `[FromServices]`".
- `DataAuditLog` is likely a StartSharp (pro) feature, since it is not in the built-in behaviors list. Unverified.

### Gaps
- Retrieve/Delete handler hook lists were not fetched (pages: https://serenity.is/docs/services/retrieve_request_handler, https://serenity.is/docs/services/delete_request_handler).
- Exact sync list hook names (`PrepareQuery`, `ApplyFilters`, ...) are assumed by symmetry, not verified.

## 5. How are endpoints defined (ServiceEndpoint, [HttpPost], IUnitOfWork, IDbConnection), and how does DI work?

### Takeaway
An endpoint is an ASP.NET Core controller deriving from `ServiceEndpoint`, routed at `Services/Module/Entity/[action]`. Write actions take `IUnitOfWork` (a transaction committed on success and rolled back on exception) and are `[HttpPost]`. Read actions take `IDbConnection`. The connection is created from the class's `[ConnectionKey]`. Handlers are injected per-action with `[FromServices]`. Handlers and behaviors are auto-registered by `services.AddServiceHandlers()`.

### Cited Findings
Real 10.6.0 endpoint — [NW ProductEndpoint.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Modules/Product/ProductEndpoint.cs):
```csharp
[Route("Services/Northwind/Product/[action]")]
[ConnectionKey(typeof(MyRow)), ServiceAuthorize(typeof(MyRow))]
public class ProductEndpoint : ServiceEndpoint
{
    [HttpPost, AuthorizeCreate(typeof(MyRow))]
    public Task<SaveResponse> Create(IUnitOfWork uow, SaveRequest<MyRow> request,
        [FromServices] IProductSaveHandler handler, CancellationToken cancellationToken = default)
        => handler.CreateAsync(uow, request, cancellationToken);

    [HttpPost, AuthorizeUpdate(typeof(MyRow))]
    public Task<SaveResponse> Update(IUnitOfWork uow, SaveRequest<MyRow> request,
        [FromServices] IProductSaveHandler handler, CancellationToken cancellationToken = default)
        => handler.UpdateAsync(uow, request, cancellationToken);

    [HttpPost, AuthorizeDelete(typeof(MyRow))]
    public Task<DeleteResponse> Delete(IUnitOfWork uow, DeleteRequest request,
        [FromServices] IProductDeleteHandler handler, CancellationToken cancellationToken = default)
        => handler.DeleteAsync(uow, request, cancellationToken);

    public Task<RetrieveResponse<MyRow>> Retrieve(IDbConnection connection, RetrieveRequest request,
        [FromServices] IProductRetrieveHandler handler, CancellationToken cancellationToken = default)
        => handler.RetrieveAsync(connection, request, cancellationToken);

    public Task<ListResponse<MyRow>> List(IDbConnection connection, ListRequest request,
        [FromServices] IProductListHandler handler, CancellationToken cancellationToken = default)
        => handler.ListAsync(connection, request, cancellationToken);
}
```
(The original uses block bodies; it also has a `ListExcel` action using `IExcelExporter`.)
- Route convention `/Services/Module/Entity`. `[HttpPost]` on writes prevents GET triggering and caching. Client helpers always POST. `IUnitOfWork` means "may modify data", and the base class commits on return and rolls back on failure. `IDbConnection` is for reads. `[ConnectionKey(typeof(MyRow))]` makes the base class use `ISqlConnections.NewByKey()` and close the connection afterwards — [Service Endpoints docs](https://serenity.is/docs/services/service_endpoints)
- `ServiceAuthorize` requires login (optionally a permission) and returns a `NotAuthorized` service error instead of redirecting. `AuthorizeCreate/Update/Delete` read the row's permission attributes. The request object derives from `ServiceRequest` and is bound from JSON (`[JsonRequest]` filter). The response derives from `ServiceResponse` with an `Error` property (`ServiceError`) — [Service Endpoints docs](https://serenity.is/docs/services/service_endpoints)
- **10.5.2:** `ServiceEndpoint` implements `IAsyncDisposable`, `UnitOfWork` has `CommitAsync`/`DisposeAsync`, the pipeline prefers async commit, and an `InTransactionAsync` controller extension was added — [CHANGELOG 10.5.2](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- DI registration in Serene Startup: `services.AddServiceHandlers();` (line ~104), `services.AddSingleton<IDataMigrations, AppServices.DataMigrations>();` and `RowFieldsProvider.SetDefaultFrom(app.ApplicationServices);` — [SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs). Handler auto-registration is documented at [Handler auto-registration docs](https://serenity.is/docs/services/handler_auto_registration)
- Handler constructors take `IRequestContext` and can inject other services (e.g. `IOptions<EnvironmentSettings>`) — [SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs); [Request Context docs](https://serenity.is/docs/services/request-context)
- Manual transactions outside endpoints: `using (var uow = new UnitOfWork(connection)) { uow.Connection.Insert(task); uow.OnCommit += () => {...}; uow.Commit(); }`. Async code uses `CommitAsync` + `DisposeAsync` (`CommitAsync` is on `UnitOfWork`, not `IUnitOfWork`) — [SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections)
- `ISqlConnections`: `New(connectionString, providerName, dialect)`, `NewByKey(key)`, and the extension `NewFor<TRow>()` — [SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections)

### Inferences
- Endpoints are thin. All logic goes into handlers or behaviors, so they can be reused from background jobs by injecting `IXxxHandler` and providing a `UnitOfWork`.

### Gaps
- The exact JSON shape of `ServiceError` beyond `Code/Arguments/Message/Details/ErrorId` was not verified against source.

## 6. How do migrations (FluentMigrator) work, and how are database connections configured?

### Takeaway
Serene runs FluentMigrator at startup (`IDataMigrations.Initialize()`). For each configured connection key it auto-creates the DB and runs `MigrateUp`. Migrations are tagged per database with a `TargetDB` subclass attribute (e.g. `[NorthwindDB]`) and versioned with `[MigrationKey(yyyyMMdd_HHmm)]`. Connections live in `appsettings.json` → `Data` → `{Key: {ConnectionString, ProviderName, Dialect?}}`. ADO.NET providers are registered with `DbProviderFactories.RegisterFactory`.

### Cited Findings
- appsettings in Serene 10.6.0 — [SER appsettings.json](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/appsettings.json):
```json
"Data": {
  "Default": {
    "ConnectionString": "Server=(localdb)\\MsSqlLocalDB;Database=Serene_Default_v1;Integrated Security=true;Encrypt=false;TrustServerCertificate=true",
    "ProviderName": "System.Data.SqlClient"
  },
  "Northwind": {
    "ConnectionString": "Server=(localdb)\\MsSqlLocalDB;Database=Serene_Northwind_v1;Integrated Security=true;Encrypt=false;TrustServerCertificate=true",
    "ProviderName": "System.Data.SqlClient"
  }
}
```
- Each entry has `ConnectionString`, `ProviderName` and optional `Dialect` (built-in class name like `"SqlServer2012"` is enough). If `Dialect` is omitted, it is inferred from the provider. `SqlSettings.DefaultDialect` does not override auto-detection — [SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections)
- Provider registration — [SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs):
```csharp
public static void RegisterDataProviders()
{
    DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
    DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
    DbProviderFactories.RegisterFactory("Microsoft.Data.Sqlite", Microsoft.Data.Sqlite.SqliteFactory.Instance);
    // DbProviderFactories.RegisterFactory("FirebirdSql.Data.FirebirdClient", FirebirdSql.Data.FirebirdClient.FirebirdClientFactory.Instance);
    // DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySqlConnector.MySqlConnectorFactory.Instance);
    // DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);
}
```
  By default only SQL Server and SQLite are registered. For another DB, uncomment the line and add the NuGet package — [SQL Connections docs](https://serenity.is/docs/framework/data-access/sql-connections). Note that "System.Data.SqlClient" is mapped to the `Microsoft.Data.SqlClient` factory (the same `SqlClientFactory.Instance`).
- Migration runner — [SER DataMigrations.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Common/AppServices/DataMigrations.cs). `Initialize()` loops over database keys, calling `EnsureDatabase` (`MigrationUtils.EnsureDatabase(...)`, auto-creates the DB; you may delete it to disable this) then `RunMigrations`. The runner selects the FluentMigrator processor by `cs.Dialect.ServerType` (`AddOracleManaged`, `AddSQLite`, `AddFirebird`, `AddMySql5`, `AddPostgres`, else `AddSqlServer`). `RunnerOptions.Tags` = key + "DB" (e.g. "NorthwindDB"). `IncludeUntaggedMigrations` only for the Default key. It scans the type-source assemblies and calls `runner.MigrateUp()`. It is invoked in Startup via `app.ApplicationServices.GetRequiredService<IDataMigrations>().Initialize();` — [SER Startup.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Initialization/Startup.cs)
- DB tag attribute: `public class NorthwindDBAttribute : TargetDBAttribute { public NorthwindDBAttribute() : base(NorthwindConnectionAttribute.Key) { } }` — [NW NorthwindDBAttribute.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDBAttribute.cs). `MigrationKeyAttribute`/`MigrationUtils` live in Serenity.Extensions — [extensions MigrationUtils](https://github.com/serenity-is/Serenity/tree/master/common-features/src/extensions/Modules/MigrationUtils)
- Migration example (new in 10.6.0) — [NW NorthwindDB_20261008_1200_ProductConcurrencyVersion.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDB_20261008_1200_ProductConcurrencyVersion.cs):
```csharp
using FluentMigrator;

namespace Serenity.Demo.Northwind.Migrations;

[NorthwindDB, MigrationKey(20261008_1200)]
public class NorthwindDB_20261008_1200_ProductConcurrencyVersion : Migration
{
    public override void Up()
    {
        Alter.Table("Products")
            .AddColumn("RowVersion").AsInt32().NotNullable().WithDefaultValue(0)
            .AddConcurrencyVersionTrigger(this, idField: "ProductID");
    }

    public override void Down()
    {
        this.DropConcurrencyVersionTrigger("Products", "RowVersion");
        Delete.Column("RowVersion").FromTable("Products");
    }
}
```
- Create-table example using Serenity's cross-DB helpers `IdentityKey(this)` / `AutoIncrement(this)` — [NW NorthwindDB_20141123_1551_Initial.cs](https://github.com/serenity-is/Serenity/blob/master/common-features/src/demo.northwind/Migrations/NorthwindDB_20141123_1551_Initial.cs):
```csharp
[NorthwindDB, MigrationKey(20141123_1551)]
public class NorthwindDB_20141123_1551_Initial(IOptions<UserEntityOptions>? userEntityOptions = null) : Migration
{
    public override void Up()
    {
        Create.Table("Categories")
            .WithColumn("CategoryID").AsInt32().IdentityKey(this)
            .WithColumn("CategoryName").AsString(15).NotNullable()
            .WithColumn("Description").AsString(int.MaxValue).Nullable();

        Create.Table("Territories")
            .WithColumn("TerritoryID").AsString(20).NotNullable().PrimaryKey()
            .WithColumn("RegionID").AsInt32().NotNullable()
                .ForeignKey("FK_Territories_RegionID", "Region", "RegionID");
    }
}
```
- Serene's Default DB migrations: `DefaultDB_20141103_1400_Initial`, `DefaultDB_20141111_1130_Permissions`, `DefaultDB_20160515_0726_UserPreferences`, `DefaultDB_20161029_1300_ExceptionLog` — [SER Migrations/DefaultDB](https://github.com/serenity-is/Serenity/tree/master/serene/src/Serene.Web/Migrations/DefaultDB)
- **10.6.0:** `AsUserIdType` / `UserIdForeignKey` migration helpers and concurrency-trigger migration helpers were added for supported dialects — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Integration tests exist for migrations on SqlServer, Postgres, MySql, Oracle, Firebird and Sqlite, which shows multi-DB support — [Serene tests/sqlintegration](https://github.com/serenity-is/Serenity/tree/master/serene/tests/Serene.Tests/sqlintegration)
- The official docs navigation has no database-migration page. "Migration" entries are version-upgrade guides — observed via [docs index](https://serenity.is/docs/)

### Inferences
- File naming convention `<DB>DB_<yyyyMMdd>_<HHmm>_<Description>.cs` with a matching `MigrationKey`. Use `IdentityKey(this)` instead of FluentMigrator's `.Identity().PrimaryKey()` so the column works across dialects (e.g. Oracle sequences).
- To switch to PostgreSQL: add Npgsql, uncomment the factory registration, and set `"ProviderName": "Npgsql"` with a Postgres connection string. The dialect is inferred automatically.

### Gaps
- No official docs page on migrations was found. All migration notes come from source code.

## 7. Validation and exceptions (ValidationError) patterns

### Takeaway
Throw `ValidationError(errorCode, fieldName, message)` from handler or behavior hooks (usually `ValidateRequestAsync`). The endpoint converts it into a `ServiceError` with HTTP 400. The client grid or dialog shows the message and highlights the field named in `Arguments`. Required checks (`[NotNull]`), editable-field checks, IsActive checks and `ICustomValidator`s run automatically in the save handler's validation pipeline.

### Cited Findings
- Save validation order: `ValidatePermissions()` → `ValidateEditable()` (trims strings, rejects non-editable assignments) → `ValidateRequired()` (create) / `ValidateRequiredIfModified` (update) → `ValidateIsActive()` → `ValidateFieldValues()` (`ICustomValidator`) → behaviors' `OnValidateRequest` — [Validation docs](https://serenity.is/docs/services/validation)
- Required failure: code `Required`, message `"{0} field is required!"`. Other codes: `InvalidValue`, `CustomValidationError`, `EntityNotFound`, `RecordNotActive`. `ValidationError` has `ErrorCode`, `Arguments`, `IsSensitiveMessage`. Endpoint → `ServiceError {Code, Arguments, Message, Details, ErrorId}` with HTTP 400. Other exceptions give HTTP 500 with a generic message — [Validation docs](https://serenity.is/docs/services/validation)
- `ICustomValidator { string Validate(IValidationContext context); }` returns null when valid. `context.GetFieldValue("StartDate")` supports cross-field checks — [Validation docs](https://serenity.is/docs/services/validation)
- `ValidationError` constructors: `()`, `(message)`, `(localizer, LocalText)`, `(message, inner)`, `(errorCode, errorMessage)`, `(localizer, errorCode, LocalText)`, `(errorCode, arguments, errorMessage)`, `(errorCode, arguments, errorMessage, inner)`, `(localizer, errorCode, arguments, LocalText)`, `(errorCode, arguments, format, params formatArgs)` — [FW ValidationError.cs](https://github.com/serenity-is/Serenity/blob/master/src/core/Interface/ValidationError.cs)
- Real usage — [SER UserSaveHandler.cs](https://github.com/serenity-is/Serenity/blob/master/serene/src/Serene.Web/Modules/Administration/User/RequestHandlers/UserSaveHandler.cs):
```csharp
username = username.TrimToNull() ?? throw DataValidation.RequiredError(fld.Username, localizer);
if (!UserHelper.IsValidUsername(username))
    throw new ValidationError("InvalidUsername", "Username",
        "Usernames should start with letters, only contain letters and numbers!");
// ...
if (existing != null && existingUserId != existing.UserId)
    throw new ValidationError("UniqueViolation", "Username",
        "A user with same name exists. Please choose another!");
```
- **10.6.0:** localized validation errors for SQL constraint violations across SQL Server, PostgreSQL, MySQL, Oracle, Firebird and SQLite (`ISqlErrorExtractor`, `HumanizeSqlExceptionBehavior`, `[Unique]`'s `DisableDefaultBehavior`). Denied users now get `EntityNotFound` instead of `AccessDenied` on delete/undelete/save, to avoid leaking entity existence. The `DENY` permission is never granted. Malformed entity ids return a validation error — [CHANGELOG 10.6.0](https://github.com/serenity-is/Serenity/blob/master/CHANGELOG.md)
- Unique checks are declarative via `[Unique]`/`[UniqueConstraint]` — [Built-in Behaviors docs](https://serenity.is/docs/services/built-in-behaviors)

### Inferences
- Prefer the localized overloads (`ITextLocalizer` + `LocalText`) in multilingual apps. Handlers expose `Localizer` (used in UserSaveHandler).

### Gaps
- `DataValidation` helper methods (e.g. `RequiredError`, `CheckNotNull`, `EntityNotFoundError`) were seen only in usage and were not enumerated from source.
