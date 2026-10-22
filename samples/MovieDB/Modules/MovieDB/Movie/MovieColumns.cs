
namespace Serene.MovieDB.Columns
{
    using Serenity.ComponentModel;
    using System;
    using System.ComponentModel;

    // Bài 04: thứ tự property = thứ tự cột. Attribute ở đây ghi đè attribute của MovieRow cho riêng grid.
    [ColumnsScript("MovieDB.Movie")]
    [BasedOnRow(typeof(Entities.MovieRow), CheckNames = true)]
    public class MovieColumns
    {
        [EditLink, DisplayName("Db.Shared.RecordId"), AlignRight]
        public Int32 MovieId { get; set; }

        [EditLink, Width(300)]
        public String Title { get; set; }

        [Width(80), AlignCenter]
        public Int32 Year { get; set; }

        [DisplayFormat("dd/MM/yyyy")]
        public DateTime ReleaseDate { get; set; }

        [Width(110), QuickFilter]
        public MovieKind Kind { get; set; }

        // Hiện tên (view field), nhưng QuickFilter tự lọc theo GenreId
        [Width(120), QuickFilter]
        public String GenreName { get; set; }

        [DisplayName("Runtime in Minutes"), Width(150), AlignRight]
        public Int32 Runtime { get; set; }

        [Hidden]
        public String Description { get; set; }
    }
}
