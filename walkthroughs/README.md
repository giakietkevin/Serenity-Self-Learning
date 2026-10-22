# Phân tích code

Các file trong thư mục này **đọc code thật, có đánh số từng đoạn** rồi giải thích. Bốn file đầu phân tích
code có sẵn trong template Serene 3.13.7. File 05 phân tích bộ code mẫu trong `samples/MovieDB`.

| # | File | Phân tích | Nên đọc sau bài |
|---|---|---|---|
| 01 | [ProductRow.cs](01-phan-tich-ProductRow.md) | Entity đầy đủ: lookup, upload ảnh, 2 khóa ngoại, `Origin`, log thay đổi | 05 |
| 02 | [UserRepository.cs](02-phan-tich-UserRepository.md) | Nghiệp vụ thật trong Save/Delete handler, băm mật khẩu, quyền theo field | 06 |
| 03 | [OrderGrid.ts + OrderDialog.ts](03-phan-tich-OrderGrid-OrderDialog.md) | Override grid và dialog: filter có tham số, nút Excel/PDF, cột icon | 04, 06 |
| 04 | [Order ↔ OrderDetail](04-phan-tich-Order-OrderDetail-master-detail.md) | Cơ chế master–detail: GridEditorBase, MasterDetailRelationBehavior | 08 |
| 05 | [Code mẫu MovieDB](05-phan-tich-code-mau-moviedb.md) | Lần theo 4 tính năng qua mọi file của một module | 08 |

Cách đọc: mở file gốc (link ở đầu mỗi bài) ở một tab bên cạnh, rồi đọc theo số ①②③.
