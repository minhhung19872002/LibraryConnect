# Bảng đáp ứng yêu cầu kỹ thuật — LibraryConnect

Bảng đối chiếu theo thứ tự các yêu cầu của **Chương V — Yêu cầu về kỹ thuật** trong E-HSMT gói thầu
"Mua sắm Phần mềm thư viện số chuẩn kết nối liên Thư viện".

Cột **Đáp ứng** chỉ được đánh **Có** khi chức năng đã chạy được thật trên hệ thống và kiểm chứng
được bằng thao tác demo hoặc bằng bộ kiểm thử tự động. Những hạng mục thuộc phân hệ chưa bàn giao
được ghi rõ là **Đang thực hiện** kèm phase dự kiến, không đánh dấu đáp ứng.

Cập nhật lần cuối: sau khi hoàn thành Phase 4.

---

## A. Yêu cầu chung về kiến trúc và công nghệ

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| A1 | Kiến trúc 3 tầng tách bạch (Data / Logic / Presentation); frontend không truy cập CSDL trực tiếp | **Có** | `LibraryConnect.Domain` / `.Application` / `.Infrastructure` / `.Api`; giao diện React chỉ gọi REST API | `docs/02-tai-lieu-quan-tri.md` mục 1.2; mã nguồn `backend/src/` |
| A2 | Unicode UTF-8 theo TCVN 6909:2001, không dùng VNI/TCVN3 | **Có** | CSDL PostgreSQL encoding UTF8, collation ICU `vi-VN`; API và giao diện đều UTF-8 | Kịch bản 2.1.9 |
| A3 | Chạy trên máy chủ vật lý lẫn ảo hóa, Windows Server 2019+ / Linux / Unix | **Có** | .NET 8 đa nền tảng; đóng gói Docker chạy trên Linux, chạy trực tiếp được trên Windows Server | `docs/04-cai-dat-cau-hinh.md` mục 1 |
| A4 | Tương thích đa trình duyệt (2 phiên bản gần nhất của Chrome, Edge, Firefox, Safari) | **Có** | SPA React 18 + Ant Design 5, build ES2022 | Kiểm thử thủ công trên trình duyệt |
| A5 | Hỗ trợ vận hành 24/7, có health check | **Có** | `/health` (liveness) và `/health/ready` (readiness: PostgreSQL, Redis) | Kịch bản 2.1.3, 2.1.4 |
| A6 | Dữ liệu lưu trữ vĩnh viễn, không tự động xóa cứng | **Có** | Xóa mềm (`deleted_at`) trên toàn bộ 113 bảng nghiệp vụ; nhật ký mặc định giữ vĩnh viễn | `docs/02-tai-lieu-quan-tri.md` mục 1.3, 3.3; kịch bản 2.3.13 |
| A7 | Phân quyền chi tiết đến từng chức năng và từng phạm vi dữ liệu | **Có** (chức năng) / **Có** (khung phạm vi dữ liệu) | 161 mã quyền `MODULE.ĐỐI_TƯỢNG.HÀNH_ĐỘNG`; bảng `sys.user_data_scopes` theo thư viện/kho/dạng tài liệu | Kịch bản 2.3.2 → 2.3.6. Việc gán phạm vi dữ liệu cụ thể mở khi có dữ liệu kho (Phase 6) |
| A8 | Báo cáo có 3 dạng đầu ra: bảng, đồ họa, xuất tệp PDF/Excel | **Một phần** | Hạ tầng dùng chung đã xong: `ExcelService` (ClosedXML), `PdfReportService` (QuestPDF); áp dụng đầu tiên cho xuất nhật ký | Kịch bản 2.3.14. Các báo cáo nghiệp vụ theo từng phân hệ |
| A9 | Không hardcode danh mục nghiệp vụ, cấu hình được từ giao diện | **Có** | 20 danh mục nghiệp vụ đều thêm/sửa/xóa/nhập/xuất được tại màn hình Danh mục; toàn bộ giá trị cấu hình nằm trong `sys.system_parameters`, sửa tại màn hình I.3 | Kịch bản I.3.2, I.3.6, DM.1 → DM.12 |

---

## B. Phân hệ I — Quản trị hệ thống

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| I.1.a | Danh sách nhóm người dùng: tìm kiếm, phân trang, lọc trạng thái | **Có** | Quản trị hệ thống → Nhóm người dùng | Kịch bản I.1.1 |
| I.1.b | Thêm/sửa/xóa nhóm; nhóm hệ thống không cho xóa | **Có** | Nhóm người dùng → Thêm mới / Sửa / Xóa | Kịch bản I.1.2, I.1.7 |
| I.1.c | Cây quyền phân cấp Module → Chức năng → Hành động, checkbox tri-state, chọn cha tự chọn con | **Có** | Nhóm người dùng → Phân quyền | Kịch bản I.1.3 |
| I.1.d | Sao chép quyền từ nhóm khác | **Có** | Phân quyền → Sao chép từ nhóm khác (thay thế hoặc gộp) | Kịch bản I.1.4, I.1.5 |
| I.1.e | Xem và thêm/bớt thành viên hàng loạt | **Có** | Nhóm người dùng → Thành viên | Kịch bản I.1.6 |
| I.2.a | Danh sách người dùng: lọc theo nhóm, trạng thái, đơn vị; tìm theo tên/username/email | **Có** | Quản trị hệ thống → Người dùng | Kịch bản I.2.1 |
| I.2.b | Thêm/sửa: thông tin cá nhân, gán nhiều nhóm, gán phạm vi dữ liệu | **Có** | Người dùng → Thêm mới / Sửa | Kịch bản I.2.2 |
| I.2.c | Đặt lại mật khẩu, buộc đổi ở lần đăng nhập đầu | **Có** | Người dùng → Đặt lại mật khẩu | Kịch bản I.2.4, 2.1.8 |
| I.2.d | Khóa / mở khóa tài khoản | **Có** | Người dùng → Khóa / Mở khóa | Kịch bản I.2.5 |
| I.2.e | Chính sách mật khẩu cấu hình được: độ dài, ký tự đặc biệt, hạn đổi, khóa sau N lần sai | **Có** | Tham số hệ thống → Chính sách mật khẩu (8 tham số) | Kịch bản 2.3.15, 2.3.16 |
| I.2.f | Import người dùng từ Excel | **Có** | Người dùng → Nhập từ Excel, có bước kiểm tra trước, báo lỗi theo từng dòng | Kịch bản I.2.9 → I.2.12 |
| I.2.g | Xem lịch sử đăng nhập của từng user | **Có** | Người dùng → Lịch sử đăng nhập | Kịch bản I.2.8 |
| I.3.a | Chỉnh sửa tham số theo nhóm, mỗi tham số render đúng loại điều khiển theo kiểu dữ liệu | **Có** | Quản trị hệ thống → Tham số hệ thống | Kịch bản I.3.1 |
| I.3.b | Đủ các nhóm tham số bắt buộc | **Có** | 10 nhóm: Thông tin thư viện, Quy tắc sinh mã, Email SMTP, Sao lưu, Lưu thông, OPAC, Ứng dụng di động, Biên mục, Giới hạn tải lên, Chính sách mật khẩu | Kịch bản 2.1.7 |
| I.3.c | Lịch sử thay đổi tham số (ai đổi, từ giá trị nào sang giá trị nào) | **Có** | Tham số hệ thống → Lịch sử thay đổi | Kịch bản I.3.5 |
| I.4.a | Cài đặt chế độ ghi nhận theo từng đối tượng: bật/tắt Create/Update/Delete/Read, đặt thời gian lưu | **Có** | Nhật ký hệ thống → Cài đặt ghi nhận | Kịch bản 2.3.12, 2.3.13 |
| I.4.b | Tra cứu nhật ký theo thời gian, người dùng, hành động, đối tượng, kết quả, IP | **Có** | Nhật ký hệ thống → Tra cứu | Kịch bản 2.3.7 → 2.3.9 |
| I.4.c | Xem chi tiết diff giá trị cũ/mới dạng JSON | **Có** | Nhật ký → Chi tiết | Kịch bản 2.3.10 |
| I.4.d | Xuất nhật ký ra Excel / PDF | **Có** | Nhật ký → Xuất Excel / Xuất PDF | Kịch bản 2.3.14 |
| I.4.e | Ghi log tự động qua interceptor, không viết thủ công ở từng chức năng | **Có** | EF Core `SaveChangesInterceptor`; ghi trong cùng transaction với thay đổi nghiệp vụ | `docs/02-tai-lieu-quan-tri.md` mục 3.1 |
| I.5.a | Sao lưu thủ công, chọn Full / Data-only | **Có** | Sao lưu cơ sở dữ liệu → Sao lưu ngay | Kịch bản 2.6.1 |
| I.5.b | Sao lưu tự động theo lịch cron, số bản giữ lại, gửi email khi lỗi | **Có** | Tham số → Cấu hình sao lưu; tác vụ nền Hangfire | Kịch bản 2.6.7, 2.6.8 |
| I.5.c | Danh sách bản sao lưu: tên, dung lượng, thời gian, trạng thái; tải về, xóa, phục hồi | **Có** | Sao lưu cơ sở dữ liệu | Kịch bản 2.6.2 |
| I.5.d | Phục hồi có cảnh báo 2 bước, yêu cầu nhập lại mật khẩu, ghi log | **Có** | Sao lưu → Phục hồi | Kịch bản 2.6.3, 2.6.4, 2.6.6 |
| I.5.e | Gọi `pg_dump` / `pg_restore` thật qua process | **Có** | `PostgresBackupService`; PostgreSQL client đóng gói sẵn trong ảnh API | `docs/03-sao-luu-phuc-hoi.md` mục 1 |
| I.5.f | Sao lưu kèm tệp MinIO | **Có** | Tùy chọn *Sao lưu kèm tệp tài liệu số* | Kịch bản 2.6.1 |

---

## B2. Danh mục nghiệp vụ (mục 4.2 và II.9)

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| DM.1 | Đầy đủ các bảng danh mục theo mục 4.2 | **Có** | 20 danh mục: dạng tài liệu, vật mang tin, ngôn ngữ, nước xuất bản, nhà xuất bản, tác giả, đề mục chủ đề, từ khóa, khung phân loại, tùng thư, bộ sưu tập, loại bạn đọc, khoa, ngành, môn học, loại vi phạm, nhà cung cấp, nguồn kinh phí, bộ sưu tập số, chuyên mục tin | Kịch bản DM.1 |
| DM.2 | CRUD cho mọi danh mục | **Có** | Danh mục → chọn danh mục → Thêm mới / Sửa / Xóa | Kịch bản DM.2 → DM.5 |
| DM.3 | Danh mục phân cấp (đề mục, phân loại, bộ sưu tập) | **Có** | Chọn cấp trên bằng cây; chặn chuyển một giá trị vào bên dưới cấp con của chính nó | Kịch bản DM.6, DM.7 |
| DM.4 | Import danh mục từ Excel | **Có** | Tệp mẫu có sheet hướng dẫn; bước kiểm tra trước không ghi dữ liệu; dòng trùng mã sẽ cập nhật thay vì tạo mới | Kịch bản DM.8 → DM.10 |
| DM.5 | Export danh mục ra Excel | **Có** | Tệp xuất dùng đúng tiêu đề cột của tệp mẫu nên sửa xong nhập lại được | Kịch bản DM.11 |
| DM.6 | Gộp trùng, cập nhật toàn bộ biểu ghi liên quan | **Có** | Tìm trùng theo tên đã bỏ dấu; hiển thị số bản ghi đang dùng của từng giá trị; gộp chuyển hết tham chiếu rồi mới xóa | Kịch bản DM.12 |
| DM.7 | Chặn xóa giá trị đang được sử dụng | **Có** | Thông báo nêu rõ số bản ghi đang dùng, hoặc số giá trị con còn lại | Kịch bản DM.5 |
| DM.8 | Nạp sẵn danh mục chuẩn quốc tế | **Có** | 21 ngôn ngữ (ISO 639-2), 24 mã nước (MARC), 14 dạng tài liệu, 8 vật mang tin, bảng tóm tắt DDC (10 lớp + 89 phân lớp), 6 loại bạn đọc, 2 thư viện, 4 kho | Kịch bản 2.1.11 |
| DM.9 | Danh mục tự tạo từ trường MARC 21 | **Có** | Khai báo danh mục bằng tag và trường con nguồn (ví dụ 260$a cho nơi xuất bản); quét toàn bộ biểu ghi bằng chính PostgreSQL để rút giá trị duy nhất, gộp các cách viết trùng (kết quả gộp sống sót qua lần quét sau), rồi dùng làm bộ lọc tra cứu | Kịch bản DM.13 → DM.17 |

---

## B3. Khổ mẫu MARC 21 và định dạng trao đổi (mục 3.1, 3.2, 3.5 và II.5)

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| MC.1 | Lưu biểu ghi đúng cấu trúc MARC 21, không phẳng hóa thành cột | **Có** | Đầu biểu 24 ký tự, trường điều khiển 001–009, trường dữ liệu 010–999 với hai chỉ thị và trường con lặp lại được; lưu dạng `jsonb` ở cột `bib.bib_records.marc_json` | Kịch bản MARC.4; Unit — `MarcJsonTests` |
| MC.2 | Hỗ trợ đầy đủ danh sách trường tối thiểu của mục 3.1 | **Có** | Bộ định nghĩa 220 trường, tên tiếng Việt, kèm ý nghĩa từng giá trị chỉ thị và từng trường con | Kịch bản MARC.1; Integration — `MarcTests` |
| MC.3 | Parser và serializer ISO 2709 tự viết, không dùng thư viện ngoài | **Có** | Dự án `LibraryConnect.Marc` không phụ thuộc gói ngoài nào; đọc và ghi đầy đủ đầu biểu, danh mục, vùng dữ liệu và ba ký tự phân cách | Kịch bản MARC.12 → MARC.14 |
| MC.4 | **Độ dài tính theo byte UTF-8, không theo ký tự** | **Có** | Mọi độ dài trong danh mục và trong đầu biểu đều đếm byte của chuỗi UTF-8; có kiểm thử riêng đối chiếu số byte với số ký tự của chuỗi tiếng Việt | Kịch bản MARC.14; Unit — `Iso2709Tests` |
| MC.5 | Round-trip ISO 2709 với dữ liệu tiếng Việt có dấu | **Có** | Xuất rồi đọc lại cho ra biểu ghi giống hệt: đầu biểu, thứ tự trường, chỉ thị và mọi dấu tiếng Việt | Kịch bản MARC.13; Unit — `Iso2709Tests`; Integration — `MarcTests` |
| MC.6 | Chịu được tệp do phần mềm khác xuất sai | **Có** | Chấp nhận dấu xuống dòng giữa các biểu ghi, dấu BOM, độ dài sai trong đầu biểu; khôi phục được biểu ghi có vị trí sai trong danh mục; báo riêng từng biểu ghi hỏng kèm số thứ tự và vị trí byte | Kịch bản MARC.18, MARC.19 |
| MC.7 | Đọc biểu ghi MARC-8 từ máy chủ nước ngoài | **Có** | Giải mã bộ Basic Latin và Extended Latin (ANSEL) — đủ cho tiếng Việt và mọi ngôn ngữ chữ Latinh — đưa dấu phụ về sau chữ cái rồi chuẩn hóa NFC. Các bộ chữ Hy Lạp, Kirin, Ả Rập, Hán–Nhật–Hàn bị từ chối kèm hướng dẫn yêu cầu máy chủ nguồn trả UTF-8, thay vì đọc sai thành ký tự vô nghĩa | Kịch bản MARC.20 |
| MC.8 | Nhận đúng bảng mã kể cả khi biểu ghi khai sai | **Có** | Kiểm tra lại chính dữ liệu thay vì tin đầu biểu vị trí 09: tệp giải mã được theo UTF-8 nghiêm ngặt thì hiểu là UTF-8 | Kịch bản MARC.21 |
| MC.9 | Nhập và xuất MARCXML theo lược đồ MARC21slim | **Có** | Không gian tên `http://www.loc.gov/MARC21/slim`; đọc được cả biểu ghi lồng trong phản hồi SRU hoặc OAI-PMH | Kịch bản MARC.15, MARC.16 |
| MC.10 | Trình soạn MARC trên giao diện | **Có** | Soạn đầu biểu theo từng vị trí có ý nghĩa; gợi ý trường và trường con theo bộ định nghĩa; chọn chỉ thị theo ý nghĩa; tách chuỗi trường con dán từ hệ thống khác | Kịch bản MARC.4 → MARC.7 |
| MC.11 | Kiểm tra biểu ghi theo bộ định nghĩa (II.5) | **Có** | Phân biệt lỗi chặn lưu với cảnh báo; mỗi thông báo chỉ đúng trường, đúng lần xuất hiện và đúng trường con để giao diện tô sáng | Kịch bản MARC.8 → MARC.11 |
| MC.12 | Cho phép thư viện khai báo trường dùng riêng | **Có** | Thêm, sửa, tắt trường trong bộ định nghĩa; trường bắt buộc không xóa được; trường 001–009 buộc phải là trường điều khiển | Kịch bản MARC.23 → MARC.25 |
| MC.13 | Phát hiện sớm biểu ghi không xuất được | **Có** | Kiểm tra ngay lúc biên mục các giới hạn của ISO 2709: một trường tối đa 9.999 byte, một biểu ghi tối đa 99.999 byte | Kịch bản MARC.22 |

---

## C. Yêu cầu phi chức năng

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| C1 | Mã quyền dạng `MODULE.ENTITY.ACTION` | **Có** | 161 mã quyền, kiểm tra định dạng bằng test tự động | Unit — `PermissionCatalogueTests` |
| C2 | Backend kiểm tra quyền độc lập với frontend, trả HTTP 403 rõ ràng | **Có** | Attribute `[RequirePermission]` trên từng endpoint | Kịch bản 2.3.2 → 2.3.4 |
| C3 | Ghi nhật ký đăng nhập, đăng xuất, đăng nhập thất bại, thay đổi quyền, thay đổi tham số, sao lưu/phục hồi, xuất dữ liệu | **Có** | Ghi tự động và ghi tường minh | `docs/02-tai-lieu-quan-tri.md` mục 3.1 |
| C4 | Lưu diff dạng jsonb | **Có** | Cột `old_value`, `new_value` kiểu `jsonb` trong `sys.audit_logs` | Kịch bản 2.3.10 |
| C5 | Phân trang server-side toàn bộ, không tải hết dữ liệu về client | **Có** | Mọi endpoint danh sách trả `{ items, totalCount, page, pageSize }` | Kịch bản 2.1.x, I.1.1 |
| C6 | Cache Redis cho danh mục, kết quả tra cứu, cấu hình | **Có** | `RedisCacheService`, tự suy giảm sang cache nội bộ khi Redis mất kết nối | Mã nguồn `Infrastructure/Services/RedisCacheService.cs` |
| C7 | Response nén gzip/brotli | **Có** | `UseResponseCompression` với Brotli và Gzip | Kiểm tra header `Content-Encoding` |
| C8 | Security headers (CSP, X-Frame-Options, X-Content-Type-Options), HSTS khi chạy HTTPS | **Có** | `SecurityHeadersMiddleware` | Kiểm tra header phản hồi |
| C9 | Mật khẩu băm BCrypt work factor ≥ 12 | **Có** | `BCryptPasswordHasher` | Unit — `BCryptPasswordHasherTests` |
| C10 | Chống SQL Injection | **Có** | Truy vấn tham số hóa qua EF Core; không ghép chuỗi SQL |  |
| C11 | Rate limiting cho endpoint đăng nhập và API công khai | **Có** | Cấu hình được qua `LC_RateLimit__*` | `docs/04-cai-dat-cau-hinh.md` mục 4.2 |
| C12 | Không log thông tin nhạy cảm | **Có** | Interceptor loại bỏ mật khẩu, khóa bí mật, token khỏi nhật ký | Kịch bản 2.3.11 |
| C13 | Structured logging, log rotation | **Có** | Serilog JSON, luân chuyển theo ngày, giữ 90 tệp | `docs/02-tai-lieu-quan-tri.md` mục 4.2 |
| C14 | Background jobs (Hangfire), dashboard bảo vệ bằng quyền admin | **Có** | 3 tác vụ định kỳ; `/hangfire` yêu cầu quyền `SYSTEM.JOB.VIEW` | `docs/02-tai-lieu-quan-tri.md` mục 4.3 |
| C15 | Font hỗ trợ đầy đủ dấu tiếng Việt | **Có** | Be Vietnam Pro / Inter trên giao diện; Lato nhúng sẵn trong báo cáo PDF | Kịch bản 2.3.14 |
| C16 | Nút lệnh, bố cục màn hình danh sách thống nhất | **Có** | Component dùng chung `PageHeader`, `FilterBar`; bố cục lọc → bảng → phân trang | Kiểm tra trực quan các màn hình |
| C17 | Thông báo lỗi tiếng Việt rõ nghĩa, hiển thị dưới đúng ô nhập | **Có** | Middleware xử lý lỗi tập trung trả lỗi theo từng trường; giao diện ánh xạ vào form | Kịch bản 2.3.15 |
| C18 | Responsive: quản trị tối thiểu 1366×768 | **Có** | Bố cục tối thiểu 1024px, cột bảng ẩn dần theo bề rộng màn hình |  |

---

## D. Trao đổi dữ liệu và các phân hệ còn lại

| # | Yêu cầu | Đáp ứng | Ghi chú |
|---|---|---|---|
| D1 | MARC 21, ISO 2709, MARCXML | **Có** | Chi tiết ở mục B3 |
| D2 | Z39.50 client và server, SRU/SRW | **Đang thực hiện** | Phase 11. Bảng `ill.z3950_targets` và tuyến `/sru` đã có trong cấu hình Nginx |
| D3 | OAI-PMH provider và harvester | **Đang thực hiện** | Phase 11. Bảng `ill.oai_repositories` và tuyến `/oai` đã có |
| D4 | Phân hệ II — Biên mục | **Đang thực hiện** | Phase 5 |
| D5 | Phân hệ III — Bổ sung và Kho | **Đang thực hiện** | Phase 6 |
| D6 | Phân hệ IV — Ấn phẩm định kỳ | **Đang thực hiện** | Phase 7 |
| D7 | Phân hệ V — Tài liệu số | **Đang thực hiện** | Phase 10 |
| D8 | Phân hệ VI — Bạn đọc | **Đang thực hiện** | Phase 8 |
| D9 | Phân hệ VII — Lưu thông | **Đang thực hiện** | Phase 9 |
| D10 | Phân hệ VIII — Quản trị nội dung | **Đang thực hiện** | Phase 12 |
| D11 | Phân hệ IX — Tra cứu OPAC | **Đang thực hiện** | Phase 12. Hạ tầng tra cứu tiếng Việt không dấu (`bib.vn_unaccent`, chỉ mục GIN/pg_trgm) đã hoàn thành ở Phase 1 |
| D12 | Phân hệ X — Tài liệu môn học | **Đang thực hiện** | Phase 13 |
| D13 | Phân hệ XI — Ứng dụng di động | **Đợt sau** | Nhóm endpoint `/api/reader/*` được hoàn thiện và kiểm thử trong đợt web này để ứng dụng cắm vào không phải sửa backend |
