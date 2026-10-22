
namespace Serene.MovieDB.Forms
{
    using Serenity.ComponentModel;
    using System;
    using System.Collections.Generic;

    // Bài 04: thứ tự property = thứ tự field trên dialog.
    // Kiểu của property ở đây chỉ để T4 sinh typing. Nhãn, validation, editor được lấy từ MovieRow.
    [FormScript("MovieDB.Movie")]
    [BasedOnRow(typeof(Entities.MovieRow), CheckNames = true)]
    public class MovieForm
    {
        [Tab("General")]
        [Category("Movie")]
        public String Title { get; set; }
        [TextAreaEditor(Rows = 3)]
        public String Description { get; set; }
        [HalfWidth]
        public MovieKind Kind { get; set; }
        [HalfWidth]
        public Int32 GenreId { get; set; }
        public List<Int32> GenreList { get; set; }

        [Category("Release")]
        [HalfWidth]
        public Int32 Year { get; set; }
        [HalfWidth]
        public DateTime ReleaseDate { get; set; }
        [Placeholder("minutes")]
        public Int32 Runtime { get; set; }

        [Tab("Storyline")]
        [TextAreaEditor(Rows = 10)]
        public String Storyline { get; set; }
    }
}
