# Serenity Platform: advanced, cross-cutting features and production guidance

> Research date: 2026-10-22. Primary source: the official docs at https://serenity.is/docs/. The docs version selector shows **10.6.0** as the latest release ([Multi-tenancy intro](https://serenity.is/docs/tutorials/multi_tenancy/multi_tenancy)).
> Method note: serenity.is could only be read through a summarizing web fetcher. Direct curl got a proxy 403, and GitHub API access to serenity-is repos was not enabled in this session. Code snippets below are verbatim where the fetcher returned them, and abbreviated where it hit quote limits. Anything not backed by a fetched page is either under "Inferences" (marked as unverified) or under "Gaps".

---

## 1. How does the permission system work end to end?

### Takeaway
Permission keys are free-form strings (`Area:Feature`). They appear in attributes on rows, pages and endpoints, are discovered by reflection (`IPermissionKeyLister`) for the role/user permission dialogs, and are checked server-side by `IPermissionService`. Serene/StartSharp implement that service with `BasePermissionService<TUserPermissionRow, TUserRoleRow>`, which adds caching and role lookup. **Since 10.6.0 permissions fail closed**: a row with no permission attributes is no longer public.

### Cited Findings

**Core abstractions (`Serenity.Abstractions`)** — [Authorization docs](https://serenity.is/docs/framework/authorization)
- `IUserAccessor` gives the current user, from `HttpContext.User` in web apps. Console apps, desktop apps and tests can supply their own implementation.
- `IPermissionService` performs permission checks. Its base implementation supports transient grants.
- `IUserRetrieveService` fetches users: `IUserDefinition? ById(string id); IUserDefinition? ByUsername(string username);`.
- Other interfaces: `IUserClaimCreator` (builds the ClaimsPrincipal), `IUserPasswordValidator`, `IPermissionKeyLister` (feeds the permission editor UI) and `IRolePermissionService`.
- `IUserProvider` combines `IUserAccessor`, `IUserRetrieveService`, `IUserClaimCreator`, `IImpersonator`, `IRemoveCachedUser` and `IRemoveAll`. It is registered with `AddUserProvider`.

**Base implementations (`Serenity.Extensions`)** — [Authorization docs](https://serenity.is/docs/framework/authorization)
- `BasePermissionService<TUserPermissionRow, TUserRoleRow>`
  - Abstract members: `GetUserRoles(ClaimsPrincipal)` and `UserHasPermission(ClaimsPrincipal, string)`.
  - Virtual members: `IsSuperAdmin` (defaults to false and is the most common override), `SuperAdminHasPermission`, `IsAsterisk`, `IsQuestionMark`, `IsDeny`, `IsValidKey`, `HasImpersonationPermission` and `HasPermission`.
  - Cache hooks: `GetUserPermissionsCacheKey`, `GetUserPermissionsCacheGroupKey`, `GetUserPermissionsCacheDuration` and `LoadUserPermissions`.
- `BaseRolePermissionService<TRolePermissionRow>` provides `LoadRolePermissions`.
- `BasePermissionKeyLister` collects keys from `[NestedPermissionKeys]` classes, assembly-level `PermissionAttributeBase` attributes, and attributes on types, methods and properties.
- `BaseUserRetrieveService<TRow>` caches users under `UserByID_<id>` and `UserByName_<lowercase username>`, with no expiry by default. The cache group is the row's `GenerationKey`. Override `ToUserDefinition(TRow)` to customize.
- Startup registration (Serene/StartSharp):
```cs
services.AddSingleton<IPermissionService, AppServices.PermissionService>();
services.AddSingleton<IPermissionKeyLister, AppServices.PermissionKeyLister>();
services.AddUserProvider<AppServices.UserAccessor, AppServices.UserRetrieveService>();
```

**Attributes** — [Authorization docs](https://serenity.is/docs/framework/authorization)
- Row level: `ReadPermission`, `ModifyPermission`, `InsertPermission`, `UpdatePermission`, `DeletePermission`, `ServiceLookupPermission` and `NavigationPermissionAttribute`. The lookup script has its own permission (`[LookupScript(Permission = ...)]`).
```cs
[ReadPermission("Administration:Security")]
[ModifyPermission("Administration:Security")]
[LookupScript(Permission = "Administration:Security")]
public sealed class UserRow : ...
```
- Field level: `FieldReadPermission`, `FieldInsertPermission`, `FieldUpdatePermission` and `FieldModifyPermission`. `FieldReadPermissionAttribute.ApplyToLookups` defaults to true.
- `RequiredPermissionAttribute` gates objects such as reports.
- `RegisterPermissionKeyAttribute` registers a key that is not used anywhere else.
- Docs error: the fine-grained example applies `[InsertPermission("Administration:User:Delete")]`, which is likely a typo for `DeletePermission`.
- `PageAuthorize` throws on unauthenticated access. `ServiceAuthorize` returns an AJAX-friendly service error. Both have overloads that take a permission key.
- Type-driven variants read the key from the row's attributes:
  - `AuthorizeCreate`: Insert, then Modify, then Read.
  - `AuthorizeUpdate`: Update, then Modify, then Read.
  - `AuthorizeDelete`: Delete, then Modify, then Read.
  - `AuthorizeList`: Read, then ServiceLookup.

**Special keys and logic expressions** — [Authorization docs](https://serenity.is/docs/framework/authorization)
- `"*"` (`SpecialPermissionKeys.Public`) means everyone, including anonymous users.
- `"?"` (`LoggedIn`) means any authenticated user.
- `"DENY"` denies everyone, including super admins.
- Logic expressions (`!`, `&`, `|`, parentheses) need the `LogicOperatorPermissionService` decorator. The parser uses shunting-yard to RPN and caches results. Plain keys bypass the parser.
```cs
services.AddSingleton<IPermissionService>(serviceProvider =>
    new LogicOperatorPermissionService(serviceProvider.GetRequiredService<BasePermissionService>()));
// e.g. "(Administration:Security & Administration:Translation) | Administration:User:Read"
```

**PermissionKeys classes and dialog registration**
```cs
[NestedPermissionKeys]
[DisplayName("Administration")]
public class PermissionKeys
{
    [Description("User, Role Management and Permissions")]
    public const string Security = "Administration:Security";
}
```
- The class is optional but recommended for IntelliSense, display names and typo prevention. The docs advise against fine-grained keys without a real business need — [Authorization docs](https://serenity.is/docs/framework/authorization)
- Discovery happens by reflection at app start. Any key used in `ReadPermission`, `ModifyPermission`, `InsertPermission`, `UpdatePermission`, `DeletePermission`, `PageAuthorize` or `ServiceAuthorize` on a Row, Page/action or Endpoint/action appears in the user and role permission dialogs. A key used nowhere else needs `[assembly: Serenity.ComponentModel.RegisterPermissionKey("MySpecialPermissionKey")]` — [How to register permissions](https://serenity.is/docs/howto/how_to_register_permissions_in_serene)
- Colons build the tree. `MyModule:SubModule:Permission1` sits under MyModule / SubModule, and the category key is the prefix ending in a colon. Don't create keys ending in `:` or equal to a category name; use `General`, `Common` or `View` instead. Category display text comes from localization, e.g. `"Permission.Northwind:Customer:"` in `site.texts.invariant.json` — [How to register permissions](https://serenity.is/docs/howto/how_to_register_permissions_in_serene)

**Implicit permissions (StartSharp)**
- `[ImplicitPermission(...)]` on a key constant grants other keys automatically. Example from the docs (abbreviated):
```cs
[ImplicitPermission(General), ImplicitPermission(View)]
public const string Delete = "Northwind:Customer:Delete";
[Description("Create/Update"), ImplicitPermission(General), ImplicitPermission(View)]
public const string Modify = "Northwind:Customer:Modify";
public const string View = "Northwind:Customer:View";
[Description("[General]")]
public const string General = "Northwind:General";
```
  Source: [Implicitly granted permissions](https://serenity.is/docs/startsharp/features/implicitly-granted-permissions).
- The authorization docs say this is "implemented only in the premium StartSharp app", not in Serene — [Authorization docs](https://serenity.is/docs/framework/authorization); the how-to says it "currently only works in Start#" — [How to register permissions](https://serenity.is/docs/howto/how_to_register_permissions_in_serene)

**Related features** — [Authorization docs](https://serenity.is/docs/framework/authorization)
- Impersonation: `IImpersonator` / `ImpersonatingUserAccessor`. Call `Impersonate` and `UndoImpersonate` in try/finally. "Login as user" is StartSharp-only and limited to the super admin by default.
- Transient grants: `ITransientGrantor.Grant/UndoGrant` keep grants in memory only. Use them for background or system operations.
- Account elevation: `RequiresElevationAttribute`, `IElevationHandler` and `DefaultElevationHandler`. The UI (`AccountElevationPageBase`) is in premium Serenity.Pro.Extensions.
- Throttler: `new Throttler(cache.Memory, key, duration, limit)` with `Check()` and `Reset()`. Serene and StartSharp allow 50 login attempts per 30 minutes per username.

**Client side** — [Client authorization docs](https://serenity.is/docs/framework/authorization-client)
- Prefer `Authorization.hasPermissionAsync` for new code, because it awaits the `UserData` script instead of blocking the UI thread.
- In client expressions `&` binds tighter than `|`. `"*"` always passes, `""` or `"?"` only checks login, and `null` returns false. Users with `IsAdmin` get all permissions.
- Other members: `Authorization.isLoggedIn` and `Authorization.userDefinition`.
- The docs state: "Client-side permission checks are **not security**."
```ts
if (await Authorization.hasPermissionAsync("Orders:View&Orders:Approve")) { ... }
```

**10.6.0 security hardening (breaking)** — [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)
- Rows without permission attributes are no longer implicitly public. Null or empty keys throw `ArgumentException`, and `DENY` is never granted.
- Save, delete and undelete return `EntityNotFound` (not `AccessDenied`) for denied users, so record existence stays hidden. Create still returns `AccessDenied`.
- Field read permissions are now enforced for sorting, equality filters, quick search and criteria operands.
- Undelete has its own permission, separate from delete.
- Fields with sensitive-looking names (secret, token, API key, 2FA) and `IPasswordRow` hash/salt fields are automatically made non-selectable, non-sortable and non-filterable.

**Role management UI**
- The docs list "Role-Based Access Control" as a StartSharp feature page — [StartSharp features](https://serenity.is/docs/startsharp/).

### Inferences
- The real flow is: attribute on a row or endpoint, then the request handler or authorize filter, then `IPermissionService.HasPermission(key)`, then `BasePermissionService` (super admin check, then user permission cache, then role permissions). Grant/revoke in the Administration "User Permissions" and "Role Permissions" dialogs writes `UserPermissions` and `RolePermissions` rows, and handlers expire the cache through group keys.
- When upgrading to 10.6+, audit every row for missing `[ReadPermission]`/`[ModifyPermission]`. Use `SpecialPermissionKeys.Public` or `LoggedIn` explicitly where intended.
- Unverified (from memory, not fetched): Serene's Administration module has `UserRow`, `RoleRow`, `UserRoleRow`, `UserPermissionRow`, `RolePermissionRow`, `PermissionKeys.Security = "Administration:Security"` and `Administration:Translation`, and the Membership pages (Login, Signup, ForgotPassword, ResetPassword, ChangePassword) live under `Modules/Membership`.

### Gaps
- Could not fetch the Serene `Administration` source code (GitHub access was not enabled). Cookie authentication setup (`AddAuthentication().AddCookie(...)`) is not documented on the authorization page.
- The "Role-Based Access Control" StartSharp page content was not fetched.

---

## 2. How is localization done?

### Takeaway
Text lookups go through `ITextLocalizer`, backed by the `ILocalTextRegistry` dictionaries. Texts are defined in code with `[NestedLocalTexts]` classes and in JSON files (`texts/resources/*.json` embedded, `App_Data/texts` on disk). The Administration Translations page edits them, writing to `App_Data/texts`.

### Cited Findings
All from [Localization docs](https://serenity.is/docs/framework/localization) unless noted.

**Core types**
- `ITextLocalizer.TryGet(string key)` resolves against `CultureInfo.CurrentUICulture`. The extension `Get` returns the key itself when there is no translation.
- `ILocalTextRegistry` has `TryGet(languageID, key, pending)` and `Add(languageID, key, text)`. Texts added later override earlier ones.
- `LocalText` wraps a key and converts implicitly from a string. `ToString(ITextLocalizer)` returns the translation, and the invariant language ID is `""`.

**Fallbacks**
- Lookup order is current language, then explicit fallback, then the neutral language, then invariant. Example: en-UK, en-US, en, invariant.
- Configure fallbacks with `ILanguageFallbacks.SetLanguageFallback`.

**Texts classes**
- Use a top-level `static partial` class with `[NestedLocalTexts]`. Nested class names are joined with dots to form keys; the top-level name is ignored.
- The attribute has `LanguageID` and `Prefix` properties. Registration happens through `AddBaseTexts`, which `DefaultLocalTextInitializer` calls.
```cs
[NestedLocalTexts]
public static partial class Texts
{
    public static class Site { public static LocalText WelcomeMessage = "Welcome to Serenity"; }
}
```

**Row texts**
- `DisplayName` on a row property produces `Db.<Prefix>.<Property>` keys.
- The prefix comes from `[Module]` or the namespace, or from `[LocalTextPrefix]`. Setting it in the `RowFields` constructor is discouraged.
- `EntitySingular` and `EntityPlural` come from `InstanceName` and `DisplayName` on the row class.

**JSON files**
- Embedded: `texts/resources` in each assembly, loaded by `AddJsonResourceTexts`.
- On disk: `App_Data/texts`, loaded by `AddJsonTexts`.
- Naming is `{Prefix}.{LanguageID}.json`, e.g. `site.texts.de.json` or `*.invariant.json`.
- Files load in file-name order. `DefaultLocalTextInitializer` loads base texts, then embedded resources, then `App_Data/texts`.
- **Before publishing, move translations from `App_Data/texts` into `texts/resources`.**

**Startup**
- `services.AddServiceHandlers()` calls `AddTextRegistry`.
- `services.AddLocalTextInitializer()` uses TryAdd, so a custom `ILocalTextInitializer` can replace it.
- `app.InitializeLocalTexts()` runs the initializer.

**Deprecated or discouraged**
- The old static `InitializeLocalTexts` method was replaced by the `ILocalTextInitializer` service.
- Overriding enum keys via `EnumKeyAttribute` is "NOT recommended".

**Translation UI**
- *Administration / Translations* saves to `App_Data/texts`. Texts can be re-initialized after edits.

**Client-side docs**
- Client-side localization has its own page: [Localization and Text (client)](https://serenity.is/docs/framework/localization-client). It was not fetched.

### Inferences
- In production (multi-instance, containers), `App_Data/texts` edits are local to one instance and get lost on redeploy. Treat the Translations UI as a dev-time tool and commit the JSON into `texts/resources`.
- Unverified (from memory): on the client, use `localText("Key")` / `tryGetText` from `@serenity-is/corelib`. Sergen generates typed `Texts` for TypeScript.

### Gaps
- How user language selection is stored (cookie or profile) is not detailed on the fetched page.
- The client localization page was not fetched.

---

## 3. How is multi-tenancy implemented in the official guide?

### Takeaway
The official tutorial (built on the Movie tutorial) adds a `TenantId` column to every table, including Users, and puts `TenantId` in the user definition/claims. A marker interface `IMultiTenantRow` plus an `IImplicitBehavior` called `MultiTenantBehavior` filter queries, stamp inserts and block cross-tenant edits and deletes. Lookups use a tenant-aware `MultiTenantRowLookupScript<>` with per-tenant caching. The author personally recommends a separate database per customer instead.

### Cited Findings
**Tutorial overview** — [Multi-tenancy intro](https://serenity.is/docs/tutorials/multi_tenancy/multi_tenancy)
- Steps:
  1. Tenants table and TenantId field
  2. Code generation
  3. Tenant selection in the User dialog
  4. Filtering users by TenantId
  5. Removing the tenant dropdown
  6. Securing tenant selection server-side
  7. Setting TenantId for new users
  8. Preventing edits to other tenants' users
  9. Hiding the Tenant Administration permission
  10. Making roles multi-tenant
  11. Using service behaviors
  12. Extending to the Movie DB
  13. Handling lookup scripts
- Trade-offs noted: lower management cost, but harder per-tenant backup or migration and possible performance impact. The author recommends a DB per customer.

**IMultiTenantRow and MultiTenantBehavior** — [Using Serenity service behaviors](https://serenity.is/docs/tutorials/multi_tenancy/using_serenity_service_behaviors)
```cs
public interface IMultiTenantRow
{
    Int32Field TenantIdField { get; }
}
```
- `MultiTenantBehavior : IImplicitBehavior` plus the async Retrieve, List, Save and Delete behavior interfaces. These interfaces have default no-op methods.
- `ActivateFor` returns false unless the row implements `IMultiTenantRow`, and caches `TenantIdField`.
- `OnPrepareQueryAsync` (list and retrieve) adds a filter unless the user has `PermissionKeys.Tenants`:
  `query.Where(tenantIdField == handler.Context.User.GetTenantId());`
- `OnSetInternalFieldsAsync` on create:
  `tenantIdField[handler.Row] = handler.Context.User.GetTenantId();`
- `OnValidateRequestAsync` on update rejects a TenantId change without the Tenants permission:
  `if (tenantIdField[handler.Old] != tenantIdField[handler.Row])`
- On delete, it rejects deleting another tenant's row without the permission:
  `if (tenantIdField[handler.Row] != handler.Context.User.GetTenantId())`
- One behavior instance is cached and shared, so it must be thread-safe. Use `IServiceResolver<T>` for extra dependencies.

**Lookup scripts** — [Handling lookup scripts](https://serenity.is/docs/tutorials/multi_tenancy/handling_lookup_scripts)
- `MultiTenantRowLookupScript<TRow> : RowLookupScript<TRow>` takes `ISqlConnections`, `ITwoLevelCache` and `IUserAccessor`.
- It sets `Expiration = TimeSpan.FromDays(-1)`, which disables the dynamic script manager cache because that cache is keyed per script, not per tenant.
- `PrepareQuery` calls `AddTenantFilter`, which runs `query.Where(r.TenantIdField == userAccessor.User.GetTenantId())`.
- `GetScript()` uses `twoLevelCache.GetLocalStoreOnly($"MultiTenantLookup:{this.ScriptName}:{userAccessor.User.GetTenantId()}", 1 hour, row GenerationKey, ...)`.
- Usage: `[LookupScript(LookupType = typeof(MultiTenantRowLookupScript<>))]`. Custom lookups that override `PrepareQuery` without calling base must call `AddTenantFilter` manually.

**StartSharp**
- The StartSharp feature list has no multi-tenancy item — [StartSharp](https://serenity.is/docs/startsharp/)

### Inferences
- For row-level security beyond tenants (owner-only rows, department scopes), copy the same pattern: a marker interface plus an `IImplicitBehavior` that adds `query.Where(...)` in `OnPrepareQuery` and validates in save and delete. The tutorial is the canonical example of row-level security in Serenity.
- Since 10.6.0, user IDs can be int, long, Guid or string via `UserEntityOptions` ([Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)). If your tenant or user keys are not `Int32`, adapt `Int32Field TenantIdField` accordingly.

### Gaps
- Code for adding `TenantId` to `UserDefinition` and claims, and for `GetTenantId()`, was not fetched. It is in the earlier tutorial pages, e.g. [Securing tenant selection](https://serenity.is/docs/tutorials/multi_tenancy/securing_tenant_selection_at_server_side) and [Making roles multi-tenant](https://serenity.is/docs/tutorials/multi_tenancy/making_roles_multi-tenant).

---

## 4. How do caching, file upload, export (Excel/PDF), email and background jobs work?

### Takeaway
- **Caching:** `ITwoLevelCache` = `IMemoryCache` + `IDistributedCache`, kept consistent with generation and group keys. Handlers expire the table's group automatically on every write.
- **Uploads:** two-phase (temporary upload, then `FileUploadBehavior` moves the file on save) through the pluggable `IUploadStorage` and `IUploadProcessor`. **10.6 requires registering an `IImageProcessor`**.
- **Reporting:** `IReport` / `IDataOnlyReport` (Excel) / HTML-to-PDF through `IHtmlToPdfConverter` (wkhtmltopdf free, Puppeteer in Pro).
- **Background tasks and mail queue:** StartSharp features.

### Cited Findings

**Caching** — [Caching docs](https://serenity.is/docs/framework/caching)
- Local `MemoryCache` needs no serialization. `IDistributedCache` (Redis, NCache, SQL) is needed for web farms.
- Serenity ships no distributed cache of its own. Serene and StartSharp default to `DistributedMemoryCache`. **Register Redis or SQL Server before `AddServiceHandlers`/`AddCaching`.**
- `MemoryCacheExtensions.Get<T>(key, expiration, loader)` caches not-found results as `DBNull`.
- `DistributedCacheExtensions.SetAutoJson/GetAutoJson` serialize values to JSON.
- Two-level read path: local cache, then distributed cache, then the database. Each value carries a random generation number stored under a companion key. The page says `$GENERATION$` in one place and `$Generation$` in another.
- Generation values are cached locally for 5 seconds (`GenerationCacheExpiration`), so other servers in a farm may serve stale data for up to 5 seconds.
- `ExpireGroupItems(groupKey)` invalidates a whole group. Create, Update, Delete and Undelete handlers call it automatically with `ConnectionKey.TableName`.
- `GetLocalStoreOnly` caches locally while still honoring group expiry.
```cs
public interface ITwoLevelCache { IMemoryCache Memory { get; } IDistributedCache Distributed { get; } }
// usage: cache.Get(key, localExpiration, distributedExpiration, groupKey, loader)
//        cache.GetLocalStoreOnly(key, expiration, groupKey, loader)
//        cache.ExpireGroupItems(MyRow.Fields.GenerationKey)
```
- Lookup caching: `RowLookupScript` results are cached with the row's `GenerationKey`, and `Expiration = TimeSpan.FromDays(-1)` disables script-manager caching — [Handling lookup scripts](https://serenity.is/docs/tutorials/multi_tenancy/handling_lookup_scripts)

**File uploads** — [Uploads docs](https://serenity.is/docs/services/upload)
- Flow:
  1. The client posts to `/File/TemporaryUpload`.
  2. `IUploadProcessor` runs a file-name security check, an optional `IUploadAVScanner` antivirus scan, `IUploadValidator` validation, then writes the temporary file and runs `IImageProcessor` scaling and thumbnails. It returns a `temporary/...` path.
  3. On save, `FileUploadBehavior` moves the file to its permanent path.
  4. Old files are deleted on commit (via `UnitOfWork.RegisterFilesToDelete`), or archived when `CopyToHistory` is set. On rollback, the new files are deleted.
  5. Temporary files are purged periodically.
- **Put the editor attribute on the Row property, not the Form.** Otherwise the behavior never runs and files stay in the temporary folder.
```cs
[DisplayName("User Image"), Size(100)]
[ImageUploadEditor(FilenameFormat = "UserImage/~", CopyToHistory = true)]
public string? UserImage { get => fields.UserImage[this]; set => fields.UserImage[this] = value; }
```
- Editors: `ImageUploadEditor`, `FileUploadEditor`, `MultipleImageUploadEditor` and `MultipleFileUploadEditor`. The multiple editors store a JSON array.
- Options on `BaseUploadEditorAttribute`:
  - Naming: `FilenameFormat`, `OriginalNameProperty`.
  - Size and dimensions: `MaxSize`/`MinSize`, `MaxWidth`/`MaxHeight`/`MinWidth`/`MinHeight`.
  - Types: `AllowNonImage`, `AllowedExtensions`.
  - Scaling: `Scale*`.
  - Thumbnails: `ThumbSizes`, `ThumbMode`, `ThumbQuality`.
  - `OverwriteOption` (Disallowed, Overwrite or AutoRename) controls name collisions.
- Storage: `DefaultUploadStorage` combines two `DiskUploadStorage` instances. Permanent files go to `UploadSettings.Path` (default `App_Data/upload/`, served at `/upload/`). Temporary files go to `<path>/temporary`.
- `appsettings` `UploadSettings`: `Path`, `Url`, `ExtensionBlacklistInclude`/`ExtensionWhitelistExclude` and `EditableMetadataKeys`.
- Registration:
```cs
public static IServiceCollection AddUploadStorage(this IServiceCollection collection)
{
    collection.TryAddSingleton<IFilenameFormatSanitizer, DefaultFilenameFormatSanitizer>();
    collection.TryAddSingleton<IUploadStorage, DefaultUploadStorage>();
    collection.TryAddSingleton<IUploadValidator, DefaultUploadValidator>();
    collection.TryAddSingleton<IImageProcessor, ThrowingImageProcessor>();
    collection.TryAddSingleton<IUploadProcessor, DefaultUploadProcessor>();
    collection.TryAddSingleton<IUploadFileResponder, DefaultUploadFileResponder>();
    return collection;
}
```
- File serving: `/upload/{path}` goes through `FilePage` to `IUploadFileResponder`. `DefaultUploadFileResponder` checks the path, sets `X-Content-Type-Options: nosniff`, and serves PDF, text and images inline; everything else downloads.
- **Per-file read security** (`[FileReadPermission]`, `[FileReadAccess]`, regex `PathPermissions`) is enforced only by `SecureUploadFileResponder` in premium Serenity.Pro.Extensions. **With Serene's default responder, anyone who knows the URL can read the file.**
- Deprecated or removed:
  - `UploadProcessor` is obsolete; use `IUploadProcessor`.
  - `ThumbnailGenerator` and the legacy `UploadProcessor` were removed.
  - The ImageSharp dependency was removed.
  - `ThrowingImageProcessor` is a placeholder. Register a real `IImageProcessor` before `AddUploadStorage()` (Serene and StartSharp ship a SkiaSharp implementation).
- Antivirus scanning has its own page: [AV scan](https://serenity.is/docs/av). It was not fetched.

**Reporting and export** — [Reporting docs](https://serenity.is/docs/services/reporting)
- Report types:
  - `IReport` has one method, `GetData()`, and its public properties are the report parameters.
  - `IDataOnlyReport` renders to Excel/CSV; `TabularDataReport` is a ready-made implementation.
  - `IExternalReport` returns a redirect URI.
  - `BaseReport` also implements `IReportWithAdditionalData`.
- Attributes:
  - `[Report]` registers the class for discovery. `[Category]`, `[DisplayName]` and `[RequiredPermission]` are optional.
  - `[ReportDesign]` points to the cshtml design.
  - `[CellDecorator]` styles Excel cells.
  - `[UseWKHtmlToPdf]` opts into the wkhtmltopdf converter. **`[UseChromeHtmlToPdf]` is obsolete.**
```cs
[Report, RequiredPermission(PermissionKeys.General)]
[Category("Northwind/Orders"), DisplayName("Customer Gross Sales")]
public class CustomerGrossSalesReport(ISqlConnections sqlConnections, ITextLocalizer localizer, IServiceProvider serviceProvider) : IReport, IDataOnlyReport
{
    [DisplayName("Start Date")] public DateTime? StartDate { get; set; }
    public object GetData() { /* query via SqlConnections */ }
}
```
- `IReportFactory` resolves constructor dependencies through DI. `IReportRegistry.GetAvailableReportsInCategory` filters reports by `[RequiredPermission]`.
- Excel: `IExcelExporter`, with `GenerateExcelFileRequest` and `GenerateCsvRequest` request models.
- PDF: `IHtmlToPdfConverter`.
  - Free option: `WKHtmlToPdfConverter`, registered by `AddReporting()`. It searches for the exe in the `WKHtmlToPdf:ExecutablePath` setting, then the assembly directory, the content root, `App_Data/Reporting`, `bin`, and finally PATH.
  - Premium option: `PuppeteerHtmlToPdfConverter` (Serenity.Pro.Extensions). Call `AddPuppeteerHtmlToPdf()` before `AddReporting()`. It is configured in the `PuppeteerHtmlToPdf` section (`Product`, `DownloadPath`, `ExecutablePath`, `CustomArgs`).
- The PDF converter calls back into the site's render endpoint, forwarding auth and language cookies plus a short-lived report cookie.
- **Behind a reverse proxy or in a container, set `EnvironmentSettings:SiteInternalUrl`** so the server-side browser can reach the site. `SiteExternalUrl` is used for links in emails.
- Since 10.6.0, `DefaultPageSize`/`MaxPageSize` can truncate lists. Use `SuppressPagingLimits()` for exports and background jobs — [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)

**Background jobs (StartSharp)** — [Background task system](https://serenity.is/docs/startsharp/features/background-task-system)
- Base class `PeriodicBackgroundTask`. Override `GetInterval()` and `InternalRun()`.
- The docs say it is simpler than, but "not as feature filled" as, HangFire.
```cs
protected override TimeSpan GetInterval()
{
    var config = Config.Get<MailingServiceSettings>();
    return TimeSpan.FromSeconds(config.Interval);
}
```
- StartSharp also documents [Background PDF Reports](https://serenity.is/docs/startsharp/features/background-pdf-reports) (not fetched).

**Email** — [Batch mail queue](https://serenity.is/docs/startsharp/features/batch-mail-queue)
- The StartSharp Batch Mail Queue sends mail in the background, retries after errors, and has a status reporting page. It is built on the background task system.
- StartSharp also has a React-based [Email Client](https://serenity.is/docs/startsharp/) feature.

### Inferences
- The `Config.Get<T>()` static in the background-task sample is a legacy pattern. Modern Serenity uses `IOptions<T>` via DI, so treat that snippet as possibly dated. (Unverified; the page does not flag it.)
- Serene (free) has no background job framework. Use ASP.NET Core `IHostedService`/`BackgroundService`, Hangfire or Quartz, and run DB work under `ITransientGrantor` or an impersonated user so permission checks pass.
- Unverified (from memory): Serene sends password-reset and activation emails through an `IEmailSender` abstraction in Serenity.Extensions, configured from an SMTP settings section in appsettings, with a MailKit-based implementation. If no SMTP host is set, emails go to a pickup directory in development. Verify against your template version.

### Gaps
- The Excel export endpoint in Serene grids (`ListExcel` + `ExcelExportHelper`, EPPlus vs other libraries) is not covered on the reporting page and was not verified.
- Email sending in Serene could not be verified from official docs (no docs page found; search returned only non-Serenity sources).
- The background task registration and appsettings section were not on the fetched page.

---

## 5. Which features are only in StartSharp / Serenity.Pro.*?

### Takeaway
StartSharp is the commercial, per-seat template. It includes everything in Serene plus premium features delivered as Serenity.Pro.* Razor class libraries.

### Cited Findings
- StartSharp features listed — [StartSharp](https://serenity.is/docs/startsharp/)
  - Premium Themes, Step by Step Wizard, Card View & React, Favourite Views
  - Excel Style Filtering, Drag & Drop Grouping, Customizable Summaries
  - Email Client (React), Dynamic Data Explorer, **Data Audit Log**, Product Picker
  - Split Master Detail, **Background Task System**, **Batch Mail Queue**, **Background PDF Reports**
  - Meeting Module, Organization & Contacts Module
  - **Login As User**, **Two Factor Authentication**, **Role-Based Access Control**, **Implicitly Granted Permissions**
  - Editors with Inline Buttons, DataTables / Bootstrap Table integration, Long Running Action with Progress
  - In the sidebar only: Account Elevation, Grid Edit Controller, OpenId Server Integration and Mobile App sample
- Licensing — [StartSharp](https://serenity.is/docs/startsharp/)
  - Serene is MIT. StartSharp is a commercial, per-seat licence that may not be used in open-source apps.
  - Tiers: Freelancer (1 seat), Business (3 seats) and Enterprise (5 seats, which adds the Flutter/MAUI samples).
  - All tiers include 12 months of updates and support.
- Data Audit Log: apply `[DataAuditLog]` to a row class (e.g. `OrderRow`, `OrderDetailRow`) and every change is logged to an audit table with no other code. Source path: `pro-features/src/Serenity.Pro.DataAuditLog` — [Data audit log](https://serenity.is/docs/startsharp/features/data-audit-log)
- Premium pieces named in framework docs:
  - `SecureUploadFileResponder`: [Uploads](https://serenity.is/docs/services/upload)
  - `PuppeteerHtmlToPdfConverter`: [Reporting](https://serenity.is/docs/services/reporting)
  - The `AccountElevationPageBase` UI, Login-as, and `ImplicitPermission`: [Authorization](https://serenity.is/docs/framework/authorization)
  - All of these live in Serenity.Pro.Extensions or the StartSharp app.
- `Serenity.Pro.Dashboards` was added in 10.6.0 as "work in progress, not for release" — [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)
- Upgrade paths from Serene: [Migrate](https://serenity.is/docs/startsharp/serene-upgrade-migrate) or [In-place](https://serenity.is/docs/startsharp/serene-upgrade-inplace)

### Inferences
- In Serene, "auditing" means the `LoggingRow` base (InsertUserId, InsertDate, UpdateUserId, UpdateDate). Since 10.6 these timestamps are UTC, and Serene's local `LoggingRow` was replaced by the Extensions version ([Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)). Full field-level change history needs StartSharp's DataAuditLog or a custom `ISaveBehavior`/`IDeleteBehavior` that writes diffs to an audit table.

### Gaps
- The full list of Serenity.Pro.* package names is not on the docs page. "Org chart" does not appear in the StartSharp feature list.

---

## 6. How do you deploy and upgrade a Serenity app?

### Takeaway
There is no dedicated deployment page in the current docs; deployment is a standard ASP.NET Core publish. Production specifics:
- enable script and CSS bundling
- set `SiteInternalUrl`/`SiteExternalUrl`
- move translations into embedded resources
- configure a real distributed cache for web farms
- make sure upload, `App_Data` and wkhtmltopdf/Chrome paths exist

Upgrades mean: update Serenity.* NuGet packages, sergen and npm packages, run `sergen restore`, then follow the release notes. Per-version migration guides stopped after 6.1.x.

### Cited Findings
- No deployment, testing or "publish" page appears in the docs navigation. The docs index points to [Script bundling](https://serenity.is/docs/framework/script-bundling) and [How to enable bundling](https://serenity.is/docs/howto/how_to_enable_script_bundling) as related pages — [Docs index](https://serenity.is/docs/)
- **Bundling** — [Script bundling](https://serenity.is/docs/framework/script-bundling)
  - Bundling is off by default. Enable both `CssBundling` and `ScriptBundling` in `appsettings.bundles.json` and make sure `Program.cs` loads that file.
  - `Minimize`, `UseMinJS` and `UseMinCSS` default to true.
  - Register with `AddDynamicScripts().AddCssAndScriptBundling()`.
  - The default minifier is `EsBuildMinifier`, which is "downloaded on demand" (plan for this in offline build agents). `NuglifyMinifier` is the alternative.
  - URLs get a `?v=` content hash for cache busting. A `CDN` section can rewrite URLs.
  - CSP: `CspNonce()` generates a per-request nonce, and `GetCspDirective` builds directives. Allow any external import-map host in `script-src`.
- Translations: move `App_Data/texts` into `texts/resources` before publishing — [Localization](https://serenity.is/docs/framework/localization)
- Distributed cache: register Redis or SQL `IDistributedCache` before `AddCaching`. Group-generation staleness can last up to 5 seconds on farms — [Caching](https://serenity.is/docs/framework/caching)
- Uploads: the default path is `App_Data/upload/`, configured with `UploadSettings:Path` — [Uploads](https://serenity.is/docs/services/upload)
- PDF:
  - Set `WKHtmlToPdf:ExecutablePath` or put wkhtmltopdf on PATH. Puppeteer needs a Chrome or Firefox executable or download path.
  - Set `EnvironmentSettings:SiteInternalUrl` behind proxies — [Reporting](https://serenity.is/docs/services/reporting)
- **Upgrade process** — [Migration intro](https://serenity.is/docs/migration/README)
  1. Update `Serenity.Assets`, `Serenity.Scripts`, `Serenity.Net.Web`, `Serenity.Extensions`, and any `Serenity.Pro.*` packages (from the premium feed).
  2. Run `dotnet tool update sergen`; its version must match Serenity.
  3. Update the npm packages `@serenity-is/corelib` and `@serenity-is/tsbuild`, then run `npm install`.
  4. Run `dotnet sergen restore`.
  5. Apply migration guides in order, plus the release notes ("Since version 6.1.8 we are no longer providing separate migration guides").
  - Premium customers can automate updates with [Stargen](https://serenity.is/docs/migration/stargen).
  - Projects after 6.1.x use ES modules by default.
  - Older guides: [v6.0→6.1.6](https://serenity.is/docs/migration/v6-0-0-to-v6-1-6), [5.2.3→6.0](https://serenity.is/docs/migration/v5-2-3-to-v6-0-0), [3.14→5.0.12](https://serenity.is/docs/migration/v3-to-v5) and [MVC→Core](https://serenity.is/docs/migration/mvc-to-core).
- **10.6.0 upgrade checklist** — [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)
  1. Add explicit permission attributes to rows that were implicitly public.
  2. Opt sensitive-named fields back in if they are needed.
  3. Register an `IImageProcessor` before `AddUploadStorage()`.
  4. Configure `UserEntityOptions` before the initial migrations, and update `LoggingRow` user ID usage (now `object`/`Field`).
  5. Update custom query code: `QueryWithParams.Params` is read-only, `IFilterableQuery` uses `ICriteria`, the `IChainable`/`With` API was removed, and interceptors receive `InterceptXMethodArgs`.
  6. StartSharp: replace `ConnectionKeys` classes with attribute `Key` constants.
  7. `DeltaLister` is strict by default; pass `DeltaOptions.IgnoreInvalidNewId` for the old tolerant behavior.
  8. Re-run Sergen.
  9. Audit list endpoints before enabling `DefaultPageSize`/`MaxPageSize`.
- Other 10.6 features useful in production:
  - Optimistic concurrency (`IConcurrencyVersionRow`)
  - `IncludeMore` to avoid `COUNT(*)`
  - Server-side page size limits (`RequestHandlerSettings`)
  - `Freeze()` queries with cached SQL
  - Compiled row factories
  - Fixed O(n²) relation batching
  - Int64 IDs serialized as strings in JS
  - Source: [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)

### Inferences (unverified, general ASP.NET Core practice applied to Serenity)
- Publish with `dotnet publish -c Release`, after running `npm ci` and the tsbuild/esbuild step so `wwwroot/esm` is current.
  - On IIS, use the ASP.NET Core Hosting Bundle.
  - On Linux, use Kestrel behind nginx with a systemd unit, and configure forwarded headers.
  - For Docker, use the `mcr.microsoft.com/dotnet/aspnet` runtime image. If you use wkhtmltopdf/Puppeteer PDF export, install its native dependencies (or Chromium) in the image.
- Mount persistent volumes for `App_Data/upload` (or a shared store via a custom `IUploadStorage`) and for the ASP.NET Core Data Protection key ring. Auth cookies and Serenity's `IDataProtector` tokens (password reset, email confirmation; see [Security](https://serenity.is/docs/framework/security)) break across restarts or instances if the keys are not persisted and shared.
- Put connection strings and secrets in environment variables or a secret store (e.g. `Data__Default__ConnectionString`), not in the committed `appsettings.json`. Run migrations (FluentMigrator in Serene) deliberately in production rather than automatically on startup, where possible.

### Gaps
- No official Serenity page for IIS, Linux or Docker deployment, appsettings production profiles, or testing was found in the current docs navigation.
- The required .NET version for 10.x is not stated in the 10.6.0 release notes.
- Testing guidance (unit-testing handlers, e.g. a `Serenity.Tests` helper package) was not found in the docs.

---

## 7. Security best practices specific to Serenity

### Takeaway
Rely on server-side attributes (rows, fields, endpoints), never on client checks. Upgrade to 10.6+ for fail-closed permissions, sensitive-field protection and LIKE escaping. Use the Throttler, data-protector tokens and CSP nonces. Remember that free Serene does not secure uploaded file reads.

### Cited Findings
- Client checks are UI only; "Client-side permission checks are **not security**" — [Client authorization](https://serenity.is/docs/framework/authorization-client)
- 10.6.0 hardening — [Release notes 10.6.0](https://serenity.is/docs/release-notes/10.6.0)
  - Permissions fail closed.
  - Existence hiding (`EntityNotFound`).
  - Sensitive-named fields and password hash/salt are auto-protected.
  - `StartsWith`, `EndsWith` and `Contains` escape `%` and `_` (closing LIKE injection).
  - Field read permissions are enforced in sort, filter and quick search.
  - The vulnerable ImageSharp 2.1.13 was removed.
  - Database constraint violations become localized validation errors instead of leaking provider exceptions.
- Web security helpers — [Security docs](https://serenity.is/docs/framework/security)
  - reCAPTCHA v2 via the `Recaptcha` config section, a `[Recaptcha]` form attribute and `RecaptchaValidation.Validate`.
  - `DataProtectorBinaryTokenExtensions` for one-time links.
  - `SiteMembershipProvider` is a legacy name; it now only provides `ComputeSHA512`.
  - CSP helpers in `HtmlCspExtensions`.
- Brute-force protection: the Throttler allows 50 failed logins per 30 minutes per username. Use a distributed cache to share counts across a farm — [Caching](https://serenity.is/docs/framework/caching); [Authorization](https://serenity.is/docs/framework/authorization)
- Step-up auth: `[RequiresElevation]` (the UI is Pro); 2FA is a StartSharp feature — [Authorization](https://serenity.is/docs/framework/authorization); [StartSharp](https://serenity.is/docs/startsharp/)
- Uploads — [Uploads](https://serenity.is/docs/services/upload)
  - Extension blacklist and whitelist settings.
  - File-name security checks.
  - An optional `IUploadAVScanner`.
  - The `nosniff` header.
  - Only safe types are served inline.
  - Per-file read authorization requires the Pro `SecureUploadFileResponder`.
- Multi-tenant isolation must be enforced server-side, both in behaviors and in lookup scripts — [MT behaviors](https://serenity.is/docs/tutorials/multi_tenancy/using_serenity_service_behaviors); [MT lookups](https://serenity.is/docs/tutorials/multi_tenancy/handling_lookup_scripts)

### Inferences
- In Serene, add your own authorization to `/upload/` for sensitive documents: a custom `IUploadFileResponder`, or store files outside the public path and serve them through a permission-checked endpoint.
- Give lookup scripts their own `Permission`. Lookups are downloaded in full to the client, so a public lookup leaks the whole table.

### Gaps
- CSRF/antiforgery handling, cookie options and HTTPS/HSTS defaults in Serene's `Startup` are not documented on the fetched security page.
