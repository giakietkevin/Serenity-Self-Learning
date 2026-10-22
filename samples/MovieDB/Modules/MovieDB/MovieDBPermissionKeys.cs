
using Serenity.Extensibility;
using System.ComponentModel;

namespace Serene.MovieDB
{
    // Bài 07: gom mọi permission key của module vào một chỗ.
    // [NestedPermissionKeys] làm các key này tự hiện trong cây quyền ở trang Roles/Users.
    [NestedPermissionKeys]
    [DisplayName("Movie Database")]
    public class PermissionKeys
    {
        [DisplayName("Movies")]
        public class Movie
        {
            public const string View = "MovieDB:Movie:View";

            [Description("Create/Update"), ImplicitPermission(View)]
            public const string Modify = "MovieDB:Movie:Modify";

            [ImplicitPermission(View)]
            public const string Delete = "MovieDB:Movie:Delete";
        }

        [Description("[General]")]
        public const string General = "MovieDB:General";
    }
}
