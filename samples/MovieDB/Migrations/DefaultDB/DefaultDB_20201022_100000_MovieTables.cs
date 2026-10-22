using FluentMigrator;
using System;

namespace Serene.Migrations.DefaultDB
{
    // Bài 02: tạo schema "mov" và 3 bảng Movie, Genre, MovieGenres trong một migration.
    // Key 14 chữ số, trùng với ngày giờ trong tên file.
    [Migration(20201022100000)]
    public class DefaultDB_20201022_100000_MovieTables : Migration
    {
        public override void Up()
        {
            Create.Schema("mov");

            Create.Table("Genre").InSchema("mov")
                .WithColumn("GenreId").AsInt32().Identity().PrimaryKey().NotNullable()
                .WithColumn("Name").AsString(100).NotNullable();

            Create.Table("Movie").InSchema("mov")
                .WithColumn("MovieId").AsInt32().Identity().PrimaryKey().NotNullable()
                .WithColumn("Title").AsString(200).NotNullable()
                .WithColumn("Description").AsString(1000).Nullable()
                .WithColumn("Storyline").AsString(Int32.MaxValue).Nullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("ReleaseDate").AsDateTime().Nullable()
                .WithColumn("Runtime").AsInt32().Nullable()
                .WithColumn("Kind").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("GenreId").AsInt32().Nullable()
                    .ForeignKey("FK_Movie_GenreId", "mov", "Genre", "GenreId")
                .WithColumn("InsertDate").AsDateTime().Nullable()
                .WithColumn("InsertUserId").AsInt32().Nullable();

            // Bảng nối cho quan hệ nhiều–nhiều Movie ↔ Genre (bài 08, phần A)
            Create.Table("MovieGenres").InSchema("mov")
                .WithColumn("MovieGenreId").AsInt32().Identity().PrimaryKey().NotNullable()
                .WithColumn("MovieId").AsInt32().NotNullable()
                    .ForeignKey("FK_MovieGenres_MovieId", "mov", "Movie", "MovieId")
                .WithColumn("GenreId").AsInt32().NotNullable()
                    .ForeignKey("FK_MovieGenres_GenreId", "mov", "Genre", "GenreId");
        }

        public override void Down()
        {
        }
    }
}
