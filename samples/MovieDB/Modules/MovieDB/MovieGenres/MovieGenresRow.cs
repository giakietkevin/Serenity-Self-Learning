
namespace Serene.MovieDB.Entities
{
    using Serenity.ComponentModel;
    using Serenity.Data;
    using Serenity.Data.Mapping;
    using System;
    using System.ComponentModel;

    // Bài 08, phần A: Row của bảng nối. Chỉ cần Row, không cần Repository/Endpoint/UI,
    // vì LinkingSetRelationBehavior tự đọc/ghi bảng này khi lưu MovieRow.
    [ConnectionKey("Default"), Module("MovieDB"), TableName("[mov].[MovieGenres]")]
    [DisplayName("Movie Genres"), InstanceName("Movie Genre")]
    [ReadPermission(PermissionKeys.Movie.View)]
    [ModifyPermission(PermissionKeys.Movie.Modify)]
    public sealed class MovieGenresRow : Row, IIdRow
    {
        [DisplayName("Movie Genre Id"), Identity]
        public Int32? MovieGenreId
        {
            get { return Fields.MovieGenreId[this]; }
            set { Fields.MovieGenreId[this] = value; }
        }

        [DisplayName("Movie"), NotNull, ForeignKey(typeof(MovieRow)), LeftJoin("m")]
        public Int32? MovieId
        {
            get { return Fields.MovieId[this]; }
            set { Fields.MovieId[this] = value; }
        }

        [DisplayName("Genre"), NotNull, ForeignKey(typeof(GenreRow)), LeftJoin("g")]
        public Int32? GenreId
        {
            get { return Fields.GenreId[this]; }
            set { Fields.GenreId[this] = value; }
        }

        IIdField IIdRow.IdField
        {
            get { return Fields.MovieGenreId; }
        }

        public static readonly RowFields Fields = new RowFields().Init();

        public MovieGenresRow()
            : base(Fields)
        {
        }

        public class RowFields : RowFieldsBase
        {
            public Int32Field MovieGenreId;
            public Int32Field MovieId;
            public Int32Field GenreId;
        }
    }
}
