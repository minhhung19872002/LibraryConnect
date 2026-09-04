# Hồ sơ bàn giao và biểu mẫu nghiệm thu

> Đáp ứng Chương V, mục 5.5 của E-HSMT ("Hồ sơ bàn giao tối thiểu"). Mỗi mẫu dưới đây điền xong, ký và
> đóng thành một tập; phần trong ngoặc *(nghiêng)* là chỗ điền khi triển khai thật.

---

## 0. Danh mục hồ sơ bàn giao

| # | Hồ sơ | Nguồn / mẫu | Đã có |
|---|---|---|---|
| 1 | Thông tin sản phẩm, phiên bản, phạm vi quyền sử dụng | Mẫu 1 dưới đây | ☐ |
| 2 | Thông tin quản trị hệ thống và danh sách thành phần đã cài | Mẫu 2 + `02-tai-lieu-quan-tri.md` | ☐ |
| 3 | Tài liệu hướng dẫn sử dụng | `01-huong-dan-su-dung.md` | ☐ |
| 4 | Tài liệu quản trị hệ thống | `02-tai-lieu-quan-tri.md` | ☐ |
| 5 | Tài liệu sao lưu / phục hồi | `03-sao-luu-phuc-hoi.md` | ☐ |
| 6 | Tài liệu cài đặt / cấu hình | `04-cai-dat-cau-hinh.md` | ☐ |
| 7 | Tài liệu API và giao thức kết nối | `05-api-reference.md` | ☐ |
| 8 | Danh sách tài khoản và nhóm quyền ban đầu | Mẫu 3 dưới đây | ☐ |
| 9 | Biên bản khảo sát hạ tầng | Mẫu 4 | ☐ |
| 10 | Biên bản chuyển đổi và đối soát dữ liệu | Mẫu 5 | ☐ |
| 11 | Kịch bản và kết quả kiểm thử | `06-kich-ban-kiem-thu.md` (điền hai cột cuối) | ☐ |
| 12 | Biên bản chạy thử | Mẫu 6 | ☐ |
| 13 | Biên bản đào tạo | Mẫu 7 | ☐ |
| 14 | Tài liệu bảo hành, bảo trì và đầu mối hỗ trợ | `12-bao-hanh-ho-tro.md` | ☐ |
| 15 | Bảng đáp ứng kỹ thuật | `07-bang-dap-ung-ky-thuat.md` | ☐ |
| 16 | Biên bản nghiệm thu, bàn giao | Mẫu 8 | ☐ |

---

## Mẫu 1 — Thông tin sản phẩm và phạm vi quyền sử dụng

| Mục | Nội dung |
|---|---|
| Tên sản phẩm | LibraryConnect — Phần mềm Thư viện số |
| Phiên bản bàn giao | *(ví dụ 1.0.0, kèm mã commit)* |
| Ngày dựng bản | *(…)* |
| Phạm vi quyền sử dụng | Theo hợp đồng số *(…)*: số máy chủ, số người dùng đồng thời, thời hạn |
| Thành phần nguồn mở sử dụng | Danh sách kèm giấy phép — xem `00-quyet-dinh-ky-thuat.md` |
| Cơ sở dữ liệu | PostgreSQL 16, không có phần mã hoá riêng nào khoá dữ liệu |
| Định dạng dữ liệu mở | MARC 21, ISO 2709, MARCXML, Dublin Core, Excel, PDF |

---

## Mẫu 2 — Thành phần đã cài đặt

| Thành phần | Phiên bản | Cổng | Ghi chú |
|---|---|---|---|
| API (`libraryconnect/api`) | *(…)* | 8080 nội bộ | .NET 8 |
| Trang quản trị (`libraryconnect/admin`) | *(…)* | 80 nội bộ | React 18 |
| Trang tra cứu (`libraryconnect/opac`) | *(…)* | 80 nội bộ | React 18 |
| PostgreSQL | 16 | 5432 nội bộ | Dữ liệu ở volume *(…)* |
| Redis | 7 | 6379 nội bộ | Bộ đệm và hàng đợi |
| MinIO | *(…)* | 9000 / 9001 | Ba bucket: `lc-documents`, `lc-images`, `lc-backups` |
| Nginx | *(…)* | 80 / 443 | Cấu hình dùng: *(nginx.prod.conf hoặc nginx.behind-proxy.conf)* |
| Máy chủ Z39.50 | — | *(210)* | Bật/tắt bằng tham số `ILL.Z3950_SERVER_ENABLED` |

Thông tin quản trị bàn giao riêng, **không ghi mật khẩu vào hồ sơ này**: tài khoản quản trị được giao
kèm mật khẩu tạm và hệ thống bắt đổi ngay ở lần đăng nhập đầu.

---

## Mẫu 3 — Tài khoản và nhóm quyền ban đầu

Năm nhóm quyền nạp sẵn:

| Mã nhóm | Tên | Phạm vi công việc |
|---|---|---|
| `SYS_ADMIN` | Quản trị hệ thống | Toàn quyền |
| `CATALOGER` | Cán bộ biên mục | Biên mục, định nghĩa MARC, hàng đợi, phích, nhập biểu ghi |
| `ACQUISITION` | Cán bộ bổ sung | Yêu cầu, đơn đặt, kiểm nhận, ĐKCB, kiểm kê, ấn phẩm định kỳ |
| `CIRCULATION` | Cán bộ lưu thông | Ghi mượn, ghi trả, đặt giữ, tiền phạt, tủ đồ, báo cáo lưu thông |
| `LIBRARIAN` | Thủ thư | Bạn đọc, tài liệu số, nội dung trang thông tin |

Danh sách tài khoản bàn giao:

| STT | Họ tên | Tài khoản | Nhóm quyền | Phạm vi dữ liệu (thư viện / kho / dạng tài liệu) |
|---|---|---|---|---|
| 1 | *(…)* | *(…)* | *(…)* | *(…)* |

---

## Mẫu 4 — Biên bản khảo sát hạ tầng

**Thời gian:** *(…)* **Địa điểm:** *(…)*

**Thành phần:** đại diện Chủ đầu tư *(…)*; đại diện Nhà thầu *(…)*

| Hạng mục | Hiện trạng ghi nhận | Đủ điều kiện | Việc cần làm trước khi cài |
|---|---|---|---|
| Máy chủ (CPU/RAM/đĩa/ảo hoá) | *(…)* | ☐ Đủ ☐ Chưa | *(…)* |
| Hệ điều hành máy chủ | *(…)* | ☐ ☐ | |
| Mạng, cổng ra Internet | *(…)* | ☐ ☐ | |
| Tên miền và chứng thư TLS | *(…)* | ☐ ☐ | |
| Ổ đĩa cho bản sao lưu | *(…)* | ☐ ☐ | |
| Máy chủ thư (SMTP) | *(…)* | ☐ ☐ | |
| Dữ liệu nguồn cần chuyển đổi | *(…)* | ☐ ☐ | |
| Thiết bị: máy quét, máy in tem, máy in thẻ | *(…)* | ☐ ☐ | |

**Kết luận:** ☐ Đủ điều kiện triển khai từ ngày *(…)* ☐ Cần bổ sung các mục nêu trên

---

## Mẫu 5 — Biên bản chuyển đổi và đối soát dữ liệu

**Nguồn dữ liệu:** *(tên phần mềm cũ, định dạng xuất, ngày trích)*

### 5.1. Đối soát số lượng

| Loại dữ liệu | Số bản ghi nguồn | Nhập thành công | Bỏ qua (trùng) | Lỗi | Cộng khớp |
|---|---|---|---|---|---|
| Biểu ghi thư mục | *(…)* | *(…)* | *(…)* | *(…)* | ☐ |
| Ấn phẩm (ĐKCB) | | | | | ☐ |
| Bạn đọc | | | | | ☐ |
| Giao dịch mượn trả đang mở | | | | | ☐ |
| Tài liệu số | | | | | ☐ |

Bốn cột giữa phải cộng đúng bằng cột "số bản ghi nguồn". Lệch một bản cũng phải giải thích được.

### 5.2. Đối soát mẫu

Rút ngẫu nhiên *(số lượng ≥ 30 hoặc 5%)* biểu ghi, so từng trường với nguồn:

| STT | Mã biểu ghi | Trường sai (nếu có) | Đã xử lý |
|---|---|---|---|
| 1 | *(…)* | *(…)* | ☐ |

### 5.3. Đối soát quan hệ

| Phép kiểm | Kết quả mong đợi | Thực tế | Đạt |
|---|---|---|---|
| Mỗi ĐKCB có biểu ghi mẹ | 0 bản mồ côi | *(…)* | ☐ |
| Mỗi phiếu mượn đang mở có bạn đọc và ĐKCB có thật | 0 phiếu treo | *(…)* | ☐ |
| Mỗi bạn đọc thuộc một loại bạn đọc có trong danh mục | 0 bạn đọc lạc loại | *(…)* | ☐ |
| Xuất lại toàn kho, đọc bằng thư viện MARC bên thứ ba | 0 biểu ghi lỗi | *(…)* | ☐ |

**Kết luận:** ☐ Dữ liệu chuyển đổi đạt ☐ Cần xử lý các mục nêu trên rồi nhập lại

---

## Mẫu 6 — Biên bản chạy thử

**Thời gian chạy thử:** từ *(…)* đến *(…)* **Môi trường:** *(…)*

| Nhóm kiểm thử (mục 5.2 E-HSMT) | Số kịch bản | Đạt | Không đạt | Ghi chú |
|---|---|---|---|---|
| 2.1 Kiểm tra cài đặt | *(…)* | | | |
| 2.2 Kiểm tra chức năng 11 phân hệ | | | | |
| 2.3 Phân quyền và nhật ký | | | | |
| 2.4 Trao đổi dữ liệu | | | | |
| 2.5 Chuyển đổi dữ liệu | | | | |
| 2.6 Sao lưu và phục hồi | | | | |
| 2.7 Ứng dụng di động | | | | |
| 2.8 Báo cáo | | | | |

**Lỗi phát hiện trong kỳ chạy thử:**

| STT | Mô tả | Mức | Ngày báo | Ngày khắc phục | Đã kiểm lại |
|---|---|---|---|---|---|
| 1 | *(…)* | *(…)* | | | ☐ |

**Kết luận:** ☐ Đủ điều kiện nghiệm thu ☐ Còn lỗi nghiêm trọng phải khắc phục

---

## Mẫu 7 — Biên bản đào tạo

**Lớp:** *(nhóm A/B/C/D/E/F/G theo `11-ke-hoach-dao-tao.md`)* **Buổi:** *(…)*
**Thời gian:** *(…)* **Địa điểm:** *(…)* **Giảng viên:** *(…)*

**Nội dung đã giảng:** *(theo bảng nội dung của buổi)*

**Bài thực hành nghiệm thu:** *(kịch bản kiểm thử tương ứng)*

| STT | Họ tên học viên | Đơn vị | Kết quả bài thực hành | Chữ ký |
|---|---|---|---|---|
| 1 | *(…)* | *(…)* | ☐ Đạt ☐ Chưa đạt | |

**Ý kiến của học viên:** *(…)*

---

## Mẫu 8 — Biên bản nghiệm thu, bàn giao

**Căn cứ:** E-HSMT gói thầu *(…)*; E-HSDT của Nhà thầu; Hợp đồng số *(…)*; Bảng đáp ứng kỹ thuật; kế
hoạch triển khai; kịch bản và kết quả kiểm thử; biên bản chuyển đổi dữ liệu; biên bản đào tạo.

**Thời gian:** *(…)* **Địa điểm:** *(…)*

**Thành phần:** Chủ đầu tư *(…)*; Nhà thầu *(…)*

### Nội dung nghiệm thu

| # | Điều kiện (mục 5.4 E-HSMT) | Căn cứ | Đạt |
|---|---|---|---|
| 1 | Các chức năng bắt buộc đáp ứng yêu cầu | Bảng đáp ứng kỹ thuật, kết quả kiểm thử | ☐ |
| 2 | Cài đặt và cấu hình hoàn tất | Biên bản chạy thử, Mẫu 2 | ☐ |
| 3 | Dữ liệu chuyển đổi đã kiểm tra, đối soát | Mẫu 5 | ☐ |
| 4 | Đào tạo, chuyển giao hoàn thành | Mẫu 7 (đủ 16 buổi) | ☐ |
| 5 | Kiểm thử sao lưu / phục hồi đạt | Kịch bản nhóm 2.6 | ☐ |
| 6 | Hồ sơ bàn giao đầy đủ | Danh mục ở mục 0 | ☐ |
| 7 | Lỗi nghiêm trọng đã khắc phục | Mẫu 6, phần lỗi | ☐ |

### Kết luận

☐ **Nghiệm thu đạt**, đưa vào sử dụng từ ngày *(…)*. Thời hạn bảo hành 12 tháng tính từ ngày này.

☐ **Chưa đạt**, Nhà thầu khắc phục các nội dung sau và đề nghị nghiệm thu lại: *(…)*

| Đại diện Chủ đầu tư | Đại diện Nhà thầu |
|---|---|
| *(ký, ghi rõ họ tên)* | *(ký, ghi rõ họ tên)* |
