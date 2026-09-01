# Nguồn dữ liệu thư mục dùng cho bản demo

Tài liệu này ghi lại kết quả khảo sát các nguồn dữ liệu thư mục công khai, giấy phép của từng nguồn,
và cách LibraryConnect lấy dữ liệu về.

**Nguyên tắc:** chỉ lấy qua giao thức chuẩn thư viện (Z39.50, SRU, OAI-PMH) hoặc API công khai của
nguồn. **Không bóc dữ liệu từ trang web (scrape HTML)** ở bất kỳ nguồn nào.

Mọi lượt gọi ra ngoài mang `User-Agent: LibraryConnect/1.0`.

---

## 1. Kết quả khảo sát

Đo ngày 01/09/2026, gọi bằng chính chức năng liên thư viện của hệ thống (Phân hệ Liên thư viện,
Phase 11) trừ những dòng ghi rõ là gọi trực tiếp để khảo sát.

### 1.1. Z39.50 và SRU

| Nguồn | Địa chỉ | Kết quả | Định dạng | Thời gian | Ghi chú |
|---|---|---|---|---|---|
| Thư viện Quốc hội Mỹ — Z39.50 | `lx2.loc.gov:210/LCDB` | **Kết nối được**, tra thử 949.569 kết quả | MARC21 | 1,8 s | Máy chủ tự khai `Metaproxy/YAZ` |
| Thư viện Quốc hội Mỹ — SRU | `lx2.loc.gov:443/lcdb` | **Kết nối được**, 949.569 kết quả | MARCXML | 0,8 s | Ổn định hơn Z39.50, xem lỗi A4 |
| Thư viện ĐH Yale | `z3950.library.yale.edu:7090/voyager` | **Không dùng được** — phía bên kia đóng kết nối | — | 12,9 s | Máy chủ ngừng phục vụ công khai |

### 1.2. OAI-PMH — nguồn Việt Nam

| Nguồn | Địa chỉ | Kết quả | Số biểu ghi | Định dạng công bố | Ghi chú |
|---|---|---|---|---|---|
| ĐH Thủy lợi | `tailieuso.tlu.edu.vn/oai/request` | **Kết nối được** | 10.069 | `oai_dc`, `marc`, `mods`, `qdc`, `dim`, `etdms`… | Đúng chuyên ngành tài nguyên nước – môi trường. TLS chập chờn, xem lỗi A6 |
| ĐH Kinh tế TP.HCM | `digital.lib.ueh.edu.vn/oai/request` | **Kết nối được** | 27.458 | như trên | Ổn định nhất trong các nguồn Việt Nam |
| ĐH Cần Thơ | `dspace.ctu.edu.vn/oai/request` | Kết nối được từ máy chủ ngoài, **thất bại TLS từ trong container** | 128.566 | như trên | Máy chủ không gửi chứng thư trung gian |
| Tạp chí KH Việt Nam Trực tuyến (VJOL) | `vjol.info.vn/index.php/index/oai` | **Kết nối được** | 222.362 | `oai_dc`, `marcxml`, `oai_marc`, `rfc1807` | Có sẵn MARCXML nhưng hệ thống chưa nhận tên định dạng này — lỗi A3 |
| ĐH Quốc gia Hà Nội | `repository.vnu.edu.vn/oai/request` | Không có (404) | — | — | Đã thử cả `/dspace-oai/request` |
| ĐH Đà Nẵng | `tailieuso.udn.vn/oai/request` | Không kết nối được | — | — | Từ chối kết nối |
| TT Học liệu Huế | `tailieuso.hueuni.edu.vn/oai/request` | Không phân giải được tên miền | — | — | |

### 1.3. OAI-PMH — nguồn quốc tế

| Nguồn | Địa chỉ | Kết quả | Ghi chú |
|---|---|---|---|
| DOAJ | `doaj.org/oai` | **Kết nối được** | Tạp chí truy cập mở, chủ yếu tiếng Anh |
| arXiv | `export.arxiv.org/oai2` | **Kết nối được** | Tiền ấn phẩm khoa học tự nhiên |
| Zenodo | `zenodo.org/oai2d` | **Kết nối được** | |
| CORE | `core.ac.uk/oai` | Không có (404) | Đã chuyển sang API có khóa |
| HathiTrust | `catalog.hathitrust.org/oai` | Từ chối (403) | Chỉ mở cho đối tác |
| OpenAIRE | `services.openaire.eu/search/oai2` | Lỗi máy chủ (500) | |

---

## 2. Giấy phép và điều kiện sử dụng

| Nguồn | Giấy phép dữ liệu thư mục | Dùng cho demo thương mại | Việc phải làm |
|---|---|---|---|
| Thư viện Quốc hội Mỹ | Biểu ghi thư mục do cơ quan liên bang Hoa Kỳ tạo — thuộc phạm vi công cộng, LoC công bố không giữ bản quyền phần metadata | **Được** | Ghi nguồn ở trường 040 |
| ĐH Thủy lợi, ĐH Kinh tế TP.HCM (DSpace) | Metadata công bố qua OAI-PMH để thu hoạch; giao thức OAI-PMH sinh ra chính là để cho phép việc này. Toàn văn tài liệu **không** lấy về, chỉ lấy phần mô tả | **Được với phần metadata** | Không tải toàn văn; ghi rõ nguồn trong biểu ghi; nêu tên kho khi trưng bày |
| VJOL | Như trên | **Được với phần metadata** | |
| DOAJ | Metadata theo CC0 (DOAJ công bố miễn trừ bản quyền metadata) | **Được** | |
| Open Library / Internet Archive | Dữ liệu theo CC0; ảnh bìa dùng lại được, nguồn khuyến nghị ghi công | **Được** | Ghi nguồn ảnh bìa |
| CORE, HathiTrust | Không khảo sát tiếp vì không truy cập được | — | Không lấy |

> Phần **toàn văn** của các kho DSpace Việt Nam có giấy phép riêng theo từng tài liệu và **không được
> lấy về** trong bản demo này. Hệ thống chỉ lấy metadata; muốn đọc toàn văn, bạn đọc bấm liên kết
> trở về kho gốc.

---

## 3. Cách hệ thống lấy dữ liệu

Toàn bộ đi qua đúng chức năng của sản phẩm, **không chèn thẳng vào cơ sở dữ liệu bằng SQL**:

1. **Liên thư viện → Kho OAI-PMH**: khai địa chỉ kho, định dạng `oai_dc`, mã bộ sưu tập, dạng tài
   liệu mặc định → bấm **Thu hoạch ngay**. Biểu ghi vào hàng đợi biên mục ở trạng thái *Chờ duyệt*
   đúng như thiết kế: Dublin Core nghèo hơn MARC nên cán bộ còn phải hiệu đính.
2. **Liên thư viện → Tra cứu**: tra sang Thư viện Quốc hội Mỹ theo chủ đề, chọn biểu ghi, bấm
   **Nhập vào hệ thống** → mở trình soạn MARC để hiệu đính rồi lưu.

Mỗi biểu ghi lấy về đều ghi lại nguồn ở cột `source` (`Oai` hoặc `Z3950`) và ở trường MARC `040$a`.

---

## 4. Kết quả nạp

Đo lúc kết thúc đợt nạp. Cột "lượt" là số lần phải chạy lại vì lỗi A2/A6 (thu hoạch bị ngắt giữa
chừng, phải bắt đầu lại từ đầu).

| Nguồn — bộ sưu tập | Lượt chạy | Lấy về | Nhập được | Nhật ký còn kẹt "Đang chạy" |
|---|---|---|---|---|
| ĐH Thủy lợi — Luận văn, luận án | 6 | 4.225 | 2.118 | 6 |
| ĐH Thủy lợi — Bài giảng điện tử | 4 | 3.161 | 1.444 | 3 |
| ĐH Thủy lợi — Tạp chí KH Thủy lợi và Môi trường | 4 | 768 | 679 | 3 |
| ĐH Thủy lợi — Sách điện tử | 1 | 482 | 444 | 0 |
| ĐH Kinh tế TP.HCM — Đề tài nghiên cứu | 3 | 3.619 | 1.780 | 3 |
| ĐH Kinh tế TP.HCM — Luận án tiến sĩ | 1 | 585 | 585 | 0 |
| ĐH Kinh tế TP.HCM — Kỷ yếu hội thảo | 1 | 407 | 407 | 0 |
| Thư viện Quốc hội Mỹ — tra cứu Z39.50 theo chủ đề | 1 | 1 | 1 | 0 |

**Tổng kho hiện có: 7.676 biểu ghi** (trước đợt nạp: 205).

Chênh lệch giữa "lấy về" và "nhập được" là biểu ghi trùng số kiểm soát với biểu ghi đã có — đúng
hành vi mong đợi khi một lượt thu hoạch bị ngắt rồi chạy lại từ đầu.

> **Ba lỗi chặn đường đã sửa.** Lúc mới nạp xong, cả 7.469 biểu ghi đứng ở trạng thái "Chờ biên
> mục": không lên trang tra cứu, không vào hàng đợi biên mục, và không mở ra sửa rồi lưu lại được vì
> thiếu trường 008 (lỗi **A7, A8, A9** trong `08-so-loi.md`). Sau khi sửa, cùng với migration dọn
> dữ liệu cũ và chức năng duyệt hàng loạt ở hàng đợi biên mục:
>
> - 7.468 biểu ghi vào hàng đợi biên mục, cán bộ nhìn thấy việc phải làm;
> - duyệt hàng loạt xong, **7.674 biểu ghi tra cứu được trên OPAC** (trước đợt nạp: 206);
> - tra cứu tiếng Việt không dấu chạy đúng trên dữ liệu thật: `thuy loi` ra 3.665 kết quả,
>   `tai nguyen nuoc` ra 414, `kinh te` ra 1.923.

---

## 5. Nhận xét về chức năng liên thư viện (Phase 11)

Đợt nạp dữ liệu này là bài kiểm chứng thật đầu tiên của Phân hệ Liên thư viện với nguồn bên ngoài.

**Chạy đúng:**
- Máy khách Z39.50 bắt tay được với Thư viện Quốc hội Mỹ, tra cứu và lấy biểu ghi MARC21 thật về.
- Lối SRU chạy ổn định hơn Z39.50 với cùng một thư viện.
- Bộ thu hoạch OAI-PMH đọc được cả bốn kho DSpace và OJS của Việt Nam, phân trang bằng
  `resumptionToken`, ánh xạ Dublin Core sang MARC21, giữ đúng dấu tiếng Việt.
- Biểu ghi lấy về giữ được dạng tài liệu, ngôn ngữ, năm xuất bản, ISBN và ghi rõ nguồn ở `040$a`.

**Lỗi phát hiện được** — ghi chi tiết trong `08-so-loi.md`:

| Mã lỗi | Tóm tắt | Mức độ |
|---|---|---|
| A1 | Tác giả viết không dấu tạo thêm bản ghi thẩm quyền trùng mã, làm đổ cả lượt nhập | Nghiêm trọng |
| A2 | Thu hoạch chạy đồng bộ trong lượt HTTP; ngắt kết nối là nhật ký kẹt "Đang chạy" | Nặng |
| A3 | Chỉ nhận `oai_dc` và `marc21`, không nhận `marc`/`marcxml`/`oai_marc` của kho thật | Vừa |
| A4 | Z39.50 báo có kết quả nhưng lấy 0 biểu ghi ở một số truy vấn | Vừa |
| A5 | Tên kho hiện ký tự thoát chưa giải mã | Nhẹ |
| A6 | Lỗi TLS giữa chừng làm hỏng cả lượt thu hoạch, không có điểm nối lại | Vừa |
| A7 | Biểu ghi thu hoạch về không vào hàng đợi biên mục — không ai biết có việc phải làm | Nghiêm trọng |
| A8 | Hoàn thành việc trong hàng đợi không đưa biểu ghi lên OPAC | Nghiêm trọng |
| A9 | Biểu ghi thu hoạch về thiếu trường 008, mở ra sửa rồi lưu lại thì bị chặn | Nặng |

**Đã sửa trong đợt này:** A1, A2, A3, A5, A7, A8, A9. Còn lại A4 (Z39.50 lấy 0 biểu ghi ở một số
truy vấn) và A6 (thu hoạch không có điểm nối lại khi đứt giữa chừng).

---

## Open Library — nguồn sách có ảnh bìa

**Vì sao cần thêm nguồn này.** Kho thu hoạch bằng OAI-PMH từ các kho số đại học Việt Nam lệch hẳn về
tài liệu xám: luận văn, luận án, đề tài nghiên cứu và bài giảng điện tử chiếm **93%**. Loại tài liệu
ấy **không tồn tại ảnh bìa ở bất kỳ nguồn nào** — chúng không có ISBN, không nhà xuất bản nào phát
hành, không hiệu sách nào bán. Đo thật: tra cả 299 cuốn Sách và Giáo trình có ISBN trong kho, không
ra được **một** ảnh nào.

| Hạng mục | Nội dung |
|---|---|
| Giao thức | Search API (`openlibrary.org/search.json`) — API mở, không cần khóa |
| Giấy phép dữ liệu | **CC0** — hiến tặng vào phạm vi công cộng, dùng lại không cần xin phép |
| Ảnh bìa | Covers API, lấy theo **mã ảnh** (`cover_i`) — lối này không giới hạn tần suất; lối theo ISBN bị chặn 100 lượt mỗi 5 phút |
| Cách nạp | Theo 24 chủ đề của ngành tài nguyên – môi trường, chia đều hạn mức cho từng chủ đề |
| Điều kiện nhận | **Chỉ nhận biểu ghi có ảnh bìa** — mục đích của lượt nạp là cân bằng lại phần sách có bìa thật |
| Ảnh lưu ở đâu | Tải về MinIO của mình, không dẫn thẳng sang máy chủ nguồn |
| Trạng thái biểu ghi | Công bố ngay, không qua hàng đợi hiệu đính |
| Ghi nguồn | `035$a = (openlibrary.org)works/OL…`, `040$a = openlibrary.org`, `856$u` trỏ về trang gốc |

**Vì sao công bố ngay mà biểu ghi OAI-PMH thì phải hiệu đính.** Dublin Core chỉ có mười lăm phần tử
nên biểu ghi dựng từ nó còn nghèo — thiếu nhà xuất bản, thiếu số trang, đề mục lẫn lộn với từ khóa.
Dữ liệu Open Library có đủ nhan đề, tác giả, năm, ISBN, nhà xuất bản, đề mục chủ đề, ngôn ngữ và số
trang, nên biểu ghi dựng ra đã đủ chuẩn để lên trang tra cứu.

**Chủ đề đã nạp:** hydrology, water resources, water supply, irrigation, hydraulic engineering,
environmental science, environmental engineering, environmental protection, geology, meteorology,
climatology, climatic changes, soil science, ecology, oceanography, remote sensing, geographic
information systems, natural resources, land use, water quality, air pollution, waste management,
renewable energy, sustainable development.

**Đổi được:** danh sách chủ đề truyền qua tham số của endpoint; bỏ trống thì dùng bộ mặc định trên.
Chạy bằng nút **Nạp sách từ Open Library** ở màn hình Liên thư viện → Kho OAI-PMH.

---

## Bộ dữ liệu trình diễn lớn — `LC_SEED_DEMO=rich`

Dữ liệu **thư mục** lấy từ nguồn thật qua đúng giao thức chuẩn, như mô tả ở trên. Dữ liệu **nghiệp
vụ** thì tự sinh, vì không nguồn nào công bố lượt mượn trả hay hồ sơ bạn đọc thật — mà có công bố
cũng không dùng được, đó là dữ liệu cá nhân.

Nạp thật trên máy phát triển ngày 02/09/2026:

| Hạng mục | Số lượng | Cách sinh |
|---|---|---|
| Biểu ghi thư mục | 7.675 | Thu hoạch thật qua OAI-PMH và Z39.50 |
| Biểu ghi có bản in | 4.700 (61%) | Sinh ĐKCB cho 60% biểu ghi, chọn cố định theo số kiểm soát |
| Đăng ký cá biệt | 9.502 | Qua đúng `ItemCreator` mà cán bộ bổ sung dùng |
| Bạn đọc | 351 | Tên Việt ghép từ ba danh sách, phân bổ theo khoa và ngành |
| Lượt mượn trả | 1.603 | Trải 18 tháng: đã trả đúng hạn, trả muộn có phạt, đang mượn, quá hạn |
| Khoản phạt | 254 | Tính bằng chính `CirculationRules` mà quầy dùng |
| Đặt giữ chỗ | 58 | Đủ bốn trạng thái: chờ, sẵn sàng, đã nhận, hết hạn |
| ĐKCB mất / hỏng / thanh lý | 37 / 37 / 37 | Để báo cáo kho và màn hình thanh lý có việc thật |

**Chạy lại được:** đặt `LC_SEED_DEMO=rich` rồi khởi động lại máy chủ. Bộ này khác bộ mặc định ở chỗ
nó chạy được trên kho **đã có dữ liệu** — nó sinh ĐKCB cho chính biểu ghi thu hoạch thật chứ không
dựng biểu ghi giả. Chỉ chạy đúng một lần, đánh dấu bằng tham số `SYS.DEMO_RICH_APPLIED`.

## Ảnh bìa — kết quả đo sau khi nạp Open Library

| Số đo | Giá trị |
|---|---|
| Tổng biểu ghi | **11.675** |
| Có ảnh bìa thật | **3.972 (34,0%)** |
| Dùng bìa máy chủ dựng sẵn | 7.703 (66,0%) |
| Biểu ghi có bản in trong kho | 8.900 (76,2%) |
| Đăng ký cá biệt | 17.902 |
| Lượt mượn trả | 3.103, trong đó 621 đang mượn |

Theo nguồn:

| Nguồn | Biểu ghi | Có ảnh thật |
|---|---|---|
| Open Library | 4.000 | **3.968 (99,2%)** |
| OAI-PMH (bốn kho số đại học Việt Nam) | 7.466 | 1 |
| Nhập tay / bộ dữ liệu mẫu | 208 | 3 |
| Z39.50 | 1 | 0 |

Con số ấy nói đúng một điều: **ảnh bìa đi kèm nguồn, không đi kèm công sức tra cứu.** Nạp từ nguồn
có bìa thì 99,2% có bìa; tra ảnh cho tài liệu nội sinh Việt Nam thì 1 trên 7.466.

## Ảnh bìa — kết quả tra thật

| Số đo | Giá trị |
|---|---|
| Biểu ghi có ISBN | **444 / 7.675 (5,8%)** |
| Tra được ảnh thật | **3** (đều từ Open Library) |
| Google Books | **0** — trả HTTP 429 "Quota exceeded" vì gọi không kèm khóa API |
| Trường 856 chứa địa chỉ ảnh | 0 |
| Dùng bìa máy chủ dựng sẵn | **7.672 (99,96%)** |

Con số này trả lời dứt khoát câu hỏi có đáng đầu tư thêm vào tra ảnh thật hay không: **không**. Kho
thư viện đại học Việt Nam phần lớn là luận văn, đề tài nghiên cứu và bài giảng điện tử — không có
ISBN, nên không nguồn nào tra ra ảnh. Phần đầu tư nằm ở bìa dựng sẵn, và nó được làm cho tử tế: màu
theo dạng tài liệu, nhan đề ngắt dòng ở chỗ giáp từ, có tác giả và năm, tỉ lệ 2:3 đúng bìa sách.

Thư viện nào muốn lớp Google Books chạy thật thì khai khóa API riêng ở **Tham số hệ thống → Cấu hình
biên mục → Khóa API Google Books**. Khóa miễn phí, lấy ở console.cloud.google.com và bật Books API.

