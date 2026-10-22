# Serenity Platform Fundamentals: Overview, Setup, Project Structure, and the Movie Tutorial (as of October 2026)

Research date: 2026-10-22. Primary source is the official docs at https://serenity.is/docs/. Note: serenity.is could only be read through a summarizing fetch tool (direct curl was blocked), and the GitHub tree view of the Serene web project was blocked by robots. Code snippets below come from the docs pages as returned by that tool.

## 1. What is Serenity, who maintains it, what are Serene and StartSharp, and how do their features differ?

### Takeaway
Serenity is an MIT-licensed ASP.NET Core + TypeScript platform for data-centric line-of-business apps, open source since 2015 under the GitHub org serenity-is. Serene is the free MIT starter template that the docs and tutorials use. StartSharp is the paid, per-developer-seat premium template: it includes everything in Serene plus themes, extra modules and Pro packages.

### Cited Findings
**What it is**
- Serenity is "an ASP.NET Core / TypeScript application platform built on open-source technologies". It targets line-of-business apps with database integration, many forms and listing/reporting pages. It reduces boilerplate and handles cross-cutting concerns such as caching, localization, validation and access control. It has been open source since 2015. — [Serenity Docs intro](https://serenity.is/docs/)
- The GitHub README describes Serenity as a platform for "data-centric business applications with a service-based architecture" that aims to cut boilerplate and maintenance cost. — [GitHub serenity-is/Serenity](https://github.com/serenity-is/Serenity)

**Repository, license and maintainer**
- License is MIT, which permits commercial use. The repo has about 2.7k stars and 804 forks. Top-level folders: `.github`, `build`, `common-features`, `docs/changelog`, `packages`, `serene`, `src`, `tests`. Root files include `Serenity.slnx`, `package.json` and `pnpm-workspace.yaml`. — [GitHub serenity-is/Serenity](https://github.com/serenity-is/Serenity)
- The old standalone `serenity-is/Serene` repo is archived and read-only. Its README says Serene "is now integrated into Serenity repository" under `serenity-is/Serenity/tree/master/serene`. The old repo had the web project at `src/Serene.Web`, plus `Serenity` and `common-features` git submodules and a `vsix` folder. It is MIT licensed. — [GitHub serenity-is/Serene (archived)](https://github.com/serenity-is/Serene)
- Maintainer: neither the docs intro nor the README names a person. The GitHub README only links to a Visual Studio Marketplace review URL containing "VolkanCeylan". — [GitHub serenity-is/Serenity](https://github.com/serenity-is/Serenity)

**Serene vs StartSharp**
- Serene is the free, open-source starter template that the docs use for tutorials and samples. StartSharp is the premium template for paid customers: everything in Serene plus a more polished theme and extra features. — [Serenity Docs intro](https://serenity.is/docs/)
- Serene and Serenity both use the MIT license, which allows open-source, commercial and proprietary use. — [StartSharp intro](https://serenity.is/docs/startsharp/README)

**StartSharp licensing and tiers**
- StartSharp is sold in three tiers: Freelancer (1 seat), Business (3 seats) and Enterprise (5 seats).
  - The commercial license covers any number of commercial apps but cannot be used in open-source apps.
  - Each purchase includes 12 months of repository access, updates and premium support. It does not auto-renew. If you don't renew, you keep the versions you received but lose updates and support.
  - No prices are listed on the docs page.
  - — [StartSharp intro](https://serenity.is/docs/startsharp/README)
- Tier extras:
  - Live developer-help credits: Business 3h, Enterprise 5h.
  - Enterprise only: sample Flutter/MAUI mobile and desktop apps (MAUI still in progress), including a WorkLog module.
  - — [StartSharp intro](https://serenity.is/docs/startsharp/README)

**StartSharp premium features**
- UI and grids: Premium Themes, Card View & React, Favorite Views, Excel Style Filtering, Drag & Drop Grouping, Customizable Summaries, Data Tables integration, Bootstrap Table & Form, Editors with Inline Buttons. — [StartSharp intro](https://serenity.is/docs/startsharp/README)
- Workflows and data: Step-by-Step Wizard, Split Master Detail, Data Audit Log, Product Picker Dialog, Dynamic Data Explorer, Long Running Action with Progress. — [StartSharp intro](https://serenity.is/docs/startsharp/README)
- Background processing: Background Task System, Batch Mail Queue, Background PDF Reports. — [StartSharp intro](https://serenity.is/docs/startsharp/README)
- Modules: Email Client (React-based), Meeting, Organization & Contacts. — [StartSharp intro](https://serenity.is/docs/startsharp/README)
- Security and access: Login As User, Two Factor Authentication, Role-Based Access Control, Implicitly Granted Permissions. — [StartSharp intro](https://serenity.is/docs/startsharp/README)

**StartSharp template options and packages**
- The `dotnet new startsharp` template has these boolean options, all defaulting to true: `-dm` (demo modules), `-dx` (Data Explorer), `-ec` (Email Client), `-mt` (Meeting), `-os` (OpenId Server), `-oc` (OpenId Client), `-T` (Two Factor Authenticator), `-wl` (Worklog). — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)
- Pro package names seen in the release notes:
  - `Serenity.Pro.Extensions` absorbed `Serenity.Pro.DataAuditLog`, `Serenity.Pro.EmailQueue` and `Serenity.Pro.Theme` in 9.0.
  - `Serenity.Pro.Meeting` absorbed `Serenity.Pro.Organization` in 9.0.
  - `Serenity.Pro.Dashboards` was work in progress as of 10.6.
  - — [9.0.0 notes](https://serenity.is/docs/release-notes/9.0.0); [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)

**What Serene ships with**
- Serene ships demo modules (Northwind, Basic Samples), Localization, and User and Role Management. After login you see a dashboard (numbers come from Northwind tables, the rest is random data) and an accordion navigation menu with search. — [Serene tour](https://serenity.is/docs/serene_tour/README)

### Inferences
- For teaching, the useful framing is: Serenity is the framework (NuGet and npm packages), Serene and StartSharp are application templates built on it, and Sergen is the scaffolding tool.
- Serene is fully adequate for learning. StartSharp matters mainly for commercial polish and modules.

### Gaps
- No official page states who the maintainer is (person or company). The Volkan Ceylan link is only indirect evidence.
- StartSharp prices are not on the docs page.

## 2. What is the tech stack, and what are the prerequisites and exact steps to create and run a new Serene project today?

### Takeaway
Current stack (10.x):
- .NET 10, with Visual Studio 2026 18.8+ when using Visual Studio.
- TypeScript 6.0.3+.
- Node LTS (node 24.10+, npm 11.1+).
- esbuild through the `tsbuild` npm tool.
- ES2022 ES modules.
- `@serenity-is/corelib`, `@serenity-is/domwise` (JSX + signals) and `@serenity-is/sleekgrid`.

To create and run from the CLI: `dotnet new install Serene.Templates`, then `dotnet new serene -n MyApp`, then `npm i`, `dotnet build` and `dotnet run`. Log in with admin / serenity.

### Cited Findings
**Current version and stack**
- Latest Serenity.Net.Web is 10.6.0 (2026-10-11) and targets net10.0. The previous releases were 10.5.2 (9/16/2026), 10.5.1, 10.5.0 (9/6/2026) and 10.4.0 (9/1/2026). — [NuGet Serenity.Net.Web](https://packages.nuget.org/packages/Serenity.Net.Web/)
- Serene.Templates 10.6.0 was last updated 10/12/2026. The install command for that version is `dotnet new install Serene.Templates@10.6.0`. — [NuGet Serene.Templates](https://packages.nuget.org/packages/Serene.Templates)
- 10.1.0 requires the .NET 10 SDK and Visual Studio 2026. Teams still on VS 2022 and .NET 8 should stay on 9.2.x. — [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
- Frontend packages:
  - `@serenity-is/corelib`: framework layer with widgets, grids, dialogs, editors, formatters and service calls.
  - `@serenity-is/domwise`: foundation layer with JSX that creates real DOM nodes, reactive signals and a disposal lifecycle.
  - `@serenity-is/sleekgrid`: "a modern rewrite of SlickGrid in TypeScript".
  - Corelib re-exports domwise. In Serene and StartSharp, corelib comes through the `Serenity.Corelib` NuGet package and its `package.json` entry points under `node_modules/.dotnet/`.
  - — [Frontend overview](https://serenity.is/docs/framework/ui/readme)
- 10.1 build changes: all scripts target ES2022. `buildGlobalBundles` builds IIFE bundles from `Modules/Common/bundles/*.bundle(.css|.ts)`. `appsettings.bundles.json` is deprecated. Import maps are supported (`AddImportMapEntry`, `RenderImportMap`). Solutions moved from `.sln` to `.slnx`. — [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
- .NET backend packages referenced in the docs: `Serenity.Net.Core`, `Serenity.Net.Services`, `Serenity.Net.Web`, `Serenity.Extensions`. — [Serenity Docs intro](https://serenity.is/docs/)

**Prerequisites**
- Visual Studio 2026+, latest updates. "The .NET 10 SDK is only supported in Visual Studio 2026 18.8+". — [Getting Started](https://serenity.is/docs/getting_started/README)
- TypeScript 6.0.3+, which the project references through `Microsoft.TypeScript.MSBuild`. — [Getting Started](https://serenity.is/docs/getting_started/README)
- Node.js and npm LTS. Example minimums: `npm -v` → 11.1.1, `node -v` → 24.10.0. — [Getting Started](https://serenity.is/docs/getting_started/README)
- Without VS 2026 you can use the CLI and VS Code. — [Getting Started](https://serenity.is/docs/getting_started/README)
- On Windows, if Visual Studio uses its older bundled Node, go to Tools > Options > Projects and Solutions > External Web Tools and put `C:\Program Files\nodejs` at the top of the list. — [Getting Started](https://serenity.is/docs/getting_started/README)

**Create and run from the CLI**
1. Run `dotnet new install Serene.Templates`. Pin an older version with `dotnet new install Serene.Templates:6.4.4`. StartSharp customers configure the premium package source, then run `dotnet new install Serenity.Pro.Templates`. — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)
2. Run `dotnet new serene -n HelloSerenity` (or `dotnet new startsharp -n HelloSerenity`). Without `-n`, the current folder name becomes the project name. — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)
3. In `HelloSerenity/HelloSerenity.Web`, run `npm i`, then `dotnet build`, then `dotnet run`. — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)
4. Browse to `http://localhost:5000`; the port may differ, so check the console. — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)

Template notes:
- `-D false` / `--DemoModules false` excludes Northwind and Basic Samples.
- Run `dotnet new serene --help` to list all options.
- `dotnet new` does not create a solution file. For Visual Studio, save one at the root, not inside the `.Web` folder.
- — [Installing Serene with dotnet new](https://serenity.is/docs/getting_started/installing_serene_with_dotnet_new)
- The template name is case-sensitive: use lowercase `serene`. `-o <folder>` sets the output folder. — [Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction)

**Create from Visual Studio**
- Choose Create a New Project, search "Serene", click Next, name the project, then click Create. The "Additional information" dialog lets you exclude demo modules. — [Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction)
- Visual Studio Marketplace extension instructions are at [installing_serene_from_visual_studio_gallery](https://serenity.is/docs/getting_started/installing_serene_from_visual_studio_gallery). This page was not fetched.

**First run**
- Press F5. On first run Serene creates its database in SQL LocalDB.
- `appsettings.json` has `Default` and `Northwind` connections using `MsSqlLocalDB`. With SQL Express, change the data source to `.\SqlExpress`.
- Log in as `admin` with password `serenity`.
- — [Starting Serene](https://serenity.is/docs/getting_started/starting_serene)
- Migrations run automatically at startup. `DataMigrations.cs` ensures each database (Default, Northwind) exists and calls `MigrateUp()`. The created database is `MovieTutorial_Default_v1` on `(localdb)\MsSqlLocalDB`. — [Movie tutorial ch.1](https://serenity.is/docs/tutorials/movies/01-creating-movie-table)

### Inferences
- On Linux or macOS there is no LocalDB, so a beginner must point the connection strings at another SQL Server or a supported database (for example Sqlite). The fetched pages did not document how; see Gaps.

### Gaps
- The fetched pages had no official instructions for Sqlite or other database providers on non-Windows systems.
- The minimum Node version is shown only as example output, not as a stated minimum.

## 3. What does each folder and file in a generated project do, and how does a request flow from browser to database?

### Takeaway
A Serene solution is a single ASP.NET Core project, `<Name>.Web`, with feature-based folders:
- `Modules/<Module>/<Entity>/`: Row, Columns, Form, Endpoint, handlers, Page, and TSX grid and dialog for each entity.
- `Migrations/DefaultDB`: FluentMigrator classes.
- `Initialization/Startup.cs`
- `AppServices`: permission, user and migration services.
- `App_Data/texts`: localization JSON.
- `wwwroot`: static content.

A browser request goes: TS service proxy POSTs JSON to `/Services/{Module}/{Entity}/{Action}` → `ServiceEndpoint` controller (`[JsonRequest]`, `[ServiceAuthorize]`, connection or UnitOfWork) → request handler → SQL through Serenity's entity layer → JSON `ServiceResponse`.

### Cited Findings
**Startup and folders**
- The Startup class is at `Initialization/Startup.cs`, beside the standard `Program.cs`. — [Application Startup](https://serenity.is/docs/framework/initialization)
- `ConfigureServices` order:
  1. `AddApplicationPartsFeatureToggles(Configuration)` and `AddApplicationPartsTypeSource()`.
  2. `ConfigureSections(Configuration)` binds `[DefaultSectionKey]` types to appsettings.
  3. ASP.NET Core services: MVC, authentication, antiforgery and so on.
  4. Serenity services: `IDataMigrations` and `IPermissionService` from `AppServices`, a user provider (`AppServices.UserAccessor`, `UserRetrieveService`), `AddServiceHandlers()`, `AddLocalTextInitializer()`, `AddDynamicScripts()`, `AddCssBundling()`, `AddScriptBundling()`, `AddUploadStorage()`, `AddReporting()`.
  - — [Application Startup](https://serenity.is/docs/framework/initialization)
- `Configure` pipeline:
  1. `RowFieldsProvider.SetDefaultFrom(...)`
  2. `InitializeLocalTexts()`
  3. Localization, exception handling, security headers, HTTPS, static files.
  4. `UseRouting()`, `UseAuthentication()`, `UseAuthorization()`.
  5. `UseDynamicScripts()`
  6. `UseEndpoints` maps controllers.
  7. `IDataMigrations.Initialize()` runs at the end.
  - In development, `UseNodeScriptRunner()` can run the TS build in watch mode.
  - — [Application Startup](https://serenity.is/docs/framework/initialization)
- `App_Data/texts` holds `*.json` localization files, loaded recursively. — [Application Startup](https://serenity.is/docs/framework/initialization)
- 10.1 replaced manual MVC convention setup with `AddServiceEndpointConventions()` and no longer enables `AllowSynchronousIO`. — [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
- The data-migration service lives at `Modules/Common/AppServices/DataMigrations.cs`. Migrations live in `Migrations/DefaultDB`; the initial one is `DefaultDB_20141103_1400_Initial.cs`. — [Movie tutorial ch.1](https://serenity.is/docs/tutorials/movies/01-creating-movie-table)
- Global bundles live in `Modules/Common/bundles/*.bundle(.css|.ts)` (10.1+). — [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
- `tsconfig.json` at the root of `X.Web` marks it as a TypeScript project. — [Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction)
- Global styles go in `wwwroot/Content/site/site.css`. — [Movie tutorial ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui)

**Files generated per entity** (Movie example, folder `Modules/MovieDB/Movie/`)
- `MovieRow.cs`: the entity class.
- `MovieColumns.cs`: grid column definitions.
- `MovieForm.cs`: form definitions.
- `MovieDialog.tsx`: the dialog. CSS such as `MovieDialog.css` is imported from the TSX.
- `MoviePage.cs` and `MoviePage.tsx`: the page controller and page script.
- `MovieGrid.tsx`: the grid.
- Save, Retrieve and Delete handlers (e.g. `MovieSaveHandler`).
- Navigation goes in `MovieDBNavigation.cs`.
- — [Movie tutorial ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui); [ch.10](https://serenity.is/docs/tutorials/movies/10-keeping-record-of-the-cast)

**Request flow**
1. A TS service helper (e.g. `XService.List`) POSTs JSON through `serviceCall` to `/Services/{Module}/{Entity}/{Action}`, e.g. `/Services/Administration/User/List`. The helper's `baseUrl` looks like `'Administration/User'`. — [Service Endpoints](https://serenity.is/docs/services/service_endpoints)
2. The `[Route]` attribute on the endpoint class sets the base path. `[ServiceAuthorize]` checks login and permission and returns a `NotAuthorized` service error rather than a redirect. — [Service Endpoints](https://serenity.is/docs/services/service_endpoints)
3. `ServiceEndpoint` deserializes the JSON body into a `ServiceRequest`-derived parameter through `[JsonRequest]`, which is async since 10.1. — [Service Endpoints](https://serenity.is/docs/services/service_endpoints); [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
4. The base class reads `[ConnectionKey]` and opens a connection with `ISqlConnections.NewByKey()`. Read actions (List, Retrieve) get an `IDbConnection`. Write actions (Create, Update, Delete) get an `IUnitOfWork` that commits on success and rolls back on failure. — [Service Endpoints](https://serenity.is/docs/services/service_endpoints)
5. The endpoint is a thin wrapper: `return handler.List(connection, request);`, with the handler injected through `[FromServices]`. The handler runs business logic and SQL. A `ServiceResponse`-derived result is returned as JSON. — [Service Endpoints](https://serenity.is/docs/services/service_endpoints)
6. Since 10.6, rows without permission attributes deny access by default, and denied save or delete requests return `EntityNotFound`. — [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)

### Inferences
- These folder roles are not covered by the fetched official pages; they are filled in from long-standing Serene template conventions and should be checked against a freshly generated project:
  - `Imports/ServerTypes`: TS types generated by `sergen servertypings` from C# rows, forms and services.
  - `Imports/MVC`: view-path helpers generated by `sergen mvc`.
  - `wwwroot/esm`: esbuild output.
  - `Modules/Texts.cs` (or similar): localizable text keys.
- The tutorial's statement that the Movie page URL is `/MovieDB/Movie` and the namespace is `MovieTutorial.MovieDB` ([ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)) shows that the module name drives both the route and the namespace.

### Gaps
- No fetched official page lists the complete folder tree of a 10.x Serene project. The GitHub tree view was blocked and the docs have no dedicated project-structure page in the sections reached. Imports, ServerTypes, Texts and the `wwwroot/esm` output are therefore unverified for 10.x.
- The fetched pages did not describe how request handlers (ListRequestHandler and the others) generate SQL.

## 4. What is Sergen and how is it used to scaffold CRUD pages from a table?

### Takeaway
Sergen is Serenity's code generator, a .NET local tool already installed in the Serene and StartSharp templates and run as `dotnet sergen <command>` from the Web project folder. It covers two jobs:
- `g` scaffolds Row, Services and UI from a database table, interactively.
- `transform` (`t`) syncs types between C# and TypeScript.

### Cited Findings
**Commands**

| Command | Alias | What it does |
|---|---|---|
| `generate` | `g` | Table code generator |
| `restore` | `r` | Restores scripts, fonts and CSS from nupkgs |
| `clienttypes` | `c` | TS editor and formatter types → C# |
| `mvc` | `m` | IntelliSense helpers for view locations |
| `servertypings` | `s` | C# row, form and service types → TS |
| `transform` | `t` | Runs `clienttypes`, `mvc` and `servertypings` at once |

- Install with `dotnet tool install sergen`; it is preinstalled in the templates. — [Sergen Commands](https://serenity.is/docs/sergen/sergen_commands)
- If `dotnet sergen g` errors, run `dotnet restore` and `dotnet tool restore` in the Web project folder. — [Movie tutorial ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)

**`dotnet sergen g` interactive flow** — [Movie tutorial ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)
1. Choose a connection from `appsettings.json` (`Default`).
2. Select the table with Space (`dbo.Movie`), then press Enter.
3. Enter the module name in PascalCase (`MovieDB`).
4. Enter the class identifier (`Movie`), which produces `MovieRow` and `MoviePage`.
5. Set the permission (default `Administration:General`).
6. Choose what to generate: All (Row, Repository & Service, User Interface, Custom), or only Row and Services.
7. Rebuild, then press F5.

Generated code is a starting point that you usually edit rather than regenerate.

**Configuration (legacy page)**
- The config page describes options such as `KDiff3Path` (merge on regeneration), `RemoveForeignFields`, `BaseRowClasses`, `TFSIntegration`, `GenerateTSTypings`, `GenerateTSCode` and `Connections`.
- It names the file `Serenity.CodeGenerator.config` and mentions `GenerateSSImports` (Saltarelle-era), which suggests the page is partly outdated.
- — [Code Generator (Sergen)](https://serenity.is/docs/sergen/code_generator_sergen)
- A Schema Provider page also exists: https://serenity.is/docs/sergen/sergen-schema (not fetched).

**Related changes in recent releases**
- 9.0: Sergen generates namespace constants and supports `[Symbol.typeInfo]` type registration. A `[GenerateInterface]` source generator was added. — [9.0.0 notes](https://serenity.is/docs/release-notes/9.0.0)
- 9.0: add `BeforeTargets="ResolveProjectStaticWebAssets"` to the `RunTSBuild` MSBuild target. — [9.0.0 notes](https://serenity.is/docs/release-notes/9.0.0)
- 10.6: Sergen validates module names. The upgrade checklist ends with "Regenerate code with your normal build or Sergen process". — [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)

### Inferences
- The `RunTSBuild` target (9.0 notes) shows that the TypeScript/esbuild build (`tsbuild`) is hooked into `dotnet build`. This is why the tutorials just say "rebuild" rather than running `sergen t` or `npm run build` by hand.
- The Sergen configuration page is probably legacy (it mentions TFS, Saltarelle and `Serenity.CodeGenerator.config`). Modern projects most likely keep this config in `sergen.json`, but that is not confirmed by the fetched pages.

### Gaps
- The fetched pages do not list the exact MSBuild targets or the `sergen.json` schema for 10.x.
- No current page reached documents a `tsbuild` command as a Sergen command.

## 5. What does the Movie tutorial teach, in order?

### Takeaway
The official Movie Database tutorial has 13 chapters (0–12). It builds a small IMDB-like app, MovieTutorial, and covers in order:
- Project creation.
- FluentMigrator migrations.
- Sergen scaffolding.
- UI customization with attributes.
- Navigation and quick search.
- Enums.
- Lookups and foreign keys.
- Many-to-many with `LinkingSetRelation`.
- Multi-value filtering.
- Master-detail with `MasterDetailRelation` and `GridEditorBase`.
- Listing movies in the Person dialog.
- Image uploads.

### Cited Findings
Chapter list: [Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction)

0. **Introduction** — Create `MovieTutorial` with `dotnet new serene -n MovieTutorial` or through Visual Studio. This produces a `MovieTutorial.Web` project. — [ch.0](https://serenity.is/docs/tutorials/movies/00-introduction)
1. **Creating Movie Table** — Add `Migrations/DefaultDB/DefaultDB_20221114_1505_MovieTable.cs`, named `{DB}_yyyyMMdd_HHmm_{Description}`.
   - Class attributes: `[DefaultDB, MigrationKey(20221114_1505)]`. The class inherits `AutoReversingMigration`.
   - `Up()` calls `Create.Table("Movie")` with columns `MovieId` (`AsInt32().IdentityKey(this)`), `Title` (200, NotNullable), `Description` (1000), `Storyline` (`Int32.MaxValue`), `Year`, `ReleaseDate` and `Runtime`.
   - Run with F5. Verify `[dbo].[Movie]` and the `VersionInfo` row `20221114150500`.
   - Never edit an applied migration; add a new one. Migrations are optional.
   - — [ch.1](https://serenity.is/docs/tutorials/movies/01-creating-movie-table)
2. **Generating Code For Movie Table** — Run `dotnet sergen g` with the choices from Section 4 (connection Default, table `dbo.Movie`, module `MovieDB`, identifier `Movie`). Rebuild, run and log in as admin/serenity. The menu gets MovieDB > Movie at `/MovieDB/Movie`, with full CRUD and no hand-written code. — [ch.2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)
3. **Customizing Movie UI**
   - Change `[DisplayName("Runtime (mins)")]` in the row.
   - Add `[DisplayName("Runtime in Minutes"), Width(150), AlignRight]` in `MovieColumns.cs`.
   - Add `[TextAreaEditor(Rows = 3)]` and `[TextAreaEditor(Rows = 8)]` in `MovieForm.cs`.
   - Create `MovieDialog.css` with a `.s-MovieDB-MovieDialog .caption` width rule and import it in `MovieDialog.tsx`.
   - Change the row-level `[DisplayName("Movies")]` and add `InstanceName("Movie")`.
   - — [ch.3](https://serenity.is/docs/tutorials/movies/03-customizing-movie-ui)
4. **Handling Movie Navigation** — [ch.4](https://serenity.is/docs/tutorials/movies/04-handling-movie-navigation) (not fetched; the title implies navigation link customization).
5. **Customizing Quick Search** — [ch.5](https://serenity.is/docs/tutorials/movies/05-customizing-quick-search) (not fetched).
6. **Adding a Movie Kind Field**
   - Add migration `DefaultDB_20221114_1825_MovieKind.cs` (Int32 `Kind`, not null, default 1).
   - Add the enum `Modules/MovieDB/Movie/MovieKind.cs` with `[EnumKey("MovieDB.MovieKind")]` and values Film=1, TvSeries=2, MiniSeries=3, each with `[Description]`.
   - Add the row property `MovieKind? Kind` with `[DisplayName("Kind"), NotNull, DefaultValue(MovieKind.Film)]` and the field `EnumField<MovieKind> Kind`.
   - Add `Kind` to the form and columns. The dialog then shows a dropdown.
   - — [ch.6](https://serenity.is/docs/tutorials/movies/06-adding-movie-kind-field)
7. **Adding Movie Genres** — [ch.7](https://serenity.is/docs/tutorials/movies/07-adding-movie-genres) (not fetched; ch.8 shows it added a single `GenreId` foreign key, `GenreName` and a `GenreRow` with a lookup).
8. **Supporting Multiple Genres per Movie**
   - Add migration `DefaultDB_20221115_1405_MovieGenres.cs`: create the `MovieGenres` table, copy the data, drop `GenreId`.
   - Run Sergen for `dbo.MovieGenres` with Row & Services only.
   - Add the row property `List<int> GenreList` with `[LookupEditor(typeof(GenreRow), Multiple = true)]`, `[NotMapped]` and `[LinkingSetRelation(typeof(MovieGenresRow), MovieId, GenreId)]`, plus a `ListField<int>` field.
   - Add a custom TS formatter `GenreListFormatter.tsx` that uses `GenreRow.getLookupAsync`, `ctx.escape` and `ctx.grid?.invalidate()`, then apply `[GenreListFormatter]` in the columns.
   - — [ch.8](https://serenity.is/docs/tutorials/movies/08-multiple-genres-per-movie)
9. **Filtering with Multiple Genres** — [ch.9](https://serenity.is/docs/tutorials/movies/09-filtering-with-multiple-genres) (not fetched).
10. **Keeping Record of the Cast**
    - Add migration `DefaultDB_20221115_1612_PersonAndMovieCast.cs` (Person and MovieCast tables, seeds three actors).
    - Run Sergen on Person, add a `Gender` enum, and add `FullName` built with `Concat` and marked `QuickSearch`, `NameProperty` and `LookupScript`.
    - Run Sergen on MovieCast and delete its page and navigation entry.
    - Add `[MasterDetailRelation(foreignKey: nameof(MovieCastRow.MovieId), ColumnsType = ...)]` on `CastList`, so `MasterDetailRelationBehavior` handles retrieve, save and delete in one transaction.
    - Create `MovieCastEditor.tsx` extending `GridEditorBase` from `@serenity-is/extensions`, and `MovieCastEditDialog.tsx` extending `GridEditorDialog`.
    - — [ch.10](https://serenity.is/docs/tutorials/movies/10-keeping-record-of-the-cast)
11. **Listing Movies in Person Dialog** — [ch.11](https://serenity.is/docs/tutorials/movies/11-listing-movies-in-person-dialog) (not fetched).
12. **Adding Primary and Gallery Images** — [ch.12](https://serenity.is/docs/tutorials/movies/12-adding-primary-and-gallery-images) (not fetched). Note that 10.6 requires an `IImageProcessor` registration for image upload paths ([10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)).

Other official tutorials: Student Information System ([courses](https://serenity.is/docs/tutorials/courses/00-introduction)) and Multi-Tenancy ([multi_tenancy](https://serenity.is/docs/tutorials/multi_tenancy/multi_tenancy)). — [Serenity Docs intro](https://serenity.is/docs/)

### Inferences
- The migration dates (2022-11) show the tutorial was written in the 6.x era but has been updated: it uses `.tsx` files, `@serenity-is/extensions` and `getLookupAsync`. It is suitable as a current teaching path.
- A teacher can map the chapters onto concepts:

| Chapters | Concept |
|---|---|
| ch.1 | Data layer |
| ch.2 | Scaffolding |
| ch.3–5 | Attributes-driven UI |
| ch.6–9 | Relations |
| ch.10–11 | Master/detail |
| ch.12 | File uploads |

### Gaps
- Chapters 4, 5, 7, 9, 11 and 12 were not fetched in detail, because of the tool-call budget.

## 6. What is the latest Serenity version and what are the key breaking changes and historical transitions?

### Takeaway
The latest version is 10.6.0 (2026-10-11).

Major milestones:

| Version | Date | Milestone |
|---|---|---|
| 1.x | 2015 | First releases |
| 3.x | up to 2020 | Older line |
| 5.x | 2022 | Older line |
| ~6.1.x | 2022 | ES modules become the default |
| 8.0 | Nov 2023 | .NET 8; Saltarelle and legacy assets purged |
| 9.0 | Sep 2025 | jQuery-era scripts removed; `Symbol.typeInfo` replaces decorators |
| 10.0 / 10.1 | Jan 2026 | .NET 10, VS 2026, ES2022, `.slnx`, `Q` namespace removed |
| 10.6 | Oct 2026 | Secure-by-default permissions and many query-API breaks |

### Cited Findings
**Version history**
- Versions with dates: 1.4.7 (2015-01-16), 2.10.3 (2017-08-17), 3.14.0 (2020-08-06), 5.2.3 (2022-01-20), 6.1.8 (2022-09-29) through 6.9.6 (2023-10-26), 8.0.0 (2023-11-16) through 8.8.1 (2024-12-01), 9.0.0 (2025-09-16), 9.2.0 (2025-11-24), 10.1.0 (2026-01-12), 10.2.0 (2026-02-16), 10.3.0 (2026-04-14), 10.4.0, 10.5.x, 10.6.0 (2026-10-11).
- There is no 7.x line.
- Separate migration guides stop after 6.1.x ("Since version 6.1.8 we are no longer providing separate migration guides"). Older guides cover 3.14→5.0.12, MVC→Core, and 5.2.3→6.0.0.
- ES modules are the default in "any project after 6.1.x".
- — [Versions](https://serenity.is/docs/versions); [Migration index](https://serenity.is/docs/migration/README)

**8.0.0 (2023-11-16)** — [8.0.0 notes](https://serenity.is/docs/release-notes/8.0.0)
- Targets net8.0 and needs VS 17.8+. Drops .NET 6 and 7.
- `serenity.css` moved to `Serenity.Extensions` as `common-style.css`.
- Many Bootstrap 3, jQuery-plugin and Saltarelle files removed from Serenity.Assets.
- jQuery updated to 3.7.1.
- Bootstrap 5.3.2 (dark mode with `data-bs-theme`).
- esbuild 0.19 and tsbuild 8.0.

**9.0.0 (2025-09-16)** — [9.0.0 notes](https://serenity.is/docs/release-notes/9.0.0)
- Static `[Symbol.typeInfo]` registration replaces decorators, which are deprecated. Projects still using decorators need `experimentalDecorators: true`.
- jQuery plugins, jQuery UI, jsPDF and CKEditor removed from Serenity.Assets.
- EPPlus replaced by ClosedXML.
- `Serenity.Net.Data` and `Serenity.Net.Entity` merged into `Serenity.Net.Services`.
- Request handlers moved to a `.RequestHandler` namespace.
- Global usings added.
- New File Explorer sample built with JSX and signals.
- `npmCopy` added to tsbuild.
- Partial row properties (need the .NET 9 SDK).

**10.1.0 (2026-01-12)**, building on the 10.0.0 move to .NET 10 — [10.1.0 notes](https://serenity.is/docs/release-notes/10.1.0)
- Requires the .NET 10 SDK and VS 2026; stay on 9.2.x for VS 2022 / .NET 8.
- The legacy `Q` namespace is removed and `Serenity` is the single entry point. Full removal is planned for the next major version.
- ES2022 target; `buildGlobalBundles` and import maps; `appsettings.bundles.json` deprecated.
- `ScriptSkip` replaced by `TransformIgnore`.
- `.sln` replaced by `.slnx`.
- Domwise is re-exported from corelib.
- jspdf is bundled locally instead of loaded from a CDN, which helps CSP.

**10.6.0 (2026-10-11)** — [10.6.0 notes](https://serenity.is/docs/release-notes/10.6.0)

Breaking changes:
- Permissions now fail closed: rows without permission attributes deny access. The `DENY` key is never granted.
- Denied save and delete requests return `EntityNotFound`.
- ImageSharp removed; image uploads need an `IImageProcessor` registration.
- `QueryWithParams.Params` is read-only and the `IChainable`/`With` API is removed.
- `ConnectionKeys` classes removed in favor of attribute `Key` constants (StartSharp).
- `DeltaLister` is strict by default.
- `StartsWith`, `EndsWith` and `Contains` escape `%` and `_`.
- `LoggingRow` user IDs are typed as `object`.

New features:
- Sensitive-field auto-protection.
- Separate undelete permission.
- Optimistic concurrency through `IConcurrencyVersionRow`.
- Configurable user ID type (int, long, Guid, string).
- `ListRequestCapabilities` and `IncludeMore`.
- Int64 precision support.
- Localized SQL constraint errors.
- Central NuGet package management in Serene and StartSharp.

### Inferences
- These are reasonable deductions, not confirmed by the fetched pages:
  - The Saltarelle (C#-to-JS) era predates 8.0, since 8.0 removed leftover Saltarelle files.
  - The ESM switch happened around 6.1.
  - jQuery removal was gradual: legacy assets went in 8.0 and jQuery UI and plugins in 9.0. The modern frontend (domwise JSX, sleekgrid) no longer requires jQuery.
- Beginners should start on 10.x and should follow tutorial code that uses `.tsx`.
- After 10.6, every new row needs an explicit permission attribute or it will be inaccessible. This is a common beginner pitfall.

### Gaps
- No 10.0.0 release-notes page was found in the version index, which starts at 10.1.0.
- The exact versions for "namespaces → ES modules", "Saltarelle removal" and "jQuery fully optional" were not stated explicitly in the fetched pages.
- No dedicated official blog post on Serenity's history was found.
