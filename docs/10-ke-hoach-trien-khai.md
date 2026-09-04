# Kế hoạch triển khai, chạy thử và chuyển đổi dữ liệu

> Đáp ứng Chương V, mục III.1 ("Yêu cầu cài đặt, cấu hình và chạy thử") và mục 5.1–5.4 của E-HSMT.
> Tài liệu này nói **ai làm gì, ngày nào, và căn cứ nào để nói là xong**. Phần thao tác kỹ thuật chi
> tiết nằm ở `04-cai-dat-cau-hinh.md`; phần kịch bản kiểm thử nằm ở `06-kich-ban-kiem-thu.md`.

Hợp đồng trọn gói, thời gian thực hiện **90 ngày** kể từ ngày hợp đồng có hiệu lực. Mốc thời gian
dưới đây tính theo ngày làm việc, ký hiệu N là ngày hợp đồng có hiệu lực.

---

## 1. Bốn giai đoạn

| Giai đoạn | Thời gian | Kết quả bàn giao |
|---|---|---|
| A. Khảo sát và chốt điều kiện triển khai | N → N+10 | Biên bản khảo sát hạ tầng; bản xác nhận điều kiện triển khai |
| B. Cài đặt, cấu hình, chuyển đổi dữ liệu | N+11 → N+45 | Hệ thống chạy trên hạ tầng của Chủ đầu tư; biên bản chuyển đổi và đối soát dữ liệu |
| C. Kiểm thử và vận hành thử | N+46 → N+70 | Kết quả 8 nhóm kiểm thử của mục 5.2; biên bản chạy thử |
| D. Đào tạo, nghiệm thu, bàn giao | N+71 → N+90 | Biên bản đào tạo; hồ sơ bàn giao; biên bản nghiệm thu |

Bốn giai đoạn gối đầu ở hai chỗ: đào tạo cán bộ biên mục bắt đầu ngay khi giai đoạn B xong phần biên
mục (không đợi hết B), và việc sửa lỗi phát hiện trong giai đoạn C chạy song song với đào tạo.

---

## 2. Giai đoạn A — Khảo sát và chốt điều kiện triển khai

### 2.1. Nội dung khảo sát

| Hạng mục | Cần xác nhận | Ảnh hưởng nếu thiếu |
|---|---|---|
| Máy chủ | Số CPU, RAM, dung lượng đĩa, vật lý hay ảo hóa, hệ điều hành | Quyết định cấu hình `docker-compose.prod.yml` và hạn mức tài nguyên |
| Mạng | Dải IP, cổng mở ra Internet, có proxy dùng chung hay không | Chọn `nginx.prod.conf` (tự cầm chứng thư) hay `nginx.behind-proxy.conf` |
| Tên miền và chứng thư | Tên miền công khai của trang tra cứu; ai cấp chứng thư TLS | Không có thì trang tra cứu chạy HTTP, không đạt mục 6.4 |
| Thư mục sao lưu | Ổ đĩa gắn ngoài dành cho bản sao lưu, dung lượng | Bản sao lưu ghi vào trong container sẽ mất khi dựng lại |
| Thư điện tử | Máy chủ SMTP, tài khoản gửi, tên hiển thị | Không có thì thư nhắc hạn trả và thư cảnh báo sao lưu không đi được |
| Dữ liệu nguồn | Phần mềm cũ, số biểu ghi, số bạn đọc, định dạng xuất được | Quyết định đường chuyển đổi ở mục 4 |
| Thiết bị đầu cuối | Máy quét mã vạch, máy in tem, máy in thẻ nhựa, cổng từ | Quyết định khổ tem và mẫu thẻ nạp sẵn |
| Người dùng | Số cán bộ theo từng nghiệp vụ, cơ cấu khoa/phòng | Quyết định danh sách tài khoản và nhóm quyền ban đầu |

### 2.2. Sản phẩm của giai đoạn

1. **Biên bản khảo sát hạ tầng** — theo mẫu ở `13-bieu-mau-ban-giao.md`.
2. **Bản xác nhận điều kiện triển khai**: Chủ đầu tư xác nhận máy chủ, tên miền, chứng thư, tài khoản
   SMTP và ổ đĩa sao lưu đã sẵn sàng. Đây là mốc bắt đầu tính giai đoạn B.
3. **Danh sách tài khoản và nhóm quyền ban đầu** do Chủ đầu tư duyệt.

---

## 3. Giai đoạn B — Cài đặt và cấu hình

### 3.1. Thứ tự việc

1. Cài Docker Engine và Docker Compose trên máy chủ (`04-cai-dat-cau-hinh.md`, mục 2).
2. Chép mã nguồn hoặc ảnh Docker đã dựng sẵn; tạo `.env` từ `.env.example`, điền 64 biến.
3. Sinh khoá JWT, mật khẩu cơ sở dữ liệu, khoá MinIO — **không dùng giá trị mẫu trong tài liệu**.
4. `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`.
5. Đợi migration chạy xong và bộ nạp dữ liệu tạo tài khoản quản trị; đổi mật khẩu tạm ngay lần đăng
   nhập đầu tiên.
6. Khai tham số hệ thống: tên thư viện, địa chỉ, logo, giờ mở cửa từng cơ sở, quy tắc sinh mã, chính
   sách mật khẩu, cấu hình SMTP, lịch sao lưu.
7. Khai cơ cấu tổ chức: thư viện và kho, giá sách, khoa, ngành, loại bạn đọc, chính sách lưu thông.
8. Lập tài khoản cán bộ theo danh sách đã duyệt ở giai đoạn A, gán nhóm quyền và **phạm vi dữ liệu**
   (thư viện, kho, dạng tài liệu).
9. Cấu hình liên thư viện: máy chủ Z39.50 đích, kho OAI-PMH cần thu hoạch.

### 3.2. Căn cứ nói là xong

- `/health/ready` trả `Healthy` với đủ ba thành phần `postgresql`, `redis`, `minio`.
- Đăng nhập được bằng tài khoản của từng nhóm quyền và thấy đúng phần việc của nhóm ấy.
- Trang tra cứu công khai mở được bằng tên miền thật, có chứng thư hợp lệ.
- Kịch bản 2.1.1 → 2.1.6 trong `06-kich-ban-kiem-thu.md` đạt.

---

## 4. Chuyển đổi dữ liệu

### 4.1. Ba đường vào, chọn theo thứ tự ưu tiên

| Ưu tiên | Đường | Dùng khi | Công cụ trong sản phẩm |
|---|---|---|---|
| 1 | ISO 2709 / MARCXML | Phần mềm cũ xuất được biểu ghi MARC | Biên mục → Nhập biểu ghi (4 bước, có bước xử lý trùng) |
| 2 | Z39.50 | Phần mềm cũ có máy chủ Z39.50 | Biên mục → Nhập từ Z39.50 |
| 3 | Excel | Dữ liệu nằm ở bảng tính hoặc kết xuất phẳng | Biên mục → Nhập từ Excel, có bước ánh xạ cột sang trường MARC và lưu được hồ sơ ánh xạ |

Bạn đọc nhập bằng Excel (Bạn đọc → Nhập xuất dữ liệu), ảnh bạn đọc nhập bằng tệp ZIP đặt tên theo mã
sinh viên hoặc số thẻ. Dữ liệu mượn trả đang mở nhập bằng Excel theo mẫu do hai bên thống nhất.

### 4.2. Quy trình bắt buộc cho mỗi lượt chuyển đổi

1. **Chạy thử trên bản sao**: nhập vào cơ sở dữ liệu kiểm thử trước, không nhập thẳng vào bản chạy thật.
2. **Đối soát số lượng**: số bản ghi nguồn — số nhập thành công — số bỏ qua — số lỗi, bốn con số phải
   cộng khớp. Màn hình nhập in ra đúng bốn con số ấy và tải được tệp nhật ký lỗi.
3. **Đối soát mẫu**: rút ngẫu nhiên tối thiểu 30 biểu ghi (hoặc 5% nếu kho nhỏ hơn 600) và so từng
   trường với dữ liệu nguồn.
4. **Đối soát quan hệ**: mỗi ĐKCB phải có biểu ghi mẹ; mỗi phiếu mượn đang mở phải có bạn đọc và ĐKCB
   có thật; mỗi bạn đọc phải thuộc một loại bạn đọc có trong danh mục.
5. **Ký biên bản chuyển đổi và đối soát dữ liệu** theo mẫu ở `13-bieu-mau-ban-giao.md`.

### 4.3. Kiểm chứng độc lập bằng công cụ ngoài

Sau khi nhập xong, xuất lại toàn kho ra ISO 2709 và MARCXML rồi cho một thư viện MARC **của bên thứ
ba** đọc (`pymarc`). Đây là bước đã làm trong quá trình phát triển và kết quả ghi ở mục A.1 của
`CLAUDE.md`: 7.675/7.675 biểu ghi hợp lệ, 0 lỗi. Tự kiểm bằng chính bộ mã của mình không chứng minh
được gì — bộ mã sai theo cùng một cách ở cả hai chiều thì phép thử vòng tròn vẫn xanh.

---

## 5. Giai đoạn C — Kiểm thử và vận hành thử

### 5.1. Tám nhóm kiểm thử của mục 5.2

Kịch bản chi tiết ở `06-kich-ban-kiem-thu.md`; bảng dưới đây là ánh xạ sang tài liệu ấy.

| Mã | Nội dung | Kịch bản |
|---|---|---|
| 2.1 | Kiểm tra cài đặt | Nhóm 2.1 |
| 2.2 | Kiểm tra chức năng 11 phân hệ | Các nhóm chức năng theo từng phân hệ |
| 2.3 | Phân quyền và nhật ký | Nhóm 2.3 |
| 2.4 | Trao đổi dữ liệu (ISO 2709, Z39.50, OAI-PMH, API) | Nhóm MARC và nhóm Liên thư viện |
| 2.5 | Chuyển đổi dữ liệu | Nhóm 2.5 |
| 2.6 | Sao lưu và phục hồi | Nhóm 2.6 |
| 2.7 | Ứng dụng di động | Nhóm 2.7 |
| 2.8 | Báo cáo | Nhóm báo cáo của từng phân hệ |

### 5.2. Vận hành thử

Hai tuần vận hành thử với dữ liệu thật, có cán bộ thư viện dùng hằng ngày ở quầy lưu thông và ở khâu
biên mục. Mọi lỗi phát hiện được ghi vào sổ lỗi kèm bước tái hiện; lỗi nghiêm trọng (làm một chức
năng không dùng được) phải sửa xong trước khi nghiệm thu, đúng tiêu chí ở mục 5.3 của E-HSMT.

---

## 6. Giai đoạn D — Nghiệm thu và bàn giao

Điều kiện nghiệm thu theo mục 5.4 của E-HSMT:

- [ ] Các chức năng bắt buộc đáp ứng — đối chiếu `07-bang-dap-ung-ky-thuat.md`.
- [ ] Cài đặt và cấu hình hoàn tất — biên bản chạy thử.
- [ ] Dữ liệu chuyển đổi đã đối soát — biên bản chuyển đổi.
- [ ] Đào tạo hoàn thành — biên bản đào tạo.
- [ ] Kiểm thử sao lưu và phục hồi đạt — kịch bản nhóm 2.6, có ảnh chụp màn hình hoặc nhật ký.
- [ ] Hồ sơ bàn giao đầy đủ theo mục 5.5 — danh mục ở `13-bieu-mau-ban-giao.md`.
- [ ] Lỗi nghiêm trọng đã khắc phục.
- [ ] Hai bên ký biên bản nghiệm thu, bàn giao.

---

## 7. Rủi ro đã lường trước và cách xử lý

| Rủi ro | Dấu hiệu sớm | Cách xử lý |
|---|---|---|
| Dữ liệu nguồn bẩn (ô tác giả chứa công thức bảng tính, nhan đề đặt nhầm ô) | Bước đối soát mẫu ở 4.2 phát hiện | Làm sạch ở tệp nguồn rồi nhập lại; **không** nhận vào rồi sửa sau, vì giá trị bẩn đã kịp thành mục trong hồ sơ thẩm quyền |
| Máy chủ không đủ dung lượng cho tài liệu số | Thẻ dung lượng ở màn hình Sao lưu và báo cáo tài liệu số | Gắn thêm ổ đĩa cho MinIO trước khi số hóa hàng loạt |
| Không có tài khoản SMTP đến sát ngày nghiệm thu | Kiểm ở giai đoạn A | Cấu hình sau, nhưng phải ghi vào biên bản là chức năng thư nhắc chưa kiểm được |
| Cán bộ chưa quen trình soạn MARC | Buổi thực hành ở giai đoạn D | Bổ sung buổi kèm riêng cho cán bộ biên mục; đây là màn hình quyết định chất lượng dữ liệu về sau |
