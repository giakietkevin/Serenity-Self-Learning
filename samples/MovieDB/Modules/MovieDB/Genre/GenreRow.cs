
namespace Serene.MovieDB.Entities
{
    using Serenity.ComponentModel;
    using Serenity.Data;
    using Serenity.Data.Mapping;
    using System;
    using System.ComponentModel;

    [ConnectionKey("Default"), Module("MovieDB"), TableName("[mov].[Genre]")]
    [DisplayName("Genres"), InstanceName("Genre")]
    [ReadPermission(PermissionKeys.Movie.View)]
    [ModifyPermission(PermissionKeys.Movie.Modify)]
    [LookupScript("MovieDB.Genre")]          // Bài 05: đưa danh sách Genre xuống client để làm dropdown
    public sealed class GenreRow : Row, IIdRow, INameRow
    {
        [DisplayName("Genre Id"), Identity]
        public Int32? GenreId
        {
            get { return Fields.GenreId[this]; }
            set { Fields.GenreId[this] = value; }
        }

        [DisplayName("Name"), Size(100), NotNull, QuickSearch]
        public String Name
        {
            get { return Fields.Name[this]; }
            set { Fields.Name[this] = value; }
        }

        IIdField IIdRow.IdField
        {
            get { return Fields.GenreId; }
        }

        StringField INameRow.NameField
        {
            get { return Fields.Name; }
        }

        public static readonly RowFields Fields = new RowFields().Init();

        public GenreRow()
            : base(Fields)
        {
        }

        public class RowFields : RowFieldsBase
        {
            public Int32Field GenreId;
            public StringField Name;
        }
    }
}
