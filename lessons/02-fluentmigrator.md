# Bài 02 – Tạo và sửa bảng bằng FluentMigrator

**Mục tiêu:** tự viết được migration để tạo bảng hoặc thêm cột, biết migration chạy lúc nào, và biết
cách kiểm tra khi nó không chạy.

Từ bài này trở đi, chúng ta làm một ví dụ xuyên suốt là **quản lý phim (module `MovieDB`)**, theo đúng
tutorial Movie trong Serenity Guide cũ. Namespace gốc trong bài là `Serene`. Ở dự án thật, bạn thay bằng
namespace của dự án mình.

Code tham khảo trong Serene 3.13.7:
- [App_Start/SiteInitialization.Migrations.cs](https://github.com/serenity-is/Serene/blob/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/App_Start/SiteInitialization.Migrations.cs)
- [Migrations/DefaultDB](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web/Migrations/DefaultDB)

---

## 1. Migration là gì

Migration là **một class C# mô tả một lần thay đổi cấu trúc database**, ví dụ tạo bảng, thêm cột hay
thêm index. Mỗi migration có một số hiệu (key). Khi migration đã chạy, key của nó được ghi vào bảng
`[dbo].[VersionInfo]`, nên lần sau migration đó sẽ không chạy lại nữa.

Lợi ích: mọi thay đổi DB nằm trong source code, cùng commit với code. Người khác pull về rồi chạy app là
DB của họ cũng được cập nhật, không cần gửi script SQL cho nhau.

> Serenity **không bắt buộc** dùng migration. Nhiều dự án làm việc với DB có sẵn và tạo bảng bằng SQL
> Management Studio. Hãy xem dự án của bạn có thư mục `Migrations/` và có file migration mới gần đây
> không. Nếu có thì team đang dùng migration.

## 2. Migration chạy khi nào

Trong Serene 3.13.7, migration chạy **tự động khi app khởi động** (`Application_Start`, rồi tới
`SiteInitialization`). Đây là đoạn quan trọng trong `SiteInitialization.Migrations.cs`:

```csharp
private static string[] databaseKeys = new[] { "Default", "Northwind" };

private static void RunMigrations(string databaseKey)
{
    var cs = SqlConnections.GetConnectionString(databaseKey);

    // Kiểm tra an toàn: chỉ chạy migration nếu tên DB có dạng "<Namespace>_<Key>_v1"
    if (cs.ConnectionString.IndexOf(typeof(SiteInitialization).Namespace +
            @"_" + databaseKey + "_v1", StringComparison.OrdinalIgnoreCase) < 0)
    {
        SkippedMigrations = true;
        return;
    }

    var runner = new RunnerContext(announcer)
    {
        Database = databaseType,
        Connection = connectionString,
        Task = "migrate:up",
        Namespace = "Serene.Migrations." + databaseKey + "DB",   // ★ chỉ chạy migration trong namespace này
        Timeout = 90
    };
    new TaskExecutor(runner).Execute();
}
```

Ba điều phải nhớ:

1. **Namespace quyết định DB nào.** Migration nằm trong `Serene.Migrations.DefaultDB` sẽ chạy trên
   connection `Default`. Migration nằm trong `Serene.Migrations.NorthwindDB` chạy trên `Northwind`.
2. **Có kiểm tra an toàn theo tên DB.** Nếu connection string trỏ tới DB có tên không chứa
   `Serene_Default_v1` thì migration bị **bỏ qua mà không báo lỗi gì**. Ở dự án thật, team thường đã sửa
   hoặc bỏ đoạn kiểm tra này. Hãy mở file này trong dự án để biết quy tắc của team.
3. Migration chạy **theo thứ tự key tăng dần**, và chỉ những key chưa có trong `VersionInfo` mới chạy.

## 3. Viết migration đầu tiên: bảng Movie

Tạo file `Migrations/DefaultDB/DefaultDB_20201022_100000_MovieTable.cs`:

```csharp
using FluentMigrator;
using System;

namespace Serene.Migrations.DefaultDB
{
    [Migration(20201022100000)]
    public class DefaultDB_20201022_100000_MovieTable : Migration
    {
        public override void Up()
        {
            Create.Schema("mov");

            Create.Table("Movie").InSchema("mov")
                .WithColumn("MovieId").AsInt32().Identity().PrimaryKey().NotNullable()
                .WithColumn("Title").AsString(200).NotNullable()
                .WithColumn("Description").AsString(1000).Nullable()
                .WithColumn("Storyline").AsString(Int32.MaxValue).Nullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("ReleaseDate").AsDateTime().Nullable()
                .WithColumn("Runtime").AsInt32().Nullable();
        }

        public override void Down()
        {
        }
    }
}
```

### Quy tắc đặt tên và key

- Tên file và class: `<DB>_<yyyyMMdd>_<HHmmss>_<MoTa>`, theo thời điểm bạn viết migration.
- Key trong `[Migration(...)]` là cùng chuỗi ngày giờ đó nhưng **bỏ dấu gạch dưới** và **luôn đủ 14 chữ
  số**, ví dụ `20201022100000`. Nếu lẫn key 12 chữ số với key 14 chữ số thì thứ tự chạy sẽ sai.
- Hai người trong team không được dùng trùng key.

### `Migration` và `AutoReversingMigration`

- Kế thừa `Migration` thì phải viết cả `Up()` và `Down()`. `Down()` có thể để trống nếu không cần rollback.
- Kế thừa `AutoReversingMigration` thì chỉ viết `Up()`, FluentMigrator tự suy ra cách đảo ngược cho các
  lệnh đơn giản như `Create.Table` hay `Create.Index`. Serene dùng cách này, ví dụ:

```csharp
[Migration(20160515072600)]
public class DefaultDB_20160515_072600_UserPreferences : AutoReversingMigration
{
    public override void Up()
    {
        this.CreateTableWithId32("UserPreferences", "UserPreferenceId", s => s
            .WithColumn("UserId").AsInt64().NotNullable()
            .WithColumn("PreferenceType").AsString(100).NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Value").AsString(int.MaxValue).Nullable());

        Create.Index("IX_UserPref_UID_PrefType_Name")
            .OnTable("UserPreferences")
            .OnColumn("UserId").Ascending()
            .OnColumn("PreferenceType").Ascending()
            .OnColumn("Name").Ascending()
            .WithOptions().Unique();
    }
}
```

`CreateTableWithId32` là hàm tiện ích trong `Migrations/MigrationUtils.cs` của Serene, giúp tạo bảng có
sẵn cột Id identity và hoạt động được trên nhiều loại DB.

## 4. Các lệnh hay dùng

```csharp
// Thêm cột vào bảng đã có
Alter.Table("Movie").InSchema("mov")
    .AddColumn("Kind").AsInt32().NotNullable().WithDefaultValue(1);

// Thêm cột có khóa ngoại
Alter.Table("Movie").InSchema("mov")
    .AddColumn("GenreId").AsInt32().Nullable()
        .ForeignKey("FK_Movie_GenreId", "mov", "Genre", "GenreId");

// Sửa kiểu cột
Alter.Table("Movie").InSchema("mov")
    .AlterColumn("Title").AsString(400).NotNullable();

// Thêm dữ liệu mẫu
Insert.IntoTable("Genre").InSchema("mov")
    .Row(new { Name = "Action" })
    .Row(new { Name = "Drama" });

// Chạy SQL thuần khi FluentMigrator không hỗ trợ
Execute.Sql("UPDATE mov.Movie SET Kind = 1 WHERE Kind IS NULL");
```

Kiểu dữ liệu: `AsInt32`, `AsInt64`, `AsString(n)`, `AsString(Int32.MaxValue)` (nvarchar(max)),
`AsDateTime`, `AsDecimal(18, 2)`, `AsBoolean`, `AsGuid`, `AsBinary`.

## 5. Quy tắc vàng

> **Đừng bao giờ sửa một migration đã chạy.** Nó sẽ không chạy lại, nên thay đổi của bạn không bao giờ
> tới được DB, và DB của từng người trong team sẽ lệch nhau. Muốn sửa gì thì viết một migration **mới**.

## 6. Kiểm tra migration đã chạy chưa

1. Chạy app (F5).
2. Mở SQL Server Management Studio, hoặc trong Visual Studio vào *View → SQL Server Object Explorer*,
   rồi kết nối tới `(localdb)\MSSqlLocalDB` hoặc server của dự án.
3. Trong DB tương ứng, tìm bảng `mov.Movie`.
4. Chạy `SELECT * FROM dbo.VersionInfo ORDER BY Version DESC`. Dòng trên cùng phải là key migration của bạn.

Nếu migration không chạy:

| Triệu chứng | Nguyên nhân hay gặp |
|---|---|
| Không có bảng, không báo lỗi | Sai namespace (`...Migrations.DefaultDB`), hoặc bị kiểm tra an toàn theo tên DB bỏ qua |
| App báo `Error executing migration` | Lỗi SQL trong migration. Đọc phần SQL được in ra trong thông báo lỗi |
| Migration cũ chạy lại hoặc chạy sai thứ tự | Key không đủ 14 chữ số |
| Sửa migration mà DB không đổi | Migration đã có trong `VersionInfo`. Hãy viết migration mới |

## 7. Bài tập

1. Mở `SiteInitialization.Migrations.cs` trong dự án. Ghi lại: các `databaseKeys`, namespace migration,
   và đoạn kiểm tra an toàn còn hay đã bị bỏ.
2. Tìm migration mới nhất trong dự án, đọc xem nó làm gì. Sau đó mở bảng `VersionInfo` để đối chiếu.
3. Trên một project Serene tạo mới (không phải DB thật của dự án), viết migration tạo bảng `mov.Movie`
   như mục 3. Chạy app và kiểm tra bảng đã được tạo.
4. Viết tiếp một migration **mới** thêm cột `Kind` (int, not null, default 1) vào `mov.Movie`.

## 8. Ghi chú của mình

-
