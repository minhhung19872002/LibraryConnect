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

### Làm tiếp gì sau đây

1. Dựng bộ dữ liệu trình diễn lớn (`LC_SEED_DEMO=rich`): sinh ĐKCB cho khoảng 60% biểu ghi đã thu
   hoạch, 300 bạn đọc, 1.500 lượt mượn trải 18 tháng, 50 lượt đặt giữ, 30 tài liệu số giấy phép mở,
   ảnh bìa lấy từ Open Library theo ISBN.
2. Rà lại một lượt nữa theo đúng cách của đợt này — lần rà đầu tìm ra 36 lỗi, phần lớn chỉ lộ ra khi
   có dữ liệu thật; nay kho đã có hơn bảy nghìn biểu ghi thì nhiều màn hình khác cũng đáng xem lại.
   Ưu tiên những chỗ đợt trước **chưa đi tới**: trình soạn MARC thao tác bằng chuột, thiết kế mẫu
   phích / mẫu thẻ / mẫu tem, luồng kiểm kê từ đầu đến cuối, đóng tập ấn phẩm định kỳ, trình đọc tài
   liệu số có đóng dấu chìm, và sao lưu – phục hồi.
3. Bài học của lượt sửa này, đáng nhớ cho lần rà sau: **lỗi ghi là "đã sửa" chưa chắc đã sửa hết**.
   B12 nằm ngay dưới D8 đã đóng — cùng một lỗi, chỉ khác thư mục. Phép thử quét mã nguồn của D8 chỉ
   quét `frontend-admin` nên không ai thấy. Sửa xong một lớp lỗi thì phải hỏi lại: luật này còn chỗ
   nào chưa được quét?
