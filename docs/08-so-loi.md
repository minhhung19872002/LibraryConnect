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
| H3 | Trình soạn MARC | Thiếu so với đặc tả II.2: wizard nhập 008 (đang là ô 40 ký tự thô), kéo thả sắp xếp trường, Ctrl+D nhân bản dòng, nút "Lấy từ Z39.50 / Lấy từ ISBN" ngay trên biểu mẫu, xem trước ISBD *trước khi lưu*. Bảng đáp ứng `docs/07` không khai các mục này nên không phải khai gian, nhưng đặc tả gốc có | So II.2 của CLAUDE.md với màn hình | Thiếu chức năng | Đặc tả | **Đã làm** (đợt sửa 03/09/2026): trình hướng dẫn 008 theo vị trí, nhân bản trường bằng nút và Ctrl+D, sắp xếp lại bằng kéo thả kèm nút lên/xuống cho bàn phím, endpoint `POST /api/marc/preview` dựng ISBD cho biểu ghi chưa lưu, hộp thoại lấy biểu ghi Z39.50 / theo ISBN ngay trên trình soạn |
| H4 | Sửa biểu ghi (`PUT /api/cataloging/bibs/{id}`) | Thêm **bất kỳ** điểm truy cập mới — tác giả, đề mục, từ khóa, phân loại — vào biểu ghi đã có là **409** "Không lưu được dữ liệu. Vui lòng kiểm tra lại thông tin nhập." Ghi SQL của PostgreSQL ra mới thấy: liên kết mới chỉ được `collection.Add` vào navigation của một biểu ghi đang *Unchanged*, Entity Framework thấy khoá đã có giá trị nên coi nó là dòng có sẵn và phát `UPDATE bib_authors … WHERE id = <mới>` với `created_at = '-infinity'` — 0 dòng bị ảnh hưởng, `DbUpdateConcurrencyException`. Với biểu ghi *mới* thì cha đang Added nên con cũng Added, vì thế đường tạo mới và mọi phép thử cũ đều xanh. Kết hợp H5 thì **6.781 trên 7.680 biểu ghi có trường 653 không sửa được** | Sửa biểu ghi tối giản, thêm `100$a` một tên mới → Lưu | **Nghiêm trọng** | Nghiệp vụ | Đã sửa — khai thẳng `Set<TLink>().Add(link)` với bộ theo dõi; phép thử tích hợp thêm tác giả và từ khóa vào biểu ghi đã có |
| H5 | Bộ đối chiếu thẩm quyền | Lọc sơ bộ theo **từ đầu tiên** rồi `.Take(200)`: "Nguyễn" khớp 3.060 tác giả trên kho thật, tên cần tìm nằm ngoài 200 dòng đầu, và ngay **lần lưu thứ hai của cùng một biểu ghi** đã tạo thêm `NGUYEN_VAN_KIEM_2`. Đo kho trước khi sửa: 485 nhóm tác giả trùng (616 dòng thừa), 109 nhóm từ khóa trùng (146 dòng thừa) — đúng thứ mà tệp thẩm quyền sinh ra để tránh | Lưu hai biểu ghi cùng tác giả họ Nguyễn → Danh mục → Tác giả | Nặng | Dữ liệu | Đã sửa — hàm `cat.lc_name_key` tính cùng khoá với `NormaliseForComparison`, chỉ mục hàm trên năm bảng thẩm quyền, so khớp ngay trong SQL, không còn ngưỡng; migration gộp **790 tác giả và 156 từ khóa** trùng, trỏ lại liên kết |
| H6 | Trình thiết kế phích + 5 màn hình | Đợt thay 130 màu (`58326b0`) đổi `'#e3d9c7'` thành `'${MAU.vien}'` mà giữ **dấu nháy đơn**: trình duyệt nhận nguyên văn `1px solid ${MAU.vien}`, coi là CSS sai và bỏ. Đo: khung phích 400 × 240 px có `border: 0px none`, nền giấy trên nền giấy — kéo ô ra ngoài mép cũng không thấy mép đâu. 11 chỗ ở 6 tệp: khung, lề và ô đang chọn của trình thiết kế phích, viền ảnh bìa, **viền đỏ ô tủ quá giờ**, viền khung soạn thảo tin (làm hỏng lại G5 một nửa), xem trước logo, khung định nghĩa trường MARC; hai tệp còn chưa `import MAU`. Phép thử cấm mã màu thẳng vẫn xanh vì không còn mã màu để bắt | Biên mục → Mẫu phích → Thêm mẫu, đo viền khung | Vừa | Giao diện (tự gây) | Đã sửa — dấu huyền và import; luật quét mới ở cả hai gói, đỏ trước xanh sau |
| H7 | Toàn hệ thống — hạ tầng | Hai nửa của một lỗi. **(a)** `CurrentUser.Ip` lấy thẳng giá trị **đầu tiên** của tiêu đề `X-Forwarded-For` — chữ do người gọi tự viết. Thử thật: gửi từ một container khác qua Nginx kèm `X-Forwarded-For: 203.0.113.9` → bảng `login_histories` ghi đúng `203.0.113.9`. Nghĩa là nhật ký hệ thống (I.4), lịch sử đăng nhập và chữ chìm trên tài liệu số đều **giả được** bằng một dòng tiêu đề. **(b)** Bộ trung gian ForwardedHeaders mặc định chỉ tin proxy ở loopback, mà Nginx trong Docker đứng ở `192.168.0.x`, nên `RemoteIpAddress` — thứ bộ giới hạn tốc độ dùng để chia ngăn — là địa chỉ của Nginx cho mọi yêu cầu: **toàn bộ bạn đọc chung một ngăn 300 yêu cầu/phút**, toàn bộ cán bộ chung 20 lượt đăng nhập/phút; 200 người dùng đồng thời (mục 6.3) là 429 hàng loạt. Lúc đầu tưởng 96.737 dòng nhật ký cùng một IP là bằng chứng của (b) — không phải: trên máy phát triển mọi yêu cầu đều từ một máy, IP ấy là địa chỉ cổng Docker của chính máy này. Bằng chứng thật là phép thử giả tiêu đề và mã nguồn của bộ giới hạn | Từ container khác: `curl -H 'X-Forwarded-For: 203.0.113.9' http://nginx/api/auth/login` rồi đọc `sys.login_histories` | **Nghiêm trọng** | Hạ tầng | Đã sửa — `ForwardedHeadersSetup` tin ba dải RFC 1918 và loopback với `ForwardLimit = 1`, khai được `LC_Proxy__TrustedNetworks`; `CurrentUser.Ip` chỉ đọc `RemoteIpAddress` đã qua bộ trung gian. Kiểm lại từ container khác qua Nginx: ghi đúng địa chỉ của container (`192.168.0.9`), tiêu đề bịa bị bỏ. Gọi thẳng cổng API từ máy chủ Docker thì tiêu đề vẫn được tin — đúng thiết kế, vì máy chủ ấy nằm trong dải được tin; bản triển khai thật không mở cổng API ra ngoài (`ports: !override []` trong `docker-compose.prod.yml`) |

### Nguy cơ ghi nhận, chưa tái hiện được

| # | Chỗ | Nguy cơ | Đo được gì |
|---|---|---|---|
| H9 | Sao lưu thủ công | Chạy **ngay trong lượt HTTP** — đúng lớp lỗi số 4 của CLAUDE.md. Kho lớn kèm kho đối tượng vài GB là vượt 300 giây của proxy, việc bị bỏ dở, dòng nhật ký kẹt "Đang chạy" | 4,2 giây cho bản dump 40 MB không kèm MinIO; trên máy này không dựng được kho đủ lớn để chạm ngưỡng. **Đã làm** (đợt sửa 03/09/2026): `BackupRunner` xếp việc vào Hangfire, khoá chống chạy trùng, quét đóng lượt treo quá 6 giờ, màn hình tự hỏi lại khi còn việc đang chạy. Ba phép thử tích hợp, đỏ trước khi sửa |

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
| I5 | Thiếu chức năng | Ứng dụng **chưa từng được biên dịch cho iOS**; lượt dựng đầu tiên đổ ở `connectivity_plus` 7.3.1: `Value of type 'NWPath' has no member 'isUltraConstrained'` | Máy phát triển chạy Windows nên không có Xcode; suốt phase 15 chỉ dựng Android. `isUltraConstrained` là API của SDK iOS mới nhất, Xcode mặc định của ảnh máy chủ cũ hơn. Kèm theo: Podfile tự viết làm Flutter 3.47 rẽ sang CocoaPods trong khi mọi gói đã có bản Swift Package. | Thêm `.github/workflows/ios.yml` chạy trên máy Mac của GitHub: dựng bản phát hành không ký rồi chạy trên iPhone Simulator với máy chủ thật, chọn Xcode cao nhất của máy chủ, bỏ Podfile, nâng ngưỡng iOS tối thiểu lên 15.5 (`mobile_scanner` 7 và Firebase iOS SDK 12 đòi). Ba kịch bản MB.34–MB.36 đạt, 12 ảnh chụp trong `docs/images/mobile/ios-*`. |
| I6 | Vừa | Phép thử iOS chạy nền chập chờn: cùng một mã, lượt xanh lượt đỏ; một lượt đăng nhập được (máy chủ ghi 200) rồi đứng im tới khi hết giờ 60 phút | Ba nguyên nhân chồng nhau: (1) cùng một lần đẩy mã chạy cả workflow Deploy, nó khởi động lại `lc-api` **đúng lúc** phép thử đang gọi; (2) `scrollUntilVisible` đòi đúng một vùng cuộn trong cây, màn hình có hai thì ném "Too many elements"; (3) `find.text(x).last` ném "No element" ngay khi chưa có, nên vòng chờ không kịp chạy. | Thêm bước chờ `/api/public/settings` trả 200 trước khi kiểm; đặt hạn 25 phút cho bước chạy máy ảo và ghi nhật ký ra tệp rồi tải lên artifact (bước bị cắt thì phần đuôi nhật ký mất, mà đúng phần ấy mới nói được chỗ kẹt); chờ bằng finder thường rồi mới lấy `.last`. |
| I7 | Nhẹ | Chỉnh **chủ đề và cỡ chữ nằm sau bức tường đăng nhập** — khách không đổi được | Thẻ Tài khoản nằm trong `_protected` của bộ định tuyến, mà hai tuỳ chọn ấy chỉ có ở đó. Phát hiện khi viết phép thử iOS: khách bấm Tài khoản bị đưa sang trang đăng nhập. | **Đã sửa** (đợt sửa 03/09/2026): bỏ thẻ Tài khoản khỏi danh sách route cần đăng nhập — bản thân màn hình đã có nhánh cho khách từ đầu. Phép thử `mobile/test/core/router_test.dart` giữ cho nó không bị xếp lại vào nhóm ấy. |
| I8 | Nặng | **Phục hồi cơ sở dữ liệu cũng chạy trong lượt HTTP** — cùng lớp lỗi số 4 với H9, và tệ hơn: `pg_restore` một kho vài GB chắc chắn vượt 300 giây của proxy, người bấm mất kết nối giữa chừng mà không biết hệ thống còn dùng được hay không | Tìm ra khi sửa H9. Đi sâu thêm thì lộ hai chuyện nữa: bản `pg_dump` ôm luôn schema `hangfire`, nên (a) phục hồi bản hôm qua làm **sống lại hàng đợi việc của hôm qua** và chạy lại những việc đã chạy rồi, (b) lượt phục hồi không thể tự chạy trong Hangfire vì nó xoá đúng bảng đang ghi nhận chính nó. Và mọi chỗ ghi tiến độ trong cơ sở dữ liệu đều bị chính lượt phục hồi xoá mất. | Loại schema `hangfire` khỏi cả `pg_dump` lẫn `pg_restore`; lượt phục hồi xếp vào Hangfire; tiến độ ghi ở **bộ nhớ đệm** — dịch vụ riêng, lượt phục hồi không đụng tới; hộp thoại theo dõi tại chỗ thay vì đứng chờ, không đóng được khi đang chạy. Bốn phép thử tích hợp và bốn phép thử đơn vị cho dòng lệnh; hai trong số ấy đỏ trước khi sửa (bỏ tạm cờ loại trừ để xác nhận). |

| I9 | Vừa | Ứng dụng di động: **mượn tự phục vụ xong, "Xem Sách của tôi" không có cuốn vừa mượn**; đặt giữ từ chi tiết tài liệu xong, thẻ Đặt giữ cũng không có dòng mới — phải kéo xuống làm mới | `currentLoansProvider` và `holdsProvider` là `autoDispose`: chỉ tự nạp lại khi thẻ chứa nó đã bị huỷ. Bạn đọc đứng sẵn ở thẻ Đang mượn rồi bấm nút mượn, quay về thì thẻ còn sống, provider còn giá trị cũ. Màn hình tự mượn và chi tiết tài liệu không `invalidate` sau khi ghi. Lộ ra ở lượt iOS 33828688766 (đỏ: không thấy nút Gia hạn); Android không bắt được vì phép thử đứng ở thẻ khác trước khi mượn. | Hai màn hình `ref.invalidate(...)` ngay sau khi máy chủ nhận. Phép thử quét `mobile/test/features/list_refresh_after_write_test.dart`: màn hình nào gọi `checkout`/`renewLoan`/`createHold`/`cancelHold` thì phải làm mới đúng provider — đỏ ở hai tệp trước khi sửa, xanh sau; lượt iOS 33830593977 thấy nút Gia hạn. |
| I10 | Nhẹ | Ứng dụng di động, iPhone SE (375×667): **bước quét của màn hình tự mượn tràn 2 điểm ảnh ở đáy** khi hộp thoại nhập mã bằng tay mở bàn phím | `Scaffold` co thân lại tránh bàn phím (mặc định); thân còn 332 điểm mà dải xác thực hai dòng + khung quét 220 + dòng gợi ý đã 334. Bắt được vì bộ kiểm thử coi mọi lỗi khung là bài đỏ (lượt 33834562537), nhưng báo cáo cuối bài chỉ còn "DEFUNCT" — phải ghi lỗi ngay lúc ném (lượt 33836450263) mới biết cột nào. | `resizeToAvoidBottomInset: false` cho màn hình ấy: ô nhập tay nằm trong hộp thoại riêng, tự lo bàn phím của nó. Xanh ở lượt 33838388725. |

Bài học thứ hai của đợt: **máy ảo mặc định không phải điện thoại của người dùng.** Cỡ chữ lớn đã soi
ở phase 15 nhưng ở bề rộng 411dp; máy 360dp với cỡ chữ 1,3 là một cấu hình khác hẳn, và đó là máy
thật của người dùng đầu tiên. Từ nay soi ở `wm density 480` + `font_scale 1.3` trước khi phát hành.

Bài học thứ ba: **một nền tảng chưa từng dựng thì mọi khẳng định về nó là phỏng đoán.** Bảng đáp ứng
ghi "iOS: mã dùng chung, chưa dựng vì thiếu máy Mac" — nghe hợp lý, nhưng lần dựng đầu cho thấy mã
ấy **không biên dịch được** trên iOS. Máy Mac của GitHub Actions là miễn phí với kho mã công khai;
đáng ra phải dùng từ phase 15 chứ không phải sau khi bàn giao.


Bài học: **"build sạch" trên máy phát triển không chứng minh kho mã đầy đủ.** Thư mục làm việc luôn
có mọi tệp; chỉ một bản clone mới lộ ra tệp nào chưa vào Git. Chưa có CI dựng từ clone sạch, nên lỗi
này sống qua 14 phase. Việc nên làm: một bước CI `git clone` + `docker compose build`.

Cùng đợt còn thêm lớp compose thứ ba `docker-compose.behind-proxy.yml` cho máy chủ đã có proxy giữ
cổng 80/443 (mục 5.1b của `docs/04`), vì `nginx.prod.conf` giả định LibraryConnect là ứng dụng duy
nhất trên máy.

## J. Đợt rà hoàn thiện (04/09/2026) — đối chiếu từng gạch đầu dòng của đặc tả vào mã

Sau khi iOS xong, rà lại **toàn bộ** đặc tả mục 5 (11 phân hệ), mục 6 (phi chức năng), mục 7–8 và
PROMPT-MOBILE theo cách khác ba đợt trước: năm nhóm rà độc lập, mỗi nhóm cầm một đoạn đặc tả, với
**từng ý nhỏ trong từng gạch đầu dòng** phải chỉ ra endpoint + handler + màn hình gọi tới, và kết luận
Đủ / Một phần / Thiếu. Bảng đáp ứng `docs/07` tự khai "Có" gần hết; đối chiếu vào mã thì lộ ra một
mô-típ lặp đi lặp lại: **backend đã làm xong nhưng giao diện không có nút nào gọi tới** (in phiếu
chuyển kho, quyết định thanh lý, cấp lại thẻ, đồng bộ đào tạo, quản lý tủ, mẫu biên mục…), và vài
chỗ **"hứa mà không làm"** nghiêm trọng hơn. Các dòng dưới đây là phần xuyên suốt backend; các nhóm
phân hệ ghi tiếp J-số theo nhóm.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| J1 | Nghiêm trọng | **Phạm vi dữ liệu (kho / thư viện / dạng tài liệu) không được cưỡng chế ở đâu cả.** Cán bộ gán kho A vẫn thấy, sửa và cho mượn ĐKCB kho B ở mọi màn hình; `docs/07` dòng A7 còn ghi "được máy chủ áp bằng bộ lọc toàn cục của EF Core" — sai | `UserDataScope` được lưu ở màn hình Người dùng và trả về lúc đăng nhập; `ICurrentUser.IsInScope`/`ScopeIds` có cài đặt nhưng **không nơi nào gọi**; bộ lọc toàn cục duy nhất là xoá mềm. Kiểm thử 2.3 E-HSMT tạo tài khoản giới hạn kho sẽ trượt | `DataScopeMiddleware` điền `IDataScopeContext` sau xác thực (gán thư viện → mọi kho của nó; gán kho → thư viện chứa nó; đệm cùng quyền), `LibraryConnectDbContext` dựng bộ lọc toàn cục cho ĐKCB, giá, kỳ kiểm kê, kho, thư viện và (theo dạng tài liệu) biểu ghi. `DataScopeTests` (2 bài) đỏ trước, xanh sau. **Không** lọc phiếu mượn qua điều hướng `Loan→Item`: EF đổi LEFT JOIN Fine→Loan thành INNER, 5 phép thử tiền phạt đỏ — ghi lại làm bài học |
| J2 | Nặng | **Sửa tài liệu số là mất liên kết biểu ghi và phần mô tả.** Form "Sửa" chỉ gửi bảy trường; handler gán `BibId`/`Description` vô điều kiện | Không có ô chọn biểu ghi trên form nên chưa ai gắn biểu ghi bằng tay để thấy nó mất; import ZIP tự khớp thì gắn được, rồi lần Lưu đầu tiên là mất | Luật mới: `null` = giữ nguyên, bỏ liên kết phải nói rõ `clearBibId`, xoá mô tả gửi chuỗi rỗng; form có ô Mô tả và ô chọn biểu ghi (`BibSearchSelect` dùng chung). `DigitalTests.Sua_tai_lieu_khong_gui_bieu_ghi…` đỏ trước, xanh sau |
| J3 | Nặng | **Cán bộ quầy không in được phiếu mượn / phiếu trả / biên lai phạt** — nhận 403 dù nút hiện ra | Endpoint in biểu mẫu dùng chung gắn quyền `ACQ.ORDER.PRINT` của phân hệ Bổ sung cho cả mười loại mẫu | Quyền theo loại mẫu (`FormTypes.PermissionsToPrint`): phiếu mượn/trả theo quyền lưu thông, biên lai theo thu phạt, giấy xác nhận theo xem hồ sơ, phiếu chuyển kho / quyết định thanh lý / biên bản kiểm kê theo quyền kho. `PermissionAndAuditTests.Can_bo_luu_thong_in_duoc_phieu_muon…` đỏ trước, xanh sau |
| J4 | Nặng | **"Sao lưu kèm tệp tài liệu số" chỉ ghi một README** nêu tên bucket và tổng dung lượng rồi trả 0 — giao diện vẫn có ô tuỳ chọn ấy | Hàm `MirrorObjectStorageAsync` được viết như chỗ giữ chỗ từ phase 2 và không ai quay lại; phép thử sao lưu chỉ kiểm "có người chạy job" | `IObjectStorageMirror` liệt kê và tải mọi object của hai bucket tài liệu + ảnh về `<bản sao lưu>-files/<bucket>/<tên object>`, chặn tên đi ra ngoài thư mục, README ghi cách phục hồi bằng `mc mirror`. `BackupTests.Sao_luu_kem_tep…` (thử thẳng bộ chép vì máy kiểm thử không có pg_dump) đỏ trước, xanh sau |
| J5 | Vừa | **Cửa đăng nhập bạn đọc không có giới hạn tốc độ** trong khi cửa cán bộ có; số thẻ dễ đoán (TV2026000001, 2, 3…) | `[EnableRateLimiting("login")]` chỉ gắn ở `AuthController` | Gắn cùng chính sách cho `POST /api/reader/auth/login`; `RateLimitTests.Cua_dang_nhap_ban_doc…` đỏ trước, xanh sau |
| J6 | Vừa | **Hạn đổi mật khẩu không có tác dụng**: `SECURITY.PASSWORD_EXPIRY_DAYS` được seed, đọc vào `PasswordPolicy.ExpiryDays`, hiện trên màn hình Tham số, mà không nơi nào dùng | Cột `password_changed_at` có từ phase 2 và được ghi khi đổi mật khẩu, chỉ thiếu đúng chỗ đọc | Lúc đăng nhập, quá hạn thì phiên mang cờ buộc đổi và bộ trung gian `PasswordChangeRequired` chặn mọi việc khác — đúng đường của mật khẩu tạm. `PasswordExpiryTests` đỏ trước, xanh sau |
| J7 | Nhẹ | `/health/ready` không hỏi MinIO: kho đối tượng chết mà điểm sẵn sàng vẫn xanh | Chỉ có kiểm tra PostgreSQL và Redis | `MinioHealthCheck` (BucketExists, hạn 5 giây) gắn thẻ `ready`; `InstallationTests` đòi "minio" trong thân trả về, đỏ trước |
| J8 | Nhẹ | Cache Redis cho **danh mục** và **kết quả tra cứu** chỉ có tiền tố khai sẵn: lệnh ghi danh mục xoá tiền tố `catalog:` mà không ai từng ghi vào; `search:` không dùng ở đâu | Cache chỉ được nối cho tham số hệ thống, quyền và bộ trường MARC | Danh sách + cây danh mục đệm 10 phút (xoá theo tiền tố khi ghi), hai trang đầu tra cứu OPAC đệm 60 giây (không đệm lượt `updatedSince`). `CatalogTests.Danh_muc_duoc_dem_trong_cache…` đỏ trước |
| J9 | Nhẹ | Trang SPA công khai **không có CSP, không có HSTS**; API trả **hai dòng X-Frame-Options mâu thuẫn** (DENY của API + SAMEORIGIN của nginx) | Tiêu đề bảo mật chỉ đặt ở `SecurityHeadersMiddleware` của API; nginx thêm bộ của nó lên mọi phản hồi | nginx đặt CSP riêng cho hai SPA (`script-src 'self'`, phông Google, iframe YouTube/Vimeo/Drive/Maps), HSTS ở bản sau proxy, và `proxy_hide_header` bộ trùng của API. Kiểm bằng `curl -D` và mở hai SPA trong Chrome: 0 lỗi CSP ở console |
| J10 | Vừa | **Không client Z39.50 nào bên ngoài kết nối được** sau `docker compose up`: cổng 210 không được công bố, máy chủ Z39.50 mặc định tắt, `.env.example` không có dòng nào | Máy chủ Z39.50 chạy trong tiến trình API (được phép theo đặc tả) nhưng compose chỉ mở 8080; tiến trình không chạy root nên không mở được cổng dưới 1024 | Compose công bố `${Z3950_PORT:-210}:2100`, seed bật máy chủ mặc định cho bản cài mới, `.env.example` có `Z3950_PORT`; bản cài cũ bật bằng tham số `ILL.Z3950_SERVER_ENABLED` |

Bài học của đợt: **bảng đáp ứng tự khai không phải bằng chứng.** Dòng A7 ghi "áp bằng bộ lọc toàn
cục" từ phase 1 mà bộ lọc ấy chưa từng tồn tại; không ai kiểm lại vì mọi phép thử đều chạy bằng tài
khoản quản trị không bị giới hạn. Muốn tin một dòng "Có" thì phải có một phép thử dựng đúng bối cảnh
của người bị giới hạn — và đợt này thêm đúng những phép thử ấy.

---

### J.C — Phân hệ I và II (quản trị hệ thống, biên mục)

Đi lại đặc tả Phân hệ I (Quản trị) và II (Biên mục) từng dòng, đối chiếu với thứ đang có trên màn
hình và trong mã. Lỗi thật ghi ở bảng này; chức năng còn thiếu so với đặc tả thì làm thẳng và ghi ở
`docs/06`/`docs/07`.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JC1 | Nặng | Nhập biểu ghi từ Excel chọn **"Gộp" mà lại ghi đè**: biểu ghi có tóm tắt 520$a viết tay, nhập bảng tính chỉ có nhan đề + ISBN với lựa chọn Gộp thì tóm tắt biến mất | `BibExcelImportRunner` không có nhánh `Merge`; mọi lựa chọn không phải Bỏ qua/Tạo mới đều rơi vào nhánh nạp biểu ghi cũ rồi ghi đè bằng biểu ghi dựng từ dòng Excel. Bộ nhập ISO 2709 có `MergeInto` riêng nên làm đúng — hai bộ nhập, hai cách hiểu một chữ trên màn hình. | Tách `MergeInto` thành `MarcMerge` dùng chung, bộ Excel gọi nó khi `OnDuplicate = Merge`. Phép thử tích hợp `ExcelImportTests.Chon_gop_thi_truong_bieu_ghi_cu_dang_co_van_con_nguyen` đỏ trước (Abstract = null), xanh sau. |
| JC2 | Vừa | Ở màn hình Danh mục tự tạo (II.9), bấm vào **số biểu ghi** của một giá trị thì ra trang danh sách biểu ghi **đầy đủ, không lọc gì** — đúng thứ đặc tả gọi là "dùng làm bộ lọc trong tra cứu" thì không có | `CustomIndexPage` dẫn sang `/bien-muc?customIndexValueId=…` từ phase 5, nhưng `BibListPage` chưa bao giờ đọc chuỗi truy vấn; máy chủ đã lọc được theo `customIndexValueId` từ đầu. Hai đầu làm xong, không ai nối. | `bibListFilters.ts` dựng bộ lọc từ địa chỉ (mã định danh phải là GUID, năm phải là số), trang hiện thẻ "Đang lọc theo liên kết: Danh mục tự tạo: Hà Nội" có nút bỏ; màn hình danh mục gửi kèm nhãn. Phép thử `bibListFilters.test.ts`: 5 phép cho bộ dựng, 2 phép quét trang đỏ trước khi sửa. |

### J.B — Phân hệ III và IV (bổ sung, ấn phẩm định kỳ)

Đợt này không mở hệ thống ra tìm lỗi mà **đặt đặc tả Phân hệ III, IV cạnh sản phẩm và dò từng
dòng**: dòng nào bảng đáp ứng ghi "Có" mà đường đi thật của cán bộ không tới được thì ghi là lỗi.
Phần lớn là chức năng máy chủ đã có mà giao diện chưa mở lối tới, hoặc mở tới nhưng tính sai.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JB1 | Thiếu chức năng | Chuyển kho và thanh lý xong **không có chỗ nào để in** phiếu chuyển kho hay quyết định thanh lý; endpoint in đã có từ phase 6 nhưng chỉ gọi được khi biết số phiếu và gõ tay địa chỉ | Màn hình kho chỉ hiện số phiếu trong một toast rồi thôi; danh sách phiếu (`GET /stock/transfers`) có API mà không có màn hình | Hộp mời in ngay sau thao tác; nút "In quyết định" ở chi tiết bản đã thanh lý; ngăn "Phiếu chuyển kho" liệt kê mọi phiếu kèm in lại. Phép thử `printing.test.ts` (đỏ trước khi có module) và hai phép thử tích hợp mở rộng |
| JB2 | Nghiêm trọng | **Yêu cầu đặt báo tính tiền như mua sách**: tạp chí tháng đặt trọn năm ra tiền của một số. Bảng đáp ứng ghi "tổng tiền tự tính" nhưng công thức là số bản × đơn giá, không nhân số kỳ; giá trị duyệt cũng vậy | Cột ISSN, kỳ hạn, thời gian đặt có ở thực thể từ phase 6 nhưng form không hiện, máy chủ không dùng | `SerialSubscription.IssueCount` đếm kỳ theo tháng đặt và số kỳ/năm; thành tiền = số bản × số kỳ × đơn giá kỳ ở cả lưu lẫn duyệt; form đổi cột khi chọn loại Ấn phẩm định kỳ, hiện số kỳ và thành tiền khi gõ. 7 phép thử đơn vị, 5 vitest, phép thử tích hợp `Serial_request_prices…` (đỏ: 50.000 thay vì 600.000). Còn để lại: đơn đặt sinh từ yêu cầu đặt báo vẫn lấy đơn giá/kỳ làm đơn giá dòng đơn, chưa nhân số kỳ — ghi ở "Làm tiếp" |
| JB3 | Thiếu chức năng | **IV.3 Bổ sung tổng thể không có màn hình**: bảng đáp ứng ghi "Có" nhưng ghi nhận hàng loạt chỉ làm được trong bàn làm việc của *một* đầu báo; nhận một chồng báo hai chục đầu là mở hai chục ngăn | `SerialIssueListRequest.DueOnly` có sẵn mà không màn hình nào gọi không kèm `serialId` | Màn hình `/an-pham-dinh-ky/bo-sung-tong-the` ba tab: số đến hạn mọi đầu báo (số lượng, ngày nhận từng dòng), đối chiếu số thiếu gom theo đầu báo + khiếu nại đa đầu báo, sinh số nhiều đầu báo. Bộ lọc `unresolvedOnly` mới (quá hạn ∪ thiếu ∪ đang khiếu nại). Phép thử tích hợp `Batch_receiving…` đỏ trước (bộ lọc chưa có nên trả cả số đã nhận), 4 vitest cho hai hàm gom |
| JB4 | Nặng | **Đóng tập "theo khoảng số" đóng cả năm**: `FromIssue`/`ToIssue` được nhận vào lệnh nhưng chỉ dùng làm nhãn in trên tập; chọn 1–2 vẫn đóng 1–12 | Handler lọc theo năm rồi ghi hai ô vào `SerialBinding` mà không thu hẹp danh sách số | Thu hẹp theo vị trí trong thứ tự phát hành (số hiệu là chuỗi, không so số học); số không có trong năm bị chặn; modal có ô từ số/đến số và báo trước sẽ đóng những số nào; nút "In nhãn gáy tập" trên dòng tập. Phép thử tích hợp `Binding_by_issue_range…` đỏ trước (IssueCount 4 thay vì 2), 4 vitest |
| JB5 | Vừa | Báo cáo bổ sung **không có biểu đồ đúng nghĩa**: ba khối "Theo kho / dạng tài liệu / tình trạng" là thanh tiến độ cắt còn 10 dòng đầu, tab thống kê theo chiều chỉ có bảng; tổng quát và duyệt mua **không xuất được tệp** dù E-HSMT đòi ba dạng đầu ra cho mọi báo cáo | Phase 6 ghi chú "đủ để trả lời cái nào nhiều hơn"; hai loại báo cáo chưa có `AcquisitionReportKind` | `StatChart` (Recharts, cột/tròn, đủ mọi dòng, màu phân loại từ `MAU_BIEU_DO`); `Overview` và `PurchaseApproval` vào bộ xuất Excel/PDF. Phép thử tích hợp mở rộng (đỏ: 400 vì enum chưa có), 3 vitest cho dữ liệu biểu đồ |
| JB6 | Thiếu chức năng | **Biên mục sơ lược chỉ mở được từ một dòng đơn đặt**; sách tặng, sách mua ngoài đơn không có lối vào. "Nhập nhanh liên tục" của đặc tả không tồn tại: modal đóng sau mỗi lần lưu | Phase 6 gắn form vào `PurchaseOrderPage` như một modal | Màn hình riêng Bổ sung › Biên mục sơ lược: lưu xong giữ bối cảnh đợt (kho, dạng tài liệu, NXB, số bản), xóa phần của cuốn vừa nhập, trả tiêu điểm về nhan đề, đếm đã nhập kèm mã vạch. 3 vitest cho quy tắc giữ/xóa ô |
| JB7 | Thiếu chức năng | **Nhãn gáy không in được logo thư viện** dù đặc tả III.2 ghi rõ; trình thiết kế tem **không có ô xem trước** nào, "xem trước" trong bảng đáp ứng là nói quá | `LabelLayoutDto` không có khối ảnh; `LabelPrintService` không nhận logo; endpoint ảnh mã vạch có từ phase 6 mà không màn hình nào gọi | Khối logo (mm) trong bố cục; hai handler in nạp logo như biểu mẫu; `LabelPreview` mô phỏng tem 5 px/mm với mã vạch thật và logo thật, dùng ở trình thiết kế (dữ liệu mẫu) và hộp in (bản đầu tiên đang chọn); `labelContent.ts` chiếu `LabelContentBuilder` với 6 vitest lặp đúng các trường hợp của phép thử C#. 3 phép thử đơn vị C# (đỏ: không biên dịch khi chưa có `Logo`; sau đó đỏ lần hai vì phép thử quên khai giấy phép QuestPDF — DI khai hộ lúc chạy thật) |
| JB8 | Vừa | **Không đánh giá được nhà cung cấp**: cột `Rating` có ở thực thể từ phase 6 nhưng không khai ở `CatalogRegistry` nên màn hình danh mục không hiện, không sửa được; lịch sử giao dịch không có số sao | Sót dòng khai trường | Trường Number 0–5 ở danh mục nhà cung cấp; `SupplierHistoryDto.Rating` hiện thành sao ở báo cáo. Phép thử tích hợp mở rộng đỏ trước (Extras không có `rating`) |
| JB9 | Vừa | **Chuyển kho hàng loạt bằng quét barcode** không có: phải tick từng dòng trên danh sách phân trang, mà sách đang ở trên tay | Hộp chuyển kho chỉ nhận lựa chọn từ bảng | Ô quét liên tục trong hộp chuyển kho, tra đúng mã, gom danh sách, quét trùng báo "đã có"; mở được hộp khi chưa tick gì. 2 vitest |
| JB10 | Nhẹ | Tiến độ kiểm kê chỉ đổi khi chính cửa sổ ấy quét; máy rời và điện thoại quét vào cùng kỳ thì người điều phối phải bấm làm mới | Không có `refetchInterval` | Nạp lại tiến độ mỗi 5 giây khi kỳ chưa chốt |

Bài học của đợt: **"Có" trong bảng đáp ứng phải là "đi được từ menu tới tờ giấy"**, không phải "có
endpoint". Bảy trong mười dòng trên đều có máy chủ làm đúng từ phase 6–7; cái thiếu là lối đi của
người dùng, và bảng đáp ứng đã ghi "Có" cho cái lối chưa có ấy.

#### Làm tiếp — Phân hệ III và IV (bổ sung, ấn phẩm định kỳ)

1. Đơn đặt sinh từ yêu cầu đặt báo (JB2): dòng đơn mang đơn giá/kỳ và số bản, chưa nhân số kỳ nên
   tổng đơn nhỏ hơn giá trị duyệt; cần thêm số kỳ vào `PurchaseOrderItem` hoặc quy ước đơn giá dòng
   đơn là "cả kỳ đặt".
2. Phiếu khiếu nại chưa in được thành văn bản gửi nhà cung cấp; biên bản bàn giao độc lập (không từ
   đơn) chưa có dòng chi tiết tự nhập; ô "tình trạng" khi ghi nhận số báo chưa có (thực thể
   `SerialIssue` không có cột); kéo thả sắp xếp trường trong trình thiết kế biểu mẫu; phân công cán
   bộ kiểm kê bằng tài khoản thay vì gõ tên.

---

### J.L — Phân hệ VI, VII và III.4 (bạn đọc, lưu thông, kiểm kê)

Đối chiếu từng dòng Phân hệ VI, VII và III.4 của đặc tả với thứ đang chạy thật, đi từ giao diện
xuống API. Lỗi ghi ở đây là chỗ **bảng đáp ứng ghi "Có" mà người dùng không làm được**.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JL1 | Nặng | Kho đã đóng để kiểm kê vẫn cho mượn và trả ở quầy như thường; màn hình quầy không biết kho nào đang đóng. Đặc tả III.4 bước 1 ghi rõ "ngưng cho mượn/trả tại kho đó, cảnh báo trên màn hình lưu thông", bảng đáp ứng đã đánh "Có" từ phase 6 | `Warehouse.IsClosedForInventory` chỉ được kiểm ở chuyển kho và nhập kho (`ItemLifecycleFeatures`, `ReceivingFeatures`); `CirculationDeskService` không tham chiếu tới nó | Quét mượn thêm cảnh báo chặn `WAREHOUSE_CLOSED`, ghi mượn thẳng API trả 409; ghi trả vẫn nhận nhưng báo giữ ở quầy (quyết định ghi ở docs/00); banner trên Quầy lưu thông đọc `IsClosedForInventory` từ danh sách kho. Hai phép thử tích hợp `Kho_dang_dong_de_kiem_ke_thi_khong_ghi_muon_duoc`, `Tra_sach_ve_kho_dang_kiem_ke_van_nhan_nhung_bao_giu_o_quay` chạy đỏ trước khi sửa |
| JL2 | Thiếu chức năng | Bảng đáp ứng ghi "Có" cho cấp lại thẻ, đồng bộ từ hệ thống đào tạo và giấy xác nhận trả sách, nhưng trên giao diện quản trị không có nút nào gọi tới: `readersApi.reissueCard`, `readersApi.sync` khai sẵn mà không màn hình nào dùng; giấy xác nhận chỉ in được bằng cách gõ URL | Phase 8 làm API và phép thử tích hợp rồi coi như xong; đợt rà trước đi theo API nên không thấy thiếu | Hộp thoại cấp lại thẻ và khối đồng bộ trong Nhập xuất dữ liệu; nút In giấy xác nhận trong hồ sơ, khóa kèm lý do khi còn nợ. Lối in riêng `GET /api/readers/{id}/clearance/print` vì lối in chung đòi quyền in chứng từ bổ sung mà cán bộ bạn đọc không có |
| JL3 | Vừa | Bảng lỗi nhập bạn đọc từ Excel chỉ đọc, dù đặc tả VI.4 ghi "bảng lỗi sửa được tại chỗ rồi nhập lại" và bảng đáp ứng đã đánh "Có" | Bước kiểm tra trả về lỗi mà không trả về nguyên ô của dòng lỗi, nên giao diện không có gì để sửa | Bước kiểm tra trả thêm `errorRowCells`; endpoint `POST /api/readers/import/rows` kiểm lại / nhập các dòng đã sửa qua đúng `ReaderImportProcessor`; hai phép thử tích hợp mới, kể cả kiểm rằng lần thử không tạo hồ sơ |
| JL4 | Nhẹ | In thẻ từ danh sách bạn đọc và từ hồ sơ không có nút xem trước, dù máy chủ đã nhận `preview` và có phép thử "xem trước không tăng số lần in" | Chỉ trình thiết kế mẫu thẻ gửi `preview: true`; hai chỗ in thật quên | Nút "Xem trước (không tính lần in)" ở cả hai chỗ, thân yêu cầu dựng qua `cardPrintRequest` có phép thử |

---

### J.D — Phân hệ V, VIII và IX (tài liệu số, nội dung, tra cứu)

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JD1 | Nghiêm trọng | **"Xuất toàn bộ dữ liệu hệ thống" (mục 4 E-HSMT) không tồn tại**: mã quyền `EXCHANGE.DATA.FULL_EXPORT` và trạng thái `FullSystemExport` đã khai từ phase 10 nhưng không có endpoint, không có handler, không có nút | Hạng mục này chỉ được nhắc một dòng trong đặc tả và không thuộc màn hình nào, nên trôi qua mọi đợt rà trước | Tác vụ nền (không chạy trong lượt HTTP) đóng gói ZIP: biểu ghi MARC dạng ISO 2709 và MARCXML, toàn bộ tệp tài liệu số, metadata Excel + Dublin Core, bảng bạn đọc / ĐKCB / giao dịch dạng CSV; có tiến độ, khoá chống chạy trùng và ghi nhật ký xuất dữ liệu |
| JD2 | Vừa | **Gói xuất tài liệu số thiếu MARCXML** dù chú thích ngay trên lớp ghi là "Excel, MARCXML và Dublin Core" | Chú thích viết trước, mã viết sau và chỉ làm hai trong ba | Thêm `metadata/marcxml.xml`; sửa lại chú thích cho khớp mã |
| JD3 | Vừa | **Nhập ZIP bỏ qua tệp Excel metadata**: mọi tài liệu nhập vào lấy nhan đề = tên tệp | Vòng lặp chỉ duyệt tệp tài liệu, bỏ mọi thứ khác | Đọc `metadata.xlsx` trong gói (tên tệp, nhan đề, mô tả, mức truy cập, mã biểu ghi) và áp vào từng tài liệu; có tệp mẫu tải về |
| JD4 | Vừa | **Thời lượng đọc tài liệu số luôn rỗng**: cột `DurationSeconds` được đọc ra ở ba chỗ mà không nơi nào ghi | Nhật ký chỉ ghi lúc mở trang, không có gì đóng phiên đọc | Trình đọc gửi tổng số giây định kỳ và một lần cuối khi rời trang; máy chủ giữ số lớn nhất |
| JD5 | Vừa | **Cây menu không kéo thả được và không nhập được icon** dù endpoint sắp xếp `PUT /content/menus/order` đã có và hàm gọi đã khai trong `api.ts` | Cây chỉ đặt `treeData`, không bật `draggable`; biểu mẫu thiếu ô icon | Bật kéo thả nối vào endpoint sẵn có, thêm ô icon và cho trang tra cứu hiện icon |
| JD6 | Nhẹ | **Trình soạn thảo nội dung chỉ chèn được ảnh**, không chèn được tệp PDF/Word/Excel như đặc tả VIII.1; chú thích còn ghi nhận SVG trong khi mã không nhận | `DetectImageType` chỉ nhận bốn định dạng ảnh | Mở rộng thành `DetectFileType` nhận thêm PDF/DOCX/XLSX bằng chữ ký nhị phân; nới quyền tải tệp cho cán bộ tin tức; sửa chú thích |
| JD7 | Vừa | **Trang tra cứu không có thẻ meta phía máy chủ**: `index.html` mang tiêu đề tĩnh cho mọi trang, máy thu thập lấy sitemap ra rồi vẫn nhận trang trống | SPA dựng bằng Vite, không có SSR và không có `react-helmet` | Nginx rẽ ba đường dẫn có nội dung riêng sang API khi User-Agent là máy thu thập; API chèn `<title>`, `description`, Open Graph vào chính `index.html` của trang tra cứu. Người thật không đi qua chặng ấy |
| JD8 | Nhẹ | **Không có nút chia sẻ** ở trang chi tiết tài liệu (đặc tả IX.2) | Bỏ sót | Nút *Chia sẻ*: Web Share API khi có, không thì chép liên kết |
| JD9 | Nhẹ | **Album ảnh chỉ có ở giao diện quản trị**: endpoint công khai `/api/public/galleries` là mã chết, trang tra cứu không có route nào | Làm phía quản trị rồi dừng | Route `/thu-vien-anh` trên trang tra cứu, mục menu và album mẫu trong dữ liệu seed |
| JD10 | Nhẹ | **Trang chủ thiếu khối "Thông báo"** mà đặc tả IX.1 liệt kê; chỗ ấy là khối "Thống kê" không có trong đặc tả | Đọc thiếu một gạch đầu dòng | Thêm khối thông báo lấy từ chuyên mục tương ứng, và không lặp lại các mục ấy ở khối tin tức |

### J.X — Ba việc còn lại của đợt, làm ở nhánh chính

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JX1 | Vừa | **Chi tiết biểu ghi thiếu tab lịch sử lưu thông** (đặc tả II.3 nói bốn tab): tab thứ tư chỉ có lịch sử sửa đổi, và không có endpoint nào trả lịch sử mượn của biểu ghi | Bảng đáp ứng ghi "Bốn tab chi tiết" và không ai đối chiếu nội dung từng tab với đặc tả | `GET /cataloging/bibs/{id}/loans` phân trang phía máy chủ, lọc phiếu chưa trả, tìm theo mã phiếu / mã vạch / tên hoặc số thẻ bạn đọc. Lọc theo `BibId` chép sẵn trên phiếu chứ không đi vòng qua ĐKCB — biên mục lại một bản in không được kéo lịch sử cũ sang biểu ghi mới |
| JX2 | Vừa | **Không có quét virus nào** dù mục 6.4 ghi "quét virus (ClamAV tùy chọn)" | "Tùy chọn" bị đọc thành "không cần làm" | `IVirusScanner` đặt ở cổng vào duy nhất của mọi tệp tải lên (cả tải một lần lẫn tải theo mảnh đều đi qua đó); `ClamAvScanner` nói thẳng giao thức INSTREAM của clamd, không thêm thư viện. Tắt thì không mở socket nào; **bật mà không nối được clamd thì từ chối tệp** |
| JX7 | Vừa | **Gửi yêu cầu gia hạn chờ duyệt báo là lỗi**: thư viện bật "gia hạn phải duyệt" thì bạn đọc bấm Gia hạn trên trang tra cứu hoặc trong ứng dụng nhận thông báo **đỏ**, dù yêu cầu đã được ghi nhận đúng | Đường thành công ném `ConflictException` nên máy chủ trả 409; hai máy khách đều coi 409 là lỗi và hiện nguyên câu ấy bằng màu lỗi | Trả về dòng phiếu (hạn trả giữ nguyên) kèm cờ `renewalPending`; endpoint đổi câu trả lời theo cờ; trang tra cứu và ứng dụng hiện thông báo xanh "Đã gửi yêu cầu gia hạn". `CirculationTests.Gui_yeu_cau_gia_han_cho_duyet_la_thanh_cong_chu_khong_phai_loi` đỏ trước, xanh sau |
| JX8 | Vừa | **"Tiếp tục khi gián đoạn" (V.1) chỉ đúng trong một lần bấm**: mã phiên tải chỉ nằm trong bộ nhớ trang, tải lại trang hay đóng trình duyệt là mất — tệp 300 MB đang dở 40% phải làm lại từ đầu | Vòng thử lại ba lượt được viết cho lỗi mạng thoáng qua, không cho lượt gián đoạn thật | Nhớ phiên (mã, tên tệp, dung lượng) trong bộ nhớ trang; chọn lại đúng tệp thì hỏi máy chủ còn thiếu mảnh nào và gửi tiếp, kèm dòng "Tiếp tục phiên dở dang: đã có N/M mảnh". Bỏ phiên quá bảy ngày vì máy chủ đã dọn. `uploadSessions.test.ts` 6 ca, gồm cả trường hợp trình duyệt chặn bộ nhớ trang và dữ liệu lưu bị hỏng |
| JX9 | Nhẹ | **Duyệt A–Z chỉ có ở nhánh Tác giả**, dù IX.2 nói duyệt "dạng cây và A-Z": Chủ đề, Phân loại, Bộ sưu tập, Ngành và Môn học không nhận tham số chữ cái, và dải chữ cái cũng chỉ hiện ở nhánh Tác giả | Tham số `letter` chỉ được xử lý trong nhánh tác giả và chỉ endpoint tác giả khai nó | Bộ lọc chữ cái đưa vào hàm lọc chung của mọi nhánh (so trên tên đã bỏ dấu, "Đ" nằm cùng chỗ "D"), năm endpoint còn lại khai thêm `letter`, dải chữ cái hiện ở mọi nhánh. `ContentAndOpacTests.Duyet_theo_chu_cai_ap_cho_moi_nhanh…` đỏ trước, xanh sau — bài này bắt được cả chỗ handler đã lọc mà endpoint chưa truyền tham số xuống |
| JX10 | Nhẹ | **Ba tab báo cáo tài liệu môn học cùng xuất ra một tệp**: bấm Xuất ở tab "Môn chưa có tài liệu" hay "Tài liệu dùng chung" đều nhận bảng mức độ đáp ứng | Bộ xuất chỉ dựng đúng một bảng, hai bảng kia tính ra rồi bỏ đó | Tham số `report` chọn bảng (coverage / uncovered / shared) cho cả Excel lẫn PDF; nút Xuất theo tab đang xem và đặt tên tệp riêng. `CourseDocumentTests.Xuat_duoc_ca_ba_bang_bao_cao_tai_lieu_mon_hoc` kiểm cả sáu tổ hợp và đòi ba tên tệp khác nhau |
| JX11 | Nhẹ | **Trình thiết kế biểu mẫu không kéo thả được** dù III.6 nói "kéo thả trường": thêm trường xong là thứ tự cố định, muốn đổi phải xoá rồi thêm lại đúng thứ tự mong muốn | Bảng trường và bảng cột chỉ có nút Thêm và Xoá, không có cách hoán vị | Kéo thả gốc của trình duyệt trên cả hai bảng (không thêm thư viện), hàm đổi chỗ tách riêng ở `lib/reorder.ts` với 5 ca kiểm thử gồm cả kéo hụt ra ngoài danh sách |
| JX12 | Vừa | **Khiếu nại số báo thiếu không ra được tờ giấy nào** dù IV.3 nói "tạo phiếu khiếu nại gửi nhà cung cấp": chỉ có dòng trên màn hình, cán bộ phải tự chép tay ra công văn | Bản ghi khiếu nại làm xong từ phase 7 nhưng không có loại biểu mẫu tương ứng | Loại mẫu `SERIAL_CLAIM` với bộ dựng dữ liệu gom mọi số cùng số phiếu vào một tờ (tên báo, ISSN, nhà cung cấp kèm địa chỉ và điện thoại, bảng số báo thiếu), mẫu mặc định nạp sẵn, nút In ở thẻ Khiếu nại. Bộ nạp mẫu chuyển sang hỏi theo **từng loại** — hỏi gộp thì bản cài cũ đã có bốn mẫu lưu thông sẽ không bao giờ nhận được mẫu thứ năm. `SerialTests.Phieu_khieu_nai_in_ra_duoc_thanh_van_ban_gui_nha_cung_cap` tự lập phiếu rồi in, và đòi số phiếu không có thật phải trả 404 |
| JX13 | Nhẹ | **Trang cá nhân của bạn đọc thiếu mục tài liệu số**, dù IX.3 liệt kê "tài liệu số được cấp quyền" trong bảy mục; trạng thái các yêu cầu đã gửi cũng không xem được ở đâu | Phần tài liệu số làm ở trang riêng, không ai nối ngược vào trang cá nhân; endpoint `/reader/digital/requests` có mà trang tra cứu không gọi | Thêm thẻ "Tài liệu số": từng yêu cầu kèm trạng thái, hạn được đọc, số lần đã xem trên tổng số cho phép, lý do bị từ chối, và lối đọc thẳng khi đã duyệt |
| JX14 | Nhẹ | **Ghi nhận số báo không có ô tình trạng** dù IV.4 nói "nhận từng số — ngày nhận, số lượng, tình trạng": cán bộ ghi lẫn vào ô ghi chú nên không lọc hay thống kê được số rách, thiếu trang | Thực thể `SerialIssue` không có cột ấy từ phase 7 | Cột `condition` (migration `20260904140000_TinhTrangSoBao`), ô nhập trong biểu mẫu ghi nhận và một cột trên lưới số báo. `SerialTests.Ghi_nhan_so_bao_luu_duoc_tinh_trang_vat_ly` đòi tình trạng nằm riêng, không trộn vào ghi chú |
| JX5 | Vừa | **Triển khai xong mà nginx vẫn chạy cấu hình cũ**: thẻ meta cho máy thu thập không có tác dụng trên máy chủ thật dù tệp cấu hình trên đĩa đã mới | Cấu hình gắn vào container theo **tệp**; `git reset --hard` của script triển khai thay tệp bằng một inode mới, container vẫn giữ inode cũ. `nginx -s reload` cũng vô ích vì nó đọc lại đúng inode cũ ấy | `gh-deploy.sh` dựng lại container nginx sau khi kéo mã. Đã chứng minh trên máy chủ thật: trước khi dựng lại, `grep lc_crawler` trong container ra 0; sau khi dựng lại ra 2 và máy thu thập nhận đúng nhan đề bản tin |
| JX6 | Vừa | **Cổng Z39.50 không ra tới ngoài trên máy chủ thật** dù compose gốc đã công bố | Lớp `docker-compose.prod.yml` đặt `ports: !override []` cho API — đúng cho HTTP (mọi lượt gọi đi qua nginx) nhưng Z39.50 là TCP thô, nginx không proxy được | Lớp sản xuất công bố lại đúng một cổng `${Z3950_PORT:-210}:2100` |
| JX4 | Nhẹ | Phép thử `The_permission_catalogue_and_the_staff_groups_are_seeded` **phụ thuộc thứ tự**: xanh khi chạy riêng, đỏ khi chạy trọn bộ | Nó khẳng định *mọi* nhóm trong trang đầu đều là nhóm hệ thống, trong khi các bài phân quyền tạo nhóm thường trên cùng một cơ sở dữ liệu — đủ nhóm mới là nhóm thứ 51 đẩy một nhóm thường vào trang. Cùng lớp lỗi với I4 | Chỉ soi năm nhóm mẫu theo mã, không soi mọi nhóm trong trang. Đỏ khi chạy cùng `PermissionAndAuditTests` trước khi sửa |
| JX3 | Nhẹ | **Dừng êm chỉ có mặc định 5 giây của .NET**: `docker compose stop` cắt ngang lượt nhập biểu ghi, phiên Z39.50 hay lượt tải tài liệu số đang chạy | Không ai đặt `ShutdownTimeout`; máy chủ Z39.50 không đóng ổ nghe khi dừng | Hạn dừng 30 giây (cấu hình được), compose cho container 45 giây; `Z3950ServerHost.StopAsync` đóng ổ nghe để trả cổng ngay cho lần khởi động sau |


### J.A — Quy trình duyệt và thông báo cho cán bộ

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JA1 | Vừa | **Quy trình duyệt nhiều cấp chỉ cấu hình được *số cấp*** dù III.1 nói "cấu hình được quy trình duyệt nhiều cấp": ai có quyền duyệt cũng duyệt được mọi cấp, và cùng một người bấm hai lần là xong — hai cấp thành hình thức | `ACQ.APPROVAL_LEVELS` đếm số lần bấm chứ không gắn cấp với ai; `ApprovedBy` chỉ ghi ở cấp cuối nên không có căn cứ để biết ai vừa duyệt cấp trước | Tham số `ACQ.APPROVAL_GROUPS` khai mã nhóm duyệt từng cấp theo thứ tự; người ngoài nhóm nhận 403, người vừa duyệt cấp trước nhận 409. `ApprovedBy/Name/At` nay ghi ở **mọi** cấp. Màn hình duyệt hiện "Chờ ‹nhóm› duyệt" trên cột trạng thái và giải thích cả hai luật trong hộp duyệt. `AcquisitionTests.Duyet_hai_cap_doi_dung_nhom_va_khong_cho_mot_nguoi_duyet_ca_hai` đỏ trước (cấp 1 trả 200 cho người sai nhóm), xanh sau. Nhóm dùng trong phép thử được cấp **đủ quyền duyệt** — mượn sẵn nhóm Thủ thư thì 403 ra từ chỗ thiếu quyền, phép thử xanh vì lý do sai |
| JA2 | Vừa | **Không có thông báo nào cho cán bộ**: III.1 nói "gửi duyệt → chuyển trạng thái, **thông báo tới người duyệt**", V.2 nói cán bộ nhận danh sách yêu cầu chờ duyệt, II.4 phân công việc biên mục — cả ba đều im lặng, cán bộ phải tự mở màn hình ra dò | Bảng `sys.notifications` có cột `user_id` từ phase 1 nhưng không chỗ nào ghi vào — toàn bộ hệ thống thông báo làm cho **bạn đọc** rồi coi là xong | `IStaffNotifier` gửi theo tài khoản, theo nhóm, hoặc theo mã quyền (dùng khi cấp duyệt chưa gắn nhóm); điều kiện "ai nhận" nằm trong câu hỏi gửi xuống cơ sở dữ liệu. Gắn vào năm chỗ: gửi duyệt, duyệt xong còn cấp trên, duyệt/từ chối báo người đề nghị, phân công biên mục, yêu cầu đọc tài liệu hạn chế. Chuông trên thanh trên của giao diện quản trị, hỏi lại mỗi phút. Email chỉ là bản sao: SMTP hỏng được ghi nhật ký chứ không làm đổ nghiệp vụ đã gọi nó. `AcquisitionTests.Gui_duyet_thi_nguoi_duyet_nhan_duoc_thong_bao` đỏ trước (0 thông báo), xanh sau |
| JA3 | Vừa | **Biên bản bàn giao không có bảng chi tiết của riêng nó** dù III.1 nói "danh sách tài liệu, số lượng, **tình trạng**": biên bản chỉ giữ ba con số tổng, bảng trên bản in được tra ngược từ đơn đặt lúc in, và không chỗ nào ghi được tình trạng từng dòng | Bảng chi tiết được coi là thứ suy ra được từ đơn đặt, nên không ai lưu nó. Hệ quả: biên bản không gắn đơn đặt (sách biếu tặng, nộp lưu chiểu) in ra tờ giấy trắng, và sửa đơn đặt về sau làm đổi luôn tờ giấy hai bên đã ký | Bảng `acq.handover_lines` (migration `20260904150000_BangGiaoChiTiet`) có cột tình trạng; lập biên bản từ đơn đặt thì dòng được **chép** sang, cán bộ mở thùng sách ra rồi ghi tình trạng từng dòng. Dòng tổng đọc từ chính bảng ấy chứ không nhận số gõ tay — hai con số trên cùng tờ giấy mà lệch nhau là biên bản không ký được. Mẫu in có thêm cột "Tình trạng"; biên bản lập trước ngày này không có dòng nào nên vẫn in từ đơn đặt như cũ. `AcquisitionTests.Bien_ban_ban_giao_co_bang_chi_tiet_va_cot_tinh_trang` đỏ trước (0 dòng), xanh sau |
| JA4 | Vừa | **Phân công kiểm kê là một ô chữ gõ tay** dù III.4 bước 2 nói "phân công cán bộ": không ai hỏi được "tôi phải kiểm kho nào", hệ thống không báo được cho ai, và người đã nghỉ việc vẫn đứng tên trên kỳ đang chạy | Cột `assigned_staff` kiểu chuỗi làm từ phase 6 và không ai đối chiếu lại với đặc tả | Bảng `acq.inventory_period_staffs` (migration `20260904160000_PhanCongKiemKe`) nối kỳ với tài khoản, kèm chỉ mục duy nhất để bấm hai lần không sinh hai dòng. Phân công lại được giữa kỳ qua `PUT /inventory/periods/{id}/staff` — người ốm, người bận, kỳ chạy cả tuần — và chỉ người **mới** thêm vào mới nhận thông báo. Cột chữ cũ giữ lại làm bản chép để in lên biên bản và để kỳ cũ vẫn in đúng như cũ |
| JA5 | Nặng | **Ô chọn cán bộ ở màn hình phân công biên mục luôn rỗng**: không phân công được việc cho ai từ giao diện | Nó gọi `/api/users`, một địa chỉ không tồn tại (đường thật là `/api/admin/users`) nên nhận 404 và danh sách rỗng — lỗi im lặng, màn hình chỉ hiện một ô chọn không có mục nào | Endpoint `GET /api/staff/options` riêng cho các ô chọn người nhận việc: chỉ đòi đăng nhập bằng tài khoản cán bộ, trả đúng tên và tên đăng nhập. Đường quản trị người dùng không dùng được ở đây vì cán bộ biên mục không có quyền ấy — mà cũng không nên có, phân việc cho đồng nghiệp không phải là xem hồ sơ tài khoản của họ. Màn hình kiểm kê dùng chung endpoint này |

### J.M — Ứng dụng di động và trang tra cứu (đợt rà 04/09/2026, đợt hai)

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JM1 | Nặng | **Thông báo đẩy chưa từng chạy được trên bản dựng nào**: mã Dart viết đủ (xin quyền, lấy token, đăng ký với máy chủ, mở đúng màn hình khi chạm) và đã nối vào ứng dụng, nhưng mọi lượt khởi động đều rơi về "không có thông báo đẩy" | `android/app/build.gradle.kts` không khai trình cắm `com.google.gms.google-services`, nên `Firebase.initializeApp()` gọi không tham số không có định danh dự án và luôn ném. Làm đúng hướng dẫn trong `mobile/README.md` (đặt `google-services.json` vào đúng chỗ) vẫn im — hướng dẫn thiếu hẳn một bước | Áp dụng trình cắm **có điều kiện**, khi thật sự có `google-services.json`: áp dụng vô điều kiện thì lần dựng nào không có tệp cũng đổ, mà tệp ấy chứa định danh Firebase riêng của từng thư viện nên không đưa vào kho mã được. Bản dựng in ra một dòng nói rõ nó có nhận thông báo đẩy hay không |
| JM2 | Nặng | **Thông điệp tới lúc ứng dụng nằm trong túi thì mất hẳn**: chỉ có `onMessage` (đang mở) và `onMessageOpenedApp` (chạm vào) | Không ai đăng ký `onBackgroundMessage`. Mà đây đúng là tình huống thường gặp nhất của XI.2 — nhắc sắp đến hạn trả và báo sách đặt giữ đã sẵn sàng gần như luôn tới lúc điện thoại không mở ứng dụng | Hàm cấp cao nhất `pushBackgroundHandler` kèm `@pragma('vm:entry-point')` (bản phát hành cắt bỏ mọi hàm không ai gọi, mà hàm này chỉ Firebase gọi); bỏ qua thông điệp hệ điều hành đã tự hiện để không ra hai dòng trùng. `test/core/push_background_test.dart` quét ba điều kiện, đỏ trước khi sửa |
| JM3 | Nhẹ | **iOS không khai chế độ nền `remote-notification`** nên không được đánh thức cho thông điệp chỉ có phần dữ liệu | Phần iOS làm trên máy Windows, không mở được Xcode | Khai trong `Info.plist`. Quyền `aps-environment` vẫn phải bật bằng Xcode trên máy Mac — ghi rõ trong `mobile/README.md` chứ không nhận là đã xong |
| JM4 | Nặng | **Nhập danh mục từ Excel nuốt lặng lẽ mọi cột tham chiếu**: gõ tên hoặc mã khoa vào cột "Khoa quản lý" của tệp ngành đào tạo thì ngành nhập xong không thuộc khoa nào, mà kết quả vẫn báo thành công, không một dòng lỗi | Trường kiểu `Reference` ghi bằng `Guid.TryParse`, hỏng thì trả `null`; bước kiểm tra chỉ soi trường bắt buộc và trường chọn, bỏ qua hẳn kiểu tham chiếu. Cột trong tệp mẫu cũng không có một dòng hướng dẫn nào | Cột tham chiếu nhận **mã hoặc tên** (so sau khi bỏ dấu và bỏ hoa thường), không khớp thì dòng ấy báo lỗi chứ không im lặng; tên trùng nhau thì đòi gõ mã chứ không tự chọn một trong hai. Tệp mẫu ghi rõ "Nhập mã hoặc tên của …". `CatalogTests.Nhap_danh_muc_nhan_ten_cho_cot_tham_chieu_va_bao_loi_khi_khong_khop` đỏ trước (0 dòng lỗi), xanh sau |
| JM5 | Nặng | **Thẻ meta cho máy thu thập mất ở đúng cấu hình triển khai thật**: `nginx.prod.conf` không có nhánh rẽ máy thu thập, nên Google, Facebook và Zalo lấy `/tai-lieu/{id}` chỉ nhận `index.html` rỗng của trang đơn | Nhánh ấy được thêm vào `nginx.conf` và `nginx.behind-proxy.conf` mà quên tệp thứ ba. Đây đúng là bài học 9 — sửa một chỗ rồi ghi là "cả sản phẩm" | Thêm bảng `$lc_crawler` và khối `location` vào `nginx.prod.conf`. Đã kiểm bằng `nginx -t` trên chính tệp ấy với chứng thư dựng tạm |
| JM6 | Vừa | **Tra cứu nâng cao thiếu ba bộ lọc** đặc tả IX.2 nêu đích danh: ngôn ngữ, dạng tài liệu, kho | Máy chủ và kiểu dữ liệu đã có đủ ba trường từ lâu; chỉ màn hình chưa có ô chọn nào | Ba ô chọn lấy danh sách giá trị từ chính bộ đếm facet của kho, kèm số lượng: bạn đọc chỉ thấy giá trị **thật sự có tài liệu**, chứ không thấy cả danh mục có mục chưa dùng đến bao giờ |
| JM7 | Vừa | **"Giờ mở cửa từng cơ sở" (VIII.1) chỉ là một ô chữ gõ tay**, không gắn với cơ sở nào | Cột `opening_hours` của từng thư viện đã có và màn hình quản trị đã nhập được, nhưng API công khai chỉ trả một chuỗi lấy từ cấu hình trang | API công khai trả thêm danh sách cơ sở kèm địa chỉ, điện thoại, giờ mở cửa và toạ độ. Chân trang liệt kê giờ theo từng cơ sở, trang Liên hệ có khối "Các cơ sở" kèm lối chỉ đường riêng. Hết cơ sở mới quay về ô chữ tự do — nó vẫn hữu ích để ghi ngoại lệ |
| JM8 | Nhẹ | **Kết quả liên thư viện không gộp** dù IX.5 nói "hiển thị kết quả gộp có ghi rõ nguồn": mỗi thư viện một bảng riêng, cùng một cuốn ở hai nơi thì phải tự dò | Trường `sourceName` có sẵn trên từng biểu ghi nhưng giao diện chưa dùng lần nào | Bảng gộp đứng trước, xếp theo nhan đề, cột "Nguồn" ghi tên thư viện. Bảng theo từng máy chủ giữ lại bên dưới vì nó là chỗ duy nhất nói được máy chủ nào không tra được và mất bao lâu |
| JM9 | Nhẹ | **Không có lối duyệt theo môn học**, dù IX.2 liệt kê "Ngành / Môn học" và máy chủ đã mở `/browse/courses?letter=`; **danh sách tài liệu của một môn dừng ở 20 dòng** không có cách xem tiếp | Nhánh môn học chỉ đến được qua ngành, và bảng tài liệu gọi trang 1 rồi không vẽ thanh phân trang | Thêm nhánh `/duyet/mon-hoc` có dải chữ cái như mọi nhánh khác, dẫn sang trang kết quả lọc theo môn (bộ lọc `courseId` đã có ở máy chủ, chỉ thiếu chỗ đọc ra từ địa chỉ). Danh sách tài liệu của môn có phân trang |
| JM10 | Nhẹ | **Sơ đồ trang bỏ sót sáu trang duyệt công khai** (bộ sưu tập, ngành, môn học, luận văn, ấn phẩm định kỳ, tài liệu số, thư viện ảnh) | Danh sách trang tĩnh viết tay từ phase 12 và không cập nhật khi thêm route mới | Bổ sung đủ; `ContentAndOpacTests.So_do_trang_liet_ke_tai_lieu_da_xuat_ban` nay đòi từng đường dẫn một, đỏ trước khi sửa |

### J.N — Phân hệ I, II, IV, V (đợt rà 04/09/2026, đợt ba)

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JN1 | Nặng | **Biểu ghi bài trích sinh ra không tra cứu được từ trang tra cứu**, đúng thứ duy nhất mà IV.2 nêu là lý do của chức năng ("để tra cứu được từ OPAC"). Màn hình còn báo ngược: "đã sinh N biểu ghi; bạn đọc tra được từ OPAC" | Biểu ghi bài trích được tạo ở trạng thái Nháp, trong khi mọi đường tạo biểu ghi khác mặc định Đã xuất bản, và trang tra cứu chỉ đọc biểu ghi đã xuất bản | Xuất bản ngay khi sinh: cán bộ đã gõ xong mục lục bài trích rồi mới bấm sinh, không còn bước biên mục nào dở dang để chờ. `SerialTests` nay tra thẳng qua `/api/search` bằng máy khách ẩn danh — đỏ trước, xanh sau |
| JN2 | Nặng | **Gộp trùng danh mục không sửa biểu ghi**, dù II.9 nói "cập nhật toàn bộ biểu ghi liên quan": danh mục và bộ lọc sạch, nhưng cột Tác giả trên danh sách, dạng ISBD, phích mục lục và tệp xuất ISO 2709 vẫn in tên cũ | Lượt gộp chỉ `ExecuteUpdate` trên ba bảng liên kết. `marc_data` — bản gốc của biểu ghi — và cột phẳng rút từ nó không ai đụng tới. Đúng bài học 16, tự mình vi phạm lại | Gộp xong thì thay tên ngay trong MARC của từng biểu ghi liên quan (100$a, 700$a, 245$c cho tác giả; 650$a; 653$a), lưu qua bộ ghi biểu ghi để cột phẳng theo và có một phiên bản trong lịch sử. Danh sách biểu ghi phải lấy **trước** lượt đổi liên kết, và phải nạp kèm bốn tập liên kết — thiếu thì bộ ghi tưởng biểu ghi chưa có liên kết nào và thêm lại, lượt lưu đổ vì trùng khoá |
| JN3 | Nặng | **Ô "Xem" trong cài đặt nhật ký là công tắc chết**: I.4 nêu đích danh Create/Update/Delete/**Read**, cột được lưu, nhánh xét cũng có, nhưng không chỗ nào trong sản phẩm phát ra hành động Read | Ba hành động ghi được bộ chặn của Entity Framework bắt tự động; đọc thì không chạm tới `SaveChanges` nên không có gì bắt được | Thuộc tính `AuditRead` gắn vào endpoint chi tiết của Bạn đọc, Người dùng, Biểu ghi và Tài liệu số; không gắn vào danh sách, vì mở một trang danh sách bạn đọc không phải là xem hồ sơ của trăm người. Đối tượng chưa có chỗ ghi thì ô bị khoá kèm lời giải thích, thay vì một công tắc không nối vào đâu. Kèm theo: bộ đệm cài đặt nhật ký được xoá sau khi nạp dữ liệu — lượt đọc đầu tiên của bản cài mới có thể rơi vào lúc bảng còn rỗng và chạy theo mặc định suốt năm phút |
| JN4 | Vừa | **Đổi lịch sao lưu trên giao diện không có tác dụng tới lần khởi động lại**, mà màn hình vẫn hiện giờ mới nên không ai biết | Việc định kỳ chỉ được đăng ký một lần trong `StartAsync` của bộ đăng ký; lượt lưu tham số không đụng tới Hangfire | Lưu tham số `BACKUP.*` xong thì đăng ký lại ngay, dùng chung đúng đường với lượt khởi động. Màn hình sao lưu hiện thêm **lịch bộ chạy nền đang giữ**, và chỉ nói ra khi nó lệch với lịch đã khai. `BackupTests.Doi_lich_sao_luu_tren_giao_dien_thi_viec_dinh_ky_doi_theo_ngay` đỏ trước (vẫn là lịch cũ), xanh sau |
| JN5 | Vừa | **Phục hồi chỉ phục hồi cơ sở dữ liệu**: tệp tài liệu số nằm ở kho đối tượng, phục hồi xong thì biểu ghi nói "có toàn văn" mà bạn đọc bấm vào chỉ nhận lỗi | Chiều sao lưu đã chép tệp ra thư mục cạnh tệp dump, nhưng chiều về thì hướng dẫn bảo tự chạy `mc mirror` bằng tay — một bước thủ công nằm trong quy trình khẩn cấp là một bước bị quên | `IObjectStorageMirror.RestoreAsync` tải các tệp ấy trở lại đúng bucket, chạy ngay sau `pg_restore`, và báo số tệp đã tải. Lỗi ở bước này không lật ngược lượt phục hồi cơ sở dữ liệu nhưng được ghi rõ ở mức lỗi. Chỉ nhận đúng hai bucket của sản phẩm, thư mục lạ trong bản sao lưu không thành bucket mới |
| JN6 | Vừa | **Danh mục tự tạo không hiện thành bộ lọc trên trang tra cứu** dù II.9 nói vậy và màn hình khai báo có hẳn ô "Hiện làm bộ lọc trên tra cứu" | Cờ `ShowAsFacet` được lưu nhưng không nơi nào đọc: bộ đếm facet trả về bảy nhóm viết cứng | Nhóm lọc dựng từ danh mục tự tạo đang bật cờ, đếm trên đúng tập kết quả đang xem như mọi nhóm khác; trang tra cứu nhận diện bằng tiền tố `custom:` nên thư viện khai bao nhiêu danh mục cũng chạy. Bộ lọc `customIndexValueId` đi qua cả tra cứu cơ bản lẫn nâng cao. `CustomIndexTests.Danh_muc_tu_tao_hien_thanh_bo_loc_tren_trang_tra_cuu` kiểm cả nhóm lọc lẫn kết quả sau khi bấm |
| JN7 | Vừa | **Kỳ nghỉ của ấn phẩm định kỳ chỉ khai được theo tháng** (IV.4 nói "các kỳ nghỉ không xuất bản"): nhật báo nghỉ Chủ nhật thì sinh số cả năm ra 365 số và lưới theo dõi hiện 52 số "thiếu" không có thật, cán bộ đi khiếu nại nhà cung cấp về những số chưa bao giờ in | `SkipMonths` là thứ duy nhất trong cấu hình kỳ hạn | Thêm nghỉ theo **thứ trong tuần** và theo **khoảng ngày** (lặp hằng năm hoặc riêng một năm, đủ cho kỳ nghỉ Tết). Hai hàm sinh theo tháng nay xét ngày phát hành thật thay vì xét tháng của mốc lặp, nên một kỳ nghỉ nằm trong tháng không kéo cả tháng đi theo. Ba phép thử đơn vị mới |
| JN8 | Nhẹ | **Thống kê năng suất biên mục không lọc được theo thời gian** — luôn cộng dồn cả lịch sử, không so được hai kỳ | Endpoint nhận `from`/`to` từ đầu, giao diện gọi không tham số và không có bộ chọn ngày | Bộ chọn khoảng ngày kèm ba mốc sẵn (tháng này, ba tháng gần đây, năm nay) |
| JN9 | Nhẹ | **Màn hình tải tài liệu số lên không gắn được biểu ghi thư mục**: tải xong phải mở sửa từng tài liệu để gắn | Máy chủ nhận `bibId` ở cả ba lối tải lên, chỉ biểu mẫu bỏ trống trường ấy | Thêm ô chọn biểu ghi vào biểu mẫu tải lên, truyền qua cả ba lối |
| JN10 | Nhẹ | **Báo cáo ấn phẩm định kỳ chỉ có bảng và tệp**, thiếu dạng đồ họa mà mọi phân hệ khác đều có (yêu cầu 6.8) | Trang báo cáo viết sau, không dùng lại thành phần biểu đồ sẵn có | Dùng lại đúng thành phần biểu đồ của phân hệ Bổ sung: cùng cách đổi cột/tròn, cùng dải màu |
| JN11 | Nhẹ | **Quy tắc sinh mã thiếu hậu tố** dù I.3 liệt kê "prefix/suffix/độ dài/reset theo năm" | Bộ sinh mã đọc khoá `_SUFFIX` từ đầu, nhưng không có dòng tham số nào nên màn hình không hiện ô nhập | Nạp tám tham số hậu tố (mã vạch, ĐKCB, số thẻ, đơn đặt, yêu cầu mua, biên bản bàn giao, phiếu chuyển kho, quyết định thanh lý), mặc định rỗng nên mã cũ không đổi |
| JN13 | Nhẹ | **Không có đường nạp lại bộ định nghĩa MARC 21 chuẩn** dù II.5 nói "Import bộ định nghĩa MARC21 chuẩn": sửa hỏng một trường thì phải sửa tay từng ô hoặc dựng lại cả cơ sở dữ liệu | Bộ 220 trường chỉ được đọc bởi bộ nạp dữ liệu lúc khởi động; chú thích trong mã còn hứa "màn hình nhập định nghĩa dùng lại khi cán bộ chọn khôi phục bộ chuẩn", mà màn hình ấy không có nút nào | `POST /marc/fields/import-standard` hai chế độ: nạp bổ sung tag còn thiếu (an toàn, chạy lúc nào cũng được) và khôi phục ghi đè (hỏi lại trước khi chạy). Trường thư viện tự thêm không bị đụng tới ở cả hai chế độ, và trường đã xoá mềm thì sống lại thay vì đâm vào ràng buộc duy nhất. `CatalogingTests.Khoi_phuc_bo_dinh_nghia_MARC_chuan_khong_dung_toi_truong_rieng` sửa hỏng trường 245 rồi khôi phục, đồng thời canh một trường 9xx của thư viện |
| JN12 | Nhẹ | **Màn hình sao lưu không nói tệp nằm ở đâu** và thư mục đích không cấu hình được từ giao diện | Chỉ có trong biến môi trường | Màn hình hiện thư mục, **chỉ đọc**. Không cho sửa là cố ý: đó là đường dẫn bên trong container và nó phải trỏ vào ổ đĩa gắn ngoài; cho gõ tay là mở đường ghi bản sao lưu vào thư mục biến mất ở lần dựng lại sau |

**Ba chỗ lệch đặc tả nhưng giữ nguyên, ghi ra để khỏi phải kiểm lại:**

1. **Bản xem thử N trang đầu (V.1)** không được sinh thành một tệp riêng; số trang xem thử được áp lúc phục vụ từng trang. Bạn đọc vẫn chỉ xem được đúng N trang, mà không phải lưu thêm một bản sao của mọi tài liệu.
2. **Xoá biểu ghi (II.3)** bị chặn khi còn **bất kỳ** ĐKCB nào, chặt hơn câu "chặn nếu còn ĐKCB đang lưu thông". Nới ra thì xoá được biểu ghi trong khi vẫn còn sách trên giá — mất chỗ dựa của chính những bản ấy.
3. **Tải bản sao lưu về** chỉ tải tệp dump, không đóng gói kèm thư mục tệp tài liệu số. Phục hồi tại chỗ đã tự tải lại tệp (JN5); gói cả kho đối tượng vào một lượt tải HTTP là hàng chục gigabyte đi qua một kết nối trình duyệt.

**Đã làm nốt trong cùng ngày:** phần nhập bộ định nghĩa MARC 21 chuẩn — xem JN13.

### J.P — Yêu cầu phi chức năng và đối chiếu thẳng Chương V (05/09/2026)

Hai đợt cuối đổi cách đọc: đợt trước đọc mục 6 của `CLAUDE.md`, đợt này đọc thẳng
`Chương V.YÊU CẦU VỀ KỸ THUẬT.pdf` — bản gốc của hồ sơ mời thầu. Cách sau bắt được cả những yêu cầu
mà đặc tả nội bộ không chép lại: hồ sơ bàn giao, kế hoạch đào tạo, cam kết bảo hành.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| JP1 | Nghiêm trọng | **Cấu hình Nginx chạy thật không có Content-Security-Policy** — đúng header chống XSS chính, trên trang có trình soạn thảo WYSIWYG. Bản dùng lúc phát triển thì có | Thêm luật vào `nginx.conf` và `nginx.behind-proxy.conf`, quên `nginx.prod.conf`. Cùng lớp lỗi với JM5 trong cùng một ngày, và cùng lớp với bài học 9 | Thêm đủ bộ header vào cả hai `location` phục vụ trang đơn của bản chạy thật. Kèm phép thử quét `NginxConfigParityTests`: sáu luật chung phải có mặt ở **cả ba** tệp cấu hình, đỏ ngay khi bỏ một header khỏi một tệp |
| JP2 | Nghiêm trọng | **Phạm vi dữ liệu theo dạng tài liệu không bật được**, dù đặc tả liệt kê đủ ba chiều (kho, thư viện, loại tài liệu) và bộ lọc toàn cục đã đọc chiều ấy từ trước | Màn hình Người dùng chỉ có hai ô chọn; chiều thứ ba được giữ lại lúc lưu nhưng không tạo cũng không sửa được — làm xong phần khó rồi bỏ dở phần dễ | Ô chọn thứ ba trên màn hình, lấy danh sách từ chính danh mục dạng tài liệu. `DataScopeTests.Gan_pham_vi_dang_tai_lieu_thi_chi_thay_bieu_ghi_dung_dang_ay` kiểm cả danh sách lẫn lượt gọi thẳng bằng mã (404, không phải 200), và kiểm rằng người không gán phạm vi vẫn thấy đủ |
| JP3 | Nghiêm trọng | **Bảy đường xuất dữ liệu không ghi nhật ký**, trong đó có đường xuất biểu ghi ra ISO 2709 — đường mang cả mục lục ra khỏi hệ thống. Mục 6.2 nêu đích danh "xuất dữ liệu" | Mỗi handler phải tự gọi bộ ghi nhật ký; bảy chỗ quên gọi. Không có gì chặn chỗ thứ tám quên tiếp | `ExportAuditBehaviour` trong đường ống MediatR ghi mọi lượt trả về tệp, nhận diện theo **kiểu trả về** chứ không theo tên lệnh. Handler đã tự ghi dòng riêng (kèm bộ lọc, kèm mã bản ghi) thì bộ dùng chung im lặng — một lượt xuất một dòng |
| JP4 | Nghiêm trọng (trang công khai) | **Menu chính của trang tra cứu không thao tác được bằng bàn phím**: `<span onClick>` không nằm trong thứ tự tab, không phím nào kích hoạt | Bộ trợ giúp `clickable` đã có và dùng ở thẻ ngành, dòng môn học — nhưng menu dựng trước đó và không ai rà lại | Menu dùng `clickable`; kèm phép thử quét `keyboard.test.ts` cấm mọi `div`/`span` có `onClick` mà thiếu `clickable` hoặc thiếu bộ ba `role` + `tabIndex` + `onKeyDown`. Phép thử phải cắt đúng thẻ mở đầu: cắt tới dấu `>` đầu tiên là dừng ngay giữa `onClick={() => ...}` và bỏ sót phần khai sau đó |
| JP5 | Cao | **Hai đường tải tệp chỉ tin `Content-Type` do máy khách gửi** — logo thư viện (nhúng vào biểu mẫu in và trang công khai) và bản scan biên bản bàn giao. Mục 6.4 đòi kiểm magic number | Hai chỗ này viết sau, và bảng chữ ký byte có tới bốn bản sao rải rác nên không ai thấy thiếu | Cả hai kiểm bằng chữ ký byte; bộ nhận dạng học thêm TIFF cho bản scan. `AcquisitionTests.Tep_dinh_kem_phai_dung_chu_ky_byte_chu_khong_chi_dung_nhan` gửi tệp chữ khoác nhãn PDF và nhãn PNG, đỏ trước khi sửa |
| JP6 | Vừa | **Mã dạng tài liệu của trang Luận văn viết cứng trong mã nguồn** (`LUANVAN`, `LUANAN`, `THESIS`, `DISSERTATION`), trong khi dạng tài liệu là danh mục nghiệp vụ cán bộ tự khai | Trái mục 1 gạch 9: không hardcode danh mục nghiệp vụ. Thư viện đặt mã "LV" hay "LATS" thì trang ấy rỗng vĩnh viễn và không sửa được từ giao diện | Tham số `OPAC.THESIS_DOCUMENT_TYPES`. `ContentAndOpacTests.Dang_tai_lieu_tinh_la_luan_van_khai_duoc_trong_tham_so` lập một dạng tài liệu mã lạ, chứng minh trang rỗng trước và có kết quả ngay sau khi khai thêm mã |
| JP7 | Vừa | **Hai thẻ báo cáo chỉ có bảng và tệp, thiếu dạng đồ họa** (dung lượng tài liệu số theo định dạng; ĐKCB hủy bỏ), trong khi yêu cầu chung của Chương V đòi đủ ba dạng đầu ra | Hai thẻ viết sau, không dùng lại thành phần biểu đồ đã có ngay trong cùng màn hình | Cả hai vẽ biểu đồ bằng đúng thành phần sẵn có; thẻ ĐKCB hủy bỏ gom theo hình thức ngay tại máy khách, không thêm lượt gọi nào |
| JP8 | Vừa | **Danh mục không in ra giấy được**, dù yêu cầu chung của Chương V ghi "cho phép in, xóa danh mục nếu đủ thẩm quyền" | Chỉ có xuất Excel — tệp để sửa hàng loạt, không phải tờ giấy mang đi ký hay dán ở quầy | `format=Pdf` trên chính endpoint xuất, dựng bằng bộ kết xuất PDF dùng chung (có tiêu đề thư viện, tổng số giá trị, người in); nút **In** bên cạnh nút Xuất Excel |
| JP9 | Vừa | **Serilog chưa bao giờ ghi xuống PostgreSQL** dù bảng công nghệ ghi "Serilog → file + PostgreSQL"; gói đã cài nhưng không nối dây | Cấu hình chỉ khai hai sink Console và File | Nối sink, **và** phát hiện hai lỗi mà bản dựng không nói: bộ ghi dùng COPY nhị phân gọi `NpgsqlBinaryImporter.Complete()` theo chữ ký Npgsql 7 (dự án dùng Npgsql 8), và bộ ghi thời gian đưa `DateTimeOffset` lệch +07 xuống cột chỉ nhận UTC. Cả hai ném ở **mỗi lô** mà Serilog nuốt lỗi, nên biểu hiện duy nhất là bảng rỗng mãi. Chuyển sang INSERT và một bộ ghi thời gian UTC riêng; bật SelfLog ra stderr để lần sau sink chết thì nói ra ngay. Đã kiểm trên container đang chạy: 4 dòng vào bảng, 0 ngoại lệ |
| JP10 | Vừa | **Thiếu hẳn bốn hồ sơ mà Chương V đòi**: kế hoạch triển khai kèm đối soát dữ liệu (mục III.1), kế hoạch đào tạo (III.2), cam kết bảo hành và mức phản hồi sự cố (III.3), hồ sơ bàn giao và biểu mẫu nghiệm thu (mục 5.5) | Đặc tả nội bộ `CLAUDE.md` chỉ chép phần chức năng của Chương V, bỏ mục III và mục 5. Bảy tài liệu bàn giao làm theo đặc tả nội bộ nên cũng thiếu đúng chừng ấy | Bốn tài liệu mới `docs/10` → `docs/13`, và bảng đáp ứng có thêm hai mục E, F đối chiếu mục III và mục 5 — nay bảng đi đúng thứ tự **toàn bộ** Chương V |


## K. Nghiệm thu thử trên máy chủ thật (05/09/2026)

Tám đợt rà trước đều chạy trên máy phát triển. Đợt này chạy trên chính bản mà hội đồng sẽ chấm —
`thuvien.bluestar.com.vn`, ảnh `524ad90`, dữ liệu 12.608 biểu ghi — theo đúng lối người dùng đi:
gọi API bằng tài khoản đúng vai (quản trị, cán bộ lưu thông, hai bạn đọc thử), rồi mở trình duyệt
đi qua trang chủ, tra cứu, chi tiết, đăng nhập bạn đọc, trang quản trị, trình soạn MARC, quầy lưu
thông. Mọi dữ liệu ghi thêm mang dấu `NTT` và đã xoá sau khi xong; kết quả từng kịch bản ở phụ lục
cuối `06-kich-ban-kiem-thu.md`. Buổi chiều: 223 kịch bản chạy bằng máy, 222 đạt, cộng phần đi bằng trình
duyệt tìm ra **5 lỗi** (K1–K5). Buổi tối chạy tiếp **test sâu từng dòng của Chương V** bằng luồng ghi
thật — đơn đặt từ yêu cầu tới biên bản, kiểm kê từ đóng kho tới quyết định mất, đầu báo từ sinh số tới
đóng tập, tài liệu số từ tải lên tới thu hồi, sao lưu thật, biểu mẫu in — thêm khoảng 150 kịch bản,
**không thấy dòng nào của Chương V thiếu chức năng**, tìm thêm **1 lỗi** (K6). Khi đưa bản sửa K6 lên
máy chủ thì lộ lỗi vận hành K7: ổ đĩa đầy vì kịch bản triển khai không dọn ảnh cũ. Đợt **test kỹ thuật**
cuối ngày đi vào lớp mà kịch bản nghiệp vụ không chạm — tranh chấp đồng thời, truy cập chéo bạn đọc, giả
token, tiêm mã, đầu vào lạ, đường dẫn vượt thư mục, nhất quán CSDL, việc nền Hangfire, nhật ký lỗi máy chủ,
quét 56 màn hình quản trị và 14 trang tra cứu bắt lỗi console, Lighthouse, bộ kiểm thử mobile — bảo mật
và hiệu năng đều đạt, tìm thêm K8 (đặt giữ đồng thời lọt hai phiếu) và K9 (ba cặp màu dưới WCAG AA).
Đợt kỹ thuật thứ hai đi vào cổng mạng lộ ra ngoài, TLS, hạn mức API công khai, phạm vi dữ liệu theo kho,
ghi nhật ký lượt xem, cài mới trên CSDL trắng với bộ dữ liệu mẫu, Lighthouse giao diện quản trị, độ liên
quan tra cứu và đường đi của thư điện tử — tìm thêm K10 (gõ đủ nhan đề ra 0 kết quả), K11 (màu quản
trị), K12 (báo "đã gửi" khi SMTP tắt) và K13 (tham số SMTP không ai đọc). Đợt kỹ thuật thứ ba ("kiểm tra tất cả
chức năng kỹ thêm lần nữa") đi vào tạo đồng thời trên mọi khoá duy nhất, nội dung thật của 10 loại tệp in và xuất,
phục hồi sao lưu trên máy phát triển, và quầy lưu thông đi bằng bàn phím trên trình duyệt — tìm thêm K14 (biên mục
sơ lược song song cùng tác giả mới thì ba lượt đổ) và, lúc dọn dữ liệu thử của chính K14, K15 (biểu ghi đã xoá vẫn
được đếm là "đang dùng" nên không xoá được tác giả). Cuối ngày dựng **camera thật của máy ảo Android** — chèn ảnh mã vạch
và mã QR vào cảnh ảo bằng `-virtualscene-poster` — để đi ba luồng quét mà tám đợt trước đều phải bỏ qua; luồng thứ ba lộ K16
(mượn tự phục vụ quét được mã trạm nhưng không quét nổi cuốn sách). Ngày 06/09/2026 chạy tiếp một đợt **soi số học nghiệp vụ**
— tự tính kỳ vọng từ bảng chính sách và tham số rồi đối chiếu với hệ thống: hạn trả qua ngày nghỉ, tiền phạt theo ngày mở cửa,
trần gia hạn, trần số cuốn, hàng đợi giữ chỗ, đền sách mất, ngưỡng nợ, thẻ hết hạn, ra trường còn nợ, sinh số báo theo ba kỳ hạn,
phân loại kiểm kê. Mười sáu phép đo khớp, một sai: K17. Soi tiếp quyền đọc tài liệu số (hết hạn tự thu, trần lượt xem, trang xem thử) và đối chiếu số liệu báo cáo với truy vấn SQL độc lập (điều 2.8 của E-HSMT) — tám con số
của trang Tổng quan khớp tuyệt đối — thì lộ thêm K18: 94 phiếu mượn của bộ dữ liệu trình diễn mang ngày ở tương lai. Kiểm lại chính bản sửa K18 trên máy chủ thật thì
phát hiện K19: hai migration sửa dữ liệu không chạy vì thiếu thuộc tính `[Migration]`. Đợt kiểm thêm cùng ngày soi bốn việc chạy
nền theo lịch (chưa có phép thử nào cho tới lúc ấy) và hai giao thức liên thư viện bằng **máy khách của người khác** — thư viện
`sickle` cho OAI-PMH, `pymarc` cho MARCXML — thì OAI-PMH đạt cả sáu verb, còn SRU lộ K20. Thử nốt máy khách Z39.50 trên máy chủ
thật: tra Thư viện Quốc hội Mỹ lấy được biểu ghi thật và nhập vào kho, nhưng một máy chủ mẫu không nối được (K21). Mở nốt gói
"xuất toàn bộ dữ liệu khi kết thúc hợp đồng" ra đếm từng dòng thì lộ K22: gói thiếu lịch sử của bạn đọc đã xoá hồ sơ.
Đợt soi bảo mật tiếp theo (giả thẻ đăng nhập, truy cập chéo bạn đọc, lọc mã độc trong trình soạn nội dung, tải tệp giả đuôi, ZIP
vượt thư mục, vòng đời thẻ làm mới, khoá tài khoản khi dò mật khẩu) đạt hết, chỉ lộ K23: giờ hiện cho người dùng là giờ UTC.
Cả 23 đã sửa.

| Mã | Mức | Lỗi | Nguyên nhân | Sửa |
|---|---|---|---|---|
| K1 | Nghiêm trọng (trang công khai) | **Gõ sai mật khẩu vài lần liền là nhận trang HTML "503 Service Temporarily Unavailable" của Nginx** thay vì thông báo tiếng Việt. Bộ giới hạn trong API trả 429 kèm JSON, nhưng `limit_req` của Nginx đứng trước nó và mặc định trả 503 — trang tra cứu và ứng dụng di động chờ JSON nên chỉ hiện được "máy chủ lỗi". Hội đồng thử "đăng nhập sai 5 lần" ở mục 2.3 sẽ thấy đúng trang ấy | Không ai đọc `limit_req_status` mặc định của Nginx; máy phát triển không có `limit_req` nên không tái hiện được | `limit_req_status 429` và trang lỗi 429 dạng JSON theo khuôn `ApiResponse` ở cả hai tệp có siết tần suất. `NginxConfigParityTests.Tep_nao_siet_tan_suat_thi_phai_tra_429_kem_json` đỏ trước khi sửa |
| K2 | Nghiêm trọng | **Bạn đọc đặt giữ được biểu ghi không có bản in nào.** Trên máy chủ thật có hơn 7.000 biểu ghi thu hoạch qua OAI-PMH chỉ có siêu dữ liệu; trang tra cứu vẫn hiện nút "Đặt giữ" và máy chủ nhận phiếu, xếp bạn đọc vào hàng đợi chờ một cuốn sách không bao giờ về | Luật đặt giữ kiểm chính sách, hạn mức, trùng phiếu, đang mượn — nhưng không hỏi câu đầu tiên: thư viện có bản in không. Kho phát triển mọi biểu ghi mẫu đều có ĐKCB nên không ai gặp | Máy chủ từ chối 409 khi biểu ghi không có bản in nào ngoài Mất/Thanh lý; trang tra cứu ẩn nút khi `itemCount = 0`. `AcceptanceRehearsalTests.Dat_giu_bieu_ghi_chua_co_ban_in_nao_thi_bi_tu_choi` đỏ trước khi sửa |
| K3 | Nặng (dữ liệu trình diễn) | **162 thẻ bạn đọc mẫu hết hạn đúng ngày 05/09/2026**, ba thẻ đã hết hạn từ tháng 8. Từ hôm sau, một phần tư bạn đọc mẫu bị quầy từ chối và OPAC cảnh báo "thẻ hết hạn" — hội đồng sẽ kết luận dữ liệu chưa sẵn sàng | Bộ dữ liệu trình diễn viết cứng bốn khóa 2021–2024, thẻ hết hạn ngày 05/09 của năm nhập học cộng năm; nạp năm 2025 thì đẹp, tới 2026 là khóa đầu rụng | Trên máy chủ: gia hạn 165 thẻ thêm 12 tháng bằng chính chức năng gia hạn hàng loạt (kịch bản BD.18). Trong mã: `DemoReaderCohort` tính khóa theo ngày nạp, khóa cũ nhất còn ít nhất một năm thẻ; `DemoReaderCohortTests` đỏ với ngày 05/09/2026 trước khi sửa |
| K4 | Vừa (ấn tượng đầu) | **Trang Tổng quan của giao diện quản trị vẫn mang dòng giữ chỗ từ phase 1**: "Hệ thống đang trong quá trình bàn giao theo từng phân hệ…", kèm ba con số về quyền của chính tài khoản. Màn hình đầu tiên sau đăng nhập nói phần mềm chưa xong | Viết ở phase 1 để hội đồng kiểm quyền, rồi không ai quay lại vì mọi đợt rà đi thẳng vào từng phân hệ | Trang Tổng quan hiện số liệu hoạt động từ đầu năm lấy từ báo cáo tổng quan (dùng chung với mục Báo cáo thống kê), kèm lối mở báo cáo; tài khoản không có quyền báo cáo thì chỉ thấy quyền của mình. `DashboardPage.test.ts` cấm dòng giữ chỗ quay lại |
| K6 | Vừa | **In lại phiếu mượn / phiếu trả của bạn đọc đã xóa hồ sơ thì máy chủ đổ 500 "lỗi hệ thống"** (tìm ra ở đợt test sâu buổi tối, khi in phiếu `PM00003117` của bạn đọc thử đã xóa) | Câu hỏi ghép bạn đọc bằng phép nối trong; hồ sơ xóa mềm bị bộ lọc toàn cục loại ra nên danh sách rỗng, mã lấy phần tử đầu và ném `IndexOutOfRange`. Bộ kiểm thử chỉ in phiếu của bạn đọc còn sống | Danh sách rỗng thì trả 404 "không tìm thấy bạn đọc của phiếu"; `AcceptanceRehearsalTests.In_phieu_muon_cua_ban_doc_da_xoa_ho_so_thi_bao_khong_tim_thay_chu_khong_do_500` đỏ trước khi sửa |
| K7 | Nặng (vận hành) | **Lượt triển khai bản sửa K6 đổ vì ổ đĩa máy chủ đầy 100%** — "no space left on device" khi kéo ảnh API; hệ thống vẫn chạy bản cũ nên người dùng không thấy gì, nhưng mọi bản sửa từ đây không lên được nữa | Mỗi lượt CI/CD kéo ba ảnh gắn tag theo mã commit (ảnh API 1,37 GB); kịch bản `gh-deploy.sh` chỉ gọi `docker image prune -f`, vốn chỉ dọn ảnh không tag. Hai ngày với 21 lượt triển khai để lại 20 bộ ảnh cũ — 27 GB, cộng 10 GB bộ đệm build — trên ổ 96 GB dùng chung với năm ứng dụng khác | Dọn tay trên máy chủ (còn trống 18 GB); `gh-deploy.sh` thêm bước `don_anh_cu` sau khi lên thành công: xoá mọi ảnh `libraryconnect-*` trừ bản mới và bản ngay trước (giữ để quay lại), không đụng ảnh ứng dụng khác. `DeployScriptTests` quét kịch bản, đỏ trước khi sửa |
| K8 | Nghiêm trọng | **Ba lượt "Đặt giữ" bấm cùng lúc từ một bạn đọc tạo được hai phiếu** (200, 200, 409) — đợt test kỹ thuật buổi tối, thử tranh chấp đồng thời trên máy chủ thật. Luật "một bạn đọc một phiếu đang chờ cho một tài liệu" chỉ kiểm ở tầng nghiệp vụ | Đúng bài học số 1: kiểm rồi mới ghi thì hai yêu cầu song song đều thấy "chưa có". Phiếu mượn đã có `ux_loans_item_dang_muon` nên hai quầy ghi mượn cùng bản chỉ lọt một (T.1 đạt), còn đặt giữ thì chưa có ràng buộc | Chỉ mục duy nhất `ux_holds_reader_bib_dang_cho` trên (reader_id, bib_id) lọc `status IN ('Waiting','Ready')`; migration hủy phiếu trùng có sẵn, giữ phiếu sớm nhất, kèm lý do. Cùng migration tính lại `item_count`/`available_item_count` cho mọi biểu ghi vì kiểm nhất quán CSDL thấy một biểu ghi ghi 5 bản trong khi có 3. `AcceptanceRehearsalTests.Hai_phieu_dat_giu_dang_cho…` ghi thẳng hai phiếu qua DbContext, đỏ trước khi sửa |
| K9 | Vừa (mục 6.6) | **Lighthouse trên trang tra cứu thật đo ba cặp màu dưới WCAG AA**: nút Tra cứu vàng chữ trắng 3,25:1; chữ mờ ở nhãn nhóm bộ lọc và bộ đếm facet 3,13:1 (chữ 11 px, không được hưởng ngưỡng 3); dòng cuối chân trang 4,01:1. Kèm ô chọn "Sắp xếp" không có nhãn cho trình đọc màn hình, và liên kết "Chi tiết" bọc nút có vùng chạm cao 0,1 px | Phép thử tương phản chỉ đo 16 cặp nó được liệt kê; ba cặp này không có trong danh sách, và một cặp còn được cho hưởng ngưỡng 3 của "chữ phụ" dù chữ chỉ 11 px. Bài học 19 đúng thêm lần nữa: nền giấy làm mọi cặp tối đi | Vàng `#b9852f → #9a6c1c` (4,63), vàng rê chuột `#8a6114` (5,53), chữ mờ `#9a8f7c → #7f7461` (4,52), dòng chân trang `#8f8a76 → #a8a28e` (5,44); đo lại sau lần sửa đầu còn lộ cặp thứ tư — chữ phụ `#7a6f5f` trên nền trang (không phải trên giấy) 4,29 — hạ xuống `#6f6556` (đạt trên cả giấy, nền trang và nền thẻ). Đổi ở cả `styles.css`, `theme.ts`, `lib/palette.ts`. `aria-label` cho ô sắp xếp; nút "Chi tiết" điều hướng thẳng thay vì bọc trong thẻ `a`. `theme.test.ts` thêm đúng năm cặp Lighthouse đo, đỏ với số đo cũ trước khi sửa. Điểm Accessibility trang tra cứu đo trên máy chủ thật: 88 → 93 (lần sửa đầu) → **100** (bản `78614e4`); hai mục Lighthouse còn lại — liên kết phân trang của Ant Design không có `href`, tệp `llms.txt` — không nằm trong yêu cầu |
| K10 | Nghiêm trọng (trang công khai) | **Gõ đúng nhan đề "Cơ sở dữ liệu — lý thuyết và bài tập" ra 0 kết quả**; "cơ sở dữ liệu bài tập" cũng 0; `"cơ sở dữ liệu"` trong ngoặc kép 0; trong khi "lý thuyết và" ra 112. Bạn đọc gõ vài từ nhớ được của nhan đề, không gõ đúng thứ tự và dấu câu | Phạm vi "Tất cả" và "Nhan đề" so cả cụm từ khóa như **một chuỗi con liền nhau** trên cột gộp: dấu gạch giữa hai cụm, dấu ngoặc, hay đảo thứ tự từ là mất hết. Kho phát triển nhan đề ngắn nên không ai gõ đủ dài để lộ | Tách từ khóa thành từng từ (bỏ dấu câu, giữ `.` và `-` cho DDC và ISBN), mỗi từ phải có mặt, ở đâu cũng được. Lần sửa đầu so từng từ như chuỗi con: kiểm trên máy chủ thấy "cơ sở dữ liệu" vọt 45 → 805 vì "co" trúng "công", "so" trúng "số" — âm tiết tiếng Việt quá ngắn để so chuỗi con. Lần hai: từ hai từ trở lên so **trọn từ** bằng biểu thức chính quy có biên từ (`\m…\M`, pg_trgm vẫn dùng chỉ mục cho `~`), từ dài hơn bốn ký tự so tiền tố ("system" ↔ "systems"); một từ giữ so chuỗi con như cũ. `Tra_cuu_nhieu_tu_dao_thu_tu_hoac_kem_dau_cau_van_tim_thay` (bốn cách gõ) và `Tra_cuu_nhieu_tu_so_tron_tu_khong_bat_am_tiet_nam_trong_tu_khac` đều đỏ trước khi sửa |
| K11 | Vừa (mục 6.6) | **Lighthouse trang Tổng quan quản trị: mô tả phụ trên nền trang 2,78:1, tiêu đề ô thống kê 3,13:1**; nút tài khoản ở thanh trên có tên truy cập không chứa chữ nhìn thấy (biểu tượng "user" của Avatar chen vào trước tên) | Cùng lớp K9 nhưng ở gói quản trị — phép thử quét chỉ chặn đúng thư mục nó quét (bài học 9); một cặp còn được cho hưởng ngưỡng 3 dù là chữ 12 px | Chữ phụ `#7a6f5f → #625848`, chữ mờ `#9a8f7c → #6f6556` ở `styles.css`, `theme.ts`, `lib/palette.ts`; `aria-label` cho nút tài khoản, biểu tượng `aria-hidden`. `theme.test.ts` của gói quản trị thêm ba cặp đo được, đỏ trước khi sửa. Accessibility Tổng quan: 95 → đo lại sau triển khai |
| K12 | Vừa | **"Gửi giỏ tài liệu qua email" báo "Đã gửi danh sách tới …" trong khi máy chủ thật chưa cấu hình SMTP** — bộ gửi im lặng bỏ qua, bạn đọc chờ một lá thư không bao giờ tới | Bộ gửi tắt thì `return` lặng, không ai hỏi nó có bật không (bài học 11: "đã lưu" chưa phải "đã đến") | `IEmailSender.IsEnabledAsync`; gửi giỏ hỏi trước, chưa cấu hình thì 409 "Thư viện chưa cấu hình máy chủ gửi thư…". Nhắc quá hạn không đổi vì đi qua kênh thông báo trong ứng dụng, bạn đọc vẫn nhận được. `Gui_gio_tai_lieu_khi_chua_cau_hinh_smtp…` đỏ trước khi sửa |
| K13 | Nặng | **Tám ô "Cấu hình email SMTP" trên màn hình Tham số hệ thống là công tắc chết**: lưu vào CSDL, không nơi nào đọc — bộ gửi chỉ đọc mục `Smtp` của appsettings/biến môi trường. Cán bộ khai máy chủ thư trên màn hình thì thư vẫn không đi | Bài học 30 đúng thêm một lần: thêm ô cấu hình mà không chỉ ra chỗ đọc. Kho phát triển và CI đều không có SMTP nên không ai gửi được thư để thấy | `SmtpSettingsResolver`: tham số đã điền thắng, ô trống rơi về appsettings; bộ gửi đọc lại mỗi lần gửi (đổi là có tác dụng ngay, bài học 31); lỗi kết nối máy chủ thư trả 409 nêu rõ host:port thay vì 500. `SmtpSettingsResolverTests` (3) và `Cau_hinh_smtp_tren_man_hinh_tham_so_la_thu_bo_gui_thu_doc` — trỏ tới `127.0.0.1:9` rồi đòi lỗi kết nối nêu đúng địa chỉ ấy; đỏ trước khi sửa vì bộ gửi vẫn nói "chưa cấu hình" |
| K14 | Vừa | **Bốn lượt biên mục sơ lược cùng lúc, cùng một tác giả chưa có trong hồ sơ thẩm quyền: một lượt lưu được, ba lượt đổ 409 "Giá trị đã tồn tại (ràng buộc `ux_author_code`)"** — cán bộ không nhập mã tác giả nào mà bị bảo nhập mã khác. Đề mục, từ khoá, nhà xuất bản, tùng thư sinh từ biểu ghi đều cùng cảnh; hai người cùng kiểm nhận một lô sách của một tác giả là chuyện thường ở buổi nhập kho | `BibAuthorityLinker` tra không thấy thì tạo; hai lượt cùng tra không thấy là cùng tạo, ràng buộc duy nhất chặn lượt sau — đúng bài học 1 — nhưng câu trả lời đúng lúc ấy là *dùng mục người kia vừa tạo*, không phải báo lỗi. Bộ kiểm thử chỉ gọi tuần tự nên không bao giờ thấy | `CatalogRaceReconciler` cắm vào `SaveChangesAsync`: đổ ở ràng buộc duy nhất của một bảng `cat.*` thì đối chiếu lại từng mục đang chờ thêm vào bảng ấy — có mục còn sống cùng khoá tên thì nhận khoá của mục ấy và trỏ lại mọi khoá ngoại đang tham chiếu, chỉ trùng mã (hai tên khác nhau cắt về cùng 40 ký tự) thì sinh mã có hậu tố — rồi lưu lại, tối đa ba lần. Nhật ký đã dựng cho lượt đổ được bỏ đi để không ghi đôi và không ghi "tạo" một mục không tồn tại. Một chỗ cho mọi bảng danh mục và mọi handler, kể cả tạo tay ở màn hình danh mục. `Bien_muc_so_luoc_song_song_cung_mot_tac_gia_moi_thi_ca_bon_luot_deu_luu_duoc` — bốn yêu cầu thật gửi song song: đỏ trước khi sửa (3 × 409), xanh sau; kiểm lại trên máy chủ thật, dòng K14 ở phụ lục `docs/06` |
| K15 | Vừa | **Xoá biểu ghi rồi vẫn không xoá được tác giả chỉ biểu ghi ấy dùng**: "Giá trị này đang được 1 bản ghi sử dụng" — mà cán bộ không còn nhìn thấy bản ghi nào. Cùng cảnh với đề mục, từ khoá, phân loại, bộ sưu tập, môn học | Liên kết `bib_authors` không xoá mềm theo biểu ghi (đúng — biểu ghi khôi phục được thì liên kết phải còn), nhưng bộ đếm `CatalogUsageService` đếm liên kết chứ không hỏi biểu ghi còn sống không. Lộ ra khi dọn dữ liệu thử của K14 trên máy chủ thật | Sáu phép đếm liên kết chỉ đếm dòng mà biểu ghi còn trong `BibRecords` (đã mang bộ lọc xoá mềm); màn hình gộp trùng dùng cùng bộ đếm nên cột "đang dùng" cũng đúng theo. `Xoa_bieu_ghi_roi_thi_tac_gia_chi_bieu_ghi_ay_dung_phai_xoa_duoc`: đỏ trước khi sửa (409 "đang được 1 bản ghi sử dụng"), xanh sau |
| K16 | Nặng | **Mượn tự phục vụ: quét được mã trạm nhưng không quét nổi cuốn sách.** Sau khi xác thực trạm, khung quét bước 2 đen kịt kèm câu "Chưa được phép dùng camera" — trong khi quyền camera đang bật. Bạn đọc vào Cài đặt, thấy quyền đã bật, và hết đường đi tiếp; muốn mượn phải gõ tay mã vạch | Hai bộ điều khiển camera: trang quét mã trạm dựng bộ riêng và chỉ nhả khi hoạt cảnh đóng trang chạy xong, mà màn chính đã mở bộ thứ hai ngay khi máy chủ xác thực xong. Bộ thứ hai không giành được camera. Lời báo lại đổ mọi lỗi cho quyền — màn Quét mã phân biệt đúng, màn này thì không: cùng một luật viết hai nơi, một nơi sai | Một bộ điều khiển dùng chung cho cả hai bước (trang quét trạm mượn lại, không dispose); ô báo lỗi tách thành `CameraErrorView` dùng chung, chỉ lỗi quyền mới nói về quyền, còn lại nói đúng lỗi của bộ quét (thêm chuỗi `scanCameraUnavailable`). Nhân tiện: mã trạm lọt vào khung quét sách nay bị bỏ qua bằng `ScanCode.classify` thay vì hỏi máy chủ rồi hiện dòng đỏ "không tìm thấy ấn phẩm". `camera_error_view_test.dart` — ba phép thử widget cho lời báo, hai phép thử **quét mã nguồn** (mọi `errorBuilder` phải dựng `CameraErrorView`; màn Mượn tự phục vụ chỉ được có một `MobileScannerController`): đỏ trước khi sửa, xanh sau. Kiểm lại trên máy ảo Android gọi máy chủ thật: quét QR trạm → quét mã vạch → "Đã mượn · hạn trả 14/09/2026" (MB.44) |
| K17 | Nặng | **Kiểm kê xếp sách đang ở tay bạn đọc vào danh sách "thiếu"**, và lệnh "xử lý thiếu" ghi mất luôn cả những cuốn ấy trong khi phiếu mượn vẫn đang mở. Đo trên kho phát triển: một kỳ toàn kho đếm **157 cuốn đang mượn là thiếu**; bấm một nút là 157 quyết định mất sai, và bản ghi "mất" chặn chính lượt trả sau đó | Danh sách kỳ vọng chốt lúc tạo kỳ chỉ loại bản đã thanh lý và đã ghi mất, không loại bản đang mượn. Kho phát triển các đợt trước không có kỳ kiểm kê nào chạy trên kho có nhiều sách đang lưu thông, nên con số không lộ ra | Danh sách kỳ vọng loại luôn bản còn phiếu mượn mở — kiểm kê đếm cái **trên giá**. Cuốn sổ ghi đang mượn mà thật ra nằm trên giá thì lúc quét hiện "thừa", đúng thứ cần biết vì nghĩa là có lượt trả chưa ghi. Chốt chặn thứ hai: `resolve-missing` bỏ qua mọi dòng còn phiếu mượn mở, che cho những kỳ lập trước bản sửa. Migration `20260906020000_KiemKeBoSachDangMuon` dọn các kỳ **chưa đóng** và trừ lại số kỳ vọng; kỳ đã đóng giữ nguyên vì đó là biên bản đã ký. `Kiem_ke_khong_duoc_coi_sach_dang_muon_la_thieu`: đỏ trước khi sửa, xanh sau |
| K18 | Vừa | **Bộ dữ liệu trình diễn có 94 phiếu mượn mang ngày ở tương lai** (xa nhất 29/11/2026), mà cột ngày trả lại nằm trước ngày mượn: mở lịch sử một bạn đọc là thấy "mượn 29/11/2026, trả 02/09/2026". Có cả trên máy chủ thật | Nhóm "trả muộn" chỉ chiếm 20% số lượt nhưng công thức rải nó trọn khoảng thời gian của cả bộ, nên đuôi vọt qua hôm nay tới một phần sáu khoảng. Không phép thử nào hỏi "có lượt mượn nào ở tương lai không" — dữ liệu mẫu vẫn bị coi là thứ ngoài sản phẩm, đúng bài học 6 | Tách công thức thành `DatabaseSeeder.NgayMuonDemo`, rải đúng khoảng của từng nhóm, kèm chốt chặn `Math.Min(offset, 0)`. `DemoLoanDatesTests` chạy qua ba cỡ bộ dữ liệu × năm chính sách: đỏ trên công thức cũ (mượn 09/09 khi hôm nay 06/09), xanh sau. Migration `20260906030000_SuaPhieuMuonNgayTuongLai` kéo phiếu cũ về quá khứ, giữ nguyên độ dài phiếu và cho hạn trả rơi đúng ngày đã trả; chạy thử trong giao dịch trên kho phát triển: 94 → 0 dòng sai |
| K19 | Nặng | **Hai migration sửa dữ liệu được commit, dựng ảnh, triển khai — và không chạy.** Máy chủ khởi động, ghi "Cơ sở dữ liệu đã ở phiên bản mới nhất", dữ liệu sai vẫn nguyên: 94 phiếu mượn ngày tương lai còn y nguyên trên máy chủ thật sau khi bản sửa K18 đã lên. Không một tiếng động nào báo hỏng | Migration viết tay thiếu thuộc tính `[DbContext(typeof(LibraryConnectDbContext))]` và `[Migration("<mã>")]`. EF Core chỉ nhận lớp có thuộc tính ấy; lớp kế thừa `Migration` mà không khai mã thì bị bỏ qua **trong im lặng**. `CLAUDE.md` có nhắc phải sửa `[Migration("…")]` khi chen migration vào giữa, nhưng không có gì chặn lúc viết mới | Thêm thuộc tính cho cả hai; và `MigrationRegistrationTests` quét thư mục migration: mỗi tệp `<mã>_<Tên>.cs` phải có lớp mang đúng `[Migration("<mã>")]`, và không lớp `Migration` nào trong assembly được thiếu thuộc tính. Bỏ thuộc tính đi thì phép thử đỏ ngay |
| K20 | Nặng | **SRU im lặng khi thư viện bạn hỏi sai.** Đo trên máy chủ thật: truy vấn `(dc.title="a" and` trả về **12.060 biểu ghi — toàn bộ kho** thay vì báo lỗi cú pháp; chỉ mục lạ `dc.khongco=abc` trả về hai biểu ghi bất kỳ; `version=9.9` được phục vụ như thể là 1.2; quan hệ so sánh `>` được nhận rồi đối xử như dấu bằng. Thư viện đối tác nhận đủ kết quả nên tưởng mình hỏi đúng | Bộ tách từ **bỏ hẳn dấu ngoặc**, và mọi thứ không hiểu được đều rơi vào nhánh "từ khóa trần", nên câu hỏi hỏng biến thành câu hỏi rộng. Tầng SRU không kiểm phiên bản, không kiểm tên chỉ mục. Phép thử cũ chỉ hỏi "câu đúng có ra kết quả không", chưa bao giờ hỏi "câu sai có bị chặn không" | `CqlParser.KiemCuPhap` bắt ngoặc lệch, ngoặc lồng nhiều toán tử, toán tử đứng cuối và hai toán tử khác nhau trong một câu; quan hệ ngoài `=` trả chẩn đoán 19; `TryMapIndex` nhận ra chỉ mục lạ để SRU trả chẩn đoán 16; phiên bản ngoài 1.1/1.2 trả chẩn đoán 5. `CqlSyntaxTests` (18 trường hợp) và `SRU_hoi_sai_thi_tra_chan_doan_dung_ma_chu_khong_tra_ket_qua` (5 trường hợp qua HTTP) |
| K21 | Vừa | **Một trong ba máy chủ liên thư viện mẫu không nối được**: `z3950.library.yale.edu:7090` mở cổng nhưng đóng phiên ngay sau bắt tay, nên màn hình "Kiểm tra kết nối" hiện một dòng đỏ ngay trong buổi nghiệm thu | Chính sách của thư viện bạn, không phải lỗi mã: đo từ máy chủ thật ngày 06/09/2026, hai máy chủ công khai khác (BnF, Thư viện Quốc gia Úc) cũng từ chối phiên ẩn danh, trong khi Thư viện Quốc hội Mỹ trả lời tốt qua cả Z39.50 lẫn SRU. Dữ liệu mẫu là một phần của sản phẩm (bài học 6) | Yale chuyển sang trạng thái **tắt**, tên ghi rõ lý do; thay chỗ nó trong danh sách đang chạy bằng kho `LCDB_MARC8` của chính Thư viện Quốc hội Mỹ (đo được 949.926 kết quả). Sửa cả bộ dữ liệu mẫu lẫn dữ liệu trên máy chủ thật; ba máy chủ đang bật đều trả lời tốt |
| K22 | Nặng | **Gói "xuất toàn bộ dữ liệu khi kết thúc hợp đồng" bỏ sót lịch sử của bạn đọc đã xoá hồ sơ.** Đo trên kho phát triển: gói thiếu đúng 10 lượt mượn, 4 khoản phạt, 3 phiếu đặt giữ — bằng đúng số bản ghi thuộc hồ sơ xoá mềm. Thư viện nhận bàn giao mất lịch sử của mọi bạn đọc từng bị xoá mà không biết | Bộ lọc xoá mềm của EF Core lan từ **thực thể cha bắt buộc** sang bản ghi con: lọc mất bạn đọc là lọc mất luôn phiếu mượn của họ. Cùng lớp lỗi với K15 (bộ đếm "đang dùng") và K6 (in phiếu của bạn đọc đã xoá) — cả ba đều là hệ quả của một quy tắc EF Core mà không ai đối chiếu lại khi viết truy vấn xuất | Năm truy vấn xuất (bạn đọc, ấn phẩm, lượt mượn, phạt, đặt giữ) dùng `IgnoreQueryFilters()` rồi tự lọc theo `DeletedAt` của **chính bản ghi**; hồ sơ bạn đọc đã xoá vẫn vào gói kèm cột "Đã xóa hồ sơ" để bên nhận hiểu, và không còn phiếu mượn nào trỏ tới số thẻ vắng mặt. `Goi_ban_giao_giu_du_lich_su_cua_ban_doc_da_xoa_ho_so`: đỏ trước khi sửa, xanh sau |
| K23 | Vừa | **Giờ hiện cho người dùng là giờ UTC, lệch bảy tiếng.** Bạn đọc nhập sai mật khẩu năm lần lúc 09:39 nhận câu "Tài khoản tạm khóa tới **02:44** 06/09/2026" — một mốc đã trôi qua bảy tiếng trước; họ thử lại ngay, vẫn bị từ chối, và không hiểu vì sao. Cùng lỗi ở câu khoá tài khoản cán bộ và ở ba cột ngày giờ của tệp xuất Excel (ngày mượn, ngày trả, ngày tải lên tài liệu số) | Npgsql chuẩn hoá `timestamptz` về UTC khi đọc, nên in thẳng ra là in UTC. Phần lớn kho mã gọi `ToLocalTime()`, năm chỗ này quên — và không có gì chặn | Năm chỗ đổi sang giờ máy; `LocalTimeInMessagesTests` quét cả hai lối in giờ (chuỗi nội suy `{…:HH:mm}` và `.ToString("…HH…")`) trong ba dự án, đòi phải qua `ToLocalTime()` hoặc lấy từ đồng hồ hệ thống. Bỏ một bản sửa ra thì phép thử đỏ ngay và chỉ đúng tên tệp |
| K5 | Vừa | **Trình soạn MARC báo "1 lỗi phải sửa trước khi lưu: thiếu 001" trên mọi biểu ghi mới**, dù bấm Lưu vẫn xong vì đường lưu tự cấp số kiểm soát trước khi kiểm. Cán bộ hoặc tự bịa một số 001, hoặc học cách bỏ qua ô đỏ — cả hai đều tệ | Đường lưu cấp 001 rồi mới kiểm; endpoint kiểm tra riêng (trình soạn gọi sau mỗi lần gõ) thì không. Nhập ISO 2709 đã gặp đúng lỗi này và tự lọc thông báo 001 ở chỗ của nó thay vì sửa gốc | Endpoint kiểm tra bỏ lỗi thiếu 001, vì đó là số hệ thống cấp. `AcceptanceRehearsalTests.Kiem_tra_bieu_ghi_khong_bao_loi_thieu_001_vi_he_thong_tu_cap` đỏ trước khi sửa |

### Đã kiểm trên máy chủ thật và vẫn tốt

- Bốn header bảo mật (kể cả Content-Security-Policy) có ở cả trang tra cứu lẫn trang quản trị; HTTP
  chuyển hướng 308 sang HTTPS; `/health` bị Caddy ẩn ra ngoài theo chủ ý, bên trong trả `Healthy`
  cho PostgreSQL, Redis, MinIO.
- Tra cứu không dấu ra đúng 45 kết quả như có dấu; gợi ý, facet đếm khớp kết quả lọc, nâng cao
  VÀ/HOẶC/KHÔNG, giới hạn năm; tra cứu 0,04–0,42 giây đo cả đường mạng.
- SRU (explain, MARCXML, Dublin Core, không dấu, phân trang, diagnostic) và OAI-PMH đủ sáu verb kèm
  `resumptionToken`, lọc thời gian, `badVerb`, nhận POST.
- Kết nối thật tới Thư viện Quốc hội Mỹ qua Z39.50: 949.926 kết quả thử, 11.534 kết quả "Vietnam",
  lấy được biểu ghi; tab "Tìm ở thư viện khác" của OPAC cũng vậy.
- Luồng ghi trọn vẹn: tạo nhóm và tài khoản (nhật ký ghi Thêm mới, Phân quyền, Đăng nhập thất bại,
  không lộ mật khẩu), cán bộ lưu thông bị 403 đủ 5 endpoint quản trị, cấp thêm quyền có hiệu lực sau
  đăng nhập lại; bạn đọc tạo mới → đổi mật khẩu bắt buộc → thẻ điện tử → mượn 2 bản → gia hạn → bạn
  đọc khác đặt giữ → gia hạn bị chặn vì có người đợi → trả có cảnh báo giữ sách → phiếu chuyển Sẵn
  sàng → huỷ → ghi mất → thu phạt → ra vào cổng → tủ gửi đồ → giấy xác nhận trả sách.
- Biểu ghi MARC tạo qua API, xuất ISO 2709 và MARCXML rồi cho `pymarc` đọc lại đúng nhan đề tiếng
  Việt; sửa biểu ghi đã có thêm trường 700 lưu được (H4 không quay lại); lịch sử phiên bản có diff.
- 23 báo cáo trả bảng, 14 lượt xuất PDF/Excel mở được (kiểm chữ ký `%PDF` / `PK`), kể cả in thẻ,
  tem, nhãn, phích, giấy xác nhận, phiếu chuyển kho.
- Trang tĩnh nháp không lộ ra công khai, đăng thì thấy ngay; mã độc trong nội dung bị lọc.

### Ghi nhận, chưa sửa (dữ liệu trình diễn trên máy chủ)

- Thư viện chưa tải logo, chưa có banner, chưa khai giá (`shelves` = 0) — bản đồ kho trống.
- "Sách mới bổ sung" ở trang chủ toàn sách tiếng Anh từ Open Library, vì đó là lượt nạp gần nhất.
- Danh mục có 10 bộ sưu tập nhưng chưa biểu ghi nào được gắn, nên mục "Duyệt theo bộ sưu tập" rỗng.

## L. Đợt rà sâu theo phân hệ trên máy chủ thật (06–07/09/2026)

Chín đợt trước đi theo đặc tả và theo luồng nghiệp vụ chính. Đợt này chọn đúng những vùng mà chín
đợt ấy **chạm ít nhất**: phân hệ VIII (quản trị nội dung), tủ gửi đồ và cổng ra vào của phân hệ VII,
mục lục bài trích, danh mục tự tạo từ trường MARC, kiểm kê nạp tệp từ máy đọc rời, năm trình thiết
kế biểu mẫu, đường ống xử lý tài liệu số, và quy trình duyệt mua nhiều cấp. Toàn bộ chạy trên
`thuvien.bluestar.com.vn` — ảnh `128fe02`, cùng mã với `main` vì ba commit sau đó chỉ sửa tài liệu.

Cách làm: kịch bản Python gọi API bằng tài khoản đúng vai, ghi dữ liệu thật rồi dọn; chỗ nào con số
đáng ngờ thì đối chiếu thẳng bằng `psql` trong container. **191 phép đo, 7 lỗi mã nguồn** (L1–L7)
cộng hai việc phải làm trên chính máy chủ (L8, L9). Kết quả từng phép đo ở phụ lục cuối
`06-kich-ban-kiem-thu.md`.

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại | Trạng thái |
|---|---|---|---|---|---|---|
| L1 | Trang chủ tra cứu · Thư viện ảnh · Tin tức | Nội dung trang thư viện của bản trình diễn — banner trang chủ, album ảnh sự kiện, sáu bản tin có chuyên mục — nằm **sau rào "chỉ nạp khi kho biểu ghi còn trống"**. Máy chủ nghiệm thu nạp biểu ghi thật từ ngày đầu nên rào ấy đóng vĩnh viễn: trang chủ không có banner nào, trang Thư viện ảnh rỗng, và hai bản tin duy nhất không mang chuyên mục nên bộ lọc chuyên mục tin trên trang tra cứu cũng rỗng. Mã nguồn có sẵn cả ba, không ai gọi tới. | Trên máy chủ: `GET /api/public/banners` trả `[]`, `GET /api/public/galleries` trả `[]`, `GET /api/public/news/categories` trả `[]` trong khi danh mục có 4 chuyên mục. `select count(*) from web.cms_banners where deleted_at is null` = 0. | Vừa | Dữ liệu | Đã sửa — tách phần nội dung trang thư viện ra trước rào (mỗi phần vẫn tự có rào riêng nên chạy lại được); `AcceptanceRehearsalTests.Noi_dung_trang_thu_vien_van_duoc_nap_khi_kho_bieu_ghi_da_co_du_lieu` |
| L2 | Ấn phẩm định kỳ → Mục lục bài trích | Bài trích đã sinh biểu ghi riêng thì không gỡ khỏi mục lục được — đúng, và câu chặn bảo cán bộ "Hãy xóa biểu ghi ở phân hệ Biên mục trước". **Làm đúng như vậy rồi vẫn bị chặn y nguyên**: bộ kiểm chỉ nhìn cột `bib_id`, mà cột ấy vẫn trỏ tới biểu ghi đã xóa mềm. Lối đi duy nhất mà thông báo chỉ ra không dẫn tới đâu, và bài trích khóa cứng trong mục lục vĩnh viễn. | Nhập một bài trích → "Sinh biểu ghi" → xóa biểu ghi ấy ở Biên mục (200, "Đã xóa biểu ghi") → gỡ bài khỏi mục lục: vẫn 409 với đúng câu cũ. | Vừa | Nghiệp vụ | Đã sửa — hỏi biểu ghi còn sống không thay vì chỉ nhìn cột khóa ngoại; `SerialTests.Articles_become_analytic_records…` |
| L3 | Tài liệu số → danh sách và trang tra cứu | Sáu tài liệu số của bộ dữ liệu trình diễn được nạp thẳng vào kho đối tượng, **không đi qua đường ống xử lý**: có tệp gốc và số trang (bộ gieo tự đặt) nhưng không có ảnh bìa và không có bản chữ dùng để tìm toàn văn. Chính lời chú trong bộ gieo nói "có tệp thật thì bước dựng ảnh bìa mới chạy được" — bước ấy không ai gọi. | `GET /api/digital/documents/{id}/thumbnail` trả **404 cho cả sáu** tài liệu trên máy chủ; `files` của mỗi tài liệu chỉ có `Original`. Cùng lúc, tải một tệp PDF lên qua giao diện thì 3 giây sau đã có `Thumbnail` — đường ống chạy tốt, chỉ bộ gieo không dùng. | Vừa | Dữ liệu | Đã sửa — bộ gieo cho tài liệu đi qua đúng đường ống ấy, kèm phần dựng lại cho bản đã cài. Lượt sửa đầu lọc theo ghi chú của bộ minh họa và **trượt đúng một trong sáu**: một lượt `PUT` từ máy khách khác đã xoá trắng ghi chú của tài liệu ấy (bài học 33). Lượt sau bỏ hẳn cách lọc theo ghi chú, chỉ hỏi "tệp PDF nào còn thiếu ảnh bìa", và chặn trần 20 tài liệu mỗi lần khởi động; `AcceptanceRehearsalTests.Tai_lieu_so_minh_hoa_thieu_anh_bia…` |
| L4 | Bổ sung → Duyệt yêu cầu đặt mua | Thư viện hạ số cấp duyệt từ 2 xuống 1 trong khi đang có yêu cầu **đã qua cấp 1**: yêu cầu ấy đã đi đủ số cấp đang khai, nhưng lượt bấm duyệt tiếp vẫn cộng thêm một cấp rồi trả 409 *"Bạn đã duyệt cấp trước của yêu cầu này; cấp tiếp theo phải do người khác duyệt."* Cấp tiếp theo ấy không còn tồn tại, nên yêu cầu nằm lại hàng chờ vĩnh viễn — không ai duyệt được nữa, kể cả quản trị. | Đặt `ACQ.APPROVAL_LEVELS=2`, gửi duyệt một yêu cầu, duyệt cấp 1, đặt lại tham số về `1`, bấm duyệt: 409, trạng thái vẫn `Submitted`, `approvalLevel` vẫn 1. | Vừa | Nghiệp vụ | Đã sửa — đã qua đủ số cấp đang khai thì lượt bấm là lượt chốt, không cộng cấp và không đòi người thứ hai; `AcquisitionTests.Ha_so_cap_duyet_thi_yeu_cau_dang_duyet_do_khong_bi_ket` |
| L5 | Biên mục → Nhập dữ liệu từ biểu ghi ISO 2709 | **Một biểu ghi hỏng trong tệp kéo theo biểu ghi lành đứng ngay sau nó.** Biểu ghi bị bộ kiểm tra từ chối đã được `Add` và dựng xong quan hệ *trước* bước kiểm tra, mà nhánh trả về "không hợp lệ" không dọn gì cả — chỉ nhánh ném ngoại lệ mới dọn bộ theo dõi. Lượt lưu của biểu ghi kế tiếp vì thế mang luôn biểu ghi hỏng đi ghi: hoặc đổ vì ràng buộc cơ sở dữ liệu (mất biểu ghi lành), hoặc **ghi được** một biểu ghi mà hệ thống vừa nói là không hợp lệ. Rồi lần ghi tiến độ kế tiếp gặp đúng biểu ghi ấy và đánh đổ cả tác vụ. | Nhập một tệp có 12.610 biểu ghi trên máy chủ thật: tác vụ dừng ở trạng thái **Thất bại** sau 268 biểu ghi. Danh sách lỗi cho thấy đúng dạng: dòng 240 lỗi "trường 856 thiếu $u" → dòng 242 đổ vì lỗi ghi cơ sở dữ liệu; lặp lại ở 354/355, 554/555. | **Nặng** | Nghiệp vụ | Đã sửa — nhánh "không hợp lệ" dọn bộ theo dõi rồi gắn lại dòng nhật ký tác vụ, đúng như nhánh ngoại lệ vẫn làm; `BibImportTests.Mot_bieu_ghi_hong_khong_keo_theo_bieu_ghi_lanh_dung_sau_no` |
| L6 | Biên mục → Nhập dữ liệu, báo cáo lỗi | Báo cáo lỗi của lượt nhập hiện nguyên văn tiếng Anh của Entity Framework cho cán bộ thư viện đọc: *"An error occurred while saving the entity changes. See the inner exception for details."* Đây đúng lớp lỗi mà C4 đã sửa một lần và có phép thử quét chặn — nhưng phép thử ấy quét thông báo của API, không chạm tới danh sách lỗi do tác vụ nền ghi ra. | Xem báo cáo lỗi của tác vụ nhập nói ở L5. | Vừa | Ngôn ngữ | Đã sửa — lỗi ghi cơ sở dữ liệu nói bằng tiếng Việt và nêu ba nguyên nhân hay gặp; nguyên văn giữ trong nhật ký máy chủ. Bộ ghi tiến độ của lượt nhập Excel cũng gắn lại dòng nhật ký sau khi dọn, để thanh tiến độ không đứng im |

| L7 | Trang chủ tra cứu → banner · Thư viện ảnh | **Chữ tiếng Việt hai dấu trên ảnh minh họa hiện dấu rời.** Ảnh banner và ảnh album của bộ dữ liệu trình diễn là SVG nhúng thẳng vào địa chỉ, và dòng nhan đề khai `font-family='Georgia,serif'`. Georgia có sẵn trên mọi máy Windows nên trình duyệt dùng thật — mà Georgia **thiếu glyph dựng sẵn của ố, ề, ắ, ữ**: nó tách thành nguyên âm một dấu cộng dấu thanh rời lấy từ bộ chữ khác, đặt cạnh nhau chứ không chồng lên. Hai tệp giao diện cũng xếp Georgia ngay sau `'Lora'`, nên chỉ cần một lần phông web không tải được là mọi tiêu đề rơi vào đúng cái bẫy ấy. | Mở `https://thuvien.bluestar.com.vn/` bằng trình duyệt: banner đầu trang hiện **"Tài liệu sô ́ mới cập nhật"**. Đo bằng canvas ngay trên trang ấy: `measureText('ố')` ở Georgia = 60,27 px trong khi `measureText('ô')` = 31,27 px — gần gấp đôi; ở `'Lora'`, ở `'Times New Roman'` và ở `serif` mặc định thì hai số bằng nhau. | Vừa | Ngôn ngữ | Đã sửa — ảnh minh họa dùng `Times New Roman,Times,serif`; hai tệp giao diện bỏ Georgia khỏi danh sách dự phòng. Phép thử quét `VietnameseFontStackTests` cấm gọi tên Georgia ở cả ba nơi |


### Hai việc phải làm trên chính máy chủ, không phải lỗi mã

| # | Việc | Tình trạng |
|---|---|---|
| L8 | **Dữ liệu thử của các đợt rà trước còn nằm trên máy chủ nghiệm thu**: 5 nhà cung cấp `Nhà cung cấp NTx… (ngừng dùng)`, 9 yêu cầu đặt mua và 3 đơn đặt mang lý do "Nghiệm thu sâu" / "Nhập từ tệp Excel", 2 biểu ghi tạp chí thử, một chủ đề thử. Chúng nằm lẫn trong màn hình Bổ sung và trong Báo cáo duyệt mua — hội đồng mở ra là thấy. | Đã dọn bằng xóa mềm, đúng lối mà thư viện xóa trên màn hình, nên vẫn lần lại được nếu cần |
| L9 | **Nhánh III.1 trên máy chủ rỗng sau khi dọn**: bộ dữ liệu trình diễn không sinh yêu cầu đặt mua, đơn đặt hay biên bản bàn giao — mục 8 của đặc tả không đòi những thứ này, nên đây không phải lỗi mã và cũng không thêm mã. Nhưng bảy màn hình của III.1 mở ra là bảng rỗng và Báo cáo duyệt mua toàn số 0. | Đã dựng bộ dữ liệu trình diễn bằng chính API: 4 yêu cầu ở đủ bốn trạng thái (Nháp, Chờ duyệt, Đã duyệt, Từ chối kèm lý do), 1 đơn đặt đã nhận đủ kèm số hợp đồng, 1 biên bản bàn giao. Báo cáo duyệt mua nay có số thật: 4 yêu cầu, 10.394.000 đ đề nghị, 2.320.000 đ đã duyệt, tỷ lệ duyệt 50% |

### Đã kiểm trong đợt này và vẫn tốt

- **Phân hệ VIII đầy đủ**: tin hẹn giờ chưa tới hạn không lọt ra trang công khai kể cả khi gõ thẳng
  đường dẫn; kéo ngày về quá khứ thì hiện ngay; nút Đăng/Gỡ đổi trạng thái đúng chiều; đếm lượt xem
  tăng theo từng lượt đọc; banner hết hạn và banner chưa tới ngày đều bị ẩn, banner không đặt ngày
  thì luôn hiện; tắt banner/liên kết/menu thì trang công khai không còn; menu con nằm đúng dưới menu
  cha; trang tĩnh chưa đăng không đọc được từ ngoài; thẻ `script`, thuộc tính `onerror`, liên kết
  `javascript:` và `iframe` bị lọc sạch mà phần lành giữ nguyên.
- **Tủ gửi đồ và cổng ra vào**: giao tủ theo số thẻ, tủ đang có người thì lượt thứ hai bị chặn, tủ
  báo hỏng không giao được, trả tủ đóng đúng lượt và tủ về "Trống"; quét thẻ ở cổng lần đầu là vào
  lần sau là ra, thẻ không có thật báo lỗi rõ nghĩa; hai báo cáo có số liệu theo giờ và theo khu vực.
- **Bảy báo cáo lưu thông** xuất Excel ra tệp thật (chữ ký `PK`), mã QR của trạm mượn tự phục vụ ra
  ảnh PNG thật.
- **Trang tra cứu**: bảy chiều facet đều có bộ đếm; gợi ý tự động chạy với chữ không dấu gõ dở; năm
  kiểu sắp xếp cho kết quả khác nhau; sáu kiểu trích dẫn (APA, MLA, Chicago, BibTeX, RIS, EndNote)
  đều ra nội dung và tải được tệp RIS; yêu thích bấm lần nữa thì bỏ ra; tìm kiếm đã lưu giữ đúng cả
  cờ cảnh báo; `sitemap.xml` 2,4 MB và `robots.txt` trỏ đúng. Gửi email danh sách giỏ tài liệu khi
  chưa cấu hình SMTP thì **nói thẳng là chưa gửi được**, không báo "đã gửi".
- **Nhận xét tài liệu**: bật tham số `OPAC.ALLOW_REVIEW` lên thì bạn đọc gửi được, nhận xét vào hàng
  chờ duyệt chứ không hiện ngay, duyệt xong mới lên trang công khai và điểm trung bình đổi theo; đã
  trả tham số về giá trị cũ sau khi kiểm.
- **Bài trích và danh mục tự tạo**: bài trích sinh biểu ghi mang trường 773 đủ `$t`, `$g`, `$x` và
  bạn đọc tra được ngay trên trang công khai; danh mục tự tạo từ `260$a` quét ra 5 giá trị duy nhất
  và **xuất hiện thật** trong bộ lọc của trang tra cứu (`custom:NOI_XUAT_BAN…`).
- **Kiểm kê**: phạm vi theo khoảng số ĐKCB chốt đúng danh sách kỳ vọng; nạp tệp quét từ máy đọc rời
  phân loại đúng khớp / thừa / sai kho; bản đang ở tay bạn đọc không bị xếp vào "thiếu" (K17 vẫn
  đúng); xuất kết quả ra Excel được.
- **Năm trình thiết kế**: mẫu phích (in và xem trước ngay từ trình soạn khi chưa lưu), mẫu thẻ bạn
  đọc khổ CR80, mẫu tem mã vạch, mẫu nhãn gáy, và trình thiết kế biểu mẫu dùng chung với 11 loại
  chứng từ — tất cả tạo mới được và in ra PDF thật. Sinh ảnh mã vạch CODE39, CODE128 và QR đều ra PNG.
- **Đường ống tài liệu số**: tải một tệp PDF lên máy chủ thật thì trong 3 giây có số trang, ảnh bìa,
  checksum SHA-256 **khớp với checksum tự tính trên tệp gốc**, và trình đọc từng trang trả ảnh PNG.
- **Chuỗi đơn đặt trọn vẹn**: yêu cầu → gửi duyệt → duyệt hai cấp (cấp 2 đòi người khác, đúng ý đồ
  "hai cặp mắt") → lập đơn theo nhà cung cấp → in đơn PDF → nhận một phần → nhận đủ → biên bản bàn
  giao → in biên bản. Nhận quá số đặt bị chặn kèm câu nêu đúng số; sinh ĐKCB đòi chọn kho và báo rõ
  dòng nào chưa biên mục.
- **Giao thức trên bản chạy thật**: OAI-PMH `Identify`, `ListRecords` (50 biểu ghi + thẻ đọc tiếp),
  `ListMetadataFormats` (oai_dc, marc21, marcxml), verb sai trả `badVerb`; SRU trả MARCXML cho câu
  đúng, trả **chẩn đoán** cho câu sai cú pháp và cho chỉ mục lạ (K20 vẫn đúng), `explain` khai báo
  được. Xuất toàn kho ra ISO 2709 (17,9 MB) và MARCXML (41,3 MB).
- **Sao lưu trên máy chủ thật**: bấm "Sao lưu ngay" → việc chạy nền → bản sao lưu 44,8 MB ở trạng
  thái Thành công; đã xóa bản thử sau khi kiểm.
- **Nhập bạn đọc**: tệp Excel mẫu tải được, hồ sơ ánh xạ cột lưu lại được, nhập ảnh hàng loạt từ ZIP
  báo đúng ảnh nào không khớp bạn đọc nào, và có đầu mối đồng bộ từ hệ thống quản lý đào tạo.

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

Cộng cả ba đợt, đợt áp thiết kế, đợt triển khai và ba đợt rà hoàn thiện ngày 04/09/2026:
**147 lỗi, đã sửa 145**; thêm **23 lỗi của đợt nghiệm thu thử, test sâu, ba đợt test kỹ thuật ngày
05/09/2026 và ba đợt soi nghiệp vụ – giao thức – bảo mật ngày 06/09/2026 (mục K), đã sửa cả 23** — tổng **170 lỗi, đã sửa 170**. Hai mục H3 và H9 đã làm xong ngày 03/09/2026 và ghi ở cột cuối
của chính hai dòng ấy — con số 134 giữ nguyên cách đếm cũ để đối chiếu được với các bản trước.
Mỗi lỗi đã sửa đều có phép thử chạy đỏ trước khi sửa và xanh sau khi sửa, kể cả H7: phép thử giả
tiêu đề đỏ trước khi sửa `CurrentUser.Ip`.

Ba đợt rà ngày 04/09/2026 đi theo một cách làm: đọc **từng gạch đầu dòng** của đặc tả rồi tìm bằng
chứng trong mã, thay vì đọc mã rồi hỏi nó có đúng không. Cách ấy bắt được đúng loại lỗi mà bộ kiểm
thử không bao giờ chạm tới — công tắc được lưu mà không ai đọc (JN3, JN6), cờ cấu hình chỉ có tác
dụng sau khi khởi động lại (JN4), và chức năng làm xong một nửa mà màn hình báo là đã xong (JN1).
Ba lần liền, chỗ hỏng nằm ở khoảng giữa hai lớp: máy chủ nhận trường mà giao diện không gửi, hoặc
giao diện gửi mà máy chủ không đọc.

### Làm tiếp gì sau đây

1. **H3, H9 và I7 đã làm xong** ngày 03/09/2026 — xem cột cuối của ba dòng ấy trong bảng lỗi.
2. **I8 đã làm xong** cùng ngày. Còn một hệ quả chưa dọn: những bản sao lưu tạo **trước** thay đổi
   này vẫn mang schema `hangfire` bên trong. Phía phục hồi đã loại nó ra nên không hại gì, nhưng
   các tệp ấy lớn hơn mức cần thiết.
3. **II.5 đã làm xong** cùng ngày (JN13). Bộ định nghĩa nạp lại được từ giao diện, cả kiểu nạp bổ
   sung lẫn kiểu khôi phục ghi đè. Chưa có: nhập một bộ định nghĩa **của thư viện khác** từ tệp tải
   lên — chưa gặp nhu cầu ấy, và bộ chuẩn đi kèm bản cài là thứ mọi thư viện Việt Nam dùng chung.
4. **Ứng dụng di động (Phase 15) đã xong** — xem `CLAUDE.md` mục A.1 và `mobile/README.md`. Sau
   đó là năm đợt rà theo đặc tả ngày 04–05/09/2026 (mục J), không còn lỗi mở.
5. **Máy chủ Z39.50 trên bản chạy thật giữ ở trạng thái tắt** — đọc lại Chương V ngày 05/09/2026:
   hồ sơ chỉ đòi chiều máy khách. Chi tiết ở `CLAUDE.md` mục A.5.
6. Bài học giữ lại từ ba đợt: **lỗi ghi là "đã sửa" chưa chắc đã sửa hết** (B12 dưới D8, H5 dưới lần
   sửa bỏ dấu), và **phép thử tự viết chỉ chạm tới bối cảnh người viết nghĩ ra** — H4 sống từ phase 5
   vì phép thử sửa biểu ghi chưa bao giờ thêm một điểm truy cập; H5 sống qua một lần sửa vì phép thử
   dùng kho trống. Dựng đúng bối cảnh của kho thật (220 tác giả cùng họ) mới bắt được.
