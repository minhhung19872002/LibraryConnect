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

## Cách chạy bộ kiểm thử tự động

```bash
cd backend
dotnet test                 # 57 unit test + 20 integration test
```

Integration test tự khởi tạo một container PostgreSQL 16 riêng, chạy migration, nạp dữ liệu nền và
gọi API qua đúng giao diện HTTP mà trình duyệt dùng — không có thành phần nào bị giả lập.

```bash
cd frontend-admin
npm test                    # 13 test giao diện
```

---

## Ghi chú

Các nhóm kiểm thử 2.2 (chức năng của 11 phân hệ), 2.4 (trao đổi dữ liệu ISO 2709 / Z39.50 / OAI-PMH),
2.5 (chuyển đổi dữ liệu), 2.7 (ứng dụng di động) và 2.8 (báo cáo) sẽ được bổ sung vào tài liệu này
theo từng phân hệ được bàn giao. Tài liệu luôn phản ánh đúng phạm vi đã hoàn thành tại thời điểm
nghiệm thu, không liệt kê trước những gì chưa làm.
