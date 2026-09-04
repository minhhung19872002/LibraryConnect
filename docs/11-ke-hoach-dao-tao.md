# Kế hoạch đào tạo, hướng dẫn sử dụng và chuyển giao công nghệ

> Đáp ứng Chương V, mục III.2 và mục "Dịch vụ" của E-HSMT: *"Nhà thầu phải có kế hoạch đào tạo cụ thể
> về thời gian, đối tượng, nội dung, hình thức và tài liệu đào tạo"*, và *"Cài đặt, chuyển giao công
> nghệ, hỗ trợ cán bộ thư viện sử dụng thuần thục phần mềm và các quy trình biên mục tài liệu, số hóa
> tài liệu"*.
>
> Đào tạo **thực hành trực tiếp trên hệ thống đã triển khai**, không giảng trên máy chiếu với dữ liệu
> giả. Mỗi buổi kết thúc bằng một việc thật mà học viên tự làm được từ đầu đến cuối.

---

## 1. Đối tượng và số buổi

| Nhóm | Số người dự kiến | Số buổi | Thời lượng mỗi buổi |
|---|---|---|---|
| A. Quản trị hệ thống | 2–3 | 3 | 3 giờ |
| B. Cán bộ biên mục | 3–5 | 4 | 3 giờ |
| C. Cán bộ bổ sung và kho | 3–5 | 3 | 3 giờ |
| D. Cán bộ lưu thông và bạn đọc | 4–8 | 2 | 3 giờ |
| E. Cán bộ tài liệu số | 2–3 | 2 | 3 giờ |
| F. Cán bộ nội dung và OPAC | 2–3 | 1 | 3 giờ |
| G. Toàn thể — ứng dụng di động | cả nhóm | 1 | 2 giờ |

Tổng **16 buổi**, trải trong ba tuần của giai đoạn D (`10-ke-hoach-trien-khai.md`). Lớp tối đa 8 người
để mỗi người có một máy thực hành.

---

## 2. Nội dung từng buổi

Mười hai nội dung tối thiểu mà E-HSMT nêu đều nằm trong bảng này; cột cuối ghi rõ buổi nào phủ nội
dung nào.

### Nhóm A — Quản trị hệ thống (3 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| A1 | Kiến trúc hệ thống, các dịch vụ Docker, health check, đọc nhật ký Serilog, Hangfire Dashboard | Dựng lại một dịch vụ và xác nhận hệ thống trở lại bình thường | Quản trị hệ thống |
| A2 | Nhóm quyền, tài khoản, phạm vi dữ liệu ba chiều, chính sách mật khẩu, nhật ký hệ thống và cài đặt ghi nhận | Lập một nhóm quyền mới, gán cho một tài khoản, chứng minh tài khoản ấy bị từ chối đúng chỗ | Tài khoản/phân quyền |
| A3 | Tham số hệ thống, sao lưu tự động và thủ công, phục hồi hai bước, kiểm tra bản sao lưu | Tạo bản sao lưu, phục hồi trên môi trường thử, đối chiếu dữ liệu sau phục hồi | Sao lưu/phục hồi |

### Nhóm B — Biên mục (4 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| B1 | Khổ mẫu MARC 21: nhãn trường, chỉ thị, trường con; trình soạn MARC; wizard trường 008; mẫu biên mục | Biên mục trọn vẹn một cuốn sách tiếng Việt từ trang bìa | Biên mục MARC 21 |
| B2 | Danh mục nghiệp vụ, hồ sơ thẩm quyền tác giả, gộp trùng, danh mục tự tạo từ trường MARC | Gộp ba cách viết của một tên tác giả và kiểm rằng biểu ghi đổi theo | Biên mục MARC 21 |
| B3 | Nhập biểu ghi từ ISO 2709, từ Z39.50, từ Excel; xử lý trùng; hàng đợi biên mục | Nhập một tệp ISO 2709 thật, xử lý dòng trùng, phân công việc cho đồng nghiệp | Kết nối và trao đổi dữ liệu |
| B4 | Xử lý phích: thiết kế mẫu, bốn loại phích, in nhiều phích trên một trang A4 | Thiết kế một mẫu phích và in thử | Biên mục MARC 21 |

### Nhóm C — Bổ sung, kho, kiểm kê (3 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| C1 | Yêu cầu đặt mua, quy trình duyệt nhiều cấp, đơn đặt, nhận hàng, biên bản bàn giao | Lập một yêu cầu, duyệt qua hai cấp, tạo đơn và in biên bản bàn giao | Bổ sung/kho/kiểm kê |
| C2 | Biên mục sơ lược, ĐKCB, xếp giá, in mã vạch và nhãn gáy, chuyển kho, thanh lý | Nhập nhanh 5 cuốn, xếp giá và in tem cho chúng | Bổ sung/kho/kiểm kê |
| C3 | Kiểm kê: đóng kho, tạo kỳ, quét liên tục, nhập tệp quét từ máy rời, chốt kỳ và xử lý bản thiếu | Chạy trọn một kỳ kiểm kê nhỏ trên một giá sách | Bổ sung/kho/kiểm kê |

### Nhóm D — Bạn đọc và lưu thông (2 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| D1 | Hồ sơ bạn đọc, nhập từ Excel, ảnh hàng loạt, in thẻ, gia hạn và ra trường hàng loạt | Nhập một lớp sinh viên từ Excel và in thẻ cho cả lớp | Bạn đọc/lưu thông |
| D2 | Chính sách lưu thông, quầy ghi mượn – ghi trả bằng bàn phím và máy quét, đặt giữ, tiền phạt, tủ đồ, bảy báo cáo | Ghi mượn – gia hạn – ghi trả trọn một vòng, thu tiền phạt và in biên lai | Bạn đọc/lưu thông |

### Nhóm E — Tài liệu số (2 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| E1 | Quy trình số hóa: quét, đặt tên tệp, tải lên (kể cả tệp lớn theo mảnh), OCR tiếng Việt, ảnh bìa, checksum | Số hóa và tải lên một tài liệu thật, gắn vào biểu ghi | Tài liệu số |
| E2 | Mức truy cập, chữ chìm, duyệt yêu cầu đọc tài liệu hạn chế, nhật ký truy cập, nhập xuất hàng loạt | Duyệt một yêu cầu đọc và kiểm rằng quyền tự hết hạn đúng ngày | Tài liệu số |

### Nhóm F — Nội dung và OPAC (1 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| F1 | Thông tin trang thư viện, trang tĩnh, menu, banner, tin tức, thư viện ảnh, kiểm duyệt nhận xét; cách bạn đọc tra cứu | Đăng một bản tin có ảnh và hẹn giờ xuất bản | OPAC; quản trị nội dung |

### Nhóm G — Ứng dụng di động (1 buổi)

| Buổi | Nội dung | Việc học viên tự làm cuối buổi | Nội dung bắt buộc phủ |
|---|---|---|---|
| G1 | Cài đặt ứng dụng, đăng nhập bằng số thẻ, tra cứu, quét mã vạch và QR, thẻ điện tử, đặt giữ, gia hạn, tài liệu số, thông báo đẩy, chế độ ngoại tuyến | Mỗi học viên tự cài, đăng nhập và mượn tự phục vụ một cuốn | Mobile Application |

---

## 3. Hình thức

- **Thực hành trực tiếp** trên hệ thống đã triển khai, mỗi học viên một tài khoản riêng đúng nhóm
  quyền của mình. Không dùng tài khoản quản trị cho lớp nghiệp vụ: học viên phải gặp đúng những chỗ
  bị chặn mà họ sẽ gặp khi làm thật.
- **Dữ liệu thật của thư viện**, không phải dữ liệu mẫu. Buổi nào cũng bắt đầu từ một việc đang tồn
  đọng của chính đơn vị.
- Giảng viên là người đã cài đặt hệ thống cho đơn vị, không phải người đọc lại tài liệu.
- Mỗi buổi có **30 phút cuối** dành cho câu hỏi từ chính công việc của học viên.

---

## 4. Tài liệu đào tạo

| Tài liệu | Dùng cho nhóm | Nguồn |
|---|---|---|
| Hướng dẫn sử dụng theo phân hệ và theo vai trò | B, C, D, E, F | `01-huong-dan-su-dung.md` |
| Tài liệu quản trị hệ thống | A | `02-tai-lieu-quan-tri.md` |
| Quy trình sao lưu và phục hồi | A | `03-sao-luu-phuc-hoi.md` |
| Cài đặt và cấu hình | A | `04-cai-dat-cau-hinh.md` |
| Mô tả API và giao thức kết nối | A, B | `05-api-reference.md` |
| Kịch bản kiểm thử (dùng làm bài thực hành) | tất cả | `06-kich-ban-kiem-thu.md` |
| Slide tóm tắt từng buổi | tất cả | Nhà thầu chuẩn bị, nộp trước buổi học 3 ngày |

Toàn bộ tài liệu **bằng tiếng Việt**, giao bản điện tử và bản in theo số lượng thống nhất trong hợp
đồng.

---

## 5. Chuyển giao công nghệ

Ngoài đào tạo sử dụng, nhà thầu chuyển giao cho bộ phận kỹ thuật của Chủ đầu tư:

1. **Quy trình biên mục** đang áp dụng: mức biên mục, trường bắt buộc, quy tắc đặt ký hiệu xếp giá,
   quy tắc sinh số ĐKCB và số thẻ — tất cả đều là tham số cấu hình được, không phải mã nguồn.
2. **Quy trình số hóa**: độ phân giải quét, định dạng lưu, đặt tên tệp, ngưỡng dung lượng chuyển sang
   tải theo mảnh, cách kiểm tra kết quả OCR tiếng Việt.
3. **Vận hành hằng ngày**: đọc health check, đọc nhật ký, xử lý việc nền bị treo trong Hangfire, kiểm
   tra bản sao lưu tối qua có thành công không.
4. **Xử lý sự cố thường gặp** — mục "Xử lý sự cố" của `02-tai-lieu-quan-tri.md`.

---

## 6. Đánh giá kết quả đào tạo

Cuối mỗi nhóm buổi, học viên làm một **bài thực hành nghiệm thu** lấy thẳng từ kịch bản kiểm thử của
phân hệ tương ứng. Học viên tự làm xong không cần trợ giúp thì tính là đạt. Kết quả ghi vào **biên bản
đào tạo** (mẫu ở `13-bieu-mau-ban-giao.md`), có chữ ký của học viên và của giảng viên.

Nhóm nào có trên một phần ba học viên chưa đạt thì nhà thầu bố trí thêm một buổi kèm, không tính thêm
chi phí.
