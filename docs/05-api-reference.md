# Tài liệu API — LibraryConnect

Toàn bộ nghiệp vụ của hệ thống nằm ở tầng API. Giao diện quản trị, trang tra cứu và ứng dụng di động
đợt sau đều là các máy khách gọi vào cùng một bộ endpoint này — không có chức năng nào chỉ tồn tại
trong giao diện.

Tài liệu này mô tả cách gọi chung, các nhóm endpoint, ba giao thức liên thư viện, và một chương riêng
dành cho người viết ứng dụng khách.

Bản đặc tả máy đọc được (OpenAPI 3) luôn đi kèm hệ thống đang chạy:

| | Địa chỉ |
|---|---|
| Giao diện thử API | `http://<máy-chủ>/swagger` |
| Tệp đặc tả JSON | `http://<máy-chủ>/swagger/v1/swagger.json` |

Tệp JSON nạp được thẳng vào Postman, Insomnia hay các công cụ sinh mã máy khách.

> Trên máy chủ chạy thật, Swagger tắt theo mặc định và chỉ mở cho dải mạng nội bộ. Bật bằng biến
> `LC_Swagger__Enabled=true` khi cần bàn giao cho đối tác tích hợp.

---

## 1. Quy ước chung

### 1.1. Địa chỉ gốc

```
http://<máy-chủ>/api/...
```

Nginx đứng trước và chuyển tiếp mọi đường dẫn bắt đầu bằng `/api` tới dịch vụ API. Trên máy chủ chạy
thật, dùng `https://`.

### 1.2. Khuôn dạng trả về

Mọi endpoint trả về cùng một khuôn:

```json
{
  "success": true,
  "data": { },
  "message": "",
  "errors": []
}
```

Khi có lỗi:

```json
{
  "success": false,
  "message": "Dữ liệu không hợp lệ.",
  "errors": [
    { "field": "cardNumber", "message": "Số thẻ không được để trống." }
  ]
}
```

Dữ liệu phân trang nằm trong `data` theo dạng:

```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

Máy khách chỉ cần đọc `success` để biết thành công hay không, `errors[].field` để tô đỏ đúng ô nhập,
và `message` để hiện thông báo — mọi thông báo đều đã bằng tiếng Việt.

### 1.3. Mã trạng thái HTTP

| Mã | Nghĩa |
|---|---|
| 200 | Thành công |
| 400 | Dữ liệu gửi lên không hợp lệ; xem `errors` |
| 401 | Chưa đăng nhập hoặc mã truy cập hết hạn |
| 403 | Đã đăng nhập nhưng không đủ quyền |
| 404 | Không tìm thấy dữ liệu |
| 409 | Xung đột nghiệp vụ: trùng mã, sách đang được mượn, yêu cầu đã tồn tại |
| 429 | Gọi quá nhanh, bị chặn tần suất |
| 500 | Lỗi máy chủ; hệ thống ghi lại kèm mã truy vết |

### 1.4. Xác thực

Hệ thống dùng JWT, **không dùng phiên trên máy chủ** — nên cùng một bộ endpoint phục vụ được cả trình
duyệt lẫn ứng dụng di động.

Có hai đường đăng nhập, cấp hai loại danh tính khác nhau:

| Đường | Dành cho | Endpoint |
|---|---|---|
| Cán bộ | Người dùng nội bộ, có nhóm quyền | `POST /api/auth/login` |
| Bạn đọc | Chủ thẻ thư viện | `POST /api/reader/auth/login` |

```http
POST /api/reader/auth/login
Content-Type: application/json

{ "cardNumber": "TV2026000001", "password": "MatKhau@2025" }
```

```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "b2f1c0d4...",
    "accessTokenExpiresAt": "2026-09-01T04:12:00+07:00",
    "refreshTokenExpiresAt": "2026-10-01T03:12:00+07:00",
    "mustChangePassword": false,
    "user": {
      "id": "6f1c…",
      "username": "TV2026000001",
      "fullName": "Nguyễn Thị Minh An",
      "email": "an.nguyen@example.edu.vn",
      "isReader": true,
      "groups": [],
      "permissions": [],
      "dataScopes": []
    }
  }
}
```

`user.isReader` phân biệt hai loại danh tính: `true` là bạn đọc, `false` là cán bộ. Với cán bộ,
`permissions` liệt kê đủ mã quyền để giao diện ẩn hiện menu — nhưng máy chủ vẫn kiểm tra lại độc lập
ở từng lượt gọi.

Gửi mã truy cập kèm mọi yêu cầu sau đó:

```
Authorization: Bearer eyJhbGciOi...
```

Mã truy cập sống 60 phút (đổi được bằng `LC_Jwt__AccessTokenMinutes`). Khi hết hạn, máy chủ trả 401;
máy khách gọi `POST /api/reader/auth/refresh` với `refreshToken` để lấy cặp mã mới rồi gửi lại yêu
cầu vừa hỏng. Mã làm mới sống 30 ngày.

> Máy khách nên gom việc làm mới vào một chỗ và **chỉ chạy một lượt làm mới tại một thời điểm**: khi
> mở màn hình có năm truy vấn song song, cả năm cùng nhận 401, và nếu mỗi truy vấn tự làm mới thì bốn
> mã làm mới sau sẽ bị vô hiệu hóa.

### 1.5. Phân quyền

Mỗi endpoint của cán bộ đòi một mã quyền dạng `PHÂNHỆ.ĐỐITƯỢNG.HÀNHĐỘNG` — ví dụ
`CATALOG.BIB.CREATE`, `CIRCULATION.LOAN.RETURN`, `COURSE.DOCUMENT.LINK`. Quyền cấp cho nhóm; người
dùng nhận quyền qua nhóm.

Ngoài quyền chức năng còn có **phạm vi dữ liệu**: cán bộ chỉ được thao tác trên kho và cơ sở được
gán. Giới hạn này áp ngay ở tầng truy vấn, nên gọi thẳng API cũng không vượt qua được.

Thiếu quyền chức năng trả **403**; thiếu phạm vi dữ liệu trả **404** cho bản ghi ngoài phạm vi — đúng
với nguyên tắc không tiết lộ sự tồn tại của dữ liệu người gọi không được biết.

### 1.6. Phân trang, sắp xếp và lọc

Danh sách nhận `page` (bắt đầu từ 1), `pageSize` (mặc định 20, tối đa 500), `keyword`, và tùy màn
hình có thêm `sort`. Bộ lọc phức tạp gửi bằng `POST .../search` với thân JSON, vì địa chỉ URL không
chứa nổi một biểu thức lọc nhiều tầng.

Toàn bộ phân trang tính ở máy chủ. Không endpoint nào trả về cả kho dữ liệu.

### 1.7. Chặn tần suất

| Nhóm | Giới hạn mặc định |
|---|---|
| Đăng nhập | 20 lượt/phút cho một địa chỉ IP (`LC_RateLimit__LoginPerMinute`) |
| Endpoint công khai | 300 lượt/phút (`LC_RateLimit__PublicPerMinute`) |

Bị chặn thì trả 429 kèm đầu đề `Retry-After` ghi số giây phải chờ, và thân trả về câu thông báo tiếng
Việt như mọi lỗi khác. Máy khách nên chờ đúng khoảng đó rồi thử lại — thử lại ngay chỉ làm cửa sổ
chặn kéo dài.

Ngoài chặn theo địa chỉ IP, tài khoản còn bị khóa tạm sau nhiều lần sai mật khẩu liên tiếp; số lần và
thời gian khóa khai trong Tham số hệ thống.

### 1.8. Ngày giờ và ký tự

- Thời điểm trả về theo ISO 8601 có múi giờ: `2026-09-01T09:15:00+07:00`.
- Ngày không kèm giờ (hạn trả, ngày xuất bản) theo dạng `2026-09-01`.
- Toàn bộ chuỗi mã hóa UTF-8. Máy khách phải gửi `Content-Type: application/json; charset=utf-8`.

---

## 2. Bản đồ các nhóm endpoint

Hệ thống có **485 endpoint** trên 378 đường dẫn, chia thành các nhóm sau (tên nhóm trùng với tên thẻ
trong Swagger).

| Nhóm | Tiền tố | Dùng cho |
|---|---|---|
| Xác thực | `/api/auth/*` | Đăng nhập cán bộ, làm mới mã, đổi mật khẩu |
| Quản trị hệ thống | `/api/admin/*` | Nhóm người dùng, người dùng, tham số, nhật ký, sao lưu |
| Danh mục | `/api/catalogs/*` | Hơn 20 bảng danh mục dùng chung một bộ endpoint |
| MARC 21 | `/api/marc/*` | Định nghĩa trường, mẫu biên mục, kiểm tra biểu ghi |
| Biên mục | `/api/cataloging/*` | Biểu ghi, ĐKCB, lịch sử phiên bản, hàng đợi, nhập/xuất, phích |
| Bổ sung | `/api/acquisition/*` | Yêu cầu mua, đơn đặt, nhận hàng, biên bản bàn giao |
| Kho và giá | `/api/locations/*` | Thư viện, kho, giá |
| Kho ấn phẩm | `/api/stock/*` | ĐKCB: kiểm nhận, khóa, chuyển kho, thanh lý, in tem |
| Kiểm kê | `/api/inventory/*` | Kỳ kiểm kê, quét, kết quả |
| Ấn phẩm định kỳ | `/api/serials/*` | Đầu báo, sinh số, ghi nhận, khiếu nại, đóng tập, bài trích |
| Tài liệu số | `/api/digital/*` | Bộ sưu tập, tải tệp, duyệt yêu cầu đọc, báo cáo |
| Bạn đọc | `/api/readers/*` | Hồ sơ, thẻ, in thẻ, nhập xuất, báo cáo |
| Lưu thông | `/api/circulation/*` | Chính sách, quầy mượn trả, đặt giữ, tiền phạt, tủ đồ, báo cáo |
| Quản trị nội dung | `/api/content/*` | Cấu hình trang, trang tĩnh, tin tức, menu, banner, nhận xét |
| Tài liệu môn học | `/api/courses/*` | Môn học, gán tài liệu, báo cáo đáp ứng |
| Liên thư viện | `/api/interlibrary/*` | Máy chủ Z39.50, thu hoạch OAI-PMH, nhập biểu ghi |
| Tra cứu | `/api/search/*`, `/api/browse/*`, `/api/bib/*` | Dùng chung cho trang tra cứu và ứng dụng khách |
| Công khai | `/api/public/*` | Tin tức, trang tĩnh, thông tin thư viện — không cần đăng nhập |
| Bạn đọc — ứng dụng khách | `/api/reader/*` | Xem chương 4 |

---

## 3. Giao thức liên thư viện

Ba lối này **không nằm dưới `/api`** vì chúng theo chuẩn quốc tế, và phần mềm thư viện khác gọi vào
theo đúng địa chỉ chuẩn.

### 3.1. Z39.50 (ISO 23950)

| | |
|---|---|
| Cổng | 210 (TCP) |
| Tên cơ sở dữ liệu | `LibraryConnect` (đổi được bằng tham số hệ thống) |
| Cú pháp biểu ghi | USMARC / MARC21 |
| PDU hỗ trợ | Init, Search, Present, Close |
| Bộ thuộc tính | Bib-1 (1 = tên tác giả, 4 = nhan đề, 7 = ISBN, 8 = ISSN, 21 = chủ đề, 1016 = bất kỳ) |

Thử bằng công cụ `yaz-client`:

```bash
yaz-client tcp:thuvien.tentruong.edu.vn:210/LibraryConnect
Z> find @attr 1=4 "cơ sở dữ liệu"
Z> show 1
```

Bật/tắt và giới hạn dải IP trong Tham số hệ thống, nhóm Liên thư viện.

### 3.2. SRU 1.2 — bản chạy trên HTTP của Z39.50

```
GET /sru?operation=searchRetrieve&version=1.2&query=<truy vấn CQL>&recordSchema=marcxml
        &startRecord=1&maximumRecords=10
```

Ví dụ:

```
/sru?operation=explain&version=1.2
/sru?operation=searchRetrieve&version=1.2&query=dc.title=%22cơ%20sở%20dữ%20liệu%22
/sru?operation=searchRetrieve&version=1.2&query=bath.isbn=9786041000001&recordSchema=marcxml
```

Chỉ mục CQL hỗ trợ — đúng danh sách mà `operation=explain` công bố: `dc.title`, `dc.creator`,
`dc.subject`, `dc.publisher`, `dc.date`, `bath.isbn`, `bath.issn`, `cql.serverChoice`.
Trả về XML theo lược đồ SRU, biểu ghi theo `http://www.loc.gov/MARC21/slim`.

Đây là lối được khuyến nghị cho bên tích hợp mới: dùng HTTP nên đi qua tường lửa và proxy như mọi
dịch vụ web khác.

### 3.3. OAI-PMH 2.0 — thu hoạch metadata

```
GET /oai?verb=Identify
GET /oai?verb=ListMetadataFormats
GET /oai?verb=ListSets
GET /oai?verb=ListIdentifiers&metadataPrefix=oai_dc&from=2026-01-01
GET /oai?verb=ListRecords&metadataPrefix=marc21&set=doctype:LUANVAN
GET /oai?verb=GetRecord&metadataPrefix=oai_dc&identifier=oai:thuvien.tentruong.edu.vn:0000000001
```

Đủ sáu verb của chuẩn. Hai lược đồ metadata: `oai_dc` (Dublin Core) và `marc21`. Bộ (set) chia theo
dạng tài liệu, mã bộ dạng `doctype:MÃ`.

Định danh biểu ghi theo dạng chuẩn `oai:<tên-miền-máy-chủ>:<số kiểm soát>`.

Danh sách dài được cắt trang bằng `resumptionToken`; lượt sau chỉ gửi lại token, không gửi lại điều
kiện lọc — đúng như chuẩn quy định.

### 3.4. Sơ đồ trang cho máy tìm kiếm

`/sitemap.xml` và `/robots.txt` do máy chủ sinh động, liệt kê trang tĩnh, bản tin và tài liệu đã xuất
bản; khu quản trị và các trang cá nhân của bạn đọc bị chặn thu thập.

---

## 4. API cho ứng dụng khách

> Chương này là **hợp đồng giữa máy chủ và ứng dụng di động** sẽ phát triển ở đợt sau (Phân hệ XI).
> Toàn bộ endpoint dưới đây đã hoàn thành, có kiểm thử tích hợp và đang được trang tra cứu OPAC dùng
> thật hằng ngày — nên người viết ứng dụng chỉ việc gọi, không phải chờ máy chủ bổ sung gì thêm.

### 4.1. Nguyên tắc

- Chỉ dùng REST + JSON + JWT. Không có trạng thái phiên nào trên máy chủ.
- **Mọi tính toán nghiệp vụ ở máy chủ.** Ứng dụng không tự tính hạn trả, tiền phạt, số lượt gia hạn
  còn lại hay hạn mức mượn — gọi API và hiển thị con số trả về.
- Một phần dữ liệu đã kèm sẵn chữ tiếng Việt: `relationLabel` của tài liệu môn học, `reason` của
  quyền đọc tài liệu số, `message` của mọi lỗi. Nhưng **các trường trạng thái trả về tên hằng số
  tiếng Anh** — `Active`, `Overdue`, `Returned`, `Waiting`, `Ready` — nên ứng dụng phải có một bảng
  đối chiếu sang tiếng Việt, giống bảng ở mục 4.5. Đừng hiện thẳng tên hằng số cho bạn đọc.
- Cùng bộ endpoint này phục vụ cả web, nên lỗi phát hiện được sớm.

### 4.2. Tài khoản và thẻ

| Method | Đường dẫn | Chức năng | Màn hình |
|---|---|---|---|
| POST | `/api/reader/auth/login` | Đăng nhập bằng số thẻ và mật khẩu | Đăng nhập |
| POST | `/api/reader/auth/refresh` | Cấp lại mã truy cập | Chạy nền |
| POST | `/api/reader/auth/change-password` | Đổi mật khẩu | Tài khoản |
| GET | `/api/reader/profile` | Hồ sơ bạn đọc đang đăng nhập | Tài khoản |
| PUT | `/api/reader/profile` | Cập nhật thông tin liên hệ | Tài khoản |
| GET | `/api/reader/card` | Thẻ điện tử: số thẻ, hạn thẻ, chuỗi sinh mã vạch/QR | Thẻ thư viện |
| POST | `/api/reader/card/renew-request` | Gửi yêu cầu gia hạn thẻ | Tài khoản |
| GET | `/api/reader/card/renew-requests` | Trạng thái các yêu cầu đã gửi | Tài khoản |

Bạn đọc chỉ sửa được email, điện thoại và địa chỉ. Họ tên, mã sinh viên, khoa, ngành do nhà trường
quản lý — gửi lên cũng bị bỏ qua.

### 4.3. Tra cứu

| Method | Đường dẫn | Chức năng | Màn hình |
|---|---|---|---|
| GET | `/api/search` | Tra cứu cơ bản: `keyword`, `scope`, `sort`, phân trang, bộ lọc | Tra cứu |
| POST | `/api/search/advanced` | Tra cứu nâng cao, nhiều điều kiện VÀ/HOẶC/KHÔNG | Tra cứu nâng cao |
| GET | `/api/search/suggest` | Gợi ý khi gõ (`term`, `limit`) | Tra cứu |
| GET | `/api/search/facets` | Bộ đếm cho các bộ lọc | Tra cứu |
| GET | `/api/search/by-isbn/{isbn}` | Tra theo ISBN | Quét mã |
| GET | `/api/search/by-barcode/{barcode}` | Tra ĐKCB theo mã vạch | Quét mã vạch/QR |
| GET | `/api/bib/{id}` | Chi tiết tài liệu + danh sách bản in, tình trạng, vị trí kho | Chi tiết sách |
| GET | `/api/bib/{id}/citation` | Trích dẫn theo APA, MLA, Chicago, BibTeX, RIS, EndNote | Chi tiết sách |

Tham số `scope` của tra cứu cơ bản: `all`, `title`, `author`, `subject`, `keyword`, `publisher`,
`isbn`, `callNumber`.

Toàn bộ tra cứu **hoạt động cả khi gõ không dấu**. Ứng dụng không cần tự bỏ dấu trước khi gửi.

Hai endpoint quét mã dành riêng cho màn hình quét: đưa thẳng chuỗi máy quét đọc được lên, máy chủ trả
về tài liệu tương ứng hoặc 404.

### 4.4. Duyệt danh mục

| Method | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/api/browse/subjects` | Duyệt theo chủ đề (cây, truyền `parentId` để mở nhánh) |
| GET | `/api/browse/authors` | Duyệt theo tác giả, lọc theo chữ cái đầu (`letter`) |
| GET | `/api/browse/classifications` | Duyệt theo khung phân loại DDC |
| GET | `/api/browse/collections` | Duyệt theo bộ sưu tập |
| GET | `/api/browse/majors` | Danh sách ngành đào tạo |
| GET | `/api/browse/courses` | Môn học, truyền `majorId` để lọc theo ngành |
| GET | `/api/browse/majors/{majorId}/courses/{courseId}/documents` | Tài liệu của một môn học |
| GET | `/api/browse/theses` | Danh mục luận văn – luận án |
| GET | `/api/browse/serials` | Danh mục ấn phẩm định kỳ kèm tình trạng nhận số |

Số đếm ở nhánh cha đã cộng dồn cả nhánh con, nên hiển thị thẳng con số trả về.

### 4.5. Mượn trả

| Method | Đường dẫn | Chức năng | Màn hình |
|---|---|---|---|
| GET | `/api/reader/loans/current` | Sách đang mượn kèm hạn trả và số lượt gia hạn còn lại | Sách của tôi |
| GET | `/api/reader/loans/history` | Lịch sử mượn trả | Lịch sử |
| POST | `/api/reader/loans/{id}/renew` | Gia hạn một cuốn | Sách của tôi |
| POST | `/api/reader/loans/self-checkout` | Mượn tự phục vụ bằng mã vạch | Tự mượn |
| GET | `/api/reader/holds` | Danh sách đặt giữ kèm vị trí trong hàng đợi | Đặt giữ |
| POST | `/api/reader/holds` | Đặt giữ một tài liệu | Chi tiết sách |
| DELETE | `/api/reader/holds/{id}` | Hủy đặt giữ của chính mình | Đặt giữ |
| GET | `/api/reader/fines` | Tiền phạt và tình trạng thanh toán | Tài khoản |

Mỗi dòng sách đang mượn có `renewedCount` và `maxRenewals`; còn lượt gia hạn hay không thì so hai số
đó. **Nhưng đừng dựa vào đó để quyết định** — gia hạn còn bị chặn vì có người đặt giữ, vì sách đã quá
hạn, hoặc vì thẻ sắp hết hạn. Cứ gọi gia hạn và xử lý câu trả lời: máy chủ trả 409 kèm `message`
giải thích bằng tiếng Việt, hiện thẳng câu đó cho bạn đọc.

Trạng thái lượt mượn (`status`) nhận một trong: `Active` (đang mượn), `Returned` (đã trả), `Overdue`
(quá hạn), `Lost` (mất), `Damaged` (hỏng). Trạng thái đặt giữ (`status` của hold): `Waiting` (đang
chờ), `Ready` (sách đã sẵn sàng), `Fulfilled` (đã nhận), `Expired` (hết hạn giữ), `Cancelled` (đã hủy).

**Mượn tự phục vụ** nhận thân yêu cầu gồm `barcodes` — mảng mã vạch, quét được nhiều cuốn một lượt —
và `locationToken` là nội dung mã QR dán tại kho:

```json
{ "barcodes": ["LC000123", "LC000124"], "locationToken": "KHOMO-TANG2" }
```

Mã điểm mượn là cách xác thực bạn đọc đang đứng trong thư viện. Thư viện chưa dán mã QR thì để trống
tham số khai báo trong hệ thống, khi đó máy chủ bỏ qua bước kiểm tra vị trí.

**Xác thực vị trí (Phase 15).** Ứng dụng di động đi hai bước, và máy chủ giữ toàn bộ quy tắc:

| Method | Đường dẫn | Chức năng |
|---|---|---|
| POST | `/api/reader/loans/self-checkout/verify` | Nộp thứ điện thoại thấy — `ssid` (tên Wi-Fi) hoặc `qrContent` (mã QR trạm vừa quét) — nhận về `verificationToken` có hạn |
| POST | `/api/reader/loans/self-checkout` | Nộp `barcodes` kèm `verificationToken` |

Cách kiểm do tham số `CIRCULATION.SELF_CHECKOUT_VERIFY_MODE` quyết: `NONE` (không kiểm), `WIFI_SSID`
(SSID phải nằm trong `MOBILE.SELF_CHECKOUT_WIFI_SSID`), `QR_STATION` (mã QR phải là của một trạm đang
hoạt động trong `Lưu thông → Trạm mượn`, ký bằng khoá của thư viện). Phiếu sống
`CIRCULATION.SELF_CHECKOUT_QR_TTL_MINUTES` phút (mặc định 15). Không đạt thì 409 với mã trong
`errors[0].code` để ứng dụng hiện đúng màn hình:

| Mã | Nghĩa |
|---|---|
| `SELF_CHECKOUT_DISABLED` | Thư viện chưa mở mượn tự phục vụ |
| `LOCATION_REQUIRED` | Chưa xác thực vị trí (thiếu SSID / chưa quét QR / thiếu phiếu) |
| `WIFI_MISMATCH` | Không phải Wi-Fi của thư viện |
| `STATION_UNKNOWN` | Mã QR không phải trạm của thư viện này, hoặc chữ ký sai |
| `STATION_INACTIVE` | Trạm đang tạm ngừng |
| `LOCATION_INVALID` | Phiếu bị sửa, sai chế độ hoặc của người khác |
| `LOCATION_EXPIRED` | Phiếu quá hạn — quét lại |

```json
{ "ssid": "LC-Thu-Vien" }
→ { "mode": "WIFI_SSID", "verificationToken": "eyJ…​.k3Q", "expiresAt": "2026-09-03T08:15:00+07:00" }
{ "barcodes": ["LC000123"], "verificationToken": "eyJ….k3Q" }
```

Phiếu mượn ghi rõ đã xác thực ở đâu (`note`: "Mượn tự phục vụ · xác thực tại KHOMO-01"), loại
`SelfCheckout` và kênh `Mobile` — phân biệt được với mượn tại quầy trong mọi báo cáo.

### 4.6. Tài liệu số

| Method | Đường dẫn | Chức năng | Màn hình |
|---|---|---|---|
| GET | `/api/reader/digital` | Danh sách tài liệu số xem được (`keyword`, `collectionId`, phân trang) | Tài liệu số |
| POST | `/api/reader/digital/search` | Cùng danh sách nhưng đủ bộ lọc: định dạng, khoảng ngày, tìm toàn văn | Tài liệu số |
| GET | `/api/reader/digital/{id}` | Chi tiết kèm quyền đọc của chính bạn đọc | Chi tiết |
| GET | `/api/reader/digital/{id}/read` | Mở phiên đọc trực tuyến | Trình đọc |
| GET | `/api/reader/digital/{id}/pages/{page}` | Một trang dạng ảnh đã đóng chữ chìm | Trình đọc |
| GET | `/api/reader/digital/{id}/download` | Tải tệp gốc, nếu được phép | Chi tiết |
| POST | `/api/reader/digital/{id}/request` | Gửi yêu cầu đọc tài liệu hạn chế | Chi tiết |
| GET | `/api/reader/digital/requests` | Trạng thái các yêu cầu đã gửi | Tài liệu số |
| GET | `/api/reader/digital/history` | Lịch sử xem và tải | Lịch sử |
| POST | `/api/reader/digital/{id}/offline-package` | Xin gói đọc ngoại tuyến: khoá AES-256-CBC, IV, hạn dùng, địa chỉ tệp đã mã hoá *(Phase 15)* | Tài liệu số |
| GET | `/api/reader/digital/offline-packages` | Các gói đã cấp, kèm hết hạn / thu hồi | Quản lý tải về |
| GET | `/api/reader/digital/offline-packages/{packageId}/file` | Tệp đã mã hoá của gói (chỉ chủ gói, chưa hết hạn) | Nền |

Đối tượng `permission` trong phần chi tiết cho ứng dụng biết chính xác phải vẽ gì:

```json
{
  "canRead": true,
  "canDownload": false,
  "canPrint": false,
  "readablePages": 10,
  "needsRequest": true,
  "requestStatus": "Pending",
  "accessExpireAt": null,
  "reason": "Tài liệu hạn chế, cần được duyệt trước khi đọc toàn văn"
}
```

`readablePages` bằng `null` nghĩa là đọc được toàn bộ. `requestStatus` nhận `Pending`, `Approved`,
`Rejected` hoặc `Expired`, và bằng `null` khi bạn đọc chưa gửi yêu cầu nào.

Ứng dụng không tự suy ra quyền từ mức truy cập của tài liệu — luôn đọc `permission`. Chính máy chủ
cũng kiểm lại bộ quy tắc này ở từng endpoint đọc và tải, nên sửa dữ liệu ở máy khách không mở thêm
được quyền gì.

**Đọc ngoại tuyến (Phase 15).** Gói chỉ cấp khi `permission.canDownload` — tài liệu cho tải về hoặc
yêu cầu được duyệt kèm quyền tải; tài liệu chỉ đọc trực tuyến trả 403 *"Tài liệu này chỉ đọc trực
tuyến, thư viện không cho tải về máy."* Mỗi gói một khoá riêng; tệp tải về là bản mã hoá
(AES-256-CBC, PKCS7), ứng dụng cất khoá trong kho bảo mật của hệ điều hành và giải mã khi đọc.
Hạn dùng theo `DIGITAL.OFFLINE_PACKAGE_DAYS` (mặc định 7 ngày), không dài hơn hạn của quyền đọc đã
duyệt. Lượt cấp ghi vào lịch sử với hành động `OfflineDownload`.

```json
{
  "packageId": "…", "documentId": "…", "title": "Giáo trình Cơ sở dữ liệu",
  "fileName": "csdl.pdf", "mimeType": "application/pdf", "sizeBytes": 812345,
  "checksum": "<sha256 của tệp gốc>", "algorithm": "AES-256-CBC",
  "keyBase64": "…", "ivBase64": "…", "expiresAt": "2026-09-10T00:00:00+07:00",
  "downloadUrl": "/api/reader/digital/offline-packages/<packageId>/file"
}
```

### 4.7. Thông báo và thiết bị

| Method | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/api/reader/notifications` | Danh sách thông báo |
| POST | `/api/reader/notifications/{id}/read` | Đánh dấu một thông báo đã đọc |
| POST | `/api/reader/notifications/read-all` | Đánh dấu tất cả đã đọc |
| POST | `/api/reader/devices` | Đăng ký thiết bị nhận thông báo đẩy (`token`, `platform`, `deviceName`, `appVersion`) |
| DELETE | `/api/reader/devices?token=` | Gỡ đăng ký khi đăng xuất |
| GET | `/api/reader/notifications/settings` | Bạn đọc đang bật loại thông báo nào *(Phase 15)* |
| PUT | `/api/reader/notifications/settings` | Bật/tắt từng loại: `{ "settings": { "NEWS": false } }` |

**Thông báo đẩy (Phase 15).** Máy chủ gửi qua Firebase Cloud Messaging (API HTTP v1) khi khai
`LC_Fcm__ProjectId` và `LC_Fcm__ServiceAccountFile`; để trống thì chỉ có email và dòng trong ứng dụng.
Mỗi thông báo mang `type` — cũng là khoá trong bảng tuỳ chọn — và thông báo đẩy mang `data.kind`,
`data.link`, `data.notificationId` để ứng dụng bấm vào là mở đúng màn hình:

| `kind` | Khi nào | `link` |
|---|---|---|
| `DUE_SOON` | Job hằng ngày, trước hạn `CIRCULATION.DUE_SOON_DAYS` ngày | `/tai-khoan` |
| `OVERDUE` | Ngày đầu tiên quá hạn | `/tai-khoan` |
| `HOLD_READY` | Sách đặt giữ vừa được trả về | `/tai-khoan` (+ `holdId`) |
| `DIGITAL_REQUEST` | Yêu cầu đọc được duyệt / từ chối | `/tai-lieu-so/{id}` |
| `CARD_RENEWAL` | Gia hạn thẻ được duyệt | `/tai-khoan` |
| `NEWS` | Tin mới đăng (chỉ đẩy, không ghi dòng trong ứng dụng) | `/tin-tuc/{slug}` |
| `SYSTEM` | Không tắt được | tuỳ |

Thiết bị mà Firebase trả `UNREGISTERED` bị đánh dấu ngừng ngay trong lượt gửi; ứng dụng đăng ký lại
mã mới ở lần mở sau.

### 4.8. Nội dung công khai

| Method | Đường dẫn | Chức năng |
|---|---|---|
| GET | `/api/public/settings` | Tên thư viện, logo, địa chỉ, giờ mở cửa, các tùy chọn hiển thị |

> Phase 15: trả thêm `selfCheckoutEnabled` (bool) và `selfCheckoutVerifyMode` (`NONE` | `WIFI_SSID` | `QR_STATION`) để ứng dụng bạn đọc biết phải xin quyền Wi-Fi hay mở máy quét mã trạm trước khi mượn tự phục vụ.
| GET | `/api/public/home` | Nội dung trang chủ: sách mới, sách mượn nhiều, tin tức, banner |
| GET | `/api/public/news` | Danh sách tin tức |
| GET | `/api/public/news/{slug}` | Một bản tin |
| GET | `/api/public/pages` | Danh sách trang tĩnh |
| GET | `/api/public/pages/{slug}` | Nội dung một trang tĩnh |
| GET | `/api/public/menus` | Cây menu điều hướng |
| GET | `/api/public/media/{objectName}` | Ảnh dùng trong nội dung |
| GET | `/api/public/app-version?platform=android|ios` | `minVersion`, `latestVersion`, `updateUrl`, `forceUpdate`, `serverTime` *(Phase 15)* |

Nhóm này **không cần đăng nhập**, dùng để dựng màn hình chào và phần giới thiệu của ứng dụng.

Gọi `/api/public/settings` ngay khi mở ứng dụng: tên và logo thư viện lấy từ đây, không được viết
cứng trong ứng dụng — cùng một bản cài đặt phải dùng lại được cho thư viện khác.

### 4.9. Bạn đọc tiện ích thêm

| Method | Đường dẫn | Chức năng |
|---|---|---|
| GET / POST | `/api/reader/favorites` | Danh sách và bật/tắt đánh dấu yêu thích |
| GET / POST / DELETE | `/api/reader/saved-searches` | Lưu và chạy lại tìm kiếm |
| POST | `/api/reader/reviews` | Gửi nhận xét về tài liệu (chờ cán bộ duyệt) |
| POST | `/api/reader/cart/email` | Gửi danh sách tài liệu về email trong hồ sơ |

### 4.10. Đồng bộ delta và ảnh theo kích thước *(Phase 15)*

Mọi danh sách phân trang nhận thêm `updatedSince=<ISO 8601>` và trả về `serverTime`. Ứng dụng ghi
`serverTime` của lần trước làm mốc cho lần sau — không dùng đồng hồ điện thoại. Áp dụng cho:

| Danh sách | Cột làm mốc |
|---|---|
| `/api/search`, `/api/search/advanced` | biểu ghi: `updated_at` (chưa sửa thì `created_at`) |
| `/api/catalogs/{catalog}/items` | danh mục: như trên |
| `/api/public/news` | bản tin: như trên |
| `/api/reader/notifications` | `created_at` |
| `/api/reader/loans/current`, `/loans/history` | lượt mượn: `updated_at` |
| `/api/reader/digital` | tài liệu số: `updated_at` (chưa sửa thì lúc tải lên) |

Ảnh bìa và ảnh nội dung nhận `?w=` và `?h=`: máy chủ thu nhỏ vừa khung, giữ tỉ lệ, không phóng to,
tối đa 2.000 điểm ảnh mỗi chiều; dấu bản (`ETag`) mang cả kích thước nên bản nhỏ và bản đủ không
lẫn nhau trong bộ nhớ đệm. Bìa dựng sẵn là SVG, trả nguyên vì tự co giãn.

```
GET /api/public/covers/{bibId}?w=120        # danh sách
GET /api/public/covers/{bibId}?w=600        # chi tiết
GET /api/public/media/cms/anh-tin.jpg?w=400
```

### 4.11. Danh sách kiểm tra khi viết ứng dụng

1. Lưu `accessToken` và `refreshToken` vào vùng lưu trữ có mã hóa của hệ điều hành, không lưu ở nơi
   đọc được bằng plain text.
2. Gom việc làm mới mã vào một chỗ, chỉ một lượt tại một thời điểm.
3. Không đoán quyền: đọc `permission` với tài liệu số; với gia hạn thì cứ gọi rồi xử lý 409.
4. Hiện thẳng `message` của máy chủ khi gặp 409 — câu đó đã viết cho bạn đọc đọc.
5. Xử lý 429 bằng cách chờ theo `Retry-After`.
6. Đặt `Accept-Language: vi-VN` để chắc chắn nhận thông báo tiếng Việt.
7. Cho phép cấu hình địa chỉ máy chủ trong ứng dụng — mỗi thư viện một tên miền.

---

## 5. Tích hợp với hệ thống khác của nhà trường

### 5.1. Đồng bộ danh sách bạn đọc

Hai cách, tùy hạ tầng sẵn có của nhà trường:

- **Nhập tệp Excel định kỳ** — `POST /api/readers/import`, đơn giản và không cần bên kia mở API.
- **Gọi API trực tiếp** — `POST /api/readers/sync` nhận một mảng bạn đọc; đối chiếu theo mã sinh
  viên, có thì cập nhật, chưa có thì tạo mới kèm sinh số thẻ.

Cả hai đường đều báo lại từng dòng lỗi kèm lý do, không dừng cả lô vì một dòng sai.

### 5.2. Máy khách gọi API bằng tài khoản dịch vụ

Hệ thống bên ngoài nên dùng một tài khoản riêng thuộc nhóm quyền hẹp, không dùng tài khoản của người
thật. Cấp cho tài khoản đó đúng những quyền cần thiết, và giới hạn phạm vi dữ liệu nếu chỉ cần đọc
một kho.

---

## 6. Tra cứu nhanh khi có lỗi

| Hiện tượng | Nguyên nhân thường gặp |
|---|---|
| 401 ngay sau khi đăng nhập | Thiếu chữ `Bearer ` trước mã, hoặc đồng hồ máy khách lệch quá xa |
| 403 dù đăng nhập bằng tài khoản quản trị | Gọi endpoint của bạn đọc bằng mã của cán bộ, hoặc ngược lại — hai loại danh tính khác nhau |
| 404 với bản ghi chắc chắn tồn tại | Bản ghi nằm ngoài phạm vi dữ liệu của tài khoản, hoặc đã bị xóa mềm |
| 400 với `errors` rỗng | Thân JSON sai cú pháp nên chưa vào tới bước kiểm tra dữ liệu |
| Tiếng Việt hiện thành ký tự lạ | Thiếu `charset=utf-8` ở `Content-Type` |
| Tra cứu ra rỗng dù kho có tài liệu | Biểu ghi chưa ở trạng thái **Đã xuất bản** |
