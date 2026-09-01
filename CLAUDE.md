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
lên hệ thống hoàn chỉnh. Phân hệ XI (mobile) chưa làm, đúng như phạm vi đã chốt ở mục 0.2.

Sau khi xong Phase 14 đã chạy thêm **một đợt rà soát chất lượng toàn diện** — mở hệ thống như người
dùng thật, đi hết từng màn hình, cố tình đi đường sai, gọi thẳng API không qua giao diện, và nạp dữ
liệu thật từ nguồn ngoài. Đợt rà ấy tìm ra **36 lỗi**, phần lớn là lỗi mà bộ kiểm thử cũ không bao
giờ chạm tới vì nó chỉ xác nhận mã nguồn làm đúng thứ người viết *nghĩ*. Lượt sửa nốt bốn lỗi cuối
tìm thêm một lỗi nữa (B12), thành **37 lỗi, đã sửa hết**.

| Tài liệu | Nội dung |
|---|---|
| `docs/08-so-loi.md` | Sổ lỗi: 37 lỗi, tình hình sửa, và phần **"Làm tiếp gì sau đây"** ở cuối |
| `docs/09-nguon-du-lieu.md` | Khảo sát 16 nguồn dữ liệu thư mục, giấy phép từng nguồn, kết quả nạp |
| `docs/01`–`docs/07` | Bảy tài liệu bàn giao theo mục 10 |

**Kho dữ liệu hiện có trên máy phát triển:** hơn 7.600 biểu ghi thật thu hoạch qua OAI-PMH từ bốn
kho DSpace/OJS của Việt Nam và Thư viện Quốc hội Mỹ, đã duyệt và lên trang tra cứu.

### A.2. Cách làm việc trên kho mã này

**Kiểm thử.** Sửa lỗi nào cũng phải kèm một phép thử **chạy đỏ trước khi sửa, xanh sau khi sửa** —
không có bước đỏ thì không biết phép thử ấy có bắt được gì không. Đã có lần viết phép thử xong thấy
xanh ngay cả khi chưa sửa, vì bối cảnh trong cơ sở dữ liệu kiểm thử tình cờ không dựng ra được tình
huống lỗi; phải tự tay dựng đúng bối cảnh ấy trong phép thử.

**Lệnh chạy đúng:**

```bash
cd backend  && dotnet test                 # 431 unit + 362 integration
cd frontend-admin && npx tsc -b && npx vitest run    # 161 test
cd frontend-opac  && npx tsc -b && npx vitest run    #  52 test
```

> `npx tsc --noEmit` **không kiểm gì cả** ở hai thư mục frontend: `tsconfig.json` là tệp solution
> rỗng chỉ trỏ tới hai tsconfig con. Luôn dùng `npx tsc -b`.

**Sáu phép thử quét mã nguồn** chặn cả một lớp lỗi thay vì chặn một chỗ. Đừng bỏ chúng đi khi thấy
vướng — mỗi cái sinh ra từ một lỗi đã xảy ra thật:

| Phép thử | Luật |
|---|---|
| `frontend-admin/src/api/download.test.ts` | Ngoài `src/api`, không được viết địa chỉ bắt đầu bằng `/api/` — xác thực là JWT trong tiêu đề, thẻ liên kết không mang theo được |
| `frontend-opac/src/lib/marcView.test.ts` | Không `JSON.stringify` biểu ghi MARC ra trang công khai |
| `frontend-admin/src/lib/datetime.test.ts` | Giao diện quản trị không tự viết cách hiện ngày riêng; dùng `lib/datetime` |
| `frontend-opac/src/lib/datetime.test.ts` | Trang tra cứu cũng vậy — hai gói riêng nên phải quét riêng, đây chính là chỗ lỗi D8 lọt qua |
| `frontend-admin/src/lib/columnLabels.test.ts` | Không đặt tên cột đúng một chữ "Giá" — trong nghề thư viện nó vừa là giá sách vừa là giá tiền |
| `backend/.../PermissionAndAuditTests.cs` | Thông báo lỗi không được lọt tiếng Anh của khung nền |

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

**⬜ Phase 15 — Mobile App (Phân hệ XI)** — *ĐỢT SAU, KHÔNG THỰC HIỆN TRONG LẦN BUILD NÀY*
Flutter, đầy đủ chức năng mục XI, gọi vào nhóm endpoint đã hoàn thiện ở Phase 14, build APK/IPA.

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
