# PROMPT HOÀN THIỆN LIBRARYCONNECT — ĐỢT SỬA LỖI, ẢNH BÌA VÀ DỮ LIỆU THẬT

> Đưa toàn bộ nội dung này cho Claude Code trong một phiên. Làm tuần tự theo thứ tự
> ưu tiên, không nhảy bước. Không hỏi lại, tự quyết theo quy tắc ở mục 0.

---

## 0. NGUYÊN TẮC CHUNG CHO CẢ ĐỢT

**Bối cảnh:** 14 phase đã "xong" với 739 test xanh, nhưng chỉ cần mở hệ thống lên vài phút
là tìm được lỗi rõ ràng. Nghĩa là bộ kiểm thử hiện tại đang bỏ sót cả một loại lỗi: em viết
code, rồi tự viết test cho chính code đó, nên test chỉ xác nhận code chạy đúng như em **nghĩ**,
không xác nhận em **nghĩ đúng**.

Đợt này không thêm tính năng mới. Chỉ sửa cho đúng và làm cho dùng được thật.

**Khi gặp chỗ chưa rõ — không hỏi, làm theo thứ tự:**
1. Suy ra từ quy ước đã dùng ở Phase 1–14 (đọc code cũ mà theo).
2. Tra chuẩn nghiệp vụ thư viện (MARC 21 chính thức của Library of Congress, RDA, TT 18/2014/TT-BVHTTDL).
3. Chọn phương án **đơn giản nhất mà chạy được thật**, và làm cho nó **cấu hình được** qua
   `sys.system_parameters`.
4. Ghi vào `docs/00-quyet-dinh-ky-thuat.md` theo mẫu đang có: `| Phase | Vấn đề | Phương án | Lý do | Đổi được không |`

**Tuyệt đối không:**
- Không stub, không mock, không hàm rỗng, không TODO. Bí thì chọn phương án đơn giản nhất rồi làm cho chạy thật.
- Không tự chấm "đạt" khi chưa chạy thật và nhìn tận mắt.
- Không bịa lỗi cho đẹp báo cáo, cũng không giấu lỗi do mình gây ra ở phase trước.

**Sau mỗi lần context bị nén:** đọc lại `PROMPT-BUILD-LIBRARYCONNECT.md` (mục 0.2, 6, 11),
`docs/00-quyet-dinh-ky-thuat.md`, và chính tệp này trước khi code tiếp.

---

## ƯU TIÊN 1 — BẢO MẬT (LÀM ĐẦU TIÊN, TRONG HÔM NAY)

### 1.1. Mật khẩu mặc định đang công khai
`README.md` in thẳng mật khẩu quản trị `LibraryConnect@2025` ở trang đầu, mà repo đang để
**public** trên GitHub. Ai đọc README cũng biết mật khẩu admin của mọi bản cài.

- Xóa mật khẩu khỏi `README.md` và mọi tài liệu trong `docs/`.
- Đổi cơ chế: lần khởi động đầu tiên **sinh mật khẩu ngẫu nhiên**, in ra console log của
  container API kèm dòng cảnh báo rõ ràng, và bắt buộc đổi ở lần đăng nhập đầu.
- Rà toàn bộ repo tìm mọi mật khẩu, khóa bí mật, chuỗi kết nối còn nằm trong mã nguồn hoặc
  tài liệu. Báo cáo danh sách tìm được.

### 1.2. Buộc đổi mật khẩu tạm phải chặn ở cả tầng API
Lỗi số 9 của Phase 14 ghi nhận việc này chỉ chặn ở giao diện. Kiểm chứng lại **bằng curl,
không qua trình duyệt**: lấy token của tài khoản chưa đổi mật khẩu tạm, gọi thẳng một API
nghiệp vụ bất kỳ. Phải trả 403. Nếu qua được thì vá và viết test.

### 1.3. Rà soát cùng loại
Tìm mọi quy tắc nghiệp vụ khác đang **chỉ chặn ở frontend**. Cách tìm: với mỗi ràng buộc
hiển thị trên giao diện (nút bị ẩn, nút bị disable, cảnh báo), gọi thẳng endpoint tương ứng
bằng curl xem có chặn không. Ưu tiên kiểm: mượn cho thẻ hết hạn/bị khóa, gia hạn quá số lần,
gia hạn khi có người đặt giữ, mượn quá hạn mức, xóa bản ghi đang được tham chiếu, truy cập
tài liệu số hạn chế chưa được duyệt.

---

## ƯU TIÊN 2 — SỬA BỘ ÁNH XẠ DUBLIN CORE → MARC 21

Bộ ánh xạ trong harvester OAI-PMH đang đổ dữ liệu thô vào sai trường. Đây không phải một lỗi
lẻ mà là một **lớp lỗi**, ảnh hưởng mọi biểu ghi harvest về.

### 2.1. Sinh trường 008 — BẮT BUỘC
008 hiện **không được sinh**. Đây là trường bắt buộc của MARC 21 bibliographic; thiếu nó
biểu ghi không hợp lệ và xuất ISO 2709 sang phần mềm khác sẽ bị từ chối. Sinh đủ 40 ký tự,
tối thiểu điền đúng:

| Vị trí | Nội dung |
|---|---|
| 00–05 | Ngày tạo biểu ghi (YYMMDD) |
| 06 | Kiểu ngày (`s` = một ngày duy nhất) |
| 07–10 | Năm xuất bản, lấy từ `dc:date` đã chuẩn hóa |
| 15–17 | Mã nước xuất bản (`vm` = Việt Nam) |
| 35–37 | Mã ngôn ngữ, khớp với 041 |
| 38, 39 | Để trống |

Vị trí không suy ra được thì để ký tự điền `|`, **không** để khoảng trắng bừa.
Viết lệnh chạy lại bổ sung 008 cho mọi biểu ghi đang thiếu trong CSDL.

### 2.2. Mã ngôn ngữ 041
Đang ra `en_` — sai. DC trả mã 2 ký tự (ISO 639-1), MARC dùng 3 ký tự (ISO 639-2/B).
Viết bảng quy đổi, tối thiểu phủ: `en→eng`, `vi→vie`, `fr→fre`, `de→ger`, `ru→rus`,
`zh→chi`, `ja→jpn`, `ko→kor`. Không quy đổi được thì để `und`, **tuyệt đối không đệm
ký tự cho đủ độ dài**.

### 2.3. Ngày xuất bản 264$c
Đang ra `2021-09-14T02:55:21Z` — đó là datestamp của OAI, không phải năm xuất bản.
- Trích 4 chữ số năm từ `dc:date`.
- Nếu `dc:date` là datestamp bản ghi chứ không phải ngày xuất bản, để `264$c = "[không rõ]"`,
  **không bịa**.
- Datestamp OAI lưu vào 005 hoặc trường quản trị nội bộ, không vào 264.

### 2.4. Mô tả vật lý 300 và bộ ba RDA
`application/pdf` không thuộc 300$a. Sửa thành:
- `300$a`: số trang/dung lượng thật nếu biết; không biết thì bỏ trống cả trường.
- MIME type → `856$q`.
- Thêm bộ ba RDA cho tài liệu điện tử:
  ```
  336 $a text            $b txt $2 rdacontent
  337 $a computer        $b c   $2 rdamedia
  338 $a online resource $b cr  $2 rdacarrier
  ```

### 2.5. Mã cơ quan — 003, 035, 040
Ba trường này đang dùng **tên hiển thị** thay vì **mã cơ quan**.
- Thêm tham số `LIBRARY.MARC_ORG_CODE` (nhóm Thông tin thư viện), dạng `VN-XXXXX`,
  cảnh báo khi để trống.
- `003` = mã cơ quan của hệ thống mình. Hiện đang ra `Thư viện` — sai.
- `040$a` = mã cơ quan biên mục gốc. Hiện đang ra `ĐH Thủy lợi — Bài giảng điện tử` — sai,
  đó là chuỗi mô tả có gạch ngang.
- `035$a` hiện ra `(OAI)oai:localhost:DHTL/11426` — chữ **`localhost`** là máy chủ của mình,
  không phải nguồn. Mất hoàn toàn khả năng truy vết. Phải ghi đúng identifier gốc từ nguồn,
  dạng `(mã-nguồn)identifier-gốc`. Viết lệnh chạy lại sửa mọi biểu ghi đang dính `localhost`.

### 2.6. Trường 856 và tài liệu số
Biểu ghi harvest có `856$u` trỏ tới file PDF nhưng tab "Tài liệu số" hiện `(0)` — bạn đọc
thấy tài liệu mà không mở được. Chọn một trong hai và ghi quyết định vào sổ:
- Tạo bản ghi tài liệu số dạng **liên kết ngoài** (giữ URL, không tải file về), mức truy cập
  theo cấu hình; hoặc
- Trang chi tiết hiện link ngoài rõ ràng thay vì báo `(0)`.

### 2.7. Kiểm chứng bằng công cụ ngoài — QUAN TRỌNG NHẤT MỤC NÀY
Từ đầu tới giờ MARC chỉ được kiểm bằng **chính parser của mình** — encode rồi decode thấy
khớp là coi như đúng. Nhưng nếu parser sai theo cùng một cách ở cả hai chiều thì test vẫn xanh.

Sau khi sửa: xuất 20 biểu ghi ra ISO 2709 và MARCXML, kiểm tính hợp lệ bằng **công cụ độc lập**
(`pymarc`, hoặc MarcEdit MARC Validator), **không dùng parser của mình để tự chấm**.
Báo cáo: số biểu ghi hợp lệ / tổng, và danh sách lỗi công cụ ngoài bắt được.

Đây chính là điều mục 2.4 trong E-HSMT sẽ kiểm.

Viết test **đỏ-trước-xanh-sau** cho từng mục 2.1–2.5: test phải fail trên code hiện tại.

---

## ƯU TIÊN 3 — ẢNH BÌA

### 3.1. Vì sao hiện không có ảnh
Biểu ghi MARC **không chứa ảnh bìa** — chuẩn MARC 21 mô tả thư mục, không có trường nào cho ảnh.
Tra ảnh cần ISBN, mà phần lớn kho thư viện đại học Việt Nam là bài giảng, giáo trình nội bộ,
luận văn, đề tài nghiên cứu — **không có ISBN**. Nên ảnh sinh tự động sẽ là ảnh chính của
đa số biểu ghi, phải làm cho tử tế.

### 3.2. Làm ảnh bìa sinh tự động cho đẹp
Ảnh dự phòng hiện tại đã chạy nhưng đơn điệu. Nâng cấp:
- Màu nền sinh theo **dạng tài liệu**, mỗi dạng một tông riêng nhất quán (sách / giáo trình /
  luận văn / luận án / bài giảng / báo tạp chí / đề tài nghiên cứu).
- Nhan đề dài tự co và ngắt dòng hợp lý — không tràn, không cắt giữa từ.
- Có tên tác giả và năm xuất bản.
- Nhãn dạng tài liệu ở chân ảnh.
- Tỉ lệ đúng bìa sách (2:3), đọc được ở cả kích thước nhỏ trong danh sách kết quả lẫn
  kích thước lớn ở trang chi tiết.
- Sinh phía máy chủ, cache lại, không sinh lại mỗi lần tải trang.

### 3.3. Tra ảnh bìa thật — 4 lớp, dừng ở lớp đầu tiên có kết quả
1. **Ảnh cán bộ tự tải lên** — ưu tiên cao nhất, không bao giờ bị ghi đè tự động.
2. **Trường `856$u`** trong chính biểu ghi, nếu là link ảnh.
3. **Google Books API** theo ISBN (phủ sách tiếng Việt tốt hơn Open Library).
4. **Open Library Covers API** theo ISBN.
5. Không có gì thì dùng ảnh sinh tự động ở mục 3.2.

Yêu cầu:
- Ảnh tải về lưu vào MinIO, **không hotlink** trực tiếp từ nguồn ngoài.
- Nút "Tra ảnh bìa" chạy hàng loạt cho biểu ghi chưa có ảnh, chạy nền, tôn trọng giới hạn
  tần suất của API.
- Màn hình biên mục cho phép cán bộ tự tải ảnh lên.
- Ghi lại nguồn ảnh của từng biểu ghi để truy vết.

**Báo cáo:** trên bộ dữ liệu hiện có — bao nhiêu biểu ghi có ISBN, bao nhiêu tra được ảnh thật
ở mỗi lớp, bao nhiêu phải dùng ảnh sinh tự động. Con số này quyết định có đáng đầu tư thêm
vào tra ảnh thật hay không.

---

## ƯU TIÊN 4 — NẠP DỮ LIỆU THẬT ĐỂ DEMO SINH ĐỘNG

### 4.1. Nguyên tắc lấy dữ liệu
**Chỉ lấy qua giao thức chuẩn thư viện (OAI-PMH, Z39.50, SRU) hoặc API mở có giấy phép
cho phép dùng lại. Không bóc HTML (scrape) từ bất kỳ trang web nào.**

Đây đồng thời là bài kiểm chứng thật cho Phase 11 — nếu Z39.50 hoặc OAI-PMH có lỗi ẩn,
việc kéo vài nghìn biểu ghi thật sẽ lòi ra ngay, vì dữ liệu thật luôn bẩn hơn dữ liệu test.

### 4.2. Khảo sát nguồn
Thử kết nối từng nguồn, ghi kết quả vào `docs/09-nguon-du-lieu.md`:

**Z39.50** (dùng máy khách đã viết ở Phase 11):
- `lx2.loc.gov:210/LCDB` — Thư viện Quốc hội Mỹ
- `z3950.loc.gov:7090/Voyager`
- Tìm thêm nguồn Z39.50 công khai còn hoạt động

**OAI-PMH** (dùng harvester đã viết ở Phase 11):
- Thử verb `Identify` với OPAC của các thư viện Việt Nam công bố endpoint OAI-PMH công khai.
  Các OPAC đáng thử (đây là **nguồn gốc**, tự công bố dữ liệu của chính họ):
  `phucvu.thuvientphcm.gov.vn`, `tracuuthuvien.angiang.gov.vn`, `opac.vaa.edu.vn`,
  và OPAC của các trường đại học khác.
- Nguồn quốc tế chắc chắn có: DOAJ, CORE, arXiv, HathiTrust.

**API mở:**
- Open Library (`openlibrary.org/developers/api`) — có MARC, có ảnh bìa, không cần khóa,
  cho phép dùng lại.
- Google Books API — metadata và ảnh bìa.

Với mỗi nguồn ghi rõ: kết nối được không, định dạng trả về, số biểu ghi tiếng Việt,
tốc độ, giới hạn tần suất, **và giấy phép có cho dùng cho mục đích demo thương mại không**.
Nguồn nào có điều khoản không cho dùng lại thì ghi vào tài liệu và **bỏ qua**.

### 4.3. Nạp dữ liệu
Mục tiêu **2.000–5.000 biểu ghi**, ưu tiên tài liệu tiếng Việt và tài liệu phù hợp một
trường đại học ngành tài nguyên – môi trường.

- Đi qua **đúng luồng nhập của hệ thống** (Z39.50 import / OAI harvest), **không chèn thẳng
  vào CSDL bằng SQL**. Mục đích là kiểm chứng luồng nhập thật.
- Tôn trọng giới hạn tần suất: chờ giữa các lần gọi, không gọi song song ồ ạt.
  Đặt `User-Agent: LibraryConnect/1.0` kèm email liên hệ.
- Ghi nguồn gốc từng biểu ghi vào trường `source` và `035$a` để truy vết được.
- Sinh ĐKCB cho khoảng 60% biểu ghi, phân bổ vào các kho, trạng thái đa dạng
  (trong kho / đang mượn / đặt giữ / chưa kiểm nhận / thanh lý).

### 4.4. Làm sinh động phần nghiệp vụ
Dữ liệu thư mục lấy từ nguồn thật, nhưng dữ liệu nghiệp vụ tự sinh cho hợp lý:
- **300 bạn đọc**: tên Việt thật, phân bổ theo khoa/ngành/khóa.
- **1.500 lượt mượn trả** trải đều 18 tháng, có đang mượn, đã trả đúng hạn, quá hạn, đang phạt.
- **50 đặt giữ chỗ** ở các trạng thái khác nhau.
- **10 đầu ấn phẩm định kỳ** với số đã nhận và số còn thiếu.
- **30 tài liệu số**: dùng PDF nguồn mở thật (Project Gutenberg, tài liệu Creative Commons),
  đủ các mức truy cập.
- **Ngành và môn học** đúng ngành đào tạo một trường tài nguyên – môi trường, có gán tài liệu.

### 4.5. Đóng gói
- Lệnh chạy lại được: `LC_SEED_DEMO=rich`, xóa sạch và nạp lại được.
- Giữ nguyên bộ seed nhỏ hiện tại làm mặc định; bộ lớn này là tùy chọn.

---

## ƯU TIÊN 5 — RÀ SOÁT CHẤT LƯỢNG TOÀN DIỆN

### 5.1. Đổi vai — kiểm tra như người dùng, không như tác giả
**Không đọc code để tìm lỗi. Không chạy lại bộ test cũ rồi báo xanh.**

Mở Chrome qua Playwright, đi hết mọi màn hình của cả `frontend-admin` và `frontend-opac`.
Với **mỗi** màn hình, chụp ảnh và tự trả lời 8 câu:

1. Có chữ nào bị cắt, tràn, đè lên nhau không?
2. Có dữ liệu kỹ thuật lọt ra giao diện không (JSON thô, UUID, tên trường tiếng Anh, mã lỗi,
   tên bảng, stack trace)?
3. Có ô trống, ảnh vỡ, chỗ đáng lẽ có dữ liệu mà rỗng không?
4. Nhãn và thông báo đã đúng tiếng Việt nghiệp vụ thư viện chưa, hay còn dịch máy / tiếng Anh?
5. Nút bấm có nhất quán vị trí, màu, thứ tự với các màn hình khác không?
6. Trạng thái rỗng hiển thị thế nào — có hướng dẫn người dùng làm gì tiếp, hay chỉ bảng trắng?
7. Trạng thái đang tải và trạng thái lỗi có được xử lý không?
8. Người dùng thật sẽ dùng màn hình này để làm gì, và họ làm được ngay mà không cần hỏi ai không?

**Hai lỗi mẫu đã tìm được, dùng để hiểu loại lỗi cần tìm** — cả hai đều **nhìn thấy được**
nhưng **không đo được bằng assert**:
- Tab "Biểu ghi MARC" trên OPAC từng đổ JSON thô ra cho bạn đọc xem (đã sửa).
- Nhãn menu "Báo cáo thống kê ..." bị cắt, đẩy mũi tên bung menu con ra ngoài khung,
  khiến người dùng tưởng mục đó hỏng.
- Nút "Đặt giữ chỗ" vẫn hiện nổi bật nhất trang khi biểu ghi có **0 ĐKCB** — bạn đọc bấm vào
  sẽ đặt giữ cái không tồn tại. Ẩn hoặc vô hiệu hóa; nếu chỉ có bản số thì thay bằng
  "Xem tài liệu số".

### 5.2. Thử phá, không thử đường đúng
Bộ test hiện có toàn đi đường đúng. Lần này đi đường sai:
- Ngày trả trước ngày mượn; ngày sinh ở tương lai; năm xuất bản 3025
- Xóa danh mục đang được biểu ghi/bạn đọc sử dụng
- Mượn cho thẻ hết hạn, thẻ bị khóa, bạn đọc đã ra trường
- Gia hạn cuốn đang có người đặt giữ; gia hạn quá số lần cho phép
- Nhập Excel: tệp rỗng, sai cột, 10.000 dòng, ô chứa công thức, ô chứa emoji
- Nhập ISO 2709: tệp hỏng giữa chừng, biểu ghi thiếu trường 245
- Tải lên tệp đổi đuôi (`.exe` đổi thành `.pdf`), tệp 0 byte, tệp 2GB
- Hai phiên cùng mượn/đặt giữ một ĐKCB cùng lúc
- Gõ tiếng Việt có dấu + ký tự đặc biệt (`'`, `"`, `<`, `>`, `&`, `\`) vào **mọi** ô nhập,
  rồi xuất PDF/Excel/ISO 2709 kiểm tra chữ có vỡ không
- Bấm nút hai lần liên tiếp thật nhanh (double submit)
- Bấm Back trình duyệt giữa chừng một luồng nhiều bước
- Mở trực tiếp URL trang chi tiết một bản ghi đã bị xóa

### 5.3. Đối chiếu ngược từ đặc tả
Mở `PROMPT-BUILD-LIBRARYCONNECT.md` mục 5. Đi từng dòng chức năng của cả 10 phân hệ đã làm.
Với mỗi dòng, **tự tìm chức năng đó trên giao diện thật** — không tra trong code, tìm bằng
cách bấm menu như người dùng. Tìm không ra trong 30 giây thì ghi nhận: chức năng có tồn tại
nhưng người dùng không tìm thấy — đó cũng là lỗi.

### 5.4. Cách báo cáo — CHIA HAI GIAI ĐOẠN, KHÔNG TRỘN

**Giai đoạn A — CHỈ TÌM, KHÔNG SỬA.**
Ghi toàn bộ phát hiện vào `docs/08-so-loi.md`, mỗi lỗi một dòng:

| # | Màn hình | Mô tả lỗi | Cách tái hiện | Mức độ | Loại |
|---|---|---|---|---|---|

- **Mức độ:** Nghiêm trọng (sai nghiệp vụ / mất dữ liệu / lỗ hổng) · Nặng (chức năng không
  dùng được) · Vừa (dùng được nhưng sai/khó) · Nhẹ (thẩm mỹ)
- **Loại:** Nghiệp vụ · Giao diện · Dữ liệu · Bảo mật · Hiệu năng · Ngôn ngữ

Tìm xong thì **dừng và đưa sổ lỗi ra xem trước khi sửa**.

**Giai đoạn B — sửa theo thứ tự mức độ.** Mỗi lỗi sửa xong viết thêm một test bắt được đúng
lỗi đó: test phải **đỏ trước khi sửa, xanh sau khi sửa** — chứng minh test thật sự bắt được lỗi.

### 5.5. Quy tắc trung thực cho mục này
- Không viết "đã kiểm tra, không có lỗi" cho một màn hình mà không chụp ảnh màn hình đó.
- **Tìm được ít lỗi là dấu hiệu rà chưa kỹ, không phải dấu hiệu phần mềm tốt.**
  Người dùng tìm được 3 lỗi trong vài phút; rà cả hệ thống mà ra dưới 30 lỗi thì rà lại.

---

## ƯU TIÊN 6 — LÀM SẠCH BẢNG ĐÁP ỨNG KỸ THUẬT

`docs/07-bang-dap-ung-ky-thuat.md` đang **tự mâu thuẫn** vì các phase sau chỉ thêm mục mới
ở cuối mà không rà lại dòng cũ:

| Chỗ | Vấn đề |
|---|---|
| Dòng 9 | Ghi *"Cập nhật lần cuối: sau khi hoàn thành Phase 11"* trong khi nội dung đã có tới mục B13 |
| A8 — Báo cáo 3 dạng đầu ra | Vẫn đánh **"Một phần"**, ghi *"áp dụng đầu tiên cho xuất nhật ký"* — nhưng phase 6, 7, 8, 9, 10, 13 đã làm xong hàng chục báo cáo đủ bảng + biểu đồ + PDF/Excel |
| II.7 — Nhập từ Z39.50 | Vẫn đánh **"Đang thực hiện — Phase 11"**, trong khi mục B10 và D2 cùng file khẳng định đã tra cứu thật tới Thư viện Quốc hội Mỹ |
| A7 | Ghi *"mở khi có dữ liệu kho (Phase 6)"* — Phase 6 xong lâu rồi |

Rà lại **toàn bộ** file, sửa mọi ô còn "Đang thực hiện" / "Một phần" không còn đúng, cập nhật
dòng "Cập nhật lần cuối". Mỗi ô đánh "Có" phải chỉ được đúng màn hình và đúng số kịch bản
kiểm thử chứng minh.

---

## THỨ TỰ THỰC HIỆN VÀ BÁO CÁO

1. Ưu tiên 1 (bảo mật) → commit
2. Ưu tiên 2 (MARC mapping) → commit, kèm kết quả kiểm bằng công cụ ngoài
3. Ưu tiên 3 (ảnh bìa) → commit, kèm số liệu phủ ảnh
4. Ưu tiên 4 (nạp dữ liệu) → commit, kèm `docs/09-nguon-du-lieu.md`
5. Ưu tiên 5 Giai đoạn A (tìm lỗi) → **dừng lại, xuất sổ lỗi**
6. Ưu tiên 5 Giai đoạn B (sửa lỗi) → commit theo nhóm mức độ
7. Ưu tiên 6 (bảng đáp ứng) → commit cuối

Mỗi ưu tiên xong báo ngắn 5 dòng: nội dung, số test, commit, lỗi thật bắt được,
số quyết định tự chốt. Dành context cho code.

**Bắt đầu Ưu tiên 1 ngay.**
