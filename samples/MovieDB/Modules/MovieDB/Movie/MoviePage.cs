
namespace Serene.MovieDB.Pages
{
    using Serenity;
    using Serenity.Web;
    using System.Web.Mvc;

    // URL: /MovieDB/Movie. PageAuthorize: cần ReadPermission của MovieRow mới vào được trang.
    // Lớp này cũng tên MovieController nhưng nằm trong namespace .Pages nên không trùng với Endpoint.
    [RoutePrefix("MovieDB/Movie"), Route("{action=index}")]
    [PageAuthorize(typeof(Entities.MovieRow))]
    public class MovieController : Controller
    {
        public ActionResult Index()
        {
            return View("~/Modules/MovieDB/Movie/MovieIndex.cshtml");
        }
    }
}
