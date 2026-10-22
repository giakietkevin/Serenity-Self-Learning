namespace Serene.MovieDB {

    @Serenity.Decorators.registerClass()
    export class MovieDialog extends Serenity.EntityDialog<MovieRow, any> {
        protected getFormKey() { return MovieForm.formKey; }        // ↔ [FormScript("MovieDB.Movie")]
        protected getIdProperty() { return MovieRow.idProperty; }
        protected getLocalTextPrefix() { return MovieRow.localTextPrefix; }
        protected getNameProperty() { return MovieRow.nameProperty; } // Title → tiêu đề dialog "Edit Movie (Inception)"
        protected getService() { return MovieService.baseUrl; }
        protected getDeletePermission() { return MovieRow.deletePermission; }
        protected getInsertPermission() { return MovieRow.insertPermission; }
        protected getUpdatePermission() { return MovieRow.updatePermission; }

        // form.Title, form.Year, ... là editor đã có kiểu. MovieForm (TS) do T4 sinh từ MovieForm.cs
        protected form = new MovieForm(this.idPrefix);

        constructor() {
            super();

            // Khi chọn ngày phát hành thì tự điền năm (nếu ô năm đang trống)
            this.form.ReleaseDate.change(e => {
                var date = Q.parseISODateTime(this.form.ReleaseDate.value);
                if (date && this.form.Year.value == null)
                    this.form.Year.value = date.getFullYear();
            });

            // Kiểm tra ngay trên client cho nhanh. Server vẫn kiểm tra lại trong MovieRepository.
            this.form.Runtime.addValidationRule(this.uniqueName, e => {
                var runtime = this.form.Runtime.value;
                if (runtime != null && (runtime <= 0 || runtime > 1000))
                    return "Runtime must be between 1 and 1000 minutes!";
            });
        }

        // Chạy sau khi dialog đã nạp xong dữ liệu (thêm mới hoặc sửa)
        protected afterLoadEntity() {
            super.afterLoadEntity();

            // Ví dụ: không cho đổi loại phim (Kind) sau khi đã tạo
            Serenity.EditorUtils.setReadOnly(this.form.Kind, this.isEditMode());
        }
    }
}
