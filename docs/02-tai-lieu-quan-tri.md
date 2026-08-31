# Tài liệu quản trị hệ thống — LibraryConnect

Dành cho quản trị viên vận hành hệ thống hằng ngày. Việc cài đặt lần đầu xem
`04-cai-dat-cau-hinh.md`; sao lưu và phục hồi xem `03-sao-luu-phuc-hoi.md`.

---

## 1. Kiến trúc hệ thống

### 1.1. Các thành phần đang chạy

| Container | Vai trò | Cổng mặc định |
|---|---|---|
| `lc-nginx` | Reverse proxy, điểm vào duy nhất từ bên ngoài | 80 |
| `lc-api` | Backend .NET 8: toàn bộ nghiệp vụ, xác thực, tác vụ nền | 8080 (nội bộ) |
| `lc-admin` | Giao diện quản trị (React, phục vụ tĩnh) | 80 (nội bộ) |
| `lc-postgres` | Cơ sở dữ liệu PostgreSQL 16 | 5432 |
| `lc-redis` | Cache và hàng đợi | 6379 |
| `lc-minio` | Kho lưu trữ tệp tài liệu số | 9000 / 9001 |

Nginx định tuyến: `/admin` → giao diện quản trị, `/api` → backend, `/swagger` → tài liệu API,
`/hangfire` → bảng điều khiển tác vụ nền, `/sru` và `/oai` → các giao thức liên thư viện.

### 1.2. Kiến trúc ba tầng

Yêu cầu của E-HSMT là ba tầng tách bạch; hệ thống thực hiện như sau:

- **Tầng trình bày** — giao diện React. Chỉ gọi REST API, **không** kết nối cơ sở dữ liệu.
- **Tầng nghiệp vụ** — `LibraryConnect.Application`. Mỗi chức năng là một use case độc lập, kèm bộ
  kiểm tra dữ liệu đầu vào riêng. Controller chỉ nhận yêu cầu và chuyển tiếp.
- **Tầng dữ liệu** — `LibraryConnect.Infrastructure` (EF Core, Redis, MinIO) và `Domain` (thực thể,
  quy tắc nghiệp vụ thuần).

Nhờ tách bạch như vậy, ứng dụng di động ở đợt sau dùng lại nguyên vẹn tầng nghiệp vụ qua cùng bộ
REST API mà không phải viết lại logic.

### 1.3. Cấu trúc cơ sở dữ liệu

113 bảng chia thành 10 schema theo nhóm nghiệp vụ:

| Schema | Nội dung |
|---|---|
| `sys` | Người dùng, nhóm, quyền, tham số, nhật ký, sao lưu, phiên đăng nhập |
| `cat` | Danh mục nghiệp vụ: dạng tài liệu, ngôn ngữ, tác giả, chủ đề, phân loại, khoa, ngành, môn học |
| `bib` | Biểu ghi thư mục MARC 21, định nghĩa trường, mẫu biên mục, hàng đợi biên mục, mẫu phích |
| `acq` | Thư viện, kho, giá, yêu cầu mua, đơn đặt, ĐKCB, chuyển kho, thanh lý, kiểm kê |
| `ser` | Ấn phẩm định kỳ: đầu báo, số, bài trích, đóng tập, khiếu nại |
| `dig` | Tài liệu số: bộ sưu tập, tệp, yêu cầu truy cập, nhật ký đọc |
| `rdr` | Bạn đọc, thẻ, vi phạm, nhập liệu hàng loạt |
| `cir` | Chính sách lưu thông, mượn trả, gia hạn, đặt giữ, tiền phạt, tủ đồ, ra vào thư viện |
| `web` | Nội dung trang thông tin, tin tức, banner, menu, tương tác trên OPAC |
| `ill` | Liên thư viện: server Z39.50, nguồn OAI-PMH, tác vụ nhập/xuất dữ liệu |

Quy ước chung của mọi bảng nghiệp vụ:

- Khóa chính `id` kiểu `uuid`.
- Cột kiểm toán: `created_at`, `created_by`, `updated_at`, `updated_by`.
- **Xóa mềm** qua `deleted_at`. Hệ thống không bao giờ xóa cứng dữ liệu — đây là yêu cầu lưu trữ vĩnh
  viễn của E-HSMT. Mọi truy vấn tự động bỏ qua bản ghi đã xóa.

---

## 2. Phân quyền

### 2.1. Mô hình

Người dùng → thuộc một hoặc nhiều **nhóm** → mỗi nhóm được cấp một tập **quyền**. Quyền hiệu lực của
một người là hợp của tất cả quyền trong các nhóm họ tham gia.

Mã quyền theo dạng `MODULE.ĐỐI_TƯỢNG.HÀNH_ĐỘNG`, ví dụ `CATALOG.BIB.CREATE`,
`CIRCULATION.LOAN.RETURN`, `ACQ.ORDER.APPROVE`. Hệ thống hiện có **161 mã quyền**.

### 2.2. Năm nhóm mẫu

| Mã nhóm | Tên | Phạm vi |
|---|---|---|
| `SYS_ADMIN` | Quản trị hệ thống | Toàn quyền |
| `CATALOGER` | Cán bộ biên mục | Biên mục MARC 21, định nghĩa trường, hàng đợi, in phích, nhập biểu ghi |
| `ACQUISITION` | Cán bộ bổ sung | Yêu cầu mua, đơn đặt, kiểm nhận, ĐKCB, mã vạch, kiểm kê, ấn phẩm định kỳ |
| `CIRCULATION` | Cán bộ lưu thông | Ghi mượn/trả, gia hạn, đặt giữ, tiền phạt, tủ đồ, báo cáo lưu thông |
| `LIBRARIAN` | Thủ thư | Hồ sơ bạn đọc, tài liệu số, nội dung trang thông tin |

Nhóm hệ thống không xóa được và không vô hiệu hóa được, nhưng **được phép sửa danh sách quyền** để
khớp với cách phân công thực tế của đơn vị.

### 2.3. Một số ràng buộc an toàn

Hệ thống chủ động chặn các thao tác dễ gây khóa chính mình ra ngoài:

- Không tự khóa hoặc tự vô hiệu hóa tài khoản đang đăng nhập.
- Không tự xóa tài khoản đang đăng nhập.
- Không xóa tài khoản quản trị hệ thống **cuối cùng** còn hoạt động.
- Không xóa nhóm còn thành viên.

### 2.4. Khi thay đổi quyền có hiệu lực

Danh sách quyền nằm trong access token của người dùng. Sau khi sửa quyền của một nhóm, thành viên
nhận quyền mới ở lần làm mới token kế tiếp — chậm nhất là sau một vòng đời access token (mặc định
60 phút), hoặc ngay lập tức nếu họ đăng xuất và đăng nhập lại.

Muốn có hiệu lực tức thì cho một người: khóa rồi mở khóa tài khoản đó, hoặc đặt lại mật khẩu — cả
hai đều thu hồi mọi phiên hiện có.

---

## 3. Nhật ký hệ thống

### 3.1. Hệ thống ghi những gì

Ghi tự động, không phụ thuộc lập trình viên có nhớ hay không: mọi thao tác **thêm / sửa / xóa** trên
các bảng nghiệp vụ đều sinh một bản ghi nhật ký, kèm ảnh chụp giá trị trước và sau ở dạng JSON.

Ngoài ra, các sự kiện sau được ghi tường minh:

| Sự kiện | Ghi chú |
|---|---|
| Đăng nhập / Đăng nhập thất bại / Đăng xuất | Kèm địa chỉ IP và trình duyệt |
| Thay đổi phân quyền | Số quyền được thêm và bị bỏ |
| Thay đổi tham số | Kèm lịch sử giá trị cũ → mới |
| Sao lưu / Phục hồi | Kể cả các lần thất bại |
| Xuất dữ liệu | Xuất nhật ký, tải bản sao lưu |
| Nhập dữ liệu | Số dòng thành công/thất bại |

Bản ghi nhật ký được ghi **trong cùng giao dịch** với thay đổi nghiệp vụ, nên không thể có trường hợp
dữ liệu đổi mà nhật ký thiếu, hay ngược lại.

### 3.2. Những gì không bao giờ vào nhật ký

Mật khẩu (kể cả dạng băm), khóa bí mật của ứng dụng tích hợp, refresh token và giá trị của các tham
số được đánh dấu là bí mật. Với tham số bí mật, lịch sử chỉ ghi nhận *đã có thay đổi*.

### 3.3. Cài đặt chế độ ghi nhận

Quản trị hệ thống → Nhật ký hệ thống → tab **Cài đặt ghi nhận**.

Với từng đối tượng nghiệp vụ, bật/tắt riêng cho Thêm mới, Cập nhật, Xóa và Xem. Cột *Thời gian lưu*
để trống nghĩa là **giữ vĩnh viễn** — đây là mặc định và là yêu cầu của hồ sơ mời thầu. Chỉ đặt số
ngày khi có lý do rõ ràng, ví dụ nhật ký *Xem* của tài liệu số sinh ra rất nhiều bản ghi.

Ghi nhận *Xem* mặc định chỉ bật cho hồ sơ bạn đọc và tài liệu số.

### 3.4. Xuất nhật ký

Nút **Xuất Excel** và **Xuất PDF** xuất đúng bộ lọc đang áp dụng trên màn hình, tối đa 50.000 dòng
mỗi lần. Bản PDF in kèm tiêu chí lọc để một bản in ký tên tự nó giải thích được phạm vi dữ liệu.

---

## 4. Giám sát vận hành

### 4.1. Kiểm tra sức khỏe

| Địa chỉ | Ý nghĩa | Dùng cho |
|---|---|---|
| `/health` | Tiến trình còn sống | Load balancer, giám sát ngoài |
| `/health/ready` | Kết nối được PostgreSQL và Redis | Kiểm tra trước khi đưa vào phục vụ |

`/health/ready` trả JSON liệt kê từng thành phần kèm thời gian phản hồi, tiện cho việc khoanh vùng
sự cố.

### 4.2. Nhật ký kỹ thuật

```bash
docker compose logs -f api          # theo dõi trực tiếp
docker compose logs --tail 200 api  # 200 dòng gần nhất
```

Log dạng JSON có cấu trúc được ghi vào thư mục `logs/` trong container (volume `api-logs`), luân
chuyển theo ngày và giữ 90 tệp gần nhất.

### 4.3. Tác vụ nền

Bảng điều khiển: `http://<máy-chủ>/hangfire` (cần quyền `SYSTEM.JOB.VIEW`).

| Tác vụ | Lịch | Nội dung |
|---|---|---|
| `libraryconnect:auto-backup` | Theo tham số, mặc định 2:00 hằng ngày | Sao lưu tự động |
| `libraryconnect:audit-purge` | 3:30 hằng ngày | Dọn nhật ký quá thời hạn lưu trữ đã cấu hình |
| `libraryconnect:token-cleanup` | 4:00 hằng ngày | Dọn refresh token hết hạn quá 30 ngày |

Các tác vụ của những phân hệ sau (tính quá hạn, gửi email nhắc trả, thu hoạch OAI-PMH) sẽ xuất hiện
ở đây khi phân hệ tương ứng được bàn giao.

---

## 5. Xử lý sự cố thường gặp

| Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|
| Người dùng báo *Tài khoản đang bị khóa đến …* | Nhập sai mật khẩu quá số lần cho phép | Quản trị hệ thống → Người dùng → biểu tượng mở khóa. Ngưỡng khóa chỉnh trong *Chính sách mật khẩu* |
| Nhiều người báo lỗi *quá nhiều yêu cầu* khi đăng nhập | Cả thư viện đi ra Internet qua một IP NAT, chạm giới hạn tần suất | Tăng `LC_RateLimit__LoginPerMinute` trong `.env`, khởi động lại `api` |
| Đã sửa quyền nhóm nhưng người dùng chưa thấy chức năng mới | Quyền nằm trong access token đang dùng | Yêu cầu đăng xuất/đăng nhập lại, hoặc đặt lại mật khẩu để thu hồi phiên |
| Giao diện hiện *Phiên đăng nhập đã hết hạn* liên tục | Đồng hồ máy chủ lệch, hoặc đổi `LC_Jwt__Secret` khi đang có người dùng | Đồng bộ giờ máy chủ (NTP); nếu vừa đổi khóa ký thì yêu cầu mọi người đăng nhập lại |
| Trang quản trị trắng sau khi nâng cấp | Trình duyệt còn giữ tệp cũ trong cache | Tải lại bỏ qua cache (Ctrl+F5) |
| Tải tài liệu số báo *Chưa cấu hình kho lưu trữ tệp MinIO* | Thiếu khóa truy cập MinIO | Bổ sung `LC_Minio__AccessKey` / `SecretKey`, khởi động lại `api` |
| Sao lưu báo *Không tìm thấy công cụ pg_dump* | API chạy ngoài container, máy chủ chưa có PostgreSQL client | Cài `postgresql-client` hoặc đặt `LC_Backup__PgDumpPath` |
| Tra cứu không ra kết quả dù dữ liệu có | Chỉ mục tìm kiếm chưa cập nhật cho dữ liệu nhập trực tiếp bằng SQL | Cập nhật qua giao diện hoặc API; chỉ mục do trigger duy trì khi biểu ghi được ghi qua ứng dụng |

---

## 6. Bảo trì định kỳ

| Việc | Tần suất | Ghi chú |
|---|---|---|
| Kiểm tra bản sao lưu gần nhất chạy thành công | Hằng ngày | Màn hình Sao lưu, cột Trạng thái |
| Đưa bản sao lưu ra khỏi máy chủ | Hằng ngày | Xem `03-sao-luu-phuc-hoi.md` mục 4 |
| Kiểm tra dung lượng ổ đĩa còn trống | Hằng tuần | Hiển thị sẵn trên màn hình Sao lưu |
| Rà nhật ký đăng nhập thất bại bất thường | Hằng tuần | Nhật ký → lọc *Đăng nhập thất bại* |
| Phục hồi thử lên máy chủ thử nghiệm | Hằng quý | Xem `03-sao-luu-phuc-hoi.md` mục 7 |
| Rà soát tài khoản của cán bộ đã nghỉ việc | Hằng quý | Người dùng → lọc theo trạng thái |
| Cập nhật phiên bản hệ thống | Khi có bản mới | Luôn sao lưu trước khi nâng cấp |
