
namespace Serene.MovieDB.Endpoints
{
    using Serenity;
    using Serenity.Data;
    using Serenity.Services;
    using System.Data;
    using System.Web.Mvc;
    using MyRepository = Repositories.MovieRepository;
    using MyRow = Entities.MovieRow;

    // URL: /Services/MovieDB/Movie/{Create|Update|Delete|Retrieve|List}
    // ConnectionKey(typeof(MyRow)): framework mở connection "Default" rồi truyền vào uow/connection.
    // ServiceAuthorize(typeof(MyRow)): cần ReadPermission của MovieRow để gọi bất kỳ action nào.
    [RoutePrefix("Services/MovieDB/Movie"), Route("{action}")]
    [ConnectionKey(typeof(MyRow)), ServiceAuthorize(typeof(MyRow))]
    public class MovieController : ServiceEndpoint
    {
        [HttpPost, AuthorizeCreate(typeof(MyRow))]   // cần Insert/ModifyPermission
        public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request)
        {
            return new MyRepository().Create(uow, request);
        }

        [HttpPost, AuthorizeUpdate(typeof(MyRow))]   // cần Update/ModifyPermission
        public SaveResponse Update(IUnitOfWork uow, SaveRequest<MyRow> request)
        {
            return new MyRepository().Update(uow, request);
        }

        [HttpPost, AuthorizeDelete(typeof(MyRow))]   // cần DeletePermission
        public DeleteResponse Delete(IUnitOfWork uow, DeleteRequest request)
        {
            return new MyRepository().Delete(uow, request);
        }

        [HttpPost]
        public RetrieveResponse<MyRow> Retrieve(IDbConnection connection, RetrieveRequest request)
        {
            return new MyRepository().Retrieve(connection, request);
        }

        [HttpPost]
        public ListResponse<MyRow> List(IDbConnection connection, ListRequest request)
        {
            return new MyRepository().List(connection, request);
        }
    }
}
