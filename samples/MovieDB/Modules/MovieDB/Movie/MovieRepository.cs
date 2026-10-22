

namespace Serene.MovieDB.Repositories
{
    using Serenity;
    using Serenity.Data;
    using Serenity.Services;
    using System;
    using System.Data;
    using MyRow = Entities.MovieRow;

    public class MovieRepository
    {
        private static MyRow.RowFields fld { get { return MyRow.Fields; } }

        // ── Các hàm public: Endpoint gọi vào đây. Phần này giữ nguyên như Sergen sinh ──
        public SaveResponse Create(IUnitOfWork uow, SaveRequest<MyRow> request)
        {
            return new MySaveHandler().Process(uow, request, SaveRequestType.Create);
        }

        public SaveResponse Update(IUnitOfWork uow, SaveRequest<MyRow> request)
        {
            return new MySaveHandler().Process(uow, request, SaveRequestType.Update);
        }

        public DeleteResponse Delete(IUnitOfWork uow, DeleteRequest request)
        {
            return new MyDeleteHandler().Process(uow, request);
        }

        public RetrieveResponse<MyRow> Retrieve(IDbConnection connection, RetrieveRequest request)
        {
            return new MyRetrieveHandler().Process(connection, request);
        }

        public ListResponse<MyRow> List(IDbConnection connection, ListRequest request)
        {
            return new MyListHandler().Process(connection, request);
        }

        // ── Nghiệp vụ nằm trong các handler bên dưới (bài 06) ──────────────────────
        private class MySaveHandler : SaveRequestHandler<MyRow>
        {
            // ① Kiểm tra dữ liệu. Ném ValidationError thì transaction rollback.
            protected override void ValidateRequest()
            {
                base.ValidateRequest();

                if (Row.Year != null &&
                    (Row.Year < 1888 || Row.Year > DateTime.Today.Year + 5))
                {
                    throw new ValidationError("InvalidYear", "Year",
                        "Năm phải nằm trong khoảng 1888 đến " + (DateTime.Today.Year + 5));
                }

                if (Row.ReleaseDate != null && Row.Year != null &&
                    Row.ReleaseDate.Value.Year != Row.Year.Value)
                {
                    throw new ValidationError("YearMismatch", "Year",
                        "Năm không khớp với ngày phát hành!");
                }

                // Chỉ kiểm tra trùng khi thêm mới hoặc khi tên thật sự bị đổi.
                // Row.IsAssigned: client có gửi field Title lên hay không.
                if (Row.IsAssigned(fld.Title) && (IsCreate || Row.Title != Old.Title))
                {
                    var title = Row.Title.TrimToNull();
                    var criteria = new Criteria(fld.Title) == title;
                    if (IsUpdate)
                        criteria &= fld.MovieId != Row.MovieId.Value;

                    if (Connection.Exists<MyRow>(criteria))
                        throw new ValidationError("UniqueViolation", "Title",
                            "Đã có phim với tên này!");
                }
            }

            // ② Gán các field mà người dùng không nhập.
            protected override void SetInternalFields()
            {
                base.SetInternalFields();

                Row.Title = Row.Title.TrimToNull();

                if (IsCreate)
                {
                    Row.InsertDate = DateTime.Now;
                    Row.InsertUserId = Convert.ToInt32(Authorization.UserId);
                }
            }
        }

        private class MyDeleteHandler : DeleteRequestHandler<MyRow>
        {
            protected override void OnBeforeDelete()
            {
                base.OnBeforeDelete();
                // Không cần xóa tay MovieGenres: LinkingSetRelationBehavior đã lo phần đó.
            }
        }

        private class MyRetrieveHandler : RetrieveRequestHandler<MyRow> { }

        private class MyListHandler : ListRequestHandler<MyRow>
        {
            // Lọc dữ liệu theo quyền: ai không có quyền General chỉ thấy phim lẻ (Film)
            protected override void ApplyFilters(SqlQuery query)
            {
                base.ApplyFilters(query);

                if (!Authorization.HasPermission(PermissionKeys.General))
                    query.Where(fld.Kind == (int)MovieKind.Film);
            }
        }
    }
}
