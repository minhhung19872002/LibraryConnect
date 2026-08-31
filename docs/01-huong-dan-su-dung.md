# Hướng dẫn sử dụng — LibraryConnect

Tài liệu dành cho cán bộ thư viện và bạn đọc. Mỗi mục dưới đây bám theo đúng thứ tự menu của phần
mềm, nên có thể mở phần mềm bên cạnh và làm theo từng bước.

Tài liệu này nói về **cách làm việc**. Phần cài đặt máy chủ xem `04-cai-dat-cau-hinh.md`; phần vận
hành, giám sát và xử lý sự cố xem `02-tai-lieu-quan-tri.md`.

---

## Mục lục

1. [Đăng nhập và làm quen giao diện](#1-đăng-nhập-và-làm-quen-giao-diện)
2. [Quản trị hệ thống](#2-quản-trị-hệ-thống-phân-hệ-i)
3. [Danh mục nghiệp vụ](#3-danh-mục-nghiệp-vụ)
4. [Biên mục](#4-biên-mục-phân-hệ-ii)
5. [Bổ sung và quản lý kho](#5-bổ-sung-và-quản-lý-kho-phân-hệ-iii)
6. [Ấn phẩm định kỳ](#6-ấn-phẩm-định-kỳ-phân-hệ-iv)
7. [Tài liệu số](#7-tài-liệu-số-phân-hệ-v)
8. [Bạn đọc](#8-bạn-đọc-phân-hệ-vi)
9. [Lưu thông](#9-lưu-thông-phân-hệ-vii)
10. [Quản trị nội dung](#10-quản-trị-nội-dung-phân-hệ-viii)
11. [Trang tra cứu dành cho bạn đọc](#11-trang-tra-cứu-dành-cho-bạn-đọc-phân-hệ-ix)
12. [Tài liệu môn học](#12-tài-liệu-môn-học-phân-hệ-x)
13. [Liên thư viện](#13-liên-thư-viện)
14. [Quy trình nghiệp vụ mẫu](#14-quy-trình-nghiệp-vụ-mẫu)
15. [Câu hỏi thường gặp](#15-câu-hỏi-thường-gặp)

---

## 1. Đăng nhập và làm quen giao diện

### 1.1. Hai cửa vào khác nhau

| Đối tượng | Địa chỉ | Đăng nhập bằng |
|---|---|---|
| Cán bộ thư viện | `http://<địa-chỉ-máy-chủ>/admin` | Tên đăng nhập và mật khẩu do quản trị viên cấp |
| Bạn đọc | `http://<địa-chỉ-máy-chủ>/` | Số thẻ thư viện và mật khẩu |

Lần đầu tiên đăng nhập bằng tài khoản quản trị (`admin`), hệ thống bắt buộc đổi mật khẩu. Không bỏ
qua bước này: mật khẩu mặc định được ghi công khai trong tài liệu cài đặt.

### 1.2. Bố cục màn hình quản trị

- **Cột trái** — menu theo phân hệ. Menu chỉ hiện những chức năng tài khoản của bạn được cấp quyền,
  nên hai người ở hai bộ phận nhìn thấy hai menu khác nhau. Đó là bình thường.
- **Thanh trên** — tên thư viện và tài khoản đang dùng. Bấm vào tên tài khoản để đổi mật khẩu hoặc
  đăng xuất.
- **Vùng giữa** — nội dung màn hình. Mọi màn hình danh sách đều theo một bố cục: thanh lọc ở trên,
  bảng ở giữa, phân trang ở dưới. Khi tick chọn nhiều dòng thì thanh thao tác hàng loạt hiện ra.

### 1.3. Những nút gặp ở mọi màn hình

| Nút | Việc nó làm |
|---|---|
| **Thêm mới** | Mở biểu mẫu tạo bản ghi mới |
| **Sửa** | Mở lại biểu mẫu với dữ liệu hiện có |
| **Xóa** | Đánh dấu đã xóa. Dữ liệu **không mất hẳn** — vẫn tra lại được trong nhật ký hệ thống |
| **Tìm kiếm** | Lọc danh sách theo điều kiện đang đặt |
| **Xuất Excel** / **Xuất PDF** | Xuất đúng phần dữ liệu đang lọc, không phải toàn bộ bảng |
| **In** | Sinh tệp PDF theo mẫu biểu đã chọn |

Mọi thao tác thêm, sửa, xóa đều được ghi vào nhật ký hệ thống kèm tên người thực hiện.

---

## 2. Quản trị hệ thống (Phân hệ I)

Dành cho quản trị viên. Cán bộ nghiệp vụ thường không thấy nhóm menu này.

### 2.1. Nhóm người dùng

Quyền được cấp cho **nhóm**, không cấp cho từng người. Một người có thể thuộc nhiều nhóm và nhận
tổng hợp quyền của các nhóm đó.

Hệ thống cài sẵn năm nhóm: Quản trị hệ thống, Cán bộ biên mục, Cán bộ bổ sung, Cán bộ lưu thông,
Thủ thư. Năm nhóm này đủ dùng cho phần lớn thư viện; chỉ tạo thêm khi có bộ phận đặc thù.

**Cấp quyền cho nhóm:**
1. Vào **Quản trị hệ thống → Nhóm người dùng**, bấm biểu tượng chìa khóa ở dòng nhóm cần sửa.
2. Cây quyền hiện ra theo ba tầng: phân hệ → chức năng → hành động (Xem / Thêm / Sửa / Xóa / Duyệt /
   In / Xuất).
3. Tick ở tầng cha sẽ tự tick toàn bộ tầng con. Lưu lại là có hiệu lực ngay ở lần gọi tiếp theo của
   người dùng, không cần đăng xuất.

Muốn tạo một nhóm gần giống nhóm đã có: dùng nút **Sao chép quyền từ nhóm khác** rồi sửa lại vài mục,
nhanh hơn tick lại từ đầu.

### 2.2. Người dùng

Thêm cán bộ mới: nhập họ tên, tên đăng nhập, email, chọn nhóm quyền, rồi chọn **phạm vi dữ liệu**.

> **Phạm vi dữ liệu** giới hạn cán bộ chỉ thao tác trên kho hoặc cơ sở được giao. Cán bộ ở Cơ sở 2
> chỉ thấy và chỉ ghi mượn được ĐKCB của Cơ sở 2, dù họ có quyền lưu thông đầy đủ. Để trống nghĩa là
> không giới hạn.

Khi cán bộ quên mật khẩu: mở hồ sơ, bấm **Đặt lại mật khẩu**, đưa mật khẩu tạm cho họ. Hệ thống buộc
họ đổi ngay ở lần đăng nhập kế tiếp.

Tài khoản nghỉ việc thì **khóa**, không xóa: xóa sẽ làm mất dấu vết ai đã làm gì trong nhật ký.

### 2.3. Tham số hệ thống

Đây là nơi khai những thứ không được viết cứng trong phần mềm:

| Nhóm tham số | Chứa gì |
|---|---|
| Thông tin thư viện | Tên, địa chỉ, điện thoại, email, logo — hiện trên mọi biểu mẫu in và trên trang tra cứu |
| Quy tắc sinh mã | Tiền tố và độ dài của số ĐKCB, số thẻ, mã đơn đặt; có đặt lại số theo năm hay không |
| Biên mục | Mẫu MARC mặc định, mã cơ quan biên mục (040$a), ngôn ngữ và nước mặc định, quy tắc ký hiệu xếp giá |
| Lưu thông | Số ngày mượn, số bản tối đa, tiền phạt mỗi ngày khi chưa khai chính sách riêng; ngưỡng nợ phí bị chặn mượn |
| OPAC | Bật/tắt các khối trên trang chủ, cho nhận xét hay không, có hiện dòng "Powered by LibraryConnect" hay không |
| Email | Máy chủ gửi thư dùng cho thông báo nhắc hạn |
| Sao lưu | Giờ chạy sao lưu tự động, số bản giữ lại |

Mọi thay đổi tham số đều ghi lại ai đổi, đổi từ giá trị nào sang giá trị nào.

### 2.4. Nhật ký hệ thống

Tra theo khoảng thời gian, người dùng, hành động, đối tượng hoặc địa chỉ IP. Bấm vào một dòng để xem
chi tiết: hệ thống hiện giá trị **trước** và **sau** khi sửa, tô sáng đúng những trường đã đổi.

Trong **Cài đặt ghi nhận**, có thể tắt ghi log thao tác xem (Read) cho những bảng ít quan trọng nếu
nhật ký phình quá nhanh. Không nên tắt ghi log Thêm/Sửa/Xóa.

### 2.5. Sao lưu cơ sở dữ liệu

- **Sao lưu ngay**: bấm nút, chọn kiểu (toàn bộ hoặc chỉ dữ liệu), có kèm tệp tài liệu số hay không.
- **Sao lưu tự động**: khai giờ chạy hằng ngày và số bản giữ lại.
- **Phục hồi**: chọn một bản sao lưu, bấm Phục hồi, xác nhận hai lần và nhập lại mật khẩu.

> Phục hồi ghi đè toàn bộ dữ liệu hiện tại. Trước khi phục hồi, hãy sao lưu tình trạng hiện tại đã —
> nếu chọn nhầm tệp thì vẫn còn đường lùi.

Quy trình đầy đủ, kể cả cách sao lưu ra ổ đĩa ngoài, xem `03-sao-luu-phuc-hoi.md`.

---

## 3. Danh mục nghiệp vụ

**Danh mục** là màn hình dùng chung cho hơn 20 bảng danh mục: dạng tài liệu, vật mang tin, ngôn ngữ,
nước xuất bản, nhà xuất bản, tác giả, đề mục chủ đề, từ khóa, khung phân loại, tùng thư, bộ sưu tập,
loại bạn đọc, khoa, ngành, môn học, nhà cung cấp, nguồn kinh phí…

Chọn danh mục ở ô trên cùng, phần còn lại của màn hình đổi theo. Mọi danh mục đều:

- Thêm / sửa / xóa, tìm kiếm, bật tắt trạng thái sử dụng.
- **Nhập từ Excel**: tải tệp mẫu, điền, tải lên. Hệ thống kiểm tra thử trước và chỉ ra dòng nào sai
  vì lý do gì, sửa xong mới nhập thật.
- **Xuất Excel** theo bộ lọc đang đặt.
- **Gộp trùng**: khi phát hiện hai dòng cùng là một (ví dụ "Nguyễn Văn A" và "Nguyen Van A"), chọn cả
  hai rồi bấm Gộp. Toàn bộ biểu ghi đang trỏ tới dòng bị gộp sẽ chuyển sang dòng giữ lại.

> Gộp trùng không hoàn tác được. Hãy xem kỹ số biểu ghi liên quan ở mỗi dòng trước khi gộp.

---

## 4. Biên mục (Phân hệ II)

### 4.1. Trình soạn MARC 21

Vào **Biên mục → Biểu ghi thư mục → Thêm mới**. Màn hình soạn biểu ghi gồm bốn phần:

1. **Đầu biểu (Leader)** và **trường 008** — hệ thống điền sẵn theo dạng tài liệu. Trường 008 có nút
   mở trình hỗ trợ: 40 vị trí mã hóa được diễn giải thành các ô chọn có nghĩa, không phải đếm ký tự.
2. **Bảng trường** — mỗi dòng là một trường: Tag | Chỉ thị 1 | Chỉ thị 2 | Nội dung. Gõ số tag sẽ
   hiện tên tiếng Việt của trường.
3. **Nội dung trường con** — gõ thẳng theo dạng `$a Giáo trình cơ sở dữ liệu /$c Nguyễn Văn A`, hoặc
   bấm mở biểu mẫu chi tiết để nhập từng trường con vào từng ô riêng.
4. **Xem trước** — hai kiểu: dạng ISBD (như phích thư viện) và dạng thẻ mục lục.

Phím tắt hay dùng: `Ctrl+S` lưu, `Ctrl+D` nhân bản dòng đang đứng.

Hệ thống kiểm tra ngay khi gõ: trường bắt buộc còn thiếu, chỉ thị không hợp lệ, trường con không
thuộc trường đó — tất cả hiện thành dòng cảnh báo màu đỏ ngay dưới dòng tương ứng, không đợi tới lúc
lưu mới báo.

**Rút ngắn thời gian nhập:**
- Chọn **mẫu biên mục** theo dạng tài liệu để có sẵn khung trường thường dùng.
- Bấm **Lấy từ Z39.50** để tải biểu ghi có sẵn từ thư viện khác rồi hiệu đính (xem mục 13).
- Trong **Cấu hình biên mục → Giá trị ngầm định**, khai sẵn những trường luôn giống nhau (040$a,
  041$a, 044$a…) để không phải gõ lại ở mỗi biểu ghi.

### 4.2. Tạo ĐKCB cho biểu ghi

Sau khi lưu biểu ghi, chuyển sang thẻ **Ấn phẩm**: nhập số bản, kho, giá, nguồn kinh phí. Hệ thống tự
sinh số ĐKCB, số mã vạch và ký hiệu xếp giá theo quy tắc đã khai trong tham số.

### 4.3. Sửa và xem lại lịch sử

Mỗi lần lưu, phiên bản cũ được giữ lại. Trong màn hình chi tiết, thẻ **Lịch sử sửa đổi** cho xem ai
sửa, sửa lúc nào, và so sánh hai phiên bản cạnh nhau. Khi cần, khôi phục lại phiên bản cũ.

Biểu ghi còn ĐKCB đang lưu thông thì không xóa được — phải xử lý hết ĐKCB trước.

### 4.4. Hàng đợi biên mục

Biểu ghi tạo từ biên mục sơ lược (bộ phận bổ sung) hoặc nhập tự động sẽ vào **Hàng đợi biên mục**.
Trưởng bộ phận phân công cán bộ, đặt hạn và độ ưu tiên; cán bộ nhận việc, biên mục xong thì gửi
duyệt. Người duyệt trả lại kèm lý do nếu chưa đạt.

### 4.5. Nhập biểu ghi hàng loạt

| Nguồn | Màn hình | Ghi chú |
|---|---|---|
| Tệp ISO 2709 (`.iso`, `.mrc`) | Nhập biểu ghi từ tệp | Đúng chuẩn trao đổi giữa các phần mềm thư viện |
| Tệp MARCXML | Nhập biểu ghi từ tệp | Nhận diện tự động theo nội dung tệp |
| Excel | Nhập biểu ghi từ Excel | Có bước ánh xạ cột Excel sang trường MARC, lưu lại được để dùng lần sau |
| Z39.50 | Tra cứu liên thư viện | Từng biểu ghi một, có hiệu đính trước khi lưu |

Cả ba đường nhập tệp đều theo bốn bước: tải tệp lên → xem trước và soát lỗi → chọn cách xử lý trùng
(bỏ qua / ghi đè / tạo mới) → chạy nền và tải về tệp kết quả.

### 4.6. In phích

**Biên mục → Mẫu phích và in phích**. Thiết kế mẫu bằng cách kéo thả ô nội dung, mỗi ô trỏ tới một
trường MARC. Khổ chuẩn 7,5 × 12,5 cm có sẵn. In hàng loạt bằng cách lọc biểu ghi rồi chọn mẫu; tệp
PDF xuất ra xếp nhiều phích trên một trang A4 đúng khổ để cắt.

---

## 5. Bổ sung và quản lý kho (Phân hệ III)

### 5.1. Đường đi của một cuốn sách

```
Yêu cầu đặt mua  →  Duyệt  →  Đơn đặt  →  Nhận hàng  →  ĐKCB (chưa kiểm nhận)
      →  Kiểm nhận  →  Xếp giá  →  In tem mã vạch và nhãn gáy  →  Cho mượn
```

### 5.2. Yêu cầu đặt mua

Khoa hoặc bộ phận gửi đề nghị mua. Mỗi dòng đề nghị có nút tra nhanh **kiểm tra thư viện đã có
chưa** theo ISBN hoặc nhan đề — tránh mua trùng. Đề nghị nhiều đầu sách một lúc thì nhập từ Excel.

Người duyệt xem danh sách chờ, duyệt cả phiếu hoặc duyệt từng dòng, có thể sửa số lượng, và từ chối
thì phải ghi lý do.

### 5.3. Đơn đặt và nhận hàng

Gom các dòng đã duyệt thành đơn đặt theo nhà cung cấp. In đơn gửi nhà cung cấp bằng nút **In đơn**.

Khi hàng về: mở đơn, nhập số lượng thực nhận từng dòng. Nhận thiếu thì đơn chuyển sang trạng thái
"Nhận một phần" và vẫn theo dõi tiếp. Nhận xong sinh ĐKCB ngay tại màn hình này, và có thể tạo
**biên bản bàn giao** in ra để hai bên ký.

### 5.4. Kiểm nhận và xếp giá

ĐKCB mới nhận ở trạng thái **Chưa kiểm nhận** và đang khóa, chưa cho mượn. Cán bộ kiểm tra tình trạng
vật lý rồi bấm **Kiểm nhận** — ĐKCB chuyển sang **Trong kho** và mở khóa.

Xếp giá: chọn nhiều ĐKCB, gán cùng kho và giá một lượt. Ký hiệu xếp giá sinh tự động theo quy tắc
(mặc định là ký hiệu DDC + ba chữ cái đầu của tên tác giả).

Khi cần rút một cuốn khỏi lưu thông tạm thời (đem đi sửa, đem đi số hóa), dùng nút **Khóa** kèm lý
do thay vì xóa.

### 5.5. In tem mã vạch và nhãn gáy

Chọn ĐKCB theo đơn đặt, theo kho, theo khoảng số ĐKCB hoặc tick chọn tay. Chọn mẫu tem, xem trước rồi
xuất PDF. Hỗ trợ mã vạch CODE39, CODE128 và QR.

> In thử một trang lên giấy thường và đặt chồng lên tờ tem để kiểm tra căn lề, trước khi in cả tập.
> Mỗi hãng giấy tem lệch nhau vài milimét.

### 5.6. Chuyển kho

Quét mã vạch nhiều cuốn, chọn kho đích, ghi lý do và số quyết định. Hệ thống in phiếu chuyển kho và
lưu lịch sử di chuyển của từng ĐKCB.

### 5.7. Kiểm kê

Đúng năm bước, làm tuần tự:

1. **Đóng kho** — ngưng mượn trả tại kho đó. Màn hình lưu thông sẽ cảnh báo.
2. **Tạo kỳ kiểm kê** — chọn kho, phạm vi, thời gian, phân công cán bộ. Hệ thống chụp lại danh sách
   ĐKCB đáng lẽ phải có.
3. **Quét** — quét mã vạch liên tục. Mỗi lần quét phản hồi ngay: khớp, thừa, hay thuộc kho khác. Nếu
   dùng máy đọc rời thì nhập tệp quét vào.
4. **Đóng kỳ** — chốt lại và đối chiếu.
5. **Xem kết quả** — bốn danh sách: khớp, thiếu, thừa, sai kho. Từ danh sách thiếu, tạo thẳng đề nghị
   thanh lý hoặc quyết định mất.

### 5.8. Báo cáo bổ sung

Bảy báo cáo, đều có bộ lọc thời gian và kho, đều hiện bảng kèm biểu đồ và xuất được Excel/PDF: theo
dạng tài liệu, theo vật mang tin, theo thời gian bổ sung, theo ngôn ngữ, theo nguồn kinh phí, danh
sách ĐKCB hủy bỏ, và báo cáo tổng quát toàn kho.

---

## 6. Ấn phẩm định kỳ (Phân hệ IV)

### 6.1. Khai một đầu báo

**Ấn phẩm định kỳ → Báo, tạp chí → Thêm mới**: tên, ISSN, nhà xuất bản, ngôn ngữ, kho lưu, và quan
trọng nhất là **kỳ hạn**: nhật báo, tuần, nửa tháng, tháng, quý, năm hoặc không định kỳ; kèm quy tắc
đánh số (số liên tục, số theo năm, hay có cả tập và số) và các kỳ nghỉ không xuất bản.

### 6.2. Sinh số dự kiến

Từ cấu hình kỳ hạn, bấm **Sinh số** cho khoảng thời gian đặt mua. Hệ thống dựng sẵn toàn bộ danh sách
số sẽ nhận. Sửa tay từng số trước khi chốt nếu tòa soạn có lịch riêng.

### 6.3. Ghi nhận số về

- Một đầu báo: mở đầu báo, thẻ **Ghi nhận**, tick số đã về, nhập ngày nhận và số lượng.
- Nhiều đầu báo cùng lúc: **Bổ sung tổng thể** liệt kê mọi số đến hạn của mọi đầu báo, tick hàng loạt.

Lưới tình trạng tô màu theo trạng thái: đã nhận, còn thiếu, hay mới chỉ dự kiến — nhìn là thấy ngay
tháng nào hụt số.

### 6.4. Khiếu nại số thiếu

Ở thẻ **Kiểm tra**, chọn các số thiếu và bấm **Tạo khiếu nại**. Hệ thống sinh phiếu khiếu nại gửi nhà
cung cấp và theo dõi phản hồi.

### 6.5. Đóng tập

Cuối năm, chọn khoảng số (ví dụ số 1–12 năm 2025), bấm **Đóng tập**. Hệ thống sinh một ĐKCB mới cho
tập đóng bìa, chuyển các số lẻ sang trạng thái "đã đóng tập" và in nhãn gáy cho tập.

### 6.6. Mục lục bài trích

Với mỗi số, nhập danh sách bài viết: nhan đề, tác giả, trang, tóm tắt, từ khóa. Mỗi bài có thể sinh
một biểu ghi MARC riêng (trường 773 trỏ về ấn phẩm mẹ) để bạn đọc tra cứu ra từng bài báo. Nhập cả
mục lục từ Excel được.

---

## 7. Tài liệu số (Phân hệ V)

### 7.1. Tải tài liệu lên

**Tài liệu số → Kho tài liệu số**. Chọn bộ sưu tập, kéo thả tệp vào (PDF, DOCX, EPUB, MP4, MP3, ảnh).
Tệp lớn được cắt thành nhiều mảnh và tải lên lần lượt, nên mạng đứt giữa chừng thì tải tiếp chứ không
mất từ đầu.

Sau khi tải xong, hệ thống tự động: đếm số trang, dựng ảnh bìa, tạo bản xem thử, tính mã kiểm tra, và
nhận dạng chữ (OCR tiếng Việt) để tìm được cả nội dung bên trong tài liệu.

### 7.2. Đặt mức truy cập

| Mức | Ai xem được |
|---|---|
| Công khai | Mọi người, không cần đăng nhập |
| Nội bộ | Bạn đọc đã đăng nhập |
| Hạn chế | Chỉ bạn đọc đã gửi yêu cầu và được duyệt |
| Cấm | Chỉ cán bộ có quyền |

Với mỗi tài liệu còn khai riêng: cho tải về hay không, cho in hay không, xem thử được mấy trang, có
đóng chữ chìm hay không.

> Tài liệu không cho tải về thì bạn đọc chỉ xem được từng trang dưới dạng ảnh do máy chủ dựng, có
> đóng chữ chìm ghi số thẻ, thời điểm và địa chỉ máy. Đây là cách bảo vệ bản quyền thực tế nhất:
> không có tệp gốc nào đi qua trình duyệt.

### 7.3. Duyệt yêu cầu đọc tài liệu hạn chế

**Tài liệu số → Yêu cầu đọc tài liệu**: xem lý do bạn đọc nêu, rồi duyệt kèm thời hạn truy cập và số
lần xem tối đa, hoặc từ chối kèm lý do. Bạn đọc nhận được thông báo. Quyền tự hết hạn đúng ngày đã
đặt, không phải nhớ để thu hồi.

---

## 8. Bạn đọc (Phân hệ VI)

### 8.1. Lập thẻ

**Bạn đọc → Hồ sơ bạn đọc → Thêm mới**. Số thẻ sinh tự động. Ảnh chân dung tải lên từ tệp hoặc chụp
thẳng bằng webcam, có khung cắt ảnh cho đúng tỷ lệ thẻ.

Đầu năm học, thay vì nhập tay: **Nhập xuất dữ liệu bạn đọc → Nhập từ Excel**. Ảnh cả khóa thì nén
thành một tệp ZIP đặt tên ảnh theo mã sinh viên, hệ thống tự khớp.

### 8.2. In thẻ

Thiết kế mẫu ở **Mẫu thẻ bạn đọc**: khổ CR80 (85,6 × 54 mm) như thẻ ngân hàng, có mặt trước và mặt
sau, đặt ảnh nền, logo, ảnh bạn đọc, các trường thông tin và mã vạch số thẻ.

In hàng loạt: lọc bạn đọc cần in, chọn mẫu, xem trước rồi xuất PDF. Tệp in được cả trên máy in thẻ
nhựa lẫn in nhiều thẻ trên một trang A4.

### 8.3. Quản lý vòng đời thẻ

| Việc | Cách làm |
|---|---|
| Gia hạn thẻ | Lọc theo khóa hoặc theo ngày hết hạn, chọn tất cả, bấm **Gia hạn hàng loạt** |
| Tạm khóa | Mở hồ sơ, bấm **Tạm khóa**, ghi lý do. Bạn đọc không mượn được nhưng vẫn tra cứu được |
| Cấp lại thẻ mất | Bấm **Cấp lại**, ghi lý do. Số thẻ mới được sinh, thẻ cũ giữ trong lịch sử |
| Sinh viên ra trường | Lọc theo khóa, bấm **Chuyển trạng thái ra trường**. Hệ thống **chặn** những người còn sách chưa trả hoặc còn nợ phí và liệt kê ra danh sách |

---

## 9. Lưu thông (Phân hệ VII)

### 9.1. Chính sách lưu thông — khai trước khi mở cửa

**Lưu thông → Chính sách lưu thông**. Mỗi dòng chính sách là một ô của ma trận *Loại bạn đọc × Dạng
tài liệu × Kho*, quy định: số bản tối đa, số ngày mượn, số lần gia hạn, tiền phạt mỗi ngày, số ngày
ân hạn, số ngày giữ chỗ.

Để trống một chiều nghĩa là "áp dụng cho mọi giá trị". Nhiều dòng cùng khớp thì dòng có **độ ưu tiên**
cao hơn thắng; bằng nhau thì dòng khai cụ thể hơn thắng.

Khai thêm **lịch nghỉ lễ**: hạn trả rơi vào ngày nghỉ sẽ tự đẩy sang ngày làm việc kế tiếp, và ngày
nghỉ không bị tính tiền phạt.

### 9.2. Quầy lưu thông — màn hình dùng nhiều nhất trong ngày

Toàn bộ thao tác làm được bằng bàn phím và máy quét, không cần chạm chuột.

**Ghi mượn:**
1. Quét thẻ bạn đọc. Thông tin và ảnh hiện lên, kèm cảnh báo nếu thẻ hết hạn, đang khóa, đang nợ phí
   hoặc đang giữ sách quá hạn.
2. Quét mã vạch từng cuốn. Mỗi lần quét, hệ thống đối chiếu chính sách và báo ngay bằng âm thanh:
   một tiếng ngắn là được, tiếng cảnh báo là có vấn đề kèm dòng giải thích.
3. Nhấn Enter để hoàn tất và in phiếu mượn.

**Ghi trả:** quét mã vạch cuốn sách. Nếu quá hạn, hệ thống tính tiền phạt và hiện ra. Nếu cuốn đó có
người đang đặt giữ, màn hình cảnh báo giữ sách lại tại quầy và gửi thông báo cho người đặt.

**Gia hạn:** quét thẻ hoặc mã vạch. Hệ thống tự kiểm tra: còn lượt gia hạn không, có ai đặt giữ không,
sách đã quá hạn chưa.

> Hạn trả, tiền phạt, số lượt gia hạn còn lại đều do máy chủ tính. Cán bộ không phải nhẩm, và hai
> quầy khác nhau không thể ra hai kết quả khác nhau.

### 9.3. Đặt giữ chỗ

Bạn đọc đặt từ trang tra cứu hoặc nhờ cán bộ đặt tại quầy. Hàng đợi xếp theo thứ tự đặt. Khi sách
được trả về, người đầu hàng đợi nhận thông báo và sách được giữ tại quầy trong số ngày đã khai.

### 9.4. Tiền phạt

**Lưu thông → Tiền phạt**: danh sách khoản phạt theo bạn đọc. Thu tiền thì bấm **Thu**, hệ thống in
biên lai. Miễn giảm phải ghi lý do và chỉ tài khoản có quyền riêng mới làm được.

### 9.5. Cổng ra vào và tủ gửi đồ

Quét thẻ tại cổng để ghi nhận lượt vào thư viện — số liệu này chạy thẳng vào báo cáo giờ cao điểm.
Tủ gửi đồ có sơ đồ trực quan: bấm một ô trống để giao tủ, bấm ô đang dùng để trả tủ.

### 9.6. Bảy báo cáo lưu thông

Bạn đọc ra vào thư viện · Bạn đọc đang giữ sách · Lịch sử mượn trả · Bạn đọc mượn quá hạn (có nút gửi
email nhắc hàng loạt) · Sử dụng tủ gửi đồ · Bạn đọc mượn nhiều nhất · Ấn phẩm được mượn nhiều nhất.

---

## 10. Quản trị nội dung (Phân hệ VIII)

- **Thông tin trang thư viện** — tên, logo, ảnh nền, khẩu hiệu, địa chỉ, giờ mở cửa từng cơ sở, mạng
  xã hội, và các khối hiện trên trang chủ.
- **Trang tĩnh** — Giới thiệu, Nội quy, Hướng dẫn sử dụng, Liên hệ, Hỏi đáp. Soạn thảo trực quan: chữ
  đậm, tiêu đề, danh sách, bảng, chèn ảnh và nhúng video YouTube/Vimeo.
- **Tin tức – sự kiện** — có chuyên mục, ảnh đại diện, tin nổi bật và hẹn giờ xuất bản.
- **Thư viện ảnh** — album ảnh sự kiện.
- **Nhận xét bạn đọc** — nhận xét chỉ hiện công khai sau khi cán bộ duyệt.

> Nội dung soạn ra được lọc mã độc ngay lúc lưu. Dán nội dung từ Word hay từ trang web khác vào là an
> toàn; phần định dạng lạ sẽ bị bỏ đi.

---

## 11. Trang tra cứu dành cho bạn đọc (Phân hệ IX)

Địa chỉ là trang gốc của hệ thống. Không cần đăng nhập vẫn tra cứu được.

### 11.1. Tìm kiếm

- **Tìm cơ bản**: gõ vào ô lớn giữa trang chủ, chọn phạm vi (tất cả, nhan đề, tác giả, chủ đề, ISBN…).
  **Gõ không dấu vẫn ra kết quả** — "co so du lieu" tìm được "Cơ sở dữ liệu".
- **Tìm nâng cao**: nhiều điều kiện nối bằng VÀ / HOẶC / KHÔNG, mỗi điều kiện chọn trường riêng, kèm
  giới hạn năm xuất bản, ngôn ngữ, dạng tài liệu, kho.
- **Duyệt theo**: chủ đề, tác giả, phân loại DDC, bộ sưu tập, ngành đào tạo, môn học, luận văn – luận
  án, báo – tạp chí.

Cột lọc bên trái đếm số tài liệu theo từng giá trị **trên đúng tập kết quả hiện tại**, nên bấm vào
không bao giờ ra danh sách rỗng.

### 11.2. Trang chi tiết tài liệu

Thông tin thư mục, tóm tắt, chủ đề bấm được để tìm tiếp, và **danh sách bản in kèm tình trạng, ký
hiệu xếp giá, kho và giá** — bạn đọc biết cuốn nào đang rảnh và nằm ở đâu trước khi lên kho.

Còn có: nút đặt giữ chỗ, nút đọc tài liệu số, xem biểu ghi MARC, và xuất trích dẫn theo sáu kiểu
(APA, MLA, Chicago, BibTeX, RIS, EndNote).

### 11.3. Trang cá nhân của bạn đọc

Đăng nhập bằng số thẻ và mật khẩu. Có tám thẻ: sách đang mượn (kèm nút gia hạn), lịch sử mượn trả,
đặt giữ, tiền phạt, thông báo, tài liệu yêu thích, tìm kiếm đã lưu, thông tin cá nhân.

---

## 12. Tài liệu môn học (Phân hệ X)

**Tài liệu môn học → Gán tài liệu cho môn học**: chọn môn ở cột trái, tìm tài liệu ở cột phải, tick
nhiều cuốn rồi gán một lần. Mỗi liên kết có một mức độ: Giáo trình chính / Tài liệu tham khảo bắt
buộc / Tài liệu tham khảo thêm.

Khoa gửi danh mục cả học kỳ thì nhập từ Excel: tệp mẫu có sẵn cột mã môn, mã tài liệu (ISBN, số kiểm
soát hoặc số ĐKCB), mức độ và ghi chú. Dòng nào sai thì bị bỏ qua kèm lý do, không chặn cả tệp.

Trên trang tra cứu, bạn đọc duyệt **Ngành → Môn học → Tài liệu** và thấy ngay cuốn nào còn bản rảnh.

**Báo cáo tài liệu môn học** trả lời ba câu hỏi: môn nào chưa có tài liệu, cuốn nào đang bị nhiều môn
dùng chung mà thiếu bản, và mỗi ngành đáp ứng được bao nhiêu phần trăm số môn.

---

## 13. Liên thư viện

### 13.1. Lấy biểu ghi từ thư viện khác

**Liên thư viện → Tra cứu liên thư viện**: chọn một hoặc nhiều thư viện đích, nhập từ khóa. Kết quả
hiện theo từng nơi. Chọn biểu ghi ưng ý, bấm **Nhập vào hệ thống** — trình soạn MARC mở ra để hiệu
đính trước khi lưu, không nhập thẳng vào kho.

Danh sách thư viện đích khai ở **Máy chủ thư viện bạn**, có nút **Kiểm tra kết nối**. Hệ thống cài sẵn
ba máy chủ công khai để thử ngay.

### 13.2. Cho thư viện khác tra vào

Hệ thống mở sẵn ba lối cho bên ngoài tra cứu vào kho của mình: Z39.50 (cổng 210), SRU và OAI-PMH.
Địa chỉ cụ thể xem `05-api-reference.md`. Bật, tắt và giới hạn dải IP trong Tham số hệ thống.

### 13.3. Thu hoạch metadata định kỳ

**Kho OAI-PMH**: khai địa chỉ kho nguồn và lịch chạy. Hệ thống tự lấy biểu ghi mới theo lịch và ghi
lại số lượng đã lấy mỗi lần.

---

## 14. Quy trình nghiệp vụ mẫu

### 14.1. Đầu năm học

1. Nhập danh sách sinh viên khóa mới từ Excel, nhập ảnh theo tệp ZIP.
2. In thẻ hàng loạt.
3. Gia hạn thẻ cho các khóa đang học.
4. Chuyển trạng thái ra trường cho khóa vừa tốt nghiệp — xử lý trước danh sách còn nợ sách.
5. Nhập danh mục tài liệu môn học của học kỳ mới.

### 14.2. Một đợt bổ sung sách

1. Nhận đề nghị mua từ các khoa (nhập Excel hoặc để họ gửi phiếu).
2. Duyệt, gom thành đơn đặt theo nhà cung cấp, in đơn gửi đi.
3. Hàng về: ghi nhận số lượng, sinh ĐKCB, in biên bản bàn giao.
4. Kiểm nhận từng cuốn, xếp giá, in tem mã vạch và nhãn gáy.
5. Biên mục chi tiết những cuốn mới (lấy sẵn biểu ghi qua Z39.50 cho nhanh).
6. Đối chiếu **Báo cáo bổ sung** với chứng từ của phòng tài chính.

### 14.3. Kiểm kê cuối năm

Xem mục 5.7. Làm ngoài giờ phục vụ hoặc chia kho ra làm từng đợt để không phải đóng cả thư viện.

---

## 15. Câu hỏi thường gặp

**Không thấy chức năng trong menu?**
Menu chỉ hiện những gì tài khoản được cấp quyền. Liên hệ quản trị viên để bổ sung nhóm quyền.

**Bấm được nút nhưng hệ thống báo "Bạn không có quyền"?**
Nút hiện theo quyền, còn máy chủ kiểm tra lại một lần nữa. Trường hợp này thường do quyền vừa bị thu
hồi. Đăng xuất rồi đăng nhập lại để cập nhật.

**Máy quét mã vạch không hoạt động?**
Máy quét hoạt động như bàn phím. Mở Notepad và quét thử: nếu ra dãy số thì máy tốt, vấn đề nằm ở chỗ
con trỏ chưa nằm trong ô nhập của màn hình.

**Xóa nhầm một biểu ghi?**
Dữ liệu chỉ bị đánh dấu đã xóa, không mất. Nhờ quản trị viên tra trong nhật ký hệ thống và khôi phục.

**Tìm không ra cuốn sách chắc chắn có trong kho?**
Kiểm tra ba điều: biểu ghi đã ở trạng thái **Đã xuất bản** chưa (biểu ghi nháp không hiện trên trang
tra cứu), có gõ nhầm chính tả không, và bộ lọc bên trái có đang giới hạn quá hẹp không.

**In tem bị lệch?**
Trong hộp thoại in của trình duyệt, đặt tỷ lệ **100%** và tắt "vừa khít khổ giấy". Trình duyệt co
trang lại là tem lệch.

**Bạn đọc báo không đăng nhập được?**
Kiểm tra trạng thái thẻ trong hồ sơ: hết hạn hoặc đang tạm khóa thì đăng nhập được nhưng không mượn
được; nếu chưa từng đặt mật khẩu, cán bộ đặt lại mật khẩu giúp trong màn hình hồ sơ bạn đọc.
