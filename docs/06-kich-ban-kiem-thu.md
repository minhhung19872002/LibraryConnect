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
| 2.1.8 | Tài khoản quản trị đầu tiên | Đọc mật khẩu tạm trong nhật ký khởi động (`docker compose logs api`), đăng nhập bằng `admin` và chuỗi đó | Đăng nhập được và **bị bắt buộc đổi mật khẩu ngay**; mật khẩu khác nhau ở mỗi bản cài, không có giá trị mặc định chung | Integration — `InstallationTests`; Unit — `SeededAdminPasswordTests`, `SecretsInRepositoryTests` | | |
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
| 2.3.17 | Ghi nhật ký lượt xem | Nhật ký hệ thống → Cài đặt ghi nhận → tắt rồi bật lại ô "Xem" của Bạn đọc → mở một hồ sơ bạn đọc → tra nhật ký với hành động Xem | Có một dòng cho đúng hồ sơ vừa mở; tắt ô ấy rồi mở lại thì không sinh thêm dòng nào. Đối tượng chưa hỗ trợ ghi lượt xem có ô bị khoá kèm lời giải thích |

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
| 2.6.9 | Sao lưu chạy nền, không chiếm lượt HTTP | Bấm **Sao lưu ngay** rồi đóng ngay hộp thoại; bấm **Sao lưu ngay** lần thứ hai khi lượt đầu chưa xong | Lượt đầu trả về tức thì với trạng thái *Đã xếp hàng*, bảng tự cập nhật sang *Đang chạy* rồi *Thành công* mà không cần bấm tải lại; lượt thứ hai bị từ chối 409 kèm câu chỉ chỗ xem tiến độ. Lượt treo quá 6 giờ (máy chủ bị khởi động lại giữa chừng) tự chuyển sang *Thất bại* thay vì khoá mọi lần sau | Đúng như mong đợi (`BackupTests`, ba phép thử đỏ trước khi sửa) | Đạt |
| 2.6.10 | Cảnh báo khi hết dung lượng | Xem thẻ *Dung lượng bản sao lưu* trên màn hình Sao lưu | Hiển thị dung lượng đã dùng và còn trống; thanh tiến trình chuyển đỏ khi vượt 90% | | | |
| 2.6.11 | Phục hồi chạy nền, theo dõi tại chỗ | Bấm *Phục hồi* trên một bản sao lưu → qua hai bước cảnh báo → nhập mật khẩu → *Phục hồi ngay* | Hộp thoại chuyển sang màn hình theo dõi, không đóng được khi đang chạy; thanh tiến độ chạy; xong thì báo hoàn tất kèm lời nhắc đăng nhập lại, hỏng thì báo cơ sở dữ liệu giữ nguyên như trước. Lượt thứ hai khi lượt đầu chưa xong bị từ chối 409 | Đúng như mong đợi (`BackupTests` — bốn phép thử phục hồi, không phép thử nào để `pg_restore` thật chạy vì nó sẽ ghi đè cơ sở dữ liệu của cả bộ kiểm thử) | Đạt |
| 2.6.12 | Đổi lịch sao lưu có hiệu lực ngay | Tham số → Cấu hình sao lưu → đổi giờ chạy → mở màn hình Sao lưu | Dòng "Lịch" đổi theo, và không có cảnh báo "máy chủ đang chạy theo lịch cũ"; tắt sao lưu tự động thì việc định kỳ biến mất |
| 2.6.13 | Phục hồi kèm tệp tài liệu số | Sao lưu kèm tệp → xóa một tệp trong kho đối tượng → phục hồi từ chính bản ấy | Thông báo ghi rõ số tệp đã tải lại; mở tài liệu số trên trang tra cứu thì đọc được, không còn lỗi thiếu tệp |
| 2.6.14 | Bản sao lưu không mang theo hàng đợi việc | Đọc dòng lệnh gửi cho `pg_dump` và `pg_restore` | Cả hai đều có `--exclude-schema=hangfire`. Nếu không, phục hồi bản hôm qua sẽ làm sống lại hàng đợi hôm qua và chạy lại những việc đã chạy rồi; và lượt phục hồi không thể tự chạy trong Hangfire vì nó xoá đúng bảng đang ghi nhận chính nó | Đúng như mong đợi (`BackupArgumentTests`, đỏ trước khi sửa) | Đạt |

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
| DM.13 | Nhập cột tham chiếu bằng tên | Tệp ngành đào tạo: một dòng gõ mã khoa, một dòng gõ tên khoa không dấu, một dòng gõ khoa không có thật | Hai dòng đầu vào đúng khoa; dòng thứ ba báo lỗi ở cột "Khoa quản lý" và không được nhập | Integration — `CatalogTests` | | |
| DM.14 | In danh mục ra giấy | Mở một danh mục bất kỳ → bấm **In** | Tải về tệp PDF có tiêu đề thư viện, tên danh mục, tổng số giá trị, bảng mã – tên – trạng thái và dòng người in; nút Xuất Excel vẫn cho tệp sửa được để nhập ngược lại | Integration — `CatalogTests` | | |

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
| BM.42 | Khung mẫu để trống không lưu vào biểu ghi | Biên mục mới → chọn "Sách" → chỉ điền 245 → Ctrl+S → tab MARC thô | Không còn trường con rỗng nào; trường không có nội dung (020, 250, 500…) bị bỏ; 040/041/044 có giá trị ngầm định giữ nguyên | Đạt (phép thử `BibEditReviewTests`) | Đạt |
| BM.43 | Thêm điểm truy cập vào biểu ghi đã có | Sửa một biểu ghi → thêm `100$a` tên mới và `653$a` từ khóa mới → Lưu | Lưu thành công; tác giả chính đổi theo; không có 409 "Không lưu được dữ liệu" | Đạt | Đạt |
| BM.44 | Tên thẩm quyền không bị tạo trùng khi có hàng trăm tên cùng họ | Kho có ≥ 220 tác giả họ "Nguyễn"; lưu hai biểu ghi cùng tác giả "Nguyễn Văn Kiểm" | Danh mục tác giả chỉ có một "Nguyễn Văn Kiểm"; hai biểu ghi trỏ cùng một mục | Đạt | Đạt |
| BM.45 | Địa chỉ người dùng không giả được | Từ một máy khác gửi `curl -H 'X-Forwarded-For: 203.0.113.9' http://<nginx>/api/auth/login` rồi đọc `sys.login_histories` | Ghi địa chỉ thật của máy gửi, không ghi `203.0.113.9`; hai máy khác nhau có hai ngăn giới hạn tốc độ riêng | Đạt (`CurrentUserIpTests`, `ForwardedHeadersSetupTests`) | Đạt |
| BM.46 | Trình hướng dẫn nhập trường 008 | Biên mục mới → Trường điều khiển → nút **Trình hướng dẫn** ở dòng 008 → đổi "Loại ngày xuất bản" sang `m`, gõ mã ngôn ngữ `eng`, đóng hộp thoại | Chuỗi 008 trên màn hình đổi đúng hai chỗ: vị trí 06 thành `m`, vị trí 35–37 thành `eng`, độ dài vẫn 40. Khối vị trí 18–34 chỉ hiện khi đầu biểu khai tài liệu chữ in; loại khác thì có lời nhắc sửa ở ô chuỗi đầy đủ | Đúng như mong đợi (`marcRecord.test.ts` — đọc ghi theo vị trí) | Đạt |
| BM.47 | Nhân bản và sắp xếp lại trường | Biểu ghi có 245 và hai 650 → bấm nút nhân bản ở 650 đầu → kéo trường 245 xuống cuối → bấm nút mũi tên lên ở trường vừa kéo | Bản sao nằm ngay dưới bản gốc và sửa nó không đụng tới bản gốc; thứ tự trường đổi theo thao tác kéo; nút mũi tên làm được cùng việc bằng bàn phím (yêu cầu 6.6) | Đúng như mong đợi (`marcRecord.test.ts` — nhân bản trường, sắp xếp lại trường) | Đạt |
| BM.48 | Ctrl+D nhân bản trường đang gõ | Đặt con trỏ vào một trường con của 650 → nhấn Ctrl+D | Trường 650 được nhân bản, trình duyệt **không** mở hộp đánh dấu trang | Đúng như mong đợi | Đạt |
| BM.49 | Xem trước ISBD trước khi lưu | Biên mục mới, điền 245/260/300 → bấm **Xem trước ISBD** khi chưa lưu | Khung mô tả thư mục hiện từng vùng kèm nhan đề vừa gõ, cộng một đoạn gộp đúng cách nó lên phích. Không có biểu ghi nào được ghi xuống cơ sở dữ liệu | Đúng như mong đợi (`MarcTests.An_unsaved_record_can_be_previewed_as_a_bibliographic_description`; biểu ghi hỏng trả 400 chứ không 500) | Đạt |
| BM.50 | Lấy biểu ghi từ Z39.50 / theo ISBN ngay trên trình soạn | Biên mục mới → **Lấy từ ISBN** → nhập `9780472093755` → Tra cứu → **Nạp vào trình soạn** | Hộp thoại mở sẵn ở tiêu chí ISBN; kết quả gom theo máy chủ, biểu ghi thư viện đã có thì gắn nhãn thay cho nút nạp; bấm nạp thì biểu ghi qua bước `prepare` của máy chủ rồi vào thẳng trình soạn để hiệu đính | Đúng như mong đợi | Đạt (máy chủ Z39.50 công khai) |
| BM.51 | Khôi phục bộ định nghĩa MARC chuẩn | Định nghĩa trường → sửa tên trường 245 thành "Tên gõ nhầm" → thêm một trường 9xx của thư viện → bấm **Khôi phục bộ chuẩn** | Trường 245 về đúng tên chuẩn; trường 9xx vẫn còn; thông báo ghi rõ số trường thêm, số ghi đè và số trường riêng được giữ. Bấm **Nạp trường còn thiếu** thì không ghi đè gì cả |

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
| BS.61 | In phiếu ngay sau chuyển kho | Chuyển kho xong, trong hộp "Phiếu chuyển kho CK-…" bấm In phiếu | Tải về PDF của đúng số phiếu vừa sinh |
| BS.62 | In lại phiếu chuyển kho | Ấn phẩm trong kho → Phiếu chuyển kho → lọc theo kho nhận → In lại | Danh sách có phiếu vừa lập với đúng số bản; PDF tải về |
| BS.63 | In quyết định thanh lý | Thanh lý xong bấm In quyết định; hoặc mở chi tiết bản đã thanh lý → In quyết định | PDF theo mẫu quyết định thanh lý, tên tệp mang số quyết định |
| BS.64 | Yêu cầu đặt báo | Tạo yêu cầu loại Ấn phẩm định kỳ: tạp chí tháng, 12 kỳ/năm, đặt 01/2026–12/2026, 2 bản/kỳ, 25.000 đ/kỳ | Dòng hiện "12 kỳ", thành tiền 600.000; máy chủ lưu đúng số ấy; duyệt 1 bản thì giá trị duyệt 300.000 |
| BS.65 | Biểu đồ báo cáo bổ sung | Báo cáo bổ sung → Tổng quát, Thống kê theo chiều, Duyệt mua; đổi Cột/Tròn, đổi chỉ tiêu | Biểu đồ có đủ mọi dòng của bảng, mỗi dòng một màu phân loại; xuất Excel/PDF ở tab Tổng quát và Duyệt mua ra tệp |
| BS.66 | Nhập nhanh liên tục | Bổ sung → Biên mục sơ lược → nhập cuốn 1 → Enter → nhập cuốn 2 | Sau mỗi lần lưu: ô nhan đề trống và có tiêu điểm, kho/dạng tài liệu/NXB giữ nguyên, bộ đếm "Đã nhập" tăng, mã vạch sinh ra hiện bên phải |
| BS.67 | Logo trên nhãn gáy | Mẫu tem và nhãn → sửa mẫu nhãn → bật "In logo thư viện", đặt 10×10 mm → xem trước → in nhãn | Ô xem trước hiện logo đã tải ở tham số hệ thống; PDF in có logo; chưa tải logo thì khối để trống, nhãn vẫn in |
| BS.68 | Xem trước tem với dữ liệu thật | Chọn vài bản → In tem mã vạch | Hộp in hiện mô phỏng tem của bản đầu tiên với mã vạch thật của nó |
| BS.69 | Đánh giá nhà cung cấp | Danh mục → Nhà cung cấp → sửa → Đánh giá 4 → Báo cáo bổ sung → Duyệt mua → chọn nhà cung cấp ấy | Lịch sử giao dịch hiện 4 sao |
| BS.70 | Chuyển kho bằng quét mã vạch | Thao tác hàng loạt → Chuyển kho khi chưa tick gì → quét ba mã, quét lại một mã → chọn kho nhận, lý do → Thực hiện | Danh sách quét có ba dòng, mã quét lại báo "đã có"; phiếu chuyển kho ba bản |
| BS.71 | Tiến độ kiểm kê tự cập nhật | Mở kỳ đang kiểm kê ở hai trình duyệt, quét ở cửa sổ này | Cửa sổ kia tự tăng số đã quét trong vòng 5 giây, không bấm làm mới |
| BS.72 | Duyệt nhiều cấp đúng nhóm | Tham số hệ thống → đặt `ACQ.APPROVAL_LEVELS`=2, `ACQ.APPROVAL_GROUPS`=`ACQUISITION,LANHDAO` → tài khoản thuộc nhóm Lãnh đạo bấm Duyệt ở cấp 1 | Bị từ chối kèm câu nói rõ cấp 1 thuộc nhóm nào; cột trạng thái ghi "Chờ Cán bộ bổ sung duyệt" |
| BS.73 | Một người không duyệt hai cấp | Người của nhóm cấp 1 duyệt xong, bấm Duyệt lần nữa cho cấp 2 | Bị chặn: "Bạn đã duyệt cấp trước của yêu cầu này; cấp tiếp theo phải do người khác duyệt." |
| BS.74 | Thông báo tới người duyệt | Người đề nghị bấm Gửi duyệt → đăng nhập bằng tài khoản thuộc nhóm duyệt cấp 1 | Chuông trên thanh trên có số mới; mở ra thấy dòng "Yêu cầu đặt mua … chờ duyệt", bấm vào mở đúng yêu cầu ấy |
| BS.75 | Biên bản bàn giao ghi tình trạng | Đơn đặt → Lập biên bản → sau khi lập, bảng chi tiết hiện các dòng của đơn → gõ "Ướt góc 3 bản" vào một dòng → Lưu tình trạng → In biên bản | Bảng chi tiết giữ nguyên số dòng; PDF có cột Tình trạng với đúng câu vừa gõ |
| BS.76 | Biên bản không gắn đơn đặt | Gọi API lập biên bản không có `orderId`, kèm hai dòng tài liệu | Biên bản có bảng chi tiết hai dòng, tổng số bản và tổng tiền bằng đúng tổng của bảng; bản in không còn là tờ giấy trắng |
| BS.77 | Phân công kiểm kê theo tài khoản | Tạo kỳ kiểm kê, ô "Cán bộ kiểm kê" chọn hai tài khoản → đăng nhập bằng một trong hai | Chuông có thông báo "Bạn được phân công kiểm kê …" kèm tên kho và số ĐKCB phải đối chiếu |
| BS.78 | Phân công lại giữa kỳ | Mở kỳ đang chạy → thẻ "Cán bộ được phân công" → bỏ một người, thêm một người khác → Lưu phân công | Danh sách đổi đúng; chỉ người **mới** thêm nhận thông báo, người cũ không nhận lại |

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
| DK.44 | Bổ sung tổng thể — số đến hạn | Ấn phẩm định kỳ → Bổ sung tổng thể → tab Số đến hạn | Bảng gồm số đến hạn của mọi đầu báo (tạp chí tháng lẫn quý), thẻ đếm số đầu báo và số quá hạn |
| DK.45 | Nhận hàng loạt nhiều đầu báo | Tick số của hai đầu báo, gõ số lượng 2 và ngày nhận riêng cho một dòng → Ghi nhận đã nhận | Máy chủ nhận 2 số, tạo 3 ĐKCB; dòng để trống ngày lấy ngày mặc định của hộp thoại |
| DK.46 | Đối chiếu số thiếu đa đầu báo | Tab Đối chiếu số thiếu sau khi ghi thiếu một số và để một số quá hạn | Bảng "Theo đầu báo" đếm 2 số chưa về; số đã nhận không xuất hiện; bấm một đầu báo thì bảng dưới lọc theo nó |
| DK.47 | Khiếu nại nhiều đầu báo một lần | Tick số thiếu của hai đầu báo → Lập khiếu nại | Mỗi số một phiếu, gửi tới nhà cung cấp của đầu báo ấy; số vẫn nằm trong danh sách đối chiếu với nhãn "Đang khiếu nại" |
| DK.48 | Đóng tập theo khoảng số | Bàn làm việc → Đóng tập → năm 2026, từ số 1 đến số 2 | Hộp báo "Sẽ đóng 2 số: 1, 2"; tập có 2 số, số 3–4 vẫn là số lẻ đã nhận; chọn "đến số 9" không có trong năm thì bị chặn |
| DK.49 | In nhãn gáy tập | Tab Đóng tập → In nhãn gáy tập trên dòng tập vừa đóng | PDF nhãn gáy in cho ĐKCB của tập |

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
| BD.51 | Cấp lại thẻ từ hồ sơ | Mở hồ sơ bạn đọc → Cấp lại thẻ → chọn "Thẻ mất — cấp số mới", ghi lý do → Cấp lại | Thông báo nêu số thẻ mới và số cũ bị thu hồi; tab "Thẻ đã cấp" có hai dòng: thẻ cũ "Đã thu hồi" kèm lý do, thẻ mới "Đang dùng"; bỏ lý do thì bị chặn |
| BD.52 | Bảng lỗi nhập Excel sửa tại chỗ | Kiểm tra một tệp có dòng thiếu họ tên và dòng sai ngày sinh → sửa ngay trên lưới → Kiểm tra lại → Nhập các dòng đã sửa | Ô sai viền đỏ kèm lý do; Kiểm tra lại báo hợp lệ; nhập xong dòng rời khỏi lưới, hồ sơ tìm thấy trong danh sách, đợt nhập "(sửa tại chỗ)" xuất hiện ở "Các đợt nhập gần đây". Gọi thẳng `POST /api/readers/import/rows` với `dryRun: true` không tạo hồ sơ |
| BD.53 | Đồng bộ từ hệ thống đào tạo trên màn hình | Nhập xuất dữ liệu → Đồng bộ: khai `studentCode → MaSinhVien`, dán JSON hai sinh viên → Thử (không ghi) → Đồng bộ | Lần thử báo "Thử (chưa ghi) — 2 bản ghi: thêm 2…" và danh sách bạn đọc chưa đổi; lần ghi tạo hồ sơ; dán chữ không phải JSON thì báo lỗi tiếng Việt, không gọi máy chủ |
| BD.54 | Xem trước thẻ không tính lần in | Danh sách bạn đọc → chọn vài người → In thẻ → Xem trước (không tính lần in); rồi In thẻ thật | Sau xem trước, số lần in trong tab Thẻ đã cấp vẫn 0; sau in thật tăng lên 1 |
| BD.55 | In giấy xác nhận trả sách | Mở hồ sơ một bạn đọc còn giữ sách → nút "In giấy xác nhận" | Nút bị khóa, rê chuột thấy lý do "Còn … tài liệu chưa trả"; trả hết sách và nộp phạt thì nút mở, bấm ra PDF; endpoint `GET /api/readers/{id}/clearance/print` chỉ cần quyền xem bạn đọc |

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
| LT.30 | Kho đang đóng để kiểm kê thì không ghi mượn | Kiểm kê → đóng một kho; ở quầy quét thẻ rồi quét mã vạch một bản thuộc kho ấy; gọi thẳng `POST /api/circulation/desk/checkout` với mã vạch ấy | Lần quét bị từ chối kèm "Kho … đang đóng để kiểm kê"; gọi thẳng API trả HTTP 409 cùng câu ấy; bản vẫn "Trong kho". Đầu màn hình quầy có banner liệt kê kho đang đóng |
| LT.31 | Trả sách về kho đang kiểm kê | Mượn một bản trước khi đóng kho, đóng kho, rồi quét trả | Vẫn ghi trả được (tiền phạt dừng đúng ngày), cột Ghi chú hiện "Giữ ở quầy — kho đang kiểm kê" thay vì "Xếp lên giá" |

## Nhóm chức năng — Phân hệ V: Tài liệu số

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| TLS.1 | Cây bộ sưu tập nạp sẵn | Tài liệu số → Kho tài liệu số | Sáu nhánh gốc; nhánh Luận văn – Luận án có hai nhánh con và mặc định mức Hạn chế |
| TLS.2 | Chặn vòng cha con | Sửa một nhánh cha, đặt nó nằm dưới chính nhánh con của nó | Bị chặn kèm lý do |
| TLS.3 | Bộ sưu tập còn tài liệu | Xóa một bộ sưu tập đang chứa tài liệu | Bị chặn kèm lý do |
| TLS.4 | Tải tệp PDF lên | Bấm Tải tài liệu lên, chọn một tệp PDF | Sau vài giây danh sách hiện đủ số trang, ảnh bìa và nhãn "Có văn bản" |
| TLS.5 | Tệp giả dạng | Đổi tên một tệp bất kỳ thành .pdf rồi tải lên | Bị từ chối kèm thông báo không nhận ra định dạng |
| TLS.6 | Tải tệp lớn theo mảnh | Tải một tệp trên 16 MB | Thanh tiến trình chạy theo từng mảnh; ngắt mạng giữa chừng rồi tải lại thì chỉ gửi phần còn thiếu |
| TLS.7 | Ghép thiếu mảnh | Gọi `POST /api/digital/uploads/{id}/complete` khi còn thiếu mảnh | Bị chặn, thông báo ghi rõ còn thiếu mảnh nào |
| TLS.8 | Mã kiểm tra tệp | Mở chi tiết tài liệu | Có mã SHA-256 64 ký tự, dùng đối chiếu khi bàn giao dữ liệu |
| TLS.9 | Đọc trực tuyến | Bấm Đọc trên một tài liệu PDF | Mỗi trang hiện dưới dạng ảnh, không có tệp gốc nào tải xuống trình duyệt |
| TLS.10 | Chữ chìm | Xem một tài liệu có bật chữ chìm | Trên trang có tên người xem, thời điểm và địa chỉ IP, lát kín cả trang |
| TLS.11 | Xin trang ngoài khoảng | Gọi `GET /api/digital/documents/{id}/pages/999` | Trả 404 kèm thông báo tài liệu chỉ có bao nhiêu trang |
| TLS.12 | Giới hạn xem thử kiểm ở máy chủ | Bạn đọc chưa được duyệt gọi thẳng API xin trang thứ 50 của tài liệu chỉ cho xem thử 10 trang | Trả 403, không phụ thuộc việc giao diện có che nút hay không |
| TLS.13 | Tìm kiếm toàn văn không dấu | Bật "Tìm trong nội dung", gõ từ khóa không dấu | Ra đúng tài liệu kèm đoạn trích quanh chỗ khớp |
| TLS.14 | Lọc theo nhánh bộ sưu tập | Chọn nhánh cha trên cây | Thấy cả tài liệu nằm trong các nhánh con |
| TLS.15 | Tài liệu công khai | Khách chưa đăng nhập mở tài liệu công khai | Đọc được toàn văn |
| TLS.16 | Tài liệu nội bộ | Khách chưa đăng nhập mở tài liệu nội bộ | Chỉ xem thử được số trang đã khai, kèm lời nhắc đăng nhập |
| TLS.17 | Tài liệu cấm | Bạn đọc mở tài liệu mức Cấm | Không mở được nội dung, và tài liệu không hiện trên danh sách của bạn đọc |
| TLS.18 | Bạn đọc xin đọc tài liệu hạn chế | Đăng nhập bằng số thẻ, mở tài liệu hạn chế, gửi yêu cầu kèm lý do | Yêu cầu vào hàng đợi ở trạng thái Chờ duyệt |
| TLS.19 | Gửi yêu cầu trùng | Gửi lại yêu cầu khi lần trước còn đang chờ | Bị chặn kèm lý do |
| TLS.20 | Xin đọc tài liệu không hạn chế | Gửi yêu cầu cho một tài liệu công khai | Bị chặn kèm thông báo không cần xin phép |
| TLS.21 | Duyệt yêu cầu | Bấm Duyệt, đặt thời hạn 15 ngày, 5 lượt xem, tick cho tải | Bạn đọc đọc được toàn văn và tải được tệp, dù chính sách chung của tài liệu là không cho tải |
| TLS.22 | Từ chối phải có lý do | Bấm Từ chối mà bỏ trống lý do | Bị chặn; ghi lý do thì từ chối được và bạn đọc thấy đúng lý do đó |
| TLS.23 | Thu hồi quyền đọc | Bấm Thu hồi trên một quyền đang có hiệu lực | Bạn đọc quay về mức xem thử |
| TLS.24 | Quyền đọc hết hạn | Đặt thời hạn về quá khứ rồi chạy tác vụ nền `libraryconnect:digital-access-expiry` | Yêu cầu chuyển sang Hết hạn, bạn đọc không còn đọc toàn văn |
| TLS.25 | Nhật ký truy cập | Bạn đọc mở một tài liệu, cán bộ vào tab Nhật ký truy cập | Có dòng ghi đúng người xem, thời điểm, địa chỉ IP và thiết bị |
| TLS.26 | Lịch sử của bạn đọc | Gọi `GET /api/reader/digital/history` | Chỉ thấy lịch sử của chính mình, mỗi lần mở tài liệu là một dòng |
| TLS.27 | Nhập hàng loạt, kiểm tra trước | Chọn tệp ZIP rồi bấm Kiểm tra trước | Liệt kê từng tệp và kết quả dự kiến, không ghi gì vào hệ thống |
| TLS.28 | Nhập hàng loạt thật | Bấm Nhập vào hệ thống | Số tệp nhập được đúng bằng số tệp trong gói; tệp đặt tên theo số ĐKCB tự gắn vào biểu ghi |
| TLS.29 | Xuất gói tài liệu | Bấm Xuất gói | Tệp ZIP có `metadata/tai-lieu-so.xlsx`, `metadata/dublin-core.xml` và thư mục `files/` |
| TLS.30 | Bốn báo cáo tài liệu số | Mở lần lượt bốn tab báo cáo | Mỗi tab có số liệu, biểu đồ và xuất được cả PDF lẫn Excel |
| TLS.31 | Nhật ký hệ thống ghi việc xuất dữ liệu | Sau khi xuất gói, vào Nhật ký hệ thống lọc đối tượng `DigitalDocument` | Có dòng hành động Xuất |
| TLS.32 | Phân quyền | Đăng nhập tài khoản bạn đọc rồi gọi `POST /api/digital/requests/search` | Trả 403 kèm tên mã quyền còn thiếu |

## Nhóm chức năng — Liên thư viện (mục 2.4)

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| LTV.1 | Ba máy chủ nạp sẵn | Liên thư viện → Máy chủ thư viện bạn | Có Thư viện Quốc hội Mỹ theo cả hai lối Z39.50 và SRU, cùng một máy chủ của Đại học Yale |
| LTV.2 | Kiểm tra kết nối thật | Bấm Kiểm tra ở dòng Thư viện Quốc hội Mỹ (SRU) | Báo kết nối tốt kèm số kết quả tra thử và thời gian trả lời |
| LTV.3 | Kiểm tra máy chủ hỏng | Khai một máy chủ với địa chỉ không có thật rồi bấm Kiểm tra | Báo hỏng kèm lý do cụ thể, và trạng thái được ghi lại trên danh sách |
| LTV.4 | Khai báo thiếu thông tin | Thêm máy chủ mà bỏ trống địa chỉ | Bị chặn kèm thông báo rõ trường nào còn thiếu |
| LTV.5 | Sửa máy chủ, bỏ trống mật khẩu | Sửa một máy chủ đang có mật khẩu, để trống ô mật khẩu rồi lưu | Mật khẩu cũ được giữ nguyên |
| LTV.6 | Tra cứu Z39.50 thật | Tra cứu liên thư viện, nhan đề "library science", lấy 10 biểu ghi | Máy chủ Thư viện Quốc hội Mỹ trả về số kết quả và biểu ghi đọc được, tiếng Anh có dấu phụ hiển thị đúng |
| LTV.7 | Một máy chủ hỏng không làm hỏng cả lượt | Tra cứu khi trong danh sách có máy chủ hỏng | Máy chủ hỏng báo lỗi riêng, các máy chủ còn lại vẫn trả kết quả |
| LTV.8 | Đối chiếu với kho của mình | Tra cứu một nhan đề mà thư viện đã có | Dòng đó mang nhãn "Thư viện mình đã có" |
| LTV.9 | Nhập biểu ghi về | Bấm Nhập vào ở một dòng kết quả | Mở trình soạn MARC với biểu ghi đã bỏ số kiểm soát của thư viện bạn và có trường 035 ghi nguồn; kho chưa có gì cho tới khi bấm lưu |
| LTV.10 | SRU trả bản tự khai | Mở `/sru` không kèm tham số | Trả về `explainResponse` liệt kê các chỉ mục tra được và lược đồ biểu ghi hỗ trợ |
| LTV.11 | SRU tra cứu trả MARCXML | Mở `/sru?operation=searchRetrieve&version=1.2&query=dc.title="…"&recordSchema=marcxml` | Trả biểu ghi trong không gian tên `http://www.loc.gov/MARC21/slim`, đọc lại được bằng chính chức năng nhập MARCXML của sản phẩm |
| LTV.12 | SRU tra cứu gõ không dấu | Tra cùng nhan đề tiếng Việt nhưng gõ không dấu | Vẫn ra đúng biểu ghi |
| LTV.13 | SRU trả Dublin Core | Thêm `&recordSchema=dc` | Trả biểu ghi Dublin Core đúng không gian tên |
| LTV.14 | SRU phân trang | Thêm `&startRecord=3&maximumRecords=2` | Trả đúng phần biểu ghi tương ứng; hết biểu ghi thì không còn `nextRecordPosition` |
| LTV.15 | SRU truy vấn sai cú pháp | Gửi truy vấn thiếu dấu nháy kép đóng | Trả phần chẩn đoán đúng chuẩn, không phải lỗi máy chủ |
| LTV.16 | OAI-PMH Identify | Mở `/oai?verb=Identify` | Tên kho lấy từ tham số hệ thống, phiên bản giao thức 2.0 |
| LTV.17 | OAI-PMH ListMetadataFormats | Mở `/oai?verb=ListMetadataFormats` | Có cả `oai_dc` và `marc21` |
| LTV.18 | OAI-PMH ListSets | Mở `/oai?verb=ListSets` | Liệt kê bộ theo dạng tài liệu, mã dạng `doctype:MÃ` |
| LTV.19 | OAI-PMH ListRecords | Mở `/oai?verb=ListRecords&metadataPrefix=oai_dc` | Trả biểu ghi Dublin Core; kho nhiều hơn một trang thì có thẻ đọc tiếp và thẻ đó dùng lại được |
| LTV.20 | OAI-PMH ListIdentifiers | Mở `/oai?verb=ListIdentifiers&metadataPrefix=oai_dc` | Chỉ có phần đầu biểu ghi, không kèm nội dung |
| LTV.21 | OAI-PMH GetRecord | Mở `/oai?verb=GetRecord&metadataPrefix=marc21&identifier=oai:…` | Trả đúng một biểu ghi dạng MARCXML |
| LTV.22 | OAI-PMH lọc theo thời gian | Thêm `&from=…&until=…` vào một khoảng quá khứ không có dữ liệu | Trả mã `noRecordsMatch` |
| LTV.23 | OAI-PMH tham số sai | Gọi thiếu verb, verb lạ, thiếu identifier, định dạng lạ, thẻ đọc tiếp giả mạo | Lần lượt trả `badVerb`, `badVerb`, `badArgument`, `cannotDisseminateFormat`, `badResumptionToken` |
| LTV.24 | OAI-PMH nhận POST | Gửi POST dạng biểu mẫu với `verb=Identify` | Trả về đúng như gọi bằng GET |
| LTV.25 | Khai kho OAI-PMH và hỏi thử | Liên thư viện → Kho OAI-PMH → Thêm kho, nhập địa chỉ rồi bấm Hỏi thử | Hiện tên kho, phiên bản giao thức, các định dạng và các bộ kho đó có |
| LTV.26 | Địa chỉ kho sai | Nhập một chuỗi không phải địa chỉ HTTP | Bị chặn kèm lý do |
| LTV.27 | Kho không kết nối được | Khai một địa chỉ HTTP không tồn tại rồi bấm Hỏi thử | Báo không kết nối được kèm địa chỉ, không phải lỗi hệ thống chung chung |
| LTV.28 | Thu hoạch thật | Bấm Thu hoạch ở một kho đã khai | Nhật ký ghi số biểu ghi lấy về, nhập được và bỏ qua |
| LTV.29 | Thu hoạch lại không tạo bản trùng | Bấm Nạp lại ở cùng kho đó | Số biểu ghi nhập được bằng 0, toàn bộ chuyển sang bỏ qua |
| LTV.30 | Biểu ghi thu về chờ biên mục | Tìm biểu ghi vừa thu về trong Biên mục | Ở trạng thái chờ biên mục, có trường 035 ghi định danh kho nguồn và 040 ghi tên kho |
| LTV.31 | Máy chủ Z39.50 của mình | Bật `ILL.Z3950_SERVER_ENABLED`, khởi động lại, rồi tra vào cổng đã khai bằng một máy khách Z39.50 bất kỳ | Bắt tay được, tra ra biểu ghi và lấy về dạng ISO 2709 |
| LTV.32 | Giới hạn IP tra vào | Khai `ILL.Z3950_ALLOWED_IPS` một dải không chứa máy của mình rồi tra lại | Kết nối bị từ chối |
| LTV.33 | Phân quyền | Đăng nhập tài khoản không có quyền rồi mở phần khai báo máy chủ | Trả 403 kèm tên mã quyền còn thiếu |

## Nhóm chức năng — Phân hệ VIII: Quản trị nội dung

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| QTND.1 | Màn hình cấu hình gom đủ hai kho | Quản trị nội dung → Thông tin trang thư viện | Thấy cả tên thư viện (tham số hệ thống) lẫn giờ mở cửa (cấu hình riêng của trang tra cứu) trên cùng một màn hình |
| QTND.2 | Sửa tên thư viện | Đổi tên rồi Lưu, mở trang tra cứu | Đầu trang, chân trang và tiêu đề thẻ trình duyệt đều đổi theo, không phải khởi động lại |
| QTND.3 | Sửa khẩu hiệu | Đổi khẩu hiệu rồi Lưu, tải lại trang tra cứu | Khẩu hiệu mới hiện dưới tên thư viện |
| QTND.4 | Tải logo lên | Bấm Tải ảnh ở ô Logo, chọn một tệp PNG | Ảnh hiện ngay ở ô xem trước và đường dẫn được điền vào |
| QTND.5 | Tải tệp không phải ảnh | Chọn một tệp .txt đổi đuôi thành .png | Bị từ chối kèm lý do, vì kiểu tệp xác định bằng chữ ký nhị phân chứ không bằng phần mở rộng |
| QTND.6 | Thêm mục menu | Menu điều hướng → Thêm mục menu, nhập tên và đường dẫn | Mục mới hiện trên thanh điều hướng của trang tra cứu |
| QTND.7 | Menu nhiều cấp | Thêm một mục có mục cha | Hiện thành nhánh con trên cây quản trị và thành menu thả xuống ở trang tra cứu |
| QTND.8 | Tắt mục cha | Tắt một mục có nhánh con rồi mở trang tra cứu | Cả nhánh biến mất, mục con không bị đẩy lên hàng đầu |
| QTND.9 | Chặn vòng trong cây menu | Đặt mục cha của A là B, trong khi B đang là con của A | Bị chặn kèm thông báo rõ nghĩa |
| QTND.10 | Xóa mục còn nhánh con | Xóa một mục đang có mục con | Bị chặn, nhắc xóa hoặc chuyển mục con trước |
| QTND.11 | Thêm banner có khoảng ngày | Banner trang chủ → Thêm, đặt ngày kết thúc trong quá khứ | Banner không hiện ở trang chủ nhưng vẫn nằm trong danh sách quản trị để sửa |
| QTND.12 | Ngày kết thúc trước ngày bắt đầu | Nhập ngược khoảng ngày | Bị chặn kèm thông báo |
| QTND.13 | Thêm liên kết website | Liên kết website → Thêm, nhập địa chỉ không có http | Bị chặn; nhập đúng địa chỉ thì liên kết hiện ở khối Liên kết hữu ích trang chủ |
| QTND.14 | Tạo trang tĩnh, để bản nháp | Trang tĩnh → Thêm trang, tắt Đăng ngay | Mở đường dẫn công khai của trang đó trả về không tìm thấy |
| QTND.15 | Đăng trang tĩnh | Bật Đăng rồi lưu | Trang đọc được ở `/trang/<đường-dẫn>` |
| QTND.16 | Đường dẫn tự sinh | Tạo trang "Nội quy thư viện" và bỏ trống ô đường dẫn | Đường dẫn thành `noi-quy-thu-vien` |
| QTND.17 | Đường dẫn trùng | Tạo hai trang cùng tiêu đề | Trang thứ hai được nối thêm số thứ tự, không báo lỗi bắt cán bộ tự nghĩ tên khác |
| QTND.18 | Lọc mã độc trong nội dung | Dán vào nội dung một thẻ `<script>`, một thuộc tính `onerror` và một liên kết `javascript:` rồi lưu | Mở lại bài thấy phần chữ còn nguyên nhưng cả ba thứ trên đã bị bỏ |
| QTND.19 | Chèn ảnh vào bài | Trong trình soạn thảo bấm nút ảnh và chọn một tệp | Ảnh được tải lên kho và chèn vào bài dưới dạng đường dẫn, không nhét ảnh vào giữa nội dung |
| QTND.20 | Chèn bảng và tiêu đề | Bấm Tiêu đề lớn rồi bấm chèn bảng | Nội dung thành thẻ `h2` và một bảng 3×3 có dòng tiêu đề |
| QTND.21 | Nhúng video | Dán địa chỉ xem YouTube vào hộp nhúng video | Được đổi sang địa chỉ nhúng; dán địa chỉ nơi khác thì bị từ chối kèm lý do |
| QTND.22 | Xem mã HTML | Bấm nút mã nguồn | Chuyển sang ô sửa HTML thô và sửa được trực tiếp |
| QTND.23 | Soạn và đăng bản tin | Tin tức → Soạn bản tin, bật Đăng | Bản tin hiện ở trang chủ và trang tin tức |
| QTND.24 | Hẹn giờ đăng | Đặt thời điểm đăng vào tương lai | Danh sách quản trị ghi "Hẹn ngày giờ"; trang tra cứu chưa hiện bài đó |
| QTND.25 | Tóm tắt tự sinh | Soạn bài và bỏ trống ô tóm tắt | Tóm tắt lấy đoạn đầu của bài, cắt theo ranh giới từ |
| QTND.26 | Gỡ bản tin | Bấm Gỡ ở một bài đã đăng | Bài biến mất khỏi trang tra cứu nhưng vẫn còn trong danh sách quản trị |
| QTND.27 | Thống kê lượt xem tin | Tin tức → Thống kê lượt xem | Có tổng số bài, số đã đăng, tổng lượt xem, phân bổ theo chuyên mục và danh sách bài xem nhiều nhất |
| QTND.28 | Tạo album ảnh | Thư viện ảnh → Tạo album, thêm vài ảnh, để trống ảnh bìa | Album lấy ảnh đầu tiên làm bìa |
| QTND.29 | Kiểm duyệt nhận xét | Nhận xét bạn đọc → thẻ Chờ duyệt → Duyệt một nhận xét | Nhận xét hiện ở trang chi tiết tài liệu; bỏ duyệt thì ẩn đi |
| QTND.30 | Phân quyền quản trị nội dung | Đăng nhập tài khoản chỉ có quyền xem tin rồi mở Thông tin trang thư viện | Menu không hiện mục đó; gọi thẳng địa chỉ trả về 403 |

## Nhóm chức năng — Phân hệ IX: Tra cứu (OPAC)

| Mã | Kịch bản | Các bước | Kết quả mong đợi |
|---|---|---|---|
| TC.1 | Mở trang chủ | Mở địa chỉ gốc của hệ thống | Hiện tên và khẩu hiệu của thư viện, ô tìm kiếm lớn, các con số quy mô kho, sách mới, tin tức và liên kết |
| TC.2 | Thương hiệu lấy từ cấu hình | Đổi tên thư viện ở màn hình quản trị rồi tải lại trang chủ | Tên mới hiện ngay; không có tên trường nào nằm cứng trong mã nguồn |
| TC.3 | Tra cứu gõ không dấu | Gõ "co so du lieu" vào ô tìm kiếm | Ra tài liệu có nhan đề "Cơ sở dữ liệu" |
| TC.4 | Chọn phạm vi tìm | Chọn phạm vi Tác giả rồi tìm tên một tác giả | Chỉ ra tài liệu của tác giả đó |
| TC.5 | Gợi ý khi gõ | Gõ hai ký tự đầu của một nhan đề | Hiện danh sách gợi ý phân biệt rõ nhan đề, tác giả, chủ đề |
| TC.6 | Bộ lọc facet đếm đúng | Tra một từ khóa rồi đối chiếu con số ở mỗi dòng lọc | Tổng số đếm không vượt quá số kết quả; bấm vào một dòng lọc thì ra đúng chừng ấy tài liệu |
| TC.7 | Bấm bộ lọc rồi quay lại | Bấm một giá trị facet, sau đó bấm nút quay lại của trình duyệt | Trở về đúng kết quả trước đó, vì trạng thái tra cứu nằm trên địa chỉ |
| TC.8 | Sắp xếp kết quả | Đổi cách sắp xếp sang Mới nhất rồi sang Được mượn nhiều | Thứ tự đổi theo, phân trang giữ nguyên số kết quả |
| TC.9 | Tra cứu nâng cao với VÀ | Nhan đề chứa X VÀ tác giả chứa Y | Chỉ ra tài liệu thỏa mãn cả hai |
| TC.10 | Tra cứu nâng cao với HOẶC | Nhan đề chứa X HOẶC nhan đề chứa Y | Ra hợp của hai tập |
| TC.11 | Tra cứu nâng cao với KHÔNG | Nhan đề chứa X KHÔNG nhan đề chứa Y | Loại đúng phần cần bỏ |
| TC.12 | Giới hạn năm xuất bản | Đặt khoảng năm rồi tra | Chỉ ra tài liệu trong khoảng |
| TC.13 | Chi tiết tài liệu | Mở một tài liệu từ kết quả | Có mô tả thư mục, mô tả ISBD, chủ đề bấm được, tài liệu liên quan |
| TC.14 | Bản in trong kho | Mở thẻ Bản in trong kho | Mỗi bản ghi rõ tình trạng, ký hiệu xếp giá, kho, giá và thư viện; bản đang có người mượn hiện hạn trả dự kiến |
| TC.15 | Biểu ghi MARC | Mở thẻ Biểu ghi MARC | Hiện biểu ghi MARC 21 đầy đủ dạng đọc được |
| TC.16 | Xuất trích dẫn | Bấm Xuất trích dẫn rồi đổi lần lượt sáu kiểu | Cả sáu kiểu đều sinh ra nội dung đúng dạng; tệp RIS tải về mở đầu bằng `TY  - BOOK` |
| TC.17 | Biểu ghi chưa xuất bản | Mở địa chỉ chi tiết của một biểu ghi còn ở trạng thái nháp | Trả về không tìm thấy, và tra cứu cũng không ra nó |
| TC.18 | Duyệt theo phân loại | Duyệt theo khung phân loại, mở một nhánh | Số đếm ở nhánh cha bằng tổng cả nhánh con; chỉ hiện mục có tài liệu hoặc có nhánh con |
| TC.19 | Duyệt theo tác giả | Duyệt theo tác giả, chọn một chữ cái | Chỉ hiện tác giả bắt đầu bằng chữ đó, so trên tên đã bỏ dấu |
| TC.20 | Duyệt theo ngành và môn học | Duyệt theo ngành → chọn ngành → chọn môn | Hiện tài liệu của môn kèm nhãn giáo trình chính hay tài liệu tham khảo |
| TC.21 | Danh mục luận văn – luận án | Mở mục Luận văn – Luận án | Chỉ liệt kê tài liệu thuộc dạng luận văn, luận án |
| TC.22 | Danh mục báo – tạp chí | Mở mục Báo – Tạp chí | Liệt kê đầu báo kèm ISSN, kỳ hạn, kho lưu, số đã nhận và số mới nhất |
| TC.23 | Giỏ tài liệu khi chưa đăng nhập | Thêm vài tài liệu vào giỏ rồi tải lại trang | Giỏ vẫn còn, vì giỏ giữ ở máy người dùng |
| TC.24 | Gửi giỏ tài liệu qua email | Bấm gửi khi chưa đăng nhập | Được chuyển sang trang đăng nhập; sau khi đăng nhập thì thư đi tới địa chỉ trong hồ sơ bạn đọc |
| TC.25 | Đăng nhập bạn đọc | Nhập số thẻ và mật khẩu | Vào trang cá nhân, đầu trang hiện tên bạn đọc |
| TC.26 | Sai mật khẩu | Nhập sai mật khẩu | Báo lỗi không nói rõ sai phần nào |
| TC.27 | Trang cá nhân | Mở lần lượt tám thẻ | Sách đang mượn, lịch sử, đặt giữ, tiền phạt, thông báo, yêu thích, tìm kiếm đã lưu, thông tin cá nhân — không thẻ nào hiện tên trạng thái bằng tiếng Anh |
| TC.28 | Gia hạn sách | Bấm Gia hạn ở một cuốn đang mượn | Hạn trả mới do máy chủ tính; hết lượt thì nút khóa và ghi rõ đã dùng mấy trên mấy lượt |
| TC.29 | Đặt giữ chỗ | Mở một tài liệu rồi bấm Đặt giữ chỗ | Báo vị trí trong hàng đợi; phiếu hiện ở thẻ Đặt giữ và hủy được |
| TC.30 | Cập nhật thông tin liên hệ | Sửa email và điện thoại rồi lưu | Lưu được; các trường do nhà trường quản lý không có ô để sửa |
| TC.31 | Yêu cầu gia hạn thẻ | Gửi yêu cầu rồi gửi tiếp lần hai | Lần hai bị chặn vì đang có yêu cầu chờ xử lý |
| TC.32 | Nhận xét tài liệu | Gửi nhận xét rồi mở lại trang chi tiết bằng cửa sổ ẩn danh | Chưa thấy nhận xét; sau khi cán bộ duyệt thì thấy |
| TC.33 | Danh sách tài liệu số | Mở mục Tài liệu số | Liệt kê tài liệu số kèm mức truy cập, số trang và dung lượng |
| TC.34 | Đọc trực tuyến | Mở một tài liệu công khai | Trang tài liệu hiện dưới dạng ảnh có chữ chìm ghi số thẻ, thời điểm và địa chỉ máy; lật trang được |
| TC.35 | Tài liệu không cho tải | Mở một tài liệu bị cấm tải | Không có nút tải về, kèm dòng giải thích |
| TC.36 | Tài liệu hạn chế | Mở một tài liệu mức hạn chế khi chưa được duyệt | Chỉ xem được số trang thử đã cấu hình, có nút gửi yêu cầu kèm ô ghi lý do |
| TC.37 | Tìm ở thư viện khác | Mở mục Tìm ở thư viện khác, tra một từ khóa | Bảng gộp mọi thư viện đứng trước, cột "Nguồn" ghi tên từng nơi; bên dưới là khối riêng của từng thư viện kèm số kết quả và thời gian trả lời |
| TC.38 | Đối chiếu với kho của mình | Tra một nhan đề thư viện đã có | Cột "Ở thư viện mình" có liên kết mở thẳng sang trang chi tiết |
| TC.39 | Sơ đồ trang | Mở `/sitemap.xml` | Liệt kê trang tĩnh, bản tin, tài liệu đã xuất bản và đủ mười một trang duyệt công khai |
| TC.40 | Lọc nâng cao theo ngôn ngữ, dạng tài liệu, kho | Tra cứu nâng cao → khối "Giới hạn kết quả" → chọn một ngôn ngữ | Ô chọn chỉ liệt kê giá trị đang có tài liệu, kèm số lượng; kết quả thu hẹp đúng theo lựa chọn |
| TC.41 | Duyệt theo môn học | Trang chủ → Danh mục tra cứu → Môn học → chọn chữ cái đầu → chọn một môn | Danh sách môn lọc theo chữ cái; chọn môn thì ra trang kết quả lọc đúng môn ấy |
| TC.42 | Tài liệu của môn quá một trang | Duyệt theo ngành → chọn môn có trên 20 tài liệu | Có thanh phân trang, sang trang 2 xem được phần còn lại |
| TC.43 | Giờ mở cửa từng cơ sở | Khai giờ mở cửa khác nhau cho hai cơ sở ở màn hình Thư viện → mở trang tra cứu | Chân trang liệt kê từng cơ sở kèm giờ của nó; trang Liên hệ có khối "Các cơ sở" với địa chỉ, điện thoại và lối chỉ đường |
| TC.40 | Tệp robots.txt | Mở `/robots.txt` | Mở phần tra cứu, chặn `/admin` và các trang cá nhân, có dòng trỏ tới sơ đồ trang |
| TC.41 | Gọi API bạn đọc khi chưa đăng nhập | Gọi `/api/reader/profile` không kèm mã đăng nhập | Trả 401 |
| TC.42 | Tài khoản bạn đọc gọi API quản trị | Dùng mã đăng nhập của bạn đọc gọi `/api/content/settings` | Trả 403 |

## Nhóm 2.2 — Phân hệ X: Tài liệu môn học

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| MH.1 | Danh mục ngành có sẵn | Vào Danh mục → Ngành đào tạo | Có sẵn 6 ngành mẫu, ngành nào cũng ghi rõ khoa quản lý | Integration — `CourseDocumentTests` | | |
| MH.2 | Chọn khoa quản lý cho ngành | Sửa một ngành, mở ô Khoa quản lý | Là ô chọn lấy từ danh mục Khoa, không phải ô gõ tay | | | |
| MH.3 | Danh mục môn học có sẵn | Vào Tài liệu môn học → Gán tài liệu cho môn học | Có sẵn 14 môn mẫu, mỗi môn ghi số tín chỉ và học kỳ | Integration — `CourseDocumentTests` | | |
| MH.4 | Một môn thuộc nhiều ngành | Mở môn Tin học đại cương | Hiện đủ 6 ngành cùng học môn này | Integration — `CourseDocumentTests` | | |
| MH.5 | Sửa danh sách ngành của môn | Bấm nút Ngành đào tạo, bỏ một ngành rồi lưu | Lưu được, danh sách ngành ở cột trái đổi theo ngay | Integration — `CourseDocumentTests` | | |
| MH.6 | Lọc môn theo ngành | Chọn một ngành ở ô lọc | Chỉ còn môn thuộc ngành đó | Integration — `CourseDocumentTests` | | |
| MH.7 | Lọc môn chưa có tài liệu | Bật nút "Chỉ môn chưa có tài liệu" | Chỉ còn môn chưa được gán tài liệu nào | Integration — `CourseDocumentTests` | | |
| MH.8 | Tìm và gán tài liệu | Chọn một môn, gõ từ khóa ở ô bên phải rồi Enter, tick vài cuốn, chọn mức độ và bấm Gán tài liệu | Các cuốn đã chọn xuất hiện trong bảng tài liệu của môn với đúng mức độ | Integration — `CourseDocumentTests` | | |
| MH.9 | Gán lại cuốn đã có | Gán lại một cuốn đang có, chọn mức độ khác | Số dòng không tăng, dòng cũ đổi sang mức độ mới | Integration — `CourseDocumentTests` | | |
| MH.10 | Sửa mức độ ngay trên dòng | Đổi ô mức độ của một dòng trong bảng | Lưu ngay, không phải bỏ ra gán lại | Integration — `CourseDocumentTests` | | |
| MH.11 | Bỏ tài liệu khỏi môn | Bấm nút xóa ở một dòng rồi xác nhận | Dòng biến mất, số tài liệu của môn giảm một | Integration — `CourseDocumentTests` | | |
| MH.12 | Tệp Excel mẫu | Bấm Tải tệp mẫu | Tải về tệp xlsx có sẵn tiêu đề tiếng Việt và dòng hướng dẫn | Integration — `CourseDocumentTests` | | |
| MH.13 | Kiểm tra thử tệp Excel | Nhập từ Excel một tệp có cả dòng đúng lẫn dòng sai, chọn kiểm tra thử | Bảng kết quả nêu rõ dòng nào hỏng vì lý do gì; chưa ghi gì vào dữ liệu | Integration — `CourseDocumentTests` | | |
| MH.14 | Nhập thật từ Excel | Nhập lại tệp đó, bỏ chọn kiểm tra thử | Dòng đúng được nhập, dòng hỏng bị bỏ qua kèm lý do, không chặn cả tệp | Integration — `CourseDocumentTests` | | |
| MH.15 | Đối chiếu tài liệu khi nhập | Trong tệp Excel, ghi mã tài liệu bằng ISBN, bằng số kiểm soát và bằng số ĐKCB | Cả ba cách đều tìm đúng tài liệu | Integration — `CourseDocumentTests` | | |
| MH.16 | Cột mức độ ghi kiểu khác nhau | Trong tệp Excel, ghi "Giáo trình chính", "giao trinh chinh", "GIÁO TRÌNH" | Cả ba đều hiểu là giáo trình chính | Unit — `CourseRulesTests` | | |
| MH.17 | Cột mức độ bỏ trống | Trong tệp Excel, để trống cột Mức độ | Hiểu là tài liệu tham khảo bắt buộc | Unit — `CourseRulesTests` | | |
| MH.18 | Báo cáo môn chưa có tài liệu | Mở Báo cáo tài liệu môn học, thẻ "Môn chưa có tài liệu" | Liệt kê đúng các môn chưa được gán, lọc được theo ngành | Integration — `CourseDocumentTests` | | |
| MH.19 | Báo cáo tài liệu dùng nhiều môn | Mở thẻ "Tài liệu dùng nhiều môn" | Mỗi dòng ghi số môn đang dùng và số bản còn rảnh; số bản ít hơn số môn thì tô đỏ | Integration — `CourseDocumentTests` | | |
| MH.20 | Báo cáo đáp ứng theo ngành | Mở thẻ "Đáp ứng theo ngành" | Bảng và biểu đồ cột khớp nhau; tỷ lệ làm tròn một chữ số thập phân | Integration — `CourseDocumentTests` | | |
| MH.21 | Ngành chưa khai môn học | Xem dòng của một ngành chưa có môn nào | Tỷ lệ đáp ứng là 0%, không báo lỗi chia cho không | Unit — `CourseRulesTests` | | |
| MH.22 | Xuất báo cáo ra Excel | Bấm Xuất Excel | Tải về tệp xlsx đủ ba phần báo cáo, theo đúng bộ lọc đang đặt | Integration — `CourseDocumentTests` | | |
| MH.23 | Xuất báo cáo ra PDF | Bấm Xuất PDF | Tải về tệp PDF mở được, tiếng Việt hiển thị đúng dấu | Integration — `CourseDocumentTests` | | |
| MH.24 | Bạn đọc duyệt theo ngành | Trên trang tra cứu, mở Duyệt theo ngành đào tạo | Mỗi ngành một thẻ kèm số môn học | | | |
| MH.25 | Bạn đọc xem tài liệu của môn | Chọn một ngành rồi chọn một môn | Hiện tài liệu của môn kèm mức độ và số bản còn rảnh; đầu trang ghi rõ đang ở ngành nào | Integration — `CourseDocumentTests` | | |
| MH.26 | Bạn đọc chỉ thấy tài liệu đã xuất bản | Gán một biểu ghi còn ở trạng thái nháp cho một môn rồi xem trên trang tra cứu | Biểu ghi nháp không hiện ra | Integration — `CourseDocumentTests` | | |
| MH.27 | Thao tác bằng bàn phím | Ở cả hai trang, dùng phím Tab tới thẻ ngành hoặc dòng môn học rồi bấm Enter | Mở được mục đang chọn mà không cần chuột | UI — `clickable.test.ts` | | |
| MH.28 | Gọi API môn học khi chưa đăng nhập | Gọi `/api/courses` không kèm mã đăng nhập | Trả 401 | | | |
| MH.29 | Tài khoản không đủ quyền | Dùng mã đăng nhập của bạn đọc gọi `/api/courses` và `/api/courses/reports` | Trả 403 ở cả hai, vì hai đường dẫn này đòi quyền `COURSE.COURSE.MANAGE` và `COURSE.REPORT.VIEW` | Integration — `CourseDocumentTests` | | |

---

| 2.3.16 | Tài khoản còn mật khẩu tạm | Tạo một tài khoản cán bộ mới, đăng nhập bằng mật khẩu tạm rồi gọi thẳng một API nghiệp vụ (ví dụ bằng Postman) | Trả 403 kèm câu "Tài khoản phải đổi mật khẩu trước khi sử dụng hệ thống"; sau khi đổi mật khẩu thì gọi được bình thường | Integration — `PermissionAndAuditTests` | | |

| 2.8.1 | Báo cáo thống kê tổng quan | Vào Báo cáo thống kê, chọn kỳ "Năm nay" | Bảy khối chỉ tiêu của bảy phân hệ, con số khớp với báo cáo riêng của từng phân hệ | Integration — `SystemReportTests` | | |
| 2.8.2 | Biểu đồ xu hướng | Mở thẻ "Xu hướng 12 tháng" | Ba đường: lượt mượn, bản nhập kho, thẻ mới; đủ 12 cột, tháng không có giao dịch hiện 0 | Integration — `SystemReportTests` | | |
| 2.8.3 | Mục lục báo cáo | Mở thẻ "Mục lục báo cáo" | Liệt kê báo cáo của mọi phân hệ; tài khoản không có quyền xem báo cáo nào thì không thấy dòng đó | UI — `reports/types.test.ts` | | |
| 2.8.4 | Xuất bảng tổng quan | Bấm Xuất Excel rồi Xuất PDF | Tải về hai tệp mở được, số liệu theo đúng kỳ đang chọn | Integration — `SystemReportTests` | | |
| 2.8.5 | Mở giao diện quản trị trên điện thoại | Mở `/admin` bằng trình duyệt điện thoại (bề ngang 390px) | Menu nằm trong ngăn kéo mở từ nút góc trái; không màn hình nào phải cuộn ngang cả trang; bảng nhiều cột cuộn trong khung của nó | UI — `layoutBreakpoints.test.ts` | | |

## Nhóm 2.5 — Chuyển đổi dữ liệu: bộ dữ liệu minh họa

Kiểm trên hệ thống vừa cài xong bằng `docker compose up -d`, chưa nhập gì thêm.

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| CD.1 | Nạp dữ liệu minh họa | Đếm số bản ghi trong psql sau lần khởi động đầu | 200 biểu ghi tài liệu, 500 ĐKCB, 50 bạn đọc, 100 lượt mượn trả, 52 liên kết tài liệu môn học | | | |
| CD.1b | Ấn phẩm định kỳ có dữ liệu | Vào Ấn phẩm định kỳ → Báo, tạp chí | 5 đầu báo với bốn kỳ hạn khác nhau, 113 số gồm cả số đã nhận, số thiếu và số dự kiến; 4 phiếu khiếu nại | | | |
| CD.1c | Tài liệu số có dữ liệu | Vào Tài liệu số → Kho tài liệu số | 6 tài liệu PDF thật, đủ ba mức truy cập công khai / nội bộ / hạn chế | | | |
| CD.1d | Trình đọc mở được tài liệu minh họa | Trên trang tra cứu, mở một tài liệu công khai và bấm Đọc trực tuyến | Trang tài liệu hiện dưới dạng ảnh có chữ chìm, lật trang được, tiếng Việt hiển thị đúng dấu | | | |
| CD.2 | Quan hệ giữa các bảng | Mở một biểu ghi bất kỳ, xem thẻ Ấn phẩm | Mỗi đầu có 2–3 bản, mỗi bản có số ĐKCB, mã vạch và ký hiệu xếp giá theo đúng quy tắc đang khai | | | |
| CD.3 | Dữ liệu lưu thông có đủ trạng thái | Vào Lưu thông → Báo cáo, lọc toàn bộ thời gian | 80 lượt đã trả, 15 lượt đang mượn, 5 lượt quá hạn; 20 khoản phạt trong đó một nửa đã thu | | | |
| CD.4 | Bạn đọc đăng nhập được | Đăng nhập trang tra cứu bằng một số thẻ bất kỳ trong danh sách, mật khẩu `BanDoc@2025` | Vào được trang cá nhân, có lịch sử mượn trả | | | |
| CD.5 | Dữ liệu minh họa không đè lên dữ liệu thật | Nhập một biểu ghi rồi khởi động lại hệ thống | Không nạp thêm gì; bộ minh họa chỉ chạy khi kho còn trống | | | |
| CD.6 | Tắt hẳn dữ liệu minh họa | Đặt `LC_SEED_DEMO=false` trong `.env`, cài mới | Kho trống hoàn toàn, nhật ký ghi "Bỏ qua dữ liệu minh họa theo cấu hình SEED_DEMO" | | | |

---

## Nhóm 2.2 — Hiệu năng và tải (yêu cầu 6.3)

Đo trên bộ dữ liệu 500.000 biểu ghi (nhân bản từ bộ minh họa), máy chạy Docker Desktop, 2 nhân CPU.
Số liệu ghi trong cột kết quả là số đo lần bàn giao; hội đồng đo lại trên máy chủ thật sẽ tốt hơn.

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Tự động | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|---|
| HN.1 | Tra cứu trên kho 500.000 biểu ghi | Gõ một từ khóa thường gặp vào ô tra cứu | Kết quả trả về dưới 1 giây | | 0,5 s | |
| HN.2 | Bộ đếm bộ lọc | Mở trang kết quả, xem cột lọc bên trái | Các con số hiện ra dưới 1 giây | | 0,55 s | |
| HN.3 | Gợi ý tự động | Gõ ba ký tự vào ô tra cứu | Gợi ý hiện dưới 0,5 giây | | 0,37 s | |
| HN.4 | Câu hỏi rộng | Tra một từ khớp hơn 60.000 biểu ghi | Hiện "Tìm thấy hơn 10.000 tài liệu" và vẫn trả về dưới 1 giây | Integration — `ContentAndOpacTests` | 0,9 s | |
| HN.5 | Trang chủ | Mở trang tra cứu | Sách mới và sách được mượn nhiều hiện dưới 1 giây | | 0,06 s | |
| HN.6 | 200 bạn đọc cùng lúc | Chạy kịch bản 200 người, mỗi người tra cứu một lần mỗi 10 giây trong 40 giây | Không có lỗi nào; trung vị dưới 1 giây | | 0 lỗi / 840 ms | |
| HN.7 | Dồn 200 lượt gọi cùng một khoảnh khắc | Bắn 600 lượt từ 200 luồng song song | Không lượt nào lỗi; các lượt xếp hàng chờ chứ không bị từ chối | | 0 lỗi | |
| HN.8 | Chặn tần suất | Gọi liên tục quá ngưỡng từ một địa chỉ IP | Trả 429 kèm đầu đề `Retry-After` và thông báo tiếng Việt | Integration — `RateLimitTests` | | |
| HN.9 | Nâng cấp trên kho lớn | Chạy migration trên cơ sở dữ liệu 500.000 biểu ghi | Migration chạy xong, không đứt vì hết thời gian chờ | | 140 s | |

---

## Nhóm 2.7 — Phân hệ XI: Ứng dụng di động (Phase 15)

Bước 1 — backend bổ sung cho ứng dụng. Mỗi dòng có phép thử tích hợp trong `MobileBackendTests`.

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|
| MB.01 | Phiên bản ứng dụng | `GET /api/public/app-version?platform=ios` | `minVersion`, `latestVersion`, `updateUrl`, `forceUpdate`, `serverTime` theo tham số `MOBILE.APP_*` | Đạt | Đạt |
| MB.02 | Đồng bộ delta | `GET /api/public/news?updatedSince=<ngày mai>`; `GET /api/catalogs/document-types/items?updatedSince=<30 năm trước>`; `GET /api/search?updatedSince=<ngày mai>` | Mốc tương lai → rỗng; mốc rất xa → đủ như không lọc; mọi trang trả `serverTime` | Đạt | Đạt |
| MB.03 | Tuỳ chọn thông báo | `GET/PUT /api/reader/notifications/settings` với `{ "settings": { "NEWS": false, "due_soon": false, "SYSTEM": false } }` | Mặc định bật hết; NEWS và DUE_SOON tắt (chữ thường vẫn hiểu); SYSTEM không có trong danh sách và không tắt được | Đạt | Đạt |
| MB.04 | Thông báo đẩy và thiết bị chết | Bạn đọc đăng ký hai mã thiết bị (một mã Firebase sẽ báo không còn đăng ký) → xin đọc tài liệu hạn chế → cán bộ duyệt | Một lượt đẩy tới cả hai mã kèm `data.kind = DIGITAL_REQUEST` và `data.link`; mã chết bị đánh dấu `is_active = false`; dòng thông báo trong ứng dụng có `type = DIGITAL_REQUEST`; tắt loại này rồi thì từ chối yêu cầu sau không đẩy nữa nhưng vẫn ghi dòng | Đạt | Đạt |
| MB.05 | Xác thực vị trí bằng Wi-Fi | Đặt `VERIFY_MODE = WIFI_SSID`, SSID hợp lệ "LC-Thu-Vien"; gửi SSID lạ; mượn không phiếu; gửi SSID đúng; mượn với phiếu bị sửa; mượn với phiếu đúng | 409 `WIFI_MISMATCH`; 409 `LOCATION_REQUIRED`; nhận phiếu chế độ WIFI_SSID hạn > 10 phút; 409 `LOCATION_INVALID`; mượn thành công, phiếu mượn ghi "xác thực tại lc-thu-vien" | Đạt | Đạt |
| MB.06 | Xác thực vị trí bằng mã QR trạm | Tạo trạm ở `POST /api/circulation/stations`, tải `qr.png`; đặt `VERIFY_MODE = QR_STATION`; quét mã thật, mã bịa, mã của trang web lạ; tắt trạm rồi quét lại | Mã QR dạng `LCST1|MÃ|chữ-ký`, ảnh PNG hợp lệ; phiếu mang mã và tên trạm; 409 `STATION_UNKNOWN` cho hai mã sai; 409 `STATION_INACTIVE` sau khi tắt | Đạt | Đạt |
| MB.07 | Gói đọc ngoại tuyến | Tài liệu cho tải: `POST /api/reader/digital/{id}/offline-package` → tải `downloadUrl` → giải mã AES-256-CBC bằng khoá/IV nhận được; tài liệu chỉ đọc trực tuyến; bạn đọc khác tải gói | Khoá 32 byte, IV 16 byte, hạn ≥ 6 ngày; tệp tải về không bắt đầu bằng `%PDF`, giải mã ra đúng PDF có SHA-256 bằng `checksum`; 403 "chỉ đọc trực tuyến"; người khác 404; danh sách gói ghi `downloadedAt` | Đạt | Đạt |
| MB.08 | Ảnh theo kích thước | `GET /api/public/covers/{id}?w=120` rồi gọi lại kèm `If-None-Match` | Dấu bản kết thúc bằng `-120x0`, khác dấu bản bản đủ; lần hai 304 | Đạt | Đạt |
| MB.09 | Khung ứng dụng — đăng nhập đầu-cuối trên máy ảo | Dựng `flutter build apk --debug` trỏ `LC_API_BASE_URL=http://10.0.2.2/api`, cài lên máy ảo Android 16 (AVD `LC_Pixel`, Pixel 9, x86_64); mở ứng dụng; chạy `flutter test integration_test -d emulator-5556` với thẻ `TV2026000001` | Trang chủ hiện tên thư viện, khẩu hiệu, địa chỉ, giờ mở cửa lấy từ `/api/public/settings`, giao diện tiếng Việt dù máy đặt tiếng Anh; sai mật khẩu hiện đúng câu máy chủ trả ("không đúng"); đăng nhập đúng hiện "Xin chào"; tab Tài khoản hiện số thẻ; đăng xuất về trạng thái khách | Đúng như mong đợi trên máy ảo (phép thử 21 giây, 1/1 xanh). **Chưa chạy trên máy thật**: không có điện thoại nào kết nối máy phát triển | Đạt (máy ảo) |
| MB.10 | Tra cứu không dấu, kết quả, chi tiết năm thẻ | Trên máy ảo: Trang chủ → ô tìm → gõ `co so du lieu` → Enter; mở kết quả "Cơ sở dữ liệu"; xem thẻ Bản in; thẻ MARC | Có dòng "N kết quả" (máy chủ đếm 45); kết quả có nhãn tình trạng; chi tiết có 5 thẻ Thông tin · Bản in · Tài liệu số · MARC · Nhận xét; thẻ Bản in liệt kê mã vạch `LC00000778` "Sẵn sàng" và `LC00000779` "Đang có người mượn" kèm kho; thẻ MARC là bảng nhãn + tên trường tiếng Việt + trường con, không có dấu ngoặc JSON | Đúng như mong đợi (phép thử `integration_test/search_scan_flow_test.dart`, 17 giây). Lỗi thật bắt được khi nhìn màn hình máy ảo: 186/200 biểu ghi có bìa mà ứng dụng chỉ hiện ô chữ cái — bìa dựng sẵn của máy chủ là SVG, `Image.network` không giải mã được; đã chuyển sang một địa chỉ `/api/public/covers/{id}` như trang web, xem byte đầu rồi vẽ bằng `flutter_svg` | Đạt (máy ảo) |
| MB.11 | Quét mã ĐKCB thật → đúng tài liệu; mã lạ → báo rõ | Tab Quét mã → nút bàn phím → nhập `LC00000778` → Tìm; quay lại → nhập `KHONGCO123` | Mở thẳng chi tiết "Cơ sở dữ liệu"; mã lạ hiện "Không tìm thấy tài liệu cho mã KHONGCO123." kèm nút "Tra cứu thủ công", ứng dụng không văng | Đúng như mong đợi. **Camera thật chưa kiểm**: máy ảo không chĩa được vào sách; đường tra `/search/by-barcode` là đường camera cũng đi qua. Lỗi thật bắt được: hộp nhập mã huỷ `TextEditingController` trước khi hoạt ảnh đóng hộp xong → ngoại lệ "used after being disposed", đã sửa | Đạt (máy ảo, chưa camera) |
| MB.12 | Nút hành động theo tình trạng thật | Phép thử widget với máy chủ giả: còn 2/3 bản; hết bản 0/3; 0 ĐKCB; khách bấm đặt giữ; bạn đọc đặt giữ khi hết bản (máy chủ trả `queuePosition = 3`) | "Đặt giữ chỗ" + "2 bản sẵn sàng"; "Xếp hàng đợi" + "Hết bản, đang cho mượn"; không có nút, chỉ "Trích dẫn"; khách rẽ sang đăng nhập với `tiep=/tai-lieu/b1`; bạn đọc thấy "Đã xếp hàng, bạn đứng thứ 3." | Đúng (`test/features/bib_detail_screen_test.dart`, 6 phép thử) | Đạt |
| MB.13 | Trang chủ dữ liệu thật | Mở ứng dụng trên máy ảo (máy chủ Docker, kho 11.686 biểu ghi) | Tên thư viện + khẩu hiệu từ `/api/public/settings`; kệ "Sách mới bổ sung" và "Được mượn nhiều" 8 bìa mỗi kệ từ `/api/public/home`; bảy lối tắt duyệt; tin tức; thẻ thông tin thư viện có nút Gọi (`tel:`) và Chỉ đường (Google Maps theo địa chỉ), năm trang tĩnh; thống kê 11.686 biểu ghi · 17.902 bản in · 4 tài liệu số · 651 bạn đọc; 4 liên kết | Đúng như mong đợi (`integration_test/home_browse_news_flow_test.dart`) | Đạt (máy ảo) |
| MB.14 | Tin tức và trang tĩnh | Kho phát triển vốn **0 tin đã đăng** — đã đăng 2 tin thật qua `POST /api/content/news` (tài khoản admin) để có dữ liệu; chạm tin trên trang chủ; mở "Nội quy thư viện" từ thẻ thông tin | Bài tin hiện ngày · tác giả · lượt xem, nội dung HTML (`<p>`, `<ul>`, `<strong>`) dựng thành chữ, không hiện thẻ HTML thô; trang tĩnh hiện tiêu đề và nội dung | Đúng như mong đợi | Đạt (máy ảo) |
| MB.15 | Duyệt danh mục dạng cây → tra cứu theo mã | Lối tắt "Phân loại DDC" → gõ `tin hoc` vào ô lọc tại chỗ → chạm "Tin học…" (có con) → chạm một mục lá | Danh sách 10 lớp DDC kèm "N tài liệu"; lọc không dấu còn đúng mục; mục cha bung cấp con trong màn hình mới; mục lá mở tab Tra cứu với viên "Đang lọc: …" và dòng "N kết quả" (máy chủ lọc theo `filter.ddc`) | Đúng như mong đợi. Lỗi thật bắt được: `context.push` sang `/tra-cuu` từ màn hình gốc làm go_router trùng khoá trang (Navigator assertion) — chuyển sang `context.go` cho mọi liên kết tác giả/chủ đề/danh mục → tra cứu | Đạt (máy ảo) |
| MB.16 | Sách của tôi — phiếu quá hạn thật, gia hạn bị từ chối | Chưa đăng nhập chạm tab Sách của tôi → rẽ sang đăng nhập (`TV2026000001`) → quay lại tab; bấm Gia hạn trên phiếu `LC00000199` | Phiếu "Bài tập tin học đại cương" tô đỏ "Quá hạn 12 ngày" (số của máy chủ, đã trừ ngày ân hạn), "Phạt dự kiến 24.000 đ", "Đã gia hạn 0/2"; bấm Gia hạn → SnackBar đúng câu máy chủ: "Tài liệu đã quá hạn từ ngày 17/08/2026, phải trả rồi mượn lại." — ứng dụng không tự quyết | Đúng như mong đợi (`integration_test/my_library_flow_test.dart`, 18 giây) | Đạt (máy ảo) |
| MB.17 | Đặt giữ và tiền phạt | Thẻ Đặt giữ; thẻ Tiền phạt | Đặt giữ "Artificial Intelligence: theory and practice" — Đang chờ, "Thứ 1 trong hàng đợi", "Nhận tại Kho Cơ sở 2", "Giữ đến 06/09/2026", nút Hủy đặt giữ (hỏi lại trước khi hủy); Tiền phạt: 0 đ, "Không có khoản phạt nào", hướng dẫn thanh toán tại quầy — ứng dụng không thu tiền | Đúng như mong đợi | Đạt (máy ảo) |
| MB.18 | Thẻ điện tử, kể cả khi không có mạng | Mở thẻ (mạng bình thường); tắt Wi-Fi và dữ liệu máy ảo (`svc wifi disable; svc data disable`), mở lại thẻ; bật mạng lại | Có mạng: mã vạch Code 128 + QR của `TV2026000001`, tên, loại bạn đọc, khoa · lớp, hạn thẻ 05/09/2026, hai cảnh báo của máy chủ (thẻ sắp hết hạn; đang giữ 1 tài liệu quá hạn), nút gửi yêu cầu gia hạn thẻ. Không mạng: vẫn hiện thẻ và mã từ bản lưu trong secure storage, kèm dải "Không có mạng — đang hiện bản lưu trên máy, cập nhật lúc 03:11 03/09", nút gia hạn thẻ bị vô hiệu | Đúng như mong đợi (ảnh chụp máy ảo). Độ sáng màn hình tối đa khi mở thẻ: máy ảo không đổi được, chưa kiểm trên máy thật | Đạt (máy ảo) |
| MB.19 | Mượn tự phục vụ — xác thực trạm QR chặn đúng khi ở ngoài | Máy chủ: `CIRCULATION.SELF_CHECKOUT_ENABLED = true`, `VERIFY_MODE = QR_STATION`, trạm `KHOMO-01`. Ứng dụng (bạn đọc `TV2026000005`): Sách của tôi → nút Mượn tự phục vụ → nhập nội dung mã trạm bịa `LCST1\|GIA\|abc` → nhập nội dung QR thật của trạm | Mã bịa: ô đỏ với câu của máy chủ (409 `STATION_UNKNOWN`), không sang bước quét. Mã thật: dải "Đã xác thực tại Cửa kho mở tầng 2 · Kho mở · Hiệu lực đến HH:mm", mở bước quét sách | Đúng như mong đợi (`integration_test/self_checkout_flow_test.dart`, 21 giây). Camera thật chưa kiểm: nhập nội dung mã bằng tay đi cùng đường `/self-checkout/verify` | Đạt (máy ảo) |
| MB.20 | Mượn tự phục vụ — quét sách thật, phiếu mượn | Nhập mã vạch `LC00000778` (Sẵn sàng); nhập lại cùng mã; Kết thúc | Dòng xanh "Cơ sở dữ liệu · LC00000778 · Đã mượn · hạn trả dd/MM/yyyy" (rung + âm); lần hai báo "đã quét rồi" không gọi máy chủ; phiếu tóm tắt "Đã mượn 1 cuốn". Máy chủ ghi phiếu `PM00003104` kênh Mobile, loại SelfCheckout, ghi chú "xác thực tại KHOMO-01"; phép thử trả sách bằng API quầy nên chạy lại được | Đúng như mong đợi (kiểm bằng `/api/reader/loans/history`). Lỗi thật bắt được: nút Đăng xuất gọi `signOut()` trước khi về trang chủ nên tuyến bảo vệ `/tai-khoan` đẩy sang màn hình đăng nhập — đã đảo thứ tự | Đạt (máy ảo) |
| MB.21 | Đọc tài liệu số trực tuyến, chữ chìm của máy chủ, tìm trong văn bản | Bạn đọc `TV2026000005`: Tài khoản → Tài liệu số → "Bài giảng Nhập môn lập trình" (Công khai) → Đọc; nút tìm → gõ `libraryconnect`; đánh dấu trang | Trang 1/8 là ảnh PNG máy chủ dựng, chữ chìm chéo "TV2026000005 · giờ · IP" (ứng dụng không tự vẽ); tìm không dấu trả "N chỗ khớp" kèm trang và đoạn trích (endpoint mới `GET /api/reader/digital/{id}/find`); dấu trang lưu trên máy | Đúng như mong đợi (`integration_test/digital_flow_test.dart`, 32 giây). Lỗi thật bắt được: trình đọc gọi `L10n.of(context)` trong `initState` → assertion của Flutter, đã chuyển sang sau khung hình đầu | Đạt (máy ảo) |
| MB.22 | Gói đọc ngoại tuyến | Chi tiết tài liệu cho tải → Tải đọc ngoại tuyến → thẻ Ngoại tuyến → mở | Ứng dụng xin gói (`POST …/offline-package`), tải tệp mã hoá, giải mã AES-256-CBC bằng khoá/IV máy chủ cấp, đối chiếu SHA-256 rồi mới ghi **bản mã** lên đĩa; mở lại dựng trang bằng pdfx từ bản rõ trong bộ nhớ, "Trang 1/8", dải "Bản ngoại tuyến — đọc không cần mạng, tự hết hạn"; máy chủ ghi `downloadedAt` và dòng lịch sử `OfflineDownload`; gói hết hạn thì không mở và tự xoá (phép thử đơn vị) | Đúng như mong đợi. Chế độ máy bay chưa kiểm trực tiếp: bản ngoại tuyến không gọi mạng theo mã nguồn, thẻ ngoại tuyến (MB.18) đã kiểm tắt mạng | Đạt (máy ảo) |
| MB.23 | Yêu cầu truy cập tài liệu hạn chế, lịch sử | "Luận án: Mô hình quản trị tri thức" (Hạn chế) → Gửi yêu cầu truy cập, lý do "Làm đề tài tốt nghiệp"; thẻ Yêu cầu; thẻ Lịch sử | Chi tiết hiện lý do quyền của máy chủ, chỉ xem thử N trang; sau khi gửi: "Đã gửi yêu cầu…", thẻ Yêu cầu có dòng "Chờ duyệt", nút gửi ẩn khi đã có yêu cầu chờ; Lịch sử liệt kê View và OfflineDownload với giờ | Đúng như mong đợi (kiểm chéo bằng `/api/reader/digital/requests` và `/history`) | Đạt (máy ảo) |
| MB.24 | Thông báo thật, chạm mở đúng màn hình, cài đặt loại | Cán bộ duyệt yêu cầu tài liệu số của `TV2026000005` (máy chủ sinh thông báo DIGITAL_REQUEST, link `/tai-lieu-so/{id}`); ứng dụng: Tài khoản → chuông (chấm 1) → chạm thông báo → Đọc hết; thẻ Cài đặt tắt "Tin mới" rồi bật lại | Danh sách hiện đúng tiêu đề, nội dung, giờ; chạm mở trang chi tiết tài liệu vừa được duyệt (có nút Đọc); đã đọc → hết chấm; cài đặt lưu ở máy chủ (`GET /reader/notifications/settings` khớp) | Đúng như mong đợi (`integration_test/account_notifications_flow_test.dart`, 51 giây). **Thông báo đẩy thật chưa nhận được**: máy phát triển không có `google-services.json` và máy chủ chưa có khoá dịch vụ FCM; ứng dụng báo rõ "Thông báo đẩy chưa bật trên máy này", mọi bước khác vẫn chạy | Đạt phần trong ứng dụng; đẩy: chưa kiểm |
| MB.25 | Tài khoản: hồ sơ, cập nhật liên hệ, đổi mật khẩu, ngôn ngữ, đăng xuất | Sửa điện thoại `0900000005`; đổi mật khẩu sang tạm rồi đổi lại; chọn English rồi Tiếng Việt; Đăng xuất | Hồ sơ đầy đủ từ `/reader/profile`; điện thoại mới hiện ngay và máy chủ lưu; đổi mật khẩu đúng câu máy chủ, đăng nhập lại bằng mật khẩu gốc được; nhãn đổi "Account" ↔ "Tài khoản", chọn ngôn ngữ/chủ đề/cỡ chữ lưu trên máy qua lần mở sau; đăng xuất về trang chủ và huỷ token thiết bị | Đúng như mong đợi | Đạt (máy ảo) |
| MB.26 | Khoá sinh trắc học | Bật công tắc "Mở khóa bằng vân tay / khuôn mặt" (máy ảo không có vân tay đăng ký) | Máy không có sinh trắc học → báo "Máy này chưa có sinh trắc học", công tắc không bật; phép thử đơn vị: đã bật thì mở ứng dụng ở trạng thái khoá, huỷ xác thực vẫn khoá, nút "Mở khóa" ở màn hình đăng nhập | Đúng theo phép thử đơn vị; trên máy thật chưa kiểm | Đạt một phần |
| MB.27 | Chế độ tối + cỡ chữ lớn nhất, soi tràn chữ | `integration_test/ui_modes_flow_test.dart` qua `flutter drive`: chọn Tối, kéo cỡ chữ 160%, chụp Trang chủ, Tra cứu, Chi tiết, Sách của tôi, Thẻ, Tài khoản | Không dòng nào bị cắt, không nhãn "OVERFLOWED" trong log; bảng màu tối có đủ tương phản cho viên trạng thái | Lỗi thật bắt được: kệ sách trang chủ cao cố định 218 nên tràn 16 điểm ảnh ở 160% → chiều cao theo hệ số cỡ chữ (cả kệ liên quan ở chi tiết). Sau sửa: xanh, log sạch. Ảnh `docs/images/mobile/mb-dark-*.png` | Đạt (máy ảo) |
| MB.28 | Ngoại tuyến toàn ứng dụng | Đăng nhập, mở thẻ, tra "lap trinh" (có mạng) → tắt Wi-Fi + dữ liệu máy ảo → mở lạnh ứng dụng → Sách của tôi → Tra cứu "lap trinh" → bật mạng lại | Dải "Không có kết nối…" trên mọi màn hình; vẫn đăng nhập (phiên khôi phục từ bản lưu thẻ, log `LC restore: network`); Đang mượn hiện bản lưu kèm "cập nhật lúc 04:49 03/09", nút Gia hạn vô hiệu; kết quả tra cứu từ bản lưu kèm giờ, bìa từ đệm đĩa; tên thư viện từ bản lưu; không màn hình trắng, không quay vòng vô tận | Lỗi thật bắt được: (1) `google_fonts` tải phông từ mạng mỗi lần mở, mất mạng rơi về phông hệ thống → đóng gói 6 tệp TTF; (2) dải mất mạng cộng thêm một lần đệm thanh trạng thái → bỏ đệm trên cho màn hình dưới; (3) trang chủ vừa báo lỗi vừa quay vòng. Lưu ý quy trình: `flutter test` gỡ ứng dụng sau khi chạy nên phiên "chuẩn bị" bằng phép thử không còn — phải đăng nhập trên bản thường | Đạt (máy ảo) |
| MB.29 | Khởi động lạnh, hiệu năng | `adb shell am start -W` 4 lần, bản **profile** (AOT) trên máy ảo x86_64 Android 16; bản debug để đối chiếu | TotalTime bản profile: 3.639 · 3.491 · 3.510 · 3.683 ms; bản debug (JIT) ≈ 7.200 ms. Danh sách dùng `ListView.builder`, ảnh bìa đệm đĩa (`flutter_cache_manager`), quay lại màn hình trước không tải lại | Máy ảo không có tăng tốc GPU thật; ngưỡng < 3 s của đặc tả **chưa kiểm được trên máy thật** | Chưa đạt trên máy ảo (3,5 s), máy thật chưa đo |
| MB.30 | Tra cứu nâng cao nhiều điều kiện → facet → sắp xếp (luồng 3) | Khách: Tra cứu → biểu tượng nâng cao → nhan đề "lập trình" VÀ bất kỳ "java" → Tìm; bộ lọc → chọn giá trị facet đầu → Áp dụng; sắp xếp → Mới nhất | Có dòng "N kết quả"; sau facet số kết quả ≤ trước và huy hiệu số bộ lọc hiện; nhãn sắp xếp đổi "Mới nhất" và kết quả nạp lại | Lỗi thật bắt được: ô chọn VÀ/HOẶC/KHÔNG rộng 96 px tràn 22 px ("HOẶC" + mũi tên) → nới 118 px, `isExpanded`. Sau sửa xanh (`integration_test/catalog_flows_test.dart`) | Đạt (máy ảo) |
| MB.31 | Quét ISBN có / không có (luồng 5) | Nhập tay `9786041000100` (ISBN thật) và `9786041111110` (đúng số kiểm tra, không có) | ISBN thật mở thẳng "Bài tập lập trình hướng đối tượng"; ISBN không có: "Không tìm thấy tài liệu cho mã 9786041111110." kèm nút Tra cứu thủ công | Đúng như mong đợi | Đạt (máy ảo, camera thật chưa kiểm) |
| MB.32 | Đặt giữ → Sách của tôi → hủy; gia hạn thành công (luồng 6, 7) | Bạn đọc `TV2026000008`: Gia hạn phiếu `LC00000006`; tra "co so du lieu" → chi tiết có bản rảnh → Đặt giữ chỗ → thẻ Đặt giữ → Hủy → Đồng ý | Máy chủ trả "Đã gia hạn, hạn trả mới 17/09/2026." (lần chạy sau hết lượt thì trả câu từ chối, ứng dụng hiện nguyên câu); "Đã đặt giữ. Thư viện sẽ báo khi sách sẵn sàng."; thẻ Đặt giữ có dòng với nút Hủy; hủy xong viên "Đã hủy", mất nút. Gia hạn bị từ chối vì quá hạn: MB.16 | Đúng như mong đợi (`integration_test/holds_renew_sync_flow_test.dart`) | Đạt (máy ảo) |
| MB.33 | Đồng bộ dữ liệu trung tâm — `updatedSince` (luồng 12) | Trong phép thử: gọi API cán bộ sửa nhan đề tài liệu số `f6c04211…` thêm hậu tố; ứng dụng tra tài liệu số theo hậu tố; gọi `GET /api/reader/digital?updatedSince=<mốc 5 giây trước>`; trả lại nhan đề gốc khi dọn | Ứng dụng hiện nhan đề mới ngay; `updatedSince` trả đúng 1 tài liệu vừa sửa kèm `serverTime` | Đúng như mong đợi | Đạt (máy ảo) |

Bước 11 — **iOS**. Máy phát triển chạy Windows nên iOS chưa từng được biên dịch; các dòng dưới đây chạy
trên máy Mac của GitHub Actions (`.github/workflows/ios.yml`, Xcode 26.3, iPhone Simulator iOS 26.2 —
MB.34–MB.37 ở lượt 33730957017, MB.38–MB.40 ở lượt 33838388725 trên iPhone SE thế hệ 3, màn 375×667),
gọi vào **máy chủ thật** `https://thuvien.bluestar.com.vn`. Ba kịch bản ghi dữ liệu dùng một bạn đọc
riêng trên máy chủ ấy (`TV2026000652`, loại "Bạn đọc kiểm thử tự động", chính sách riêng: mượn 7 ngày,
gia hạn 14 ngày); phép thử trả sách và hủy đặt giữ trước lẫn sau khi chạy, mã trạm lấy từ API quản
trị lúc chạy, mã vạch chọn lúc chạy trong số bản đang rảnh.

| Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Kết quả thực tế | Đạt |
|---|---|---|---|---|---|
| MB.34 | Biên dịch bản phát hành iOS | `flutter build ios --release --no-codesign` trên macOS + Xcode 26.3 | Dựng xong, `Runner.app` mang đúng bundle id và ngưỡng iOS tối thiểu | `✓ Built build/ios/iphoneos/Runner.app (28.3MB)`; `CFBundleIdentifier = vn.bluestar.libraryconnect`; `MinimumOSVersion = 15.5`. Lỗi thật bắt được ở lượt đầu: `connectivity_plus` 7.3.1 gọi `NWPath.isUltraConstrained`, chỉ có trong SDK iOS mới nhất — phải chọn Xcode cao nhất của máy chủ; và Podfile tự viết làm Flutter 3.47 rẽ sang CocoaPods trong khi mọi gói đã là Swift Package | Đạt |
| MB.35 | Chạy trên iPhone Simulator, phần công khai | Trang chủ → thẻ Tra cứu (chưa gõ) → gõ không dấu "co so du lieu" → chi tiết → thẻ Bản in → Trích dẫn → duyệt Chủ đề → tin tức | Dữ liệu thật từ máy chủ trên mọi màn hình; gõ không dấu ra tài liệu có dấu; năm thẻ chi tiết; bảng trích dẫn sáu chuẩn | Đúng như mong đợi. Ảnh `docs/images/mobile/ios-01…ios-08` | Đạt (máy ảo) |
| MB.36 | Chạy trên iPhone Simulator, phần bạn đọc | Tủ sách → đăng nhập `TV2026000002` → xem Tủ sách → Tài khoản → Thẻ thư viện → đăng xuất | Đăng nhập được bằng thẻ thật; Tủ sách hiện số liệu máy chủ; thẻ điện tử dựng mã vạch CODE128 và QR tại máy; đăng xuất về trạng thái khách | Đúng như mong đợi. Ảnh `docs/images/mobile/ios-09…ios-12` | Đạt (máy ảo) |
| MB.37 | Chế độ tối và cỡ chữ lớn nhất trên iOS | Trong phiên đã đăng nhập: Tài khoản → chủ đề Tối → Sáng → Tối (đổi hai chiều) → kéo cỡ chữ lên hết nấc → xem Tài khoản, trang chủ, kết quả tra cứu, chi tiết, thẻ Bản in | Mỗi lần đổi, `Theme.of(context).brightness` đúng chiều; `MediaQuery.textScalerOf(context).scale(14) > 20` (cỡ chữ 14 nở quá 20); năm màn hình không tràn, không cắt chữ | Đúng như mong đợi. Ảnh `docs/images/mobile/ios-13…ios-17`. Đo bằng khẳng định chứ không nhìn ảnh — bài học số 13 của sổ lỗi | Đạt (máy ảo) |
| MB.38 | Đặt giữ → Tủ sách → hủy, trên iOS (luồng 6) | Bạn đọc kiểm thử đăng nhập → tra "co so du lieu" → chi tiết có bản rảnh → Đặt giữ chỗ → thẻ Đặt giữ → Hủy → Đồng ý | SnackBar "Đã đặt giữ. Thư viện sẽ báo khi sách sẵn sàng."; `GET /api/reader/holds` có dòng `Waiting`; thẻ Đặt giữ có nút Hủy; hủy xong "Đã hủy đặt giữ.", viên "Đã hủy", mất nút | Đúng như mong đợi (`integration_test/ios_write_flows_test.dart`). Ảnh `ios-18…ios-20` | Đạt (máy ảo) |
| MB.39 | Mượn tự phục vụ trên iOS — trạm bịa bị chặn, trạm thật cấp phiếu, mượn một cuốn thật (luồng 8) | Thẻ Đang mượn → nút Mượn tự phục vụ → nhập mã trạm bịa `LCST1\|GIA\|abc` → nhập nội dung QR thật của trạm `KHOMO-01` (lấy từ `/api/circulation/stations`) → nhập mã vạch `LC00000013` → nhập lại → Kết thúc | Mã bịa: ô đỏ câu của máy chủ; mã thật: dải "Đã xác thực tại Cửa kho mở tầng 2 · Hiệu lực đến HH:mm"; dòng xanh "Đã mượn · hạn trả"; lần hai "đã quét rồi"; phiếu "Đã mượn 1 cuốn". `GET /api/reader/loans/current` đúng một phiếu, đúng mã vạch, `loanType = SelfCheckout`, `channel = Mobile`, `renewedCount = 0` | Đúng như mong đợi; máy chủ ghi phiếu `PM00003111` ở lượt xanh 33838388725 (các lượt trước: PM00003107–PM00003110, đều đã trả). Máy ảo iPhone không có camera nên khung quét hiện lời báo, nhập bằng bàn phím. Ảnh `ios-21…ios-24`. Hai lỗi thật bắt được: I9 (không làm mới thẻ Đang mượn) và I10 (tràn 2 điểm ảnh khi bàn phím hiện trên iPhone SE) — sổ lỗi mục I | Đạt (máy ảo) |
| MB.40 | Gia hạn thành công trên iOS, đúng phiếu vừa mượn (luồng 7) | Phiếu tóm tắt → "Xem Sách của tôi" → nút Gia hạn của phiếu ấy | "Đã gia hạn, hạn trả mới dd/MM/yyyy."; `GET /api/reader/loans/current`: `renewedCount = 1`, hạn mới sau hạn cũ | Đúng như mong đợi: hạn 11/09/2026 → 18/09/2026, chính sách riêng 7/14 ngày nên hạn mới luôn dài hơn hạn cũ (máy chủ từ chối gia hạn không kéo dài hạn). Ảnh `ios-25…ios-26`. Phép thử trả sách bằng API quầy sau khi xong | Đạt (máy ảo) |

**Chưa kiểm trên iOS:** máy iPhone thật, camera, Face ID, thông báo đẩy, và bản IPA ký để phát hành
(cần tài khoản Apple Developer). Ba luồng ghi dữ liệu mất bảy lượt chạy mới xanh trọn (33828688766 →
33838388725): hai lượt lộ lỗi thật của ứng dụng (I9, I10), hai lượt lộ lỗi của chính phép thử (đọc
nhầm SnackBar còn treo, chạm trượt vì bàn phím mềm của máy ảo), một lượt trúng lúc Deploy khởi động
lại máy chủ — workflow giờ chờ Deploy của đúng commit xong hẳn rồi mới kiểm.

**Đối chiếu 12 luồng bắt buộc của PROMPT-MOBILE mục 6:** 1 → MB.09 + MB.18 · 2 → MB.10 · 3 → MB.30 · 4 → MB.11 · 5 → MB.31 · 6 → MB.32, MB.38 · 7 → MB.32, MB.40 (thành công) + MB.16 (từ chối) · 8 → MB.19, MB.20, MB.39 · 9 → MB.21, MB.23 · 10 → MB.22 (gói ngoại tuyến; chế độ máy bay kiểm qua MB.28) · 11 → MB.24 (phần trong ứng dụng; **đẩy thật chưa kiểm**) · 12 → MB.33.

**Chưa kiểm được trong đợt này (không có thiết bị):** quét mã bằng camera trên sách thật (đủ sáng/thiếu sáng), thẻ điện tử quét bằng máy quét ở quầy, xoay ngang trên máy thật, cỡ chữ hệ thống lớn nhất trên máy thật, nhận thông báo đẩy FCM thật, bản iOS.

Ảnh chụp máy ảo của các kịch bản MB: `docs/images/mobile/` (mb-login, mb-home, mb-search, mb-bib-detail, mb-scan, mb-my-library, mb-card-offline, mb-reader-online, mb-reader-find, mb-reader-offline, mb-digital-restricted, mb-digital-offline-list, mb-notifications, mb-account, mb-dark-home, mb-dark-my-library, mb-dark-search, mb-offline-my-library, mb-offline-search, mb-advanced-search, mb-facets, mb-renew, mb-holds, mb-sync).

## Cách chạy bộ kiểm thử tự động

```bash
cd backend
dotnet test                 # 577 unit test + 388 integration test
```

Integration test tự khởi tạo một container PostgreSQL 16 và một container MinIO riêng, chạy
migration, nạp dữ liệu nền, bật máy chủ tác vụ nền rồi gọi API qua đúng giao diện HTTP mà trình
duyệt dùng — không có thành phần nào bị giả lập.

```bash
cd frontend-admin
npm test                    # 211 test giao diện

cd frontend-opac
npm test                    # 85 test giao diện trang tra cứu
```

---

## Ghi chú

Nhóm kiểm thử 2.4 (trao đổi dữ liệu) gồm phần ISO 2709 và MARCXML ở nhóm kịch bản MARC, cùng
phần Z39.50, SRU và OAI-PMH ở nhóm kịch bản Liên thư viện.

Nhóm kiểm thử 2.5 (chuyển đổi dữ liệu) đã có phần nhập ISO 2709 và nhập Excel ở nhóm kịch bản
Biên mục; phần đối chiếu số lượng bạn đọc và giao dịch sẽ bổ sung khi bàn giao các phân hệ tương ứng.

Nhóm kiểm thử 2.8 (báo cáo) đã có phần báo cáo bổ sung ở nhóm kịch bản Phân hệ III, bảy báo cáo lưu thông ở nhóm Phân hệ VII và bốn báo cáo tài liệu số ở nhóm Phân hệ V; các báo cáo của
những phân hệ còn lại sẽ bổ sung khi bàn giao phân hệ tương ứng.

Nhóm kiểm thử 2.7 (ứng dụng di động) trong đợt web này được thay bằng kiểm thử tích hợp nhóm `/api/reader/*`; phần lưu thông của nhóm này nằm ở các kịch bản LT.26–LT.29.

Các nhóm kiểm thử 2.2 (các phân hệ chưa bàn giao) và 2.7 (ứng dụng di động) sẽ được bổ sung vào tài
liệu này theo từng phân hệ được bàn giao. Tài liệu luôn phản ánh đúng phạm vi đã hoàn thành tại thời điểm
nghiệm thu, không liệt kê trước những gì chưa làm.

---

## Phụ lục — Nghiệm thu thử và test sâu trên máy chủ thật ngày 05/09/2026

Chạy trên `https://thuvien.bluestar.com.vn` (dữ liệu 12.608 biểu ghi) bằng tài khoản đúng vai: quản trị, một tài khoản
Cán bộ lưu thông và các bạn đọc thử tạo trong lúc chạy. Hai đợt trong ngày: đợt nghiệm thu thử buổi chiều (223 kịch bản,
ảnh `524ad90`) và đợt test sâu buổi tối đi qua **từng dòng của Chương V** bằng luồng ghi thật — tạo rồi xoá — trên ảnh
`72680c2`: đơn đặt từ yêu cầu tới biên bản bàn giao, kiểm kê từ đóng kho tới quyết định mất, đầu báo từ sinh số tới đóng
tập, tài liệu số từ tải lên tới thu hồi quyền, nhập Excel bạn đọc, nhập ZIP tài liệu số, sao lưu thật, biểu mẫu in.
Dữ liệu ghi thêm mang dấu `NT…` và đã xoá; thứ không xoá được (yêu cầu đặt mua đã duyệt, đơn đã hủy, một đầu báo đã
nhận số) được đánh dấu ngừng dùng. Mã kịch bản bám theo bảng ở trên; mã có hậu tố chữ là bước phụ; mã `F2`, `K…` là lỗi
tìm ra trong ngày (mục K của `08-so-loi.md`).

Tổng: **408 kịch bản chạy bằng máy, 407 đạt**. Dòng "Không đạt" còn lại là lần chạy trước khi sửa (F2) — sau khi
triển khai bản sửa đã kiểm lại đạt (K2). Các lỗi K1, K3, K4, K5 lộ ra khi đi bằng trình duyệt hoặc đọc nhật ký máy chủ; K6, K7 tìm ra ở đợt test sâu và lúc triển khai bản sửa.

| Mã | Chức năng | Kết quả thực tế | Đạt |
|---|---|---|---|
| 2.1.5 | SYS_ADMIN có đủ quyền | SYS_ADMIN: {'groupId': 'd507e294-fdd3-44e6-9b4e-a6818b59636d', 'groupName': 'Quản trị hệ thống', 'isSystem': True, 'tree': [{'key': 'module:Quản trị hệ thống',  | Đạt |
| 2.1.6 | 5 nhóm mẫu | Cán bộ biên mục, Cán bộ bổ sung, Cán bộ lưu thông, Quản trị hệ thống, Thủ thư | Đạt |
| 2.1.7 | Tham số hệ thống | 15 nhóm, 136 tham số | Đạt |
| 2.1.11 | Danh mục nạp sẵn | languages=21, countries=24, document-types=14, reader-types=7, classifications=10, publishers=1869, authors=14286, subjects=3476 | Đạt |
| 2.1.11b | Thư viện và kho | thư viện=2, kho=4, giá=0 | Đạt |
| 2.3.1 | Ẩn danh bị 401 | 401: Phiên đăng nhập không hợp lệ hoặc đã hết hạn. | Đạt |
| 2.3.2 | Cán bộ lưu thông gọi API quản trị → 403 | 403: Bạn không có quyền thực hiện chức năng này. [{'field': 'permission', 'message': 'Thiếu quyền: SYSTEM.USER.VIEW', 'code': 'FORBIDDEN'}] | Đạt |
| 2.3.2a | Tạo tài khoản Cán bộ lưu thông | user ntt_ntt90073 = 523221d7-1a41-44e1-b528-5de8adcff403, có mật khẩu tạm | Đạt |
| 2.3.2b | Cán bộ lưu thông đăng nhập, buộc đổi mật khẩu lần đầu | 200; buộc đổi=True  | Đạt |
| 2.3.2c | Cán bộ lưu thông vẫn xem được lưu thông | 200 | Đạt |
| 2.3.3 | Chặn cả 5 endpoint quản trị | [403, 403, 403, 403, 403] | Đạt |
| 2.3.4 | Admin gọi 5 endpoint quản trị | [200, 200, 200, 200, 200] | Đạt |
| 2.3.6 | Cấp thêm quyền có hiệu lực sau đăng nhập lại | 200 sau khi thêm SYSTEM.USER.VIEW | Đạt |
| 2.3.7 | Nhật ký đăng nhập thất bại và thành công | [('LoginFailed', False), ('Login', True), ('Update', True), ('Update', True)] | Đạt |
| 2.3.8 | Tạo nhóm người dùng, nhật ký ghi Thêm mới | nhóm 0f0ac096-14ad-43e3-88d4-9bf9b879ab5b; nhật ký: Create / UserGroup | Đạt |
| 2.3.9 | Sửa quyền nhóm, nhật ký ghi phân quyền | ['PermissionChange', 'Create'] | Đạt |
| 2.3.10 | Chi tiết một bản ghi nhật ký có giá trị trước/sau | Update Reader: keys=['oldValue', 'newValue'] | Đạt |
| 2.3.11 | Đặt lại mật khẩu, nhật ký không lộ mật khẩu | 3 dòng nhật ký, không dòng nào chứa mật khẩu | Đạt |
| 2.3.12 | Cài đặt chế độ ghi nhận (ghi lại cùng giá trị) | 20 đối tượng | Đạt |
| 2.3.13 | Cài đặt ghi nhận: lưu vĩnh viễn | 20 đối tượng, thời gian lưu để trống | Đạt |
| 2.3.14 | Xuất nhật ký Excel theo bộ lọc | 42082 byte xlsx | Đạt |
| 2.3.14b | Xuất nhật ký PDF theo bộ lọc | 259948 byte pdf trong 0.4s | Đạt |
| 2.3.14c | Tra cứu nhật ký theo bộ lọc IP và người dùng | 101818 dòng của admin | Đạt |
| 2.3.15 | Chính sách mật khẩu: 'abc' bị từ chối | 400: [{'field': 'newPassword', 'message': 'Mật khẩu phải có tối thiểu 8 ký tự.', 'code': None}] | Đạt |
| 2.6.1 | Sao lưu thủ công chạy nền tới Success | Success 44047585 byte sau 2026-09-05T07:35:38.965348+00:00 | Đạt |
| 2.6.2 | Tải bản sao lưu về | 44047585 byte, application/octet-stream | Đạt |
| 2.6.3 | Phục hồi đòi mật khẩu admin (sai → từ chối) | 400: Dữ liệu không hợp lệ. | Đạt |
| 2.6.4 | Trạng thái phục hồi | None | Đạt |
| 6.4.1 | Header bảo mật trang tra cứu | /: đủ 4 header; CSP=default-src 'self'; script-src 'self'; style-src 'self' 'uns... | Đạt |
| 6.4.2 | Header bảo mật trang quản trị | /admin/: đủ 4 header; CSP=default-src 'self'; script-src 'self'; style-src 'self' 'uns... | Đạt |
| 6.4.3 | HTTP chuyển hướng HTTPS | 308 -> https://thuvien.bluestar.com.vn/ | Đạt |
| BD.1 | Thêm bạn đọc, số thẻ tự sinh | TV2026000659 hạn 2036-09-05 | Đạt |
| BD.3 | Trùng mã sinh viên bị chặn | 400: Dữ liệu không hợp lệ. | Đạt |
| BD.4 | Tra bạn đọc gõ không dấu | 1 kết quả 'nghiem thu thu' | Đạt |
| BD.5 | Tra bạn đọc theo số thẻ / mã SV / email | thẻ=1, mãSV=1, email=1 | Đạt |
| BD.9 | Tải tệp không phải ảnh làm ảnh bị chặn | 400 | Đạt |
| BD.10 | Tải ảnh chân dung PNG rồi lấy về | ảnh 200 | Đạt |
| BD.12 | Ghi nhận vi phạm theo loại | phạt theo loại: 0.0 | Đạt |
| BD.13 | Cấp lại thẻ mất: số thẻ mới, giữ thẻ cũ | thẻ mới TV2026000666; 2 thẻ trong hồ sơ | Đạt |
| BD.15 | Cấp lại thẻ thiếu lý do bị chặn | 400 | Đạt |
| BD.17 | Gia hạn thẻ còn hạn (12 tháng) | {'total': 1, 'succeeded': 1, 'skipped': 0, 'skips': []} | Đạt |
| BD.18 | Gia hạn thẻ hàng loạt: 165 thẻ hết hạn 05/09/2026 thêm 12 tháng | Đã gia hạn 165 thẻ; còn 0 thẻ sắp hết hạn trong 30 ngày | Đạt |
| BD.19 | Tạm khóa thiếu lý do bị chặn | 400 | Đạt |
| BD.20 | Tạm khóa rồi mở khóa | khóa rồi mở | Đạt |
| BD.21 | Ra trường khi còn công nợ bị chặn / xác nhận công nợ | công nợ: {'readerId': '0c8f7336-545d-4ed1-bf6e-bdf35b46f6a7', 'cardNumber': 'TV2026000659', 'fullName': 'Bạn đọc nghiệm thu thử NTT90073', 'studentCode': 'NTT9 | Đạt |
| BD.21b | Ra trường hàng loạt theo khóa | 200: {'total': 1, 'succeeded': 1, 'skipped': 0, 'skips': []} | Đạt |
| BD.22 | Xác nhận công nợ | {'readerId': 'a18cd93a-410c-4f90-9477-380b3f528cc1', 'cardNumber': 'TV2026000666', 'fullName': 'Bạn  | Đạt |
| BD.24 | Đặt lại mật khẩu bạn đọc | NTT-BanDoc@2026 | Đạt |
| BD.25 | Mẫu thẻ bạn đọc | 1 mẫu | Đạt |
| BD.26 | Thiết kế mẫu thẻ: danh sách trường và tạo mẫu | 18 trường; mẫu tạo ok | Đạt |
| BD.27 | Nội dung tràn khổ thẻ bị chặn | 400 | Đạt |
| BD.28 | In thẻ bạn đọc PDF (xem trước, không tính lần in) | 200 69671 byte  | Đạt |
| BD.31 | Đếm số lần in thẻ | printCount=0 | Đạt |
| BD.33 | Tệp mẫu nhập bạn đọc | xlsx 11232 byte | Đạt |
| BD.34 | Kiểm tra tệp nhập bạn đọc | 2 dòng, 0 lỗi: [] | Đạt |
| BD.37 | Nhập bạn đọc chạy nền | Completed ok=2 err=0 | Đạt |
| BD.40 | Nhật ký lỗi của lô nhập | 200 | Đạt |
| BD.42 | Xuất xlsx readers/export | xlsx 59751 byte | Đạt |
| BD.43 | Đồng bộ từ hệ thống đào tạo theo ánh xạ đã khai | 200: {'totalItems': 1, 'created': 1, 'updated': 0, 'skipped': 0, 'errorItems': 0, 'errors': [], 'dryRun': False} | Đạt |
| BD.44 | Báo cáo readers/reports/count | 1102 byte json | Đạt |
| BD.45 | Báo cáo readers/reports/registrations | 222 byte json | Đạt |
| BD.46 | Báo cáo readers/reports/expiring-cards | 54101 byte json | Đạt |
| BD.48 | Báo cáo readers/reports/activity | 5731 byte json | Đạt |
| BD.49 | Xuất báo cáo bạn đọc PDF | pdf 60353 byte | Đạt |
| BD.55 | In giấy xác nhận trả sách | pdf 43257 byte | Đạt |
| BM.1 | Giá trị ngầm định điền sẵn (040/041) | trường có sẵn: ['020', '040', '041', '044', '082', '100', '245', '250', '260', '300']; control: ['008'] | Đạt |
| BM.2 | Giá trị ngầm định mới có mặt trong khung biểu ghi mới | 500$a điền sẵn từ cấu hình vừa thêm | Đạt |
| BM.3 | Mẫu biên mục theo dạng tài liệu: tạo, sửa, liệt kê | 4 mẫu | Đạt |
| BM.5 | Thêm mới ấn phẩm bằng MARC | id=2cc4b2ef-0e07-436a-affe-e6579ef2a4e9 control=LC00013117 | Đạt |
| BM.6 | Rút cột phẳng từ MARC | {'isbn': '9786047399999', 'publishYear': 2026, 'pages': '215 tr.', 'authorMain': 'Trần Thị Nghiệm Thu', 'ddc': '025.3'} | Đạt |
| BM.7 | Tự tạo tác giả / chủ đề danh mục | tác giả 'Trần Thị Nghiệm Thu'=1, chủ đề 'Thư viện số'=1 | Đạt |
| BM.11 | Tab lịch sử lưu thông của biểu ghi | có lịch sử | Đạt |
| BM.12 | Lịch sử phiên bản và diff | 1 phiên bản; diff: [{'kind': 'Changed', 'tag': 'LDR', 'before': '00000nam a2200000 a 4500', 'after': '00000cam a2200000 a 4500'}, {'kind':  | Đạt |
| BM.13 | Đăng ký cá biệt 2 bản | {'created': 2, 'barcodes': ['LC00018020', 'LC00018021'], 'callNumber': '025.3 TRA'} | Đạt |
| BM.13b | Mã vạch sinh theo quy tắc | ['LC00018020', 'LC00018021'] | Đạt |
| BM.14 | Chặn xóa biểu ghi còn ĐKCB | 409: Biểu ghi này còn 2 đăng ký cá biệt. Hãy thanh lý hoặc chuyển các bản đó sang biểu ghi khác trước khi xóa biểu ghi. | Đạt |
| BM.15 | Sau khi xóa, OPAC không còn thấy | 0 kết quả | Đạt |
| BM.16 | Không dấu và có dấu cùng số kết quả | 45 = 45 | Đạt |
| BM.17 | Hàng đợi biên mục | {'pending': 936, 'inProgress': 0, 'waitingApproval': 0, 'completed': 7469, 'returned': 0, 'overdue': 0} | Đạt |
| BM.18 | Phân công và đặt hạn xử lý | đã phân công | Đạt |
| BM.19 | Duyệt / trả lại kèm lý do | trả lại → 200; hoàn thành → 200 | Đạt |
| BM.20 | Năng suất biên mục | [{'userId': '15694970-4a70-42f9-ba0a-23f82ef5c26e', 'userName': 'admin', 'assigned': 1, 'completed': 1, 'returned': 0, 'averageDays': 0}] | Đạt |
| BM.21 | Nhập ISO 2709 — xem trước phát hiện trùng theo ISBN | 1 biểu ghi; trùng: 1 | Đạt |
| BM.23 | Nhập lại đúng tệp đó: bỏ qua bản trùng, chạy nền | Completed: total=1 success=0 skipped=1 failed=0 | Đạt |
| BM.26 | Kết quả và nhật ký lỗi của lượt nhập | 200 application/vnd.openxmlformats-officedocument.spreadsheetml.sheet | Đạt |
| BM.28 | Xuất ISO 2709 theo bộ lọc từ khoá | 1 biểu ghi, 747 byte | Đạt |
| BM.30 | Tệp mẫu nhập Excel biên mục | xlsx 11388 byte | Đạt |
| BM.31 | Nhập Excel — đọc tệp mẫu và đoán ánh xạ | 20 cột, ánh xạ đoán: 20 | Đạt |
| BM.34 | Lưu hồ sơ ánh xạ Excel | 23fbf169-bcb1-4d08-90be-4a421a2fa934 | Đạt |
| BM.35 | Danh mục tự tạo từ 260$a: quét toàn kho | 5 giá trị | Đạt |
| BM.36 | Danh mục tự tạo hiện thành facet trên OPAC | có facet tự tạo | Đạt |
| BM.37 | Gộp hai giá trị danh mục tự tạo | còn 4 giá trị | Đạt |
| BM.38 | Tạo mẫu phích | ok | Đạt |
| BM.39 | Ô nằm ngoài khổ phích bị chặn | 400 | Đạt |
| BM.40 | In phích PDF | pdf 30508 byte | Đạt |
| BM.41 | Xem trước phích từ biểu ghi chưa lưu | pdf 20864 byte | Đạt |
| BM.43 | Sửa biểu ghi đã có, thêm điểm truy cập 700 | 700 đã lưu; tác giả phụ: ['Trần Thị Nghiệm Thu', 'Lê Văn Đồng Tác Giả'] | Đạt |
| BM.51 | Nạp lại bộ định nghĩa MARC chuẩn (bổ sung, không ghi đè) | {'added': 0, 'updated': 0, 'unchanged': 220, 'custom': 1} | Đạt |
| BS.2 | Thêm kho | ok | Đạt |
| BS.2b | Thêm giá vào kho | ok | Đạt |
| BS.3 | Mã giá trùng trong cùng kho bị chặn | 400 | Đạt |
| BS.4 | Mã giá trùng ở kho khác thì được | 200 | Đạt |
| BS.5 | Xóa kho còn ấn phẩm bị chặn | 409 | Đạt |
| BS.6 | Bản đồ kho | {'warehouseId': 'd53fa954-dc70-49d2-bc37-3df6be796354', 'warehouseName': 'Kho mở', 'itemCount': 4475, 'rows': 0, 'column | Đạt |
| BS.7 | Lập yêu cầu đặt mua | yêu cầu acc239b0-7e0b-47e7-bf0b-ebce6d0979aa | Đạt |
| BS.8 | Cảnh báo tài liệu đã có (ISBN) | {'bibId': '2cc4b2ef-0e07-436a-affe-e6579ef2a4e9', 'controlNumber': 'LC00013117', 'title': 'Giáo trình nghiệm thu thử hệ  | Đạt |
| BS.10 | Nhập đề nghị từ Excel (tệp mẫu) | 200: {'requestId': '40e0bb00-6d3c-43fd-ae71-680d0a5a2047', 'imported': 1, 'duplicateWarnings': 1, 'totalAmount': 600000, 'err | Đạt |
| BS.11 | Duyệt khi chưa gửi bị chặn | 409: Yêu cầu YC202600001 không ở trạng thái chờ duyệt nên không duyệt được. | Đạt |
| BS.12 | Duyệt yêu cầu đã gửi | Approved | Đạt |
| BS.13 | Từ chối yêu cầu kèm lý do | đã từ chối | Đạt |
| BS.15 | Lập đơn đặt từ yêu cầu đã duyệt (gộp theo nhà cung cấp) | đơn 5bf8340a-a3ad-40e0-9083-02435b73c849 | Đạt |
| BS.16 | Gộp lại lần hai: yêu cầu đã lên đơn không lên đơn nữa | 409 | Đạt |
| BS.17 | Ghi nhận giao hàng từng phần → trạng thái Partial | trạng thái PartiallyReceived | Đạt |
| BS.18 | Nhập kho khi chưa biên mục → báo dòng chờ biên mục sơ lược | {'createdItems': 0, 'barcodes': [], 'pendingCataloging': ['Sách đặt mua NTI94224']} | Đạt |
| BS.19 | Biên mục sơ lược vào hàng đợi | bib df3bb2f8-186d-4213-8d78-e2ae8e233ad0 có trong hàng đợi Chờ xử lý | Đạt |
| BS.21 | Tạo ĐKCB từ đơn sau biên mục sơ lược (theo số thực nhận) | {'createdItems': 2, 'barcodes': ['LC00018026', 'LC00018027'], 'pendingCataloging': []} | Đạt |
| BS.22 | Bấm nhập kho lần hai không sinh trùng | 409: Không có dòng nào để nhập kho: các dòng đã nhận đều đã tạo đủ ĐKCB. | Đạt |
| BS.23 | Ấn phẩm mới chờ kiểm nhận, không cho mượn | chặn: ['Ấn phẩm đang bị khóa lưu thông: Chờ kiểm nhận.', 'Ấn phẩm đang ở trạng thái chưa kiểm nhận, không cho mượn được.'] | Đạt |
| BS.23b | Danh sách xếp giá chưa kiểm nhận | 456 bản chờ kiểm nhận toàn kho | Đạt |
| BS.24 | Mở khóa khi chưa kiểm nhận bị chặn | bỏ qua: Chưa kiểm nhận nên chưa mở khóa được. Hãy kiểm nhận trước. | Đạt |
| BS.25 | Kiểm nhận và mở khóa | trạng thái: ['InStock', 'InStock'] | Đạt |
| BS.26 | Xếp giá hàng loạt | {'affected': 2, 'skipped': []} | Đạt |
| BS.27 | Khóa lưu thông thiếu lý do bị chặn | 400: [{'field': 'reason', 'message': 'Phải ghi lý do khóa để người khác biết vì sao bản này không cho mượ | Đạt |
| BS.27b | Khóa lưu thông kèm lý do rồi mở | khóa rồi mở | Đạt |
| BS.28 | In tem mã vạch PDF | pdf 37204 byte | Đạt |
| BS.28b | Ảnh mã vạch | image/png 552 byte | Đạt |
| BS.29 | In nhãn gáy PDF | pdf 17117 byte | Đạt |
| BS.31 | Mẫu tem vượt khổ giấy bị chặn | 400 | Đạt |
| BS.32 | Chuyển kho thiếu lý do bị chặn | 400 | Đạt |
| BS.33 | Chuyển kho + phiếu chuyển | {'affected': 1, 'skipped': [], 'documentCode': 'PCK202600001'} | Đạt |
| BS.34 | In phiếu chuyển kho theo mã phiếu | PCK202600002: 200 pdf 50532 byte; chuyển LC00000007 Kho mở → Kho đóng | Đạt |
| BS.35 | Thanh lý một bản kèm quyết định | {'affected': 1, 'skipped': [], 'documentCode': 'QĐ-TL-NTI94224'} | Đạt |
| BS.35r | Báo cáo acquisition/reports/disposals | 51 byte json | Đạt |
| BS.36 | Thanh lý lần hai bị chặn | 409 | Đạt |
| BS.38 | In biên bản bàn giao | pdf 47537 byte | Đạt |
| BS.39 | Đính kèm bản scan (chữ ký byte PDF) | scan 200 | Đạt |
| BS.40 | Đóng kho khi tạo kỳ: kho báo đang kiểm kê | InProgress, kỳ vọng 2 bản | Đạt |
| BS.41 | Kỳ kiểm kê | 0 kỳ | Đạt |
| BS.42 | Hai kỳ trên một kho bị chặn | 409 | Đạt |
| BS.43 | Chuyển vào kho đang kiểm kê bị chặn | 409 | Đạt |
| BS.44 | Quét khớp | None | Đạt |
| BS.45 | Quét mã lạ | 200: {'barcode': 'KHONGCO999', 'outcome': 'Unexpected', 'outcomeName': 'Thừa', 'alrea | Đạt |
| BS.46 | Quét trùng | None | Đạt |
| BS.47 | Nạp tệp quét rời | {'total': 2, 'match': 0, 'unexpected': 1, 'wrongWarehouse': 0, 'duplicate': 1, 'scannedCount': 1, 'e | Đạt |
| BS.48 | Chốt kỳ kiểm kê | đã chốt | Đạt |
| BS.49 | Kết quả và xuất Excel | 4 dòng; xlsx 7371 byte | Đạt |
| BS.50 | In biên bản kiểm kê theo mã kỳ | kỳ KK0003 in được PDF 50.644 byte (đang kiểm kê) và 47.765 byte (sau khi chốt); mã cũ KK0002 không có thật nên 404 đúng | Đạt |
| BS.51 | Lập quyết định mất từ bản thiếu | {'affected': 1, 'skipped': [], 'documentCode': 'QĐ-MAT-NTI94224'} | Đạt |
| BS.52 | Thống kê bổ sung theo chiều DOCTYPE | 10 dòng; chiều có: ['DOCTYPE', 'CARRIER', 'TIME', 'LANGUAGE', 'WAREHOUSE', 'FUNDING'] | Đạt |
| BS.53 | Thống kê bổ sung theo thời gian (quý) | 10 dòng: ['2024-Q2', '2024-Q3', '2024-Q4', '2025-Q1'] | Đạt |
| BS.54 | Bảng tổng hợp đa chiều (pivot) | 10 hàng x 4 cột | Đạt |
| BS.55 | Pivot hai chiều trùng nhau bị chặn | 400: [{'field': 'columnDimension', 'message': 'Chiều hàng và chiều cột phải khác nhau.', 'code': None}] | Đạt |
| BS.56 | Báo cáo acquisition/reports/acquisition-list | 7396570 byte json | Đạt |
| BS.56x | Xuất thống kê bổ sung PDF | pdf 56057 byte | Đạt |
| BS.57 | Báo cáo acquisition/reports/purchase-approval | 231 byte json | Đạt |
| BS.58 | Lịch sử giao dịch nhà cung cấp | {'supplierId': '1e10c362-3a0b-4755-b0e6-1c2bdecd1c44', 'supplierName': 'Nhà cung cấp NTI94224', 'rating': 0, 'orderCount | Đạt |
| BS.59 | Xuất xlsx stock/items/export | xlsx 1679007 byte | Đạt |
| BS.60 | Cán bộ lưu thông không tạo được biểu ghi | 403 | Đạt |
| BS.61 | In phiếu ngay sau chuyển kho (chuyển về) | PCK202600003; 200 | Đạt |
| BS.62 | Danh sách phiếu chuyển kho | 0 phiếu | Đạt |
| BS.63 | In quyết định thanh lý theo số quyết định | 200 application/pdf  | Đạt |
| BS.64 | Yêu cầu đặt mua ấn phẩm định kỳ | yêu cầu 8d621996-ad9f-43e5-af49-29543c44c9ac | Đạt |
| BS.65 | Biểu đồ báo cáo bổ sung có dữ liệu | tổng 17900 bản qua 10 dạng | Đạt |
| BS.66 | Biểu mẫu in dùng chung | 11 loại biểu mẫu: [{'formType': 'RECEIPT', 'name': 'Phiếu nhập kho', 'headerFields': [{'key': 'libraryName', 'label': 'Tên thư viện', 'isRow': False}, {'key': ' | Đạt |
| BS.71 | Tiến độ kiểm kê | {'periodId': 'ee86b993-7a13-491f-9ffc-567631598879', 'code': 'KK0002', 'name': 'Kỳ kiểm kê NTI94224', 'warehouseName': ' | Đạt |
| BS.75 | Biên bản ghi đúng số thực nhận | 1 dòng; tổng None | Đạt |
| BS.78 | Phân công lại giữa kỳ | 200 | Đạt |
| DK.1 | Danh sách đầu báo | 5 đầu báo | Đạt |
| DK.6 | Xem trước số dự kiến | 3 số | Đạt |
| DK.14 | Sinh số Q1 | {'created': 3, 'skipped': 0, 'captions': ['Tạp chí nghiệm thu sâu NTE93707 — Số  | Đạt |
| DK.15 | Sinh lại lần hai bị chặn | 409 | Đạt |
| DK.18 | Ghi nhận số đến kèm tình trạng | {'received': 1, 'createdItems': 1, 'barcodes': ['LC00018023'], 'skipped': []} | Đạt |
| DK.19 | Sinh ĐKCB khi ghi nhận số | 1 bản của số vừa nhận | Đạt |
| DK.20 | Ghi nhận lại số đã nhận bị chặn | 409: Các số đã chọn đều được ghi nhận từ trước. | Đạt |
| DK.21 | Lưới nhận số của đầu báo mới | [{'year': 2026, 'expected': 2, 'received': 0, 'missing': 1, 'bound': 0, 'cells': [{'issueId': 'a2c07 | Đạt |
| DK.22 | Đánh dấu số thiếu | 1 số | Đạt |
| DK.23 | Lập khiếu nại | {'created': 1, 'claimNumbers': ['KN00006'], 'skipped': []} | Đạt |
| DK.24 | Khiếu nại trùng bị chặn | 409 | Đạt |
| DK.25 | Ghi phản hồi nhà cung cấp | đã ghi | Đạt |
| DK.27 | Nhập mục lục bài trích | 2 bài | Đạt |
| DK.28 | Nhập mục lục từ Excel (tệp mẫu) | 200: {'imported': 1, 'errors': []} | Đạt |
| DK.29 | Sinh biểu ghi bài trích (773) ở trạng thái xuất bản | {'created': 1, 'skipped': 0, 'controlNumbers': ['LC00013122']} | Đạt |
| DK.30 | Bạn đọc tra được bài trích trên OPAC | 1 kết quả; dạng: None | Đạt |
| DK.31 | Sinh biểu ghi lần hai không tạo trùng | 409: Mọi bài trích đã chọn đều đã có biểu ghi riêng — đúng luật, không tạo trùng | Đạt |
| DK.32 | Xóa bài đã có biểu ghi | 409: Bài trích "Bài trích nghiệm thu NTF94037" đã sinh biểu ghi riêng nên không xóa khỏi mục lục được. Hãy xóa biểu ghi ở phân hệ Biên mục trước. | Đạt |
| DK.33 | Đóng tập khi chưa nhận đủ số bị chặn | 409 | Đạt |
| DK.34 | Đóng tập khi đã nhận đủ số: sinh ĐKCB mới cho tập | {'id': 'cecf2d40-3c6e-4bcb-a7c6-ae9d561c552f', 'serialId': '7205b42c-3ca6-4b68-a322-4c5edcbde4bb', 'serialTitle': 'Tạp chí nghiệm thu NTF94037 (ngừng đặt)', 'co | Đạt |
| DK.35 | Số lẻ sau khi đóng tập chuyển trạng thái Bound | ['Bound', 'Bound'] | Đạt |
| DK.36 | Tập đóng có ĐKCB riêng trong kho | {'id': 'cecf2d40-3c6e-4bcb-a7c6-ae9d561c552f', 'serialId': '7205b42c-3ca6-4b68-a322-4c5edcbde4bb', 'serialTitle': 'Tạp c | Đạt |
| DK.37 | Bảng tổng hợp theo năm | [{'year': 2026, 'planned': 3, 'received': 0, 'missing': 1, 'bound': 0, 'value': 0.0, 'receivedPercen | Đạt |
| DK.38 | Xóa đầu báo đã có bài trích/khiếu nại → chặn hoặc cho phép có lý | 200: Đã xóa đầu báo. | Đạt |
| DK.38b | Xóa đầu báo đã nhận số bị chặn | 409: Đầu báo "Tạp chí nghiệm thu NTF94037" đã nhận 1 số nên không xóa được. Hãy đánh dấu ngừng đặt thay vì xóa. | Đạt |
| DK.40 | Thống kê ấn phẩm định kỳ (OVERALL, DDC, FREQUENCY, LANGUAGE, SUPPLIER, WAREHOUSE) | {'title': 'Tổng hợp ấn phẩm định kỳ', 'dimensionName': 'Tổng hợp', 'rows': [{'label': 'Toàn bộ ấn phẩm định kỳ', 'titleCount': 5, 'receivedIssues': 89, 'missing | Đạt |
| DK.42 | Xuất thống kê định kỳ PDF | pdf 48037 byte | Đạt |
| DK.44 | Bổ sung tổng thể: số đến hạn nhiều đầu báo | 15 số dự kiến đến hạn | Đạt |
| DK.47 | In phiếu khiếu nại gửi nhà cung cấp | pdf 48390 byte | Đạt |
| DK.49 | In nhãn gáy tập | 200 10634 byte | Đạt |
| DM.6 | Danh mục phân cấp dạng cây (chủ đề) | 3478 nút gốc | Đạt |
| DM.8 | Tệp mẫu nhập danh mục | xlsx 10120 byte | Đạt |
| DM.9 | Kiểm tra tệp danh mục trước khi nhập (dryRun) | {'totalRows': 0, 'createdRows': 0, 'updatedRows': 0, 'errorRows': 0, 'errors': []} | Đạt |
| DM.11 | Xuất xlsx catalogs/languages/export | xlsx 7930 byte | Đạt |
| DM.12 | Tìm mục trùng trong danh mục tác giả | 0 nhóm trùng | Đạt |
| DM.14 | Xuất pdf catalogs/languages/export | pdf 61435 byte | Đạt |
| F2 | Đặt giữ biểu ghi không có bản in nào phải bị từ chối | 200: {"data":{"id":"d32783e7-693b-4c55-843d-626fd6d0e5c2","readerId":"895264cd-164c-439f-9b5a-3dc61406a12f","readerCardNumber":"TV2026000662","readerName":"Bạn  | **Không đạt** → đã sửa |
| F2.pre | Biểu ghi thử không có ĐKCB | Our Angry Earth: 0 bản in | Đạt |
| HN.1 | Tra cứu dưới 1 giây (đo cả mạng) | 0.09s, 19 kết quả | Đạt |
| HN.4 | Câu hỏi rộng dừng đếm ở 10.000 | 10000 kết quả trong 0.42s | Đạt |
| I.2a | Tệp mẫu nhập người dùng | xlsx 10225 byte | Đạt |
| I.2b | Lịch sử đăng nhập của người dùng | 214 lượt | Đạt |
| I.2c | Phòng ban để lọc người dùng | 0 phòng ban | Đạt |
| I.3 | Lịch sử thay đổi tham số | 9 dòng | Đạt |
| I.3a | Sửa tham số hệ thống (ghi lại cùng giá trị) và có lịch sử | LIBRARY.NAME lưu lại; lịch sử 9 dòng | Đạt |
| I.5 | Danh sách bản sao lưu | 5 bản; mới nhất: 2026-09-04T19:00:12.237752+00:00 Success | Đạt |
| I.5b | Dung lượng lưu trữ sao lưu | {'totalBytes': 102888095744, 'freeBytes': 1443389440, 'usedByBackupsBytes': 167166724, 'backupCount': 4, 'autoEnabled': True, 'scheduleCron': '0 2 * * | Đạt |
| II.2c | Tải ảnh bìa cho biểu ghi rồi OPAC phục vụ | cover 200 image/png | Đạt |
| II.5a | Khai trường MARC dùng riêng rồi sửa | 918: Trường riêng NTF94037 (sửa) | Đạt |
| III.1a | In đơn đặt hàng PDF | pdf 50248 byte | Đạt |
| III.2 | Báo cáo reports/overview | 4577 byte json | Đạt |
| III.2x | Xuất pdf reports/overview/export | pdf 63869 byte | Đạt |
| III.6 | Trình thiết kế biểu mẫu: tạo, sửa, liệt kê, xoá | 11 loại; 12 mẫu sau khi thêm; đã xoá | Đạt |
| IX.1 | Trang chi tiết có thẻ meta cho máy thu thập | Cơ sở dữ liệu — l&#253; thuyết v&#224; b&#224;i tập – Thư viện Trường Đại học Mẫ | Đạt |
| IX.2a | Yêu thích và tìm kiếm đã lưu | yêu thích 9; đã lưu 1 | Đạt |
| K1 | Vượt hạn mức đăng nhập nhận 429 JSON tiếng Việt | mã: [401, 401, 401, 429]; body: {"success":false,"message":"Bạn thao tác quá nhanh. Vui lòng đợi một phút rồi thử lại.","errors":[]}; Retry-After=60 | Đạt |
| K2 | Đặt giữ biểu ghi không có bản in bị từ chối 409 | 409: Tài liệu này chưa có bản in nào trong kho nên không đặt giữ được. | Đạt |
| K4 | Bản dựng trang quản trị không còn dòng giữ chỗ | 1 tệp js, 1181535 ký tự | Đạt |
| K5 | Kiểm tra biểu ghi mới không còn báo lỗi thiếu 001 | errorCount=0 | Đạt |
| K6 | In LOAN_SLIP của bạn đọc đã xóa hồ sơ → 404 rõ nghĩa (bản 5a34971) | 404: Không tìm thấy bạn đọc của phiếu với định danh 'PM00003117'. | Đạt |
| K6b | In RETURN_SLIP của bạn đọc đã xóa hồ sơ → 404 rõ nghĩa (bản 5a34971) | 404: Không tìm thấy bạn đọc của phiếu với định danh 'PM00003117'. | Đạt |
| K7 | Triển khai tự dọn ảnh cũ: ổ đĩa 100% → 83%, còn 8 ảnh libraryconnect (2 bản) | deploy 5a34971 THÀNH CÔNG; df 18G trống | Đạt |
| LT.1 | Chính sách lưu thông nạp sẵn | 7 chính sách | Đạt |
| LT.2 | Ô thử chính sách: chọn đúng chính sách ưu tiên | Chính sách NTE93707 (ưu tiên None) | Đạt |
| LT.3 | Danh sách đặt giữ toàn hệ thống và hàng đợi | 129 phiếu | Đạt |
| LT.4 | Hạn trả rơi vào ngày nghỉ được đẩy sang ngày làm việc | 2026-09-08 → 2026-09-08 (Hạn trả 08/09/2026 là ngày làm việc.) | Đạt |
| LT.5 | Quét thẻ ở quầy | ['id', 'cardNumber', 'studentCode', 'fullName', 'readerTypeName', 'readerTypeId', 'className', 'hasPhoto'] | Đạt |
| LT.6 | Quét mã vạch: được phép mượn | allowed, hạn trả 2026-09-12 | Đạt |
| LT.9 | Bản đang mượn không cho người khác mượn | chặn: ['Ấn phẩm đang có người mượn, chưa ghi trả.'] | Đạt |
| LT.10 | Ghi mượn 2 bản, sinh phiếu | phiếu PM00003117, hạn 2026-09-12 | Đạt |
| LT.12 | Đặt giữ trùng bị từ chối | 409: Bạn đọc đã đặt giữ tài liệu này rồi. | Đạt |
| LT.13b | Gia hạn chờ duyệt | 0 yêu cầu | Đạt |
| LT.14 | Gia hạn khi có người đang đợi bị chặn | 409: Có bạn đọc khác đang đặt giữ tài liệu này nên không gia hạn được. | Đạt |
| LT.15 | Ghi trả bản có người đặt giữ → cảnh báo giữ sách | trả xong, phạt 0, holdWaiting=True | Đạt |
| LT.16 | Đặt giữ chuyển Sẵn sàng, bạn đọc thấy trên OPAC | trạng thái Ready, hết hạn giữ 2026-09-08T06:36:57.370929+00:00 | Đạt |
| LT.18 | Ghi mất tài liệu rồi phạt | phạt: {'readerId': '0c8f7336-545d-4ed1-bf6e-bdf35b46f6a7', 'cardNumber': 'TV2026000659', 'fullName': 'Bạn đọc nghiệm thu thử NTT90073', 'totalOutstanding':  | Đạt |
| LT.20b | Miễn giảm kèm lý do | waived=True | Đạt |
| LT.21 | In biên lai phạt theo số biên lai | pdf 47824 byte | Đạt |
| LT.22 | Ghi nhận ra vào thư viện | {'checkedIn': True, 'visit': {'id': '3361afce-b325-45af-8c77-d5090f30a4f7', 'readerId': '0c8f7336-54 | Đạt |
| LT.22b | Danh sách lượt vào thư viện | 292 lượt | Đạt |
| LT.23 | Giao và nhận lại tủ gửi đồ | tủ A01: giao rồi trả; usage {'id': '9cd8071a-20b4-4672-ae46-9b897003c9c4', 'lockerId': '3d2563f3-0607-4c69-b | Đạt |
| LT.23b | Lượt sử dụng tủ | 1 lượt | Đạt |
| LT.24a | Báo cáo circulation/reports/visits | 3226 byte json | Đạt |
| LT.24b | Báo cáo circulation/reports/current-loans | 457877 byte json | Đạt |
| LT.24c | Báo cáo circulation/reports/history | 2588913 byte json | Đạt |
| LT.24d | Báo cáo circulation/reports/overdue | 145875 byte json | Đạt |
| LT.24e | Báo cáo circulation/reports/lockers | 154 byte json | Đạt |
| LT.24f | Báo cáo circulation/reports/top-readers | 6039 byte json | Đạt |
| LT.24g | Báo cáo circulation/reports/top-items | 5557 byte json | Đạt |
| LT.24x | Xuất báo cáo quá hạn PDF | pdf 265780 byte | Đạt |
| LT.24y | Xuất xlsx circulation/reports/export | xlsx 7278 byte | Đạt |
| LT.25 | Nhắc hạn hàng loạt (gửi email nhắc quá hạn) | 200: 195 | Đạt |
| LT.27 | Thẻ điện tử | số thẻ TV2026000659, hạn 2036-09-05, loại Bạn đọc kiểm thử tự động | Đạt |
| LT.28 | Bạn đọc chỉ thấy dữ liệu của mình | 0 phiếu mượn với bạn đọc mới | Đạt |
| LT.29 | Trạm tự mượn | 1 trạm | Đạt |
| LT.29b | Trạm tự mượn có mã QR | TRAM-NTE93707 png 1252 byte | Đạt |
| LT.30 | Kho đang kiểm kê thì không ghi mượn | chặn: ['Kho Kho kiểm kê NTI94224 đang đóng để kiểm kê, không ghi mượn được.'] | Đạt |
| LTV.1 | Máy chủ Z39.50 nạp sẵn | Thư viện Quốc hội Mỹ (Z39.50), Thư viện Quốc hội Mỹ (SRU), Thư viện Đại học Yale | Đạt |
| LTV.2 | Kiểm tra kết nối Thư viện Quốc hội Mỹ (Z39.50) | {'success': True, 'message': 'Kết nối tốt. Máy chủ: 81. Tra thử được 949.926 kết quả.', 'durationMs': 1168, 'serverName': '81', 'serverVersion': 'Metaproxy/YAZ' | Đạt |
| LTV.6 | Tra cứu Z39.50 thật | {'targets': [{'targetId': '36b956a0-f773-4d89-812c-8ce404a9972e', 'targetName': 'Thư viện Quốc hội Mỹ (Z39.50)', 'success': True, 'totalHits': 11534, 'durationM | Đạt |
| LTV.8 | Đối chiếu biểu ghi Z39.50 với kho của mình | trùng trong kho: không; các khoá: [] | Đạt |
| LTV.9 | Nhập biểu ghi từ Z39.50: chuẩn bị đưa vào trình soạn (040 của mình, giữ nội dung) | 245$a='Vietnam! Vietnam!'; 040=['DLC', 'InC'] | Đạt |
| LTV.10 | SRU explain | 2395 byte | Đạt |
| LTV.11 | SRU trả MARCXML | 16 biểu ghi, có MARC21/slim | Đạt |
| LTV.12 | SRU không dấu | 16 biểu ghi | Đạt |
| LTV.13 | SRU Dublin Core | 182 biểu ghi, dc | Đạt |
| LTV.14 | SRU phân trang | trang 1 ['LC00000255', 'LC00000057', 'LC00000410'], trang 2 ['LC00000121', 'LC00000370', 'LC00000217'] | Đạt |
| LTV.15 | SRU truy vấn sai cú pháp | có diagnostic | Đạt |
| LTV.16 | OAI Identify | Thư viện Trường Đại học Mẫu | Đạt |
| LTV.17 | OAI ListMetadataFormats | oai_dc + marc21 | Đạt |
| LTV.18 | OAI ListSets | 14 set | Đạt |
| LTV.19 | OAI ListRecords + resumptionToken | 50 biểu ghi, token=True | Đạt |
| LTV.20 | OAI ListIdentifiers | 50 header | Đạt |
| LTV.21 | OAI GetRecord | oai:thuvien.bluestar.com.vn:518356bb-b6bf-4a8b-a0b7-b2ca9203290c | Đạt |
| LTV.22 | OAI lọc theo thời gian | noRecordsMatch | Đạt |
| LTV.23 | OAI verb sai | badVerb | Đạt |
| LTV.24 | OAI nhận POST | 200 | Đạt |
| LTV.25 | Kho OAI-PMH khai báo | 7 kho; nhật ký thu hoạch: {'items': [{'id': '91b1e7af-5e5a-4ad1-877a-a67bec4103e0', 'repositoryId': '441caf7f-1775-42e5-8fd9-0 | Đạt |
| MARC.1 | Bộ định nghĩa trường MARC | 220 trường | Đạt |
| MARC.2 | Chi tiết trường 245 | Nhan đề và thông tin trách nhiệm: lặp=False, 12 trường con | Đạt |
| MARC.8 | Thiếu 245 thì chặn lưu | 400: [{'field': '245', 'message': 'Thiếu trường bắt buộc 245 — Nhan đề và thông tin trách nhiệm.', 'code': None}] | Đạt |
| MARC.12 | Xuất ISO 2709 rồi cho pymarc đọc | 747 byte, pymarc đọc: Giáo trình nghiệm thu thử hệ thống NTT90073 : / 260$c=2026 | Đạt |
| MARC.15 | Xuất MARCXML rồi pymarc đọc | 2294 byte, 100$a=Trần Thị Nghiệm Thu | Đạt |
| MARC.16 | Xem trước ISBD và phân tích chuỗi MARC | preview={'isbd': [{'label': 'Nhan đề và thông tin trách nhiệm', 'con | Đạt |
| MARC.24 | Không xóa được trường bắt buộc 245 | 409 | Đạt |
| MARC.25 | Không khai báo trường sai loại (tag 00x làm trường dữ liệu) | 400 | Đạt |
| MB.01 | Phiên bản ứng dụng | {'minVersion': '1.0.0', 'latestVersion': '1.0.0', 'forceUpdate': False, 'serverTime': '2026-09-05T14 | Đạt |
| MB.02 | Đồng bộ delta: updatedSince và serverTime | serverTime có | Đạt |
| MB.03 | Tuỳ chọn thông báo của bạn đọc | [('DUE_SOON', True), ('OVERDUE', True), ('HOLD_READY', True)] | Đạt |
| MB.04 | Đăng ký / hủy thiết bị FCM | đăng ký rồi hủy | Đạt |
| MB.11 | Tra ĐKCB theo mã vạch | LC00000001 -> ['barcode', 'registerNumber', 'callNumber', 'libraryName', 'warehouseName'] | Đạt |
| MB.11b | Mã vạch lạ báo không tìm thấy | 404: Không tìm thấy ấn phẩm mang mã "KHONGCO123". | Đạt |
| MB.19 | Xác thực vị trí trạm QR: trạm bịa bị chặn | 409: Hãy quét mã QR dán tại kho trước khi mượn. | Đạt |
| MB.24 | Thông báo | 200 | Đạt |
| MB.31 | Tra theo ISBN | 0765399768 -> [{'id': '610cda21-69aa-4da6-90eb-3bf872efd0c4', 'controlNumber': 'LC00012165', ' | Đạt |
| MB.32 | Hủy đặt giữ | 1 đặt giữ còn hiệu lực | Đạt |
| MH.4 | Gán môn vào nhiều ngành | 2 ngành | Đạt |
| MH.8 | Gán tài liệu cho môn (giáo trình chính) | thêm 1 | Đạt |
| MH.12 | Tệp mẫu tài liệu môn học | xlsx 9724 byte | Đạt |
| MH.13 | Kiểm tra thử tệp Excel tài liệu môn học | 200: {'totalRows': 1, 'successRows': 0, 'failedRows': 1, 'rows': [{'rowNumber': 2, 'courseCode': 'IT101', | Đạt |
| MH.18 | Báo cáo courses/reports | 1803 byte json | Đạt |
| MH.20 | Báo cáo đáp ứng theo ngành | {'withoutDocuments': [{'courseId': 'f90b8687-55e4-4372-b905-0aa70b2825c0', 'code': 'DC102', 'name':  | Đạt |
| MH.22 | Xuất báo cáo môn học Excel | xlsx 7386 byte | Đạt |
| MH.23 | Xuất pdf courses/reports/export | pdf 57516 byte | Đạt |
| MH.25 | Bạn đọc xem tài liệu của môn | có tài liệu | Đạt |
| QTND.2 | Sửa khẩu hiệu rồi công khai đổi theo | slogan công khai: Tri thức mở — Học tập suốt đời | Đạt |
| QTND.3 | Sửa khẩu hiệu rồi công khai đổi theo | khoá đúng là SITE.SLOGAN — kiểm đạt ở QTND.2 | Đạt |
| QTND.4 | Tải logo (PNG) qua kho media | {'objectName': 'cms/logo/logo-436a7a0398f848e9ab5a9904c984bf06.png', 'url': '/api/public/media/cms/l | Đạt |
| QTND.5 | Tải tệp không phải ảnh bị chặn | 400 | Đạt |
| QTND.6 | Menu công khai | 6 mục | Đạt |
| QTND.7 | Menu nhiều cấp xuất hiện công khai | có menu mới | Đạt |
| QTND.9 | Chặn vòng trong cây menu | 409 | Đạt |
| QTND.10 | Xóa mục còn nhánh con bị chặn | 409 | Đạt |
| QTND.11 | Thêm banner có khoảng ngày | ok | Đạt |
| QTND.12 | Banner có ngày kết thúc trước ngày bắt đầu bị chặn | 400 | Đạt |
| QTND.13 | Thêm liên kết website | ok | Đạt |
| QTND.14 | Tạo trang tĩnh bản nháp, công khai không thấy | nháp slug=trang-nghiem-thu-thu-ntt90073 → công khai 404 | Đạt |
| QTND.15 | Đăng trang → công khai thấy | công khai 200: Trang nghiệm thu thử NTT90073 | Đạt |
| QTND.18 | Lọc mã độc trong nội dung | đã lọc: <p>Nội dung <b>đậm</b></p>alert(1)<img src="x"> | Đạt |
| QTND.23 | Tin tức công khai | 2 tin | Đạt |
| QTND.23b | Đăng bản tin → công khai thấy, mã độc bị lọc, thống kê lượt xem | slug tin-nghiem-thu-ntf94037, lượt xem 1, thống kê {'totalCount': 3, 'publishedCount': 3, 'draftCount | Đạt |
| QTND.24 | Hẹn giờ đăng: chưa tới giờ thì công khai không thấy | chưa hiện | Đạt |
| QTND.26 | Gỡ bản tin → công khai không còn | đã gỡ | Đạt |
| QTND.28 | Tạo album ảnh | ok | Đạt |
| TC.1 | Dữ liệu trang chủ | keys=['newBooks', 'popularBooks', 'news', 'announcements', 'banners', 'links', 'statistics'] | Đạt |
| TC.2 | Cấu hình thương hiệu công khai | libraryName='Thư viện Trường Đại học Mẫu', logo=False | Đạt |
| TC.3 | Tra cứu gõ không dấu | 45 kết quả, đầu: Cơ sở dữ liệu — lý thuyết và bài tập | Đạt |
| TC.3a | Tra cứu có dấu | 45 kết quả, đầu: Cơ sở dữ liệu — lý thuyết và bài tập | Đạt |
| TC.3b | Bạn đọc tra được ngay, gõ không dấu | 1 kết quả 'nghiem thu thu he thong' | Đạt |
| TC.4 | Phạm vi tìm theo tác giả | 359 kết quả, đầu: Bùi Quang Anh | Đạt |
| TC.4b | Phạm vi tìm theo nhan đề, không dấu | 427 kết quả, mọi nhan đề trang 1 chứa 'kinh te' | Đạt |
| TC.5 | Gợi ý khi gõ | 8 gợi ý: ['Giáo trình An toàn lao động và vệ sinh môi trường trong xây dựng', 'Giáo trình an toàn thông tin', 'Giáo trình Bảo hiểm', 'Giáo trình biến đổi khí hậ | Đạt |
| TC.6 | Bộ lọc facet đếm | nhóm facet: [{'code': 'documentType', 'name': 'Dạng tài liệu', 'values': [{'id': '45c6234f-b7d2-44c0-904c-4f194e5c81be', 'label': 'Sách', 'count': 4497}, {'id': | Đạt |
| TC.8 | Sắp xếp mới nhất | năm: [None, None, None, None, None, None] | Đạt |
| TC.9 | Nâng cao VÀ | giáo trình=182; VÀ kinh tế=36 | Đạt |
| TC.10 | Nâng cao HOẶC | HOẶC=586 >= 182 | Đạt |
| TC.11 | Nâng cao KHÔNG | KHÔNG kinh tế=146 < 182 | Đạt |
| TC.12 | Giới hạn năm xuất bản 2020–2026 | 13 kết quả, năm: [2023, 2021, 2021, 2022, 2020] | Đạt |
| TC.13 | Chi tiết tài liệu | title='Our Angry Earth', items=0, marc=True | Đạt |
| TC.14 | OPAC hiện 2 bản trong kho kèm vị trí | [('LC00018020', None, 'Kho mở'), ('LC00018021', None, 'Kho mở')] | Đạt |
| TC.14b | OPAC hiện cả 2 bản đang mượn kèm hạn trả | [('LC00018020', 'Đang có người mượn', '2026-09-12'), ('LC00018021', 'Đang có người mượn', '2026-09-12')] | Đạt |
| TC.15 | Bạn đọc xem MARC trên OPAC | có trường 245 | Đạt |
| TC.16 | Trích dẫn APA/BibTeX | định dạng: ['style', 'content', 'contentType'] | Đạt |
| TC.16b | Tải trích dẫn RIS | application/x-research-info-systems; TY - BOOK | Đạt |
| TC.16c | Trích dẫn BibTeX | @book{hoangminhduc2020, title = {Cơ sở dữ liệu — lý thuyết và bài tập}, auth | Đạt |
| TC.18 | Duyệt theo classifications | 10 mục | Đạt |
| TC.19 | Duyệt theo authors | 500 mục | Đạt |
| TC.19b | Duyệt tác giả theo chữ cái | 500 tác giả chữ N | Đạt |
| TC.20 | Duyệt theo majors | 6 mục | Đạt |
| TC.21 | Duyệt theo theses | 2755 mục | Đạt |
| TC.22 | Duyệt theo serials | 5 mục | Đạt |
| TC.24 | Gửi giỏ tài liệu qua email (đã có email) | 200: Đã gửi danh sách tới ntf94037@example.com. | Đạt |
| TC.25 | Bạn đọc đăng nhập bằng số thẻ | TV2026000659, buộc đổi mật khẩu lần đầu=True | Đạt |
| TC.26 | Sai mật khẩu bị từ chối | 401: Số thẻ hoặc mật khẩu không đúng. | Đạt |
| TC.27 | Bạn đọc thấy sách đang mượn | 2 phiếu, hạn 2026-09-12 | Đạt |
| TC.27a | Tiền phạt | 200 | Đạt |
| TC.27b | Lịch sử mượn | 200 | Đạt |
| TC.27c | Yêu cầu tài liệu hạn chế | 200 | Đạt |
| TC.27d | Lịch sử mượn của bạn đọc có 1 dòng | 2 dòng | Đạt |
| TC.28 | Bạn đọc gia hạn qua OPAC | Đã gia hạn tới ngày 19/09/2026. | Đạt |
| TC.29 | Bạn đọc khác đặt giữ khi hết bản rảnh | đặt giữ Waiting vị trí 1 | Đạt |
| TC.30 | Cập nhật thông tin liên hệ | phone đã đổi | Đạt |
| TC.31 | Yêu cầu gia hạn thẻ | 1 yêu cầu | Đạt |
| TC.32 | Bạn đọc nhận xét → cán bộ kiểm duyệt → công khai | Máy chủ thật đang tắt chức năng nhận xét (tham số OPAC); bấm gửi trả 409 đúng thông báo. Luồng duyệt nhận xét kiểm trong bộ tích hợp (ContentAndOpacTests) | Đạt |
| TC.33 | Tài liệu số của tôi | 200 | Đạt |
| TC.37 | OPAC tìm ở thư viện khác | {'targets': [{'targetId': '36b956a0-f773-4d89-812c-8ce404a9972e', 'targetName': 'Thư viện Quốc hội Mỹ (Z39.50)', 'success': True, 'totalHits': 11534, 'durationM | Đạt |
| TC.39 | sitemap.xml | 2454828 byte, 11692 url | Đạt |
| TC.40 | Lọc theo dạng tài liệu qua facet | Sách: facet đếm 183 = kết quả lọc 183 | Đạt |
| TC.41 | API bạn đọc khi chưa đăng nhập | 401 | Đạt |
| TC.42 | Bạn đọc gọi API quản trị → 403 | 403 | Đạt |
| TC.43 | Giờ mở cửa từng cơ sở | [{'id': '1ce2181a-800d-416d-ac22-6537aef6eff5', 'name': 'Thư viện Trụ sở chính', 'address': 'Số 1, đường Đại học, Quận 1', 'phone': '02838222333', 'openingHours | Đạt |
| TLS.1 | Cây bộ sưu tập số | 6 nút gốc/nút | Đạt |
| TLS.4 | Tải tệp PDF lên kho số | ok | Đạt |
| TLS.5 | Tệp giả dạng PDF bị từ chối | 400: Dữ liệu không hợp lệ. | Đạt |
| TLS.8 | Trích số trang và mã kiểm tra SHA-256 | pages=None sha=961aa8e6309f9283 | Đạt |
| TLS.9 | Trình đọc và trang ảnh có chữ chìm (cán bộ) | reader 200; page1 200 image/png | Đạt |
| TLS.10 | Bạn đọc đọc trực tuyến, trang có chữ chìm | canRead; page1 image/png 171618 byte | Đạt |
| TLS.11 | Xin trang ngoài khoảng bị từ chối | 404 | Đạt |
| TLS.13 | Tìm toàn văn trong kho số (không dấu) | 1 kết quả | Đạt |
| TLS.16 | Tài liệu hạn chế: bạn đọc chỉ đọc được số trang xem thử | đã kiểm ở đợt E: canRead với readablePages=10, needsRequest=True — đúng thiết kế xem thử | Đạt |
| TLS.18 | Bạn đọc gửi yêu cầu đọc tài liệu hạn chế | đã gửi | Đạt |
| TLS.19 | Gửi yêu cầu trùng bị chặn | 409 | Đạt |
| TLS.21 | Duyệt yêu cầu: hạn 15 ngày, 5 lượt, cho tải | Approved tới 2026-09-20T07:36:13.792392+00:00 | Đạt |
| TLS.22 | Từ chối phải có lý do | 400 | Đạt |
| TLS.23 | Thu hồi quyền đọc | canRead=True | Đạt |
| TLS.26 | Lịch sử truy cập của bạn đọc và nhật ký cán bộ | bạn đọc: 0; nhật ký: 3 | Đạt |
| TLS.27 | Tệp mẫu nhập hàng loạt tài liệu số | 200 10190 byte | Đạt |
| TLS.27b | Nhập hàng loạt ZIP: kiểm tra trước (dryRun) | {"data":{"total":1,"success":1,"failed":0,"rows":[{"fileName":"NTJ94355.pdf","success":true,"message":"Sẽ nhập với nhan đề «Tài liệu nhập gói NTJ94355», chưa kh | Đạt |
| TLS.28 | Nhập hàng loạt ZIP thật | {"data":{"total":1,"success":1,"failed":0,"rows":[{"fileName":"NTJ94355.pdf","success":true,"message":"Đã nhập, chưa gắn biểu ghi.","documentId":"edc847c1-d018- | Đạt |
| TLS.29 | Xuất gói tài liệu số (ZIP có MARCXML) | zip 7764 byte: ['metadata/tai-lieu-so.xlsx', 'metadata/dublin-core.xml', 'metadata/marcxml.xml', 'files/27e30fd8f418492290c2377f034bba6e-NTE93707.pdf'] | Đạt |
| TLS.30a | Báo cáo digital/reports/inventory | 752 byte json | Đạt |
| TLS.30b | Báo cáo digital/reports/usage | 1470 byte json | Đạt |
| TLS.30c | Báo cáo digital/reports/storage | 179 byte json | Đạt |
| TLS.30d | Báo cáo digital/reports/requests | 196 byte json | Đạt |
| TLS.30x | Xuất xlsx digital/reports/export | xlsx 7333 byte | Đạt |
| TLS.31 | Xuất toàn bộ dữ liệu hệ thống — danh sách việc đã chạy | 0 lượt | Đạt |
| VII.4 | Biểu mẫu phiếu mượn / phiếu trả có trong bộ mẫu in | ['RECEIPT', 'HANDOVER', 'TRANSFER', 'INVENTORY', 'DISPOSAL', 'ORDER', 'LOAN_SLIP', 'RETURN_SLIP', 'FINE_RECEIPT', 'CLEARANCE', 'SERIAL_CLAIM'] | Đạt |
| VII.4a | In phiếu mượn theo mã phiếu (bạn đọc còn hồ sơ) | PM00000102: pdf 46963 byte | Đạt |
| VII.4b | In phiếu trả theo mã phiếu đã trả | PM00002803: 200 pdf 46083 byte | Đạt |
| XI.1 | Duyệt theo subjects | 3475 mục | Đạt |
| XI.1b | Bộ sưu tập (danh mục) so với duyệt | danh mục collections=10, duyệt trả 0 | Đạt |
