# Sổ lỗi — rà soát chất lượng toàn diện

Đợt rà soát này **không đọc mã nguồn để tìm lỗi** và **không chạy lại bộ kiểm thử cũ**. Cách làm:
mở hệ thống đang chạy như một người dùng thật, đi hết từng màn hình, cố tình đi đường sai, và gọi
thẳng API không qua giao diện. Mã nguồn chỉ được mở ra sau khi đã thấy triệu chứng, để nói chính
xác lỗi nằm ở đâu.

Mức độ: **Nghiêm trọng** (sai nghiệp vụ / mất dữ liệu / lỗ hổng) · **Nặng** (chức năng không dùng
được) · **Vừa** (dùng được nhưng sai hoặc khó) · **Nhẹ** (thẩm mỹ).

Loại: Nghiệp vụ · Giao diện · Dữ liệu · Bảo mật · Hiệu năng · Ngôn ngữ.

Cột **Trạng thái**: `Mới` (chưa sửa) · `Đã sửa` (kèm chỗ đã sửa).

---

## A. Liên thư viện — phát hiện khi nạp dữ liệu thật từ nguồn ngoài

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| A1 | Liên thư viện → Kho OAI-PMH | Hai tác giả có tên khác nhau nhưng sinh ra cùng một mã (bỏ dấu rồi viết hoa) làm vi phạm ràng buộc duy nhất `ux_author_code`; **toàn bộ lượt thu hoạch đổ, không biểu ghi nào được lưu**. Cùng lỗi này sẽ xảy ra khi nhập ISO 2709 hoặc Excel có hai biểu ghi cùng tác giả viết khác dấu. | Thu hoạch bộ `com_DHTL_13003` của `tailieuso.tlu.edu.vn` (2.353 biểu ghi): 0 biểu ghi vào hệ thống, nhật ký API ghi `duplicate key value violates unique constraint "ux_author_code" Key (code)=(PHAM_VIET_HOA)`. Trong kho đã có "Phạm Việt Hòa". | Nghiêm trọng | Dữ liệu | Đã sửa — `BibAuthorityLinker` |
| A2 | Liên thư viện → Kho OAI-PMH | Thu hoạch chạy đồng bộ ngay trong lượt HTTP. Máy khách ngắt kết nối (đóng tab, hết hạn chờ của proxy 300 giây) là lượt thu hoạch bị bỏ dở giữa chừng, và **dòng nhật ký kẹt ở trạng thái "Đang chạy" vĩnh viễn** — không ai biết kho ấy đã lấy xong hay chưa. Hệ thống đã có Hangfire nhưng không dùng ở đây. | Bấm "Thu hoạch ngay" cho một kho lớn rồi đóng tab. Xem Nhật ký thu hoạch: dòng đó đứng mãi ở "Đang chạy", số biểu ghi 0. | Nặng | Nghiệp vụ | Đã sửa — thu hoạch chạy nền qua Hangfire, nhật ký chốt lại; lượt kẹt quá 6 giờ tự đóng |
| A3 | Liên thư viện → Kho OAI-PMH | Chỉ nhận đúng hai tên định dạng `oai_dc` và `marc21`. Kho thật khai tên khác: VJOL khai `marcxml` và `oai_marc`, DSpace khai `marc`. Hậu quả: không lấy được biểu ghi MARC đầy đủ từ chính những kho có sẵn MARC, phải hạ xuống Dublin Core nghèo hơn nhiều. | Thêm kho `https://vjol.info.vn/index.php/index/oai` với định dạng `marcxml` → bị chặn ở bước kiểm tra dữ liệu. | Vừa | Nghiệp vụ | Đã sửa — nhận mọi tên định dạng đúng cú pháp của chuẩn |
| A4 | Liên thư viện → Tra cứu | Một số truy vấn Z39.50 báo có kết quả nhưng lấy về 0 biểu ghi (máy chủ từ chối bước Present). Cùng truy vấn ấy qua SRU vẫn lấy được. Hệ thống không tự chuyển sang lối SRU của cùng thư viện. | Tra "Nhan đề = Vietnam" ở Thư viện Quốc hội Mỹ (Z39.50): 11.528 kết quả, 0 biểu ghi. Cùng truy vấn ở lối SRU: 5 biểu ghi. | Vừa | Nghiệp vụ | Đã sửa — tự chuyển sang lối SRU của cùng thư viện, kèm câu nói rõ đã lấy qua lối nào |
| A5 | Liên thư viện → Kho OAI-PMH | Tên kho hiện ra còn nguyên ký tự thoát chưa giải mã: `Th&#432; Vi&#7879;n S&#7889; &#272;&#7841;i H&#7885;c Th&#7911;y L&#7907;i` thay vì "Thư Viện Số Đại Học Thủy Lợi". | Bấm "Kiểm tra kết nối" với `https://tailieuso.tlu.edu.vn/oai/request`. | Nhẹ | Ngôn ngữ | Đã sửa — giải mã ký tự thoát HTML khi đọc Identify |
| A6 | Liên thư viện → Kho OAI-PMH | Lỗi TLS ở giữa chừng làm hỏng cả lượt thu hoạch và **mất luôn phần đã lấy được**; thông báo cho cán bộ chỉ nói chung chung, không nêu nguyên nhân thật (máy chủ nguồn thiếu chứng thư trung gian). Thu hoạch cũng không có điểm nối lại — `resumptionToken` đã đi tới đâu không được lưu, nên chạy lại là lấy lại từ đầu. | Thu hoạch `dspace.ctu.edu.vn` từ trong container: hỏng giữa chừng, phải chạy lại từ đầu. | Vừa | Nghiệp vụ | Đã sửa — ghi thẻ đọc tiếp sau mỗi trang để chạy tiếp từ chỗ dừng; lỗi chứng thư được nói đúng nguyên nhân |
| A7 | Liên thư viện → Kho OAI-PMH **và** Biên mục → Hàng đợi | Biểu ghi thu hoạch về được đặt trạng thái "Chờ biên mục" nhưng **không có dòng nào được tạo trong hàng đợi biên mục**. Màn hình Hàng đợi biên mục vẫn hiện 0 ở cả năm cột trong khi kho đã có hơn 3.200 biểu ghi đang chờ. Không cán bộ nào biết có việc phải làm; số biểu ghi ấy nằm chết trong hệ thống — không lên OPAC, không vào hàng đợi. | Thu hoạch một kho OAI bất kỳ → mở Biên mục → Hàng đợi biên mục: Chờ xử lý 0, Đang biên mục 0, Chờ duyệt 0. `GET /api/cataloging/queue` trả `totalCount = 0`. | Nghiêm trọng | Nghiệp vụ | Đã sửa — thu hoạch tạo dòng việc trong hàng đợi biên mục |
| A8 | Biên mục → Hàng đợi biên mục | **Hoàn thành một việc trong hàng đợi không đưa biểu ghi lên OPAC.** Đi hết luồng Chờ xử lý → Đang biên mục → Chờ duyệt → Đã hoàn thành, biểu ghi vẫn ở trạng thái "Chờ biên mục" và bạn đọc vẫn không tra ra. Trong toàn hệ thống không có chỗ nào chuyển biểu ghi sang "Đã xuất bản" ngoài việc mở trình soạn MARC lưu lại — nghĩa là cả luồng biên mục sơ lược → hàng đợi → duyệt là một ngõ cụt. | Tạo việc bằng `POST /api/cataloging/queue`, đổi trạng thái lần lượt tới `Completed`, rồi xem lại biểu ghi: vẫn `Queued`. Số biểu ghi OPAC tra được không đổi (206 trước và sau). | Nghiêm trọng | Nghiệp vụ | Đã sửa — trạng thái việc kéo theo trạng thái biểu ghi |
| A9 | Liên thư viện → Kho OAI-PMH | Biểu ghi thu hoạch về **thiếu trường điều khiển 008** — tức là không hợp lệ theo chính quy tắc kiểm tra của hệ thống. Cán bộ mở biểu ghi vừa thu hoạch trong trình soạn MARC rồi bấm Lưu thì bị chặn: *"Thiếu trường bắt buộc 008 — Yếu tố dữ liệu có độ dài cố định."* Muốn hiệu đính thì phải tự gõ đủ 40 ký tự trường 008 cho từng biểu ghi. Kiểm 30/30 biểu ghi lấy về đều thiếu. | Mở một biểu ghi nguồn `Oai` bất kỳ trong Biên mục → bấm Lưu. | Nặng | Dữ liệu | Đã sửa — dựng trường 008 khi thu hoạch |

---

## B. Trang tra cứu (OPAC)

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| B1 | Chi tiết tài liệu → thẻ "Biểu ghi MARC" | Đổ JSON thô ra cho bạn đọc xem. Đây là trang công khai, phải hiện bảng MARC chuẩn (Tag · Chỉ thị 1 · Chỉ thị 2 · Trường con) như MARC view của Koha/Voyager. | Mở một tài liệu bất kỳ → thẻ "Biểu ghi MARC". | Nặng | Giao diện | Đã sửa — bảng MARC 21 có tên trường tiếng Việt |
| B2 | Trang chủ → "Sách mới bổ sung" | Năm ô đầu là báo và tạp chí không có bản in nào, tác giả hiện dấu "—", nhãn "Chưa có bản in trong kho". Bạn đọc vào trang chủ thấy ngay năm cuốn không mượn được. | Mở trang chủ sau khi nạp dữ liệu ấn phẩm định kỳ. | Vừa | Nghiệp vụ | Đã sửa — trang chủ chỉ nêu tài liệu có bản in hoặc bản số |
| B3 | Trang chủ → dải số liệu | Ghi "6 tài liệu số" nhưng khách chưa đăng nhập vào mục Tài liệu số chỉ thấy 4 — con số đếm cả tài liệu nội bộ và hạn chế mà người xem không mở được. | So dải số liệu trang chủ với danh sách ở `/tai-lieu-so` khi chưa đăng nhập. | Nhẹ | Dữ liệu | Đã sửa — đếm cùng luật với danh sách khách vãng lai xem được |
| B4 | Mọi màn hình có ảnh bìa | Tài liệu không có ảnh bìa hiện một ô xám trống kèm dòng "Chưa có ảnh bìa". Trang kết quả tra cứu thành một dãy ô xám, nhìn như trang hỏng. Phần mềm thư viện thường sinh ảnh bìa thay thế có nhan đề và tác giả. | Tra cứu bất kỳ trên OPAC. | Vừa | Giao diện | Đã sửa — sinh bìa thay thế mang nhan đề và tác giả |
| B5 | Tra cứu → không có kết quả | Trạng thái rỗng chỉ nói "Không tìm thấy tài liệu nào phù hợp", không gợi ý gì tiếp theo (bỏ bớt từ khoá, kiểm tra chính tả, chuyển sang tìm nâng cao, tìm ở thư viện khác). | Tra một từ khoá không có, ví dụ `zzzz`. | Nhẹ | Giao diện | Đã sửa — trạng thái rỗng có bốn gợi ý làm tiếp |
| B6 | Duyệt theo bộ sưu tập | Bảng `cat.collections` chưa bao giờ được gieo dữ liệu, nên mục "Bộ sưu tập" luôn rỗng trong khi trang chủ vẫn dẫn vào. Bạn đọc bấm vào một lối cụt. | Trang chủ → Duyệt theo bộ sưu tập. | Vừa | Dữ liệu | Đã sửa — gieo sẵn 10 bộ sưu tập trong bộ dữ liệu nền |
| B7 | Tài khoản bạn đọc → Đang mượn / Lịch sử | Cột nhan đề hiện dấu "—" thay vì tên sách: dữ liệu mượn trả trong bộ gieo mẫu không điền nhan đề, và giao diện không có phương án dự phòng (không tra ngược sang biểu ghi). Bạn đọc nhìn danh sách sách đang mượn mà không biết mình đang mượn cuốn gì. Màn hình quầy lưu thông cũng vậy. | Đăng nhập bằng một thẻ có sách đang mượn của bộ dữ liệu mẫu. | Nặng | Dữ liệu | Đã sửa — lấy nhan đề từ biểu ghi khi cột chép sẵn trống; bộ gieo dữ liệu cũng chép nhan đề |
| B8 | Duyệt theo tác giả | **Chỉ hiện đúng một tác giả** dù hồ sơ thẩm quyền có 9.361 tên. Nguyên nhân: hệ thống lấy 500 tác giả đầu bảng chữ cái rồi mới bỏ những người chưa có tài liệu xuất bản — kho càng lớn thì trang duyệt càng rỗng. Chọn chữ cái N cũng chỉ ra đúng một người. Đây là lỗi chung cho mọi thư viện có hơn 500 tên tác giả, không riêng bộ dữ liệu này. | OPAC → Duyệt theo tác giả → Tất cả: chỉ một thẻ "Bùi Thị Lan". `GET /api/browse/authors` trả `totalCount = 1`; `GET /api/catalogs/authors/items` trả 9.361. | Nặng | Nghiệp vụ | Đã sửa — lọc tác giả có tài liệu trước rồi mới cắt danh sách |
| B9 | Báo – Tạp chí | Cột "Nhà xuất bản" trống ở toàn bộ các dòng. | OPAC → Báo – Tạp chí. | Vừa | Dữ liệu | Đã sửa — bộ dữ liệu mẫu điền cơ quan xuất bản cho từng đầu báo |
| B10 | Báo – Tạp chí | Bảng rộng 1.260 px nằm trong khung 1.126 px nên cột cuối "Số mới nhất" bị che một nửa (`119 (3…`). Cuộn ngang được **bên trong bảng** nhưng không có dấu hiệu nào cho biết, người xem tưởng dữ liệu bị cụt. | OPAC → Báo – Tạp chí trên màn hình rộng 1440 px. | Nhẹ | Giao diện | Đã sửa — dòng nhắc cuộn ngang, dải mờ ở mép phải, cột tên báo neo lại |
| B11 | Tài liệu số | Chỉ có ô tìm theo nhan đề. Đặc tả IX.4 đòi bộ lọc riêng cho tài liệu số (bộ sưu tập, định dạng, mức truy cập) — không có. | OPAC → Tài liệu số. | Vừa | Nghiệp vụ | Đã sửa — thêm lọc theo bộ sưu tập, định dạng, mức truy cập và tìm trong nội dung |
| B12 | Toàn bộ trang tra cứu | Ngày trên trang công khai vẫn hiện dạng `5/9/2029` — **chính lỗi D8 chưa sửa hết**. Lần trước chỉ sửa giao diện quản trị và phép thử quét mã nguồn cũng chỉ quét thư mục ấy, nên dòng "cả sản phẩm dùng chung một cách viết ngày" ở D8 là nói quá. Bạn đọc và cán bộ nhìn thấy hai cách viết ngày khác nhau; với ngày từ 12 trở xuống thì không ai biết đâu là ngày đâu là tháng. | OPAC → Tài khoản của tôi → Đang mượn, cột hạn trả. | Vừa | Giao diện | Đã sửa — chép `lib/datetime` sang trang tra cứu và quét mã nguồn ở cả hai thư mục |

---

## C. Giao diện quản trị

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| C1 | Menu bên trái | Nhãn "Báo cáo thống kê" bị cắt thành "Báo cáo thống kê …", đẩy mũi tên bung menu con ra ngoài khung; người dùng tưởng mục đó hỏng. | Mở giao diện quản trị, nhìn mục cuối của menu. | Vừa | Giao diện | Đã sửa phần gốc — mục "Báo cáo thống kê" nay là màn hình có thật, không còn dấu "chưa làm". **Nhưng lỗi cắt chữ vẫn còn ở menu con, xem C7.** |
| C2 | Báo cáo thống kê → Xuất Excel / Xuất PDF · Tài liệu môn học → Báo cáo → xuất · Tài liệu môn học → Gán tài liệu → "Tải tệp mẫu" | Ba nút này mở thẻ trình duyệt mới trỏ thẳng vào API. Thẻ mới **không mang theo mã đăng nhập** (hệ thống dùng JWT trong tiêu đề, không dùng cookie), nên cán bộ nhận về một trang trắng in dòng JSON `{"success":false,"message":"Phiên đăng nhập không hợp lệ hoặc đã hết hạn."}`. Ba chức năng xuất/tải này **không dùng được**, và người dùng bị dẫn tới nghĩ mình đã hết phiên đăng nhập. | Đăng nhập, mở Báo cáo thống kê, bấm "Xuất Excel". | Nặng | Nghiệp vụ | Đã sửa — tải tệp qua lớp gọi API có mã đăng nhập, kèm phép thử quét mã nguồn chặn tái diễn |
| C3 | Quản trị hệ thống → Nhật ký hệ thống | Bảng hiện thẳng mã định danh máy `1b4c4855-804f-400d-a3f3-f493908256bf` cho cán bộ đọc. Nhật ký cần cho biết *đối tượng nào* (tên biểu ghi, tên bạn đọc), không phải chuỗi 36 ký tự. | Quản trị hệ thống → Nhật ký hệ thống. | Vừa | Giao diện | Đã sửa — nói bằng tiếng Việt, mã định danh chuyển sang phần chi tiết |
| C4 | Toàn bộ API | Khi thiếu một tham số biểu mẫu, hệ thống trả thông báo **tiếng Anh** của khung nền: `"The options field is required."`. Cả sản phẩm phải tiếng Việt. | `POST /api/cataloging/import` chỉ đính tệp, không gửi `options`. | Vừa | Ngôn ngữ | Đã sửa — thay toàn bộ thông báo của khung nền sang tiếng Việt |
| C5 | Bổ sung → Ấn phẩm, thẻ "Bản in trong kho" | Cột "Giá" trống hoàn toàn ở mọi dòng. Nhãn cũng tối nghĩa: "Giá" ở đây là giá sách (kệ) hay giá tiền? Cán bộ thư viện đọc hai nghĩa khác nhau. | Bổ sung → Ấn phẩm → mở một biểu ghi → thẻ "Bản in trong kho". | Vừa | Giao diện | Đã sửa — nhãn đổi thành "Vị trí giá" ở cả tám chỗ, kèm phép thử quét mã nguồn |
| C6 | Biên mục → Biểu ghi thư mục | Cột **Nhan đề** chỉ rộng 66 px trong khi "Xuất bản" được 201 px và "Số kiểm soát" 135 px. Nhan đề tiếng Việt dài bị bẻ thành thang chữ dọc mỗi dòng một từ, dòng bảng cao 171–259 px, một màn hình chỉ xem được vài biểu ghi. Màn hình danh sách chính của cả phân hệ Biên mục gần như không dùng để duyệt được. Lỗi chỉ lộ ra khi có dữ liệu thật — bộ dữ liệu mẫu nhan đề ngắn nên trước đây nhìn vẫn ổn. | Biên mục → Biểu ghi thư mục sau khi thu hoạch dữ liệu thật. | Nặng | Giao diện | Đã sửa — nhan đề là cột rộng nhất, bảng cuộn ngang thay vì bóp cột |
| C7 | Menu bên trái — menu con | Nhãn menu con dài bị cắt: "Nhập xuất dữ liệu bạn đọc" (cần 164 px, chỉ có 141 px), "Thông tin trang thư viện", "Định nghĩa trường MARC", "Công cụ biểu ghi MARC", "Gán tài liệu cho môn học", "Báo cáo tài liệu môn học", "Báo cáo ấn phẩm định kỳ". Đúng lỗi đã báo ở C1 nhưng ở tầng menu con — sửa C1 chưa động tới chỗ này. | Mở giao diện quản trị, bung menu Bạn đọc. | Vừa | Giao diện | Đã sửa — nới bề rộng menu con |

---

## D. Nghiệp vụ — phát hiện khi cố tình đi đường sai

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| D1 | Lưu thông → Quầy ghi mượn | **Hai người mượn được cùng một cuốn sách.** Hai lượt `POST /api/circulation/desk/checkout` gửi đồng thời cho cùng một mã vạch đều trả 200 và sinh hai phiếu mượn đang mở. Không có khoá hàng ở mức cơ sở dữ liệu, chỉ có phép đọc-rồi-ghi. Hai quầy làm việc song song (hoặc bạn đọc tự mượn bằng điện thoại đúng lúc cán bộ ghi mượn) là sổ sách sai ngay. | Gửi hai lượt ghi mượn song song cho mã vạch `LC00000006`: sinh phiếu `PM00000102` và `PM00000103` cho hai bạn đọc khác nhau, cùng một bản in. | Nghiêm trọng | Nghiệp vụ | Đã sửa — ràng buộc duy nhất ở cơ sở dữ liệu, kèm câu báo cho cán bộ ở quầy |
| D2 | Biên mục → Trình soạn MARC | Nhận năm xuất bản **3025** không một lời cảnh báo, lưu thẳng vào kho và hiện lên OPAC. | Sửa `260$c` thành 3025 rồi lưu. | Vừa | Dữ liệu | Đã sửa — nhắc khi năm nằm ngoài khoảng hợp lý |
| D3 | Bạn đọc → Hồ sơ | Nhận ngày sinh **năm 2099** — bạn đọc chưa ra đời. Không chặn ngày trong tương lai. | Thêm bạn đọc, đặt ngày sinh 01/01/2099, lưu. | Vừa | Dữ liệu | Đã sửa — chặn ngày sinh tương lai và trước năm 1900 |
| D4 | Bạn đọc → Nhập từ Excel | Đưa một tệp không phải Excel vào thì trả **HTTP 500 "Đã xảy ra lỗi hệ thống"** thay vì một câu tiếng Việt nói rõ tệp sai định dạng. Lỗi hệ thống làm người dùng tưởng phần mềm hỏng. | Nhập một tệp `.txt` đổi đuôi thành `.xlsx`. | Vừa | Nghiệp vụ | Đã sửa — báo rõ tệp sai định dạng, hướng dẫn lưu lại thành .xlsx |
| D5 | Liên thư viện → Kho OAI-PMH | Không có khoá chống chạy trùng: bấm "Thu hoạch ngay" hai lần (hoặc bấm lại sau khi proxy hết giờ chờ ở A2) thì hai — ba lượt thu hoạch cùng chạy trên một kho, cùng ghi vào kho dữ liệu, cùng đếm số riêng. Nhật ký thu hoạch hiện ba dòng "Đang chạy" của cùng một kho. | Bấm "Thu hoạch ngay" hai lần liên tiếp cho một kho lớn. | Nặng | Nghiệp vụ | Đã sửa — khoá chống chạy trùng trên từng kho |
| D6 | Bộ dữ liệu mẫu | 51 bạn đọc nhưng chỉ có khoảng 17 tên khác nhau: "Bùi Hoàng Khánh" ba người, "Đặng Hoàng Hùng" ba người, "Hoàng Thị Giang" ba người… Cùng một người vừa là Sinh viên vừa là Cán bộ. Đem đi trình diễn thì trông như dữ liệu rác. | Bạn đọc → Hồ sơ bạn đọc. | Nhẹ | Dữ liệu | Đã sửa — `DemoNames` ghép ba danh sách 20 × 21 × 29 tên |
| D7 | Bộ dữ liệu mẫu | Không có bản tin nào, không có bộ sưu tập nào. Trang chủ OPAC hiện "Chưa có bản tin nào được đăng", mục Bộ sưu tập rỗng — Phân hệ Quản trị nội dung không demo được nếu không tự nhập tay trước. | Mở trang chủ OPAC. | Vừa | Dữ liệu | Đã sửa — bộ dữ liệu mẫu có 6 bản tin và 10 bộ sưu tập |
| D8 | Bạn đọc → Hồ sơ | Ngày hiện dạng `5/9/2029` (không có số 0 ở đầu) trong khi các màn hình khác dùng `15/09/2026`. Với ngày ≤ 12 thì người đọc không biết là ngày 5 tháng 9 hay tháng 5 ngày 9. | Bạn đọc → Hồ sơ bạn đọc, cột Hạn thẻ. | Nhẹ | Giao diện | Đã sửa — cả sản phẩm dùng chung một cách viết ngày dd/MM/yyyy |
| D9 | Toàn hệ thống | Tham số tên thư viện chưa được đặt, nên OPAC và giao diện quản trị đều hiện đúng chữ "Thư viện" ở chỗ đáng lẽ là tên khách hàng. Đúng thiết kế (không hardcode) nhưng bộ gieo dữ liệu phải đặt sẵn một tên mẫu, nếu không bản demo trông như chưa cài xong. | Mở trang chủ OPAC. | Nhẹ | Dữ liệu | Đã sửa — bộ dữ liệu mẫu đặt tên "Thư viện Trường Đại học Mẫu" |

---

## E. Đợt rà thứ hai — trên kho dữ liệu trình diễn lớn

Rà lại sau khi nạp bộ dữ liệu trình diễn lớn: **7.675 biểu ghi, 9.502 ĐKCB, 351 bạn đọc, 1.603 lượt
mượn, 254 khoản phạt**. Cách rà giữ nguyên như đợt đầu — mở trình duyệt thật ở khổ 1440×900, đăng
nhập thật, đi hết **72 màn hình** (19 OPAC + 53 quản trị), chụp lại tất cả, rồi đo bằng máy các dấu
hiệu chữ bị cắt, bảng tràn khung, ảnh hỏng, mã định danh máy lọt ra, chữ tiếng Anh lọt ra, ô bảng
trống và lỗi JavaScript.

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| E1 | Danh mục → Tác giả (và mọi danh mục dùng chung bảng ấy) | Hai cột **Tên** và **Tên tiếng Anh** rộng đúng **0 px**. Mười một ô tiêu đề bị bóp cao 91 px và chữ chồng lên nhau: hàng tiêu đề đọc thành "T / Đọ và tên đầy đủ / n" và "Th / Thao tác". Bảng chung khai 5 cột, riêng danh mục tác giả khai thêm 6 cột nữa, tổng 11 cột trong 1.290 px mà hai cột không khai bề rộng — đúng lỗi C6 nhưng ở màn hình khác | Danh mục → Tác giả, khổ 1440×900 | Nặng | Giao diện | Đã sửa — mọi cột khai bề rộng, bảng cuộn ngang thay vì bóp cột; kèm phép thử chặn kiểu viết cũ |
| E2 | 18 bảng của giao diện quản trị | Bảng rộng hơn khung chứa nên cột cuối — thường là **Thao tác**, chỗ đặt nút Sửa và Xoá — nằm hẳn ngoài màn hình, mà không có dấu hiệu nào cho biết cuộn ngang được. Đo được: Tiền phạt 1.500 px trong 1.136 px, Đặt giữ 1.500/1.136, Kho OAI-PMH 1.500/1.136, Yêu cầu đọc hạn chế 1.700/1.136, Biểu ghi thư mục 1.484/1.136, Tin tức 1.480/1.102… Đây chính là lỗi B10 đã sửa cho một bảng của OPAC nhưng chưa đụng tới giao diện quản trị | Lưu thông → Tiền phạt: cột "Thao tác" không thấy đâu | Nặng | Giao diện | Đã sửa — dải mờ và mũi tên ở mép phải, gắn một lần ở tầng bố cục nên che cả bảng viết sau |
| E3 | Tài liệu số → Kho tài liệu số | Bảng rộng 1.700 px nhồi vào khung **848 px** trong khi cây bộ sưu tập bên trái chỉ chiếm một phần tư màn hình. Bốn cột cuối (Chính sách, Chữ chìm, Tải về, Thao tác) bị cắt giữa chừng | Tài liệu số → Kho tài liệu số | Nặng | Giao diện | Đã sửa — cây bộ sưu tập thu hẹp, khung bảng nới từ 848 px lên 1.136 px |
| E4 | OPAC → Duyệt theo tác giả; Danh mục → Tác giả | Hồ sơ thẩm quyền tác giả nhận cả những giá trị không thể là tên người: **hai công thức bảng tính** (`+AA2994AA2967:AA2997AA29AA2967:AA2994`), một dòng `6th edition`, và một nhan đề dài 91 ký tự đặt nhầm vào ô tác giả. Hai công thức đứng **đầu tiên** trên trang công khai vì sắp theo bảng chữ cái, nên đó là hai thứ đầu tiên bạn đọc nhìn thấy. Dữ liệu bẩn tới từ `dc:creator` của kho nguồn, nhưng hệ thống nhận vào mà không kiểm gì | OPAC → Duyệt theo tác giả → Tất cả | Vừa | Dữ liệu | Đã sửa — lọc giá trị không thể là tên trước khi lập hồ sơ thẩm quyền, kèm migration gỡ 7 mục đã lọt |
| E5 | Toàn hệ thống | Nạp xong bộ dữ liệu trình diễn lớn mà tham số tên thư viện vẫn để mặc định, nên đầu trang OPAC và giao diện quản trị đều hiện đúng chữ "Thư viện". Bộ mặc định đã đặt sẵn một tên mẫu (lỗi D9) nhưng bộ lớn thì chưa | Nạp `LC_SEED_DEMO=rich` rồi mở trang chủ OPAC | Nhẹ | Dữ liệu | Đã sửa — bộ trình diễn lớn đặt tên thư viện và mã cơ quan MARC nếu tham số còn để mặc định |

### Đã nghi là lỗ hổng nhưng kiểm ra thì không phải

Ghi lại vì đây là chỗ dễ báo động nhầm, và báo động nhầm cũng tốn công người đọc y như bỏ sót.

Hai tên tác giả bắt đầu bằng dấu `+` làm dấy lên nghi ngờ **tiêm công thức vào tệp bảng tính**
(CWE-1236): giá trị bắt đầu bằng `= + @ -` khi mở bằng Excel sẽ được hiểu là công thức. Đã xuất thật
danh mục tác giả ra tệp (546 KB, 9.362 dòng) rồi mở bằng `openpyxl` để soi từng ô: bốn ô bắt đầu
bằng `+`, nhưng **kiểu ô là chuỗi** (`data_type='s'`), nghĩa là tệp .xlsx có khai rõ đây là chữ chứ
không phải công thức, và Excel không tính nó. Lỗ hổng ấy chỉ áp cho tệp CSV — định dạng không mang
thông tin kiểu — mà hệ thống thì không xuất CSV ở đâu cả. **Không phải lỗ hổng.** Phần còn lại là
lỗi chất lượng dữ liệu, ghi ở E4.

### Những chỗ đã kiểm lại và vẫn tốt

| Phép thử | Kết quả |
|---|---|
| Gõ `<script>alert(1)</script> & "nháy" 'đơn' \ | ; -- DROP TABLE; =CMD()` vào tên danh mục | Lưu được, đọc lại nguyên văn, không bị cắt và không thực thi ở đâu |
| Chính sách lưu thông với số âm | 400 kèm câu tiếng Việt |
| Tạo danh mục với mã và tên rỗng | 400 |
| Mở địa chỉ biểu ghi / bạn đọc không tồn tại | 404 kèm câu tiếng Việt |
| `page=-5&pageSize=999999` | Ép về khoảng hợp lệ, trả 500 dòng |
| Gửi hai lượt tạo cùng một mã danh mục **cùng lúc** | Chỉ một lượt thành công, kho có đúng một bản ghi |
| Nhập biểu ghi bằng tệp rỗng | 400 `"Vui lòng chọn tệp biểu ghi cần nhập."` |
| Xuất ISO 2709 và MARCXML toàn kho rồi cho `pymarc` đọc | 7.675/7.675 hợp lệ, 0 lỗi |
| Ảnh bìa: gọi lại kèm dấu bản (ETag) | 304, trình duyệt giữ bản cũ |
| 72 màn hình: mã định danh máy, JSON thô, chữ tiếng Anh của khung nền, ảnh hỏng | Không màn hình nào lọt |

**Chưa đi tới trong đợt này:** trình soạn MARC thao tác bằng chuột, thiết kế mẫu phích / mẫu thẻ /
mẫu tem, luồng kiểm kê từ đầu đến cuối, đóng tập ấn phẩm định kỳ, trình đọc tài liệu số có đóng dấu
chìm, và sao lưu – phục hồi. Đây vẫn đúng những chỗ chưa đi tới của đợt đầu.

---

### Tình hình sửa — đợt rà thứ hai

Cả 5 lỗi đã sửa, mỗi lỗi kèm một phép thử **chạy đỏ trước khi sửa và xanh sau khi sửa**.

| # | Đã làm | Bằng chứng |
|---|---|---|
| E1 | Mọi cột của bảng danh mục khai bề rộng, bảng cuộn ngang theo đúng tổng bề rộng ấy | Phép thử `catalogColumns.test.ts` chạy đỏ với hai chỗ vi phạm. Đo lại trên hệ thống: không còn cột nào rộng 0 px, ô tiêu đề từ 91 px xuống một dòng, bảng cuộn 1.810 px trong 1.136 px |
| E2 | Dải mờ và mũi tên ở mép phải mọi bảng còn cột nằm ngoài khung. Gắn **một lần** ở tầng bố cục dùng chung (`useTableScrollHint`) chứ không sửa 18 màn hình — cách này che cả bảng viết sau | Ảnh chụp màn hình Danh mục → Tác giả: mũi tên ở mép phải. Ant Design tự khai `left: 0` cho hai phần tử giả ấy nên lần đầu dấu hiệu rơi về mép trái — phải khai `left: auto` mới đúng chỗ |
| E3 | Cây bộ sưu tập của Kho tài liệu số thu hẹp lại, và chỉ giữ hai cột từ 1600 px trở lên | Đo lại: khung bảng nới từ 848 px lên 1.136 px |
| E4 | Lọc giá trị không thể là tên người hay tên cơ quan trước khi lập hồ sơ thẩm quyền: công thức bảng tính, dòng lần xuất bản, chuỗi không có chữ cái nào, và câu có dấu hai chấm giữa dòng. Trường MARC giữ nguyên — chỉ không dựng điểm truy cập từ nó | 23 phép thử đơn vị, trong đó 10 phép thử giữ cho tên thật không bị loại nhầm. Migration gỡ **7 mục** khỏi 9.361; trang Duyệt theo tác giả nay mở đầu bằng "Ahmad Binti Nurbarirah" thay vì hai công thức |
| E5 | Bộ trình diễn lớn đặt tên thư viện và mã cơ quan MARC nếu tham số còn để mặc định | Đặt xong trên máy đang chạy: trang chủ OPAC hiện "Thư viện Trường Đại học Mẫu" |

**Bộ lọc tên ở E4 cố ý dè dặt.** Loại nhầm một cái tên thật thì biểu ghi mất điểm truy cập, tệ hơn
là để lọt vài dòng rác — nên chỉ loại những gì chắc chắn không phải tên. Riêng chỗ phân biệt nhan đề
với tên cơ quan thì đếm số từ không đủ: nhan đề *"Adoption of fintech payment services in vietnam:
Empirical evidence from an emerging country"* có 13 từ, đúng bằng số từ của tên cơ quan *"Trường Đại
học Tài nguyên và Môi trường Thành phố Hồ Chí Minh"*. Dấu hai chấm giữa dòng mới là chỗ phân biệt
được: đó là dấu ngăn nhan đề chính với nhan đề phụ theo quy tắc mô tả ISBD, còn tên thì không dùng.

---

## G. Đợt áp bản thiết kế giao diện

Đợt này không đi rà lỗi — việc chính là áp một bản thiết kế mới (nền giấy ngà, xanh rêu, chữ có
chân Lora cho tiêu đề) vào cả hai ứng dụng. Nhưng cứ mỗi lần **đo** thay vì nhìn ảnh chụp là lại ra
một lỗi, và năm trong chín lỗi dưới đây có từ các phase trước chứ không phải mới sinh ra.

Bài học chung của cả bảng: **ảnh chụp màn hình không phải bằng chứng.** Một phần tử thiếu hẳn kiểu
vẫn hiện ra, chỉ là hiện trần; nhìn thì thấy "hơi nhạt", phải `getComputedStyle` mới thấy nền của
nó là `rgba(0, 0, 0, 0)`.

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| G1 | Toàn hệ thống — hạ tầng | Nginx khai máy chủ đích bằng khối `upstream`, mà khối ấy **tra tên máy một lần lúc khởi động rồi ghim IP mãi**. Dựng lại một dịch vụ là nó nhận IP mới còn Nginx vẫn gửi tới IP cũ. Xảy ra thật: sau `docker compose up -d admin opac`, hai container **đổi chỗ IP cho nhau**, và **trang tra cứu công khai trả về giao diện quản trị** — mã trả về vẫn 200, nhật ký Nginx không có một dòng lỗi nào | `docker compose up -d --force-recreate admin opac` rồi mở `http://localhost/` | **Nghiêm trọng** | Hạ tầng | Đã sửa — dùng máy chủ tên của Docker (`resolver 127.0.0.11 valid=10s`) và đặt tên dịch vụ vào biến, nên Nginx tra tên lại theo từng yêu cầu. Sửa cả `nginx.conf` lẫn `nginx.prod.conf`. Kiểm bằng cách ép hai container đổi IP cho nhau rồi gọi lại **không** khởi động lại Nginx |
| G2 | OPAC → chi tiết tài liệu | **4.624 biểu ghi** hiện dòng "Mô tả vật lý: `application/pdf`" cho bạn đọc. Bộ ánh xạ Dublin Core cũ đổ `dc:format` vào trường 300; migration `20260902100000` đã sửa cả bộ ánh xạ lẫn trường MARC — kiểm lại thì **không còn biểu ghi nào** mang `application/*` ở trường 300 — nhưng nó bỏ sót **cột phẳng** `pages`, mà đó mới là thứ trang tra cứu thật sự đọc | Mở bất kỳ tài liệu nào thu hoạch qua OAI-PMH trước ngày 2026-09-02 | Nặng | Dữ liệu | Đã sửa — migration `20260902160000` rút lại `pages` từ chính `300$a`, kèm chặn ở **tầng chiếu** (`MarcProjection.MoTaVatLy`) để bịt cả sáu lối vào kho, không riêng Dublin Core. Đo: 4.624 → 0, còn 6.526 mô tả vật lý thật giữ nguyên |
| G3 | Quản trị → Quản lý kho | Chọn một thư viện thì hàng ấy **không sáng lên**, nên cán bộ không biết danh sách kho bên dưới đang là của thư viện nào — nhất là sau khi cuộn. Lớp `lc-row-selected` được gắn vào hàng từ phase 6 nhưng **chưa bao giờ có luật CSS nào** | Bổ sung → Quản lý kho → bấm một thư viện | Vừa | Giao diện | Đã sửa — nền xanh nhạt kèm vạch đứng 3 px ở mép trái |
| G4 | Quản trị → Nhập dữ liệu bạn đọc | Dòng nhập sai trong bảng xem trước **không đỏ**, phải đọc từng cột của từng dòng mới tìm ra, mà cột "lỗi" lại nằm ngoài khung nhìn bên phải. Lớp `lc-row-error` cũng chưa bao giờ có kiểu | Bạn đọc → Nhập xuất dữ liệu → tải lên tệp có dòng sai | Vừa | Giao diện | Đã sửa — nền đỏ nhạt kèm vạch đứng ở mép trái |
| G5 | Quản trị → Quản trị nội dung | Khung soạn thảo tin tức và trang tĩnh (`contentEditable`) không viền, không nền, không con trỏ trước khi bấm vào — nhìn hệt một khoảng trắng, người dùng không biết gõ vào đâu. Lớp `lc-editor` chưa có kiểu | Quản trị nội dung → Tin tức → Thêm mới | Vừa | Giao diện | Đã sửa — viền và nền giấy như mọi ô nhập khác, sáng viền khi đang gõ |
| G6 | Quản trị → Quản lý kho → Bản đồ kho | Ô giá trống trên lưới bản đồ không vẽ gì nên lẫn với nền trang, cái lưới mất hình dạng, nhìn không ra kho có mấy hàng mấy cột. Hai lớp `lc-shelf-cell` và `lc-shelf-cell-empty` chưa có kiểu | Bổ sung → Quản lý kho → Giá và bản đồ kho | Nhẹ | Giao diện | Đã sửa — ô trống có viền đứt và nền kẻ chéo mờ. **Chưa nhìn tận mắt được**: bộ dữ liệu hiện tại chưa giá nào đặt vị trí hàng/cột nên bản đồ không vẽ ra. Chỉ chốt được bằng phép thử quét mã nguồn |
| G7 | Quản trị → thanh trên | Huy hiệu tình trạng máy chủ cao **66 px** trong một thanh cao 58 px, thành một vệt bầu dục to hơn cả thanh. Nguyên nhân: Ant Design đặt `line-height` của thanh trên bằng đúng chiều cao thanh, huy hiệu thừa kế con số ấy, cộng lề trong thành 66 px, mà `border-radius: 999px` biến nó thành hình viên thuốc khổng lồ | Đăng nhập giao diện quản trị, nhìn góc phải thanh trên | Nhẹ | Giao diện | Đã sửa — khai lại `line-height` cho riêng huy hiệu |
| G8 | 32 màn hình của giao diện quản trị + 4 của trang tra cứu | Áp xong bản thiết kế mà **130 chỗ trong 33 màu** vẫn giữ nguyên bảng màu mặc định của Ant Design, vì chúng viết màu **thẳng trong TSX** nên không đi qua token nào: cả trang báo cáo đầy biểu đồ xanh dương `#1677ff` giữa một sản phẩm đã chuyển hẳn sang xanh rêu trên nền giấy. Hai trong số đó còn **trượt tương phản** ngay từ trước: xanh `#52c41a` trên nền giấy chỉ đạt **2,23:1** và cam `#faad14` đạt **1,87:1** — dưới cả ngưỡng 3:1 của chữ cỡ lớn, mà chúng đang là màu con số thống kê | Lưu thông → Báo cáo lưu thông; Bạn đọc → Báo cáo bạn đọc | Nặng | Giao diện | Đã sửa — dựng `lib/palette.ts` cho ba loại chỗ không đi qua Ant Design được (`valueStyle` của `Statistic`, `fill`/`stroke` của Recharts, vài khối tự vẽ), thay hết 130 chỗ, kèm phép thử cấm viết mã màu thẳng trong TSX |
| G9 | Ba trang báo cáo | Biểu đồ tròn lấy **màu ngữ nghĩa** làm màu phân loại: `MAU.chinh` và `MAU.tot` là hai sắc xanh rêu gần nhau nên hai mảng cạnh nhau không phân biệt được, mà chúng còn *mang nghĩa* — xanh lá "tốt", đỏ "hỏng" — trong khi ở đây chỉ đang đánh dấu loại bạn đọc, chẳng có gì tốt xấu | Bạn đọc → Báo cáo bạn đọc → Số lượng bạn đọc | Nhẹ | Giao diện | Đã sửa — biểu đồ phân loại dùng dải 12 sắc riêng, đo lại: 5 mảng ra 5 sắc khác hẳn nhau |

### Ba lỗi tự mình gây ra ngay trong đợt này

Ghi thẳng, không bào chữa:

1. **Luật CSS của huy hiệu tình trạng rơi mất lúc soạn tệp.** Viết xong khối luật, lúc chuyển cách
   soạn thì đánh rơi mất nó. Ảnh chụp trông vẫn bình thường vì huy hiệu là chữ, chữ thì vẫn hiện.
   Chỉ `getComputedStyle` mới thấy nền là `rgba(0, 0, 0, 0)`.
2. **Luật đổi màu thẻ đặt sẵn không hề có tác dụng.** Ant Design 5 sinh kiểu bằng JavaScript rồi
   chèn vào `<head>` **sau** tệp kiểu của mình, và bọc bộ chọn trong `:where(...)` nên độ ưu tiên
   chỉ là 0-1-0 — bằng đúng `.ant-tag-blue` viết trần. Bằng nhau thì cái chèn sau thắng. Nhìn ảnh
   thì thẻ vẫn "hơi xanh", dễ tưởng đã ăn. Sửa bằng cách viết `.ant-tag.ant-tag-blue` cho lên 0-2-0.
3. **Migration `20260902100000` của đợt trước thiếu phần dọn cột phẳng** — chính là lỗi G2. CLAUDE.md
   mục A.3 đã ghi sẵn bài học "sửa mã nguồn thôi thì số dữ liệu cũ vẫn nằm im", mà vẫn vi phạm.

### Đo được gì bằng máy trong đợt này

| Phép đo | Kết quả |
|---|---|
| Hai bộ chữ có thật sự hiện không | Đo bề rộng cùng một chuỗi bằng ba phông: Be Vietnam Pro 441,8 px · Arial 415,9 px · Lora 431,8 px · serif 388,4 px — khác nhau rõ ở **cả hai** ứng dụng, nên cả hai phông đều đã tải và đang dùng |
| Độ tương phản WCAG AA | 19 cặp màu, **18 đạt**. Một cặp trượt: chữ thẻ `#7a6f5f` trên nền thẻ `#f1ebdd` chỉ được **4,14:1** (cần 4,5:1) — nhìn thì vẫn đọc được, mà thẻ đang mang thông tin thật (dạng tài liệu, ngôn ngữ, trạng thái bản in). Đã đổi sang `#6e6252`, đạt 5,00:1 |
| Lớp `lc-*` gắn vào phần tử mà thiếu kiểu | 7 lớp (1 mới, 6 có từ trước) → 0 |
| Màu viết thẳng trong TSX, không qua token | 130 chỗ / 33 màu → **6 chỗ**, và cả sáu đều có lý do ghi rõ: nền thẻ nhựa và nền ảnh thẻ phải trắng thật (thẻ in trên phôi trắng), vạch mã vạch phải gần đen tuyệt đối (máy quét đọc theo độ tương phản), màu chữ mặc định trong ô mẫu thẻ là mực in, nền khung máy ảnh để đen cho khuôn mặt nổi lên |
| Dải 12 màu biểu đồ trên nền giấy | thấp nhất 7,32:1 — cả dải đạt |
| Luật CSS khai ra rồi bỏ đấy | 1 (`lc-cover--placeholder`, sót lại từ đợt chuyển bìa thay thế sang cho máy chủ dựng) → 0 |
| Chữ bị cắt trên 15 màn hình | 0 |
| Lỗi JavaScript trên 15 màn hình | 0 |

### Bốn phép thử quét mã nguồn mới

Nâng tổng số lên **mười một**. Cả bốn đều đã kiểm **đỏ trước, xanh sau**:

| Phép thử | Luật | Đã đỏ khi nào |
|---|---|---|
| `frontend-*/src/styles.test.ts` | Mọi lớp `lc-*` gắn vào phần tử phải có kiểu; mọi luật khai ra phải có người dùng | Gỡ thử luật `.lc-status-pill` → đỏ cả hai chiều |
| `frontend-*/src/theme.test.ts` | Token của `theme.ts` phải trùng biến `--lc-*` của `styles.css`; hai bộ chữ phải được `index.html` tải thật | Lệch **một chữ số hex** (`#35523f` → `#35523e`) → đỏ ngay |
| `frontend-*/src/theme.test.ts` (phần WCAG) | 16 cặp màu của bảng màu phải đạt ngưỡng tương phản 4,5:1 (chữ thường) hoặc 3:1 (chữ phụ trợ) | Chính cặp thẻ trung tính đã đỏ và buộc phải đổi màu |
| `frontend-*/src/lib/palette.test.ts` | Không viết mã màu thẳng trong TSX; bảng màu dùng chung phải trùng token; mọi màu con số và màu biểu đồ phải đạt tương phản trên nền giấy | Đặt lại `#389e0d` vào một `valueStyle` → đỏ, chỉ đúng tên tệp và mã màu |

---

## H. Đợt rà thứ ba — sáu chỗ hai đợt trước chưa đi tới

Rà đúng những chỗ hai đợt trước ghi là *chưa đi tới*: trình soạn MARC thao tác bằng chuột, ba trình
thiết kế mẫu, luồng kiểm kê từ đầu đến cuối, đóng tập ấn phẩm định kỳ, trình đọc tài liệu số có chữ
chìm, và sao lưu – phục hồi. Cách rà giữ nguyên: mở trình duyệt thật ở khổ 1440×900, đăng nhập thật,
cố tình đi đường sai, đo bằng `getComputedStyle` thay vì nhìn ảnh, và với việc dài thì gọi thẳng API
rồi đối chiếu trong cơ sở dữ liệu. Đợt này cũng áp lại bản thiết kế OPAC theo dự án Claude Design
"LibraryConnect layout design" — đầu trang một hàng, câu chào giữa trang, nút "Tra cứu" vàng đồng,
thẻ kết quả có bìa 60 × 84, trang chi tiết có bảng "Bản sẵn có tại thư viện", chân trang ba cột.

Lỗi nặng nhất đợt này **không nằm ở sáu chỗ ấy** mà lộ ra ngay ở bước thứ hai của trình soạn MARC:
sửa một biểu ghi đã có thì không lưu được.

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| H1 | Nhật ký máy chủ | Mọi lỗi nghiệp vụ — 400, 401, 404 — đều bị ghi thành **`ERR … trả về 500`** kèm cả vết ngăn xếp, vì bộ ghi nhật ký yêu cầu đứng *sau* bộ xử lý ngoại lệ nên nhìn thấy ngoại lệ trước khi nó được đổi thành mã đúng. Đếm trên máy đang chạy: 5 dòng "trả về 500" mà không có lượt nào trả 500 thật. Lỗi 500 thật chìm trong đó | Gõ sai mật khẩu một lần rồi đọc `docker logs lc-api` | Vừa | Vận hành | Đã sửa — đảo thứ tự hai tầng; phép thử quét `Program.cs` chốt thứ tự |
| H2 | Trình soạn MARC → Lưu | Khung mẫu điền sẵn 20 trường; cán bộ điền 3 trường rồi Ctrl+S → biểu ghi lưu **nguyên 24 trường con rỗng** (`020 ## $a $c`, `650 #4 $a $x`, `700 1# $a $e`…). Bộ kiểm biết — bấm "Kiểm tra" ra đúng 24 cảnh báo — nhưng "Lưu" vẫn ghi và chỉ báo "Đã lưu". Xuất ISO 2709 là mang rác sang thư viện khác, tab "Xem MARC" của bạn đọc hiện nguyên chúng ra. Đếm kho: mọi nguồn khác đều sạch, lỗi nằm đúng ở đường nhập tay | Biên mục mới → chọn "Sách" → điền 245 → Ctrl+S → tab MARC thô | Nặng | Nghiệp vụ | Đã sửa — gọt ở **tầng ghi** (`MarcCleanup`) trước khi kiểm tra, bịt cả sáu lối vào kho; migration gọt biểu ghi đã có |
| H3 | Trình soạn MARC | Thiếu so với đặc tả II.2: wizard nhập 008 (đang là ô 40 ký tự thô), kéo thả sắp xếp trường, Ctrl+D nhân bản dòng, nút "Lấy từ Z39.50 / Lấy từ ISBN" ngay trên biểu mẫu, xem trước ISBD *trước khi lưu*. Bảng đáp ứng `docs/07` không khai các mục này nên không phải khai gian, nhưng đặc tả gốc có | So II.2 của CLAUDE.md với màn hình | Thiếu chức năng | Đặc tả | Chưa làm — ghi ở "Làm tiếp" |
| H4 | Sửa biểu ghi (`PUT /api/cataloging/bibs/{id}`) | Thêm **bất kỳ** điểm truy cập mới — tác giả, đề mục, từ khóa, phân loại — vào biểu ghi đã có là **409** "Không lưu được dữ liệu. Vui lòng kiểm tra lại thông tin nhập." Ghi SQL của PostgreSQL ra mới thấy: liên kết mới chỉ được `collection.Add` vào navigation của một biểu ghi đang *Unchanged*, Entity Framework thấy khoá đã có giá trị nên coi nó là dòng có sẵn và phát `UPDATE bib_authors … WHERE id = <mới>` với `created_at = '-infinity'` — 0 dòng bị ảnh hưởng, `DbUpdateConcurrencyException`. Với biểu ghi *mới* thì cha đang Added nên con cũng Added, vì thế đường tạo mới và mọi phép thử cũ đều xanh. Kết hợp H5 thì **6.781 trên 7.680 biểu ghi có trường 653 không sửa được** | Sửa biểu ghi tối giản, thêm `100$a` một tên mới → Lưu | **Nghiêm trọng** | Nghiệp vụ | Đã sửa — khai thẳng `Set<TLink>().Add(link)` với bộ theo dõi; phép thử tích hợp thêm tác giả và từ khóa vào biểu ghi đã có |
| H5 | Bộ đối chiếu thẩm quyền | Lọc sơ bộ theo **từ đầu tiên** rồi `.Take(200)`: "Nguyễn" khớp 3.060 tác giả trên kho thật, tên cần tìm nằm ngoài 200 dòng đầu, và ngay **lần lưu thứ hai của cùng một biểu ghi** đã tạo thêm `NGUYEN_VAN_KIEM_2`. Đo kho trước khi sửa: 485 nhóm tác giả trùng (616 dòng thừa), 109 nhóm từ khóa trùng (146 dòng thừa) — đúng thứ mà tệp thẩm quyền sinh ra để tránh | Lưu hai biểu ghi cùng tác giả họ Nguyễn → Danh mục → Tác giả | Nặng | Dữ liệu | Đã sửa — hàm `cat.lc_name_key` tính cùng khoá với `NormaliseForComparison`, chỉ mục hàm trên năm bảng thẩm quyền, so khớp ngay trong SQL, không còn ngưỡng; migration gộp **790 tác giả và 156 từ khóa** trùng, trỏ lại liên kết |
| H6 | Trình thiết kế phích + 5 màn hình | Đợt thay 130 màu (`58326b0`) đổi `'#e3d9c7'` thành `'${MAU.vien}'` mà giữ **dấu nháy đơn**: trình duyệt nhận nguyên văn `1px solid ${MAU.vien}`, coi là CSS sai và bỏ. Đo: khung phích 400 × 240 px có `border: 0px none`, nền giấy trên nền giấy — kéo ô ra ngoài mép cũng không thấy mép đâu. 11 chỗ ở 6 tệp: khung, lề và ô đang chọn của trình thiết kế phích, viền ảnh bìa, **viền đỏ ô tủ quá giờ**, viền khung soạn thảo tin (làm hỏng lại G5 một nửa), xem trước logo, khung định nghĩa trường MARC; hai tệp còn chưa `import MAU`. Phép thử cấm mã màu thẳng vẫn xanh vì không còn mã màu để bắt | Biên mục → Mẫu phích → Thêm mẫu, đo viền khung | Vừa | Giao diện (tự gây) | Đã sửa — dấu huyền và import; luật quét mới ở cả hai gói, đỏ trước xanh sau |
| H7 | Toàn hệ thống — hạ tầng | Hai nửa của một lỗi. **(a)** `CurrentUser.Ip` lấy thẳng giá trị **đầu tiên** của tiêu đề `X-Forwarded-For` — chữ do người gọi tự viết. Thử thật: gửi từ một container khác qua Nginx kèm `X-Forwarded-For: 203.0.113.9` → bảng `login_histories` ghi đúng `203.0.113.9`. Nghĩa là nhật ký hệ thống (I.4), lịch sử đăng nhập và chữ chìm trên tài liệu số đều **giả được** bằng một dòng tiêu đề. **(b)** Bộ trung gian ForwardedHeaders mặc định chỉ tin proxy ở loopback, mà Nginx trong Docker đứng ở `192.168.0.x`, nên `RemoteIpAddress` — thứ bộ giới hạn tốc độ dùng để chia ngăn — là địa chỉ của Nginx cho mọi yêu cầu: **toàn bộ bạn đọc chung một ngăn 300 yêu cầu/phút**, toàn bộ cán bộ chung 20 lượt đăng nhập/phút; 200 người dùng đồng thời (mục 6.3) là 429 hàng loạt. Lúc đầu tưởng 96.737 dòng nhật ký cùng một IP là bằng chứng của (b) — không phải: trên máy phát triển mọi yêu cầu đều từ một máy, IP ấy là địa chỉ cổng Docker của chính máy này. Bằng chứng thật là phép thử giả tiêu đề và mã nguồn của bộ giới hạn | Từ container khác: `curl -H 'X-Forwarded-For: 203.0.113.9' http://nginx/api/auth/login` rồi đọc `sys.login_histories` | **Nghiêm trọng** | Hạ tầng | Đã sửa — `ForwardedHeadersSetup` tin ba dải RFC 1918 và loopback với `ForwardLimit = 1`, khai được `LC_Proxy__TrustedNetworks`; `CurrentUser.Ip` chỉ đọc `RemoteIpAddress` đã qua bộ trung gian. Kiểm lại từ container khác qua Nginx: ghi đúng địa chỉ của container (`192.168.0.9`), tiêu đề bịa bị bỏ. Gọi thẳng cổng API từ máy chủ Docker thì tiêu đề vẫn được tin — đúng thiết kế, vì máy chủ ấy nằm trong dải được tin; bản triển khai thật không mở cổng API ra ngoài (`ports: !override []` trong `docker-compose.prod.yml`) |

### Nguy cơ ghi nhận, chưa tái hiện được

| # | Chỗ | Nguy cơ | Đo được gì |
|---|---|---|---|
| H9 | Sao lưu thủ công | Chạy **ngay trong lượt HTTP** — đúng lớp lỗi số 4 của CLAUDE.md. Kho lớn kèm kho đối tượng vài GB là vượt 300 giây của proxy, việc bị bỏ dở, dòng nhật ký kẹt "Đang chạy" | 4,2 giây cho bản dump 40 MB không kèm MinIO; trên máy này không dựng được kho đủ lớn để chạm ngưỡng. Để "Làm tiếp": xếp vào Hangfire như thu hoạch OAI-PMH, màn hình đọc trạng thái từ bảng `backup_jobs` |

### Đã kiểm lại và vẫn tốt

| Phép thử | Kết quả |
|---|---|
| Trình soạn MARC bằng chuột: chọn dạng tài liệu, điền, Ctrl+S, thêm trường 856 từ gợi ý, tách chuỗi `$a…$b…` dán vào | Chạy đúng; trường mới chèn đúng thứ tự nhãn |
| Trình thiết kế phích: đặt tên, bấm ô để sửa, **kéo thả** ô, lưu, in 53 biểu ghi × 4 loại phích ra PDF | Kéo đúng (X 30 → 0, Y 0 → 58,8 mm, có kẹp trong mép phích); PDF 7 trang, 3 phích/tờ A4, tiếng Việt đủ dấu; chặn trên 2.000 biểu ghi một lần in kèm câu tiếng Việt |
| Kiểm kê từ đầu đến cuối qua API: tạo kỳ trên kho 4.421 bản → quét khớp, quét bản của kho khác, quét mã không có, quét trùng → tổng hợp → chốt → kết quả → xuất Excel → quét sau khi chốt | Bốn kết cục đúng tên ("Khớp", "Sai kho", "Thừa", "đã quét rồi"); chốt: khớp 2, thiếu 4.419, thừa 1, sai kho 1; Excel 318 KB; quét sau khi chốt bị từ chối 409 |
| Đóng tập: ba số đã nhận → tập `DT00001` kèm ĐKCB mới; đóng lại cùng ba số; đóng một số còn thiếu | 200 → 409 "đã đóng tập từ trước" → 409 "không có số nào đã nhận" |
| Trình đọc tài liệu số: trang 1 dạng ảnh qua cả lối cán bộ lẫn lối bạn đọc | PNG 909 × 1286, chữ chìm chéo lặp khắp trang ghi số thẻ, giờ và IP — chính dòng IP ấy dẫn tới H7 |
| Sao lưu → tạo một biểu ghi → phục hồi (mật khẩu sai rồi mật khẩu đúng) | Dump 40 MB, `pg_restore --list` đọc được 129 bảng; mật khẩu sai 400 kèm tên trường; phục hồi 12,9 giây, biểu ghi tạo sau sao lưu biến mất, dữ liệu trước đó nguyên vẹn, `/health` xanh |
| 18 phép thử biên mục cũ sau khi đổi bộ đối chiếu thẩm quyền | Xanh cả |

### Ba lỗi tự mình gây ra, ghi thẳng

1. **H6 là hậu quả trực tiếp của commit trước.** Thay 130 màu bằng công cụ mà không nhìn từng chỗ; và
   phép thử viết cho việc ấy chỉ bắt *mã màu*, không bắt *chuỗi mẫu viết nhầm dấu nháy* — nó xanh
   trong khi 11 chỗ hỏng.
2. **H5 đã được "sửa" một lần rồi** (bỏ dấu ở cả hai phía) mà vẫn còn nguyên cái ngưỡng 200; phép
   thử cho lần sửa ấy dùng hai tên trong một kho trống nên không bao giờ chạm ngưỡng. Lần này phép
   thử dựng đúng 220 tác giả cùng họ trước rồi mới lưu.
3. **H4 sống từ phase 5** vì phép thử sửa biểu ghi chỉ đổi nhan đề — chưa bao giờ *thêm* một điểm
   truy cập. Bài học số 7 của CLAUDE.md, tự mình vi phạm.

### Phép thử mới

| Phép thử | Bắt gì | Đã đỏ khi nào |
|---|---|---|
| `LibraryConnect.UnitTests/Infrastructure/RequestLoggingOrderTests.cs` | Bộ ghi nhật ký yêu cầu đứng trước bộ xử lý ngoại lệ trong `Program.cs` | Trước khi đảo thứ tự |
| `LibraryConnect.UnitTests/Marc/MarcCleanupTests.cs` | Gọt trường con rỗng, bỏ trường trống, không đụng trường điều khiển | — (hàm mới) |
| `LibraryConnect.IntegrationTests/BibEditReviewTests.cs` (3) | Thêm điểm truy cập vào biểu ghi đã có; 220 tác giả cùng họ không làm tạo trùng; khung mẫu rỗng không lưu | Cả ba đỏ trước khi sửa |
| `LibraryConnect.IntegrationTests/ForwardedHeadersSetupTests.cs` (5) | Tin mạng nội bộ, không tin IP lạ, khai dải riêng thì thay mặc định, `ForwardLimit = 1`, dải sai dạng báo rõ | — (lớp mới; bằng chứng đỏ là mã nguồn bộ giới hạn tốc độ đọc `RemoteIpAddress` với proxy chưa được tin) |
| `LibraryConnect.IntegrationTests/CurrentUserIpTests.cs` (3) | Không tin tiêu đề `X-Forwarded-For` thô; địa chỉ IPv4 ánh xạ IPv6 trả về dạng IPv4; không có kết nối thì null | Đỏ trước khi sửa `CurrentUser.Ip` |
| `frontend-*/src/lib/palette.test.ts` (luật mới, cả hai gói) | Không chuỗi nháy đơn nào chứa `${MAU.…}` | Đỏ với đúng dòng 101 của `CardDesigner.tsx` |

---

## I. Đợt triển khai lên máy chủ thật (03/09/2026)

Lần đầu dựng hệ thống từ một bản clone sạch trên máy khác, không phải từ thư mục làm việc.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| I1 | Nghiêm trọng | Clone kho từ GitHub dựng ảnh API **không biên dịch được**: thiếu namespace `Features.Admin.Backups` | Dòng `backups/` trong `.gitignore` (dành cho thư mục bản sao lưu) khớp luôn thư mục mã nguồn `Features/Admin/Backups/`, mà Git trên Windows không phân biệt hoa thường. `BackupFeatures.cs` chưa bao giờ được commit, suốt từ phase 2. Mọi phép thử đều xanh vì chạy trên thư mục làm việc có sẵn tệp ấy. | Neo luật thành `/backups/` và `deploy/backups/`, đưa tệp vào Git (`d77b917`). |
| I2 | Nặng | Ứng dụng di động **vỡ giao diện trên Samsung thật** (ảnh người dùng gửi): nhãn thanh điều hướng gãy hai dòng; thẻ "Bản in" ép ĐKCB thành cột vài ký tự; hai nút dưới bảng trích dẫn bị thanh điều hướng hệ thống che; chip tác giả bị cắt cụt | Máy để cỡ chữ lớn (≈1,3) và thu phóng màn hình, bề rộng hữu dụng chỉ 360dp. Phase 15 chỉ soi cỡ chữ lớn trên máy ảo Pixel 9 **411dp** — chưa bao giờ ở 360dp, là bề rộng phổ biến nhất của điện thoại Việt Nam. `ListTile.trailing` nhận trọn bề rộng nó đòi; bảng trích dẫn không cuộn và không chừa vùng an toàn dưới; cột cạnh ảnh bìa chỉ còn ~200dp. Tái hiện đúng bằng `wm density 480` + `font_scale 1.3` trên máy ảo. | Thanh điều hướng ghim cỡ chữ 1,0 và nhãn "Tủ sách"; viên trạng thái xuống dưới ĐKCB; bảng trích dẫn cuộn được + `useSafeArea`; chip tác giả và viên trạng thái xuống dưới hàng ảnh bìa. Kiểm lại ở 360dp/1,3 trước và sau, ảnh trong `docs/images/mobile/`. |
| I3 | Vừa | CI đỏ: `/health/ready` trả **503** trên runner sạch dù phép thử xanh trên máy dev suốt từ phase 1 | Kiểm tra sẵn sàng thêm Redis hễ có chuỗi kết nối (mặc định `localhost:6379`), bỏ qua `Redis:Enabled=false` mà bộ kiểm thử đã đặt. Máy dev tình cờ có Redis ở cổng ấy nên xanh; runner GitHub không có nên đỏ. Một bản cài cố ý không dùng Redis cũng báo "không sẵn sàng". | Chỉ thêm kiểm tra Redis khi `Enabled` = true. Chạy đỏ trên CI (run 33713284557) trước khi sửa. |
| I4 | Nhẹ | `The_seeded_administrator_can_sign_in…` **phụ thuộc thứ tự**: chạy riêng lớp InstallationTests thì đỏ (mong 401, nhận 200), chạy cả bộ thì xanh | Bài khẳng định "mật khẩu tạm đã hết tác dụng" nhưng không tự đổi mật khẩu; nó trông chờ một bài khác đã gọi `SignInAsync` (fixture đổi mật khẩu tạm ở lượt đăng nhập đầu) trước nó. | Bài tự đăng nhập và đổi mật khẩu trước, rồi mới khẳng định mật khẩu tạm bị từ chối. Đỏ khi chạy riêng lớp trước khi sửa, xanh sau; trọn bộ 390/390. |

Bài học thứ hai của đợt: **máy ảo mặc định không phải điện thoại của người dùng.** Cỡ chữ lớn đã soi
ở phase 15 nhưng ở bề rộng 411dp; máy 360dp với cỡ chữ 1,3 là một cấu hình khác hẳn, và đó là máy
thật của người dùng đầu tiên. Từ nay soi ở `wm density 480` + `font_scale 1.3` trước khi phát hành.

Bài học: **"build sạch" trên máy phát triển không chứng minh kho mã đầy đủ.** Thư mục làm việc luôn
có mọi tệp; chỉ một bản clone mới lộ ra tệp nào chưa vào Git. Chưa có CI dựng từ clone sạch, nên lỗi
này sống qua 14 phase. Việc nên làm: một bước CI `git clone` + `docker compose build`.

Cùng đợt còn thêm lớp compose thứ ba `docker-compose.behind-proxy.yml` cho máy chủ đã có proxy giữ
cổng 80/443 (mục 5.1b của `docs/04`), vì `nginx.prod.conf` giả định LibraryConnect là ứng dụng duy
nhất trên máy.

---

## Đ. Những chỗ đã thử phá nhưng hệ thống chịu được

Ghi lại để biết chỗ nào đã kiểm và không phải kiểm lại — kèm bằng chứng, không ghi suông.

| Phép thử | Kết quả |
|---|---|
| Nhập ISO 2709 tệp bị cắt giữa chừng | Đọc được phần lành, đánh dấu biểu ghi hỏng, không đổ |
| Nhập ISO 2709 tệp rác hoàn toàn | `"Không tìm thấy dấu kết thúc danh mục (0x1E). Tệp không đúng định dạng ISO 2709."` |
| Nhập ISO 2709 tệp rỗng | 400 `"Vui lòng chọn tệp biểu ghi cần nhập."` |
| Nhập ISO 2709 biểu ghi thiếu trường 245 | Nhận tệp, từ chối đúng biểu ghi ấy: `"Biểu ghi không có nhan đề (trường 245$a)."` |
| Nhập ISO 2709 danh mục sai cấu trúc | `"Danh mục dài 37 byte, không chia hết cho 12 byte của một mục. Biểu ghi bị hỏng cấu trúc."` |
| Ghi mượn cho thẻ hết hạn / thẻ bị khoá | Chặn, câu thông báo nêu rõ lý do |
| Trả một cuốn đã trả rồi | Chặn |
| Xoá danh mục đang được dùng | 409 kèm câu giải thích đang bị gì tham chiếu |
| Mở URL của biểu ghi đã xoá trên OPAC | 404 đúng trang "không tìm thấy" |
| Tải lên tệp `.exe` đổi đuôi `.pdf` | 400, chặn theo chữ ký tệp |
| Tải lên tệp 0 byte | 400 |
| `page=-5`, `pageSize=999999` | Ép về 1 và 500, không đổ |
| Gọi API xuất báo cáo khi chưa đăng nhập | 401 kèm câu tiếng Việt |
| Nhập ISO 2709 tệp có ký tự đặc biệt (`<script>`, `& " \`) và dấu tiếng Việt đầy đủ | Xuất ra 581 byte, độ dài khai báo trong Leader khớp đúng byte thật; nhập lại được nguyên văn |
| Xuất MARCXML biểu ghi có `<script>` | Thoát ký tự đúng (`&lt;script&gt;`), XML hợp lệ |
| Nhận biết biểu ghi trùng theo số kiểm soát 001 | Đúng — chỉ ra biểu ghi trùng và số kiểm soát của nó |
| Chính sách lưu thông với số âm | 400 kèm câu tiếng Việt cho từng trường |
| Hạn thẻ đặt trước ngày cấp thẻ | 400 `"Ngày hết hạn thẻ phải sau ngày cấp thẻ."` |
| Thu tiền phạt nhiều hơn phần còn nợ / thu số âm | 400 cả hai, số dư không đổi |
| Gia hạn phiếu mượn đã quá hạn | 409 `"Tài liệu đã quá hạn từ ngày 08/08/2026, phải trả rồi mượn lại."` |
| Bạn đọc B gia hạn phiếu mượn của bạn đọc A | 403 `"Lượt mượn này không thuộc về bạn đọc đang đăng nhập."` |
| Bạn đọc gọi API quản trị (biểu ghi, người dùng, báo cáo, hồ sơ bạn đọc khác) | 403 cả bốn |
| Mượn tự phục vụ khi thư viện chưa bật chức năng | 409 `"Thư viện chưa mở chức năng mượn tự phục vụ."` |
| Toàn bộ 10 endpoint nhóm `/api/reader/*` với thẻ bạn đọc thật | 200, trả đúng dữ liệu của chính người đăng nhập |

---

## Cách rà soát và phạm vi đã đi qua

- Chụp ảnh **51 màn hình quản trị** và **19 màn hình OPAC** ở khổ 1440×900, lưu lại toàn bộ; đo bằng
  máy các dấu hiệu chữ bị cắt, bảng tràn khung, ảnh hỏng, chữ tiếng Anh lọt ra, mã định danh máy lọt
  ra; sau đó mở từng ảnh của những màn hình chính để nhìn bằng mắt.
- Gọi thẳng API không qua giao diện cho: nhập ISO 2709 (tệp hỏng, tệp rác, tệp rỗng, biểu ghi thiếu
  245), nhập Excel sai định dạng, chính sách lưu thông, hồ sơ bạn đọc, tiền phạt, gia hạn, mượn tự
  phục vụ, và toàn bộ nhóm `/api/reader/*` bằng thẻ bạn đọc thật.
- Nạp 7.676 biểu ghi thật từ nguồn ngoài qua đúng chức năng liên thư viện — chính đợt nạp này làm lộ
  ra A1, A6–A9 và C6, những lỗi mà bộ dữ liệu mẫu nhỏ không bao giờ chạm tới.

**Chưa đi tới:** trình soạn MARC thao tác bằng chuột trên màn hình thật (mới thử qua API), thiết kế
mẫu phích / mẫu thẻ / mẫu tem, luồng kiểm kê từ đầu đến cuối, đóng tập ấn phẩm định kỳ, trình đọc
tài liệu số có đóng dấu chìm, và sao lưu – phục hồi lần này.

---

## Tình hình sửa

| Mức độ | Tổng | Đã sửa | Còn lại |
|---|---|---|---|
| Nghiêm trọng | 4 | 4 | 0 |
| Nặng | 8 | 8 | 0 |
| Vừa | 18 | 18 | 0 |
| Nhẹ | 7 | 7 | 0 |

Tổng: **37 lỗi, đã sửa 37, còn 0**. (36 lỗi của đợt rà đầu, cộng B12 tìm ra khi sửa nốt bốn lỗi
cuối.)

Mỗi lỗi đã sửa đều có phép thử đi kèm, **chạy đỏ trước khi sửa và xanh sau khi sửa**. Sáu phép thử
dạng quét mã nguồn chặn cả lớp lỗi quay lại thay vì chỉ chặn một chỗ:

| Phép thử | Luật |
|---|---|
| `frontend-admin/src/api/download.test.ts` | Ngoài `src/api`, không viết địa chỉ bắt đầu bằng `/api/` |
| `frontend-opac/src/lib/marcView.test.ts` | Không `JSON.stringify` biểu ghi MARC ra trang công khai |
| `frontend-admin/src/lib/datetime.test.ts` | Giao diện quản trị không tự viết cách hiện ngày riêng |
| `frontend-opac/src/lib/datetime.test.ts` | Trang tra cứu cũng vậy — thêm sau khi tìm ra B12 |
| `frontend-admin/src/lib/columnLabels.test.ts` | Không đặt tên cột đúng một chữ "Giá" — tối nghĩa |
| `backend/.../PermissionAndAuditTests.cs` | Thông báo lỗi không lọt tiếng Anh của khung nền |

### Việc đã làm ngoài mã nguồn

- **Migration dọn dữ liệu cũ** cho A7, A8, D1, A2: thư viện nào đã chạy bản trước đều mang sẵn biểu
  ghi kẹt ngoài hàng đợi, nhật ký thu hoạch kẹt "Đang chạy" và có thể có phiếu mượn trùng. Trên
  chính máy đang chạy: 7.468 biểu ghi vào hàng đợi, 15 dòng nhật ký được đóng lại.
- **Duyệt hàng loạt** ở hàng đợi biên mục — chức năng mới, vì một lượt thu hoạch đưa về hàng nghìn
  biểu ghi cùng nguồn mà bắt duyệt từng cái thì không ai dùng. Nhờ đó **7.674 biểu ghi thật** đã lên
  trang tra cứu.
- **Kiểm chứng bộ dữ liệu mẫu trên một cơ sở dữ liệu mới tinh** (không phải trên máy đang chạy, vì
  máy đang chạy đã có dữ liệu nên bộ gieo không chạy lại): tên thư viện, 6 bản tin, 10 bộ sưu tập,
  200 biểu ghi được gắn bộ sưu tập, 50 bạn đọc với 50 tên khác nhau.

### Bốn lỗi cuối — đã sửa và đã kiểm thế nào

| # | Đã làm | Bằng chứng |
|---|---|---|
| A4 | Máy chủ Z39.50 báo có kết quả nhưng không trả biểu ghi thì hệ thống lấy lại cùng truy vấn ấy qua lối SRU của chính thư viện đó, kèm một câu nói rõ đã đi lối nào. Lối SRU khai ngay trên dòng máy chủ Z39.50 (ô "Địa chỉ SRU dự phòng"); migration nối sẵn cho thư viện nào đã khai hai lối thành hai dòng riêng | Bốn phép thử đơn vị cho phần quyết định chuyển lối. Kiểm trên máy thật: dựng một dòng máy chủ đòi cú pháp biểu ghi mà Thư viện Quốc hội Mỹ không phát, bước Present bị từ chối đúng như A4 → hệ thống trả về 10 biểu ghi qua lối SRU kèm câu *"Máy chủ Z39.50 báo có 11.528 kết quả nhưng không trả biểu ghi nào, nên hệ thống đã lấy qua lối SRU của cùng thư viện."* Lưu ý: đúng lúc kiểm thì lối Z39.50 thật của họ lại chạy được, nên phải dựng tình huống mới tái hiện |
| A6 | Ghi thẻ đọc tiếp vào kho sau **mỗi trang**, không đợi tới cuối lượt — cái cần chống chính là lượt chết giữa chừng. Lượt sau nối tiếp từ đó; thẻ đã hết hạn thì quét lại từ đầu chứ không kẹt vĩnh viễn. Lỗi TLS, lỗi tên máy, lỗi cổng nay được nói đúng nguyên nhân bằng tiếng Việt | Ba phép thử tích hợp chạy trên PostgreSQL thật với một máy chủ OAI-PMH giả dựng trong bộ nhớ — chỉ dựng được mới ép nó đứt đúng ở trang thứ hai. Màn hình danh sách kho hiện nhãn "Còn dở dang" cho kho nào đang dừng giữa chừng |
| B10 | Dòng nhắc "Còn cột bên phải — cuộn ngang trong bảng để xem hết", dải mờ ở mép phải, và neo cột tên báo lại để cuộn sang phải vẫn biết đang đọc dòng nào. Chỉ nhắc khi bảng thật sự còn phần chưa xem — đo bằng `ResizeObserver`, cuộn tới hết thì nhắc tự tắt | Ảnh chụp màn hình 1440×900 trên hệ thống đang chạy: dòng nhắc hiện, thanh cuộn hiện, mép phải mờ dần |
| C5 | Chữ "Giá" đứng một mình bị bỏ hẳn. Cột và ô nhập chỉ cái giá xếp sách đổi thành "Vị trí giá" (8 chỗ), cột tiền trong báo cáo kiểm kê đổi thành "Đơn giá". Ô trống nay ghi "Chưa xếp" thay vì để trắng | Phép thử quét mã nguồn `columnLabels.test.ts` — chạy đỏ với 9 chỗ vi phạm trước khi sửa, trong đó có 5 chỗ mà đợt rà đầu chưa nhìn thấy |

### Tình hình sửa — đợt rà thứ ba

| Mức độ | Tổng | Đã sửa | Còn lại |
|---|---|---|---|
| Nghiêm trọng | 2 | 2 | 0 |
| Nặng | 2 | 2 | 0 |
| Vừa | 2 | 2 | 0 |
| Thiếu chức năng | 1 | 0 | 1 (H3, ghi ở "Làm tiếp") |
| Nguy cơ | 1 | 0 | 1 (H9, ghi ở "Làm tiếp") |

Cộng cả ba đợt và đợt áp thiết kế: **57 lỗi, đã sửa 55**, còn hai mục là thiếu chức năng và nguy cơ
đã ghi rõ chỗ. Mỗi lỗi đã sửa của đợt này đều có phép thử chạy đỏ trước khi sửa và xanh sau khi sửa,
kể cả H7: phép thử giả tiêu đề đỏ trước khi sửa `CurrentUser.Ip`.

### Làm tiếp gì sau đây

1. **Trình soạn MARC còn thiếu so với đặc tả II.2** (H3): wizard 008, kéo thả sắp xếp trường, Ctrl+D
   nhân bản dòng, nút "Lấy từ Z39.50 / ISBN" trên biểu mẫu, xem trước ISBD trước khi lưu.
2. **Sao lưu thủ công xếp vào Hangfire** (H9): chạy trong lượt HTTP là đúng lớp lỗi số 4; màn hình
   đọc trạng thái từ bảng `backup_jobs` như kho OAI-PMH đang làm.
3. **Đợt sau là ứng dụng di động** (Phase 15) theo `PROMPT-MOBILE-LIBRARYCONNECT.md`. Nhóm
   `/api/reader/*` đã kiểm lại trong đợt này qua lối bạn đọc thật (đọc tài liệu số có chữ chìm).
4. Bài học giữ lại từ ba đợt: **lỗi ghi là "đã sửa" chưa chắc đã sửa hết** (B12 dưới D8, H5 dưới lần
   sửa bỏ dấu), và **phép thử tự viết chỉ chạm tới bối cảnh người viết nghĩ ra** — H4 sống từ phase 5
   vì phép thử sửa biểu ghi chưa bao giờ thêm một điểm truy cập; H5 sống qua một lần sửa vì phép thử
   dùng kho trống. Dựng đúng bối cảnh của kho thật (220 tác giả cùng họ) mới bắt được.
