using Serenity.ComponentModel;
using System.ComponentModel;

namespace Serene.MovieDB
{
    // Bài 05: enum cho danh sách cố định. DB lưu số nguyên (1, 2, 3).
    // [EnumKey] là tên enum ở phía client. Nhớ chạy T4 sau khi thêm hoặc sửa enum.
    [EnumKey("MovieDB.MovieKind")]
    public enum MovieKind
    {
        [Description("Film")]
        Film = 1,
        [Description("TV Series")]
        TvSeries = 2,
        [Description("Mini Series")]
        MiniSeries = 3
    }
}
