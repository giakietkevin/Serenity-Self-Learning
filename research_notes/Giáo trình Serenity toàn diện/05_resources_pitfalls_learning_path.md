# Serenity Platform: Learning Resources, Common Pitfalls, Best Practices, and a Learning Path

Research date: 2026-10-22. The current Serenity release is **10.6.0, released 2026-10-11** ([blog index](https://serenity.is/blog/)). The source material splits into several eras: Saltarelle/T4/ASP.NET MVC (pre-2020), ASP.NET Core with namespaces (v3 to v5), the move to ES modules (v6, from 2022), jQuery removal and `Symbol.typeInfo` (v9, 2025), and DomWise with fail-closed permissions (v10, 2026). When reading any source, check which era it comes from first.

## 1. Official GitHub repos and sample apps: what exists and what each teaches

### Takeaway
Everything official is now consolidated into the **serenity-is/Serenity** monorepo. The Serene template and the common-features samples (Northwind, BasicSamples) live in `serene/` and `common-features/` inside it. The standalone `serene` and `common-features` repos were archived in Oct 2024 and are out of date. There is no separate public "demo" repo. The demo is a hosted site, demo.serenity.is.

### Cited Findings
- The serenity-is org has 9 public repos: **Serenity** (~2.7k stars, updated Oct 2026), **docs** (documentation site source), **sleekgrid** (a read-only subtree of the grid component, a modern SlickGrid-inspired grid), **blog**, **devtools** (client-side developer tools), **serene** (archived), **common-features** (archived), **exceptional** (fork for Serenity.Exceptional), and **.github**. — [GitHub serenity-is org](https://github.com/serenity-is)
- The serenity-is/Serenity master branch has these top-level folders: `build`, `common-features`, `docs/changelog`, `packages`, `serene`, `src`, `tests`. It also has `Serenity.slnx`, `Directory.Packages.props`, a pnpm workspace and `vitest.config.ts`. The README quickstart is `dotnet new install Serene.Templates`, then `dotnet new serene`, then `npm i` in the `.Web` folder, then `dotnet run`. The license is MIT and allows commercial use. Premium customers use the StartSharp template from the members area. The README warns that community video tutorials are **unofficial and may be outdated**. — [serenity-is/Serenity](https://github.com/serenity-is/Serenity)
- The serene repo README says the template "is now integrated into Serenity repository at" `Serenity/tree/master/serene`, and that the files in the archived repo are likely out of date. The repo was archived on 2024-10-29. — [serenity-is/serene](https://github.com/serenity-is/serene)
- The common-features repo is described as "Demo modules and features shared between Serene and StartSharp" and was archived in Oct 2024. — [GitHub serenity-is org](https://github.com/serenity-is)
- The Serene feature tour in the docs covers the Northwind module, the Basic Samples module, localization, and user/role management. — [Serenity docs index](https://serenity.is/docs/)
- The online demo is at https://demo.serenity.is (login admin / serenity). It also has Google Play and App Store "Serenity demo app" listings. — [demo.serenity.is](https://demo.serenity.is); [Serenity README](https://github.com/serenity-is/Serenity)
- Since 9.0.1, demo source links point to release commits. Version 9.0 added the File Explorer, Calendar (FullCalendar) and Inline Grid Editor samples. — [9.0.0 release notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
- The official tutorials are:
  - **Movie Database**: 13 steps, from the movie table through primary and gallery images.
  - **Student Information System (courses)**: 9 steps.
  - **Multi-Tenancy**: 14 steps, from a tenants table through lookup scripts.
  — [docs index](https://serenity.is/docs/)
- The Movie tutorial is current for CLI use. It creates the project with `dotnet new serene -n MovieTutorial` and runs Sergen with `dotnet sergen g`, a console UI. If Sergen errors, it says to run `dotnet restore` and `dotnet tool restore`. Generation uses the default permission `Administration:General`, which only `admin` has by default. — [Movie tutorial intro](https://serenity.is/docs/tutorials/movies/00-introduction); [Movie step 2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)
- **devtools** is a separate repo for a Chrome extension of client-side developer tools. — [Serenity README](https://github.com/serenity-is/Serenity); [devtools](https://github.com/serenity-is/devtools)
- The docs site source is public at serenity-is/docs, and the blog source at serenity-is/blog. — [GitHub serenity-is org](https://github.com/serenity-is)

### Inferences
- What each resource teaches:
  - **Serene (`serene/`)**: project skeleton, startup and dependency injection, the Administration module (users, roles, permissions, languages, translations), and migrations.
  - **common-features/Northwind**: a realistic CRUD domain with master/detail (Order and OrderDetail), lookups, filtering, and reporting.
  - **BasicSamples**: one focused sample per UI technique (grids, editors, dialogs, inline editing).
  - **Movie tutorial**: the end-to-end Sergen workflow and customizing grids, forms and lookups.
  - **Courses tutorial**: relational modelling and navigation.
  - **Multi-tenancy tutorial**: row filtering, row behaviors, and cached lookups. This is advanced.
- Learners should clone serenity-is/Serenity and read `serene/` and `common-features/` at the git tag matching their NuGet version. The archived repos should not be used.
- The demo's "source" links let you go from a UI behavior you see to the code that produces it.

### Gaps
- I could not list the inner folder contents of `common-features/` (GitHub tree fetch was blocked by robots). The module names (Northwind, BasicSamples) come from the docs tour, not a direct listing.
- I found no public repo named serenity-is/demo. The demo appears to be hosted only, with StartSharp-only modules behind the premium license.

## 2. Community resources: what exists and how reliable it is

### Takeaway
The community ecosystem is **thin and mostly old**. The most reliable sources after the official docs are the official blog's release notes (one per version, highly current), GitHub Discussions (active but often unanswered), and the framework source itself. Third-party blogs, videos and forks are mostly pre-v6 and should be treated as outdated.

### Cited Findings
- **Official blog (most current, high reliability)**: release notes for every version, from 10.1.0 (2026-01-12) through 10.6.0 (2026-10-11), plus 9.0/9.1/9.2 (2025) and 8.x (2023-2024). Key conceptual posts:
  - "Switching to ES Modules" (2022-10-15)
  - "Introducing DomWise" (2026-07-24), on a JSX library that compiles straight to real DOM nodes instead of a virtual DOM
  - "Source Generators in StartSharp 6.1" (2022)
  - Older migration posts from 2017 to 2021, which are outdated for new learners
  — [blog index](https://serenity.is/blog/); [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules); [DomWise post](https://serenity.is/blog/2026/07/24/domwise)
- **GitHub Discussions**: categories are Help, General, Ideas, Announcements, and Tricks and Samples. On the first page (25 threads), 17 were unanswered. Recent recurring themes:
  - PDF export (including a 9.2.1 failure)
  - StartSharp/premium repo access
  - grid behavior (frozen columns, in-memory detail grid sorting, tooltips)
  - master/detail saving
  - post-upgrade breakage (`getPersistanceKey()` disappeared in 9.1+; "use strict" bundling errors in QA)
  - multi-file uploads
  — [Serenity Discussions](https://github.com/serenity-is/Serenity/discussions)
- The Serenity repo shows **0 open issues** (search API metadata). This suggests issues are closed or redirected and Discussions are the support channel. — [GitHub search result for serenity-is/Serenity](https://github.com/serenity-is/Serenity)
- A GitHub search for Serenity/Serene repos with more than 15 stars returns mostly unrelated projects (SerenityOS, Serenity BDD, serenity-rs). The only relevant ones are the official repos. Search results also show many plain forks of Serenity (asutosha/Serenity, kipergil/Serenity, gitee mirrors) with no added teaching value. — GitHub repository search (query: `serenity serene stars:>15`); [example fork](https://github.com/kipergil/Serenity)
- The official README itself says community video tutorials are unofficial and may be outdated. — [Serenity README](https://github.com/serenity-is/Serenity)
- The ES-modules post said in 2022 that "older tutorials and most existing documentation use namespaces". Anything that uses `namespace X { }`, globals like `Serenity.EntityDialog`, Saltarelle, T4 templates, or `web.config` predates modern Serenity. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- **Even official docs pages contain stale content.**
  - The Troubleshooting page still covers Saltarelle Script projects, `System.dll` references, T4 transforms, `web.config` LocalTextPackages, Select2 3.5.1, and medium-trust hosting. All of these are obsolete. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
  - The FAQ refers to `JsDate.Today` (Saltarelle-era) and to publishing "only the Web project, not the script project". — [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)
  - The Sergen page describes the old `Serenity.CodeGenerator.config` options (GenerateSSImports, KDiff3, TFS integration) and does not explain the modern `dotnet sergen` commands. — [Sergen docs](https://serenity.is/docs/sergen/code_generator_sergen)
- The docs migration section has guides only up to "6.0.0 to 6.1.6". From v6 to v10, the release-notes blog posts serve as the upgrade guides. — [docs index](https://serenity.is/docs/); [blog index](https://serenity.is/blog/)
- Sergen ships as a NuGet .NET tool (`sergen`, versions 10.3.x seen). — [nuget sergen](https://www.nuget.org/packages/sergen)

### Inferences
- Reliability ranking:
  1. Framework source and the `serene/` and `common-features/` folders at your version tag
  2. Release notes on the blog
  3. Current docs sections: Backend/Frontend Framework, tutorials, API reference
  4. GitHub Discussions, when answered by maintainers
  5. StackOverflow and third-party blogs and videos, assumed outdated unless dated 2023 or later and module-style
- Version rule of thumb: v6 introduced ES modules. v8 is the 2023-2024 line. v9 (Sep 2025) removed the jQuery assets and introduced `Symbol.typeInfo`. v10 (2026) adds DomWise and fail-closed permissions. Treat material older than v8 as conceptually useful but syntactically outdated.

### Gaps
- **StackOverflow [serenity-platform] tag**: access was blocked by the fetch tool, so I could not measure question counts or recency. Based on general knowledge (unverified here), most answers there are pre-2021 and namespace-era.
- I found no well-maintained third-party video series, course, or blog for v8+ in searches. If one exists it is not prominent. I found no reliable source for specific YouTube channels.

## 3. Common problems learners hit and how to fix them

### Takeaway
Modern pitfalls fall into five groups: (a) environment and tooling (SDK/Node/TypeScript versions, `dotnet tool restore`, the tsbuild watcher), (b) the namespace vs ES-module split, (c) permission keys, which are now fail-closed in 10.6, (d) lookup and cascade configuration, and (e) upgrade breakage across major versions. The cascade, permission and master/detail fixes from the old Troubleshooting page are still conceptually valid. The tooling-related entries there are obsolete.

### Cited Findings
**Setup and tooling**
- Current prerequisites: the .NET 10 SDK (supported in VS 2026 18.8+), Node.js/npm LTS (examples show Node 24.10.0 and npm 11.1.1; versions should be "equal to or higher"), and TypeScript 6.0.3+ recommended. Without Visual Studio, use the CLI with VS Code. — [Getting Started](https://serenity.is/docs/getting_started/README)
- Sergen fails to start → run `dotnet restore` and `dotnet tool restore` in the `.Web` folder, then `dotnet sergen g`. — [Movie step 2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)
- Missing login template ("Template.LoginPanel is not found") → the solution name contains a dot. Avoid dots when creating the project. This entry is old, so verify it on current versions. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- On .NET 9+, build and publish failures → add `BeforeTargets="ResolveProjectStaticWebAssets"` to the `RunTSBuild` target and remove the `GenerateComputedBuildStaticWebAssetsDependsOn` line. — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
- Script errors with decorators → set `experimentalDecorators: true` in tsconfig, because of an esbuild bug. — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
- In development, the tsbuild watcher is started via `StartNodeScripts` in `appsettings.Development.json`, and its output goes to the application log. If changed TypeScript does not appear in the browser, check that log. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)

**Namespace vs ESM confusion**
- Module files without an `import` or `export` fail under `isolatedModules` with a "global script file" error. Fix: add an import or export. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- Modular classes are not global, so `MyProject.Module.XDialog` in the console returns `undefined`. Module code must import what it uses, for example `import { EntityDialog } from "@serenity-is/corelib"`. The `@/` alias maps to `Modules/`. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- Inplace-add buttons cannot find modular dialogs by class name unless the calling class imports them. A workaround was added in 6.2.6. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- Telling the styles apart: namespace files use `namespace X {}` and `Serenity.*` globals and live in `Namespaces/`. ESM files use import/export and live in `Modules/`. Generated server types are in `Modules/ServerTypes`, regenerated by sergen on build. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- In v9, decorator-based registration (`Decorators.registerClass`) is deprecated in favour of `static [Symbol.typeInfo]`. A type cannot use both. — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
- A "use strict" in bundled output causing form-assignment failures in QA is reported in Discussions (2026-04, unanswered). — [Discussions](https://github.com/serenity-is/Serenity/discussions)

**Permissions ("permission denied", page missing from the menu)**
- In **10.6.0, permission checks are fail-closed**. A missing permission key denies access, null or empty keys are not allowed, and denied delete/undelete/save return `EntityNotFound`. Rows that were implicitly public need explicit permission attributes. — [10.6.0 notes](https://serenity.is/blog/2026/10/11/release-notes-10.6.0)
- A page not shown in navigation → the `PageAuthorize` on the page controller must reference a permission the user actually has. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- After changing a row's permission key, errors and no records → the Endpoint has its own `[ServiceAuthorize]` with the old key, which must be updated too. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- A key added to `PermissionKeys` does not show in the permission dialog → `PermissionKeys` only exists for IntelliSense. Permissions must be registered separately. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- Sergen's default permission `Administration:General` is admin-only, so non-admin users will not see generated pages. — [Movie step 2](https://serenity.is/docs/tutorials/movies/02-generating-code-for-movie-table)
- To split insert and update rights, use `InsertPermission`/`UpdatePermission`, which fall back to Modify and then Read. List and Retrieve check only `ReadPermission`. — [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)

**Lookups, cascades and editors**
- To refresh lookup data, call `reloadLookup` with the lookup key. — [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)
- If the second cascaded dropdown is empty, check three things:
  - `CascadeField` must exactly match the client-side property name (`CountryId`, not `CountryID`). Use `nameof()`.
  - `CascadeFrom` must match the first editor's id.
  - The cascade field must be sent with the lookup data via `[LookupInclude]`. Only Id and Name are sent by default.
  — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting); see also [How-to: cascaded editors](https://serenity.is/docs/howto/how_to_setup_cascaded_editors)
- Default values such as "today" go on the **form** declaration (`[DefaultValue("today")]`), not on the row. — [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)

**Master/detail**
- Duplicate rows when updating an in-memory detail editor → remove `[IdProperty]` from the detail dialog, because GridEditorDialog uses a fake `__id`. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- View fields are empty when a detail record is reopened → add `[MinSelectLevel(SelectLevel.List)]`, because only table fields load by default. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)

**Sergen regeneration**
- Generate once, then add new fields to the Row, Columns and Form by hand. When regenerating, Sergen opens a merge (KDiff3) so you can keep your edits. — [FAQ](https://serenity.is/docs/faq/frequently_asked_questions)

**Deployment and migrations**
- "Table not found" after publishing → Serene has a safety check that prevents migrations on an arbitrary database. Adjust it in `RunMigrations`. This is old guidance; verify the location in the current template. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)
- SQL paging "Incorrect syntax near OFFSET" means an old SQL Server is being used with the default 2012+ dialect. This mostly matters for legacy servers. — [Troubleshooting](https://serenity.is/docs/faq/troubleshooting)

**Upgrade breakage (v9 → v10.6)**
- **v9**:
  - jQuery-based scripts (jquery-ui, jspdf, CKEditor and others) were removed from Serenity.Assets. Reinstall them via npm, libman or a CDN, and remove `CKEditorBasePath` from `ScriptInit.ts`.
  - `Serenity.Net.Data` and `Serenity.Net.Entity` were merged into `Serenity.Net.Services`.
  - The Excel library moved from EPPlus to ClosedXML.
  - Request handlers moved to a `RequestHandlers` namespace.
  - Global usings are now automatic.
  — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
- `DataGrid.getPersistanceKey()` disappeared after 9.1+. — [Discussions](https://github.com/serenity-is/Serenity/discussions)
- **v10.6**:
  - ImageSharp was removed, so image uploads need an `IImageProcessor` (Serene registers a SkiaSharp one before `AddUploadStorage()`).
  - `StartsWith`, `EndsWith` and `Contains` now escape wildcards.
  - `QueryWithParams.Params` is read-only.
  - The `params string[] Where` helper was removed.
  - `LoggingRow` user id properties are now `object`.
  - `DeltaLister` is strict by default.
  — [10.6.0 notes](https://serenity.is/blog/2026/10/11/release-notes-10.6.0)
- Also in 10.6, fields with secret-like names and the hash/salt fields of `IPasswordRow` are automatically hidden from select, sort and filter, and blocked on insert and update. A field named like "ApiSecret" will therefore seem to "vanish". — [10.6.0 notes](https://serenity.is/blog/2026/10/11/release-notes-10.6.0)

### Inferences
- The top beginner failure modes for v10 are likely:
  1. Version mismatches (old SDK or Node).
  2. Copying namespace-style snippets from old sources into `Modules/` files.
  3. Forgetting that permissions are needed at three levels: row attributes, the endpoint's `[ServiceAuthorize]`, and the page's `[PageAuthorize]`, all now fail-closed.
  4. Lookup cache and `[LookupInclude]` issues.
  5. Running Sergen again over customized files without merging.
- Best practices:
  - Pin and record your Serenity version.
  - Read the release notes before every upgrade, one minor version at a time.
  - Use `nameof()` for cascade and field references.
  - Keep generated code under source control so Sergen diffs are visible.
  - Prefer `Symbol.typeInfo` over decorators in new code.
  - Never edit `Modules/ServerTypes` by hand, because it is regenerated.

### Gaps
- I found no official, current (v8+) troubleshooting page. The existing one mixes valid concepts with obsolete tooling advice.
- The "lookup not refreshing after save" scenario: the FAQ gives `reloadLookup`. I did not verify the current server-side cache invalidation behavior (for example, `[LookupScript]` expiration or `TwoLevelCache` group invalidation) against a current source.

## 4. Prior knowledge needed, and the minimal level

### Takeaway
The docs list tool prerequisites (.NET 10 SDK, Node LTS, TypeScript 6, VS 2026 or VS Code) but **do not state required skill levels**. The skill levels below are inferred from what the tutorials and framework expect.

### Cited Findings
- Tool prerequisites: .NET 10 SDK, Node 24.x/npm 11.x or newer, TypeScript 6.0.3+, and Visual Studio 2026 (18.8+) or the CLI with VS Code. — [Getting Started](https://serenity.is/docs/getting_started/README)
- The stack is ASP.NET Core MVC with TypeScript, and the framework covers dependency injection, configuration, entities, Fluent SQL, service endpoints, request handlers, validation, dynamic scripts and script bundling. — [Serenity README](https://github.com/serenity-is/Serenity); [docs index](https://serenity.is/docs/)
- The front end now uses ES modules with imports from `@serenity-is/corelib`, built by esbuild through tsbuild. — [ES Modules post](https://serenity.is/blog/2022/10/15/switching-to-es-modules)
- Since v9 the front end is jQuery-free, and since 2026 JSX via DomWise is used for templates. — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0); [DomWise post](https://serenity.is/blog/2026/07/24/domwise)
- Data access relies on SQL tables and migrations, and the tutorials start by creating tables (Movie, Department, Tenants). — [docs index](https://serenity.is/docs/)

### Inferences (minimal level, not stated by official sources)
- **C#**: solid intermediate level. You need classes, attributes, generics, LINQ basics, async, nullable types, partial classes, and `nameof`. Attributes are central, because Serenity is attribute-driven (Rows, Forms, Columns, permissions).
- **ASP.NET Core**: basic level. You need the middleware pipeline, dependency injection, `appsettings`, controllers and Razor views, and authentication concepts.
- **SQL**: comfortable level. You need table design, primary and foreign keys, joins and views. Reading generated SQL helps with debugging. Knowing FluentMigrator-style migrations is a plus.
- **TypeScript/JS**: basic to intermediate. You need ES module import/export, classes and inheritance (overriding grid and dialog methods), npm, and basic JSX. jQuery is no longer needed.
- **Tooling**: the dotnet CLI, npm, git, and browser devtools.

### Gaps
- There is no official "who is this for" or skills-prerequisite statement in the docs.

## 5. Recommended learning path and practice projects

### Takeaway
The best path is built from the official assets: Serene tour → Movie tutorial → Courses tutorial → reading the Northwind and BasicSamples source → Multi-tenancy tutorial → release notes and framework internals. Each phase should end with a self-built mini-project of increasing complexity.

### Cited Findings (assets used for each phase)
- Serene tour (Northwind, BasicSamples, localization, users and roles); tutorials Movie (13 steps), Courses (9 steps) and Multi-Tenancy (14 steps); how-tos (removing sample modules, script bundling, cascaded editors). — [docs index](https://serenity.is/docs/)
- Framework topics for the intermediate and advanced phases: Authorization, Localization, Caching, Entities, Fluent SQL, Request Handlers, Validation, Reporting, Uploads, Dynamic Scripts, Feature Toggles, Grids, Editors, Custom Editors, EntityDialog, and type-safe service calls. — [docs index](https://serenity.is/docs/)
- Advanced v9/v10 features to explore:
  - inline grid editing (`GridEditController`), the `[EditLink]` attribute, and interceptors for unit testing — [9.0.0 notes](https://serenity.is/blog/2025/09/16/release-notes-9.0.0)
  - optimistic concurrency, projections, `IncludeMore`, and server-side page size limits — [10.6.0 notes](https://serenity.is/blog/2026/10/11/release-notes-10.6.0)
  - DomWise JSX — [DomWise post](https://serenity.is/blog/2026/07/24/domwise)

### Inferences (suggested path, which is my synthesis)
**Phase 0, Foundations (about 1 week if needed)**
- Brush up on C# attributes and LINQ, an ASP.NET Core DI and config refresher, SQL joins, and TS modules and classes.
- Install the .NET 10 SDK and Node LTS, create a Serene app with `dotnet new serene`, log in, and explore every menu.

**Phase 1, Beginner (1-2 weeks)**
- Do the Serene tour and the Movie tutorial end to end.
- Learn the Row/Columns/Form/Endpoint/Grid/Dialog file set that Sergen produces.
- *Mini-project A, "Personal Library":* Book, Author and Genre tables. Write the migration yourself, run `dotnet sergen g`, add lookups (Author on Book), quick filters, column formatting, and a "today" default on the form.
- *Exercise:* create a non-admin role, set custom permission keys on row, endpoint and page, and verify the user sees only what they are allowed to (this matters because of the 10.6 fail-closed behavior).

**Phase 2, Intermediate (2-4 weeks)**
- Do the Courses tutorial, and read `common-features` Northwind (Order and OrderDetail master/detail) and BasicSamples at your version tag.
- *Mini-project B, "Inventory and Orders":* Customer, Product, Category, Order and OrderDetail, with an in-memory detail grid editor, cascaded editors (Country → City), calculated totals, Excel export (ClosedXML since v9), and file and image uploads (an `IImageProcessor` is required in 10.6).
- *Exercises:*
  - a custom editor
  - a custom request handler behavior, such as auditing
  - localization of texts
  - a lookup that refreshes after save (`reloadLookup` or cache invalidation)

**Phase 3, Advanced (4+ weeks)**
- Do the Multi-tenancy tutorial.
- Study row behaviors, Fluent SQL, dynamic scripts, script bundling, feature toggles, unit tests with interceptors, inline grid editing, and DomWise JSX components.
- *Mini-project C, "Multi-tenant Helpdesk/CRM":* tenants with per-tenant data isolation, ticket workflow with status and assignee, dashboards, reporting/PDF, optimistic concurrency, row-level permissions, and deployment (publish, connection strings, migrations safety check).
- *Exercise:* upgrade a project across one minor version using only the release notes, and document every breaking change you hit.

**Habits to build throughout**
- Keep the demo (demo.serenity.is) and the `serene/` and `common-features/` source open as a reference.
- Check the date and version of any snippet. Reject namespace-style or Saltarelle-era code for new work.
- Ask questions in GitHub Discussions with your version number and a minimal repro.

### Gaps
- There is no official learning path or time estimate. Phase durations above are rough inferences.
- Which modules are StartSharp-only (premium) versus included in Serene is not fully enumerated in the sources I fetched. Learners on Serene may not have access to some samples shown in the demo.
