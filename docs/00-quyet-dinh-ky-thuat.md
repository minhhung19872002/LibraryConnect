# Sổ quyết định kỹ thuật — LibraryConnect

Tài liệu ghi lại những quyết định phải tự chốt trong quá trình xây dựng, khi đặc tả
`PROMPT-BUILD-LIBRARYCONNECT.md` không nói rõ. Thứ tự ưu tiên khi chốt:

1. Theo đúng quy ước đã dùng ở các Phase trước (đọc code cũ mà theo).
2. Theo nghiệp vụ thư viện Việt Nam thông dụng (TT 18/2014/TT-BVHTTDL, quy chế thư viện đại học).
3. Chọn phương án đơn giản nhất mà chạy được thật, và làm cho nó **cấu hình được** qua
   `sys.system_parameters` để sau này đổi không phải sửa code.

Cột "Đổi được không" cho biết khách hàng có tự đổi được trên giao diện hay không, và đổi ở đâu.

---

## Phase 9 — Phân hệ VII: Lưu thông

| Phase | Vấn đề | Phương án đã chọn | Lý do | Đổi được không |
|---|---|---|---|---|
| 9 | Đặc tả mục 4.8 có bảng `circulation_templates` riêng, trong khi Phase 6 đã có trình thiết kế biểu mẫu dùng chung (`acq.form_templates`) | Dùng lại trình thiết kế của Phase 6, thêm bốn loại biểu mẫu: Phiếu mượn, Phiếu trả, Biên lai phạt, Giấy xác nhận trả sách | Hai trình thiết kế song song là hai chỗ phải bảo trì và hai giao diện cán bộ phải học; loại biểu mẫu vốn đã là một trường chuỗi nên thêm loại không phải đổi cấu trúc | Có — Bổ sung → Mẫu biểu mẫu, chọn loại biểu mẫu tương ứng |
| 9 | Ngưỡng nợ phí bao nhiêu thì chặn mượn tiếp | Chặn khi tổng nợ chưa thanh toán vượt ngưỡng khai trong tham số; mặc định 50.000 đ | Đặc tả chỉ nói "cảnh báo nợ phí" mà không nói ngưỡng; các thư viện đại học Việt Nam thường chặn khi nợ vượt một mức nhỏ chứ không chặn ngay từ đồng đầu tiên | Có — `CIRCULATION.DEBT_BLOCK_THRESHOLD` |
| 9 | Có cho mượn khi thẻ sắp hết hạn trong thời hạn mượn không | Cho mượn, nhưng hạn trả bị cắt về đúng ngày hết hạn thẻ nếu hạn trả vượt quá | Trả sách sau khi thẻ hết hạn là tình huống thư viện luôn phải xử lý thủ công; cắt hạn trả là cách phổ biến và không cần cán bộ can thiệp | Có — `CIRCULATION.CLAMP_DUE_TO_CARD` |
| 9 | Ngày nghỉ hằng tuần | Cấu hình danh sách thứ nghỉ trong tham số, mặc định Chủ nhật | Đặc tả chỉ nói tới lịch nghỉ lễ; thư viện đại học Việt Nam thường nghỉ Chủ nhật, một số nơi làm cả tuần | Có — `CIRCULATION.WEEKLY_CLOSED_DAYS` |
| 9 | Tiền phạt tính theo ngày nào khi có ngày nghỉ | Trừ hết ngày nghỉ lễ và ngày nghỉ hằng tuần ra khỏi số ngày quá hạn | Đặc tả VII.1 nói rõ "không tính phạt ngày nghỉ" | Có — `CIRCULATION.SKIP_HOLIDAY` |
| 9 | Khi trả sách mà có người đặt giữ thì bản đó đi đâu | Chuyển sang trạng thái "Đặt giữ" và giữ ở quầy cho người đầu hàng đợi trong số ngày của chính sách, hết hạn thì tự trả về kho và chuyển cho người kế tiếp | Cách làm chuẩn của mọi ILS; đặc tả có trạng thái ĐẶT GIỮ trong danh sách trạng thái ấn phẩm | Có — số ngày giữ nằm trong chính sách lưu thông |
| 9 | Mượn tự phục vụ (mục XI.2) xác thực vị trí thế nào khi chưa có app | Nhận mã điểm quét đặt tại kho (mã QR dán ở kho) và đối chiếu với mã khai trong tham số; bỏ trống tham số thì không bắt buộc | Đặc tả nêu hai cách là Wi-Fi SSID hoặc quét QR tại kho; QR kiểm chứng được ngay trên web, SSID thì không | Có — `CIRCULATION.SELF_CHECKOUT_TOKENS` |
| 9 | Nhóm `/api/reader/*` phần mượn trả làm ở phase nào | Làm ngay ở Phase 9 cùng nghiệp vụ lưu thông (đăng nhập bạn đọc, sách đang mượn, lịch sử, gia hạn, đặt giữ, tiền phạt, mượn tự phục vụ) | Mục XI.4 bắt buộc hoàn thành nhóm này trong đợt web; logic nằm ở đây nên làm cùng lúc rẻ hơn và có thể kiểm chứng ngay | — |
| 9 | Mật khẩu đăng nhập của bạn đọc khi thư viện chưa cấp | Bạn đọc chưa có mật khẩu thì không đăng nhập được; cán bộ đặt lại mật khẩu ở hồ sơ, hoặc bật tùy chọn đặt mật khẩu ban đầu bằng ngày sinh khi nhập từ Excel | An toàn hơn việc mặc định mật khẩu bằng số thẻ — thứ ai nhìn thẻ cũng thấy | Có — tùy chọn khi nhập Excel (Phase 8) |
| 9 | Ngưỡng nợ phí chặn mượn tiếp | Chặn khi nợ vượt ngưỡng khai trong tham số, mặc định 50.000 đ, chứ không chặn ngay khi vừa có nợ | Đặc tả chỉ nói "cảnh báo nợ phí" mà không nói chặn hay không; chặn ngay từ đồng đầu tiên làm quầy tắc mỗi khi có ai đó phạt vài nghìn | Có — `CIRCULATION.DEBT_BLOCK_THRESHOLD`, đặt 0 là chặn mọi khoản nợ |
| 9 | Hạn trả vượt quá hạn thẻ | Cắt hạn trả về đúng ngày hết hạn thẻ | Cho mượn quá hạn thẻ thì tới lúc đòi sách không còn ràng buộc nào; thực tế thư viện đại học đều cắt như vậy | Có — `CIRCULATION.CLAMP_DUE_TO_CARD`, tắt thì giữ nguyên hạn theo chính sách |

