# Bài 00a – Khởi tạo một dự án Serenity 3.13.7 (.NET Framework) trong Visual Studio

**Mục tiêu:** tạo được một project Serene bản ASP.NET MVC 5 / .NET Framework, đưa đúng về Serenity **3.13.7**
và chạy được lần đầu (đăng nhập `admin` / `serenity`). Project này dùng để làm bài tập trong cả khóa mà
không đụng tới code của dự án thật.

Tham khảo:
- Guide cũ: [getting_started/](https://github.com/volkanceylan/Serenity-Guide/tree/6d945041646b74c8a95663e940c57508cca29ddc/getting_started)
- Template MVC trong repo Serene: nhánh [`serene-mvc`](https://github.com/serenity-is/Serene/tree/c1c34c0669bd8e2fcb7a0071f1a591c913fd6771)
  (commit "released serene asp.net mvc obsolete version as a separate template", 09/07/2020)
- Bản 3.13.7 để so sánh: [Serene/Serene.Web @ 4769edd](https://github.com/serenity-is/Serene/tree/4769edde155c4bf35f180136dd20dcf03a50310c/Serene/Serene.Web)

---

## 1. Chuyện phiên bản cần biết trước

Từ tháng 7/2020, template **Serene** chính trên Visual Studio Marketplace chỉ còn bản **ASP.NET Core**. Bản
.NET Framework được tách ra thành một extension riêng tên **"Serene (ASP.NET MVC - Obsolete)"**:

| | Serene (chính) | Serene (ASP.NET MVC - Obsolete) |
|---|---|---|
| Marketplace | `VolkanCeylan.SereneSerenityApplicationTemplate` | [`VolkanCeylan.SereneMvc`](https://marketplace.visualstudio.com/items?itemName=VolkanCeylan.SereneMvc) |
| Nền tảng | .NET Core 3.1 trở lên | **.NET Framework 4.5.1, ASP.NET MVC 5** ← cái mình cần |
| Visual Studio cài được | Tùy phiên bản | **2012 → 2019** (manifest ghi `[11.0,17.0)`, tức **không** cài được vào VS 2022) |
| Bản Serenity khi tạo | | **3.13.5** (manifest bản cuối là `3.13.5.12`) |

Không có VSIX nào tạo thẳng ra 3.13.7. Cách làm: tạo bằng template MVC (3.13.5), sau đó **nâng 7 package
Serenity lên 3.13.7**. So sánh nhánh `serene-mvc` với commit 3.13.7 thì phần khác nhau chỉ là số phiên bản
package và các file script do package `Serenity.Scripts` chép vào, nên nâng package là đủ.

## 2. Chuẩn bị máy

| Thành phần | Ghi chú |
|---|---|
| **Visual Studio 2019** (Community là đủ) | Bản ít rắc rối nhất. Tải từ trang *Older downloads* của Visual Studio |
| Workload **ASP.NET and web development** | Có sẵn SQL Server **LocalDB** và công cụ web |
| *Individual components* → **.NET Framework 4.5.1 targeting pack** | Project nhắm `v4.5.1`. Thiếu cái này VS sẽ đòi retarget |
| **Node.js** bản LTS (kèm npm) | Template dùng npm để tải file typings `.d.ts`. Lúc tạo project, wizard sẽ kiểm tra và hỏi cài nếu thiếu |
| Git (tùy chọn) | Để commit project bài tập |

Sau khi cài Node, vào **Tools → Options → Projects and Solutions → Web Package Management → External Web Tools**,
thêm `C:\Program Files\nodejs` và đưa lên **đầu danh sách**. Nếu không, VS có thể dùng bản Node cũ đi kèm.

Kiểm tra trong Command Prompt:

```cmd
node -v
npm -v
sqllocaldb info          :: phải thấy MSSQLLocalDB
```

> **Nếu bắt buộc dùng VS 2022:** VSIX trên không cài được, và VS 2022 không còn kèm targeting pack 4.5.1.
> Cách đơn giản là tạo project bằng VS 2019 (máy khác hoặc máy ảo), rồi mở project đó bằng VS 2022 sau
> khi đã cài targeting pack. Nếu dự án ở công ty đang dùng VS nào, hãy dùng đúng bản đó.

## 3. Cài template và tạo project

1. Tải file `.vsix` từ trang Marketplace **Serene (ASP.NET MVC - Obsolete)** (link ở bảng mục 1), bấm
   **Download**.
2. Đóng Visual Studio, double-click file `.vsix`, chọn VS 2019, bấm **Install**.
3. Mở VS 2019 → **Create a new project** → gõ `Serene` vào ô tìm kiếm → chọn
   **Serene (ASP.NET MVC - Obsolete)**. Đừng chọn nhầm bản "Serene" thường (bản .NET Core).
4. Đặt tên project:
   - Dùng **PascalCase**, ví dụ `MovieLab`, `HocSerenity`. Không dùng chữ thường đầu (`myProject`).
   - **Không** đặt tên `Serenity`, sẽ trùng với các assembly của framework.
   - Tên này sẽ thay cho chữ `Serene` ở mọi nơi: namespace (`MovieLab.MovieDB`), tên project
     (`MovieLab.Web`), tên database (`MovieLab_Default_v1`, `MovieLab_Northwind_v1`).
5. Màn hình **Feature Selection**: lần đầu nên **để tick hết** (Northwind, Basic Samples...) để có code mẫu
   mà các bài học đang tham chiếu. Sau này muốn project gọn thì tạo lại và bỏ tick.
6. Bấm OK và chờ. VS sẽ restore NuGet và chạy `npm install`, mất vài phút ở lần đầu.

## 4. Nâng Serenity lên 3.13.7

Mở **Tools → NuGet Package Manager → Package Manager Console**, chọn *Default project* là `MovieLab.Web`, rồi
chạy:

```powershell
Update-Package Serenity.Core -Version 3.13.7
Update-Package Serenity.Data -Version 3.13.7
Update-Package Serenity.Data.Entity -Version 3.13.7
Update-Package Serenity.Services -Version 3.13.7
Update-Package Serenity.Web -Version 3.13.7
Update-Package Serenity.Scripts -Version 3.13.7
Update-Package Serenity.CodeGenerator -Version 3.13.7
```

- Khi `Serenity.Scripts` hỏi có ghi đè file trong `Scripts/serenity/` không, chọn **A** (Yes to All). Các file
  này là của framework, không phải code của mình.
- **Không** nâng `Serenity.Web.Assets` (giữ `3.9.2`) và `Serenity.Web.Tooling` (giữ `3.9.10`). Bản 3.13.7
  gốc cũng dùng đúng hai phiên bản này.
- **Không** bấm *Update all* trong giao diện NuGet: nó sẽ kéo Serenity lên 3.14 hoặc mới hơn.

Kiểm tra `packages.config`: 7 dòng `Serenity.*` ở trên phải là `version="3.13.7"`.

## 5. Build và chạy lần đầu

1. Chuột phải solution → **Restore NuGet Packages** (cho chắc), rồi **Build → Rebuild Solution**.
2. **Build → Transform All T4 Templates**, rồi build lại một lần nữa. Bước này sinh lại
   `Imports/ServerTypings` và `Imports/ClientTypes` cho khớp với 3.13.7 (bài 01, bài 15).
3. Nhấn **F5**. Lần chạy đầu, app sẽ:
   - tạo database `MovieLab_Default_v1` và `MovieLab_Northwind_v1` trong LocalDB (`EnsureDatabase`),
   - chạy toàn bộ migration để tạo bảng và dữ liệu mẫu (bài 02).
4. Đăng nhập bằng **`admin` / `serenity`**. Thấy menu Northwind, Basic Samples là xong.

Mở *SQL Server Object Explorer* (View → SQL Server Object Explorer) → `(localdb)\MSSQLLocalDB` để xem hai
database vừa tạo.

## 6. Lỗi hay gặp khi khởi tạo

| Hiện tượng | Cách xử lý |
|---|---|
| Không tìm thấy template "Serene (ASP.NET MVC - Obsolete)" | VSIX chưa cài vào đúng bản VS, hoặc đang dùng VS 2022. Kiểm tra *Extensions → Manage Extensions → Installed* |
| `A network-related or instance-specific error ... SQL Server` khi F5 | Chưa có LocalDB. Chạy `sqllocaldb create MSSQLLocalDB` rồi `sqllocaldb start MSSQLLocalDB`. Hoặc sửa `Data Source` trong `Web.config` thành `.\SQLEXPRESS` cho **cả** `Default` và `Northwind` |
| `The reference assemblies for .NETFramework,Version=v4.5.1 were not found` | Thiếu targeting pack 4.5.1 (mục 2) |
| Build báo thành công nhưng trang trắng, F12 báo lỗi JS | Lỗi TypeScript bị nuốt vì `ContinueOnError="true"` (bài 15). Xem tab *Error List* chọn *Build + IntelliSense* |
| Lỗi `npm` hoặc thiếu thư mục `node_modules/@types` | Mở Command Prompt tại thư mục `MovieLab.Web`, chạy `npm install`. Kiểm tra lại thứ tự External Web Tools |
| Dialog không mở, báo thiếu form hoặc enum | Chưa chạy *Transform All T4 Templates* |
| `Could not load file or assembly Serenity.*` sau khi nâng version | Có package chưa lên 3.13.7. Mở `packages.config` kiểm tra, rồi *Clean* + *Rebuild* |

## 7. Bước tiếp theo

- Tạo module **MovieDB** theo bài 02 (migration) và bài 03 (Sergen). Lệnh Sergen bản này chạy trong
  *Package Manager Console*: gõ `sergen` (xem `Readme.CodeGenerator.txt` trong project).
- Nên `git init` project bài tập và commit ngay sau khi chạy được lần đầu, để có điểm quay lại khi làm hỏng.

## 8. Bài tập

1. Tạo project `MovieLab` theo các bước trên, nâng lên 3.13.7 và đăng nhập được.
2. So sánh `packages.config` của project mới với project ở công ty. Package nào khác phiên bản?
3. Đổi chuỗi kết nối sang SQL Server/SQL Express trên máy (nếu có), xóa DB cũ và chạy lại để xem migration tự
   tạo DB.
4. Tạo một project thứ hai, lần này **bỏ tick hết** ở Feature Selection, rồi so sánh menu và thư mục
   `Modules/` với project đầu tiên.

## 9. Ghi chú của mình

-
