# **LibraryConnect** — PHẦN MỀM THƯ VIỆN SỐ CHUẨN KẾT NỐI LIÊN THƯ VIỆN

> Tài liệu này vừa là đặc tả gốc theo hồ sơ mời thầu, vừa là bản hướng dẫn làm việc trên kho mã.
> Bản đặc tả nguyên văn chưa chỉnh sửa nằm ở `PROMPT-BUILD-LIBRARYCONNECT.md`.
>
> **Đọc phần A trước khi làm bất cứ việc gì.** Phần 0–13 phía sau là đặc tả yêu cầu, giữ nguyên để
> đối chiếu với `docs/07-bang-dap-ung-ky-thuat.md` khi nộp thầu.

---

## A. TÌNH HÌNH HIỆN TẠI — ĐỌC TRƯỚC

### A.1. Đã xong tới đâu

Phần **web đã dựng xong toàn bộ Phase 1–14**: mười phân hệ I–X chạy thật, `docker compose up -d` là
lên hệ thống hoàn chỉnh. **Phân hệ XI (ứng dụng di động Flutter, `mobile/`) đã làm xong cả 10 bước** của
`PROMPT-MOBILE-LIBRARYCONNECT.md` trên Android: 76 phép thử đơn vị/widget, 12 luồng đầu-cuối chạy trên
máy ảo với máy chủ Docker thật (`docs/06`, MB.01–MB.33), APK/AAB release dựng được. **iOS** dựng và
chạy trên iPhone Simulator của máy Mac GitHub Actions (`.github/workflows/ios.yml`, MB.34–MB.40), kể
cả ba luồng ghi dữ liệu vào máy chủ thật bằng một bạn đọc kiểm thử riêng. Chưa có: máy iPhone thật,
IPA ký (không có tài khoản Apple Developer), nhận thông báo đẩy FCM thật, quét trên **sách thật** bằng camera phần cứng — ghi
rõ trong `docs/06`/`docs/07`, không đánh "Đạt". Cách làm việc, cạm bẫy dựng Android, cách chụp ảnh bằng
`flutter drive`: `mobile/README.md`.

Sau khi xong Phase 14 đã chạy thêm **một đợt rà soát chất lượng toàn diện** — mở hệ thống như người
dùng thật, đi hết từng màn hình, cố tình đi đường sai, gọi thẳng API không qua giao diện, và nạp dữ
liệu thật từ nguồn ngoài. Đợt rà ấy tìm ra **36 lỗi**, phần lớn là lỗi mà bộ kiểm thử cũ không bao
giờ chạm tới vì nó chỉ xác nhận mã nguồn làm đúng thứ người viết *nghĩ*.

Sau đó là **đợt hoàn thiện** sáu ưu tiên: bảo mật, bộ ánh xạ Dublin Core → MARC 21, ảnh bìa, bộ dữ
liệu trình diễn lớn, rà soát lần hai, và làm sạch bảng đáp ứng. Rồi **đợt áp bản thiết kế** (mục G
sổ lỗi) và **đợt rà thứ ba** (mục H) đi vào sáu chỗ hai đợt trước chưa tới — trình soạn MARC bằng
chuột, ba trình thiết kế mẫu, kiểm kê, đóng tập, trình đọc có chữ chìm, sao lưu – phục hồi — và tìm
ra hai lỗi nghiêm trọng đã sống từ phase 5: sửa biểu ghi đã có thì không lưu được, và địa chỉ IP người
dùng vừa giả được vừa bị nhốt chung một ngăn giới hạn tốc độ. Trang tra cứu đã áp lại theo dự án
Claude Design "LibraryConnect layout design".

Ngày 04–05/09/2026 chạy tiếp **năm đợt rà theo đặc tả**: đọc từng gạch đầu dòng rồi tìm bằng chứng
trong mã, thay vì đọc mã rồi hỏi nó có đúng không. Ba đợt đầu soi mười một phân hệ, đợt thứ tư soi
yêu cầu phi chức năng, đợt cuối đọc thẳng `Chương V.YÊU CẦU VỀ KỸ THUẬT.pdf` — bản gốc của hồ sơ mời
thầu, thứ mà chính tài liệu này chỉ chép lại phần chức năng.

Năm đợt tìm thêm **38 lỗi** mà 1.073 phép thử không bao giờ chạm tới, vì chúng nằm ở khoảng giữa hai
lớp: công tắc được lưu mà không ai đọc (ghi nhật ký lượt xem, danh mục tự tạo làm bộ lọc, phạm vi dữ
liệu theo dạng tài liệu), cờ cấu hình chỉ có tác dụng sau khi khởi động lại (lịch sao lưu), chức năng
làm xong một nửa mà màn hình báo là đã xong (biểu ghi bài trích ở trạng thái Nháp nên bạn đọc không
tra được), thông báo đẩy của ứng dụng di động chưa bản dựng nào chạy được vì thiếu một trình cắm
Gradle, luật thêm vào hai trong ba tệp cấu hình Nginx (chính sách nội dung mất ở đúng bản chạy thật),
và một sink nhật ký ném ở mỗi lô mà thư viện log nuốt lỗi nên bảng cứ rỗng.
Cộng tất cả: **147 lỗi, đã sửa 145**.

Chiều 05/09/2026 chạy **nghiệm thu thử trên máy chủ thật** (`thuvien.bluestar.com.vn`, mục K sổ
lỗi): 223 kịch bản của `docs/06` chạy bằng máy qua API với tài khoản đúng vai, cộng trình duyệt đi
qua tám màn hình chính. Tìm ra **5 lỗi** mà tám đợt trên máy phát triển không thấy, vì chúng chỉ có
ở bản chạy thật: hạn mức đăng nhập của Nginx trả trang HTML 503 thay vì JSON 429; đặt giữ được
biểu ghi không có bản in nào (7.000 biểu ghi thu hoạch chỉ có siêu dữ liệu); 162 thẻ bạn đọc mẫu
hết hạn đúng hôm ấy vì khóa viết cứng; trang Tổng quan còn dòng giữ chỗ "đang bàn giao" từ phase 1;
trình soạn MARC báo lỗi thiếu 001 trên mọi biểu ghi mới. Tối cùng ngày chạy **test sâu từng dòng của
Chương V** bằng luồng ghi thật trên máy chủ (đơn đặt → biên bản, kiểm kê → quyết định mất, đầu báo → đóng
tập, tài liệu số → thu hồi quyền, sao lưu thật, nhập Excel/ZIP, biểu mẫu in): **không dòng nào của Chương V
thiếu chức năng**, tìm thêm một lỗi (in phiếu mượn của bạn đọc đã xóa hồ sơ đổ 500). Đưa bản sửa lên
máy chủ thì lộ lỗi vận hành thứ bảy: ổ đĩa đầy vì kịch bản triển khai không dọn ảnh cũ (20 bộ ảnh,
27 GB). Đợt **test kỹ thuật** cuối ngày (tranh chấp đồng thời, truy cập chéo, giả token, tiêm mã, nhất quán
CSDL, Hangfire, quét 70 màn hình bắt lỗi console, Lighthouse) tìm thêm hai lỗi: đặt giữ đồng thời lọt hai
phiếu vì thiếu ràng buộc duy nhất, và ba cặp màu OPAC dưới WCAG AA. Đợt kỹ thuật thứ hai (cổng mạng, TLS,
hạn mức, phạm vi dữ liệu theo kho, cài mới CSDL trắng, Lighthouse quản trị, độ liên quan, đường đi của
thư) tìm thêm bốn: gõ đủ nhan đề ra 0 kết quả vì tra cứu so cả cụm như chuỗi con; màu quản trị; báo "đã
gửi" khi SMTP tắt; tám ô SMTP trên màn hình không ai đọc. Đợt kỹ thuật thứ ba (tạo đồng thời trên mọi khoá duy nhất,
nội dung thật của tệp in/xuất, phục hồi sao lưu, quầy bàn phím trên trình duyệt) tìm thêm một: bốn lượt biên mục sơ
lược song song cùng một tác giả mới thì ba lượt đổ trùng mã; và lúc dọn dữ liệu thử của chính lỗi ấy lộ thêm một: biểu
ghi đã xoá vẫn được đếm là "đang dùng" nên không xoá được tác giả. Cuối ngày quét mã **bằng camera của máy ảo Android**
(chèn ảnh mã vạch/QR vào cảnh ảo) và đi trọn luồng mượn tự phục vụ trên máy chủ thật — tìm thêm một: khung quét bước 2
không mở được camera vì trang trước chưa nhả, lại báo nhầm thành thiếu quyền. Ngày 06/09/2026 soi **số học nghiệp vụ** — tự
tính kỳ vọng từ bảng chính sách rồi đối chiếu: hạn trả qua ngày nghỉ, tiền phạt, trần gia hạn, hàng đợi giữ chỗ, đền sách mất,
ngưỡng nợ, thẻ hết hạn, sinh số báo, kiểm kê — 16 phép đo khớp, một sai: kiểm kê coi sách đang ở tay bạn đọc là thiếu (157 cuốn
trên kho phát triển) và lệnh xử lý thiếu ghi mất luôn cả chúng. Đối chiếu tiếp số liệu tám ô của trang Tổng quan với SQL độc lập
(điều 2.8) thì khớp tuyệt đối, nhưng lộ ra 94 phiếu mượn của bộ dữ liệu trình diễn mang ngày ở tương lai, ngày trả nằm trước ngày
mượn. Kiểm lại chính bản sửa ấy trên máy chủ thật thì lộ lỗi thứ mười chín: hai migration sửa dữ liệu **không chạy** vì thiếu
thuộc tính `[Migration]`, mà máy chủ vẫn ghi "đã ở phiên bản mới nhất". Đợt kiểm thêm soi bốn việc chạy nền theo lịch và hai
giao thức liên thư viện bằng máy khách của người khác (`sickle` cho OAI-PMH, `pymarc` cho MARCXML): OAI-PMH đạt cả sáu verb,
SRU thì trả **toàn bộ kho** cho một truy vấn sai cú pháp và không báo lỗi cho chỉ mục lạ. Máy khách Z39.50 tra Thư viện Quốc hội
Mỹ lấy được biểu ghi thật và nhập vào kho; một máy chủ mẫu (Yale) đóng phiên ẩn danh nên chuyển sang tắt và thay bằng kho khác
của Thư viện Quốc hội Mỹ. Mở gói "xuất toàn bộ dữ liệu khi kết thúc hợp đồng" ra đếm từng dòng thì thấy nó thiếu lịch sử của
bạn đọc đã xoá hồ sơ. Đợt soi bảo mật (giả thẻ đăng nhập, truy cập chéo, lọc mã độc, tải tệp giả đuôi, ZIP vượt thư mục, vòng đời
thẻ làm mới, khoá tài khoản) đạt hết, chỉ lộ một lỗi: giờ hiện cho người dùng là giờ UTC, lệch bảy tiếng. Cả 23 đã sửa,
tổng **170 lỗi, đã sửa 170**.

Ngày 06–07/09/2026 chạy tiếp **đợt rà sâu theo phân hệ trên máy chủ thật** (mục L sổ lỗi), lần này chọn đúng những vùng chín đợt
trước chạm ít nhất: quản trị nội dung, tủ gửi đồ và cổng ra vào, mục lục bài trích, danh mục tự tạo từ trường MARC, kiểm kê nạp
tệp từ máy đọc rời, năm trình thiết kế biểu mẫu, đường ống xử lý tài liệu số, quy trình duyệt mua nhiều cấp, cộng phân quyền (mục 2.3) và trao đổi dữ liệu (mục 2.4). **252 phép đo, 8 lỗi**:
nội dung trình diễn của phân hệ VIII (banner trang chủ, album ảnh) nằm sau rào "chỉ nạp khi kho biểu ghi còn trống" nên máy chủ
nghiệm thu không có banner nào; xoá biểu ghi bài trích ở Biên mục xong vẫn không gỡ được bài khỏi mục lục dù câu báo lỗi bảo làm
đúng như vậy; sáu tài liệu số minh hoạ không đi qua đường ống xử lý nên endpoint ảnh bìa trả 404 cho cả sáu; hạ số cấp duyệt từ 2
xuống 1 làm yêu cầu đặt mua đã qua cấp 1 kẹt vĩnh viễn. Lỗi nặng nhất lộ ra lúc nhập một tệp ISO 2709 lớn lên máy chủ: **một biểu ghi
hỏng kéo theo biểu ghi lành đứng ngay sau nó**, vì biểu ghi bị từ chối vẫn nằm lại trong bộ theo dõi của EF rồi đi theo lượt lưu kế
tiếp — lượt nhập 12.610 biểu ghi chết hẳn sau 268 dòng, và báo cáo lỗi hiện nguyên văn tiếng Anh của khung nền. Lỗi cuối tìm ra bằng
cách mở trang chủ máy chủ thật trên trình duyệt: banner của bản trình diễn hiện **"Tài liệu sô ́ mới cập nhật"** — ảnh SVG khai `font-family='Georgia,serif'`, mà Georgia thiếu glyph dựng sẵn của nguyên âm tiếng Việt hai dấu nên trình duyệt tách dấu ra đứng cạnh. Cộng thêm hai việc trên chính máy chủ, không phải lỗi mã: dọn dữ liệu thử còn sót của các đợt trước, và dựng bộ dữ liệu trình diễn cho nhánh III.1 vốn rỗng. Lỗi thứ tám: mảng mã vạch có phần tử `null` làm cả hai lối của quầy đổ 500 vì gọi `Trim()` trước khi lọc rỗng.

Đợt rà thứ mười một (07/09/2026) đi theo **lớp** chứ không theo phân hệ — tham số truy vấn thù địch, phạm vi dữ liệu ở chiều **ghi**,
chuyển trạng thái phi pháp, xoá khi còn ràng buộc, và tranh chấp đồng thời ở những lối chưa đua bao giờ. Phạm vi ghi, trạng thái và
ràng buộc xoá đều đạt sạch (6/6, 9/9, 5/5); ba lỗi nữa lộ ra: **một ký tự rỗng U+0000 làm bảy endpoint đổ 500**, ba trong đó là lối
công khai ai cũng gọi được; **ba lượt cấp lại thẻ song song để lại ba thẻ cùng hiệu lực**, nghĩa là thẻ vừa báo mất vẫn quét được;
và **ba lượt mở kỳ kiểm kê cùng một kho song song đều thành công**, mỗi kỳ chốt một danh sách kỳ vọng riêng. Hai lỗi sau là bài học 1
và 45 lặp lại ở hai chỗ nữa: luật "một … một" chỉ kiểm ở tầng nghiệp vụ thì không chặn được hai lượt cùng lúc. Cả 11 đã sửa.

Đợt thứ mười hai hỏi ba câu nữa: **endpoint nào chưa được canh quyền**, **bất biến dữ liệu trên chính kho thật** (38 phép đo SQL,
thứ không API nào nhìn ra), và **việc chạy nền theo lịch có để lại đúng dấu vết của nó không**. Chín việc nền chạy đúng giờ, 15 lượt
thành công, 0 hỏng; 26/29 bất biến sạch ngay. Ba lỗi nữa: **thẻ đăng nhập của bạn đọc đọc được danh sách cán bộ kèm tên đăng nhập**
(endpoint chỉ có `[Authorize]`); **hàng đợi biên mục đếm 981 việc mà một trang 200 dòng chỉ trả 155** vì phần đếm chạy trên bảng công
việc còn phần lấy dòng phải nối sang biểu ghi; và **xoá một bản sách đã trả xong làm mất luôn lượt mượn ấy khỏi lịch sử bạn đọc** —
bài học 57 lần thứ tư, lần này kèm hậu quả tiền bạc vì khoản phạt gắn phiếu cũng biến khỏi danh sách mà vẫn tính vào công nợ. Cả 14
đã sửa, tổng **184 lỗi, đã sửa 184**.

Đợt thứ mười ba hỏi đúng một câu, hỏi cho **cả 48 danh sách có phân trang**: con số ở góc bảng có bằng số
dòng lấy ra được không, và đi hết các trang có gặp đúng từng ấy dòng khác nhau không. **11 danh sách sai**,
quy về bốn lỗi: chín danh sách đếm cả dòng mà chúng không hiện nổi (bài học 70/71 còn nguyên ở chín chỗ nữa —
kỳ kiểm kê và lượt gửi tủ hiện **0 dòng** trên con số 2 và 1); mọi danh sách sắp theo cột không duy nhất nên
trang sau lặp dòng của trang trước (396 dòng tiền phạt chỉ có 316 dòng khác nhau); **phạm vi dữ liệu theo kho
của danh sách phiếu mượn chỉ được cưỡng chế bằng tác dụng phụ**, nên cán bộ một kho thấy tổng 3.122 phiếu của
cả thư viện mà chỉ lấy được 302; và xoá được kho vẫn còn kỳ kiểm kê. Cả 4 đã sửa.

Đợt thứ mười bốn làm lại đúng cách ấy với hai luật khác, rút danh sách phải đo thẳng từ mã nguồn: **~130 ô
lọc** khai trong các lớp yêu cầu, **51 cột sắp xếp**, ô tìm kiếm của **26 màn hình**. Đo bằng giá trị không
thể khớp gì cả rồi đòi kết quả bằng 0. **233 phép đo, 4 lỗi** — và ba con số đáng mừng: 129/130 ô lọc,
26/26 ô tìm kiếm, mọi cột sắp xếp chiều tăng dần đều đúng. Bốn lỗi nằm chỗ khác: **không kho nào có giá**
(bộ gieo dựng kho từ phase 6 mà chưa bao giờ dựng giá, nên 17.900/17.900 bản "chưa xếp giá", bản đồ kho
rỗng, bạn đọc không thấy vị trí giá — chức năng xếp giá thì chạy đúng từng bước); **sắp giảm dần đẩy ô
trống lên đầu** (7.465/12.609 biểu ghi không có năm xuất bản, nên "mới nhất trước" là 150 trang trắng);
nhóm định dạng lạ lặng lẽ trả về cả kho; và con số facet xấp xỉ hiện ra như số đúng. Cả 4 đã sửa.

Đợt thứ mười lăm soi **26 báo cáo thống kê** theo hai câu của hồ sơ: ràng buộc kỹ thuật số 8 (đủ ba dạng đầu
ra) và mục kiểm thử 2.8 (số liệu khớp truy vấn kiểm chứng độc lập, SQL tự viết từ định nghĩa nghiệp vụ).
**81 phép đo đầu ra đạt sạch** — mọi báo cáo xuất ra tệp Excel và PDF thật, kiểm bằng chữ ký byte. **40 phép
đo số liệu: 5 lỗi**, tất cả ở tầng báo cáo mà năm đợt trước không chạm: **báo cáo ĐKCB hủy bỏ trả 0 dòng**
trên 3 quyết định thanh lý (bài học 57 lần thứ sáu), báo cáo lượt xem tài liệu số đếm 13 trên 14, nhãn kỳ của
biểu đồ dựng từ giờ UTC nên có cột "tháng 8" trong tháng không có lượt nào, báo cáo dung lượng có tổng và biểu
đồ chênh nhau 50 lần, và danh sách chạm trần bị cắt trong im lặng. Cả 5 đã sửa.

Đợt thứ mười sáu soi **nhóm `/api/reader/*`** — hợp đồng API của ứng dụng di động: truy cập chéo giữa hai
bạn đọc, danh sách "của tôi", lối cho khách, sửa hồ sơ, xác thực vị trí khi mượn tự phục vụ, gói đọc ngoại
tuyến, và vòng đời thẻ đăng nhập. **59 phép đo, 2 lỗi** — truy cập chéo sạch hoàn toàn, nhưng cả hai lỗi
cùng làm hỏng đúng một lệnh nghiệp vụ: **"tạm khoá thẻ bạn đọc" không dừng được phiên đang mở**. Chín trong
mười một việc vẫn làm được sau khi khoá, kể cả tự cấp gói đọc ngoại tuyến còn hạn bảy ngày, và thẻ làm mới
không bị thu hồi nên phiên ấy không bao giờ kết thúc. Lỗi thứ ba lộ ra lúc dọn dữ liệu thử: **lập một khoản
phạt không cập nhật cột công nợ chép sẵn trên hồ sơ** — quầy nói "còn nợ 12.000 đ" mà bạn đọc mở ứng dụng
thấy "còn nợ 0 đ", vì hàm đồng bộ chỉ được gọi khi thu và khi miễn phạt. Cả 3 đã sửa.

Đợt thứ mười bảy soi **chiều ghi**: rút từ mã nguồn ra 86 lối ghi có thân JSON rồi gửi thân rỗng, chuỗi
5.000 ký tự và số âm khổng lồ — **258 phép đo, 258 đạt**, không lối nào đổ 500. Hai hình dạng nữa thì lộ
**3 lỗi**: nút "Gửi nhắc hàng loạt" bấm ba lần sinh **1.083 thông báo cho 361 bạn đọc**; hai lối của quầy
không có trần số mã vạch nên 2.000 mã mất 8,9 giây và 50.000 mã thì proxy cắt ngang; và khoảng ngày ngược
trả bảng rỗng im lặng ở mười sáu bộ lọc trong khi bảng Tổng quan lại tự đổi thầm hai mốc. Cả 3 đã sửa.

Đợt thứ mười tám (08/09/2026) mở **tầng tệp xuất** ra đếm — tầng duy nhất mà bài học 78 kể tên nhưng chưa
ai đo. Đợt 15 chỉ hỏi "có phải tệp thật không" (chữ ký byte `PK` / `%PDF`); đợt này hỏi **trong tệp có gì**:
số dòng so với màn hình, tệp có nói ra nó lọc theo gì và có bị cắt bớt không, và chữ rút lại từ tệp có ra
đúng chữ đã ghi vào không. **110 phép đo, 4 lỗi.** Nặng nhất: **mọi tệp PDF của sản phẩm có lớp chữ sai** —
ghép chữ của phông Lato thay mỗi cặp chữ bằng một glyph mà bảng ToUnicode không tra ngược được, nên "thông
tin" rút ra thành "thông ঞn", "tình trạng" thành "টnh trạng" (14.000 ký tự hỏng trên 10 tệp), trong khi ảnh
chụp 300 dpi thì mắt thường đọc vẫn đúng: Ctrl+F trong chính tệp báo cáo không tìm ra chữ, chép ra ngoài dán
thành chữ Bengali. Ba lỗi còn lại: ô tìm kiếm của danh mục phân cấp chỉ tìm ở cấp gốc (114/124 chỉ số phân
loại không tìm ra được); dòng "đã chạm trần" và phần tiêu chí lọc của O5 **chỉ tới được bản PDF**, còn bản
Excel mất cả hai — nên lượt xuất nhật ký mang về 50.000 trên 196.612 dòng mà không nói gì; và loại phích lạ
thì hệ thống đổ lỗi cho biểu ghi. Cả 4 đã sửa, tổng **209 lỗi, đã sửa 209**.
Đợt thứ mười chín (08/09/2026) quét ngang **mã Flutter** — thứ mười tám đợt trước chưa lần nào đo,
vì ứng dụng di động mới chỉ được soi ở đặc tả (đợt J) và ở hợp đồng API phía máy chủ (đợt P). Luật
lấy từ mục XI.3 (*"hỗ trợ sáng/tối, cỡ chữ điều chỉnh được"*) và mục 6.6 (*contrast đạt WCAG AA*);
cách đo là dựng thật từng màn hình trong phép thử widget rồi **đi khắp cây widget đọc màu chữ đã
phân giải và màu nền đục gần nhất, tính tỉ lệ tương phản bằng máy**. **116 phép đo, 3 lỗi.** Chế độ
tối đổi chủ đề nhưng 82 chỗ ở màn hình gọi thẳng hằng số của bảng màu nền giấy nên không đổi theo:
chữ phụ còn **3,23 : 1** trên nền tối, tấm nền nhạt giữ màu sáng nên chữ trên nó xuống **1,08 : 1**.
Thẻ thư viện điện tử ghim nền giấy trắng mà không ghim chữ, nên ở chế độ tối họ tên bạn đọc là chữ
sáng trên giấy trắng — **1,21 : 1**, trên đúng tấm thẻ chìa ra ở quầy. Và bảng màu có **20 cặp trượt
ngưỡng ngay ở chế độ sáng**, đúng bài học 19 của phía web lặp lại ở di động. Cỡ chữ thì đạt 9/9 tới
200%. Lỗi thứ tư lộ ra lúc **cài APK lên máy ảo và nhìn bằng mắt**: viên nhãn trạng thái ghim màu
sáng, và nó sống sót vì `StatusPill` nằm trong `core/theme/` — đúng thư mục mà phép thử quét cố ý bỏ
qua. Lỗi thứ năm thì do **phép thử iOS trên máy Mac của GitHub** bắt: máy chủ tách thông báo ra
khối riêng từ 04/09/2026, ứng dụng không khai trường ấy nên trên máy chủ nghiệm thu — nơi cả hai bản
tin đều là thông báo — trang chủ **không hiện tin nào**. Lỗi thứ sáu lộ ra từ chính **bộ ảnh iOS** ấy: ở cỡ chữ 160% nhãn "Giao diện" bị ô chọn bóp đến mức
vỡ thành "Gi / ao / diệ / n". Cả 6 đã sửa.

Đợt thứ hai mươi mang đúng bài học ấy sang phía web: mục 6.6 cam kết **admin tối thiểu 1366×768**,
mà mọi ảnh chụp của mười chín đợt trước đều ở 1440×900. Đo trên máy chủ thật ở đúng khổ tối thiểu —
58 đường dẫn cộng 67 phép đo theo thẻ, **137 phép đo, 1 lỗi**: nhãn biểu đồ tròn của Báo cáo bổ sung
chạy ra ngoài khung SVG (tên chỉ mục tiếng Việt dài) và **đẩy cả trang cuộn ngang 18 px**. Ở 1440
trang không cuộn, nhãn chỉ bị cắt cụt thành ": 3621" — trông như số liệu, nên chín đợt đi qua. Cả
bốn biểu đồ tròn nay vẽ tỉ lệ phần trăm **bên trong lát**, tên lát để ở chú giải. Sửa xong đo lại thì
lộ **lỗi thứ hai nằm dưới**: ba bảng số liệu khai cột cố định cộng lại 370 px trong ô rộng 330 px —
tràn khung là một chồng, gỡ lớp trên mới thấy lớp dưới.

Đợt thứ hai mươi mốt hỏi nốt **vế thứ hai của chính câu ấy**: mục 6.6 viết "admin tối thiểu
1366×768, **OPAC hỗ trợ mobile**" — vế đầu vừa đo xong, vế sau chưa ai đo suốt hai mươi đợt vì nó
không nói ra con số nào. Chọn 375×812 rồi đo: **73 phép đo, 2 lỗi**, và cả hai đều nặng hơn của đợt
20 vì chúng nằm ở khung chung. Thanh đầu trang có ba khối, khối giữa ẩn ở khổ hẹp còn **hai khối hai
đầu đều `flex: none`** — không ai co được nên hàng ấy đòi 506 px trên 343 px dùng được và **mọi
trang** cuộn ngang 146 px. Sửa xong đo lại thì lớp dưới lộ ra đúng như bài học 101: trang chi tiết
tài liệu — trang bạn đọc mở nhiều nhất sau khi tra cứu — rộng 578 px vì `margin: 0 auto` huỷ việc
kéo giãn của một phần tử flex, khiến nó tự đo theo nội dung thay vì theo màn hình. Lượt quét cuối
38 phép đo (gồm cả trạng thái đã đăng nhập và các trang chi tiết) không còn chỗ nào tràn. Lần này
guard **là một phép thử đơn vị thật**, không chỉ là biên bản: `styles.phone.test.tsx` dựng cây DOM
của khung trang rồi đọc `styles.css` như trình duyệt đọc ở 375 px và hỏi từng hàng flex có ai co
được không. Tổng **219 lỗi, đã sửa 219**.
Phụ lục cuối `docs/06` ghi kết quả từng kịch bản (hơn 700 dòng).

Đọc thẳng hồ sơ gốc còn tìm ra thứ không phải lỗi mã: **bốn hồ sơ bàn giao** mà Chương V mục III và
mục 5 đòi — kế hoạch triển khai, kế hoạch đào tạo, cam kết bảo hành, hồ sơ nghiệm thu — nay là
`docs/10` → `docs/13`, và bảng đáp ứng có thêm hai mục đối chiếu đúng thứ tự của cả Chương V.

| Tài liệu | Nội dung |
|---|---|
| `docs/08-so-loi.md` | Sổ lỗi chín đợt: 37 lỗi hai đợt đầu, 5 lỗi đợt rà thứ hai (mục E), 9 lỗi đợt áp bản thiết kế (mục G), 7 lỗi đợt rà thứ ba (mục H), 8 lỗi đợt triển khai (mục I), **88 lỗi năm đợt rà theo đặc tả ngày 04–05/09/2026 (mục J và các mục con)**, 23 lỗi nghiệm thu thử và soi số học (mục K), **14 lỗi ba đợt rà ngày 06–07/09/2026 (mục L)**, 19 lỗi năm đợt rà ngang ngày 07/09/2026 (mục M–Q), **4 lỗi đợt rà tầng tệp xuất ngày 08/09/2026 (mục R)**, 6 lỗi đợt rà ứng dụng di động (mục S), 4 lỗi hai đợt đo khổ màn hình (mục T–U) |
| `docs/09-nguon-du-lieu.md` | Khảo sát 16 nguồn dữ liệu thư mục, giấy phép từng nguồn, kết quả nạp |
| `docs/10-ke-hoach-trien-khai.md` | Kế hoạch triển khai, chạy thử và chuyển đổi dữ liệu (Chương V mục III.1) |
| `docs/11-ke-hoach-dao-tao.md` | Kế hoạch đào tạo 16 buổi cho 7 nhóm học viên (Chương V mục III.2) |
| `docs/12-bao-hanh-ho-tro.md` | Bảo hành 12 tháng, mức sự cố và thời gian phản hồi, quyền quản lý dữ liệu (mục III.3, III.4) |
| `docs/13-bieu-mau-ban-giao.md` | Danh mục 16 hồ sơ bàn giao và 8 biểu mẫu nghiệm thu (Chương V mục 5.5) |
| `docs/00-quyet-dinh-ky-thuat.md` | Sổ quyết định — mọi chỗ tự chốt khi đặc tả không nói rõ |
| `docs/01`–`docs/07` | Bảy tài liệu bàn giao theo mục 10 |

**Kho dữ liệu hiện có trên máy phát triển:** 7.675 biểu ghi thật thu hoạch qua OAI-PMH từ bốn kho
DSpace/OJS của Việt Nam và Thư viện Quốc hội Mỹ, cộng bộ dữ liệu trình diễn lớn (`LC_SEED_DEMO=rich`)
sinh trên chính kho ấy: 9.502 ĐKCB, 351 bạn đọc, 1.603 lượt mượn trải 18 tháng, 254 khoản phạt, 58
lượt đặt giữ.

**Đã kiểm bằng công cụ ngoài:** xuất toàn kho ra ISO 2709 và MARCXML rồi cho `pymarc` 5.4 đọc —
**7.675/7.675 biểu ghi hợp lệ, 0 lỗi**, nhan đề khớp 100% giữa hai định dạng. Đây là bằng chứng cho
mục 2.4 của E-HSMT, và nó mạnh hơn mọi phép thử round-trip tự viết: bộ mã của mình mà sai theo cùng
một cách ở cả hai chiều thì phép thử tự viết vẫn xanh.

### A.2. Cách làm việc trên kho mã này

**Kiểm thử.** Sửa lỗi nào cũng phải kèm một phép thử **chạy đỏ trước khi sửa, xanh sau khi sửa** —
không có bước đỏ thì không biết phép thử ấy có bắt được gì không. Đã có lần viết phép thử xong thấy
xanh ngay cả khi chưa sửa, vì bối cảnh trong cơ sở dữ liệu kiểm thử tình cờ không dựng ra được tình
huống lỗi; phải tự tay dựng đúng bối cảnh ấy trong phép thử.

**Lệnh chạy đúng:**

```bash
cd backend  && dotnet test                 # 655 unit + 544 integration
cd frontend-admin && npx tsc -b && npx vitest run    # 350 test
cd frontend-opac  && npx tsc -b && npx vitest run    # 106 test
cd mobile   && flutter analyze && flutter test       # 143 test
```

> `npx tsc --noEmit` **không kiểm gì cả** ở hai thư mục frontend: `tsconfig.json` là tệp solution
> rỗng chỉ trỏ tới hai tsconfig con. Luôn dùng `npx tsc -b`.

**Mười bảy phép thử quét mã nguồn** chặn cả một lớp lỗi thay vì chặn một chỗ. Đừng bỏ chúng đi khi thấy
vướng — mỗi cái sinh ra từ một lỗi đã xảy ra thật:

| Phép thử | Luật |
|---|---|
| `frontend-admin/src/api/download.test.ts` | Ngoài `src/api`, không được viết địa chỉ bắt đầu bằng `/api/` — xác thực là JWT trong tiêu đề, thẻ liên kết không mang theo được |
| `frontend-opac/src/lib/marcView.test.ts` | Không `JSON.stringify` biểu ghi MARC ra trang công khai |
| `frontend-admin/src/lib/datetime.test.ts` | Giao diện quản trị không tự viết cách hiện ngày riêng; dùng `lib/datetime` |
| `frontend-opac/src/lib/datetime.test.ts` | Trang tra cứu cũng vậy — hai gói riêng nên phải quét riêng, đây chính là chỗ lỗi D8 lọt qua |
| `frontend-admin/src/lib/columnLabels.test.ts` | Không đặt tên cột đúng một chữ "Giá" — trong nghề thư viện nó vừa là giá sách vừa là giá tiền |
| `frontend-admin/src/modules/catalogs/catalogColumns.test.ts` | Bảng danh mục: mọi cột khai bề rộng, không cột nào chỉ nhận phần thừa |
| `backend/.../Security/SecretsInRepositoryTests.cs` | Kho mã không mang mật khẩu dùng được; tài liệu nói cách lấy chứ không in giá trị |
| `backend/.../PermissionAndAuditTests.cs` | Thông báo lỗi không được lọt tiếng Anh của khung nền |
| `frontend-*/src/styles.test.ts` | Mọi lớp `lc-*` gắn vào phần tử phải có kiểu, và mọi luật khai ra phải có người dùng — bảy lớp từng được gắn mà chưa bao giờ có kiểu, nên hàng đang chọn ở Quản lý kho không sáng lên suốt mấy phase |
| `frontend-*/src/theme.test.ts` | Token của `theme.ts` phải trùng biến `--lc-*` của `styles.css`, và `index.html` phải tải thật hai bộ chữ |
| `frontend-*/src/theme.test.ts` (phần WCAG) | 16 cặp màu của bảng màu phải đạt ngưỡng tương phản mục 6.6 |
| `frontend-*/src/lib/palette.test.ts` | Không viết mã màu thẳng trong TSX — màu viết thẳng không đi qua token nào, nên đổi thiết kế xong 130 chỗ vẫn giữ màu cũ. Sáu chỗ ngoại lệ là thứ đi ra máy in, không phải màn hình |
| `frontend-*/src/lib/palette.test.ts` (luật thứ hai) | Không chuỗi nháy đơn nào chứa `${MAU.…}` — đợt thay 130 màu để lại 11 chỗ `'1px solid ${MAU.vien}'` trong dấu nháy đơn, trình duyệt bỏ qua cả dòng CSS mà phép thử cấm mã màu vẫn xanh vì không còn mã màu để bắt |
| `backend/.../Infrastructure/RequestLoggingOrderTests.cs` | Bộ ghi nhật ký yêu cầu đứng trước bộ xử lý ngoại lệ trong `Program.cs`; đứng sau là mọi lỗi 400/401/404 bị ghi thành ERR 500 kèm vết ngăn xếp |
| `backend/.../Security/NginxConfigParityTests.cs` | **Ba** tệp cấu hình Nginx phải cùng mang sáu luật: bốn tiêu đề bảo mật, nhánh rẽ cho máy thu thập, và `resolver` thay cho `upstream`. Thêm luật vào hai tệp mà quên tệp thứ ba đã xảy ra hai lần trong một ngày, cả hai lần đều mất đúng ở bản chạy thật |
| `frontend-*/src/components/keyboard.test.ts` | `div`/`span` có `onClick` phải kèm `clickable(...)` hoặc đủ bộ ba `role` + `tabIndex` + `onKeyDown` — thanh menu chính của trang tra cứu từng không Tab tới được |
| `mobile/test/core/push_background_test.dart` | Có đăng ký `onBackgroundMessage`, hàm xử lý là hàm cấp cao nhất mang `@pragma('vm:entry-point')`, và Gradle áp dụng trình cắm google-services khi có tệp cấu hình |
| `mobile/test/features/camera_error_view_test.dart` | Mọi `errorBuilder` của khung quét phải dựng `CameraErrorView` (chỉ lỗi quyền mới được nói về quyền), và màn Mượn tự phục vụ chỉ được có **một** `MobileScannerController` — hai bộ là hai máy khách camera, bộ sau không giành được và bạn đọc bị bảo đi cấp một quyền đã bật |
| `mobile/test/core/palette_scan_test.dart` | Màn hình không gọi thẳng hằng số màu của chế độ sáng (`LcColors.muted`…) — phải qua `context.lc.<tên>` để đổi theo chế độ. Chế độ tối từng đổi chủ đề mà 82 chỗ giữ nguyên màu nền giấy: chữ phụ 3,23 : 1, chữ sáng trên tấm nền nhạt 1,08 : 1. Nền cố định ở cả hai chế độ thì khai ngoại lệ kèm lý do |
| `mobile/test/features/screens/text_scale_and_dark_test.dart` | Mỗi màn hình dựng ở cỡ chữ 100/150/200% không tràn khung, và ở **cả hai** chế độ không dòng chữ nào tụt dưới 4,5 : 1 — đo bằng cách đi khắp cây widget, không nhìn ảnh chụp |
| `mobile/test/features/list_refresh_after_write_test.dart` | Màn hình gọi `checkout`/`renewLoan`/`createHold`/`cancelHold` phải `ref.invalidate` đúng provider tương ứng — hai màn hình từng ghi xong mà danh sách không đổi |
| `backend/.../Security/NginxConfigParityTests.cs` (luật thứ hai) | Tệp Nginx nào có `limit_req` thì phải có `limit_req_status 429` và trang lỗi 429 dạng JSON — mặc định Nginx trả trang HTML 503, máy khách chỉ hiện được "máy chủ lỗi" |
| `frontend-admin/src/modules/dashboard/DashboardPage.test.ts` | Trang Tổng quan không mang dòng giữ chỗ về tiến độ dự án và phải đọc báo cáo tổng quan — màn hình đầu tiên sau đăng nhập từng nói "đang bàn giao" suốt từ phase 1 tới bản chạy thật |
| `backend/.../Security/LocalTimeInMessagesTests.cs` | Mọi mốc giờ hiện cho người dùng phải qua `ToLocalTime()` hoặc lấy từ đồng hồ hệ thống — quét cả `{…:HH:mm}` lẫn `.ToString("…HH…")`. Câu "tạm khóa tới 02:44" từng hiện giờ UTC cho một mốc thật ra là 09:44 |
| `backend/.../Infrastructure/MigrationRegistrationTests.cs` | Mỗi tệp trong thư mục Migrations phải có lớp mang đúng `[Migration("<mã>")]`, và không lớp `Migration` nào được thiếu thuộc tính — thiếu là EF Core bỏ qua trong im lặng, bản sửa dữ liệu triển khai xong mà không chạy |
| `backend/.../Infrastructure/DemoLoanDatesTests.cs` | Không lượt mượn nào của bộ dữ liệu trình diễn rơi vào tương lai, ở mọi cỡ bộ dữ liệu và mọi chính sách — 94 phiếu "mượn 29/11, trả 02/09" từng sống trên cả máy chủ thật |
| `backend/.../Security/EndpointAuthorisationTests.cs` | Mọi endpoint quản trị phải khai `[RequirePermission]`, khai `[AllowAnonymous]`, hoặc có tên trong danh sách "tự canh trong bộ xử lý" kèm lý do — `[Authorize]` một mình cho thẻ bạn đọc đi qua |
| `backend/.../UnstorableTextTests.cs` | Ký tự PostgreSQL không lưu được (U+0000 và họ) bị bỏ ở **cả hai** cửa vào — chuỗi truy vấn và thân JSON. Một ký tự rỗng từng làm bảy endpoint đổ 500, ba trong đó công khai |
| `backend/.../ConcurrencyTests.cs` | Hai luật "một … một" phải có ràng buộc duy nhất ở CSDL: một bạn đọc một thẻ hiệu lực, một kho một kỳ kiểm kê chưa chốt. Phép thử gửi ba yêu cầu **thật sự song song**; gọi tuần tự thì cả hai vẫn xanh |
| `backend/.../Infrastructure/VietnameseFontStackTests.cs` | Không nơi nào gọi tên Georgia trong danh sách phông — Georgia thiếu glyph dựng sẵn của ố, ề, ắ, ữ nên trình duyệt tách dấu ra đứng cạnh nguyên âm. Quét cả ảnh SVG của bộ dữ liệu trình diễn lẫn `styles.css`/`theme.ts` của hai giao diện |
| `backend/.../Infrastructure/DeployScriptTests.cs` | `gh-deploy.sh` phải có bước `don_anh_cu` giữ bản mới và bản trước, xoá ảnh `libraryconnect-*` còn lại — 20 bộ ảnh cũ từng làm đầy ổ 96 GB và chặn mọi lượt triển khai |
| `backend/.../Security/LocalTimeInMessagesTests.cs` (luật thứ hai) | Nhãn kỳ của biểu đồ (`…At.ToString("yyyy…")`) cũng phải qua `ToLocalTime()` — luật cũ chỉ dò chuỗi có `HH` nên nhãn `yyyy-MM` lọt lưới, và một lượt xem lúc 02:00 ngày 01/09 hiện ở cột tháng 8 |
| `frontend-admin/src/components/pieLabel.test.ts` | Mọi `<Pie>` phải dùng nhãn dùng chung `nhanTrongLat` (vẽ phần trăm **trong** lát) và phải có `<Legend />`. Nhãn ngoài của Recharts nằm ngoài khung SVG: tên chỉ mục tiếng Việt dài đẩy trang cuộn ngang ở 1366×768, và ở 1440 thì bị cắt cụt thành ": 3621" nên trông như số liệu |
| `backend/.../Infrastructure/PdfTextLayerTests.cs` | Chữ rút lại từ tệp PDF phải bằng chữ ghi vào — đo bằng PdfPig, thư viện của người khác. Và sáu bộ dựng PDF phải khai phông qua `PdfTextStyles.Base()`, không bộ nào khai `FontFamily` thẳng: ghép chữ của Lato làm "thông tin" rút ra thành "thông ঞn" trong khi trang in nhìn vẫn đúng |
| `backend/.../Infrastructure/StablePagingOrderTests.cs` | Mọi lượt `ToPagedResultAsync` phải kết thúc chuỗi sắp xếp bằng một khóa duy nhất — qua `ApplySort` (tự gắn) hoặc tự viết `ThenBy(x => x.Id)`. Sắp theo cột không duy nhất là trang sau lặp dòng của trang trước và đúng bấy nhiêu dòng khác không bao giờ hiện ra: 396 dòng tiền phạt chỉ có 316 dòng khác nhau |
| `frontend-opac/src/styles.phone.test.tsx` | Ở bề ngang 375 px, mỗi hàng flex của khung trang phải có ít nhất một phần tử con chịu co, và cặp nhãn `lc-only-wide` / `lc-only-narrow` không được cùng ẩn. Hai khối hai đầu thanh đầu trang cùng `flex: none` từng làm **mọi trang** của trang tra cứu cuộn ngang 146 px, và lượt sửa nó để lại một nút đăng nhập rỗng ruột |

> Một phép thử quét mã nguồn chỉ chặn đúng thư mục nó quét. Thêm luật mới thì hỏi ngay: gói kia có
> vi phạm cùng luật ấy không? Lỗi D8 sửa cho `frontend-admin` rồi ghi là "cả sản phẩm", nhưng
> `frontend-opac` vẫn còn nguyên suốt một đợt.

**Migration.** Sửa lỗi nghiệp vụ thường phải kèm migration dọn dữ liệu cũ: thư viện đã chạy bản
trước mang sẵn hậu quả của lỗi ấy trong kho, sửa mã nguồn thôi thì số dữ liệu ấy vẫn nằm im. Bốn
migration gần nhất đều thuộc loại này.

**Bộ dữ liệu trình diễn** chỉ chạy khi `bib_records` còn rỗng, nên không kiểm chứng được trên máy
đang chạy. Muốn kiểm thì dựng một cơ sở dữ liệu trắng:

```bash
docker exec lc-postgres psql -U libraryconnect -d postgres -c "CREATE DATABASE lc_kiem;"
docker compose run --rm -d --name lc-api-kiem -e LC_DB_NAME=lc_kiem -e LC_SEED_DEMO=true api
# đợi khoảng 80 giây rồi truy vấn thẳng bằng psql, xong thì xoá cả container lẫn database
```

### A.3. Những chỗ đã trả giá — đừng lặp lại

1. **Tầng nghiệp vụ không chặn được tranh chấp.** Kiểm "sách còn rảnh không" rồi mới ghi là hai quầy
   làm cùng lúc đều ghi được. Luật kiểu "một bản in một phiếu đang mở" phải là **ràng buộc duy nhất
   ở cơ sở dữ liệu**.
2. **Đặt trạng thái không bằng tạo việc.** Biểu ghi mang trạng thái "Chờ biên mục" mà không có dòng
   trong `bib.catalog_queue` thì không ai nhìn thấy. Màn hình đọc từ bảng công việc, không quét cột
   trạng thái.
3. **Cắt trước, lọc sau là sai.** Lấy 500 dòng đầu rồi mới bỏ dòng rỗng thì kho càng lớn danh sách
   càng rỗng. Luôn lọc trong câu hỏi gửi xuống cơ sở dữ liệu.
4. **Việc dài không được chạy trong lượt HTTP.** Proxy cắt ở 300 giây, việc bị bỏ dở, nhật ký kẹt
   "Đang chạy" vĩnh viễn. Xếp vào Hangfire, kèm khoá chống chạy trùng và cơ chế đóng lượt chết.
5. **Bảng có cột cố định thì cột quan trọng nhất cũng phải khai bề rộng.** Để trống một cột cho nó
   "nhận phần thừa" là khi hết phần thừa nó co lại còn vài chục điểm ảnh.
6. **Dữ liệu mẫu là một phần của sản phẩm.** Tên bạn đọc trùng nhau, danh mục rỗng, chưa đặt tên thư
   viện — người xem buổi nghiệm thu kết luận là phần mềm chưa cài xong, dù nghiệp vụ chạy đúng.
7. **Lỗi chỉ lộ ra khi có dữ liệu thật.** Bộ dữ liệu 200 biểu ghi nhan đề ngắn che mất một loạt lỗi
   giao diện và hiệu năng. Có nghi ngờ thì nạp dữ liệu thật rồi nhìn lại.
8. **Tự kiểm bằng chính bộ mã của mình không chứng minh được gì.** MARC từng chỉ được kiểm bằng phép
   thử vòng tròn — encode rồi decode bằng cùng bộ mã, thấy khớp là coi như đúng. Đem tệp xuất cho
   `pymarc` đọc thì lộ ra ngay: xuất 7.675 biểu ghi trả về lỗi hệ thống và không lấy được biểu ghi
   nào, chỉ vì một biểu ghi có trường dài quá giới hạn 9.999 byte của ISO 2709.
9. **Đúng luật ở một thư mục không có nghĩa là đúng ở thư mục kia.** Phép thử quét mã nguồn chỉ chặn
   đúng chỗ nó quét. Lỗi D8 sửa cho `frontend-admin` rồi ghi là "cả sản phẩm", mà `frontend-opac` còn
   nguyên suốt một đợt.
10. **Dữ liệu bẩn của kho nguồn là dữ liệu của mình sau khi thu hoạch.** Ô tác giả của kho bạn có cả
   công thức bảng tính và nhan đề đặt nhầm; nhận vào không kiểm thì chúng thành mục trong hồ sơ thẩm
   quyền và đứng đầu trang tra cứu của bạn đọc.
11. **"Đã lưu" chưa phải "hiện ra được".** Bộ tra ảnh bìa tải ảnh về kho đối tượng rồi ghi vào biểu
   ghi một địa chỉ trỏ tới endpoint chỉ phục vụ thư mục khác. Cơ sở dữ liệu nói có ảnh, kho đối
   tượng có tệp, báo cáo nói xong — mà trang tra cứu hiện 16 ô ảnh hỏng. Kiểm phải đi tới **nơi
   người dùng nhìn thấy**, không dừng ở chỗ hệ thống tự nói là đã lưu.
12. **Chặn trên phải đếm đúng thứ mình muốn chặn.** Lượt nạp Open Library đếm cả biểu ghi bỏ qua vào
   hạn mức, nên dừng ở 152 biểu ghi trong khi xin 2.600: phần lớn kết quả tìm kiếm không có ảnh bìa
   nên hạn mức hết veo ngay trang đầu.

13. **Ảnh chụp màn hình không phải bằng chứng.** Một phần tử thiếu hẳn kiểu CSS vẫn hiện ra, chỉ là
    hiện trần — nhìn thấy "hơi nhạt" rồi cho qua. Phải `getComputedStyle` mới thấy nền của nó là
    `rgba(0, 0, 0, 0)`. Bảy lớp `lc-*` đã sống như vậy qua mấy phase.
14. **Ant Design chèn kiểu của nó sau tệp kiểu của mình.** Nó sinh CSS bằng JavaScript lúc chạy rồi
    chèn vào `<head>`, và bọc bộ chọn trong `:where(...)` nên độ ưu tiên chỉ 0-1-0 — bằng đúng
    `.ant-tag-blue` viết trần, mà bằng nhau thì cái chèn sau thắng. Muốn đè phải lên 0-2-0
    (`.ant-tag.ant-tag-blue`), không cần `!important`.
15. **`upstream` của Nginx ghim IP một lần rồi thôi.** Dựng lại một container là nó nhận IP mới, mà
    Nginx vẫn gửi tới IP cũ. Đã xảy ra: hai container đổi chỗ IP cho nhau và **trang tra cứu công
    khai trả về giao diện quản trị**, mã 200, nhật ký sạch. Dùng `resolver 127.0.0.11` cộng tên dịch
    vụ đặt trong biến thì Nginx tra tên lại theo từng yêu cầu.
16. **Sửa `marc_data` chưa phải là sửa xong.** Cột phẳng rút từ MARC (`pages`, `publish_year`,
    `title`…) không tự cập nhật theo, mà **trang tra cứu đọc cột phẳng**. Migration
    `20260902100000` sửa trường 300 sạch sẽ nhưng bỏ quên cột `pages`, nên 4.624 biểu ghi vẫn hiện
    "Mô tả vật lý: application/pdf" cho bạn đọc suốt từ đó. Đây đúng là bài học số 8 ở trên, tự mình
    vi phạm lại.
17. **Màu viết thẳng trong TSX không đi qua token nào.** Áp thiết kế qua `ConfigProvider` là hơn
    trăm màn hình đổi theo cùng lúc — trừ 130 chỗ viết `'#1677ff'` thẳng vào `valueStyle` và vào
    `fill` của Recharts. Chúng nằm ngoài mọi token, đổi thiết kế xong vẫn nguyên màu cũ. Dùng
    `lib/palette.ts`, và có phép thử cấm viết mã màu thẳng.
18. **Màu ngữ nghĩa không dùng làm màu phân loại được.** Xanh lá là "tốt", đỏ là "hỏng" — đem chúng
    tô biểu đồ loại bạn đọc là gán nghĩa không có thật, mà hai sắc xanh trong bảng lại gần nhau nên
    hai mảng cạnh nhau còn không phân biệt được. Hai dải riêng.
19. **Bảng màu nền giấy dễ trượt tương phản.** Nền không phải trắng nên mọi cặp màu tối đi một chút
    so với lúc chọn trên nền trắng, mà mắt không đo được. Một cặp đã trượt xuống 4,14:1. Đo bằng
    phép thử, đừng nhìn.

20. **Thêm vào navigation của thực thể đang Unchanged không phải là "thêm".** Entity Framework thấy
    khoá đã có giá trị thì coi dòng ấy có sẵn và phát UPDATE — 0 dòng bị ảnh hưởng, cả lượt lưu đổ.
    Với cha đang Added thì con cũng Added, nên đường tạo mới xanh mà đường sửa đổ suốt từ phase 5.
    Liên kết mới thì `Set<T>().Add` cho rõ.
21. **Lọc sơ bộ rồi `Take(n)` là chấp nhận bỏ sót.** "Nguyễn" khớp 3.060 tác giả trên kho thật; lấy
    200 dòng đầu là tên đã có nằm ngoài danh sách và bị tạo lại. Cái cần khớp thì so ngay trong SQL
    bằng khoá chuẩn hoá có chỉ mục, không đặt ngưỡng.
22. **Thay hàng loạt bằng công cụ rồi phải nhìn lại từng chỗ.** Đổi `'#e3d9c7'` thành `'${MAU.vien}'`
    mà giữ dấu nháy đơn là chuỗi mẫu không còn là chuỗi mẫu; CSS sai lặng lẽ, phép thử viết cho
    chính lượt thay ấy vẫn xanh vì nó chỉ bắt mã màu.
23. **Địa chỉ người dùng chỉ lấy từ chỗ proxy đã xác nhận.** Đọc thẳng `X-Forwarded-For` là tin chữ
    người gọi tự viết — một dòng tiêu đề là giả được IP trong nhật ký. Mà bộ trung gian
    ForwardedHeaders mặc định chỉ tin proxy ở loopback, Nginx trong Docker thì không, nên
    `RemoteIpAddress` là địa chỉ của Nginx cho mọi người và bộ giới hạn tốc độ nhốt cả thư viện vào
    một ngăn. Tin đúng dải mạng của proxy, rồi mọi chỗ chỉ đọc `RemoteIpAddress`.
24. **Con số giống nhau chưa chắc là bằng chứng.** 96.737 dòng nhật ký cùng một IP trông như lỗi
    proxy, hoá ra là vì mọi yêu cầu trên máy phát triển đều từ một máy. Phải dựng một nguồn thứ hai
    (container khác) mới tách được "lỗi" khỏi "bối cảnh".

25. **`flutter test integration_test` gỡ ứng dụng sau khi chạy.** Phiên "chuẩn bị" bằng phép thử không
    còn khi cài bản thường — mất một giờ đi tìm "lỗi mất phiên khi ngoại tuyến" không tồn tại. Muốn
    giữ phiên cho kiểm bằng adb thì cài bản thường và đăng nhập trên đó.
26. **Chiều cao cố định là tràn chữ chờ sẵn.** Kệ sách cao 218 điểm tràn 16 điểm ở cỡ chữ 160%. Cái gì
    chứa chữ thì chiều cao theo `MediaQuery.textScalerOf`, và phải chụp thử ở cỡ chữ lớn nhất.
27. **Gói bên thứ ba cũng gọi ra ngoài.** `google_fonts` tải phông từ fonts.gstatic.com mỗi lần mở
    ứng dụng; mất mạng là đổi phông. Đóng gói phông vào tài nguyên và tắt tải lúc chạy.
28. **Đồng hồ thiết bị không phải mốc đồng bộ.** Máy ảo lệch vài chục giây so với máy chủ nên
    `updatedSince` tính từ điện thoại bỏ sót đúng bản ghi vừa sửa. Máy chủ trả `serverTime` là để dùng.

29. **Đọc đặc tả rồi đi tìm bằng chứng, đừng đọc mã rồi hỏi nó có đúng không.** Năm đợt rà ngày
    04–05/09/2026 làm theo cách thứ nhất và tìm ra 38 lỗi mà 1.073 phép thử không chạm tới. Phép thử
    chỉ hỏi "mã có làm đúng thứ người viết nghĩ không"; đặc tả hỏi "sản phẩm có làm đúng thứ khách
    cần không". Hai câu hỏi khác nhau.
30. **Công tắc được lưu không có nghĩa là có ai đọc nó.** Ô "ghi nhật ký lượt xem", cờ "hiện làm bộ
    lọc trên tra cứu" của danh mục tự tạo, chiều phạm vi dữ liệu theo dạng tài liệu — cả ba lưu đúng
    vào cơ sở dữ liệu và không nơi nào đọc ra. Thêm một ô cấu hình thì phải chỉ được ra **chỗ đọc**
    nó; không chỉ ra được thì đó là một công tắc chết.
31. **Cấu hình đọc một lần lúc khởi động là cấu hình không đổi được.** Lịch sao lưu sửa trên màn
    hình chỉ ghi vào bảng; việc định kỳ giữ giờ cũ tới lần khởi động lại, mà màn hình vẫn hiện giờ
    mới. Đổi tham số nào có việc nền đi kèm thì đăng ký lại ngay, và màn hình nên hiện **cái đang
    chạy thật** bên cạnh cái đã khai.
32. **Thư viện nhật ký nuốt lỗi của sink.** Sink PostgreSQL của Serilog ném ở mỗi lô — một lần vì
    chữ ký `NpgsqlBinaryImporter.Complete()` của Npgsql 7 trong khi dự án dùng Npgsql 8, một lần vì
    `DateTimeOffset` lệch +07 đưa vào cột chỉ nhận UTC — mà biểu hiện duy nhất là bảng rỗng mãi.
    `Serilog.Debugging.SelfLog` thấy ngay, nay đã bật sẵn ra stderr.
33. **`PUT` thay toàn bộ tài nguyên.** Gọi `PUT /cataloging/bibs/{id}` mà không gửi `documentTypeId`
    là xoá dạng tài liệu của biểu ghi. Giao diện luôn gửi đủ nên không ai thấy; máy khách khác thì
    mất dữ liệu lặng lẽ. Phép thử đi qua API phải gửi đủ trạng thái, đúng như giao diện gửi.
34. **Khoá chuẩn hoá của danh mục coi "Phạm Văn Gộp" và "Pham Van Gop" là một.** Phép thử gộp trùng
    lập hai cách viết ấy rồi gộp, nhưng lượt lập thứ hai chỉ trả về đúng mục cũ nên không còn gì để
    gộp — phép thử đỏ vì lý do sai. Muốn hai mục thật sự khác nhau thì tên phải khác **sau khi bỏ
    dấu**.
35. **Sửa cấu hình hạ tầng thì đếm xem có mấy tệp cùng loại.** Kho có ba tệp Nginx cho ba cách
    triển khai. Nhánh rẽ máy thu thập và chính sách nội dung đều được thêm vào hai tệp, quên tệp
    dùng khi chạy thật — mất đúng ở môi trường duy nhất cần tới.
36. **Không tin `Content-Type` do máy khách gửi.** Hai đường tải tệp (logo thư viện nhúng vào biểu
    mẫu in và trang công khai, bản scan biên bản bàn giao) chỉ đọc cái nhãn ấy. Kiểm bằng chữ ký
    byte, và giữ **một** bảng chữ ký dùng chung — bốn bản sao rải rác là lý do không ai thấy thiếu.
37. **Đặc tả nội bộ này không thay được hồ sơ gốc.** `Chương V.YÊU CẦU VỀ KỸ THUẬT.pdf` ở thư mục
    gốc mới là bản bên mời thầu chấm. Tài liệu này chép lại phần chức năng nhưng bỏ mục III (triển
    khai, đào tạo, bảo hành, quyền dữ liệu) và mục 5 (kiểm thử, hồ sơ bàn giao) — bốn hồ sơ bắt buộc
    vì thế thiếu tới tận ngày 05/09/2026. Trước khi nói "đã đủ", mở lại tệp PDF ấy.
38. **Rà trên máy phát triển không thay được chạy thử trên máy chủ thật.** Nghiệm thu thử ngày
    05/09/2026 tìm ra 5 lỗi mà tám đợt rà trước không thấy, vì chúng chỉ tồn tại ở bản chạy thật:
    `limit_req` chỉ có trong hai tệp Nginx triển khai, dữ liệu thật có 7.000 biểu ghi không có bản
    in, và thẻ mẫu chỉ hết hạn khi lịch đi tới ngày ấy. Trước buổi nghiệm thu, chạy lại đúng bộ
    kịch bản trên đúng máy chủ sẽ chấm.
39. **Mỗi tầng chặn đứng trước phải nói cùng một thứ tiếng với tầng sau.** API trả 429 JSON tiếng
    Việt, nhưng Nginx đứng trước nó trả 503 HTML — người dùng chỉ thấy tầng nào bắt được yêu cầu
    trước. Thêm một lớp chặn ở hạ tầng thì phải cho nó trả đúng khuôn `ApiResponse`.
40. **Luật nghiệp vụ phải hỏi câu đầu tiên trước.** Đặt giữ kiểm chính sách, hạn mức, trùng phiếu,
    đang mượn — mà không hỏi "thư viện có bản in không". Kho phát triển mọi biểu ghi đều có ĐKCB
    nên câu ấy chưa bao giờ cần; kho thật thì 7.000 biểu ghi trả lời "không".
41. **Dữ liệu trình diễn không được có ngày viết cứng.** Bốn khóa 2021–2024 đẹp lúc viết, tới đúng
    05/09/2026 thì cả khóa đầu hết hạn thẻ. Mọi mốc thời gian của dữ liệu mẫu tính từ ngày nạp.
42. **Hai lối đi tới cùng một luật phải cho cùng một câu trả lời.** Đường lưu cấp 001 rồi mới kiểm,
    endpoint kiểm tra riêng thì không — trình soạn báo lỗi mà bấm Lưu vẫn xong. Nhập ISO 2709 đã
    gặp đúng chuyện này và lọc thông báo ở chỗ của nó thay vì sửa gốc; lần này sửa gốc.
43. **Mỗi lượt triển khai để lại một bộ ảnh; ai không dọn thì ổ đĩa dọn.** `docker image prune -f`
    chỉ xoá ảnh không tag, mà ảnh kéo về gắn tag theo mã commit nên nằm nguyên: 21 lượt trong hai
    ngày là 27 GB, ổ dùng chung đầy 100% và bản sửa không lên được. Kịch bản triển khai phải tự dọn,
    giữ đúng bản đang chạy và bản trước để quay lại.
44. **Bash đọc kịch bản theo từng đoạn.** `git reset --hard` giữa chừng thay tệp bằng inode mới, bash
    vẫn đọc nốt phần đuôi của bản cũ — lượt triển khai đầu sau khi sửa kịch bản chạy đúng kịch bản
    cũ. Bọc toàn bộ thân kịch bản trong `main()` để bash phân tích trọn tệp trước khi chạy.
45. **Bài học số 1 áp cho mọi luật "một … một".** Phiếu mượn đã có ràng buộc duy nhất, đặt giữ thì
    chưa — ba lượt bấm cùng lúc lọt hai phiếu trên máy chủ thật. Có luật "một bạn đọc một phiếu"
    ở tầng nghiệp vụ thì hỏi ngay: chỉ mục duy nhất tương ứng ở đâu?
46. **Phép thử tương phản chỉ đo cặp nó được kể.** Mười sáu cặp xanh, ba cặp trượt nằm ngoài danh
    sách, và một cặp được cho hưởng ngưỡng 3 của "chữ lớn" dù chữ chỉ 11 px. Chạy Lighthouse trên
    trang thật để tìm cặp còn thiếu, rồi mới đưa vào danh sách.
47. **Tra cứu phải khớp từng từ, không khớp cả cụm.** So cả từ khóa như một chuỗi con thì gõ đúng
    nhan đề có dấu gạch ra 0 kết quả, đảo hai từ ra 0. Kho phát triển nhan đề ngắn nên không ai gõ
    đủ dài để thấy; kho thật lộ ngay ở câu đầu tiên hội đồng gõ.
48. **Việc gửi đi phải hỏi kênh gửi có mở không, và kênh gửi phải đọc đúng ô cấu hình của nó.**
    Bộ gửi thư tắt thì lặng im mà màn hình vẫn báo "đã gửi"; tám ô SMTP trên màn hình lưu vào CSDL
    mà bộ gửi chỉ đọc appsettings. Kho phát triển và CI không có SMTP nên cả hai lỗi sống qua mọi
    đợt rà cho tới khi soi đường đi của một lá thư trên máy chủ thật.
49. **Ràng buộc duy nhất chặn được tranh chấp, nhưng chặn xong phải nói đúng chuyện.** Hai lượt cùng
    tạo một tác giả chưa có: ràng buộc chặn lượt sau đúng như bài học 1, mà câu trả lời "nhập mã khác"
    vô nghĩa với người không nhập mã nào. Mục danh mục tự sinh từ biểu ghi thì lượt thua phải nhận mục
    người thắng vừa tạo rồi lưu lại — làm ở một chỗ (`SaveChangesAsync`, `CatalogRaceReconciler`), không
    chép vào từng handler. Và phép thử đồng thời phải gửi yêu cầu **thật sự song song**; gọi tuần tự thì
    không bao giờ thấy.
51. **Không kiểm được bằng thiết bị thật thì dựng lấy thiết bị.** Máy ảo Android chèn được ảnh vào cảnh
    camera (`-camera-back virtualscene -virtualscene-poster wall=<png>`), nên mã vạch và mã QR sinh ra bằng
    `python-barcode`/`qrcode` là quét được thật. Ba luồng quét bị ghi "chưa kiểm" suốt chín đợt chạy được
    trong một buổi, và luồng thứ ba lộ ngay một lỗi nặng. Ảnh phải nhỏ và nằm giữa tấm áp phích, không thì
    nó tràn ra ngoài khung ngắm.
52. **Một lời báo lỗi sai còn tệ hơn không có lời nào.** "Chưa được phép dùng camera" khi quyền đang bật
    đẩy bạn đọc vào Cài đặt, thấy quyền đã bật, rồi hết đường. Chỉ lỗi quyền mới được nói về quyền; mọi lỗi
    khác nói đúng lỗi của nó. Và luật ấy để **một chỗ** (`CameraErrorView`) — màn thứ hai tự viết lại là
    đúng chỗ nó sai.
50. **Đếm "đang dùng" là đếm thứ người dùng còn nhìn thấy.** Liên kết biểu ghi–tác giả giữ nguyên khi biểu
    ghi xoá mềm (đúng, vì biểu ghi khôi phục được), nhưng bộ đếm tham chiếu đếm cả liên kết của biểu ghi
    đã xoá, nên xoá sách rồi vẫn không dọn được hồ sơ thẩm quyền. Đếm qua bảng có bộ lọc xoá mềm, đừng
    đếm bảng nối. Và dọn dữ liệu thử cũng là một phép thử — lỗi này lộ ra đúng lúc dọn.
53. **Kiểm kê đếm cái trên giá, không đếm cái trong sổ.** Sách đang ở tay bạn đọc không nằm trên giá,
    nên xếp nó vào "thiếu" là sai hai lần: cán bộ đi tìm thứ không thể có ở đó, và danh sách thiếu —
    thứ dùng để lập quyết định mất — mang theo cả những cuốn có người giữ hợp lệ. Mọi danh sách kỳ
    vọng phải hỏi "vật này lúc này đang ở đâu", không phải "vật này thuộc kho nào".
54. **Kỳ vọng của phép thử phải đọc từ đúng chỗ cấu hình.** Ba phép đo đầu của đợt nghiệp vụ đỏ vì
    kịch bản đoán khoá tham số (`CIRCULATION.WEEKLY_CLOSED_DAYS` chứ không phải `CLOSED_WEEKDAYS`) và
    vì gia hạn ngay hôm mượn thì hạn mới không dài hơn hạn cũ. Cả ba là lỗi của phép thử. Đọc cấu hình
    thật trước, rồi mới nói sản phẩm sai.
55. **Triển khai xong phải kiểm chính thứ vừa sửa, trên máy chủ thật.** Hai migration sửa dữ liệu đi
    qua CI xanh, dựng ảnh, triển khai thành công — và không chạy, vì thiếu `[Migration]`. Bằng chứng
    duy nhất là con số dữ liệu trên máy chủ vẫn y như cũ. "Đã triển khai" không phải "đã có tác dụng".
56. **Giao thức trao đổi phải trả lời sai một cách ồn ào.** SRU nhận `(dc.title="a" and` rồi trả cả
    12.060 biểu ghi vì bộ phân tích coi phần không hiểu là từ khóa. Thư viện bạn nhận đủ kết quả nên
    tưởng đúng. Với mọi thứ nói chuyện với hệ thống khác, phép thử phải hỏi cả **câu sai có bị chặn
    không**, không chỉ câu đúng có chạy không — và kiểm bằng máy khách của người khác, không bằng máy
    khách của chính mình.
57. **Bộ lọc xoá mềm lan từ cha sang con.** Bạn đọc là đầu bắt buộc của quan hệ, nên lọc mất bạn đọc
    là lọc mất luôn phiếu mượn của họ — không cần ai viết dòng nào. Đã trả giá ba lần: in phiếu của
    bạn đọc đã xoá đổ 500 (K6), bộ đếm "đang dùng" tính cả biểu ghi đã xoá (K15), và gói bàn giao
    thiếu lịch sử của bạn đọc đã xoá (K22). Truy vấn nào phải thấy **mọi** dòng thì dùng
    `IgnoreQueryFilters()` rồi tự lọc theo `DeletedAt` của chính bản ghi.
58. **Rào của bộ gieo dữ liệu phải hỏi đúng câu.** "Kho biểu ghi còn trống chưa" là câu hỏi đúng cho
    200 cuốn sách bịa, nhưng sai cho banner trang chủ và album ảnh — chúng không phải dữ liệu thư
    mục. Máy chủ nghiệm thu có biểu ghi thật từ ngày đầu nên rào ấy đóng vĩnh viễn: trang chủ không
    có banner nào và trang Thư viện ảnh rỗng, dù mã nguồn có sẵn cả hai. Rào của một phần dữ liệu
    phải hỏi về **chính phần ấy**, không hỏi về phần khác.
59. **Câu báo lỗi chỉ đường thì đường ấy phải đi được.** "Hãy xóa biểu ghi ở phân hệ Biên mục trước" —
    làm đúng vậy vẫn bị chặn y nguyên, vì bộ kiểm nhìn cột khóa ngoại chứ không hỏi biểu ghi còn
    sống không (bài học 57 lần thứ tư). Viết một câu chỉ đường thì phải tự đi thử con đường ấy;
    một lối cụt còn tệ hơn câu "không xóa được" trống trơn (bài học 52).
60. **Đổi tham số cấu hình giữa chừng phải nghĩ tới bản ghi đang dở.** Hạ số cấp duyệt từ 2 xuống 1
    làm mọi yêu cầu đặt mua đã qua cấp 1 kẹt vĩnh viễn: hệ thống vẫn đòi "cấp tiếp theo do người
    khác duyệt" trong khi cấp ấy không còn tồn tại. Tham số nào đếm bước của một quy trình thì lúc
    xử lý phải so **trạng thái hiện có** với giá trị đang khai, không cộng mù thêm một bước.
61. **Máy chủ nghiệm thu cũng phải dọn.** Dữ liệu thử của các đợt rà trước — 5 nhà cung cấp `NTx…`,
    9 yêu cầu đặt mua "Nghiệm thu sâu", 3 đơn đặt — nằm nguyên trong màn hình Bổ sung và trong Báo
    cáo duyệt mua. Kịch bản rà nào ghi dữ liệu lên máy chủ thật thì phần dọn là một bước của kịch
    bản ấy, không phải việc để lại cho lần sau; và trước buổi nghiệm thu phải soi lại một lượt.
62. **Nhánh trả về bình thường cũng để lại rác trong bộ theo dõi.** Bộ nhập ISO 2709 dọn
    `ChangeTracker` ở nhánh bắt ngoại lệ — kèm hẳn một dòng chú thích nói vì sao — mà quên nhánh
    "biểu ghi không hợp lệ", vì nhánh ấy `return` chứ không `throw`. Biểu ghi hỏng nằm lại rồi đi
    theo lượt lưu của biểu ghi kế tiếp: hoặc đánh đổ biểu ghi lành, hoặc **được ghi vào kho** dù hệ
    thống vừa từ chối nó. Dọn theo *kết cục* (hỏng là dọn), đừng dọn theo *cách thoát* (ném thì dọn).
63. **`ChangeTracker.Clear()` tháo luôn thứ mình đang cần.** Dòng nhật ký tác vụ bị tháo ra cùng, nên
    từ lần hỏng đầu tiên trở đi mọi lượt ghi tiến độ lặng lẽ không ghi được gì — thanh tiến độ đứng
    im tới lúc kết thúc mà không ai báo lỗi. Dọn xong thì gắn lại thứ phải sống tiếp.
64. **Đừng chạy lệnh ghi hàng loạt lên máy chủ thật để thử.** Một lượt "nhập lại chính tệp vừa xuất"
    để kiểm xử lý trùng đã tạo 268 biểu ghi thừa trên kho nghiệm thu (đối chiếu trùng theo ISBN, mà
    phần lớn biểu ghi thu hoạch không có ISBN). Đã dọn bằng xoá mềm và đối chiếu lại đúng con số cũ,
    nhưng phép thử loại này phải chạy trên kho riêng. Đổi lại nó lộ ra L5 và L6 — ghi vào đây để lần
    sau dựng đúng bối cảnh ấy ở chỗ an toàn.
65. **Georgia không có chữ tiếng Việt hai dấu.** Nó có sẵn trên mọi máy Windows nên hay được xếp vào
    danh sách phông dự phòng, và trình duyệt dùng nó thật — rồi tách ố thành ô cộng dấu sắc rời lấy
    từ phông khác, đặt cạnh nhau chứ không chồng lên. Banner trang chủ máy chủ nghiệm thu hiện "Tài
    liệu sô ́ mới cập nhật" suốt buổi. Cách đo dứt điểm: `canvas.measureText('ố')` so với
    `measureText('ô')` — bằng nhau là phông có chữ, gần gấp đôi là dấu đứng rời. Ảnh SVG nhúng thẳng
    vào địa chỉ là chỗ nguy nhất vì nó không tải được phông web nào.
66. **Cắt khoảng trắng trước khi lọc rỗng là để `null` lọt vào.** `barcodes.Select(v => v.Trim())`
    ném NullReferenceException ngay giữa tầng nghiệp vụ khi mảng JSON có một phần tử `null`, và
    người dùng nhận về 500 "Đã xảy ra lỗi hệ thống" thay vì câu nói mình gửi sai gì. Mảng rỗng và
    chuỗi rỗng đều đã được canh — chỉ `null` lọt, vì nó đi qua đúng chỗ không ai nghĩ tới. Lọc
    `IsNullOrWhiteSpace` trước, cắt sau.
67. **Ký tự rỗng U+0000 là đầu vào mà tầng nghiệp vụ không chặn nổi.** Cột text của PostgreSQL không
    chứa được nó, và lỗi nổ ở tầng trình điều khiển — sau khi câu truy vấn đã gửi đi — nên mọi bộ
    kiểm tra phía trên đều vô can. Một ký tự ấy làm bảy endpoint đổ 500, ba trong đó công khai. Bộ
    lọc `PlainText.RemoveUnstorableCharacters` đã có sẵn từ lâu mà chỉ được gọi ở một đường: lọc
    phải đặt ở **cửa vào** — lớp trung gian cho chuỗi truy vấn, bộ chuyển đổi JSON cho thân yêu cầu
    — chứ không phải ở từng chỗ nhớ ra.
68. **Bài học 1 không chỉ áp cho phiếu mượn và đặt giữ.** Hai luật "một … một" nữa vẫn đang là
    đọc-rồi-ghi: một bạn đọc một thẻ hiệu lực, một kho một kỳ kiểm kê chưa chốt. Ba lượt bấm song
    song để lại ba thẻ và ba kỳ. Cách rà: liệt kê **mọi** câu trong đặc tả có dạng "chỉ một", rồi
    hỏi từng câu "chỉ mục duy nhất tương ứng ở đâu" — đừng đợi gặp lỗi mới đi tìm.
69. **`[Authorize]` không phải là canh quyền.** Thẻ đăng nhập của bạn đọc cũng qua được nó, nên
    endpoint nào quên `[RequirePermission]` là ai có thẻ thư viện cũng gọi được. Lời chú của lớp
    viết "chỉ đòi tài khoản cán bộ" mà không có gì bắt điều đó — và danh sách cán bộ kèm **tên đăng
    nhập** ra ngoài suốt. Cách rà rẻ: quét mã nguồn, mọi endpoint phải chọn một trong ba lối —
    `[RequirePermission]`, `[AllowAnonymous]`, hoặc ghi tên vào danh sách tự canh kèm lý do.
70. **Bộ đếm và danh sách phải chạy trên cùng một tập dòng.** Đếm trên bảng gốc rồi lấy dòng qua một
    phép chiếu có nối bảng là hai câu hỏi khác nhau: mọi navigation bắt buộc trong `Select` biến
    thành INNER JOIN, và bộ lọc xóa mềm ở bảng bên kia lặng lẽ cắt bớt dòng. Hàng đợi biên mục báo
    981 việc mà một trang 200 dòng trả về 155; lịch sử bạn đọc báo 5 phiếu mà trả về 4. Thấy `totalCount`
    lớn hơn số dòng lấy về là thấy đúng cái bẫy này.
71. **Xóa mềm một thực thể là xóa mọi dòng trỏ tới nó khỏi mọi danh sách.** Không phải "ẩn nó đi" mà
    là **cắt cả dòng cha**. Xóa một bản sách đã trả xong là mất lượt mượn khỏi lịch sử bạn đọc, mất
    phiếu khỏi danh sách quầy, và mất khoản phạt khỏi danh sách phạt trong khi vẫn tính vào công nợ.
    Trước khi cho xóa một thứ, hỏi "cái gì đang trỏ tới nó" — rồi hoặc chặn xóa và chỉ sang chức năng
    giữ được lịch sử (thanh lý), hoặc dọn luôn thứ trỏ tới nó (dòng việc hàng đợi).

72. **Viết được bài học không có nghĩa là đã sửa xong lớp lỗi.** Bài học 70 và 71 ra đời ngày
    06/09/2026 từ hai chỗ; hôm sau đo **cả 48 danh sách có phân trang** thì còn nguyên **chín chỗ
    nữa** cùng đúng một lỗi ấy — đặt giữ, tiền phạt, số báo, kỳ kiểm kê, lượt gửi tủ, yêu cầu và
    nhật ký tài liệu số. Sửa một chỗ rồi ghi vào sổ là xong một chỗ. Muốn xong cả lớp thì phải có
    **một phép đo quét hết mọi chỗ cùng loại** — ở đây là một kịch bản đi hết các trang của từng
    danh sách và so `totalCount` với số dòng lấy được. Và `x.Cha != null ? … : …` trong phép chiếu
    **không cứu được dòng**: khóa ngoại bắt buộc thì EF vẫn nối INNER JOIN.
73. **Phân trang mà sắp theo cột không duy nhất là mất dòng, không phải "thứ tự hơi lạ".** Mỗi trang
    là một câu `LIMIT/OFFSET` riêng; các dòng bằng nhau ở cột sắp xếp thì PostgreSQL được phép xếp
    khác đi giữa hai câu, nên một dòng hiện hai lần ở trang sau đồng nghĩa **một dòng khác không bao
    giờ hiện ra**. Đo được: danh sách tiền phạt lấy 396 dòng chỉ có 316 dòng khác nhau; danh sách bạn
    đọc mất một người vì hai bạn đọc trùng họ tên. Mọi chuỗi sắp xếp phải kết thúc bằng khóa chính.
74. **Cưỡng chế bằng tác dụng phụ thì có chỗ không đi qua tác dụng phụ ấy.** Phạm vi dữ liệu theo kho
    của danh sách phiếu mượn không do bộ lọc nào cưỡng chế — nó dựa vào việc phép chiếu nối sang ĐKCB
    đã lọc. Mà `CountAsync` lược bỏ đúng cái JOIN ấy, nên cán bộ được cấp một kho nhìn thấy tổng của
    cả thư viện: 3.122 phiếu, trong khi đi hết các trang chỉ lấy được 302. Luật bảo mật phải viết
    thành một `Where` trên chính bảng, ở chỗ cả phép đếm lẫn phép lấy dòng đều đi qua.

75. **Cách đo một ô lọc mà không cần biết dữ liệu: đưa vào giá trị không thể khớp gì cả.** GUID
    ngẫu nhiên cho ô lọc theo khoá, chuỗi bịa cho ô lọc chữ, mốc "từ ngày" 2999 và "đến ngày" 1900
    cho khoảng thời gian — rồi đòi kết quả bằng 0. Ô lọc chết trả về **đúng tổng gốc**, và đó là dấu
    hiệu duy nhất cần tìm. Rút danh sách phải đo bằng cách quét chính các lớp `*Request` / `*Filter`
    trong tầng Application: 130 ô lọc, 233 phép đo, chạy trong một buổi.
76. **PostgreSQL xếp ô trống lên đầu khi sắp giảm dần.** "Năm xuất bản, mới nhất trước" — thao tác
    tự nhiên nhất của cán bộ biên mục — mở ra là trang trắng, vì 7.465 trong 12.609 biểu ghi thu
    hoạch không mang năm nào; phải lật 150 trang mới tới cuốn 2026. Chiều tăng dần thì mặc định đã
    đúng. Sắp giảm thì phải "ô trống sau cùng" rồi mới giảm dần theo giá trị, và chỉ thêm điều kiện
    ấy cho cột thật sự có thể rỗng — thêm cho cột không rỗng là bỏ phí chỉ mục.
77. **Một danh mục rỗng làm chết bốn màn hình cùng lúc, không kêu tiếng nào.** Bộ gieo dựng kho từ
    phase 6 mà chưa bao giờ dựng **giá**; hai bộ gieo dữ liệu trình diễn đều đọc `_db.Shelves` để
    gán giá cho từng bản, đọc ra danh sách rỗng rồi bỏ qua trong im lặng. Kết quả trên máy chủ
    nghiệm thu: bảng giá rỗng, bản đồ kho không có ô nào, 17.900/17.900 bản "chưa xếp giá", và bạn
    đọc không bao giờ thấy vị trí giá mà IX.2 hứa. Chức năng thì chạy đúng từng bước — thiếu mỗi
    chỗ để xếp vào. Đọc `_db.X` ra rỗng trong bộ gieo thì phải hỏi ngay: **ai lẽ ra phải gieo X?**

78. **Tầng báo cáo là một tầng riêng; sửa danh sách không sửa nó.** Đợt 13 chữa chín danh sách khỏi
    lỗi "phép chiếu đi qua điều hướng bắt buộc làm rơi dòng", nhưng báo cáo dùng truy vấn khác nên
    còn nguyên: báo cáo ĐKCB hủy bỏ trả **0 dòng** trên 3 quyết định, báo cáo lượt xem tài liệu số
    đếm 13 trên 14. Sửa xong một lớp lỗi thì liệt kê **mọi tầng đọc dữ liệu** — danh sách, báo cáo,
    tệp xuất, giao thức — rồi đo lại từng tầng.
79. **Phép thử quét chỉ bắt đúng khuôn nó biết.** Luật "mốc giờ hiện cho người dùng phải là giờ máy"
    có từ K23 nhưng chỉ dò chuỗi định dạng chứa `HH`, nên nhãn kỳ `yyyy-MM` của biểu đồ đi qua tự
    do và một lượt xem lúc 02:00 ngày 01/09 hiện ở cột tháng 8. Nới khuôn ra `yyyy` thì bắt được
    ngay chỗ thứ hai chưa ai biết: `<dc:date>` của tệp metadata xuất ra cũng ghi ngày UTC.
80. **Con số tổng và biểu đồ ngay cạnh nó phải đếm cùng một tập.** Báo cáo dung lượng cộng tổng từ
    bảng tệp (19 tệp, 12,5 MB) còn phần chia theo định dạng cộng từ bảng tài liệu (6 tài liệu,
    266 KB): người đọc thấy hai con số cạnh nhau chênh nhau 50 lần và không biết tin cái nào. Hỏi
    cho từng báo cáo: **tổng đếm cái gì, mỗi lát của biểu đồ đếm cái gì** — hai câu trả lời phải
    trùng nhau.
81. **Cắt bớt trong im lặng là nói dối trong một hồ sơ.** Mọi báo cáo danh sách đều có
    `Take(MaxRows)`, đúng theo mục 6.3, nhưng không chỗ nào nói ra khi trần chạm tới — và tệp xuất
    là thứ đi kèm quyết định. Máy chủ nghiệm thu đang ở 17.900 trên trần 20.000. Đặt trần thì đặt
    luôn câu nói ra khi chạm trần (`ReportRowLimit`).
82. **Đặt văn hoá mặc định của tiến trình, đừng để .NET chọn hộ.** Không khai thì mọi `{tien:N0}` ra
    "2,320,000 đ" giữa câu tiếng Việt — 16 câu thông báo nghiệp vụ và mọi cột tiền của báo cáo in
    ra. Một dòng `CultureInfo.DefaultThreadCurrentCulture = vi-VN` trong `Program.cs` sửa hết; chỗ
    nào cần định dạng cho máy đọc (MARC, ISO 2709, khoá cache) vốn đã khai `InvariantCulture` tại
    chỗ nên không bị kéo theo.

83. **Khoá một tài khoản không khoá được cái thẻ đang cầm.** JWT không có trạng thái ở máy chủ, nên
    lệnh "tạm khoá thẻ" chỉ đổi một cột trong kho mà tầng xác thực không bao giờ đọc lại. Đo trên
    máy chủ thật: khoá thẻ một bạn đọc xong, phiên đang mở vẫn làm được 9 trong 11 việc — trong đó
    có tự cấp cho mình một gói đọc ngoại tuyến còn hạn bảy ngày — và làm mới thẻ được vô thời hạn.
    Câu "chủ thẻ này còn được vào không" thuộc **tầng xác thực** (`OnTokenValidated`), không thuộc
    từng bộ xử lý: đặt ở handler thì chỗ thứ mười lại quên. Đệm ngắn 30 giây cho rẻ, và lệnh khoá
    xoá đệm để tác dụng là tức thì.
84. **Hai lối làm cùng một việc thì so chúng với nhau.** Khoá tài khoản cán bộ thu hồi thẻ làm mới;
    khoá thẻ bạn đọc thì không — cùng một lệnh nghiệp vụ, hai lối cài, một lối thiếu. Mỗi khi thấy
    một cặp "bản cán bộ / bản bạn đọc", "bản web / bản di động", "bản nhập / bản xuất", hãy đọc
    chúng cạnh nhau: chỗ lệch chính là chỗ hỏng.

85. **Một cột chép sẵn phải được cập nhật ở **mọi** lối làm nó đổi, và cập nhật **sau** khi lưu.**
    `readers.debt_amount` là bản chép của tổng phạt chưa thu; hàm đồng bộ có sẵn nhưng chỉ được gọi
    ở hai lối làm giảm nợ (thu, miễn), không ở ba lối làm tăng (lập phạt tại quầy, phạt quá hạn lúc
    ghi trả, đóng phiếu vì mất sách). Quầy cộng thẳng từ bảng nên luôn đúng; ứng dụng di động đọc
    cột chép sẵn nên luôn sai. Và phép cộng chạy trên cơ sở dữ liệu, nên gọi **trước** `SaveChanges`
    là cộng cái kho đang có, bỏ qua dòng còn nằm trong bộ theo dõi — phép thử đầu tiên viết ra đã
    bắt đúng chỗ ấy.

86. **Thư tổng hợp thì một ngày một lần.** Nút "Gửi nhắc hàng loạt" bấm ba lần sinh 1.083 thông báo
    cho 361 bạn đọc trong 18 giây — mỗi người ba lá thư giống hệt nhau, vì nội dung thư là danh
    sách mọi tài liệu quá hạn của người ấy gộp làm một. Loại thông báo nào mang bản chất "tổng hợp
    theo ngày" thì phải khai ra và bộ gửi tự bỏ qua lượt trùng; loại theo từng sự việc (sách đặt
    giữ đã về) thì không.
87. **Lệnh hàng loạt nào cũng phải có trần, kể cả lệnh trông có vẻ nhỏ.** Mười hai lệnh hàng loạt
    của sản phẩm đã có trần từ lâu; đúng hai lối bận nhất trong ngày — ghi mượn và ghi trả ở quầy —
    thì không. Mỗi mã vạch là một lượt tra cộng một lượt kiểm chính sách: 2.000 mã mất 8,9 giây,
    50.000 mã thì proxy cắt ngang. Cách rà rẻ: gửi mảng 50.000 phần tử vào **mọi** lệnh nhận mảng
    và xem cái nào không trả lời trong một giây.
88. **Cùng một sai sót của người dùng thì cả sản phẩm phải trả lời một kiểu.** Khoảng ngày ngược:
    mười sáu bộ lọc trả bảng rỗng im lặng, riêng bảng Tổng quan tự đổi thầm hai mốc. Cả hai đều
    không nói cho người dùng biết họ vừa hỏi sai. Chọn một lối — nói ra — rồi đặt nó ở **đường
    ống**, không ở từng bộ lọc, vì mỗi bộ lọc thêm vào ngày mai là một cơ hội quên.

89. **Trang in nhìn đúng không có nghĩa là lớp chữ đúng.** Ghép chữ (ligature) của phông thay mỗi
    cặp chữ bằng **một glyph**, và bảng ToUnicode ghi vào tệp PDF không tra ngược được glyph ấy về
    hai chữ cái gốc — nó điền đại một mã khác. Mắt đọc "tình trạng", máy đọc "টnh trạng": Ctrl+F
    trong chính tệp báo cáo không tìm ra chữ, chép ra ngoài dán thành chữ Bengali, công cụ đánh chỉ
    mục toàn văn đọc sai cả tệp. Ảnh chụp màn hình không bắt được (bài học 13 ở một chỗ khác). Cách
    đo dứt điểm: rút chữ từ tệp bằng thư viện của người khác rồi tìm ký tự nằm ngoài bảng chữ Việt.
90. **Ô tìm kiếm không phải là ô duyệt.** Danh mục phân cấp duyệt theo từng cấp là đúng — cho tới
    khi người dùng gõ từ khoá. Ràng buộc "chỉ hiện mục cấp gốc" áp cả cho lượt tìm kiếm làm 114
    trong 124 chỉ số phân loại không bao giờ tìm ra được, và màn hình trả bảng trắng không nói vì
    sao. Mỗi khi một danh sách có hai chế độ — duyệt và tìm — hỏi xem bộ lọc của chế độ này có đang
    bám theo chế độ kia không.
91. **Tầng tệp xuất là một tầng riêng, và nó có hai định dạng.** Bài học 78 kể tên bốn tầng đọc dữ
    liệu; tầng tệp xuất còn chia đôi nữa. Dòng "danh sách đã chạm trần" của O5 ghép vào
    `PdfReportHeader.Criteria` nên tới được bản PDF, còn lối Excel không có tham số nào cho phần
    tiêu chí — mà Excel mới là định dạng cán bộ xuất danh sách. Sửa xong một thứ ở tầng tệp thì hỏi
    ngay: **định dạng kia có nhận được nó không?**

92. **Một hằng số màu trông y như một token, nhưng nó không đổi theo chủ đề.** `LcColors.muted`
    đọc lên như thể đã đi qua bảng màu, mà thật ra là một `static const` của bảng màu **nền giấy**:
    bật chế độ tối thì chủ đề đổi, còn 82 chỗ ấy giữ nguyên. Bài học 17 của phía web nguyên hình,
    chỉ khác là ở web màu viết thẳng trông đã sai sẵn (`'#1677ff'`) nên dễ nghi, còn ở đây tên hằng
    số trông như đúng. Hỏi cho mọi bảng màu: **cái tên này đọc ra giá trị nào khi đổi chủ đề?**
93. **Ghim nền thì phải ghim cả chữ.** Hình vẽ thẻ thư viện cố ý giữ giấy trắng ở cả hai chế độ —
    đúng, vì nó vẽ lại tấm thẻ nhựa thật. Nhưng chữ trên nó vẫn lấy `theme.textTheme`, nên ở chế độ
    tối là chữ sáng trên giấy trắng, 1,21 : 1. Và đặt `DefaultTextStyle` không cứu được: kiểu chữ
    của chủ đề **đã mang sẵn màu**, nó đè lên. Cách đúng là dựng cả khối bằng `AppTheme.light()`.
94. **Đo tương phản phải đi khắp cây widget, không đo bảng màu.** Bảng màu đúng vẫn hỏng khi một
    tấm nền sáng gặp chữ của chế độ tối — cặp ấy không có trong bảng nào cả, nó chỉ sinh ra lúc
    dựng. Phép đo đáng tin là: dựng màn hình thật, với mỗi `Text` lấy màu đã phân giải và màu nền
    đục gần nhất phía trên nó, rồi tính. Ba lỗi của đợt 19 đều lộ ra theo đúng đường ấy.

95. **Phép thử quét có vùng loại trừ, và lỗi trốn ở đấy.** Luật "không gọi thẳng hằng số màu chế
    độ sáng" bỏ qua `core/theme/` vì giả định trong ấy cái gì cũng đã theo chủ đề. `StatusPill` là
    một **widget** nằm trong tệp chủ đề, nên ba trong bốn sắc thái của nó ghim màu sáng suốt từ
    phase 15. Cặp màu tự nó đọc được nên cả phép đo tương phản cũng không bắt. Cách chữa không phải
    là thêm ngoại lệ mà là **dời widget ra khỏi vùng loại trừ** — luật chạm tới được thì thôi trốn.
    Và hễ khai một vùng loại trừ, hỏi ngay: trong ấy có thứ gì thuộc loại luật đang canh không?

96. **Máy chủ thêm một khối dữ liệu, máy khách không đọc thì mất trong im lặng.** `/api/public/home`
    có thêm `announcements` từ 04/09/2026 cho trang tra cứu; `HomePayload` của Flutter không khai
    trường ấy nên `fromJson` bỏ qua — không lỗi, không cảnh báo, chỉ là trang chủ thiếu một khối.
    Bài học 30 lật ngược: ở đấy là công tắc lưu mà không ai đọc, ở đây là dữ liệu gửi mà không ai
    dựng. Sửa hợp đồng API thì đi hết **mọi** máy khách, kể cả máy khách viết bằng ngôn ngữ khác.
97. **APK dựng không kèm `--dart-define` là APK trỏ về máy dev.** Mặc định của `LC_API_BASE_URL` là
    `http://10.0.2.2/api`. Cài bản ấy lên máy ảo rồi kết luận "đã kiểm trên máy chủ thật" là sai —
    và nguy hơn: dữ liệu hai nơi khác nhau nên lỗi chỉ có ở máy chủ thật sẽ **không hiện ra**. Dấu
    hiệu rẻ nhất để biết mình đang xem máy nào: đối chiếu bốn con số ở khối thống kê trang chủ với
    `/api/public/home` của máy chủ định kiểm.

98. **Phép thử widget mặc định dựng ở 800×600 — rộng hơn mọi điện thoại.** Bố cục bị bóp trên máy
    thật vẫn "vừa" trong phép thử, nên cả bộ quét cỡ chữ của đợt 19 báo sạch trong khi ảnh iPhone
    Simulator cho thấy nhãn vỡ giữa từ. Mọi phép thử về bố cục phải đặt `tester.view.physicalSize`
    bằng một màn hình điện thoại thật (`dungManHinhDienThoai` trong khung dựng chung).
99. **"Tràn khung" không phải dấu hiệu duy nhất của bố cục hỏng.** Một nhãn bị bóp còn 42 điểm ảnh
    rồi xuống dòng giữa từ thì không ném ngoại lệ nào — nó vẫn vừa. Bất biến đo được ở đây là hình
    dạng ô chữ: chữ ngắn dựng đúng thì **rộng hơn cao**; vỡ thành cột hẹp là cao hơn rộng.

100. **Đo ở đúng khổ đã cam kết, không ở khổ mình đang dùng.** Mục 6.6 hứa admin chạy từ
     1366×768; mười chín đợt chụp ảnh ở 1440×900 và không thấy gì. Ở 1366 có một trang cuộn ngang;
     ở 1440 chính nó chỉ cắt cụt nhãn thành ": 3621" — trông như số liệu, không trông như lỗi. Hễ hồ
     sơ nói một con số tối thiểu (khổ màn hình, cỡ chữ, phiên bản trình duyệt, số bản ghi) thì phép
     đo phải đứng đúng ở con số ấy, không ở chỗ thoải mái hơn.
101. **Tràn khung là một chồng, không phải một chỗ.** Sửa xong nhãn biểu đồ rồi đo lại đúng trang ấy
     thì trang **vẫn cuộn ngang** — thủ phạm mới là ba cái bảng bên dưới, trước đó bị nhãn che khuất
     vì nhãn tràn xa hơn. Phép đo "phần tử nào chạy xa nhất" chỉ kể được một tên mỗi lượt: sửa xong
     phải **đo lại**, không được suy ra là đã hết.
102. **Nhãn vẽ ra ngoài khung là nhãn đẩy cả trang.** Recharts đặt nhãn ngoài của biểu đồ tròn ở
     toạ độ nằm ngoài SVG; tên tiếng Việt dài thì nó ra khỏi màn hình. Mà chú giải ngay dưới biểu đồ
     đã nói đúng những tên ấy — nhãn ngoài vừa thừa vừa phá. Trong lát chỉ nên có phần trăm.
103. **Một hàng ngang phải có ít nhất một phần tử chịu co.** Thanh đầu trang tra cứu có ba khối:
     khối giữa ẩn từ 768 px trở xuống (đúng, điện thoại dùng menu khác), hai khối hai đầu đều khai
     `flex: none`. Không còn ai co được, nên hàng ấy rộng bằng **tổng nội dung của nó** ở mọi màn
     hình — mọi trang của trang tra cứu cuộn ngang 146 px trên máy 375 px. Cách rà rẻ và làm được
     bằng máy: với mỗi hàng flex, kể tên phần tử con được phép co ở bề ngang nhỏ nhất; không kể được
     tên nào là hàng ấy sẽ tràn. Và `flex: 1 1 auto` chưa đủ — thiếu `min-width: 0` thì phần tử flex
     vẫn không chịu co dưới bề ngang nội dung của nó, ba dòng cắt chữ bên dưới thành vô nghĩa.
104. **Lề tự động trên trục ngang huỷ việc kéo giãn của phần tử flex.** `margin: 0 auto` là cách căn
     giữa quen tay và nó đúng trên màn hình rộng; nhưng một phần tử flex có lề tự động thì mất
     `stretch`, nên nó tự lấy bề ngang **nội dung tối thiểu** của mình thay vì bề ngang khung cha —
     trang chi tiết tài liệu rộng 578 px trên màn hình 375 vì một thanh thẻ bên trong rộng 489. Ở
     khổ hẹp thì bỏ lề tự động và khai `width: 100%`.
105. **Cam kết "hỗ trợ mobile" cũng là một con số phải đo.** Bài học 100 áp cho vế "admin tối thiểu
     1366×768" của mục 6.6; vế thứ hai của **chính câu ấy** — "OPAC hỗ trợ mobile" — vẫn chưa ai đo
     suốt hai mươi đợt, vì nó không nói ra con số nào. Câu cam kết không có số thì tự chọn lấy một
     con số bảo vệ được (375×812, khổ logic của phần lớn điện thoại) rồi đo ở đó, đừng coi nó là
     điều không kiểm được.

### A.4. Cơ chế dùng chung — dùng lại, đừng viết chỗ mới

Sáu thứ dưới đây sinh ra để chặn "chỗ thứ tám quên gọi". Thêm chức năng cùng loại thì cắm vào đây,
đừng chép logic sang handler mới:

| Cơ chế | Dùng khi | Ghi chú |
|---|---|---|
| `[AuditRead("Reader")]` (`Api/Security/AuditReadAttribute.cs`) | Endpoint xem chi tiết dữ liệu cá nhân hoặc dữ liệu hạn chế | Chỉ ghi khi `audit_settings` bật `Read` cho thực thể ấy |
| `ExportAuditBehaviour` (đường ống MediatR) | Mọi lượt trả về tệp | Nhận diện theo **kiểu trả về** (`ExportedFile`…), không theo tên lệnh; handler đã tự ghi dòng riêng thì bộ dùng chung im lặng |
| `IStaffNotifier` (`NotifyUsersAsync` / `NotifyGroupAsync` / `NotifyPermissionAsync`) | Việc cần cán bộ biết: chờ duyệt, quá hạn, việc nền hỏng | Người nhận là `Expression<Func<User,bool>>` đẩy xuống SQL; gửi thư hỏng thì ghi nhật ký, không ném |
| `DateRangeBehaviour` (đường ống MediatR) | Mọi yêu cầu có cặp ô ngày | Soi bảy cặp tên (`FromDate`/`ToDate`, `From`/`To`, `CreatedFrom`/`CreatedTo`…) trên chính yêu cầu và trên `Filter` của nó; thêm cặp tên mới thì khai vào đây, đừng kiểm ở handler |
| `ISessionValidator` (`OnTokenValidated` trong `Program.cs`) | Mọi câu hỏi "chủ thẻ đăng nhập này còn được vào không" | Khoá tài khoản / khoá thẻ / xoá hồ sơ phải gọi `ForgetUserAsync` hay `ForgetReaderAsync` ngay sau khi lưu, nếu không đệm 30 giây giữ trạng thái cũ |
| `context.lc` / `LcScheme` (`mobile/lib/core/theme/app_theme.dart`) | Mọi màu ở màn hình của ứng dụng di động | Đổi theo `Theme.of(context).brightness`; gọi thẳng `LcColors.*` là màu đứng yên khi bật chế độ tối |
| `PdfTextStyles.Base()` (`Reporting/Pdf`) | Mọi bộ dựng PDF — báo cáo, phích, thẻ, tem, nhãn, biểu mẫu | Khai phông và tắt ghép chữ ở một chỗ; khai `FontFamily` thẳng là lớp chữ của tệp ấy lại rút ra sai |
| `ReportRowLimit` + tham số `criteria` của `IExcelService.Write` | Mọi lối xuất có trần số dòng hoặc có bộ lọc | Dòng "đã chạm trần" và phần tiêu chí phải tới **cả** bản PDF lẫn bản Excel |
| `IBibRecordWriter.ApplyAsync` | Mọi lượt sửa dữ liệu rút từ MARC | Nhớ `.Include(Authors/Subjects/Keywords/Classifications)`, thiếu là bộ ghi thêm lại liên kết và đổ ở `ux_bib_classifications` |

Lệnh sinh migration chạy đúng trong kho này (dự án hạ tầng vừa là dự án khởi động):

```bash
cd backend && dotnet ef migrations add TenCoNghia   --project src/LibraryConnect.Infrastructure   --startup-project src/LibraryConnect.Infrastructure   --output-dir Persistence/Migrations
```

Cần chen migration vào giữa thì đổi tên tệp **và** sửa `[Migration("…")]` cho khớp.

### A.5. Việc còn treo — chờ người dùng quyết

Không còn mục nào treo.

**Đã chốt, không hỏi lại:**

- **Máy chủ Z39.50 trên bản chạy thật: để tắt.** Ngày 05/09/2026 đọc lại `Chương V.YÊU CẦU VỀ KỸ
  THUẬT.pdf`: hồ sơ chỉ đòi chiều máy khách ("Nhập dữ liệu từ chuẩn Z39.50", kiểm thử 2.4 "tìm/nhận
  biểu ghi qua Z39.50"), không có dòng nào đòi cho thư viện khác tra vào. Chiều máy chủ là do mục
  3.3b của tài liệu này tự thêm. Mã và phép thử giữ nguyên, `docs/07` mục B10 vẫn ghi "Có"; cần
  trình diễn thì bật `ILL.Z3950_SERVER_ENABLED` vài phút rồi tắt. SRU tại `/sru` đã chạy sẵn trên
  bản thật và là lối tương đương.
- H3 và H9 của sổ lỗi đã đóng ngày 03/09/2026.
- **Mật khẩu `admin` của bản chạy thật: giữ nguyên.** Ngày 05/09/2026 người dùng quyết không đổi, dù
  chuỗi ấy trùng với giá trị mặc định trong phép thử Android của kho mã công khai. Không đề xuất lại;
  tài liệu hướng dẫn nghiệm thu giữ như hiện có.

**Phạm vi từ đây:** chỉ làm thứ `Chương V.YÊU CẦU VỀ KỸ THUẬT.pdf` yêu cầu. Không thêm chức năng
ngoài hồ sơ; thấy thiếu thì đối chiếu với tệp PDF trước khi làm.

---

## 0. VAI TRÒ VÀ NHIỆM VỤ

Bạn là kỹ sư phần mềm chính, xây dựng **LibraryConnect** — một **Hệ thống Thư viện Tích hợp (ILS – Integrated Library System)** đầy đủ cho các trường đại học và thư viện tại Việt Nam, đáp ứng hồ sơ mời thầu E-HSMT gói "Mua sắm Phần mềm thư viện số chuẩn kết nối liên Thư viện".

Yêu cầu quan trọng nhất: **hệ thống phải được nghiệm thu bằng cách demo trực tiếp**. Không được để chức năng ở dạng stub, mock, hay "TODO". Mọi chức năng liệt kê trong tài liệu này phải chạy được thật, với dữ liệu thật, kiểm thử được.

Toàn bộ giao diện, thông báo, dữ liệu mẫu và tài liệu bàn giao đều bằng **tiếng Việt**.

### 0.1. Định danh sản phẩm — dùng nhất quán toàn hệ thống

| Hạng mục | Giá trị |
|---|---|
| Tên sản phẩm | **LibraryConnect** |
| Tên đầy đủ (hồ sơ thầu, tài liệu tiếng Việt) | Phần mềm Thư viện số LibraryConnect |
| Slug / thư mục repo | `libraryconnect` |
| Namespace .NET gốc | `LibraryConnect.*` |
| Tên solution | `LibraryConnect.sln` |
| Package npm (admin / opac) | `@libraryconnect/admin`, `@libraryconnect/opac` |
| Docker image | `libraryconnect/api`, `libraryconnect/admin`, `libraryconnect/opac` |
| Docker compose project | `libraryconnect` |
| Tên CSDL PostgreSQL | `libraryconnect` |
| DB user | `libraryconnect` |
| Prefix biến môi trường | `LC_` (ví dụ `LC_DB_HOST`, `LC_JWT_SECRET`, `LC_MINIO_ENDPOINT`) |
| Bucket MinIO | `lc-documents`, `lc-images`, `lc-backups` |
| Flutter package | `libraryconnect_mobile` |
| Application ID Android | `vn.bluestar.libraryconnect` |
| Bundle ID iOS | `vn.bluestar.libraryconnect` |
| Tên hiển thị app mobile | LibraryConnect |
| Prefix Redis key | `lc:` |
| Issuer JWT | `LibraryConnect` |
| Tiêu đề Swagger | `LibraryConnect API` |
| User-Agent khi gọi Z39.50 / OAI-PMH | `LibraryConnect/1.0` |
| Trường MARC `040$a` mặc định (nguồn biên mục) | lấy từ tham số hệ thống, **không hardcode** tên trường học |

**Quy tắc phân biệt quan trọng:** "LibraryConnect" là tên **sản phẩm**, còn tên thư viện/trường sử dụng hệ thống là **dữ liệu cấu hình** (`sys.system_parameters` + `web.cms_settings`). Tuyệt đối không hardcode tên trường, logo, địa chỉ vào code — sản phẩm phải triển khai lại được cho khách hàng khác chỉ bằng cách đổi tham số.

Trên giao diện: header admin hiển thị logo LibraryConnect nhỏ ở góc + tên thư viện của khách hàng làm tiêu đề chính. Trang OPAC hiển thị thương hiệu khách hàng là chính, dòng "Powered by LibraryConnect" ở footer (bật/tắt được bằng tham số).

### 0.2. Phạm vi đợt build này

**Đợt này chỉ xây dựng phần WEB** — backend API + Admin SPA + OPAC SPA. Ứng dụng mobile (Phân hệ XI) sẽ được phát triển ở đợt sau, không nằm trong phạm vi lệnh build hiện tại.

Tuy nhiên **backend phải được thiết kế sẵn để mobile cắm vào mà không phải sửa lại**:

- Toàn bộ nghiệp vụ nằm ở REST API. Không được đặt logic nghiệp vụ trong controller riêng cho web, cũng không được để frontend tự tính toán rồi gửi kết quả xuống.
- Auth dùng JWT access + refresh token (không dùng cookie session), để client mobile dùng lại y nguyên.
- Mọi API trả JSON theo format thống nhất ở mục 11, phân trang server-side, không phụ thuộc trạng thái phiên trên server.
- Xây sẵn và test đầy đủ nhóm endpoint `/api/reader/*` — đây chính là nhóm mà app mobile sẽ gọi (xem danh sách bắt buộc ở Phân hệ XI). OPAC dùng chung nhóm endpoint này, nên chúng được kiểm chứng ngay trong đợt web.
- Chuẩn bị sẵn nhưng chưa cần triển khai: bảng `sys.device_tokens` (lưu FCM token) và service gửi thông báo đẩy dạng interface `INotificationSender` với implementation email trước, FCM sau.
- CORS cấu hình được qua biến môi trường để sau này thêm origin của app.
- Swagger phải mô tả đầy đủ nhóm `/api/reader/*` làm tài liệu cho người viết app sau.

Giữ nguyên thư mục `mobile/` rỗng kèm `README.md` ghi rõ phạm vi đợt sau. Không sinh code Flutter trong đợt này.

> **Lưu ý hồ sơ thầu:** E-HSMT bắt buộc có Phân hệ XI Mobile Application và mục kiểm thử 2.7. Việc hoãn mobile chỉ áp dụng cho **thứ tự phát triển nội bộ**, không có nghĩa gói thầu được phép thiếu app. Phần đặc tả Phân hệ XI vẫn giữ nguyên trong tài liệu này để làm cơ sở cho đợt sau và cho Bảng đáp ứng kỹ thuật.

---

## 1. STACK BẮT BUỘC

| Thành phần | Công nghệ | Ghi chú |
|---|---|---|
| Backend | .NET 8 (ASP.NET Core Web API) | Chạy được cả Linux lẫn Windows Server 2019+ |
| ORM | Entity Framework Core 8 + Npgsql | Code-first, migrations |
| CSDL | PostgreSQL 16 | Encoding UTF8, collation `vi-VN-x-icu` |
| Cache / Queue | Redis 7 | Session, cache tra cứu, hàng đợi biên mục |
| Tìm kiếm | PostgreSQL Full-Text Search + `unaccent` + `pg_trgm` | Không dùng Elasticsearch để giảm chi phí hạ tầng |
| Object storage | MinIO (S3-compatible) | Lưu file tài liệu số, ảnh bìa, avatar |
| Frontend Admin | React 18 + TypeScript + Vite | SPA cho cán bộ thư viện |
| Frontend OPAC | React 18 + TypeScript + Vite | SPA công khai cho bạn đọc |
| UI Library | Ant Design 5 | Bảng biểu nghiệp vụ nhiều, AntD phù hợp |
| State / Data | TanStack Query + Zustand | |
| Mobile | Flutter 3.x | **Đợt sau** — iOS + Android, dùng chung REST API |
| Triển khai | Docker + Docker Compose | Kèm Dockerfile multi-stage cho từng service |
| Reverse proxy | Nginx | Serve static SPA + proxy API |
| Báo cáo | QuestPDF (PDF) + ClosedXML (Excel) | Xuất báo cáo |
| Biểu đồ | Recharts | Báo cáo dạng đồ họa |
| Log | Serilog → file + PostgreSQL | Nhật ký hệ thống |
| Auth | JWT (access + refresh token) | RBAC phân quyền chi tiết |

### Ràng buộc kỹ thuật rút ra từ E-HSMT (phải tuân thủ tuyệt đối)

1. Kiến trúc **3 tầng tách bạch**: Data Layer / Logic Layer / Presentation Layer. Frontend không được truy cập DB trực tiếp.
2. Toàn bộ chuỗi ký tự dùng **Unicode UTF-8**, tuân thủ TCVN 6909:2001. Không dùng VNI/TCVN3.
3. Chạy được trên máy chủ **vật lý lẫn ảo hóa**, hệ điều hành Windows Server 2019+ / Linux / Unix.
4. Tương thích **đa trình duyệt**: Chrome, Edge, Firefox, Safari (2 phiên bản gần nhất).
5. Hỗ trợ vận hành **24/7**, có health check endpoint.
6. Dữ liệu phải lưu trữ **vĩnh viễn** — không có cơ chế tự động xóa cứng. Mọi thao tác xóa là soft-delete (`deleted_at`).
7. Phân quyền **chi tiết đến từng chức năng và từng phạm vi dữ liệu** (kho, thư viện, loại tài liệu).
8. Mọi báo cáo phải có 3 dạng đầu ra: **xem trên màn hình (bảng), đồ họa (chart), xuất file (PDF/Excel)**.
9. **Không được hardcode** danh mục nghiệp vụ — tất cả phải cấu hình được từ giao diện.

---

## 2. CẤU TRÚC REPO

```
libraryconnect/
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
├── CLAUDE.md
├── README.md
├── docs/
│   ├── 01-huong-dan-su-dung.md
│   ├── 02-tai-lieu-quan-tri.md
│   ├── 03-sao-luu-phuc-hoi.md
│   ├── 04-cai-dat-cau-hinh.md
│   ├── 05-api-reference.md
│   ├── 06-kich-ban-kiem-thu.md
│   └── 07-bang-dap-ung-ky-thuat.md
├── backend/
│   ├── LibraryConnect.sln
│   ├── src/
│   │   ├── LibraryConnect.Domain/            # Entities, Value Objects, Enums, Domain Events
│   │   ├── LibraryConnect.Application/       # Use cases, DTOs, Validators, Interfaces
│   │   ├── LibraryConnect.Infrastructure/    # EF Core, Repositories, MinIO, Redis, Email
│   │   ├── LibraryConnect.Marc/              # MARC21 / ISO2709 / Z39.50 / OAI-PMH
│   │   ├── LibraryConnect.Reporting/         # QuestPDF, ClosedXML templates
│   │   └── LibraryConnect.Api/               # Controllers, Middleware, Program.cs
│   └── tests/
│       ├── LibraryConnect.UnitTests/
│       └── LibraryConnect.IntegrationTests/
├── frontend-admin/                    # React SPA cho cán bộ thư viện
│   └── src/
│       ├── modules/                   # Mỗi phân hệ 1 thư mục
│       ├── components/
│       ├── hooks/
│       ├── api/
│       └── layouts/
├── frontend-opac/                     # React SPA công khai
├── mobile/                            # (đợt sau) — đợt này chỉ tạo thư mục + README
└── deploy/
    ├── nginx/
    ├── postgres/init/
    └── scripts/backup.sh, restore.sh
```

**Nguyên tắc code backend:** Clean Architecture + CQRS nhẹ (MediatR). Mỗi use case là một Command/Query handler. FluentValidation cho input. AutoMapper cho DTO. Không đặt logic nghiệp vụ trong Controller.

---

## 3. CHUẨN NGHIỆP VỤ THƯ VIỆN (PHẦN QUAN TRỌNG NHẤT)

Đây là phần dễ làm sai nhất. Đọc kỹ trước khi code.

### 3.1. MARC 21 – Machine Readable Cataloging

Biểu ghi thư mục **không lưu dạng cột phẳng**. Phải lưu đúng cấu trúc MARC:

- **Leader**: chuỗi 24 ký tự cố định (vị trí 05 = record status, 06 = type of record, 07 = bibliographic level, 17 = encoding level...).
- **Control fields** (tag 001–009): chỉ có giá trị, không có indicator, không có subfield. Ví dụ `008` là chuỗi 40 ký tự mã hóa ngày tạo, nước xuất bản, ngôn ngữ...
- **Data fields** (tag 010–999): có 2 indicator (mỗi cái 1 ký tự, có thể là khoảng trắng) và nhiều subfield, mỗi subfield có mã 1 ký tự (`a`–`z`, `0`–`9`).

Các trường bắt buộc phải hỗ trợ đầy đủ (danh sách tối thiểu):

| Tag | Tên | Subfield chính |
|---|---|---|
| 020 | ISBN | $a, $c, $q |
| 022 | ISSN | $a |
| 040 | Nguồn biên mục | $a, $b, $c |
| 041 | Mã ngôn ngữ | $a, $h |
| 044 | Mã nước xuất bản | $a |
| 082 | Chỉ số DDC | $a, $b, $2 |
| 084 | Chỉ số phân loại khác | $a, $2 |
| 100 | Tác giả cá nhân (chính) | $a, $d, $e, $4 |
| 110 | Tác giả tập thể | $a, $b |
| 111 | Tên hội nghị | $a, $c, $d |
| 130 | Nhan đề đồng nhất | $a |
| 245 | Nhan đề và thông tin trách nhiệm | $a, $b, $c, $n, $p |
| 246 | Nhan đề khác | $a, $i |
| 250 | Lần xuất bản | $a |
| 260/264 | Thông tin xuất bản | $a, $b, $c |
| 300 | Mô tả vật lý | $a, $b, $c, $e |
| 310 | Kỳ hạn xuất bản hiện tại | $a |
| 336/337/338 | RDA content/media/carrier | $a, $b, $2 |
| 490 | Tùng thư | $a, $v |
| 500 | Phụ chú chung | $a |
| 504 | Phụ chú thư mục | $a |
| 505 | Phụ chú nội dung | $a |
| 520 | Tóm tắt | $a |
| 650 | Đề mục chủ đề | $a, $x, $y, $z, $2 |
| 653 | Từ khóa tự do | $a |
| 700 | Tác giả bổ sung cá nhân | $a, $e, $4 |
| 710 | Tác giả bổ sung tập thể | $a, $b |
| 773 | Nguồn chủ (bài trích) | $t, $g |
| 852 | Ký hiệu xếp giá | $a, $b, $h, $p |
| 856 | Địa chỉ điện tử | $u, $y, $3 |

**Cách lưu trong PostgreSQL:** dùng cột `jsonb` cho toàn bộ biểu ghi + các cột phẳng được index để tra cứu nhanh (title, author, isbn, publish_year, ddc). Trigger cập nhật cột phẳng khi jsonb thay đổi.

```json
{
  "leader": "00000nam a2200000 a 4500",
  "controlFields": [
    { "tag": "001", "value": "VNU00012345" },
    { "tag": "008", "value": "240115s2023    vm a     b    000 0 vie d" }
  ],
  "dataFields": [
    {
      "tag": "245",
      "ind1": "1", "ind2": "0",
      "subfields": [
        { "code": "a", "value": "Giáo trình cơ sở dữ liệu /" },
        { "code": "c", "value": "Nguyễn Văn A" }
      ]
    }
  ]
}
```

### 3.2. ISO 2709 – Định dạng trao đổi biểu ghi

Phải viết **parser và serializer đầy đủ**, không dùng thư viện ngoài (hệ sinh thái .NET rất mỏng).

Cấu trúc file:
```
[Leader 24 bytes][Directory][FT][Field data...][RT]
```
- `FS` (field terminator) = `0x1E`
- `RS` (record terminator) = `0x1D`
- `SS` (subfield delimiter) = `0x1F`
- Directory: mỗi entry 12 bytes = tag(3) + length(4) + start position(5)
- Leader vị trí 00–04 = tổng độ dài record (5 chữ số, pad 0), 12–16 = base address of data

**Lưu ý sống còn:** độ dài trường phải tính theo **byte UTF-8**, không phải số ký tự. Tiếng Việt có dấu chiếm 2–3 byte. Sai chỗ này là file xuất ra không import được vào phần mềm khác → trượt nghiệm thu mục 2.4.

Viết unit test: encode → decode → so sánh phải bằng biểu ghi gốc (round-trip test) với dữ liệu tiếng Việt có dấu.

### 3.3. Z39.50 – Giao thức tra cứu liên thư viện

Đây là **yêu cầu cốt lõi của gói thầu** ("chuẩn kết nối liên Thư viện"). Cần cả 2 chiều:

**a) Z39.50 Client** (nhập biểu ghi từ thư viện khác):
- Kết nối TCP tới host:port của server đích (mặc định port 210).
- Encode/decode BER (Basic Encoding Rules) của ASN.1.
- Các PDU cần implement: `InitRequest/InitResponse`, `SearchRequest/SearchResponse`, `PresentRequest/PresentResponse`, `Close`.
- Query dùng **Type-1 query (RPN)** với Bib-1 Attribute Set. Các use attribute quan trọng: 1=Personal name, 4=Title, 7=ISBN, 8=ISSN, 21=Subject, 1016=Any.
- Record syntax yêu cầu: `USMARC` (OID 1.2.840.10003.5.10) hoặc `MARC21`.
- Cấu hình được danh sách server đích trong giao diện (tên, host, port, database name, username/password, charset).
- Server mẫu để test: Library of Congress (`lx2.loc.gov:210/LCDB`).

**b) Z39.50 Server** (cho thư viện khác tra cứu vào hệ thống mình):
- Lắng nghe TCP, xử lý Init/Search/Present, trả biểu ghi MARC21.
- Cấu hình bật/tắt, giới hạn IP.

**Fallback bắt buộc:** cài đặt song song **SRU/SRW** (Search/Retrieve via URL) — đây là phiên bản HTTP của Z39.50, dễ implement hơn nhiều và được chấp nhận là "giải pháp tương đương" theo Ghi chú chung của Chương V. Endpoint: `/sru?operation=searchRetrieve&version=1.2&query=...&recordSchema=marcxml`.

### 3.4. OAI-PMH – Harvest metadata

Implement **cả provider lẫn harvester**.

Provider endpoint `/oai` hỗ trợ 6 verb:
- `Identify`, `ListMetadataFormats`, `ListSets`, `ListIdentifiers`, `ListRecords`, `GetRecord`
- Metadata prefix bắt buộc: `oai_dc` (Dublin Core), khuyến nghị thêm `marc21`
- Hỗ trợ `resumptionToken` phân trang, `from`/`until` lọc theo thời gian

Harvester: cấu hình được nguồn, lịch chạy định kỳ (Hangfire/Quartz), map Dublin Core → MARC21.

### 3.5. MARCXML

Import/export theo schema `http://www.loc.gov/MARC21/slim`. Dùng cho SRU và OAI-PMH.

---

## 4. MÔ HÌNH DỮ LIỆU POSTGRESQL

Đặt tên bảng `snake_case`, số nhiều. Mọi bảng có: `id` (uuid, default `gen_random_uuid()`), `created_at`, `created_by`, `updated_at`, `updated_by`, `deleted_at` (soft delete).

### 4.1. Nhóm Hệ thống (schema `sys`)

```
users                 -- id, username, password_hash, full_name, email, phone, is_active,
                      -- must_change_password, last_login_at, failed_login_count, locked_until
user_groups           -- id, code, name, description, is_system
user_group_members    -- user_id, group_id
permissions           -- id, code (vd: CATALOG.BIB.CREATE), module, name, description
group_permissions     -- group_id, permission_id
user_data_scopes      -- user_id, scope_type (LIBRARY|WAREHOUSE|DOCTYPE), scope_id
system_parameters     -- key, value, data_type, group, name, description, is_editable
audit_logs            -- id, user_id, username, ip, user_agent, action, entity, entity_id,
                      -- old_value(jsonb), new_value(jsonb), result, message, occurred_at
audit_settings        -- entity, log_create, log_update, log_delete, log_read, retention_days
backup_jobs           -- id, type(FULL|INCREMENTAL), status, file_path, size_bytes,
                      -- started_at, finished_at, message, is_auto
notifications         -- id, user_id, type, title, body, is_read, link, created_at
```

### 4.2. Nhóm Danh mục (schema `cat`)

Mỗi danh mục là bảng riêng, đều có `code`, `name`, `name_en`, `sort_order`, `is_active`, `parent_id` (nếu phân cấp):

```
document_types        -- Dạng tài liệu: Sách, Báo, Tạp chí, Luận văn, Luận án, Đề tài NC, Bản đồ...
carrier_types         -- Vật mang tin: Giấy, CD/DVD, File số, Vi phim, Băng từ...
languages             -- Ngôn ngữ (mã ISO 639-2: vie, eng, fra...)
countries             -- Nước xuất bản (mã MARC)
publishers            -- Nhà xuất bản
authors               -- Tác giả (authority file): họ tên, năm sinh/mất, vai trò, tên khác
subjects              -- Đề mục chủ đề (phân cấp)
keywords              -- Từ khóa
classifications       -- Khung phân loại: DDC, BBK, LCC (phân cấp, có ký hiệu + tên)
series                -- Tùng thư
collections           -- Bộ sưu tập
reader_types          -- Loại bạn đọc: Sinh viên, Học viên, NCS, Giảng viên, CBNV, Khách
faculties             -- Khoa
majors                -- Ngành đào tạo
courses               -- Môn học
suppliers             -- Nhà cung cấp: tên, MST, địa chỉ, liên hệ, tài khoản NH
funding_sources       -- Nguồn kinh phí
custom_indexes        -- Danh mục tự tạo: id, name, marc_tag, marc_subfield, is_hierarchical
custom_index_values   -- custom_index_id, code, name, parent_id
```

### 4.3. Nhóm Biên mục (schema `bib`)

```
bib_records           -- id, control_number(001), record_status, marc_data(jsonb),
                      -- title, subtitle, statement_of_responsibility, author_main,
                      -- isbn, issn, publisher_id, publish_place, publish_year, edition,
                      -- pages, dimensions, ddc, language_id, document_type_id,
                      -- carrier_type_id, series_id, abstract, cover_image_url,
                      -- search_vector(tsvector), status(DRAFT|QUEUED|APPROVED|PUBLISHED),
                      -- source(MANUAL|ISO2709|Z3950|EXCEL|OAI), source_ref
bib_authors           -- bib_id, author_id, role, is_main, sort_order
bib_subjects          -- bib_id, subject_id
bib_keywords          -- bib_id, keyword_id
bib_classifications   -- bib_id, classification_id, scheme
bib_courses           -- bib_id, course_id, relation_type(GIÁO TRÌNH|THAM KHẢO)
marc_templates        -- id, name, document_type_id, is_default, fields(jsonb)
marc_field_defaults   -- tag, ind1, ind2, subfield, default_value, document_type_id
marc_field_definitions-- tag, name, is_repeatable, is_control, indicators(jsonb),
                      -- subfields(jsonb), is_required
catalog_queue         -- id, bib_id, assigned_to, priority, status, note, deadline
card_templates        -- id, name, size, layout(jsonb), fields_mapping(jsonb)
```

### 4.4. Nhóm Bổ sung & Kho (schema `acq`)

```
libraries             -- Thư viện (Trụ sở, Cơ sở Nhà Bè): code, name, address, phone
warehouses            -- Kho: library_id, code, name, type(KHO MỞ|KHO ĐÓNG|PHÒNG ĐỌC|THANH LÝ)
shelves               -- Giá: warehouse_id, code, name, capacity
purchase_requests     -- id, code, type(MONOGRAPH|SERIAL), requester_id, department,
                      -- request_date, reason, status(DRAFT|SUBMITTED|APPROVED|REJECTED),
                      -- approved_by, approved_at, reject_reason, total_amount
purchase_request_items-- request_id, title, author, publisher, isbn, quantity, unit_price,
                      -- estimated_amount, supplier_id, note, bib_id
purchase_orders       -- id, code, supplier_id, order_date, expected_date, funding_source_id,
                      -- contract_no, total_amount, status(NEW|ORDERED|PARTIAL|RECEIVED|CANCELLED)
purchase_order_items  -- order_id, request_item_id, bib_id, quantity, unit_price, received_qty
handover_records      -- id, code, order_id, handover_date, party_a, party_b, content, file_url
items                 -- Ấn phẩm (ĐKCB): id, bib_id, barcode, register_number,
                      -- warehouse_id, shelf_id, call_number, price, funding_source_id,
                      -- acquisition_date, acquisition_type(MUA|TẶNG|TRAO ĐỔI|NỘP LƯU CHIỂU),
                      -- order_id, status(CHƯA KIỂM NHẬN|TRONG KHO|ĐANG MƯỢN|ĐẶT GIỮ|
                      -- MẤT|HỎNG|THANH LÝ|ĐANG KIỂM KÊ), condition, is_locked,
                      -- lock_reason, note, volume_number, copy_number
item_movements        -- item_id, from_warehouse_id, to_warehouse_id, movement_date,
                      -- reason, decision_no, performed_by
item_disposals        -- item_id, disposal_date, reason, decision_no, approved_by, value
barcode_templates     -- name, width, height, layout(jsonb), barcode_type(CODE39|CODE128|QR)
label_templates       -- name, width, height, layout(jsonb)
inventory_periods     -- id, code, name, warehouse_id, start_date, end_date,
                      -- status(CHUẨN BỊ|ĐANG KIỂM KÊ|ĐÃ ĐÓNG), closed_by, closed_at
inventory_scans       -- period_id, item_id, barcode, scanned_at, scanned_by, device
inventory_results     -- period_id, item_id, expected_status, actual_status,
                      -- result(KHỚP|THIẾU|THỪA|SAI KHO), note
```

### 4.5. Nhóm Ấn phẩm định kỳ (schema `ser`)

```
serials               -- id, bib_id, title, issn, publisher_id, language_id,
                      -- frequency(NHẬT BÁO|TUẦN|NỬA THÁNG|THÁNG|QUÝ|NĂM|KHÔNG ĐỊNH KỲ),
                      -- frequency_config(jsonb), warehouse_id, subscription_start,
                      -- subscription_end, status
serial_predictions    -- serial_id, expected_issue_no, expected_volume, expected_year,
                      -- expected_date, is_generated
serial_issues         -- Số cụ thể: serial_id, issue_no, volume, year, issue_date,
                      -- received_date, received_by, quantity, status(DỰ KIẾN|ĐÃ NHẬN|
                      -- THIẾU|KHIẾU NẠI), barcode, warehouse_id, note
serial_issue_articles -- Mục lục bài trích: issue_id, title, authors, page_from, page_to,
                      -- abstract, keywords, bib_id
serial_bindings       -- Đóng tập: id, serial_id, code, from_issue, to_issue, year,
                      -- binding_date, item_id (sinh ĐKCB mới), note
serial_claims         -- issue_id, claim_date, claim_no, supplier_id, response, status
```

### 4.6. Nhóm Tài liệu số (schema `dig`)

```
digital_collections   -- id, code, name, parent_id, description, access_level
digital_documents     -- id, bib_id, collection_id, title, file_name, file_path,
                      -- file_size, mime_type, page_count, checksum_sha256,
                      -- access_level(CÔNG KHAI|NỘI BỘ|HẠN CHẾ|CẤM),
                      -- allow_download, allow_print, watermark_enabled,
                      -- preview_pages, upload_by, upload_at, view_count, download_count
digital_document_files-- document_id, type(ORIGINAL|PREVIEW|THUMBNAIL|OCR_TEXT), path, size
digital_access_requests-- id, document_id, reader_id, request_date, reason,
                      -- status(CHỜ DUYỆT|ĐÃ DUYỆT|TỪ CHỐI|HẾT HẠN), approved_by,
                      -- approved_at, expire_at, reject_reason, max_views, view_count
digital_access_logs   -- document_id, reader_id, action(VIEW|DOWNLOAD|PRINT), ip,
                      -- device, page_from, page_to, duration_seconds, occurred_at
```

### 4.7. Nhóm Bạn đọc (schema `rdr`)

```
readers               -- id, card_number, student_code, full_name, gender, date_of_birth,
                      -- id_card_number, email, phone, address, avatar_url, photo_url,
                      -- reader_type_id, faculty_id, major_id, class_name, course_year,
                      -- card_issue_date, card_expire_date,
                      -- status(HOẠT ĐỘNG|HẾT HẠN|TẠM KHÓA|KHÓA|ĐÃ RA TRƯỜNG),
                      -- deposit_amount, debt_amount, note, user_id
reader_cards          -- reader_id, card_number, issue_date, expire_date, print_count,
                      -- template_id, is_current, reissue_reason
card_templates_reader -- name, width, height, front_layout(jsonb), back_layout(jsonb)
reader_import_batches -- file_name, total_rows, success_rows, error_rows, errors(jsonb)
reader_violations     -- reader_id, type, description, fine_amount, occurred_at, resolved_at
```

### 4.8. Nhóm Lưu thông (schema `cir`)

```
circulation_policies  -- id, name, reader_type_id, document_type_id, warehouse_id,
                      -- max_items, loan_days, max_renewals, renewal_days,
                      -- fine_per_day, grace_days, max_holds, hold_expire_days,
                      -- allow_loan, allow_renew, allow_hold, priority, is_active
loans                 -- id, code, reader_id, item_id, loan_date, due_date, return_date,
                      -- renewed_count, status(ĐANG MƯỢN|ĐÃ TRẢ|QUÁ HẠN|MẤT|HỎNG),
                      -- loan_by, return_by, loan_type(TẠI CHỖ|VỀ NHÀ|SELF_CHECKOUT),
                      -- fine_amount, fine_paid, note
loan_renewals         -- loan_id, renewal_date, old_due_date, new_due_date,
                      -- requested_by, approved_by, channel(QUẦY|OPAC|MOBILE)
holds                 -- id, reader_id, bib_id, item_id, hold_date, expire_date,
                      -- pickup_warehouse_id, status(CHỜ|SẴN SÀNG|ĐÃ NHẬN|HẾT HẠN|HỦY),
                      -- queue_position, notified_at
fines                 -- reader_id, loan_id, type(QUÁ HẠN|MẤT|HỎNG|KHÁC), amount,
                      -- paid_amount, paid_at, paid_by, waived, waive_reason, note
lockers               -- code, location_id, size, status(TRỐNG|ĐANG DÙNG|HỎNG|KHÓA)
locker_usages         -- locker_id, reader_id, checkin_at, checkout_at, key_number, note
library_visits        -- reader_id, checkin_at, checkout_at, gate, purpose
circulation_templates -- name, type(PHIẾU MƯỢN|PHIẾU TRẢ|BIÊN LAI PHẠT), layout(jsonb)
```

### 4.9. Nhóm Nội dung & OPAC (schema `web`)

```
cms_pages             -- slug, title, content(html), meta_description, is_published,
                      -- published_at, view_count, sort_order, parent_id
cms_news              -- title, slug, summary, content, thumbnail_url, category_id,
                      -- tags, author, is_featured, is_published, published_at, view_count
cms_news_categories   -- code, name, sort_order
cms_banners           -- title, image_url, link, position, sort_order, start_date, end_date
cms_menus             -- name, url, parent_id, sort_order, target, icon, is_active
cms_settings          -- Logo, tên thư viện, giờ mở cửa, địa chỉ, hotline, mạng xã hội
opac_search_logs      -- keyword, search_type, result_count, reader_id, ip, occurred_at
opac_saved_searches   -- reader_id, name, query(jsonb), alert_enabled
opac_favorites        -- reader_id, bib_id, created_at
opac_reviews          -- bib_id, reader_id, rating, comment, is_approved
```

### 4.10. Nhóm Liên thư viện (schema `ill`)

```
z3950_targets         -- name, host, port, database_name, username, password,
                      -- charset, record_syntax, timeout_seconds, is_active, sort_order
z3950_search_logs     -- target_id, query, result_count, duration_ms, occurred_at, user_id
oai_repositories      -- name, base_url, metadata_prefix, set_spec, last_harvest_at,
                      -- schedule_cron, is_active
oai_harvest_logs      -- repository_id, started_at, finished_at, records_fetched,
                      -- records_imported, errors
import_export_jobs    -- type(ISO2709_IN|ISO2709_OUT|EXCEL_IN|MARCXML_IN|MARCXML_OUT),
                      -- file_name, total, success, failed, errors(jsonb), status,
                      -- created_by, started_at, finished_at
api_clients           -- name, client_id, client_secret_hash, scopes, rate_limit, is_active
```

### 4.11. Index bắt buộc

```sql
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Tra cứu tiếng Việt không dấu
CREATE INDEX idx_bib_search ON bib.bib_records USING GIN(search_vector);
CREATE INDEX idx_bib_title_trgm ON bib.bib_records USING GIN(unaccent(title) gin_trgm_ops);
CREATE INDEX idx_bib_marc ON bib.bib_records USING GIN(marc_data jsonb_path_ops);
CREATE UNIQUE INDEX idx_item_barcode ON acq.items(barcode) WHERE deleted_at IS NULL;
CREATE INDEX idx_loan_reader_status ON cir.loans(reader_id, status);
CREATE INDEX idx_loan_due ON cir.loans(due_date) WHERE status = 'ĐANG MƯỢN';
CREATE INDEX idx_audit_occurred ON sys.audit_logs(occurred_at DESC);
```

Hàm chuẩn hóa tiếng Việt để tìm kiếm không dấu — tạo immutable function `vn_unaccent(text)` và dùng trong index.

---

## 5. ĐẶC TẢ CHI TIẾT 11 PHÂN HỆ

> Với mỗi chức năng: liệt kê màn hình, API, quy tắc nghiệp vụ. Tất cả đều phải chạy thật.

### PHÂN HỆ I — QUẢN TRỊ HỆ THỐNG

**I.1. Quản lý nhóm người dùng**
- Màn hình danh sách nhóm: tìm kiếm, phân trang, lọc theo trạng thái.
- Thêm/sửa/xóa nhóm (nhóm hệ thống `is_system=true` không cho xóa).
- Gán quyền cho nhóm: cây quyền phân cấp theo module → chức năng → hành động (Xem/Thêm/Sửa/Xóa/Duyệt/In/Xuất). Checkbox tri-state, chọn cha tự chọn con.
- Sao chép quyền từ nhóm khác.
- Xem danh sách thành viên trong nhóm, thêm/bớt thành viên hàng loạt.
- API: `GET/POST/PUT/DELETE /api/admin/user-groups`, `GET/PUT /api/admin/user-groups/{id}/permissions`, `POST /api/admin/user-groups/{id}/clone`

**I.2. Quản lý người dùng**
- Danh sách: lọc theo nhóm, trạng thái, khoa/phòng; tìm theo tên/username/email.
- Thêm/sửa: thông tin cá nhân, gán nhiều nhóm, gán phạm vi dữ liệu (thư viện/kho được phép thao tác).
- Đặt lại mật khẩu (buộc đổi lần đăng nhập đầu), khóa/mở khóa tài khoản.
- Chính sách mật khẩu cấu hình được: độ dài tối thiểu, ký tự đặc biệt, hạn đổi mật khẩu, khóa sau N lần sai.
- Import người dùng từ Excel.
- Xem lịch sử đăng nhập của từng user.
- API: `/api/admin/users`, `/api/admin/users/{id}/reset-password`, `/api/admin/users/{id}/lock`

**I.3. Tham số hệ thống**
- Giao diện chỉnh sửa tham số theo nhóm, mỗi tham số có kiểu dữ liệu (text/number/bool/date/json/file) và render control tương ứng.
- Nhóm tham số tối thiểu: Thông tin thư viện, Quy tắc sinh mã (ĐKCB, số thẻ, mã đơn đặt — có prefix/suffix/độ dài/reset theo năm), Cấu hình email SMTP, Cấu hình sao lưu, Cấu hình lưu thông mặc định, Cấu hình OPAC, Cấu hình mobile, Cấu hình biên mục (MARC template mặc định), Giới hạn upload.
- Lịch sử thay đổi tham số (ai đổi, từ giá trị nào sang giá trị nào).
- API: `GET/PUT /api/admin/parameters`

**I.4. Nhật ký hệ thống**
- **Cài đặt chế độ ghi nhận**: bảng cấu hình theo từng entity, bật/tắt ghi log cho Create/Update/Delete/Read; đặt thời gian lưu trữ (nhưng mặc định là vĩnh viễn theo yêu cầu E-HSMT).
- **Tra cứu nhật ký**: lọc theo khoảng thời gian, người dùng, hành động, đối tượng, kết quả (thành công/thất bại), IP. Xem chi tiết diff giá trị cũ/mới dạng JSON được highlight. Xuất Excel/PDF.
- Ghi log tự động qua EF Core Interceptor + Middleware, không phải viết thủ công ở từng handler.
- API: `GET /api/admin/audit-logs`, `GET/PUT /api/admin/audit-settings`

**I.5. Sao lưu cơ sở dữ liệu**
- Sao lưu thủ công: nút "Sao lưu ngay", chọn Full/Data-only, hiển thị tiến trình.
- Sao lưu tự động: cấu hình lịch (cron), thư mục đích, số bản giữ lại, gửi email khi lỗi.
- Danh sách bản sao lưu: tên file, dung lượng, thời gian, trạng thái; tải về, xóa, **phục hồi**.
- Phục hồi: cảnh báo 2 bước, yêu cầu nhập lại mật khẩu admin, ghi log.
- Backend gọi `pg_dump`/`pg_restore` qua process; script đóng gói sẵn trong container.
- Kèm backup file MinIO (tài liệu số).
- API: `/api/admin/backups`, `POST /api/admin/backups/{id}/restore`

---

### PHÂN HỆ II — BIÊN MỤC

**II.1. Cài đặt giá trị ngầm định cho trường MARC 21**
- Bảng cấu hình: chọn dạng tài liệu → khai báo tag/ind1/ind2/subfield → giá trị mặc định.
- Ví dụ: sách tiếng Việt mặc định `040$a = Thư viện ĐH TN&MT TP.HCM`, `041$a = vie`, `008` vị trí 35–37 = `vie`.
- Khi tạo biểu ghi mới, tự động điền.

**II.2. Thêm mới ấn phẩm (biên mục chi tiết)**
- **Trình soạn MARC chuyên nghiệp** — đây là màn hình quan trọng nhất của cả hệ thống:
  - Bảng nhập theo dòng: cột Tag | Ind1 | Ind2 | Nội dung (các subfield).
  - Gõ tag → hiện gợi ý tên trường tiếng Việt (tooltip từ `marc_field_definitions`).
  - Nhập subfield bằng ký tự phân cách (`$a`, `$b`) hoặc bằng form chi tiết bung ra.
  - Nút thêm/xóa/nhân bản dòng, kéo thả sắp xếp, trường lặp được thì cho phép lặp.
  - Validate realtime: trường bắt buộc (Leader, 008, 245), indicator hợp lệ, subfield hợp lệ theo định nghĩa.
  - Wizard hỗ trợ nhập `008` (form hóa 40 vị trí thành các dropdown có nghĩa).
  - Chọn mẫu biên mục (`marc_templates`) theo dạng tài liệu để load khung sẵn.
  - Xem trước dạng ISBD và dạng thẻ mục lục.
  - Nút "Lấy từ Z39.50" / "Lấy từ ISBN" ngay trên form.
  - Ctrl+S lưu, Ctrl+D nhân bản dòng.
- Sau khi lưu biểu ghi, chuyển sang tab "Ấn phẩm" để tạo ĐKCB (số bản, kho, giá, ký hiệu xếp giá) — sinh barcode tự động theo quy tắc.
- Upload ảnh bìa, đính kèm file tài liệu số.

**II.3. Cập nhật / Xóa / Xem chi tiết ấn phẩm**
- Cập nhật: mở lại trình soạn MARC, ghi lại lịch sử phiên bản (giữ mọi phiên bản cũ, xem diff, khôi phục phiên bản).
- Xóa: soft delete, chặn nếu còn ĐKCB đang lưu thông; yêu cầu nhập lý do.
- Xem chi tiết: 4 tab — Thông tin thư mục (dạng ISBD dễ đọc) | MARC thô | Danh sách ĐKCB kèm trạng thái vị trí | Lịch sử lưu thông & lịch sử sửa đổi.

**II.4. Hàng đợi biên mục chi tiết**
- Biểu ghi từ biên mục sơ lược (phân hệ Bổ sung) hoặc import tự động vào hàng đợi.
- Màn hình dạng kanban hoặc bảng: Chờ xử lý | Đang biên mục | Chờ duyệt | Đã hoàn thành.
- Phân công cán bộ, đặt độ ưu tiên và hạn xử lý, ghi chú.
- Duyệt/trả lại kèm lý do. Thống kê năng suất biên mục theo cán bộ.

**II.5. Cập nhật mẫu/trường biên mục**
- CRUD `marc_field_definitions`: tag, tên tiếng Việt, mô tả, lặp được không, danh sách indicator hợp lệ (giá trị + ý nghĩa), danh sách subfield hợp lệ (mã + tên + lặp được không).
- CRUD `marc_templates`: tạo khung biên mục cho từng dạng tài liệu, đặt mẫu mặc định.
- Import bộ định nghĩa MARC21 chuẩn (seed sẵn ~200 trường thông dụng).

**II.6. Nhập dữ liệu từ biểu ghi ISO 2709**
- Upload file `.iso`/`.mrc` (hỗ trợ nhiều file, tối đa cấu hình được).
- Bước 1: parse và hiển thị preview danh sách biểu ghi, đánh dấu lỗi từng biểu ghi.
- Bước 2: cấu hình xử lý trùng (theo ISBN / 001 / nhan đề+tác giả): Bỏ qua | Ghi đè | Tạo mới | Gộp.
- Bước 3: chọn dạng tài liệu, kho mặc định, có tạo ĐKCB tự động không.
- Bước 4: import chạy nền (background job), có thanh tiến trình, báo cáo kết quả, tải file log lỗi.
- **Xuất ISO 2709**: chọn biểu ghi theo bộ lọc hoặc tick chọn, xuất ra file tải về.

**II.7. Nhập dữ liệu từ chuẩn Z39.50**
- Màn hình tra cứu: chọn 1 hoặc nhiều server đích (tra song song), nhập từ khóa theo tiêu chí (Nhan đề / Tác giả / ISBN / ISSN / Chủ đề / Bất kỳ).
- Kết quả hiển thị theo từng server, xem trước biểu ghi MARC, so sánh với biểu ghi đã có trong hệ thống.
- Chọn biểu ghi → "Nhập vào hệ thống" → mở trình soạn MARC để hiệu đính trước khi lưu.
- Quản lý danh sách server: thêm/sửa/xóa, nút "Kiểm tra kết nối".

**II.8. Nhập dữ liệu từ Excel**
- Tải file mẫu Excel có sẵn header tiếng Việt và sheet hướng dẫn.
- Upload → mapping cột Excel sang trường MARC (giao diện kéo thả hoặc dropdown), lưu được mapping profile để dùng lại.
- Validate từng dòng, hiển thị bảng lỗi có thể sửa trực tiếp trên màn hình rồi import lại.
- Import chạy nền, xuất file kết quả.

**II.9. Quản lý danh mục (chỉ mục)**
- **Danh mục có sẵn**: CRUD tất cả bảng ở mục 4.2, có import/export Excel, gộp trùng (merge 2 tác giả trùng tên → cập nhật toàn bộ biểu ghi liên quan).
- **Danh mục tự tạo từ trường MARC 21**: người dùng khai báo một danh mục mới bằng cách chỉ định tag+subfield nguồn (ví dụ tạo danh mục "Nơi xuất bản" từ `260$a`). Hệ thống quét toàn bộ biểu ghi, rút trích giá trị duy nhất, cho phép chuẩn hóa/gộp, sau đó dùng làm bộ lọc trong tra cứu.

**II.10. Xử lý phích (thẻ mục lục)**
- **Tạo mẫu phích**: designer kéo thả, khổ giấy chuẩn 7.5×12.5cm hoặc tùy chỉnh, đặt các ô nội dung ánh xạ tới trường MARC, chọn font/cỡ chữ/căn lề/viền.
- Các loại phích: phích chính (tác giả), phích nhan đề, phích chủ đề, phích phân loại.
- **In phích**: chọn biểu ghi (đơn lẻ hoặc hàng loạt theo bộ lọc), chọn mẫu, xem trước, xuất PDF đúng khổ, hỗ trợ in nhiều phích trên 1 trang A4.

---

### PHÂN HỆ III — BỔ SUNG

**III.1. Quản lý đơn đặt**

*Yêu cầu đặt mua ấn phẩm đơn bản:*
- Form đề nghị mua: người đề nghị, đơn vị, lý do, nguồn kinh phí dự kiến.
- Thêm từng đầu sách: nhan đề, tác giả, NXB, năm, ISBN, số lượng, đơn giá dự kiến, nhà cung cấp gợi ý.
- Nút tra cứu nhanh: kiểm tra thư viện đã có tài liệu này chưa (theo ISBN/nhan đề), cảnh báo trùng.
- Import danh sách đề nghị từ Excel.
- Gửi duyệt → chuyển trạng thái, thông báo tới người duyệt.

*Yêu cầu đặt mua ấn phẩm định kỳ:*
- Form riêng: tên báo/tạp chí, ISSN, kỳ hạn, số kỳ/năm, thời gian đặt (từ tháng/năm đến tháng/năm), đơn giá/kỳ, tổng tiền tự tính.

*Duyệt yêu cầu:*
- Danh sách yêu cầu chờ duyệt, xem chi tiết, duyệt toàn bộ hoặc duyệt từng dòng (có thể sửa số lượng), từ chối kèm lý do.
- Cấu hình được quy trình duyệt nhiều cấp.

*Quản lý đơn đặt:*
- Tạo đơn đặt hàng từ các yêu cầu đã duyệt (gộp nhiều yêu cầu, nhóm theo nhà cung cấp).
- Thông tin đơn: mã đơn, NCC, ngày đặt, ngày dự kiến giao, số hợp đồng, nguồn kinh phí.
- In đơn đặt hàng (PDF theo mẫu).
- Theo dõi tình trạng giao hàng: nhận từng phần, ghi nhận số lượng thực nhận, tự động chuyển trạng thái.
- Cảnh báo đơn quá hạn giao.

*Biên bản bàn giao:*
- Tạo biên bản từ đơn đặt: bên giao, bên nhận, danh sách tài liệu, số lượng, tình trạng.
- In PDF theo mẫu chuẩn, đính kèm file scan bản ký.

*Báo cáo duyệt mua:* thống kê yêu cầu theo trạng thái, đơn vị đề nghị, thời gian; tỷ lệ duyệt/từ chối; tổng kinh phí duyệt.

*Quản lý nhà cung cấp:* CRUD, thông tin liên hệ, mã số thuế, lịch sử giao dịch, đánh giá.

**III.2. Quản lý thông tin bổ sung**

*Biên mục sơ lược (tuân thủ MARC 21):*
- Form rút gọn ~10 trường (nhan đề, tác giả, NXB, năm, ISBN, số trang, giá, dạng tài liệu, ngôn ngữ, phân loại) nhưng lưu **đúng cấu trúc MARC21** vào `bib_records`.
- Sau khi lưu, tự động đẩy vào `catalog_queue` để biên mục chi tiết sau.
- Nhập nhanh liên tục (lưu xong giữ nguyên form, focus lại trường đầu).

*Xếp giá:*
- Gán ĐKCB vào kho → giá, sinh ký hiệu xếp giá tự động theo quy tắc cấu hình (ví dụ: `DDC + 3 chữ cái đầu tên tác giả + số bản`).
- Xếp giá hàng loạt: chọn nhiều ĐKCB, gán cùng kho/giá.
- Bản đồ kho trực quan: xem giá nào đầy/còn trống.

*In mã vạch:*
- Chọn ĐKCB (theo đơn đặt, theo kho, theo khoảng ĐKCB, hoặc tick chọn).
- Chọn mẫu tem, xem trước, xuất PDF đúng khổ giấy tem (A4 nhiều tem/trang).
- Hỗ trợ CODE39, CODE128, QR Code.

*In nhãn:* tương tự mã vạch, nhãn gáy sách có ký hiệu xếp giá, logo thư viện.

*Báo cáo bổ sung:* danh sách tài liệu bổ sung theo khoảng thời gian, nguồn kinh phí, hình thức bổ sung, nhà cung cấp — kèm số lượng và giá trị.

*Báo cáo ĐKCB hủy bỏ:* danh sách ĐKCB đã thanh lý/mất/hỏng, lý do, số quyết định, giá trị.

*Báo cáo tổng quát:* tổng số biểu ghi, tổng số ĐKCB, phân bổ theo kho, theo dạng tài liệu, theo tình trạng — kèm biểu đồ.

*Báo cáo tổng hợp:* bảng tổng hợp đa chiều, người dùng tự chọn hàng/cột/chỉ tiêu (pivot).

**III.3. Quản lý kho**
- *Thông tin thư viện*: CRUD các thư viện/cơ sở (Trụ sở, Cơ sở Nhà Bè), địa chỉ, giờ mở cửa, người phụ trách.
- *Thông tin kho*: CRUD kho thuộc thư viện, loại kho, sức chứa, danh sách giá và ngăn, quy tắc đặt ký hiệu.

**III.4. Quản lý kiểm kê**
Quy trình đúng thứ tự nghiệp vụ:
1. **Đóng kho** (bắt đầu): khóa kho, ngưng cho mượn/trả tại kho đó, cảnh báo trên màn hình lưu thông.
2. **Tạo kỳ kiểm kê**: mã kỳ, tên, kho, phạm vi (toàn kho / theo khoảng ĐKCB / theo dạng tài liệu), ngày bắt đầu–kết thúc, phân công cán bộ. Hệ thống snapshot danh sách ĐKCB kỳ vọng.
3. **Kiểm kê**: màn hình quét barcode liên tục (web + mobile), mỗi lần quét ghi nhận và phản hồi ngay (khớp/thừa/sai kho). Hỗ trợ import file quét từ máy đọc rời. Xem tiến độ realtime (đã quét X/Y).
4. **Đóng kho** (kết thúc): chốt kỳ, đối chiếu, sinh kết quả.
5. **Báo cáo kết quả kiểm kê**: danh sách khớp / thiếu / thừa / sai kho; xuất Excel; từ danh sách thiếu tạo thẳng đề nghị thanh lý hoặc quyết định mất.

**III.5. Quản lý ấn phẩm bổ sung (theo trạng thái xếp giá)**
- *Xếp giá chưa kiểm nhận*: ĐKCB mới nhập, chưa cho lưu thông.
- *Xếp giá trong kho*: đã kiểm nhận, sẵn sàng lưu thông.
- *Xếp giá thanh lý*: đã có quyết định thanh lý.
- *Chuyển kho*: form chuyển ĐKCB giữa các kho (đơn lẻ hoặc hàng loạt bằng quét barcode), ghi lý do + số quyết định, in phiếu chuyển kho, lưu lịch sử `item_movements`.
- *Kiểm nhận và mở khóa*: cán bộ kiểm tra tình trạng vật lý → xác nhận kiểm nhận → ĐKCB chuyển sang "Trong kho" và mở khóa cho phép lưu thông. Có thể khóa lại (đang sửa chữa, đang số hóa) kèm lý do.

**III.6. Tạo các biểu mẫu**
- Trình thiết kế biểu mẫu dùng chung: chọn nguồn dữ liệu, kéo thả trường, đặt tiêu đề/chân trang, chèn logo, đặt khổ giấy và hướng giấy.
- Áp dụng cho: phiếu nhập kho, biên bản bàn giao, phiếu chuyển kho, biên bản kiểm kê, quyết định thanh lý, phiếu mượn/trả.

**III.7. Báo cáo thống kê bổ sung**
Bốn báo cáo, mỗi báo cáo đều có: bộ lọc thời gian + kho + thư viện, hiển thị bảng, biểu đồ (cột/tròn), xuất PDF và Excel.
- Theo dạng tài liệu
- Theo vật mang tin
- Theo thời gian bổ sung (theo ngày/tháng/quý/năm)
- Theo ngôn ngữ

---

### PHÂN HỆ IV — ẤN PHẨM ĐỊNH KỲ

**IV.1. Tìm kiếm báo/tạp chí**
- Tìm theo tên, ISSN, NXB, kỳ hạn, ngôn ngữ, kho, trạng thái đặt.
- Kết quả: xem nhanh tình trạng nhận số (lưới các số theo năm, tô màu: đã nhận / thiếu / dự kiến).

**IV.2. Quản lý mục lục báo tạp chí (bài trích)**
- Với mỗi số, nhập danh sách bài viết: nhan đề bài, tác giả, trang từ–đến, tóm tắt, từ khóa.
- Mỗi bài trích có thể sinh biểu ghi MARC riêng (trường 773 liên kết tới ấn phẩm mẹ) để tra cứu được từ OPAC.
- Import mục lục từ Excel.

**IV.3. Bổ sung tổng thể (xử lý hàng loạt nhiều đầu báo cùng lúc)**
- *Sinh số*: chọn nhiều đầu báo, chọn khoảng thời gian → hệ thống sinh dự kiến toàn bộ các số theo kỳ hạn của từng đầu báo.
- *Ghi nhận*: màn hình dạng bảng, hiển thị các số dự kiến đến hạn, tick nhận hàng loạt, nhập số lượng thực nhận và ngày nhận.
- *Kiểm tra*: đối chiếu số dự kiến vs số đã nhận, liệt kê số thiếu, tạo phiếu khiếu nại gửi nhà cung cấp.

**IV.4. Bổ sung một ấn phẩm (xử lý chi tiết một đầu báo)**
- *Phân kho*: chọn kho lưu, giá, ký hiệu xếp giá cho đầu báo.
- *Định kỳ*: khai báo kỳ hạn chi tiết — dạng chu kỳ (ngày/tuần/tháng/quý/năm), số kỳ/năm, ngày phát hành trong chu kỳ, quy tắc đánh số (số liên tục / số theo năm / có tập & số), năm bắt đầu, số bắt đầu, các kỳ nghỉ không xuất bản.
- *Sinh số*: dựa trên cấu hình định kỳ, sinh danh sách số dự kiến cho khoảng thời gian đặt mua, cho phép sửa tay từng số trước khi chốt.
- *Ghi nhận*: nhận từng số — ngày nhận, số lượng, tình trạng, sinh barcode cho từng bản, ghi vào kho.
- *Kiểm tra*: xem lưới tình trạng, đánh dấu số thiếu, tạo khiếu nại.
- *Đóng tập*: chọn khoảng số (ví dụ số 1–12 năm 2025) → tạo tập đóng bìa → sinh một ĐKCB mới cho tập, các số lẻ chuyển trạng thái "đã đóng tập", in nhãn gáy tập.
- *Tổng hợp*: bảng tổng hợp tình hình nhận số của đầu báo theo năm.

**IV.5. Báo cáo thống kê ấn phẩm định kỳ**
- Tổng hợp (số đầu báo, số kỳ đã nhận, giá trị)
- Theo môn loại (phân loại DDC)
- Theo mức định kỳ (nhật báo/tuần/tháng...)
- Theo ngôn ngữ

---

### PHÂN HỆ V — TÀI LIỆU SỐ

**V.1. Quản lý kho tài liệu số**
- Cây bộ sưu tập phân cấp (Giáo trình / Luận văn / Luận án / Đề tài NCKH / Bài giảng / Tài liệu tham khảo...).
- Upload file: PDF, DOCX, EPUB, MP4, MP3, ảnh. Hỗ trợ upload nhiều file, upload theo chunk cho file lớn (>100MB), hiển thị tiến trình, tiếp tục khi gián đoạn.
- Gắn tài liệu số vào biểu ghi thư mục (một biểu ghi có nhiều file).
- Tự động: trích số trang, sinh thumbnail trang bìa, tạo bản preview (N trang đầu), tính checksum SHA-256, OCR văn bản (Tesseract, tiếng Việt) để tìm kiếm toàn văn.
- Đặt mức truy cập: Công khai / Nội bộ (đăng nhập) / Hạn chế (phải xin duyệt) / Cấm.
- Cấu hình: cho phép tải về không, cho phép in không, số trang xem thử, bật watermark.
- **Trình đọc trực tuyến**: xem PDF ngay trên trình duyệt, chặn tải/in bằng cách stream từng trang dạng ảnh khi tài liệu không cho tải, đóng watermark động (tên bạn đọc + thời gian + IP) lên từng trang.
- Tìm kiếm toàn văn trong nội dung tài liệu số.

**V.2. Xử lý yêu cầu đọc tài liệu hạn chế**
- Bạn đọc gửi yêu cầu từ OPAC/Mobile kèm lý do sử dụng.
- Cán bộ nhận danh sách yêu cầu chờ duyệt → xem thông tin bạn đọc và tài liệu → duyệt (đặt thời hạn truy cập, số lần xem tối đa, có cho tải không) hoặc từ chối kèm lý do.
- Tự động gửi email/thông báo cho bạn đọc.
- Quyền truy cập tự hết hạn theo thời hạn đã đặt.
- Nhật ký truy cập chi tiết: ai xem, tài liệu nào, trang nào, thời điểm, IP, thời lượng.

**V.3. Xuất nhập dữ liệu tài liệu số**
- Import hàng loạt: upload thư mục nén (ZIP), file Excel metadata đi kèm, khớp file với biểu ghi theo tên file hoặc mã.
- Export: xuất metadata (Excel/MARCXML/Dublin Core) kèm file, đóng gói ZIP.
- Yêu cầu E-HSMT mục 4: khi kết thúc hợp đồng phải xuất được toàn bộ dữ liệu → làm chức năng "Xuất toàn bộ dữ liệu hệ thống" (biểu ghi MARC + file số + metadata).

**V.4. Báo cáo thống kê tài liệu số**
- Số lượng tài liệu theo bộ sưu tập, theo định dạng, theo mức truy cập.
- Lượt xem / lượt tải theo thời gian, theo tài liệu (top N), theo bạn đọc.
- Dung lượng lưu trữ đã dùng.
- Thống kê yêu cầu truy cập hạn chế (tổng, đã duyệt, từ chối, thời gian xử lý trung bình).

---

### PHÂN HỆ VI — BẠN ĐỌC

**VI.1. Quản lý hồ sơ bạn đọc**
- Danh sách: tìm theo số thẻ, mã SV, họ tên, CCCD, email, điện thoại; lọc theo loại bạn đọc, khoa, ngành, lớp, khóa, trạng thái thẻ.
- Thêm/sửa: đầy đủ trường ở mục 4.7, upload ảnh (có cắt ảnh), chụp ảnh từ webcam.
- Sinh số thẻ tự động theo quy tắc cấu hình.
- Tab lịch sử: sách đang mượn, lịch sử mượn trả, tiền phạt, vi phạm, lượt vào thư viện, tài liệu số đã truy cập.
- Thao tác: gia hạn thẻ (đơn lẻ và hàng loạt theo bộ lọc), tạm khóa/mở khóa kèm lý do, cấp lại thẻ (giữ lịch sử thẻ cũ), chuyển trạng thái ra trường hàng loạt theo khóa.
- Kiểm tra công nợ trước khi cho ra trường (chặn nếu còn sách/nợ phí).

**VI.2. Quản lý in thẻ bạn đọc**
- Thiết kế mẫu thẻ: kéo thả, mặt trước/mặt sau, khổ CR80 (85.6×54mm) hoặc tùy chỉnh, đặt ảnh nền, logo, ảnh bạn đọc, các trường thông tin, mã vạch/QR số thẻ.
- In hàng loạt: chọn bạn đọc theo bộ lọc, xem trước, xuất PDF đúng khổ (hỗ trợ in trên máy in thẻ nhựa và in nhiều thẻ/trang A4).
- Đếm số lần in mỗi thẻ.

**VI.3. Quản lý danh mục bạn đọc**
- CRUD: Loại bạn đọc (kèm chính sách lưu thông mặc định, thời hạn thẻ, phí thẻ), Khoa, Ngành, Lớp, Khóa học, Loại vi phạm.

**VI.4. Quản lý nhập xuất dữ liệu bạn đọc**
- Import Excel: file mẫu, mapping cột, validate (trùng mã SV, sai định dạng email/ngày), bảng lỗi sửa được tại chỗ, import chạy nền, xuất log.
- Import ảnh hàng loạt: upload ZIP ảnh đặt tên theo mã SV, tự khớp.
- Export danh sách bạn đọc ra Excel theo bộ lọc.
- Đồng bộ từ hệ thống quản lý đào tạo qua API (thiết kế sẵn endpoint và cấu hình mapping).

**VI.5. Báo cáo thống kê bạn đọc**
- Số lượng bạn đọc theo loại / khoa / ngành / khóa / trạng thái — bảng + biểu đồ.
- Bạn đọc mới đăng ký theo thời gian.
- Thẻ sắp hết hạn / đã hết hạn.
- Bạn đọc chưa từng mượn / bạn đọc tích cực.

---

### PHÂN HỆ VII — LƯU THÔNG

**VII.1. Quản lý chính sách lưu thông**
- Ma trận chính sách: Loại bạn đọc × Dạng tài liệu × Kho.
- Mỗi chính sách quy định: số lượng mượn tối đa, số ngày mượn, số lần gia hạn tối đa, số ngày mỗi lần gia hạn, tiền phạt/ngày quá hạn, số ngày ân hạn, số đặt giữ tối đa, số ngày giữ chỗ, có cho mượn không, có cho gia hạn không, có cho đặt giữ không.
- Độ ưu tiên khi nhiều chính sách cùng khớp.
- Lịch nghỉ lễ: cấu hình ngày nghỉ, hạn trả rơi vào ngày nghỉ tự động đẩy sang ngày làm việc kế tiếp; không tính phạt ngày nghỉ.

**VII.2. Màn hình ghi mượn / ghi trả** (màn hình cán bộ dùng nhiều nhất — phải tối ưu tốc độ)
- *Ghi mượn*: quét/nhập số thẻ → hiện thông tin bạn đọc, ảnh, số sách đang mượn, cảnh báo (thẻ hết hạn, đang bị khóa, nợ phí, quá hạn) → quét barcode ĐKCB liên tục → mỗi lần quét kiểm tra chính sách và thêm vào danh sách → hoàn tất → in phiếu mượn.
- Toàn bộ thao tác bằng bàn phím + máy quét, không cần chuột. Phản hồi bằng âm thanh (thành công/lỗi).
- *Ghi trả*: quét barcode ĐKCB → hiện thông tin mượn, tính tiền phạt nếu quá hạn → xác nhận trả → nếu có người đặt giữ thì hiện cảnh báo giữ sách và gửi thông báo cho người đặt.
- *Gia hạn*: quét thẻ hoặc barcode, kiểm tra điều kiện gia hạn (chưa vượt số lần, không có người đặt giữ, không quá hạn), gia hạn.
- *Đặt giữ chỗ*: đặt theo biểu ghi (bất kỳ bản nào rảnh) hoặc theo ĐKCB cụ thể, xếp hàng đợi, thông báo khi có sách.
- *Thu tiền phạt*: màn hình thanh toán, in biên lai, cho phép miễn giảm kèm lý do và quyền hạn.
- *Ghi nhận ra/vào thư viện*: quét thẻ tại cổng.

**VII.3. Quản lý tủ gửi đồ**
- Sơ đồ tủ trực quan theo khu vực, màu theo trạng thái.
- Giao tủ: quét thẻ bạn đọc → chọn tủ trống → giao chìa/mã → ghi nhận.
- Trả tủ: quét thẻ hoặc nhập số tủ → kết thúc.
- Cảnh báo tủ quá giờ chưa trả, báo hỏng tủ.

**VII.4. Quản lý biểu mẫu ghi mượn, ghi trả**
- Thiết kế mẫu phiếu mượn, phiếu trả, biên lai phạt, giấy xác nhận trả sách (cho SV ra trường).
- Chọn mẫu mặc định, in trực tiếp hoặc xuất PDF.

**VII.5. Báo cáo lưu thông** (7 báo cáo bắt buộc, mỗi báo cáo có lọc thời gian, bảng, biểu đồ, xuất PDF/Excel)
1. Báo cáo bạn đọc ra vào thư viện (theo ngày/giờ/loại bạn đọc, biểu đồ giờ cao điểm)
2. Báo cáo bạn đọc đang mượn sách trong thư viện (danh sách hiện tại)
3. Báo cáo lịch sử bạn đọc mượn sách (tra theo bạn đọc hoặc theo khoảng thời gian)
4. Báo cáo bạn đọc mượn quá hạn (kèm số ngày quá hạn, tiền phạt dự kiến, nút gửi email nhắc hàng loạt)
5. Báo cáo sử dụng tủ đựng đồ (tần suất, thời lượng trung bình)
6. Thống kê bạn đọc mượn tài liệu nhiều nhất (top N, theo kỳ)
7. Thống kê ấn phẩm được mượn nhiều nhất (top N, theo dạng tài liệu/kho/môn loại)

---

### PHÂN HỆ VIII — QUẢN TRỊ NỘI DUNG

**VIII.1. Cập nhật thông tin trang thư viện**
- Cấu hình chung: tên thư viện, logo, favicon, ảnh banner, slogan, địa chỉ, điện thoại, email, giờ mở cửa từng cơ sở, liên kết mạng xã hội.
- Quản lý trang tĩnh (Giới thiệu, Nội quy, Hướng dẫn sử dụng, Liên hệ, Hỏi đáp): trình soạn thảo WYSIWYG, chèn ảnh/file/bảng/video.
- Quản lý menu điều hướng: cây menu kéo thả, đặt link nội bộ/ngoài, icon, hiển thị/ẩn.
- Quản lý banner/slider trang chủ: ảnh, link, thứ tự, thời gian hiển thị.
- Quản lý liên kết website (thư viện bạn, CSDL trực tuyến).

**VIII.2. Quản lý tin tức – sự kiện**
- CRUD tin: tiêu đề, slug, tóm tắt, nội dung WYSIWYG, ảnh đại diện, chuyên mục, thẻ, tin nổi bật, lên lịch xuất bản.
- Quản lý chuyên mục tin.
- Quản lý thư viện ảnh (album sự kiện).
- Thống kê lượt xem tin.

---

### PHÂN HỆ IX — TRA CỨU (OPAC)

**IX.1. Trang thông tin điện tử** (SPA riêng, công khai, responsive)
- Trang chủ: ô tìm kiếm lớn, banner, sách mới bổ sung, sách được mượn nhiều, tin tức, thông báo, liên kết nhanh.
- Trang tin tức, trang tĩnh, trang liên hệ.
- SEO: server-side meta tags, sitemap.xml, robots.txt.

**IX.2. Tra cứu tài liệu**
- *Tìm kiếm cơ bản*: một ô, chọn phạm vi (Tất cả / Nhan đề / Tác giả / Chủ đề / ISBN / Từ khóa). Gợi ý tự động khi gõ. **Tìm được cả khi gõ không dấu.**
- *Tìm kiếm nâng cao*: nhiều điều kiện kết hợp AND/OR/NOT, chọn trường cho từng điều kiện, lọc theo năm xuất bản (khoảng), ngôn ngữ, dạng tài liệu, kho, có tài liệu số hay không.
- *Duyệt theo*: Chủ đề / Đề mục / Tác giả / Phân loại DDC / Bộ sưu tập / Ngành / Môn học — dạng cây và A-Z.
- *Kết quả*: phân trang, sắp xếp (liên quan nhất / mới nhất / nhan đề / tác giả / được mượn nhiều), bộ lọc facet bên trái (tự động đếm số lượng theo từng giá trị: tác giả, năm, ngôn ngữ, dạng tài liệu, chủ đề, kho).
- *Chi tiết tài liệu*: ảnh bìa, thông tin thư mục dạng ISBD, tóm tắt, chủ đề (click để tìm tiếp), **danh sách ĐKCB kèm trạng thái sẵn sàng và vị trí kho/giá**, nút đặt giữ, nút xem tài liệu số, xem MARC, xuất trích dẫn (APA/MLA/Chicago/BibTeX/RIS/EndNote), chia sẻ, tài liệu liên quan.
- Lưu tìm kiếm, đánh dấu yêu thích, giỏ tài liệu, gửi email danh sách.

**IX.3. Đăng ký mượn sách giới hạn từ trang OPAC**
- Bạn đọc đăng nhập bằng số thẻ + mật khẩu.
- Trang cá nhân: sách đang mượn (kèm hạn trả, nút gia hạn), lịch sử mượn, đặt giữ đang chờ, tiền phạt, tài liệu số được cấp quyền, thông báo.
- Đăng ký mượn (đặt trước): chọn tài liệu → đặt giữ → hệ thống kiểm tra hạn mức theo chính sách, giới hạn số lượng đăng ký đồng thời → cán bộ nhận danh sách để chuẩn bị sách.
- Gửi yêu cầu gia hạn (nếu cấu hình yêu cầu duyệt).
- Đổi mật khẩu, cập nhật thông tin liên hệ.

**IX.4. Tra cứu tài liệu điện tử**
- Bộ lọc riêng cho tài liệu số, xem trước, đọc trực tuyến, tải về (theo quyền), gửi yêu cầu truy cập tài liệu hạn chế.

**IX.5. Kết nối liên thư viện trên OPAC**
- Tab "Tìm ở thư viện khác": tra cứu song song qua Z39.50/SRU tới các thư viện đã cấu hình, hiển thị kết quả gộp có ghi rõ nguồn.

---

### PHÂN HỆ X — TÀI LIỆU MÔN HỌC

**X.1. Quản lý ngành**
- CRUD ngành đào tạo: mã ngành, tên, khoa quản lý, bậc đào tạo (ĐH/ThS/TS), mô tả.
- Import từ Excel.

**X.2. Quản lý môn học**
- CRUD môn học: mã môn, tên môn, số tín chỉ, ngành, học kỳ, giảng viên phụ trách, mô tả.
- Gán môn học vào nhiều ngành (quan hệ nhiều-nhiều).

**X.3. Quản lý liên kết tài liệu theo môn học**
- Màn hình 2 cột: chọn môn học bên trái → tìm và gán tài liệu bên phải.
- Phân loại liên kết: Giáo trình chính / Tài liệu tham khảo bắt buộc / Tài liệu tham khảo thêm.
- Gán hàng loạt, import danh mục tài liệu môn học từ Excel.
- Trên OPAC và Mobile: bạn đọc duyệt theo Ngành → Môn học → thấy danh sách tài liệu, biết ngay còn bản rảnh không.
- Báo cáo: môn học chưa có tài liệu, tài liệu được gán nhiều môn nhất, mức độ đáp ứng tài liệu theo ngành.

---

### PHÂN HỆ XI — MOBILE APPLICATION *(ĐỢT SAU — KHÔNG BUILD TRONG ĐỢT NÀY)*

> Phần đặc tả dưới đây giữ nguyên để làm cơ sở cho đợt phát triển sau và cho Bảng đáp ứng kỹ thuật.
> **Việc duy nhất cần làm trong đợt này:** bảo đảm mọi chức năng liệt kê bên dưới đều đã có endpoint tương ứng trong nhóm `/api/reader/*`, hoạt động thật và có test. Xem danh sách endpoint bắt buộc ở cuối mục này.

**XI.1. Chức năng cơ bản (không cần đăng nhập)**
- Tra cứu tài liệu: cơ bản, nâng cao, theo ISBN, **quét mã vạch**, **quét QR** (dùng `mobile_scanner`).
- Duyệt danh mục sách theo Chủ đề, Đề mục, Tác giả.
- Duyệt danh mục sách theo Chuyên ngành đào tạo, Môn học.
- Danh mục Luận văn / Luận án.
- Danh mục Ấn phẩm định kỳ.
- Xem chi tiết tài liệu, tình trạng sẵn có, vị trí kho.
- Tin tức – sự kiện, thông tin thư viện, giờ mở cửa, bản đồ chỉ đường.

**XI.2. Dành cho độc giả (sau đăng nhập)**
- Tra cứu và mượn/trả tài liệu số: đọc trực tuyến trong app, tải về vùng offline có mã hóa và tự hết hạn.
- **Mượn sách giấy tự phục vụ**: bạn đọc tự vào kho chọn sách → quét barcode sách bằng app → hệ thống kiểm tra chính sách → ghi mượn. Kèm xác thực vị trí (đang ở trong thư viện) qua Wi-Fi SSID hoặc quét QR đặt tại kho để chống lạm dụng.
- Đặt giữ chỗ tài liệu; xem vị trí trong hàng đợi; nhận thông báo đẩy khi sách sẵn sàng.
- Gửi yêu cầu mượn/tải tài liệu số hạn chế.
- Xem lịch sử mượn/tải tài liệu số.
- Xem lịch sử mượn trả tài liệu giấy; gửi yêu cầu gia hạn sách.
- Đổi mật mã; gia hạn thẻ thư viện (gửi yêu cầu, xem trạng thái).
- Thẻ thư viện điện tử: hiển thị mã vạch/QR số thẻ để quét tại quầy và cổng ra vào.
- Thông báo đẩy (Firebase Cloud Messaging): sắp đến hạn trả, quá hạn, sách đặt giữ đã sẵn sàng, yêu cầu được duyệt, tin mới.
- Chế độ offline: cache kết quả tra cứu gần đây và thẻ điện tử.

**XI.3. Yêu cầu kỹ thuật app**
- Dùng chung REST API với web, xác thực JWT, refresh token tự động.
- Hỗ trợ sáng/tối, cỡ chữ điều chỉnh được.
- Đồng bộ dữ liệu trung tâm (yêu cầu kiểm thử mục 2.7 của E-HSMT).
- Build được cả APK và IPA, có hướng dẫn cấu hình endpoint.

**XI.4. Nhóm endpoint `/api/reader/*` — BẮT BUỘC HOÀN THÀNH TRONG ĐỢT WEB NÀY**

Đây là hợp đồng API giữa backend và app mobile đợt sau. OPAC dùng chung chính nhóm này, nên làm xong là kiểm chứng được ngay, và đợt sau người viết Flutter chỉ việc gọi.

| Endpoint | Method | Chức năng | App dùng ở màn hình |
|---|---|---|---|
| `/api/reader/auth/login` | POST | Đăng nhập bằng số thẻ + mật khẩu | Đăng nhập |
| `/api/reader/auth/refresh` | POST | Làm mới token | Nền |
| `/api/reader/auth/change-password` | POST | Đổi mật mã | Tài khoản |
| `/api/reader/profile` | GET/PUT | Hồ sơ, ảnh, thông tin liên hệ | Tài khoản |
| `/api/reader/card` | GET | Thẻ điện tử: số thẻ, hạn thẻ, chuỗi mã vạch/QR | Thẻ thư viện |
| `/api/reader/card/renew-request` | POST | Gửi yêu cầu gia hạn thẻ | Tài khoản |
| `/api/search` | GET | Tra cứu cơ bản (từ khóa, phạm vi, phân trang, sắp xếp) | Tra cứu |
| `/api/search/advanced` | POST | Tra cứu nâng cao nhiều điều kiện | Tra cứu nâng cao |
| `/api/search/suggest` | GET | Gợi ý tự động khi gõ | Tra cứu |
| `/api/search/facets` | GET | Bộ đếm facet cho bộ lọc | Tra cứu |
| `/api/search/by-isbn/{isbn}` | GET | Tra theo ISBN | Quét mã |
| `/api/search/by-barcode/{barcode}` | GET | Tra ĐKCB theo barcode | Quét mã vạch/QR |
| `/api/bib/{id}` | GET | Chi tiết tài liệu + danh sách ĐKCB, trạng thái, vị trí kho | Chi tiết sách |
| `/api/browse/subjects` `/authors` `/classifications` | GET | Duyệt theo chủ đề, đề mục, tác giả | Danh mục |
| `/api/browse/majors` `/courses` | GET | Duyệt theo ngành, môn học | Danh mục |
| `/api/browse/majors/{id}/courses/{cid}/documents` | GET | Tài liệu theo môn học | Danh mục |
| `/api/browse/theses` | GET | Danh mục luận văn/luận án | Danh mục |
| `/api/browse/serials` | GET | Danh mục ấn phẩm định kỳ | Danh mục |
| `/api/reader/loans/current` | GET | Sách đang mượn + hạn trả | Sách của tôi |
| `/api/reader/loans/history` | GET | Lịch sử mượn trả giấy | Lịch sử |
| `/api/reader/loans/{id}/renew` | POST | Gửi yêu cầu gia hạn sách | Sách của tôi |
| `/api/reader/loans/self-checkout` | POST | Mượn tự phục vụ bằng barcode + xác thực vị trí | Tự mượn |
| `/api/reader/holds` | GET/POST | Xem và tạo đặt giữ chỗ | Đặt giữ |
| `/api/reader/holds/{id}` | DELETE | Hủy đặt giữ | Đặt giữ |
| `/api/reader/fines` | GET | Tiền phạt, tình trạng thanh toán | Tài khoản |
| `/api/reader/digital` | GET | Danh sách tài liệu số được phép truy cập | Tài liệu số |
| `/api/reader/digital/{id}/read` | GET | Stream nội dung đọc trực tuyến (có watermark) | Trình đọc |
| `/api/reader/digital/{id}/download` | GET | Tải về (kiểm tra quyền) | Tài liệu số |
| `/api/reader/digital/{id}/request` | POST | Gửi yêu cầu truy cập tài liệu hạn chế | Tài liệu số |
| `/api/reader/digital/requests` | GET | Trạng thái các yêu cầu đã gửi | Tài liệu số |
| `/api/reader/digital/history` | GET | Lịch sử xem/tải tài liệu số | Lịch sử |
| `/api/reader/notifications` | GET | Danh sách thông báo | Thông báo |
| `/api/reader/notifications/{id}/read` | POST | Đánh dấu đã đọc | Thông báo |
| `/api/reader/devices` | POST/DELETE | Đăng ký/hủy FCM token *(chuẩn bị sẵn, đợt sau dùng)* | Nền |
| `/api/public/news` `/pages` `/settings` | GET | Tin tức, trang tĩnh, thông tin thư viện | Trang chủ |

Yêu cầu chất lượng cho nhóm này: có integration test cho từng endpoint, mô tả đầy đủ trong Swagger kèm ví dụ request/response, và ghi vào `docs/05-api-reference.md` thành một chương riêng "API cho ứng dụng khách" để bàn giao cho người viết app.

---

## 6. YÊU CẦU PHI CHỨC NĂNG

### 6.1. Phân quyền (RBAC + Data Scope)
- Mã quyền dạng `MODULE.ENTITY.ACTION`, ví dụ: `CATALOG.BIB.CREATE`, `CIRCULATION.LOAN.RETURN`, `ACQ.ORDER.APPROVE`.
- Backend: attribute `[RequirePermission("CATALOG.BIB.CREATE")]` trên từng endpoint.
- Data scope: người dùng chỉ thao tác được trên kho/thư viện được gán — enforce bằng EF Core global query filter.
- Frontend: ẩn menu và disable nút theo quyền, nhưng **backend vẫn phải kiểm tra độc lập**.
- Kiểm thử mục 2.3 của E-HSMT sẽ tạo tài khoản quyền khác nhau để xác nhận — phải trả HTTP 403 rõ ràng khi không đủ quyền.

### 6.2. Nhật ký
- Ghi tự động mọi thao tác Create/Update/Delete qua EF Core `SaveChangesInterceptor`.
- Ghi đăng nhập/đăng xuất/đăng nhập thất bại, thay đổi quyền, thay đổi tham số, sao lưu/phục hồi, xuất dữ liệu.
- Lưu diff dạng jsonb.

### 6.3. Hiệu năng
- Tra cứu OPAC trả kết quả < 1 giây với 500.000 biểu ghi.
- Hỗ trợ 200 người dùng đồng thời.
- Phân trang server-side toàn bộ, không load hết dữ liệu về client.
- Cache Redis cho: danh mục, kết quả tra cứu phổ biến, cấu hình hệ thống.
- Response nén gzip/brotli.

### 6.4. Bảo mật
- HTTPS, HSTS, security headers (CSP, X-Frame-Options, X-Content-Type-Options).
- Mật khẩu băm bằng BCrypt (work factor ≥ 12).
- Chống SQL Injection (dùng ORM tham số hóa), XSS (sanitize HTML từ WYSIWYG bằng HtmlSanitizer), CSRF (SameSite cookie + token).
- Rate limiting cho API công khai và endpoint đăng nhập.
- Upload file: kiểm tra magic number, giới hạn kích thước và phần mở rộng, quét virus (ClamAV tùy chọn), lưu ngoài web root.
- Không log thông tin nhạy cảm.

### 6.5. Vận hành 24/7
- Health check: `/health` (liveness), `/health/ready` (readiness — kiểm tra DB, Redis, MinIO).
- Graceful shutdown, connection pooling.
- Structured logging (Serilog JSON), log rotation.
- Background jobs (Hangfire): tính quá hạn hằng ngày, gửi email nhắc hạn, sao lưu tự động, harvest OAI-PMH, dọn phiên hết hạn, hết hạn quyền truy cập tài liệu số.
- Hangfire Dashboard bảo vệ bằng quyền admin.

### 6.6. Giao diện
- Font: Inter hoặc Be Vietnam Pro (hỗ trợ đầy đủ dấu tiếng Việt).
- Toàn bộ nút lệnh thống nhất: vị trí, màu, icon, nhãn (Thêm mới / Sửa / Xóa / Lưu / Hủy / Tìm kiếm / Xuất Excel / In).
- Layout thống nhất mọi màn hình danh sách: thanh bộ lọc trên → bảng giữa → phân trang dưới → thanh hành động hàng loạt khi có chọn.
- Form: label bên trái hoặc trên, đánh dấu `*` trường bắt buộc, validate hiển thị dưới field, thông báo lỗi tiếng Việt rõ nghĩa.
- Toast thông báo thành công/lỗi, confirm dialog cho thao tác xóa.
- Responsive: admin tối thiểu 1366×768, OPAC hỗ trợ mobile.
- Accessibility cơ bản: có thể thao tác bằng bàn phím, contrast đạt WCAG AA.

---

## 7. DOCKER

`docker-compose.yml` gồm các service:

```yaml
services:
  postgres:      # postgres:16-alpine, volume dữ liệu, init script tạo extension + schema
  redis:         # redis:7-alpine
  minio:         # minio/minio, console port 9001
  api:           # build từ backend/, depends_on postgres+redis+minio, health check
  admin:         # build từ frontend-admin/, nginx serve static
  opac:          # build từ frontend-opac/, nginx serve static
  nginx:         # reverse proxy, route / -> opac, /admin -> admin, /api -> api
  z3950:         # service Z39.50 server (TCP 210) — có thể chung với api
```

Yêu cầu:
- Dockerfile multi-stage cho backend (SDK build → runtime aspnet).
- Dockerfile multi-stage cho frontend (node build → nginx alpine).
- Toàn bộ cấu hình qua biến môi trường, có `.env.example` đầy đủ chú thích tiếng Việt.
- `docker-compose.prod.yml` riêng: bật HTTPS, giới hạn tài nguyên, restart policy, log driver.
- Script `deploy/scripts/backup.sh` và `restore.sh` chạy được từ host.
- Chạy `docker compose up -d` là hệ thống lên hoàn chỉnh, có sẵn dữ liệu seed và tài khoản admin.

---

## 8. DỮ LIỆU SEED

Khi khởi động lần đầu, tự động seed:
- Tài khoản `admin` / mật khẩu tạm (buộc đổi lần đầu).
- 5 nhóm người dùng mẫu: Quản trị hệ thống, Cán bộ biên mục, Cán bộ bổ sung, Cán bộ lưu thông, Thủ thư — với bộ quyền phù hợp.
- Đầy đủ bảng quyền (~150 mã quyền).
- Bộ định nghĩa MARC 21 (~200 trường thông dụng, tên tiếng Việt).
- Khung phân loại DDC 23 rút gọn tới 3 chữ số.
- Danh mục ngôn ngữ (ISO 639-2), nước (mã MARC).
- 2 thư viện (Trụ sở, Cơ sở Nhà Bè), 4 kho mẫu.
- 6 loại bạn đọc kèm chính sách lưu thông tương ứng.
- **200 biểu ghi thư mục mẫu + 500 ĐKCB + 50 bạn đọc + 100 giao dịch mượn trả** — để demo và kiểm thử được ngay, không phải nhập tay.
- 3 server Z39.50 công khai để test.

---

## 9. KIỂM THỬ

Viết test đối chiếu trực tiếp với **Mục 5 phần 2 của E-HSMT** (8 nội dung kiểm tra):

| Mã | Nội dung | Test cần viết |
|---|---|---|
| 2.1 | Kiểm tra cài đặt | Integration test: docker compose up → health check pass, tất cả migration chạy xong, seed data đủ |
| 2.2 | Kiểm tra chức năng | E2E test cho luồng chính của cả 11 phân hệ |
| 2.3 | Phân quyền & nhật ký | Test tài khoản có quyền → 200, không quyền → 403; mọi thao tác quan trọng sinh audit log |
| 2.4 | Trao đổi dữ liệu | Round-trip ISO 2709 với dữ liệu tiếng Việt; SRU query trả MARCXML hợp lệ; OAI-PMH 6 verb; Z39.50 client kết nối server thật |
| 2.5 | Chuyển đổi dữ liệu | Test import Excel/ISO 2709 → đối chiếu số lượng và quan hệ biểu ghi–ĐKCB–bạn đọc–giao dịch |
| 2.6 | Sao lưu/phục hồi | Test tạo backup → xóa dữ liệu → restore → so sánh checksum |
| 2.7 | Mobile Application | *(Đợt sau)* — đợt này thay bằng integration test toàn bộ nhóm `/api/reader/*`: đăng nhập, tra cứu, tra theo barcode, đặt giữ, gia hạn, tài liệu số, lịch sử, thông báo |
| 2.8 | Báo cáo | Test số liệu báo cáo khớp với query kiểm chứng độc lập; xuất PDF/Excel không lỗi |

Công cụ: xUnit + FluentAssertions + Testcontainers (backend), Vitest + Playwright (frontend), `flutter test` + `integration_test` (mobile).

Đồng thời tạo file `docs/06-kich-ban-kiem-thu.md`: bảng kịch bản kiểm thử tiếng Việt, mỗi dòng gồm Mã | Chức năng | Bước thực hiện | Kết quả mong đợi | Kết quả thực tế | Đạt/Không đạt — dùng làm phụ lục nghiệm thu.

---

## 10. TÀI LIỆU BÀN GIAO

Sinh đầy đủ 7 tài liệu trong `docs/`, tiếng Việt, có ảnh chụp màn hình:
1. **Hướng dẫn sử dụng** — theo từng phân hệ, từng vai trò, có quy trình nghiệp vụ minh họa.
2. **Tài liệu quản trị hệ thống** — kiến trúc, cấu hình, giám sát, xử lý sự cố thường gặp.
3. **Sao lưu/phục hồi** — quy trình, lịch, kiểm chứng, tình huống khẩn cấp.
4. **Cài đặt/cấu hình** — yêu cầu hạ tầng, các bước triển khai, biến môi trường, cấu hình Nginx/HTTPS.
5. **API reference** — OpenAPI/Swagger sinh tự động + mô tả tiếng Việt cho từng nhóm endpoint, mô tả giao thức Z39.50/SRU/OAI-PMH.
6. **Kịch bản kiểm thử** (mục 9).
7. **Bảng đáp ứng kỹ thuật** — bảng đối chiếu **đúng thứ tự từng yêu cầu trong Chương V E-HSMT**, mỗi dòng ghi: Yêu cầu | Đáp ứng (Có/Không) | Tên chức năng tương ứng trong sản phẩm | Ghi chú/Chứng minh. Đây là tài liệu bắt buộc nộp thầu.

---

## 11. QUY TẮC CODE

- Code comment và tên biến bằng tiếng Anh; mọi chuỗi hiển thị cho người dùng bằng tiếng Việt, tập trung trong file i18n.
- Backend: mỗi feature một thư mục trong `LibraryConnect.Application/Features/`, gồm Command/Query + Handler + Validator + DTO.
- Frontend: mỗi phân hệ một thư mục trong `src/modules/`, gồm `pages/`, `components/`, `api/`, `types/`, `hooks/`.
- Không dùng `any` trong TypeScript.
- Mọi API trả về format thống nhất:
  ```json
  { "success": true, "data": {}, "message": "", "errors": [] }
  ```
  Phân trang: `{ "items": [], "totalCount": 0, "page": 1, "pageSize": 20 }`
- Exception handling tập trung ở middleware, không try-catch rải rác.
- Migration đặt tên có nghĩa, không sửa migration đã commit.
- Mỗi Phase hoàn thành phải: build sạch không warning, test pass, cập nhật `README.md` và `docs/`.

---

## 12. THỨ TỰ THỰC HIỆN (làm tuần tự, không nhảy bước)

> **Phase 1–14 đã xong.** Giữ lại danh sách dưới đây để đối chiếu phạm vi từng phase
> khi rà soát. Việc còn lại xem `docs/08-so-loi.md`, phần "Làm tiếp gì sau đây".

**✅ Phase 1 — Nền móng**
Khởi tạo solution, cấu trúc Clean Architecture, EF Core + PostgreSQL, docker-compose (postgres/redis/minio/api), JWT auth, RBAC, audit log interceptor, exception middleware, health check, Serilog. Khung React admin (layout, sidebar theo quyền, routing, API client, form/table components dùng chung). Seed quyền + tài khoản admin.
→ *Nghiệm thu Phase: đăng nhập được, menu hiển thị theo quyền, thao tác sinh audit log.*

**✅ Phase 2 — Quản trị hệ thống (Phân hệ I)**
Đầy đủ 5 nhóm chức năng, kể cả sao lưu/phục hồi thật bằng pg_dump.

**✅ Phase 3 — Danh mục**
Toàn bộ bảng danh mục ở mục 4.2, kèm import/export Excel và chức năng gộp trùng.

**✅ Phase 4 — MARC Core** *(quan trọng nhất, làm kỹ)*
`LibraryConnect.Marc`: model MARC21, parser/serializer ISO 2709, MARCXML, unit test round-trip tiếng Việt. Định nghĩa trường MARC + seed. Trình soạn MARC trên React.

**✅ Phase 5 — Biên mục (Phân hệ II)**
Đầy đủ 10 nhóm chức năng, gồm hàng đợi biên mục và xử lý phích.

**✅ Phase 6 — Bổ sung & Kho (Phân hệ III)**
Đơn đặt → nhập kho → ĐKCB → in mã vạch/nhãn → kiểm kê → chuyển kho → báo cáo.

**✅ Phase 7 — Ấn phẩm định kỳ (Phân hệ IV)**
Chú ý thuật toán sinh số theo kỳ hạn và chức năng đóng tập.

**✅ Phase 8 — Bạn đọc (Phân hệ VI)**
Hồ sơ, in thẻ, import/export, báo cáo.

**✅ Phase 9 — Lưu thông (Phân hệ VII)**
Chính sách, ghi mượn/trả tối ưu tốc độ, đặt giữ, phạt, tủ đồ, 7 báo cáo.

**✅ Phase 10 — Tài liệu số (Phân hệ V)**
MinIO, upload chunk, OCR, trình đọc có watermark, duyệt yêu cầu truy cập hạn chế.

**✅ Phase 11 — Liên thư viện**
Z39.50 client + server, SRU, OAI-PMH provider + harvester. Test với server thật.

**✅ Phase 12 — OPAC + CMS (Phân hệ VIII, IX)**
SPA công khai, tra cứu facet, tài khoản bạn đọc, quản trị nội dung.

**✅ Phase 13 — Tài liệu môn học (Phân hệ X)**

**✅ Phase 14 — Hoàn thiện web**
Tối ưu hiệu năng, rà soát bảo mật, seed dữ liệu demo đầy đủ, viết trọn 7 tài liệu bàn giao, docker-compose.prod, script backup/restore, kịch bản kiểm thử.
Rà soát lần cuối nhóm `/api/reader/*` (mục XI.4): đủ endpoint, đủ test, đủ mô tả Swagger, đã viết chương "API cho ứng dụng khách" trong `docs/05-api-reference.md`.
→ *Nghiệm thu Phase: `docker compose up -d` là hệ thống web chạy hoàn chỉnh với dữ liệu demo, mọi phân hệ I–X demo được.*

**✅ Phase 15 — Mobile App (Phân hệ XI)** — *đã làm xong, xem mục A.1*
Flutter, đầy đủ chức năng mục XI, gọi vào nhóm endpoint đã hoàn thiện ở Phase 14, build APK/AAB trên
Android và chạy trên iPhone Simulator. Chưa có: máy iPhone thật, IPA ký, thông báo đẩy FCM thật,
quét bằng camera thật — ghi rõ trong `docs/06`/`docs/07`, không đánh "Đạt".

---

## 13. LƯU Ý CUỐI

1. **Không được stub.** Nếu một chức năng chưa làm được ngay, dừng lại hỏi thay vì viết hàm rỗng trả dữ liệu giả.
2. **ISO 2709 tính độ dài theo byte UTF-8**, không theo ký tự. Sai chỗ này là hỏng toàn bộ khả năng trao đổi biểu ghi.
3. **Tìm kiếm phải hoạt động khi gõ không dấu.** Người Việt tra cứu thường không bỏ dấu.
4. **Màn hình ghi mượn/ghi trả** là nơi cán bộ dùng nhiều nhất trong ngày — ưu tiên tốc độ và thao tác bàn phím hơn là đẹp.
5. **Trình soạn MARC** quyết định chất lượng sản phẩm trong mắt cán bộ thư viện chuyên môn. Đầu tư kỹ.
6. Mỗi khi hoàn thành một Phase, tự đối chiếu lại với `docs/07-bang-dap-ung-ky-thuat.md` và cập nhật trạng thái đáp ứng.
7. **Test xanh không có nghĩa là chức năng đúng.** Người viết mã tự viết test cho mã của mình chỉ
   xác nhận mã làm đúng thứ mình *nghĩ*, không xác nhận mình nghĩ đúng. Cách kiểm duy nhất đáng tin
   là mở hệ thống ra dùng như người dùng thật, có dữ liệu thật, và cố tình đi đường sai.
8. **Ghi lỗi thì ghi thẳng.** Sổ lỗi `docs/08-so-loi.md` chép cả những lỗi do chính mình gây ra ở
   các phase trước, không bào chữa. Có bằng chứng — ảnh màn hình, số đo, câu lệnh tái hiện — mới
   được ghi là đã kiểm.
