namespace Serene.MovieDB {

    // MovieRow, MovieService, MovieKind, ... ở đây là typing do T4 (ServerTypings.tt) sinh từ C#.
    // Sửa C# xong phải: build → Transform All T4 Templates → build.
    @Serenity.Decorators.registerClass()
    export class MovieGrid extends Serenity.EntityGrid<MovieRow, any> {
        protected getColumnsKey() { return 'MovieDB.Movie'; }      // ↔ [ColumnsScript("MovieDB.Movie")]
        protected getDialogType() { return MovieDialog; }           // dialog mở khi bấm EditLink / New
        protected getIdProperty() { return MovieRow.idProperty; }
        protected getInsertPermission() { return MovieRow.insertPermission; }
        protected getLocalTextPrefix() { return MovieRow.localTextPrefix; }
        protected getService() { return MovieService.baseUrl; }     // ↔ /Services/MovieDB/Movie

        constructor(container: JQuery) {
            super(container);
        }

        // Bài 04: dropdown chọn field cho ô tìm nhanh.
        // Dùng MovieRow.Fields thay vì gõ chuỗi để tránh sai tên field.
        protected getQuickSearchFields(): Serenity.QuickSearchField[] {
            let fld = MovieRow.Fields;
            let txt = (s: string) => Q.text("Db." + MovieRow.localTextPrefix + "." + s).toLowerCase();
            return [
                { name: "", title: "all" },
                { name: fld.Title, title: txt(fld.Title) },
                { name: fld.Description, title: txt(fld.Description) },
                { name: fld.Year, title: txt(fld.Year) }
            ];
        }

        // Bài 04: sắp xếp mặc định theo năm giảm dần (thay vì theo tên)
        protected getDefaultSortBy() {
            return [MovieRow.Fields.Year + " DESC"];
        }

        // Thêm một nút trên toolbar
        protected getButtons(): Serenity.ToolButton[] {
            var buttons = super.getButtons();

            buttons.push({
                title: 'Refresh',
                cssClass: 'refresh-button',
                onClick: () => this.refresh()
            });

            return buttons;
        }
    }
}
