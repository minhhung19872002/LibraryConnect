# Bảo hành, bảo trì và hỗ trợ vận hành

> Đáp ứng Chương V, mục III.3 và mục III.4 của E-HSMT, và mục "Dịch vụ": *hỗ trợ bảo trì phần mềm và
> hỗ trợ vận hành hệ thống trong vòng 12 tháng*.

---

## 1. Phạm vi và thời hạn

| Hạng mục | Cam kết |
|---|---|
| Thời hạn | **12 tháng** kể từ ngày ký biên bản nghiệm thu, bàn giao đưa vào sử dụng |
| Sửa lỗi phần mềm thuộc phạm vi sản phẩm | **Miễn phí** trong suốt thời hạn |
| Bản vá lỗi và bản vá an toàn thông tin của phiên bản đang dùng | Cung cấp trong suốt thời hạn |
| Chức năng phát triển mới ngoài phạm vi hợp đồng | Theo thỏa thuận riêng |
| Hỗ trợ vận hành | Qua điện thoại, thư điện tử và hệ thống hỗ trợ trực tuyến |

**Lỗi phần mềm** là chỗ sản phẩm chạy sai so với `07-bang-dap-ung-ky-thuat.md` và tài liệu bàn giao.
Không thuộc phạm vi bảo hành: hỏng hóc hạ tầng của Chủ đầu tư (máy chủ, mạng, điện), dữ liệu bị sửa
sai do thao tác của người dùng, và yêu cầu chức năng mới.

---

## 2. Đầu mối hỗ trợ

| Kênh | Thông tin | Giờ tiếp nhận |
|---|---|---|
| Điện thoại | *(điền khi ký hợp đồng)* | 8h00–17h00 các ngày làm việc |
| Thư điện tử | *(điền khi ký hợp đồng)* | 24/7, tính giờ phản hồi theo giờ làm việc |
| Hệ thống hỗ trợ trực tuyến | *(điền khi ký hợp đồng)* | 24/7 |

Chủ đầu tư chỉ định **một đầu mối chính** và một người thay thế; nhà thầu chỉ định một kỹ sư phụ trách
tài khoản và một người thay thế. Danh sách này ghi trong biên bản bàn giao và cập nhật bằng văn bản
khi có thay đổi.

---

## 3. Phân loại sự cố và thời gian phản hồi

| Mức | Định nghĩa | Ví dụ | Phản hồi ban đầu | Mục tiêu xử lý |
|---|---|---|---|---|
| **Nghiêm trọng** | Hệ thống không dùng được, hoặc một nghiệp vụ chính dừng hẳn | Không đăng nhập được; quầy lưu thông không ghi mượn được; trang tra cứu không mở | **Không quá 04 giờ làm việc** kể từ khi nhận thông báo | Khắc phục hoặc có biện pháp thay thế trong 24 giờ làm việc |
| Nặng | Một chức năng không dùng được nhưng có đường vòng | Không in được tem mã vạch trong khi vẫn ghi mượn bằng số ĐKCB | 08 giờ làm việc | 03 ngày làm việc |
| Vừa | Chức năng chạy sai một phần, không chặn công việc | Một báo cáo sai số liệu ở một cột | 16 giờ làm việc | 07 ngày làm việc |
| Nhẹ | Sai sót hiển thị, câu chữ, đề nghị cải tiến nhỏ | Nhãn cột viết tắt khó hiểu | 03 ngày làm việc | Gộp vào bản vá định kỳ |

Mốc "04 giờ làm việc" cho sự cố nghiêm trọng là cam kết bắt buộc của E-HSMT. Giờ làm việc tính
8h00–17h00 các ngày từ thứ Hai đến thứ Sáu, trừ ngày lễ theo quy định.

---

## 4. Thông tin cần có khi báo sự cố

Báo đủ những mục này thì lượt xử lý nhanh hơn nhiều, vì kỹ sư tái hiện được ngay:

1. Màn hình đang mở và thao tác vừa làm (theo thứ tự).
2. Câu thông báo lỗi **nguyên văn**, hoặc ảnh chụp màn hình.
3. Thời điểm xảy ra và tài khoản đang đăng nhập.
4. Sự cố xảy ra một lần hay lặp lại; lặp lại thì theo bước nào.
5. Với sự cố toàn hệ thống: kết quả `curl https://<tên miền>/health/ready` và dòng nhật ký cuối cùng
   (`docker compose logs --tail=100 api`).

Hai thứ **không** gửi qua thư: mật khẩu và tệp sao lưu cơ sở dữ liệu. Cần đến thì hai bên thống nhất
kênh riêng.

---

## 5. Cập nhật và bản vá

### 5.1. Nguyên tắc

*"Việc cập nhật không được làm mất hoặc sai lệch dữ liệu hiện có; trước các thay đổi quan trọng phải
có biện pháp sao lưu/phục hồi và thống nhất với Chủ đầu tư"* — E-HSMT, mục III.3.

### 5.2. Quy trình mỗi lần cập nhật

1. Nhà thầu gửi **thông báo cập nhật** trước ít nhất 03 ngày làm việc: nội dung sửa, phân hệ ảnh
   hưởng, có migration cơ sở dữ liệu hay không, thời gian dừng dịch vụ dự kiến.
2. Chủ đầu tư xác nhận khung giờ; mặc định chọn ngoài giờ phục vụ bạn đọc.
3. **Sao lưu trước khi cập nhật** — bắt buộc, kể cả bản vá nhỏ. Bản sao lưu gồm cơ sở dữ liệu và tệp
   tài liệu số; lượt phục hồi kéo cả hai về.
4. Cập nhật ảnh Docker và chạy migration. Migration của sản phẩm chạy tự động lúc khởi động và có cả
   phần dọn dữ liệu cũ khi cần.
5. Kiểm nhanh sau cập nhật: `/health/ready`, đăng nhập, ghi mượn một cuốn, mở một biểu ghi trên trang
   tra cứu.
6. Có sự cố thì **quay lui**: dựng lại ảnh phiên bản trước và phục hồi bản sao lưu ở bước 3.

### 5.3. Bản vá an toàn thông tin

Lỗ hổng ảnh hưởng trực tiếp tới sản phẩm được vá và thông báo trong thời hạn bảo hành. Mức nghiêm
trọng (cho phép chiếm quyền hoặc lộ dữ liệu cá nhân) được xử lý theo mức "Nghiêm trọng" ở mục 3, không
đợi kỳ vá định kỳ.

---

## 6. Bảo trì định kỳ

| Việc | Tần suất | Ai làm | Căn cứ kiểm |
|---|---|---|---|
| Kiểm bản sao lưu tối qua | Hằng ngày | Chủ đầu tư | Màn hình Sao lưu: dòng gần nhất trạng thái "Thành công" |
| Kiểm dung lượng đĩa | Hằng tuần | Chủ đầu tư | Thẻ dung lượng ở màn hình Sao lưu và báo cáo tài liệu số |
| Thử phục hồi trên môi trường thử | Hằng quý | Hai bên | Biên bản thử phục hồi |
| Rà nhật ký lỗi và việc nền treo | Hằng tháng | Nhà thầu | Hangfire Dashboard, nhật ký Serilog |
| Rà tài khoản và nhóm quyền | Sáu tháng | Chủ đầu tư | Danh sách người dùng, cột lần đăng nhập cuối |
| Báo cáo tình hình vận hành | Hằng quý | Nhà thầu | Số sự cố theo mức, thời gian xử lý, việc đã làm |

Một bản sao lưu chưa từng phục hồi thử thì chưa biết có dùng được không — đó là lý do có dòng "thử
phục hồi hằng quý" ở trên.

---

## 7. Dữ liệu và quyền quản lý dữ liệu

Theo mục III.4 của E-HSMT:

1. **Toàn bộ dữ liệu thuộc quyền quản lý của Chủ đầu tư**, kể cả dữ liệu hình thành trong quá trình
   sử dụng.
2. Nhà thầu **không** sử dụng, sao chép hay cung cấp dữ liệu cho bên thứ ba ngoài phạm vi hợp đồng.
   Khi cần dữ liệu thật để tái hiện lỗi, hai bên thống nhất bằng văn bản và dữ liệu được xoá sau khi
   xử lý xong.
3. **Xuất dữ liệu bất cứ lúc nào**, không cần nhà thầu can thiệp:

| Loại dữ liệu | Cách xuất | Định dạng |
|---|---|---|
| Biểu ghi thư mục | Biên mục → Xuất biểu ghi | ISO 2709, MARCXML |
| Toàn bộ hệ thống | Tài liệu số → Xuất toàn bộ dữ liệu | Gói ZIP: MARCXML + Dublin Core + Excel + tệp số |
| Từng danh mục | Danh mục → Xuất | Excel (nhập lại được), PDF (bản in) |
| Bạn đọc, ĐKCB, giao dịch | Màn hình tương ứng → Xuất | Excel |
| Báo cáo | Từng màn hình báo cáo | Excel, PDF |
| Cơ sở dữ liệu đầy đủ | Sao lưu → Sao lưu ngay → Tải về | `pg_dump` định dạng custom, kèm thư mục tệp số |

4. **Không có biện pháp kỹ thuật nào khoá dữ liệu**: cơ sở dữ liệu là PostgreSQL tiêu chuẩn, tệp số
   nằm trong kho đối tượng tương thích S3, biểu ghi theo chuẩn MARC 21 mở. Kết thúc hợp đồng, Chủ đầu
   tư giữ nguyên khả năng đọc và chuyển dữ liệu sang hệ thống khác.

---

## 8. Bàn giao lại khi kết thúc bảo hành

Trước khi hết 12 tháng, hai bên rà soát:

- [ ] Danh sách lỗi đã báo và tình trạng xử lý từng lỗi.
- [ ] Phiên bản sản phẩm đang chạy và danh sách bản vá đã áp dụng.
- [ ] Bản sao lưu gần nhất đã thử phục hồi thành công.
- [ ] Tài liệu bàn giao đã cập nhật theo các thay đổi trong năm.
- [ ] Thỏa thuận tiếp theo (nếu có) về bảo trì và hỗ trợ.
