
namespace Serene.MovieDB.Entities
{
    using Serenity.ComponentModel;
    using Serenity.Data;
    using Serenity.Data.Mapping;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    // ── Cấp class ─────────────────────────────────────────────────────────────
    // ConnectionKey  : tên connection string trong Web.config
    // Module         : tên module → LocalTextPrefix "MovieDB.Movie", URL /MovieDB/Movie
    // TableName      : bảng thật trong DB (có schema)
    // DisplayName    : tên số nhiều → tiêu đề trang
    // InstanceName   : tên số ít → "New Movie", "Edit Movie"
    // Read/Modify/DeletePermission : bài 07
    [ConnectionKey("Default"), Module("MovieDB"), TableName("[mov].[Movie]")]
    [DisplayName("Movies"), InstanceName("Movie")]
    [ReadPermission(PermissionKeys.Movie.View)]
    [ModifyPermission(PermissionKeys.Movie.Modify)]
    [DeletePermission(PermissionKeys.Movie.Delete)]
    public sealed class MovieRow : Row, IIdRow, INameRow
    {
        // ── Field của bảng Movie ─────────────────────────────────────────────
        [DisplayName("Movie Id"), Identity]
        public Int32? MovieId
        {
            get { return Fields.MovieId[this]; }
            set { Fields.MovieId[this] = value; }
        }

        [DisplayName("Title"), Size(200), NotNull, QuickSearch]
        public String Title
        {
            get { return Fields.Title[this]; }
            set { Fields.Title[this] = value; }
        }

        [DisplayName("Description"), Size(1000), QuickSearch]
        public String Description
        {
            get { return Fields.Description[this]; }
            set { Fields.Description[this] = value; }
        }

        [DisplayName("Storyline"), QuickSearch]
        public String Storyline
        {
            get { return Fields.Storyline[this]; }
            set { Fields.Storyline[this] = value; }
        }

        // Tìm nhanh theo năm: chỉ khớp chính xác và chỉ khi gõ số
        [DisplayName("Year"), QuickSearch(SearchType.Equals, numericOnly: 1)]
        public Int32? Year
        {
            get { return Fields.Year[this]; }
            set { Fields.Year[this] = value; }
        }

        [DisplayName("Release Date")]
        public DateTime? ReleaseDate
        {
            get { return Fields.ReleaseDate[this]; }
            set { Fields.ReleaseDate[this] = value; }
        }

        [DisplayName("Runtime (mins)")]
        public Int32? Runtime
        {
            get { return Fields.Runtime[this]; }
            set { Fields.Runtime[this] = value; }
        }

        // Enum: property kiểu MovieKind?, field kiểu Int32Field → phải ép kiểu
        [DisplayName("Kind"), NotNull, DefaultValue(MovieKind.Film)]
        public MovieKind? Kind
        {
            get { return (MovieKind?)Fields.Kind[this]; }
            set { Fields.Kind[this] = (Int32?)value; }
        }

        // Khóa ngoại + join + dropdown (bài 05)
        [DisplayName("Genre"), ForeignKey(typeof(GenreRow)), LeftJoin("g")]
        [LookupEditor(typeof(GenreRow), InplaceAdd = true)]
        public Int32? GenreId
        {
            get { return Fields.GenreId[this]; }
            set { Fields.GenreId[this] = value; }
        }

        // Không cho người dùng nhập. Repository tự gán (bài 06)
        [DisplayName("Insert Date"), Insertable(false), Updatable(false)]
        public DateTime? InsertDate
        {
            get { return Fields.InsertDate[this]; }
            set { Fields.InsertDate[this] = value; }
        }

        [DisplayName("Insert User Id"), Insertable(false), Updatable(false)]
        public Int32? InsertUserId
        {
            get { return Fields.InsertUserId[this]; }
            set { Fields.InsertUserId[this] = value; }
        }

        // ── View field: lấy từ bảng Genre qua alias "g" ──────────────────────
        // Expression("g.Name") và Origin("g") tương đương nhau. Origin tự lấy cột cùng tên ("Name")
        // khi property tên là <alias-prefix><Cột>. Ở đây dùng Expression cho dễ đọc.
        [DisplayName("Genre"), Expression("g.Name")]
        public String GenreName
        {
            get { return Fields.GenreName[this]; }
            set { Fields.GenreName[this] = value; }
        }

        // ── Field không có trong bảng (NotMapped) ────────────────────────────
        // Bài 08 A: nhiều–nhiều qua bảng nối MovieGenres
        [DisplayName("Genres"), NotMapped]
        [LookupEditor(typeof(GenreRow), Multiple = true)]
        [LinkingSetRelation(typeof(MovieGenresRow), "MovieId", "GenreId")]
        public List<Int32> GenreList
        {
            get { return Fields.GenreList[this]; }
            set { Fields.GenreList[this] = value; }
        }

        // ── Interface: cho framework biết khóa và "tên" của bản ghi ──────────
        IIdField IIdRow.IdField
        {
            get { return Fields.MovieId; }
        }

        StringField INameRow.NameField
        {
            get { return Fields.Title; }
        }

        // ── Phần bắt buộc của mọi Row trong 3.x ──────────────────────────────
        public static readonly RowFields Fields = new RowFields().Init();

        public MovieRow()
            : base(Fields)
        {
        }

        // Mỗi property ở trên PHẢI có một field tương ứng ở đây, cùng tên.
        public class RowFields : RowFieldsBase
        {
            public Int32Field MovieId;
            public StringField Title;
            public StringField Description;
            public StringField Storyline;
            public Int32Field Year;
            public DateTimeField ReleaseDate;
            public Int32Field Runtime;
            public Int32Field Kind;
            public Int32Field GenreId;
            public DateTimeField InsertDate;
            public Int32Field InsertUserId;

            public StringField GenreName;

            public ListField<Int32> GenreList;
        }
    }
}
