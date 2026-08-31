# Kịch bản kiểm thử — LibraryConnect

Phụ lục nghiệm thu. Mỗi dòng là một kịch bản kiểm thử được thực hiện trên hệ thống đã cài đặt.

Cột **Tự động** cho biết kịch bản đó có được bao phủ bởi bộ kiểm thử tự động hay không, và bởi lớp
kiểm thử nào — người nghiệm thu có thể chạy lại bằng `dotnet test` trong thư mục `backend/`.

Cột **Kết quả thực tế** và **Đạt/Không đạt** để trống, dành cho hội đồng nghiệm thu điền khi chạy thật.

Ký hiệu bám theo **Mục 5 phần 2 của E-HSMT**.

---

## Nhóm 2.1 — Kiểm tra cài đặt

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| 2.1.1 | Cài đặt hệ thống | Chép `.env.example` thành `.env`, đặt mật khẩu CSDL, MinIO và khóa JWT, chạy `docker compose up -d` | 6 container ở trạng thái `healthy`; không phải thao tác gì thêm | | | |
| 2.1.2 | Tạo cấu trúc CSDL | Sau bước 2.1.1, chạy `\dn` và đếm bảng trong psql | 10 schema (`sys`, `cat`, `bib`, `acq`, `ser`, `dig`, `rdr`, `cir`, `web`, `ill`), 113 bảng | | | |
| 2.1.3 | Kiểm tra dịch vụ sống | Mở `http://<máy-chủ>/health` | Trả HTTP 200 | Integration — `InstallationTests` | | |
| 2.1.4 | Kiểm tra kết nối phụ trợ | Mở `http://<máy-chủ>/health/ready` | Trả 200, liệt kê `postgresql` và `redis` ở trạng thái `Healthy` | Integration — `InstallationTests` | | |
| 2.1.5 | Bảng mã quyền | Đăng nhập, vào Nhóm người dùng | Nhóm `SYS_ADMIN` có 161 quyền | Integration — `InstallationTests` | | |
| 2.1.6 | Nhóm người dùng mẫu | Màn hình Nhóm người dùng | Đủ 5 nhóm: Quản trị hệ thống, Cán bộ biên mục, Cán bộ bổ sung, Cán bộ lưu thông, Thủ thư | Integration — `InstallationTests` | | |
| 2.1.7 | Tham số hệ thống | Màn hình Tham số hệ thống | Ít nhất 10 nhóm tham số, trên 50 tham số | Integration — `InstallationTests` | | |
| 2.1.8 | Tài khoản quản trị mặc định | Đăng nhập `admin` / `LibraryConnect@2025` | Đăng nhập được và **bị bắt buộc đổi mật khẩu ngay** | Integration — `InstallationTests` | | |
| 2.1.9 | Mã hóa tiếng Việt | Nhập tên thư viện có dấu, lưu, tải lại trang | Hiển thị đúng dấu tiếng Việt; CSDL dùng UTF-8, collation ICU `vi-VN` | | | |
| 2.1.10 | Tra cứu tiếng Việt không dấu | Chạy `SELECT bib.vn_unaccent('Giáo trình Cơ sở dữ liệu');` | Trả `giao trinh co so du lieu` | | | |
| 2.1.11 | Danh mục chuẩn được nạp sẵn | Vào Danh mục → Ngôn ngữ / Nước xuất bản / Khung phân loại | 21 ngôn ngữ ISO 639-2, 24 mã nước MARC, DDC 10 lớp chính và 89 phân lớp, 14 dạng tài liệu, 6 loại bạn đọc, 2 thư viện và 4 kho | Integration — `CatalogTests` | | |

---

## Nhóm 2.3 — Phân quyền và nhật ký

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| 2.3.1 | Chặn truy cập ẩn danh | Gọi `GET /api/admin/users` không kèm token | HTTP 401 kèm thông báo tiếng Việt | Integration — `PermissionAndAuditTests` | | |
| 2.3.2 | Chặn theo quyền | Tạo tài khoản thuộc nhóm *Cán bộ lưu thông*, đăng nhập, gọi `GET /api/admin/users` | HTTP **403**, thông báo "Bạn không có quyền thực hiện chức năng này." kèm mã quyền còn thiếu | Integration — `PermissionAndAuditTests` | | |
| 2.3.3 | Chặn toàn bộ nhóm chức năng | Với tài khoản trên, gọi lần lượt 5 endpoint quản trị hệ thống | Cả 5 đều trả 403 | Integration — `PermissionAndAuditTests` | | |
| 2.3.4 | Cho phép đúng quyền | Với tài khoản quản trị, gọi cùng 5 endpoint | Cả 5 đều trả 200 | Integration — `PermissionAndAuditTests` | | |
| 2.3.5 | Ẩn menu theo quyền | Đăng nhập bằng tài khoản *Cán bộ lưu thông* trên giao diện | Menu Quản trị hệ thống không xuất hiện | Frontend — `menuConfig.test.ts` | | |
| 2.3.6 | Quyền mới có hiệu lực | Cấp thêm quyền `SYSTEM.USER.VIEW` cho nhóm *Cán bộ lưu thông*, cho thành viên đăng nhập lại | Thành viên truy cập được danh sách người dùng | Integration — `PermissionAndAuditTests` | | |
| 2.3.7 | Ghi nhật ký đăng nhập | Đăng nhập sai một lần, rồi đăng nhập đúng | Nhật ký có cả *Đăng nhập thất bại* (kết quả: thất bại) và *Đăng nhập* (thành công) | Integration — `PermissionAndAuditTests` | | |
| 2.3.8 | Ghi nhật ký thêm mới | Tạo một nhóm người dùng | Nhật ký có bản ghi *Thêm mới / Nhóm người dùng*, phần "Giá trị sau" chứa mã nhóm | Integration — `PermissionAndAuditTests` | | |
| 2.3.9 | Ghi nhật ký phân quyền | Sửa danh sách quyền của một nhóm | Nhật ký có bản ghi *Thay đổi phân quyền* trỏ đúng nhóm | Integration — `PermissionAndAuditTests` | | |
| 2.3.10 | Xem diff giá trị cũ/mới | Mở chi tiết một bản ghi *Cập nhật* trong nhật ký | Hiển thị hai cột "Giá trị trước" và "Giá trị sau" dạng JSON | | | |
| 2.3.11 | Không ghi thông tin nhạy cảm | Đặt lại mật khẩu một người dùng, xem bản ghi nhật ký tương ứng | Không có mật khẩu hay chuỗi băm trong nội dung nhật ký | | | |
| 2.3.12 | Cài đặt chế độ ghi nhận | Tắt ghi *Cập nhật* cho một đối tượng, sửa một bản ghi của đối tượng đó | Không sinh bản ghi nhật ký mới cho thao tác đó | | | |
| 2.3.13 | Lưu trữ vĩnh viễn | Xem cột *Thời gian lưu* trong Cài đặt ghi nhận | Mặc định để trống, nghĩa là giữ vĩnh viễn | | | |
| 2.3.14 | Xuất nhật ký | Lọc theo khoảng thời gian rồi bấm Xuất Excel và Xuất PDF | Tải được tệp, nội dung khớp bộ lọc, tiếng Việt hiển thị đúng dấu, PDF in kèm tiêu chí lọc | | | |
| 2.3.15 | Chính sách mật khẩu | Đổi mật khẩu thành `abc` | Bị từ chối, thông báo "Mật khẩu phải có tối thiểu 8 ký tự." hiển thị ngay dưới ô nhập | Unit — `PasswordPolicyTests` | | |
| 2.3.16 | Khóa tài khoản sau N lần sai | Nhập sai mật khẩu đủ số lần cấu hình | Tài khoản bị khóa, thông báo nêu rõ thời điểm hết khóa | | | |

---

## Nhóm 2.6 — Sao lưu và phục hồi

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| 2.6.1 | Sao lưu thủ công | Quản trị hệ thống → Sao lưu → *Sao lưu ngay*, chọn Toàn bộ | Tạo được tệp `.dump`, hiển thị dung lượng và mã kiểm tra SHA-256 | | | |
| 2.6.2 | Tải bản sao lưu về | Bấm *Tải về* trên một bản sao lưu | Tệp tải xuống thành công; nhật ký ghi nhận thao tác *Xuất dữ liệu* | | | |
| 2.6.3 | Bảo vệ thao tác phục hồi | Bấm *Phục hồi*, nhập sai mật khẩu xác nhận | Bị từ chối, thông báo "Mật khẩu xác nhận không đúng.", **không** có thay đổi nào trên CSDL | | | |
| 2.6.4 | Phục hồi thành công | Sao lưu → thêm một bản ghi mới → phục hồi từ bản sao lưu đó → kiểm tra lại | Bản ghi thêm sau thời điểm sao lưu biến mất; dữ liệu trở về đúng trạng thái lúc sao lưu | | | |
| 2.6.5 | Phục hồi nguyên tử | Phục hồi từ một tệp hỏng hoặc không hợp lệ | Thất bại và CSDL **giữ nguyên như trước**, không ở trạng thái nửa vời | | | |
| 2.6.6 | Cảnh báo hai bước | Bấm *Phục hồi* | Bước 1 cảnh báo ghi đè dữ liệu; bước 2 yêu cầu nhập lại mật khẩu | | | |
| 2.6.7 | Sao lưu tự động | Đặt lịch cron, khởi động lại dịch vụ API, xem `/hangfire` | Tác vụ `libraryconnect:auto-backup` xuất hiện đúng lịch đã đặt | | | |
| 2.6.8 | Giữ N bản gần nhất | Đặt *Số bản sao lưu giữ lại* = 3, sao lưu 4 lần | Chỉ còn 3 tệp trên đĩa; lịch sử vẫn hiển thị đủ 4 dòng, dòng cũ nhất báo tệp đã bị xóa | | | |
| 2.6.9 | Cảnh báo khi hết dung lượng | Xem thẻ *Dung lượng bản sao lưu* trên màn hình Sao lưu | Hiển thị dung lượng đã dùng và còn trống; thanh tiến trình chuyển đỏ khi vượt 90% | | | |

---

## Nhóm chức năng — Phân hệ I: Quản trị hệ thống

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| I.1.1 | Danh sách nhóm | Vào Nhóm người dùng, tìm theo từ khóa, lọc theo trạng thái | Kết quả đúng, phân trang phía máy chủ | | | |
| I.1.2 | Thêm nhóm | Thêm nhóm mã `TEST01` | Tạo thành công; trùng mã bị từ chối kèm thông báo rõ nghĩa | | | |
| I.1.3 | Cây quyền tri-state | Mở Phân quyền một nhóm | Cây 3 cấp Module → Chức năng → Hành động; chọn cấp cha tự chọn toàn bộ cấp con; module cấp một phần hiển thị trạng thái nửa | | | |
| I.1.4 | Sao chép quyền | Dùng *Sao chép từ nhóm khác*, chọn chế độ thay thế | Bộ quyền của nhóm đích khớp nhóm nguồn | | | |
| I.1.5 | Gộp quyền | Sao chép ở chế độ gộp | Nhóm đích giữ quyền cũ và có thêm quyền của nhóm nguồn | | | |
| I.1.6 | Quản lý thành viên | Thêm và bớt thành viên hàng loạt | Danh sách cập nhật; số thành viên trên lưới đổi theo | | | |
| I.1.7 | Bảo vệ nhóm hệ thống | Thử xóa nhóm `SYS_ADMIN` | Nút xóa bị vô hiệu hóa; gọi trực tiếp API trả 409 | | | |
| I.1.8 | Bảo vệ nhóm còn thành viên | Xóa một nhóm đang có thành viên | Bị từ chối kèm số thành viên hiện có | | | |
| I.2.1 | Danh sách người dùng | Lọc theo nhóm, đơn vị, trạng thái | Kết quả đúng theo từng bộ lọc và tổ hợp bộ lọc | | | |
| I.2.2 | Thêm người dùng | Thêm tài khoản không nhập mật khẩu | Hệ thống sinh mật khẩu tạm, hiển thị **một lần**, tài khoản bị buộc đổi mật khẩu lần đầu | Unit — `TemporaryPasswordGeneratorTests` | | |
| I.2.3 | Mật khẩu tạm dễ đọc | Xem mật khẩu được sinh | Không chứa ký tự dễ nhầm `0 O o 1 l I`; luôn thỏa mãn chính sách đang cấu hình | Unit — `TemporaryPasswordGeneratorTests` | | |
| I.2.4 | Đặt lại mật khẩu | Đặt lại mật khẩu một tài khoản đang đăng nhập ở máy khác | Máy kia bị đăng xuất ở yêu cầu kế tiếp | | | |
| I.2.5 | Khóa / mở khóa | Khóa một tài khoản | Tài khoản không đăng nhập được; mở khóa thì đăng nhập lại được | | | |
| I.2.6 | Không tự khóa mình | Thử khóa chính tài khoản đang dùng | Bị từ chối kèm thông báo rõ nghĩa | | | |
| I.2.7 | Không xóa quản trị cuối cùng | Thử xóa tài khoản quản trị duy nhất | Bị từ chối | | | |
| I.2.8 | Lịch sử đăng nhập | Mở lịch sử đăng nhập của một tài khoản | Hiển thị cả lần thành công lẫn thất bại, kèm IP và trình duyệt | | | |
| I.2.9 | Tệp mẫu nhập Excel | Bấm *Tải tệp mẫu* | Tệp có sheet dữ liệu và sheet *Hướng dẫn* mô tả từng cột | Unit — `ExcelServiceTests` | | |
| I.2.10 | Kiểm tra tệp trước khi nhập | Chọn tệp có dòng sai, bấm *Kiểm tra tệp* | Liệt kê lỗi theo từng dòng và cột; **không** ghi bản ghi nào vào hệ thống | | | |
| I.2.11 | Nhập người dùng | Bấm *Nhập dữ liệu* với tệp hợp lệ | Tạo đủ tài khoản, hiển thị mật khẩu tạm của từng tài khoản | | | |
| I.2.12 | Nhập tệp lỗi định dạng | Nhập tệp thiếu cột bắt buộc | Báo rõ cột thiếu, không ghi bản ghi nào | | | |
| I.3.1 | Xem tham số | Mở Tham số hệ thống | Tham số nhóm theo chủ đề; mỗi kiểu dữ liệu hiển thị đúng loại điều khiển | | | |
| I.3.2 | Sửa tham số | Đổi tên thư viện rồi lưu | Tên mới hiển thị ngay trên đầu trang quản trị và trên OPAC | | | |
| I.3.3 | Kiểm tra kiểu dữ liệu | Nhập chữ vào một tham số kiểu số | Bị từ chối kèm thông báo nêu rõ kiểu mong đợi | | | |
| I.3.4 | Tham số bí mật | Xem tham số *Mật khẩu SMTP* | Giá trị không được trả về; để trống khi lưu thì giá trị cũ giữ nguyên | Integration — `InstallationTests` | | |
| I.3.5 | Lịch sử tham số | Mở *Lịch sử thay đổi* | Hiển thị giá trị cũ, giá trị mới, người đổi và thời điểm; tham số bí mật chỉ hiện dấu sao | | | |
| I.3.6 | Không hardcode | Đổi tên thư viện thành một tên khác hẳn | Toàn bộ giao diện, chân trang và biểu mẫu in dùng tên mới | | | |

---

## Nhóm chức năng — Danh mục nghiệp vụ

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| DM.1 | Danh sách danh mục | Vào menu Danh mục | Hiển thị đủ 20 danh mục, nhóm theo nghiệp vụ, đánh dấu danh mục phân cấp và danh mục hỗ trợ gộp trùng | Integration — `CatalogTests` | | |
| DM.2 | Thêm giá trị | Thêm một nhà xuất bản kèm địa chỉ và điện thoại | Tạo thành công; các trường riêng của danh mục hiển thị đúng trên lưới | Integration — `CatalogTests` | | |
| DM.3 | Tự sinh mã | Thêm một từ khóa "Cơ sở dữ liệu" và để trống ô mã | Hệ thống sinh mã `CO_SO_DU_LIEU` | Integration — `CatalogTests`; Unit — `CatalogCodeGeneratorTests` | | |
| DM.4 | Trùng mã | Thêm hai giá trị cùng mã | Lần thứ hai bị từ chối, thông báo nêu rõ mã bị trùng | Integration — `CatalogTests` | | |
| DM.5 | Chặn xóa | Xóa một giá trị đang được biểu ghi sử dụng, và xóa một giá trị còn cấp con | Cả hai bị từ chối; thông báo nêu rõ số bản ghi đang dùng hoặc số giá trị con | Integration — `CatalogTests` | | |
| DM.6 | Danh mục phân cấp | Mở Khung phân loại | Cây DDC hai cấp; ô lọc và ô chọn cấp trên đều dạng cây | Integration — `CatalogTests` | | |
| DM.7 | Chặn vòng lặp cây | Sửa một giá trị và chọn chính cấp con của nó làm cấp trên | Bị từ chối kèm thông báo rõ nghĩa; ô chọn cũng đã ẩn nhánh đó | Frontend — `treeUtils.test.ts` | | |
| DM.8 | Tệp mẫu nhập | Bấm Nhập dữ liệu → Tải tệp mẫu | Tệp có sheet dữ liệu và sheet Hướng dẫn mô tả từng cột, kể cả các trường riêng của danh mục | Integration — `CatalogTests` | | |
| DM.9 | Kiểm tra tệp trước khi nhập | Chọn tệp có dòng thiếu tên, bấm Kiểm tra tệp | Liệt kê lỗi theo dòng và cột; không ghi bản ghi nào | | | |
| DM.10 | Nhập cập nhật theo mã | Nhập tệp có mã đã tồn tại | Các dòng đó cập nhật giá trị hiện có thay vì tạo bản ghi trùng | Integration — `CatalogTests` | | |
| DM.11 | Vòng xuất – nhập | Xuất một danh mục ra Excel rồi nhập lại chính tệp đó | Toàn bộ dòng được ghi nhận là cập nhật, không dòng nào lỗi, không tạo bản ghi mới | Integration — `CatalogTests` | | |
| DM.12 | Gộp trùng | Tạo ba cách viết của cùng một tên tác giả, mở Gộp trùng | Ba giá trị vào cùng một nhóm; sau khi gộp chỉ còn một, mọi biểu ghi tham chiếu được chuyển sang giá trị giữ lại | Integration — `CatalogTests`; Unit — `DuplicateDetectionTests` | | |

---

## Nhóm chức năng — Khổ mẫu MARC 21 và trao đổi biểu ghi (mục 2.4)

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| MARC.1 | Bộ định nghĩa trường được nạp sẵn | Vào Khổ mẫu MARC 21 → Định nghĩa trường MARC | 220 trường, tên tiếng Việt; có đủ các trường bắt buộc theo mục 3.1: 020, 022, 040, 041, 044, 082, 084, 100, 110, 111, 130, 245, 246, 250, 260, 264, 300, 310, 336, 337, 338, 490, 500, 504, 505, 520, 650, 653, 700, 710, 773, 852, 856 | Integration — `MarcTests` | | |
| MARC.2 | Chi tiết định nghĩa một trường | Mở rộng dòng trường 245 | Hiện 2 chỉ thị kèm ý nghĩa từng giá trị và danh sách trường con $a, $b, $c, $n, $p… có đánh dấu bắt buộc và lặp lại | Integration — `MarcTests` | | |
| MARC.3 | Trường điều khiển không có chỉ thị và trường con | Mở rộng dòng trường 008 | Được đánh dấu "Trường điều khiển"; không có chỉ thị và trường con | Integration — `MarcTests` | | |
| MARC.4 | Soạn biểu ghi theo đúng cấu trúc MARC | Công cụ biểu ghi MARC → Biểu ghi trống | Có đầu biểu 24 ký tự, trường điều khiển 001 và 008 (đúng 40 ký tự), trường dữ liệu 245 với hai chỉ thị và trường con | Vitest — `marcRecord.test.ts` | | |
| MARC.5 | Gợi ý theo bộ định nghĩa | Gõ "245" hoặc "nhan đề" vào ô Thêm trường | Danh sách gợi ý hiện nhãn trường kèm tên tiếng Việt; chọn xong hệ thống mở sẵn các trường con bắt buộc và điền chỉ thị hợp lệ | Vitest — `marcRecord.test.ts` | | |
| MARC.6 | Chọn chỉ thị theo ý nghĩa | Mở ô chỉ thị 2 của trường 245 | Danh sách hiện "0 — Không bỏ qua ký tự nào" đến "9 — Bỏ qua 9 ký tự" thay vì bắt cán bộ nhớ mã | | | |
| MARC.7 | Dán chuỗi trường con từ hệ thống khác | Dán chuỗi `$aGiáo trình cơ sở dữ liệu : $bdùng cho sinh viên / $cNguyễn Văn Ánh` vào một ô trường con rồi bấm nút tách | Chuỗi được tách thành ba trường con $a, $b, $c đúng nội dung | Vitest — `marcRecord.test.ts` | | |
| MARC.8 | Kiểm tra biểu ghi — lỗi chặn lưu | Xóa trường 245 rồi bấm Kiểm tra biểu ghi | Báo lỗi "Thiếu trường bắt buộc 245", biểu ghi bị đánh dấu không hợp lệ | Integration — `MarcTests`; Unit — `MarcValidatorTests` | | |
| MARC.9 | Kiểm tra biểu ghi — cảnh báo không chặn lưu | Xóa trường 082 rồi bấm Kiểm tra biểu ghi | Cảnh báo "Nên bổ sung trường 082" nhưng biểu ghi vẫn hợp lệ | Integration — `MarcTests`; Unit — `MarcValidatorTests` | | |
| MARC.10 | Chặn trường lặp sai quy định | Thêm trường 245 lần thứ hai rồi kiểm tra | Báo lỗi trường 245 không được lặp lại | Unit — `MarcValidatorTests` | | |
| MARC.11 | Chặn trường con lặp sai quy định | Thêm hai trường con $a vào trường 245 rồi kiểm tra | Báo lỗi trường con $a không được lặp lại | Unit — `MarcValidatorTests` | | |
| MARC.12 | **Xuất ISO 2709** | Soạn biểu ghi tiếng Việt có dấu, bấm Xuất .mrc | Tải về tệp `.mrc`; 5 ký tự đầu là tổng độ dài tệp tính bằng byte; byte cuối là 0x1D | Integration — `MarcTests` | | |
| MARC.13 | **Round-trip ISO 2709 với tiếng Việt** | Bấm Đọc tệp .mrc và chọn chính tệp vừa xuất | Biểu ghi hiện lại **giống hệt** bản gốc: đủ dấu ở nhan đề, tên tác giả, tên nhà xuất bản; chỉ thị và thứ tự trường không đổi | Unit — `Iso2709Tests`; Integration — `MarcTests` | | |
| MARC.14 | Độ dài trường tính theo byte UTF-8 | Mở tệp `.mrc` bằng trình xem hệ mười sáu, đối chiếu mục danh mục của trường 245 | Độ dài ghi trong danh mục bằng số **byte** của trường, lớn hơn số ký tự vì chữ có dấu chiếm 2–3 byte | Unit — `Iso2709Tests` | | |
| MARC.15 | Xuất và nhập lại MARCXML | Bấm Xuất MARCXML rồi Đọc lại chính tệp đó | Tệp dùng không gian tên `http://www.loc.gov/MARC21/slim`, chữ tiếng Việt ở dạng UTF-8 nguyên bản; biểu ghi đọc lại giống bản gốc | Unit — `MarcXmlTests`; Integration — `MarcTests` | | |
| MARC.16 | Hai định dạng mô tả cùng một biểu ghi | Xuất cùng một biểu ghi ra cả `.mrc` và `.xml`, đọc lại cả hai | Hai biểu ghi đọc được giống hệt nhau | Unit — `MarcXmlTests` | | |
| MARC.17 | Tệp nhiều biểu ghi | Đọc một tệp `.mrc` chứa nhiều biểu ghi | Danh sách bên trái liệt kê từng biểu ghi kèm nhan đề và số lỗi; chọn dòng nào thì mở biểu ghi đó | Unit — `Iso2709Tests`; Integration — `MarcTests` | | |
| MARC.18 | Tệp có biểu ghi hỏng | Đọc một tệp có biểu ghi bị cắt cụt ở cuối | Các biểu ghi lành vẫn đọc được; biểu ghi hỏng được báo riêng kèm số thứ tự và vị trí byte | Unit — `Iso2709Tests`; Integration — `MarcTests` | | |
| MARC.19 | Tệp có danh mục sai vị trí | Đọc tệp do phần mềm khác xuất sai vị trí trong danh mục | Hệ thống vẫn khôi phục được biểu ghi bằng cách quét dấu kết thúc trường | Unit — `Iso2709Tests` | | |
| MARC.20 | Biểu ghi MARC-8 từ máy chủ nước ngoài | Đọc tệp có đầu biểu vị trí 09 là khoảng trắng | Dấu phụ được giải mã đúng và chuẩn hóa NFC, ví dụ "ế" là một ký tự | Unit — `Marc8Tests` | | |
| MARC.21 | Tệp UTF-8 nhưng khai sai bảng mã | Đọc tệp UTF-8 có đầu biểu vị trí 09 để trống | Hệ thống tự nhận ra là UTF-8, chữ tiếng Việt không bị hỏng | Unit — `Marc8Tests` | | |
| MARC.22 | Cảnh báo trường vượt giới hạn định dạng | Nhập tóm tắt dài trên 3.400 ký tự tiếng Việt vào trường 520 rồi kiểm tra | Báo lỗi trường vượt 9.999 byte kèm hướng dẫn tách nội dung — phát hiện ngay khi biên mục chứ không đợi đến lúc xuất tệp | Unit — `MarcValidatorTests`, `Iso2709Tests` | | |
| MARC.23 | Khai báo trường dùng riêng của thư viện | Thêm trường 998 vào bộ định nghĩa rồi kiểm tra lại biểu ghi có trường 998 | Trước khi khai báo thì có cảnh báo "chưa có trong bộ định nghĩa"; sau khi khai báo thì hết cảnh báo | Integration — `MarcTests` | | |
| MARC.24 | Không xóa được trường bắt buộc | Xóa định nghĩa trường 245 | Bị từ chối kèm giải thích phải bỏ đánh dấu bắt buộc trước | Integration — `MarcTests` | | |
| MARC.25 | Không khai báo sai loại trường | Khai báo trường 007 kèm trường con | Bị từ chối: trường 001–009 là trường điều khiển, chỉ có giá trị | Integration — `MarcTests` | | |

---

## Nhóm chức năng — Phân hệ II: Biên mục

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| BM.1 | Giá trị ngầm định cho trường MARC (II.1) | Biên mục → Cấu hình biên mục → Giá trị ngầm định | Có sẵn 7 giá trị; ba giá trị lấy từ tham số hệ thống (nguồn biên mục, ngôn ngữ, mã nước) | Integration — `CatalogingTests` | | |
| BM.2 | Đổi tham số thì biểu ghi mới đổi theo | Sửa tham số CATALOG.MARC_040A rồi tạo biểu ghi mới | Trường 040$a của biểu ghi mới mang giá trị vừa sửa, không phải sửa ở hai nơi | | | |
| BM.3 | Mẫu biên mục theo dạng tài liệu (II.5) | Biên mục → Cấu hình biên mục → Mẫu biên mục | Có sẵn 3 mẫu: sách, luận văn, bài trích; mẫu sách là mặc định | Integration — `CatalogingTests` | | |
| BM.4 | Khung biểu ghi mới | Biên mục mới → chọn dạng tài liệu Sách → Bắt đầu biên mục | Khung có đủ trường của mẫu, sắp theo thứ tự nhãn trường; trường 008 đúng 40 ký tự với mã ngôn ngữ ở vị trí 35–37 và mã nước ở 15–17 | Integration — `CatalogingTests` | | |
| BM.5 | **Thêm mới ấn phẩm (II.2)** | Nhập nhan đề, tác giả, xuất bản, mô tả vật lý, ISBN, DDC, chủ đề rồi bấm Lưu (hoặc Ctrl+S) | Lưu được, hệ thống cấp số kiểm soát tự động, hiện thông báo kèm số kiểm soát | Integration — `CatalogingTests` | | |
| BM.6 | Rút dữ liệu tra cứu từ MARC | Mở biểu ghi vừa lưu | Nhan đề, tác giả, NXB, năm, ISBN đã chuẩn hóa, DDC, chủ đề, từ khóa đều đúng; dấu câu ISBD không lọt vào cột hiển thị | Integration — `CatalogingTests`; Unit — `MarcProjectionTests` | | |
| BM.7 | Tự tạo giá trị danh mục khi biên mục | Sau BM.5, vào Danh mục → Tác giả | Tác giả vừa nhập đã có trong danh mục, không phải tạo trước | Integration — `CatalogingTests` | | |
| BM.8 | Không sinh tác giả trùng do cách viết | Lưu một biểu ghi khác với tác giả viết hoa toàn bộ | Vẫn chỉ một bản ghi tác giả trong danh mục | Integration — `CatalogingTests` | | |
| BM.9 | Chỉ số phân loại vào đúng nhánh | Lưu biểu ghi có DDC 005.74, rồi xem cây Khung phân loại | DDC vẫn 10 lớp chính; 005.74 nằm dưới lớp 000 chứ không thành lớp thứ 11 | Integration — `CatalogingTests` | | |
| BM.10 | Chặn lưu biểu ghi thiếu trường bắt buộc | Xóa trường 245 rồi bấm Lưu | Bị từ chối, lỗi hiện ngay dưới trường 245 | Integration — `CatalogingTests` | | |
| BM.11 | Xem chi tiết bốn tab (II.3) | Mở một biểu ghi | Bốn tab: Thông tin thư mục (mô tả ISBD theo vùng), MARC thô, Đăng ký cá biệt, Lịch sử | | | |
| BM.12 | **Lịch sử phiên bản và khôi phục (II.3)** | Sửa nhan đề, lưu kèm ghi chú, mở tab Lịch sử, chọn phiên bản, xem khác biệt rồi bấm Khôi phục | Phiên bản trước còn nguyên kèm người sửa và ghi chú; bảng khác biệt chỉ đúng trường đã đổi; khôi phục đưa nhan đề về như cũ | Integration — `CatalogingTests` | | |
| BM.13 | **Đăng ký cá biệt (II.2)** | Tab Đăng ký cá biệt → Thêm bản sách → số bản 3, chọn kho | Tạo 3 bản, mã vạch và số ĐKCB liền nhau, ký hiệu xếp giá sinh theo quy tắc (ví dụ 005.74 NGU) | Integration — `CatalogingTests` | | |
| BM.14 | Chặn xóa biểu ghi còn ĐKCB | Bấm Xóa biểu ghi đang có bản sách | Bị từ chối kèm số bản còn lại | Integration — `CatalogingTests` | | |
| BM.15 | Xóa biểu ghi bắt buộc nhập lý do | Xóa biểu ghi không còn bản sách, để trống ô lý do | Bị từ chối; nhập lý do thì xóa được và biểu ghi biến mất khỏi danh sách | Integration — `CatalogingTests` | | |
| BM.16 | **Tra cứu tiếng Việt không dấu** | Gõ "giao trinh co so du lieu" và "NGUYEN VAN ANH" vào ô tìm kiếm | Cả hai đều ra kết quả đúng | Integration — `CatalogingTests` | | |
| BM.17 | Hàng đợi biên mục — chờ xử lý (II.4) | Đưa một biểu ghi vào hàng đợi | Việc nằm ở cột "Chờ xử lý", chưa phân công cho ai | Integration — `CatalogQueueTests` | | |
| BM.18 | Phân công và đặt hạn xử lý | Chọn việc → Phân công → chọn cán bộ, ưu tiên 1, hạn 7 ngày | Việc chuyển sang cột "Đang biên mục", hiện tên cán bộ và hạn | Integration — `CatalogQueueTests` | | |
| BM.19 | Quy trình duyệt và trả lại | Nhận việc → Gửi duyệt → Trả lại (bỏ trống lý do) → Trả lại kèm lý do | Trả lại không có lý do bị từ chối; có lý do thì việc sang cột "Bị trả lại" và hiện lý do cho cán bộ biên mục | Integration — `CatalogQueueTests` | | |
| BM.20 | Thống kê năng suất biên mục | Hoàn thành một việc rồi xem bảng Năng suất | Hiện số việc được giao, hoàn thành, bị trả lại và thời gian trung bình theo từng cán bộ | Integration — `CatalogQueueTests` | | |
| BM.21 | **Nhập ISO 2709 — xem trước (II.6)** | Nhập biểu ghi từ tệp → chọn tệp .mrc | Hiện định dạng, số biểu ghi, biểu ghi nào trùng dữ liệu đã có; chưa ghi gì vào CSDL | Integration — `BibImportTests` | | |
| BM.22 | Nhập ISO 2709 — chạy thật | Chọn xử lý trùng "Bỏ qua" rồi bấm Bắt đầu nhập | Thanh tiến trình chạy, kết thúc báo số thành công / bỏ qua / lỗi | Integration — `BibImportTests` | | |
| BM.23 | Nhập lại đúng tệp đó | Nhập lại tệp vừa nhập với cùng tùy chọn | Toàn bộ bị bỏ qua vì trùng — kho không bị nhân đôi | Integration — `BibImportTests` | | |
| BM.24 | Xử lý trùng bằng ghi đè | Sửa nhan đề trong tệp rồi nhập với tùy chọn "Ghi đè" | Biểu ghi được cập nhật; bản trước khi ghi đè vẫn còn trong lịch sử phiên bản | Integration — `BibImportTests` | | |
| BM.25 | Xử lý trùng bằng gộp | Nhập tệp có thêm trường 500 và nhan đề khác, chọn "Gộp" | Nhan đề tại chỗ giữ nguyên; trường 500 mà biểu ghi chưa có được bổ sung | Integration — `BibImportTests` | | |
| BM.26 | Nhật ký lỗi từng dòng | Nhập tệp có một biểu ghi thiếu nhan đề | Các biểu ghi lành vẫn vào; biểu ghi hỏng báo riêng kèm số thứ tự và lý do | Integration — `BibImportTests` | | |
| BM.27 | Nhập tệp MARCXML | Nhập một tệp .xml theo MARC21slim | Nhập được như tệp .mrc | Integration — `BibImportTests` | | |
| BM.28 | **Xuất ISO 2709 theo bộ lọc** | Lọc danh sách rồi bấm "Xuất theo bộ lọc (.mrc)" | Tải về tệp .mrc; đọc lại được đúng các biểu ghi đã lọc | Integration — `BibImportTests` | | |
| BM.29 | Xuất biểu ghi đã tick chọn | Tick vài dòng rồi bấm xuất | Nút đổi thành "Xuất N biểu ghi", tệp chỉ chứa những biểu ghi đã chọn | | | |
| BM.30 | **Nhập Excel — tệp mẫu (II.8)** | Nhập biểu ghi từ Excel → Tải tệp mẫu | Tệp có tiêu đề tiếng Việt và một sheet hướng dẫn giải thích từng cột | Integration — `ExcelImportTests` | | |
| BM.31 | Nhập Excel — đoán ánh xạ | Tải lên tệp làm theo mẫu | Hệ thống đoán sẵn ánh xạ cột sang trường MARC, cán bộ chỉ sửa chỗ sai | Integration — `ExcelImportTests` | | |
| BM.32 | Nhập Excel — một ô nhiều giá trị | Ô đề mục chủ đề ghi "Cơ sở dữ liệu; Tin học; Lập trình", đặt ký tự tách là dấu chấm phẩy | Biểu ghi nhập vào có ba trường 650 riêng | Integration — `ExcelImportTests` | | |
| BM.33 | Nhập Excel — báo lỗi theo dòng | Để trống ô nhan đề ở một dòng | Dòng đó báo lỗi kèm đúng số dòng trong bảng tính; các dòng khác vẫn nhập | Integration — `ExcelImportTests` | | |
| BM.34 | Nhập Excel — lưu hồ sơ ánh xạ | Ánh xạ xong bấm "Lưu hồ sơ ánh xạ" | Lần sau nhận tệp cùng khuôn, chọn lại hồ sơ là xong | Integration — `ExcelImportTests` | | |
| BM.35 | **Danh mục tự tạo (II.9)** | Danh mục tự tạo → Khai báo "Nơi xuất bản" từ 260$a → Quét | Rút được các giá trị duy nhất kèm số biểu ghi dùng mỗi giá trị | Integration — `CustomIndexTests` | | |
| BM.36 | Danh mục tự tạo — dùng làm bộ lọc | Bấm vào số biểu ghi của một giá trị | Danh sách biểu ghi lọc đúng theo giá trị đó | Integration — `CustomIndexTests` | | |
| BM.37 | Danh mục tự tạo — gộp và giữ kết quả gộp | Gộp "TP. HCM" vào "TP. Hồ Chí Minh" rồi bấm Quét lại | Sau khi quét lại, cách viết đã gộp không bị tạo lại; số biểu ghi cộng dồn đúng | Integration — `CustomIndexTests` | | |
| BM.38 | **Mẫu phích kéo thả (II.10)** | Mẫu phích và in phích → Thêm mẫu phích | Khung vẽ đúng tỷ lệ khổ phích, các ô kéo thả được, mỗi ô chọn nguồn nội dung | Integration — `CardPrintTests` | | |
| BM.39 | Chặn ô nằm ngoài khổ phích | Kéo một ô vượt ra ngoài rồi bấm Lưu | Bị từ chối kèm số đo khổ phích | Integration — `CardPrintTests` | | |
| BM.40 | **In phích ra PDF (II.10)** | Chọn cả bốn loại phích rồi bấm Tạo tệp PDF | Tải về tệp PDF hợp lệ; phích chính xếp theo tác giả, phích nhan đề theo nhan đề, mỗi đề mục chủ đề một phích; chữ tiếng Việt đủ dấu | Integration — `CardPrintTests` | | |
| BM.41 | Hai cách xếp giấy | In lần lượt hai chế độ | "Nhiều phích trên A4" xếp lưới để cắt; "mỗi phích một trang" đúng khổ phích để in lên bìa in sẵn | Integration — `CardPrintTests` | | |

---

## Nhóm chức năng — Phân hệ III: Bổ sung và Kho

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| BS.1 | Thêm thư viện / cơ sở | Bổ sung → Quản lý kho → Thư viện → Thêm | Cơ sở mới xuất hiện; đánh dấu trụ sở chính thì cơ sở cũ tự bỏ đánh dấu |
| BS.2 | Thêm kho và giá | Tab Kho → Thêm kho; tab Giá → Thêm giá, đặt hàng 1 cột 1 | Giá hiện trong danh sách và trong bản đồ kho |
| BS.3 | Mã giá trùng trong cùng kho | Thêm giá thứ hai cùng mã trong một kho | Bị chặn kèm thông báo mã đã dùng |
| BS.4 | Mã giá trùng ở kho khác | Thêm giá cùng mã ở kho khác | Lưu được — mã giá chỉ duy nhất trong phạm vi kho |
| BS.5 | Xóa kho còn ấn phẩm | Xóa kho đang chứa sách | Bị chặn kèm số ấn phẩm còn trong kho |
| BS.6 | Bản đồ kho | Chọn kho ở tab Giá và bản đồ kho | Lưới giá theo hàng/cột; mỗi ô hiện số bản trên sức chứa |
| BS.7 | Lập yêu cầu đặt mua | Bổ sung → Yêu cầu đặt mua → Tạo yêu cầu, thêm hai đầu sách | Yêu cầu ở trạng thái Nháp, tổng tiền bằng tổng số lượng nhân đơn giá |
| BS.8 | Cảnh báo tài liệu đã có | Nhập ISBN của sách thư viện đang có rồi lưu | Dòng được đánh dấu trùng, danh sách hiện "n dòng trùng" |
| BS.9 | Tra nhanh không dấu | Bấm nút tra nhanh với nhan đề gõ không dấu | Vẫn tìm ra biểu ghi đã có, ghi rõ khớp theo nhan đề |
| BS.10 | Nhập đề nghị từ Excel | Tải tệp mẫu, điền, nhập lại | Số dòng nhập được báo đúng; dòng thiếu nhan đề báo theo số dòng trong tệp |
| BS.11 | Duyệt sai quy trình | Duyệt một yêu cầu còn ở trạng thái Nháp | Bị chặn — phải gửi duyệt trước |
| BS.12 | Duyệt một phần | Gửi duyệt rồi duyệt 3 trên 4 bản | Yêu cầu chuyển sang Duyệt một phần, giá trị duyệt tính theo số đã duyệt |
| BS.13 | Từ chối yêu cầu | Bấm Từ chối, nhập lý do | Yêu cầu chuyển sang Từ chối, giá trị duyệt về 0, lý do hiện trên màn hình |
| BS.14 | Duyệt nhiều cấp | Đặt `ACQ.APPROVAL_LEVELS` = 2, duyệt lần một | Yêu cầu vẫn ở Chờ duyệt, hiện "đã qua 1/2 cấp" |
| BS.15 | Lập đơn đặt từ yêu cầu | Chọn yêu cầu đã duyệt → Lập đơn đặt | Một đơn cho mỗi nhà cung cấp; dòng đơn mang số đã duyệt, không phải số đề nghị |
| BS.16 | Gộp lại lần hai | Lập đơn lần nữa từ chính yêu cầu đó | Bị chặn — các dòng đã nằm trong đơn trước |
| BS.17 | Ghi nhận giao hàng từng phần | Nhận 2 trên 3 bản | Đơn chuyển sang Nhận một phần |
| BS.18 | Nhập kho khi chưa biên mục | Bấm Nhập kho cho dòng chưa có biểu ghi | Không tạo ĐKCB, hệ thống liệt kê rõ dòng còn thiếu biểu ghi |
| BS.19 | Biên mục sơ lược | Bấm Biên mục sơ lược trên dòng đơn, nhập mười trường | Sinh biểu ghi MARC 21, vào hàng đợi biên mục chi tiết, nối vào dòng đơn |
| BS.20 | Biên mục sơ lược trùng | Biên mục sơ lược một cuốn thư viện đã có | Dùng lại biểu ghi cũ, không tạo biểu ghi thứ hai |
| BS.21 | Tạo ĐKCB từ đơn | Bấm Nhập kho sau khi đã biên mục | Tạo đúng số bản đã nhận, mã vạch và số ĐKCB sinh liên tiếp |
| BS.22 | Bấm nhập kho hai lần | Bấm Nhập kho lần nữa | Bị chặn — đã tạo đủ ĐKCB cho số đã nhận |
| BS.23 | Ấn phẩm mới chờ kiểm nhận | Mở Ấn phẩm trong kho | Bản mới ở trạng thái Chưa kiểm nhận và đang khóa lưu thông |
| BS.24 | Mở khóa khi chưa kiểm nhận | Chọn bản chưa kiểm nhận → Mở khóa | Bỏ qua kèm lý do "chưa kiểm nhận" |
| BS.25 | Kiểm nhận | Chọn các bản → Kiểm nhận, ghi tình trạng Tốt | Chuyển sang Trong kho, mở khóa, ghi tình trạng vật lý |
| BS.26 | Xếp giá hàng loạt | Chọn nhiều bản → Xếp giá, bật sinh lại ký hiệu | Các bản về đúng giá, ký hiệu xếp giá sinh theo quy tắc của kho |
| BS.27 | Khóa lưu thông thiếu lý do | Khóa mà bỏ trống lý do | Bị chặn — phải ghi lý do |
| BS.28 | In tem mã vạch | Chọn vài bản → In tem mã vạch | Tệp PDF đúng khổ tờ tem, mỗi tem có vạch và dãy số dưới vạch |
| BS.29 | In nhãn gáy | Chọn vài bản → In nhãn gáy | Tệp PDF, ký hiệu xếp giá tách thành các dòng trên nhãn |
| BS.30 | In khi chưa chọn gì | Bấm In tem khi chưa chọn bản nào | Báo rõ chưa chọn ấn phẩm nào |
| BS.31 | Mẫu tem vượt khổ giấy | Đặt 6 cột × 50 mm, lề trái 8 mm | Màn hình chặn lưu và chỉ rõ vượt khổ A4 |
| BS.32 | Chuyển kho thiếu lý do | Chuyển kho mà bỏ trống lý do | Bị chặn — phiếu chuyển kho cần lý do |
| BS.33 | Chuyển kho | Chọn bản → Chuyển kho, ghi lý do và số quyết định | Sinh số phiếu chuyển kho; bản sách đổi kho; lịch sử hiện trên bản đó |
| BS.34 | In phiếu chuyển kho | Mở phiếu vừa lập → In | PDF đúng thể thức hành chính, có bảng chi tiết và ô ký |
| BS.35 | Thanh lý | Chọn bản → Thanh lý, ghi lý do | Sinh số quyết định; bản chuyển sang Thanh lý, rời giá, bị khóa |
| BS.36 | Thanh lý lần hai | Thanh lý lại chính các bản đó | Bị chặn — các bản đã ra khỏi kho |
| BS.37 | Lập biên bản bàn giao | Mở đơn đặt → Lập biên bản | Số bản và giá trị lấy đúng theo số thực nhận của đơn |
| BS.38 | In biên bản bàn giao | Bấm In trên biên bản | PDF có quốc hiệu, tên biểu mẫu, bảng chi tiết, dòng tổng cộng và hai ô ký |
| BS.39 | Đính kèm bản scan | Tải lên tệp PDF đã ký | Biên bản hiện đã có bản scan; tải lại đúng tệp đã gửi |
| BS.40 | Đóng kho | Bổ sung → Kiểm kê kho → Đóng kho | Kho chuyển sang Đang đóng |
| BS.41 | Tạo kỳ kiểm kê | Tạo kỳ, phạm vi toàn kho | Danh sách kỳ vọng được chốt ngay; kho tự đóng nếu chọn |
| BS.42 | Hai kỳ trên một kho | Tạo kỳ thứ hai cho cùng kho | Bị chặn — còn kỳ chưa chốt |
| BS.43 | Chuyển vào kho đang kiểm kê | Chuyển sách vào kho đang đóng | Bị chặn kèm lý do kho đang kiểm kê |
| BS.44 | Quét khớp | Quét mã vạch của bản thuộc kho | Báo Khớp, hiện nhan đề, tiến độ tăng |
| BS.45 | Quét mã lạ | Quét một mã không có trong hệ thống | Báo Thừa; tiến độ không vượt quá số bản kỳ vọng |
| BS.46 | Quét trùng | Quét lại đúng mã vừa quét | Báo đã quét rồi, không tính thêm |
| BS.47 | Nạp tệp quét rời | Tải lên tệp mỗi dòng một mã | Báo số mã khớp, thừa, sai kho và trùng |
| BS.48 | Chốt kỳ kiểm kê | Bấm Chốt kỳ | Ra bốn nhóm kết quả và giá trị bản thiếu; kho được mở lại |
| BS.49 | Xuất kết quả kiểm kê | Bấm Xuất kết quả | Tệp Excel liệt kê đúng nhóm đang lọc |
| BS.50 | In biên bản kiểm kê | Bấm In biên bản | PDF liệt kê các bản không khớp, kèm số liệu tổng hợp |
| BS.51 | Lập quyết định từ bản thiếu | Bấm Xử lý bản thiếu → Mất | Sinh quyết định, các bản chuyển sang Mất, dòng kết quả đánh dấu đã xử lý |
| BS.52 | Thống kê theo chiều | Báo cáo bổ sung → Thống kê theo chiều, đổi chiều | Bảng và tỷ trọng đổi theo; tổng cộng khớp với tổng số bản |
| BS.53 | Thống kê theo thời gian | Chọn chiều Thời gian, đổi đơn vị nhóm | Dòng gộp lại theo ngày / tháng / quý / năm |
| BS.54 | Bảng tổng hợp đa chiều | Chọn chiều hàng và chiều cột khác nhau | Tổng theo hàng, theo cột và tổng chung khớp nhau |
| BS.55 | Hai chiều trùng nhau | Chọn cùng một chiều cho hàng và cột | Bị chặn kèm thông báo rõ |
| BS.56 | Xuất báo cáo | Bấm Excel và PDF trên từng tab | Tệp xuất ra đúng bằng bộ lọc đang xem |
| BS.57 | Báo cáo duyệt mua | Mở tab Duyệt mua | Số yêu cầu theo trạng thái và đơn vị; tỷ lệ duyệt trong khoảng 0–100% |
| BS.58 | Lịch sử nhà cung cấp | Chọn một nhà cung cấp ở bộ lọc | Số đơn, tổng giá trị, tỷ lệ giao đủ và danh sách đơn |
| BS.59 | Xuất danh sách ĐKCB | Ấn phẩm trong kho → Xuất Excel | Tệp Excel đúng bằng danh sách đang lọc |
| BS.60 | Phân quyền | Đăng nhập tài khoản không có quyền Bổ sung | Menu Bổ sung không hiện; gọi thẳng API trả 403 |

## Nhóm chức năng — Phân hệ IV: Ấn phẩm định kỳ

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| DK.1 | Khai đầu báo mới | Ấn phẩm định kỳ → Báo, tạp chí → Thêm đầu báo | Đầu báo xuất hiện; hệ thống sinh kèm biểu ghi MARC 21 có leader vị trí 07 = 's', trường 022 ISSN và 310 kỳ hạn |
| DK.2 | Hiển thị ISSN | Xem danh sách sau khi lưu ISSN không dấu gạch | Màn hình hiện lại đúng cách viết chuẩn, ví dụ 1859-1450 |
| DK.3 | Khai kỳ hạn theo tháng | Chọn kỳ hạn Tháng, ngày phát hành 15 | Form hiện ô "Ngày phát hành trong tháng" |
| DK.4 | Khai kỳ hạn theo tuần | Chọn kỳ hạn Tuần | Ô ngày trong tháng đổi thành ô chọn thứ phát hành |
| DK.5 | Kỳ hạn không định kỳ | Chọn Không định kỳ, bỏ trống số kỳ/năm rồi lưu | Bị chặn — kỳ hạn này bắt buộc khai số kỳ để dựng khung số |
| DK.6 | Xem trước số dự kiến | Mở bàn làm việc → Sinh số dự kiến → Xem trước | Danh sách số hiện ra; kiểm tra lại danh sách số trong cơ sở dữ liệu vẫn trống |
| DK.7 | Ngày phát hành đúng thứ | Xem trước một tuần báo ra thứ Sáu | Mọi số đều rơi vào thứ Sáu |
| DK.8 | Ngày không tồn tại | Khai ngày phát hành 31, xem trước tháng Hai | Số tháng Hai rơi vào ngày cuối tháng chứ không bị bỏ |
| DK.9 | Kỳ nghỉ không xuất bản | Khai nghỉ tháng 7 và 8, xem trước cả năm | Không có số nào trong hai tháng đó; số vẫn chạy liền mạch qua kỳ nghỉ |
| DK.10 | Đánh lại số theo năm | Sinh số cho khoảng vắt qua năm mới | Số về lại 1 khi sang năm mới |
| DK.11 | Số liên tục | Chọn cách đánh số liên tục, số bắt đầu 250 | Số chạy 250, 251, 252... không đặt lại theo năm |
| DK.12 | Tập và số | Chọn có tập và số, tập bắt đầu 12 | Nhãn số hiện "Tập 12, Số 1 (2026)"; sang năm sau tập tăng lên 13 |
| DK.13 | Sửa tay trước khi chốt | Sửa số, tập và ngày của một dòng ở bước xem trước rồi chốt | Danh sách lưu xuống đúng như đã sửa |
| DK.14 | Chốt danh sách số | Bấm Chốt danh sách | Số dự kiến xuất hiện trên lưới nhận số |
| DK.15 | Sinh lại lần hai | Sinh lại đúng khoảng vừa sinh | Bị chặn kèm thông báo các số đã được sinh trước đó |
| DK.16 | Mở rộng khoảng sinh | Sinh cho khoảng rộng hơn | Chỉ sinh phần còn thiếu, số cũ được bỏ qua |
| DK.17 | Sinh số hàng loạt | Chọn nhiều đầu báo trên danh sách → Sinh số hàng loạt | Mỗi đầu báo sinh theo kỳ hạn của chính nó |
| DK.18 | Ghi nhận số đến | Bàn làm việc → Ghi nhận số → chọn số → Ghi nhận đã nhận | Số chuyển sang Đã nhận, ghi ngày nhận và người nhận |
| DK.19 | Sinh ĐKCB khi ghi nhận | Bật "Sinh ĐKCB", nhận 2 bản | Tạo 2 ấn phẩm có mã vạch riêng trong kho, trạng thái Trong kho và không khóa |
| DK.20 | Ghi nhận lại số đã nhận | Ghi nhận lại chính số đó | Bị chặn kèm lý do đã ghi nhận trước đó |
| DK.21 | Lưới nhận số | Xem tab Lưới nhận số | Các số nhóm theo năm, tô màu theo trạng thái; số quá hạn tô đỏ |
| DK.22 | Đánh dấu số thiếu | Chọn số quá hạn → Đánh dấu thiếu | Số chuyển sang trạng thái Thiếu |
| DK.23 | Lập khiếu nại | Chọn số thiếu → Lập khiếu nại | Sinh số phiếu; nội dung soạn sẵn có tên số và ngày phát hành dự kiến |
| DK.24 | Khiếu nại trùng | Lập khiếu nại lần hai cho số đang có phiếu mở | Bị chặn |
| DK.25 | Ghi phản hồi nhà cung cấp | Tab Khiếu nại → Ghi phản hồi → Đã giải quyết | Phiếu đổi trạng thái, ghi ngày phản hồi |
| DK.26 | Hủy khiếu nại | Ghi phản hồi với trạng thái Đã hủy | Số quay lại trạng thái Thiếu để vẫn nằm trong danh sách theo dõi |
| DK.27 | Nhập mục lục bài trích | Tab Mục lục bài trích → chọn số → Thêm bài → Lưu | Danh sách bài lưu lại, cột số bài trên ô chọn số tăng lên |
| DK.28 | Nhập mục lục từ Excel | Tải tệp mẫu, điền, nhập lại | Số bài nhập được báo đúng; dòng thiếu nhan đề và dòng trang ngược báo theo số dòng |
| DK.29 | Sinh biểu ghi bài trích | Bấm Sinh biểu ghi bài trích | Mỗi bài có một biểu ghi MARC riêng, leader vị trí 07 = 'a', trường 773 mang $t tên tạp chí, $g số và trang, $x ISSN |
| DK.30 | Tra cứu bài trích | Biên mục → tra tên bài, gõ không dấu | Bài trích tìm được như một biểu ghi bình thường |
| DK.31 | Sinh biểu ghi lần hai | Bấm sinh lại | Bị chặn — mọi bài đã có biểu ghi |
| DK.32 | Xóa bài đã có biểu ghi | Bỏ bài khỏi mục lục rồi lưu | Bị chặn kèm hướng dẫn xóa biểu ghi ở phân hệ Biên mục trước |
| DK.33 | Đóng tập khi chưa nhận số | Tab Đóng tập → Đóng tập cho năm chưa nhận số nào | Bị chặn — không đóng tập rỗng ruột |
| DK.34 | Đóng tập | Nhận đủ các số rồi Đóng tập cả năm | Sinh mã tập, một ĐKCB mới có mã vạch và ký hiệu xếp giá riêng |
| DK.35 | Số lẻ sau khi đóng tập | Xem lại lưới nhận số | Các số chuyển sang "Đã đóng tập" nhưng vẫn còn trong sổ để đối chiếu khi kiểm kê |
| DK.36 | Tập đóng nằm trong kho | Bổ sung → Ấn phẩm trong kho → tra mã vạch của tập | Tập hiện ra như một ấn phẩm bình thường, có giá và ký hiệu xếp giá |
| DK.37 | Bảng tổng hợp theo năm | Tab Lưới nhận số, phần Tổng hợp theo năm | Số kỳ dự kiến, đã nhận, thiếu, đã đóng tập, tỷ lệ nhận và giá trị |
| DK.38 | Xóa đầu báo đã nhận số | Xóa một đầu báo đã nhận số | Bị chặn kèm số kỳ đã nhận |
| DK.39 | Xóa đầu báo chưa nhận số | Xóa đầu báo mới chỉ có số dự kiến | Xóa được, các số dự kiến bị xóa cùng |
| DK.40 | Báo cáo bốn chiều | Báo cáo ấn phẩm định kỳ → đổi chiều thống kê | Tổng hợp, môn loại, mức định kỳ và ngôn ngữ đều ra số liệu; tổng cộng khớp |
| DK.41 | Gộp môn loại | Xem chiều Môn loại với các đầu báo có DDC 020.5 và 070.1 | Cùng gộp vào lớp "000 — Tin học, thông tin và tác phẩm tổng quát" |
| DK.42 | Xuất báo cáo | Bấm Excel và PDF | Tệp xuất ra đúng bằng bộ lọc đang xem |
| DK.43 | Phân quyền | Đăng nhập tài khoản không có quyền Ấn phẩm định kỳ | Menu không hiện; gọi thẳng API trả 403 |

## Nhóm chức năng — Phân hệ VI: Bạn đọc

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| BD.1 | Thêm bạn đọc, để trống số thẻ | Bạn đọc → Hồ sơ bạn đọc → Thêm bạn đọc, bỏ trống ô Số thẻ | Hệ thống sinh số thẻ theo quy tắc trong Tham số hệ thống; sổ cấp thẻ có một thẻ hiện hành trùng số |
| BD.2 | Hạn thẻ tính theo loại bạn đọc | Thêm bạn đọc loại Sinh viên, bỏ trống ngày hết hạn | Hạn thẻ bằng ngày cấp cộng số tháng khai trong danh mục loại bạn đọc |
| BD.3 | Trùng mã sinh viên | Thêm bạn đọc thứ hai cùng mã sinh viên | Bị chặn kèm thông báo mã đã có trong hệ thống |
| BD.4 | Tra cứu gõ không dấu | Gõ "nguyen van an" vào ô tìm kiếm | Tìm được bạn đọc tên "Nguyễn Văn An" |
| BD.5 | Tra cứu bằng số thẻ, mã SV, CCCD, email, điện thoại | Lần lượt gõ từng thứ vào cùng một ô | Mỗi lần đều ra đúng bạn đọc, không phải chọn trước tìm theo trường nào |
| BD.6 | Lọc theo lớp và khóa | Chọn lớp, chọn khóa trên thanh lọc | Danh sách chỉ còn bạn đọc của lớp và khóa đó |
| BD.7 | Cảnh báo hạn thẻ | Xem cột Hạn thẻ | Mỗi dòng ghi rõ "Còn N ngày" hoặc "Quá hạn N ngày", quá hạn tô đỏ |
| BD.8 | Sửa hạn thẻ | Mở hồ sơ → Sửa hồ sơ → đổi ngày hết hạn | Sổ cấp thẻ đổi theo, không còn hai con số khác nhau trên cùng một tấm thẻ |
| BD.9 | Tải ảnh không phải ảnh | Đổi tên một tệp bất kỳ thành .jpg rồi tải lên | Bị chặn — hệ thống kiểm tra chữ ký nhị phân chứ không tin phần mở rộng |
| BD.10 | Cắt ảnh chân dung | Đổi ảnh → chọn tệp → kéo và phóng to trong khung 3×4 → Lưu | Ảnh lưu đúng phần đã cắt, hiện trên hồ sơ và trên danh sách |
| BD.11 | Chụp ảnh từ webcam | Đổi ảnh → Chụp từ webcam → Chụp → Lưu | Ảnh chụp vào thẳng khung cắt; đóng hộp thoại là webcam tắt |
| BD.12 | Ghi nhận vi phạm | Tab Vi phạm → Ghi nhận vi phạm, chọn loại, bỏ trống mức phạt | Mức phạt lấy theo mặc định của loại vi phạm |
| BD.13 | Cấp lại thẻ mất | Hồ sơ → Cấp lại thẻ, ghi lý do | Số thẻ mới được cấp; thẻ cũ vẫn còn trong tab Thẻ đã cấp, đánh dấu đã thu hồi |
| BD.14 | Cấp lại thẻ hỏng | Cấp lại thẻ, tick giữ nguyên số thẻ | Số thẻ không đổi, vẫn ghi thêm một dòng trong sổ cấp thẻ |
| BD.15 | Cấp lại thẻ thiếu lý do | Bấm cấp lại mà không ghi lý do | Bị chặn |
| BD.16 | Gia hạn thẻ đã hết hạn | Chọn bạn đọc có thẻ quá hạn → Gia hạn 12 tháng | Hạn mới tính từ hôm nay cộng 12 tháng |
| BD.17 | Gia hạn thẻ còn hạn | Chọn bạn đọc còn 6 tháng → Gia hạn 12 tháng | Hạn mới cộng tiếp vào hạn cũ, không mất phần thời gian chưa dùng |
| BD.18 | Gia hạn cả khóa | Lọc theo khóa → tick chọn → bật "Áp dụng cho toàn bộ kết quả lọc" → Gia hạn | Toàn bộ bạn đọc của khóa được gia hạn, không phải tick từng dòng |
| BD.19 | Tạm khóa thẻ thiếu lý do | Thao tác hàng loạt → Tạm khóa, bỏ trống lý do | Bị chặn |
| BD.20 | Tạm khóa và mở khóa | Khóa kèm lý do rồi mở lại | Trạng thái đổi tương ứng; khi khóa, hồ sơ hiện cảnh báo không đủ điều kiện mượn |
| BD.21 | Ra trường khi còn công nợ | Chọn bạn đọc còn sách hoặc còn nợ phí → Chuyển ra trường | Người đó bị giữ lại kèm lý do; những người khác vẫn chuyển được |
| BD.22 | Xác nhận công nợ | Mở hồ sơ, xem khối Công nợ với thư viện | Ghi rõ còn giữ mấy tài liệu và còn nợ bao nhiêu tiền |
| BD.23 | Xóa hồ sơ còn công nợ | Xóa hồ sơ của bạn đọc đang nợ phí | Bị chặn |
| BD.24 | Đặt lại mật khẩu bạn đọc | Hồ sơ → Đặt lại mật khẩu | Hiện mật khẩu mới để đọc lại cho bạn đọc; phiên đăng nhập cũ trên điện thoại mất hiệu lực; nhật ký ghi việc đặt lại nhưng không ghi mật khẩu |
| BD.25 | Mẫu thẻ mặc định | Bạn đọc → Mẫu thẻ bạn đọc | Có sẵn mẫu CR80 85,6 × 54 mm dùng in được ngay |
| BD.26 | Kéo thả thiết kế thẻ | Mở mẫu thẻ, kéo một ô trên khung xem trước | Tọa độ milimét đổi theo, làm tròn tới 0,5 mm |
| BD.27 | Nội dung tràn khổ thẻ | Đặt một ô ra ngoài mép thẻ rồi lưu | Bị chặn — bắt lỗi trước khi in hỏng cả hộp phôi thẻ |
| BD.28 | In thẻ một người | Hồ sơ → In thẻ | Tệp PDF một trang đúng khổ CR80, có ảnh, mã vạch số thẻ và tên thư viện lấy từ tham số |
| BD.29 | Tên thư viện dài | Đặt tên thư viện dài trong Tham số hệ thống rồi in thẻ | Chữ tự co lại cho vừa ô, không bị cắt cụt |
| BD.30 | In thẻ cả lớp | Lọc theo lớp → In thẻ, chọn xếp nhiều thẻ trên A4 | Một tệp PDF nhiều thẻ, mặt sau đảo cột để lật giấy in hai mặt là khớp |
| BD.31 | Đếm số lần in | In thẻ rồi mở lại tab Thẻ đã cấp | Số lần in tăng lên một |
| BD.32 | Xem trước thẻ | Bấm In thử trên màn hình mẫu thẻ | Ra tệp PDF nhưng số lần in giữ nguyên |
| BD.33 | Tải tệp mẫu nhập bạn đọc | Nhập xuất dữ liệu bạn đọc → Tải tệp mẫu | Tệp Excel có hàng tiêu đề tiếng Việt và sheet hướng dẫn từng cột |
| BD.34 | Kiểm tra tệp nhập | Chọn tệp có dòng thiếu tên, sai ngày, sai email → Kiểm tra tệp | Bảng lỗi chỉ đúng số dòng và đúng cột; không dòng nào được ghi vào hệ thống |
| BD.35 | Trùng ngay trong tệp | Tệp có hai dòng cùng mã sinh viên | Dòng thứ hai bị báo lặp trong chính tệp |
| BD.36 | Ánh xạ cột | Tệp của phòng đào tạo đặt tên cột khác tệp mẫu → khai ánh xạ | Nhập được; ánh xạ lưu lại cho lần nhập sau |
| BD.37 | Nhập chạy nền | Bấm Nhập vào hệ thống | Đợt nhập hiện trong bảng bên dưới, tự cập nhật tiến độ đến khi hoàn thành |
| BD.38 | Tự tạo danh mục khi nhập | Tệp có khoa, lớp, khóa chưa có trong danh mục | Được tạo mới để lần sau lọc được; tắt tùy chọn thì báo lỗi thay vì tự tạo |
| BD.39 | Nhập lại tệp cũ | Nhập lại đúng tệp vừa nhập | Mặc định báo trùng; đổi sang Cập nhật thì hồ sơ được cập nhật chứ không nhân đôi |
| BD.40 | Nhật ký lỗi | Đợt nhập có dòng lỗi → Nhật ký lỗi | Tải về tệp Excel liệt kê dòng, cột, giá trị và lý do |
| BD.41 | Nhập ảnh hàng loạt | Nén ảnh đặt tên theo mã sinh viên thành ZIP rồi nhập | Ảnh khớp vào đúng hồ sơ; ảnh không tìm được chủ và tệp không phải ảnh được liệt kê riêng |
| BD.42 | Xuất danh sách bạn đọc | Lọc theo lớp → Xuất Excel | Tệp xuất đúng bằng bộ lọc; nhật ký hệ thống ghi lại lượt xuất dữ liệu cá nhân |
| BD.43 | Đồng bộ từ hệ thống đào tạo | Khai ánh xạ trường rồi gọi `POST /api/readers/sync` | Bản ghi mới được tạo, gọi lại lần hai thì cập nhật; chế độ chạy thử không ghi gì |
| BD.44 | Báo cáo số lượng bạn đọc | Báo cáo bạn đọc → đổi chiều thống kê | Bảy chiều đều ra số liệu; tổng cộng khớp với tổng số trên danh sách |
| BD.45 | Báo cáo đăng ký mới | Chọn Đăng ký mới theo thời gian, gộp theo tháng | Biểu đồ đường và bảng số liệu, có cột cộng dồn |
| BD.46 | Báo cáo thẻ sắp hết hạn | Chọn Thẻ sắp hết hạn, đặt 30 ngày | Ba con số tổng quan và danh sách kèm số ngày còn lại |
| BD.47 | Người ra trường không bị nhắc | Cho một bạn đọc ra trường rồi xem lại báo cáo | Không xuất hiện trong danh sách nhắc gia hạn |
| BD.48 | Báo cáo mức độ sử dụng | Chọn Mức độ sử dụng, đổi giữa hai chế độ | Ra danh sách mượn nhiều nhất kèm biểu đồ cột, và danh sách chưa từng mượn |
| BD.49 | Xuất báo cáo | Bấm Excel và PDF ở từng báo cáo | Tệp xuất đúng bằng bộ lọc đang hiển thị |
| BD.50 | Phân quyền | Đăng nhập tài khoản Cán bộ biên mục | Menu Bạn đọc không hiện; gọi thẳng API trả 403 |

## Nhóm chức năng — Phân hệ VII: Lưu thông

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| LT.1 | Chính sách nạp sẵn khi cài đặt | Lưu thông → Chính sách lưu thông | Đủ sáu chính sách cho sáu loại bạn đọc; loại "Khách" tắt mượn về nhà |
| LT.2 | Ô thử chính sách | Chọn loại bạn đọc Sinh viên, dạng tài liệu Sách, kho Kho mở rồi bấm Thử | Hiện chính sách thắng kèm số bản, số ngày mượn, số lần gia hạn và tiền phạt mỗi ngày |
| LT.3 | Độ ưu tiên khi nhiều chính sách khớp | Tạo thêm một chính sách chỉ khai loại bạn đọc, độ ưu tiên thấp hơn chính sách khai đủ ba chiều, rồi thử lại | Chính sách khai cụ thể hơn thắng; đổi độ ưu tiên thì kết quả đổi theo |
| LT.4 | Hạn trả rơi vào ngày nghỉ lễ | Mở ô thử hạn trả, chọn ngày mượn 01/09 và số ngày mượn 1 | Hạn trả 02/09 bị dời sang ngày làm việc kế tiếp, màn hình ghi rõ đã dời |
| LT.5 | Quét thẻ ở quầy | Lưu thông → Quầy lưu thông, quét số thẻ (hoặc mã sinh viên) | Hiện ảnh, số sách đang mượn, hạn mức còn lại, nợ phí, hạn thẻ còn bao nhiêu ngày và danh sách phiếu đang mượn |
| LT.6 | Quét mã vạch tài liệu | Quét mã vạch một bản đang trong kho | Dòng được thêm kèm hạn trả **do máy chủ tính**; kiểm chứng bằng cách gọi thẳng `POST /api/circulation/desk/scan` bằng Postman phải ra cùng ngày |
| LT.7 | Quét trùng trong cùng lượt | Quét lại đúng mã vạch vừa quét | Bị chặn kèm lý do, không sinh hai phiếu mượn cho một bản |
| LT.8 | Vượt hạn mức | Quét bốn bản cho một sinh viên (hạn mức 3) rồi hoàn tất | Ghi mượn 3 phiếu, bản thứ tư bị giữ lại kèm lý do "đã mượn đủ 3 tài liệu" |
| LT.9 | Bản đang có người mượn | Bạn đọc khác quét đúng bản vừa cho mượn | Bị chặn kèm tên trạng thái, không cho mượn chồng |
| LT.10 | In phiếu mượn | Sau khi hoàn tất, bấm In phiếu mượn | Ra tệp PDF đúng mẫu; ô chữ ký hiện tên cán bộ đang đăng nhập, không hiện mã trường dữ liệu |
| LT.11 | Thao tác toàn bàn phím | Dùng F2, F3, F4 và Esc để làm trọn một lượt ghi mượn | Không cần chạm chuột; mỗi lần quét có tiếng bíp phản hồi khác nhau cho thành công và lỗi |
| LT.12 | Đặt giữ chỗ và hàng đợi | Hai bạn đọc lần lượt đặt giữ cùng một biểu ghi | Người trước số 1, người sau số 2 |
| LT.13 | Hủy phiếu đầu hàng đợi | Hủy phiếu số 1 kèm lý do | Người số 2 lên số 1 ngay, không còn phiếu nào mang số 2 |
| LT.14 | Gia hạn khi có người đang đợi | Bấm gia hạn phiếu mượn của tài liệu đang có người đặt giữ | Bị chặn kèm lý do |
| LT.15 | Ghi trả bản có người đặt giữ | Quét mã vạch bản đó ở tab Ghi trả | Bản chuyển sang trạng thái "Đặt giữ", giữ lại tại quầy, màn hình ghi rõ giữ cho ai và người đó nhận được thông báo |
| LT.16 | Người đặt giữ tới nhận | Người đứng đầu hàng đợi quét thẻ rồi quét đúng bản đang giữ | Mượn được; phiếu đặt giữ chuyển sang "Đã nhận" |
| LT.17 | Tính tiền phạt quá hạn | Trả một phiếu đã quá hạn | Số ngày phạt trừ ngày ân hạn và trừ ngày nghỉ; tiền phạt bằng số ngày còn lại nhân đơn giá của chính sách |
| LT.18 | Ghi mất tài liệu | Mở phiếu mượn → Ghi nhận mất | Lập khoản bồi thường theo hệ số cấu hình (mặc định gấp đôi giá bìa); ấn phẩm chuyển sang "Mất" và bị khóa |
| LT.19 | Thu tiền phạt nhiều lần | Thu một phần, rồi thu nốt phần còn lại | Còn nợ giảm dần về 0; thu thêm khi đã đủ thì bị chặn |
| LT.20 | Miễn giảm phải có lý do | Bấm Miễn mà không ghi lý do | Bị chặn; ghi lý do thì miễn được và khoản nợ về 0 |
| LT.21 | In biên lai phạt | Bấm Biên lai trên một khoản phạt | Ra PDF có số tiền bằng chữ tiếng Việt |
| LT.22 | Ghi nhận ra vào thư viện | Quét cùng một thẻ hai lần tại tab Ra vào thư viện | Lần đầu ghi vào, lần sau ghi ra; danh sách người đang ở trong thư viện đổi theo |
| LT.23 | Giao và nhận lại tủ gửi đồ | Bấm một ô tủ trống, quét thẻ, giao chìa; sau đó bấm lại ô tủ đó | Tủ chuyển sang "Đang dùng" rồi về "Trống"; giao tủ thứ hai cho cùng bạn đọc bị chặn |
| LT.24 | Bảy báo cáo lưu thông | Lưu thông → Báo cáo lưu thông, mở lần lượt bảy tab | Mỗi tab có bảng, biểu đồ và xuất được PDF lẫn Excel đúng bộ lọc đang xem |
| LT.25 | Nhắc hạn hàng loạt | Ở báo cáo quá hạn, bấm Gửi nhắc hàng loạt | Chỉ gửi cho đúng những phiếu đang lọc; số lượng gửi hiện trên màn hình |
| LT.26 | Bạn đọc đăng nhập bằng số thẻ | `POST /api/reader/auth/login` với số thẻ và mật khẩu | Trả access token và refresh token; sai mật khẩu 5 lần thì khóa 15 phút |
| LT.27 | Thẻ điện tử | `GET /api/reader/card` | Trả số thẻ, hạn thẻ và chuỗi mã vạch để hiện lên điện thoại |
| LT.28 | Bạn đọc chỉ thấy dữ liệu của mình | Đăng nhập bạn đọc A rồi gọi `POST /api/reader/loans/{id}/renew` với phiếu của bạn đọc B | Trả HTTP 403 |
| LT.29 | Mượn tự phục vụ khi chưa bật | `POST /api/reader/loans/self-checkout` | Bị chặn kèm thông báo thư viện chưa mở chức năng; bật tham số rồi gọi lại kèm mã điểm quét đúng thì mượn được |

## Cách chạy bộ kiểm thử tự động

```bash
cd backend
dotnet test                 # 280 unit test + 223 integration test
```

Integration test tự khởi tạo một container PostgreSQL 16 và một container MinIO riêng, chạy
migration, nạp dữ liệu nền, bật máy chủ tác vụ nền rồi gọi API qua đúng giao diện HTTP mà trình
duyệt dùng — không có thành phần nào bị giả lập.

```bash
cd frontend-admin
npm test                    # 85 test giao diện
```

---

## Ghi chú

Nhóm kiểm thử 2.4 (trao đổi dữ liệu) đã có phần ISO 2709 và MARCXML ở nhóm kịch bản MARC bên
trên; phần Z39.50 và OAI-PMH sẽ bổ sung khi bàn giao Phase 11.

Nhóm kiểm thử 2.5 (chuyển đổi dữ liệu) đã có phần nhập ISO 2709 và nhập Excel ở nhóm kịch bản
Biên mục; phần đối chiếu số lượng bạn đọc và giao dịch sẽ bổ sung khi bàn giao các phân hệ tương ứng.

Nhóm kiểm thử 2.8 (báo cáo) đã có phần báo cáo bổ sung ở nhóm kịch bản Phân hệ III và bảy báo cáo lưu thông ở nhóm Phân hệ VII; các báo cáo của
những phân hệ còn lại sẽ bổ sung khi bàn giao phân hệ tương ứng.

Nhóm kiểm thử 2.7 (ứng dụng di động) trong đợt web này được thay bằng kiểm thử tích hợp nhóm `/api/reader/*`; phần lưu thông của nhóm này nằm ở các kịch bản LT.26–LT.29.

Các nhóm kiểm thử 2.2 (các phân hệ chưa bàn giao) và 2.7 (ứng dụng di động) sẽ được bổ sung vào tài
liệu này theo từng phân hệ được bàn giao. Tài liệu luôn phản ánh đúng phạm vi đã hoàn thành tại thời điểm
nghiệm thu, không liệt kê trước những gì chưa làm.
