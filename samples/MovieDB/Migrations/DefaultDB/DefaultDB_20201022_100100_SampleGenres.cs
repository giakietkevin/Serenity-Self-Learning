using FluentMigrator;

namespace Serene.Migrations.DefaultDB
{
    // Dữ liệu mẫu cho bảng Genre. Migration riêng, chạy sau migration tạo bảng (key lớn hơn).
    [Migration(20201022100100)]
    public class DefaultDB_20201022_100100_SampleGenres : Migration
    {
        public override void Up()
        {
            Insert.IntoTable("Genre").InSchema("mov")
                .Row(new { Name = "Action" })
                .Row(new { Name = "Drama" })
                .Row(new { Name = "Comedy" })
                .Row(new { Name = "Sci-fi" })
                .Row(new { Name = "Fantasy" })
                .Row(new { Name = "Documentary" });
        }

        public override void Down()
        {
        }
    }
}
