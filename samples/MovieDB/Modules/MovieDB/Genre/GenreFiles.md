# Các file còn lại của module Genre

Ngoài `GenreRow.cs`, các file Repository, Endpoint, Form, Columns, Page, Index, Grid và Dialog của Genre
**giống hệt mẫu Sergen**. Bạn chỉ cần thay `Movie` bằng `Genre` trong các file tương ứng của thư mục
`../Movie/`, rồi bỏ phần tùy biến. Cũng có thể để Sergen sinh ra (bài 03).

Riêng `GenreDialog.ts` phải tồn tại và có đúng tên `Serene.MovieDB.GenreDialog`. Lý do: `LookupEditor(...,
InplaceAdd = true)` trên `MovieRow.GenreId` tìm dialog theo quy ước `<LookupKey>Dialog`, ở đây là
`MovieDB.Genre` + `Dialog`.

```ts
namespace Serene.MovieDB {

    @Serenity.Decorators.registerClass()
    export class GenreDialog extends Serenity.EntityDialog<GenreRow, any> {
        protected getFormKey() { return GenreForm.formKey; }
        protected getIdProperty() { return GenreRow.idProperty; }
        protected getLocalTextPrefix() { return GenreRow.localTextPrefix; }
        protected getNameProperty() { return GenreRow.nameProperty; }
        protected getService() { return GenreService.baseUrl; }
        protected getDeletePermission() { return GenreRow.deletePermission; }
        protected getInsertPermission() { return GenreRow.insertPermission; }
        protected getUpdatePermission() { return GenreRow.updatePermission; }

        protected form = new GenreForm(this.idPrefix);
    }
}
```
