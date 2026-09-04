# Bảng đáp ứng yêu cầu kỹ thuật — LibraryConnect

Bảng đối chiếu theo thứ tự các yêu cầu của **Chương V — Yêu cầu về kỹ thuật** trong E-HSMT gói thầu
"Mua sắm Phần mềm thư viện số chuẩn kết nối liên Thư viện".

Cột **Đáp ứng** chỉ được đánh **Có** khi chức năng đã chạy được thật trên hệ thống và kiểm chứng
được bằng thao tác demo hoặc bằng bộ kiểm thử tự động. Hạng mục duy nhất chưa bàn giao là Phân hệ XI
(ứng dụng di động, dòng D13) — ghi rõ **Đợt sau**, không đánh dấu đáp ứng.

Mỗi ô đánh **Có** chỉ đúng màn hình thao tác được và đúng mã kịch bản kiểm thử trong
`06-kich-ban-kiem-thu.md`; không có ô nào đánh **Có** dựa trên mã nguồn đã viết mà chưa chạy thật.

Cập nhật lần cuối: 02/09/2026, sau đợt hoàn thiện (bảo mật, bộ ánh xạ Dublin Core → MARC 21, ảnh bìa, dữ liệu thật).

---

## A. Yêu cầu chung về kiến trúc và công nghệ

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| A1 | Kiến trúc 3 tầng tách bạch (Data / Logic / Presentation); frontend không truy cập CSDL trực tiếp | **Có** | `LibraryConnect.Domain` / `.Application` / `.Infrastructure` / `.Api`; giao diện React chỉ gọi REST API | `docs/02-tai-lieu-quan-tri.md` mục 1.2; mã nguồn `backend/src/` |
| A2 | Unicode UTF-8 theo TCVN 6909:2001, không dùng VNI/TCVN3 | **Có** | CSDL PostgreSQL encoding UTF8, collation ICU `vi-VN`; API và giao diện đều UTF-8 | Kịch bản 2.1.9 |
| A3 | Chạy trên máy chủ vật lý lẫn ảo hóa, Windows Server 2019+ / Linux / Unix | **Có** | .NET 8 đa nền tảng; đóng gói Docker chạy trên Linux, chạy trực tiếp được trên Windows Server | `docs/04-cai-dat-cau-hinh.md` mục 1 |
| A4 | Tương thích đa trình duyệt (2 phiên bản gần nhất của Chrome, Edge, Firefox, Safari) | **Có** | SPA React 18 + Ant Design 5, build ES2022 | Kiểm thử thủ công trên trình duyệt |
| A5 | Hỗ trợ vận hành 24/7, có health check | **Có** | `/health` (liveness) và `/health/ready` (readiness: PostgreSQL, Redis, MinIO) | Kịch bản 2.1.3, 2.1.4 |
| A6 | Dữ liệu lưu trữ vĩnh viễn, không tự động xóa cứng | **Có** | Xóa mềm (`deleted_at`) trên toàn bộ 113 bảng nghiệp vụ; nhật ký mặc định giữ vĩnh viễn | `docs/02-tai-lieu-quan-tri.md` mục 1.3, 3.3; kịch bản 2.3.13 |
| A7 | Phân quyền chi tiết đến từng chức năng và từng phạm vi dữ liệu | **Có** | 161 mã quyền `MODULE.ĐỐI_TƯỢNG.HÀNH_ĐỘNG`; phạm vi dữ liệu theo thư viện / kho / dạng tài liệu gán được trên màn hình Người dùng và được máy chủ áp bằng bộ lọc toàn cục của EF Core (`DataScopeMiddleware` + `LibraryConnectDbContext.ApplyDataScopeFilters`, có từ 04/09/2026 — trước đó chỉ lưu, không áp; sổ lỗi J1) | Kịch bản 2.3.2 → 2.3.6; `DataScopeTests`: cán bộ gán kho A không thấy kho B lẫn ĐKCB của nó, gán thư viện thì thấy mọi kho của thư viện ấy. Kiểm bằng curl ngoài giao diện: tài khoản chưa đổi mật khẩu tạm bị chặn 403 ở cả sáu endpoint nghiệp vụ thử |
| A8 | Báo cáo có 3 dạng đầu ra: bảng, đồ họa, xuất tệp PDF/Excel | **Có** | Đủ ba dạng ở mọi báo cáo nghiệp vụ của bảy phân hệ: Bổ sung (4 báo cáo), Ấn phẩm định kỳ (4), Bạn đọc (4), Lưu thông (7), Tài liệu số (4), Tài liệu môn học (3), cùng trang Báo cáo thống kê tổng hợp. Bảng dựng bằng AntD Table, đồ họa bằng Recharts, xuất tệp bằng `ExcelService` (ClosedXML) và `PdfReportService` (QuestPDF) | Kịch bản 2.3.14 và các kịch bản báo cáo của từng phân hệ |
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
| I.2.e | Chính sách mật khẩu cấu hình được: độ dài, ký tự đặc biệt, hạn đổi, khóa sau N lần sai | **Có** | Tham số hệ thống → Chính sách mật khẩu (8 tham số); hạn đổi thực thi lúc đăng nhập — quá `SECURITY.PASSWORD_EXPIRY_DAYS` ngày thì phiên bị buộc đổi (`PasswordExpiryTests`, từ 04/09/2026 — sổ lỗi J6) | Kịch bản 2.3.15, 2.3.16 |
| I.2.f | Import người dùng từ Excel | **Có** | Người dùng → Nhập từ Excel, có bước kiểm tra trước, báo lỗi theo từng dòng | Kịch bản I.2.9 → I.2.12 |
| I.2.g | Xem lịch sử đăng nhập của từng user | **Có** | Người dùng → Lịch sử đăng nhập | Kịch bản I.2.8 |
| I.3.a | Chỉnh sửa tham số theo nhóm, mỗi tham số render đúng loại điều khiển theo kiểu dữ liệu | **Có** | Quản trị hệ thống → Tham số hệ thống | Kịch bản I.3.1 |
| I.3.b | Đủ các nhóm tham số bắt buộc | **Có** | 10 nhóm: Thông tin thư viện, Quy tắc sinh mã, Email SMTP, Sao lưu, Lưu thông, OPAC, Ứng dụng di động, Biên mục, Giới hạn tải lên, Chính sách mật khẩu | Kịch bản 2.1.7 |
| I.3.c | Lịch sử thay đổi tham số (ai đổi, từ giá trị nào sang giá trị nào) | **Có** | Tham số hệ thống → Lịch sử thay đổi | Kịch bản I.3.5 |
| I.4.a | Cài đặt chế độ ghi nhận theo từng đối tượng: bật/tắt Create/Update/Delete/Read, đặt thời gian lưu | **Có** | Nhật ký hệ thống → Cài đặt ghi nhận. Ba hành động ghi bắt tự động ở tầng dữ liệu; lượt **xem** ghi ở các màn hình chi tiết Bạn đọc, Người dùng, Biểu ghi và Tài liệu số (bổ sung 04/09/2026), không ghi ở màn hình danh sách. Đối tượng chưa có chỗ ghi lượt xem hiện ô khoá kèm lời giải thích | Kịch bản 2.3.12, 2.3.13, 2.3.14 |
| I.4.b | Tra cứu nhật ký theo thời gian, người dùng, hành động, đối tượng, kết quả, IP | **Có** | Nhật ký hệ thống → Tra cứu | Kịch bản 2.3.7 → 2.3.9 |
| I.4.c | Xem chi tiết diff giá trị cũ/mới dạng JSON | **Có** | Nhật ký → Chi tiết | Kịch bản 2.3.10 |
| I.4.d | Xuất nhật ký ra Excel / PDF | **Có** | Nhật ký → Xuất Excel / Xuất PDF | Kịch bản 2.3.14 |
| I.4.e | Ghi log tự động qua interceptor, không viết thủ công ở từng chức năng | **Có** | EF Core `SaveChangesInterceptor`; ghi trong cùng transaction với thay đổi nghiệp vụ | `docs/02-tai-lieu-quan-tri.md` mục 3.1 |
| I.5.a | Sao lưu thủ công, chọn Full / Data-only | **Có** | Sao lưu cơ sở dữ liệu → Sao lưu ngay. Lượt sao lưu **xếp vào hàng đợi Hangfire**, trả về ngay và không phụ thuộc giới hạn thời gian của proxy; màn hình tự cập nhật trạng thái từ bảng `backup_jobs` | Kịch bản 2.6.1, 2.6.9 |
| I.5.b | Sao lưu tự động theo lịch cron, số bản giữ lại, gửi email khi lỗi | **Có** | Tham số → Cấu hình sao lưu; tác vụ nền Hangfire. Đổi lịch có hiệu lực ngay, không đợi khởi động lại, và màn hình sao lưu hiện cả lịch bộ chạy nền đang giữ lẫn thư mục chứa tệp (bổ sung 04/09/2026) | Kịch bản 2.6.7, 2.6.8, 2.6.12 |
| I.5.c | Danh sách bản sao lưu: tên, dung lượng, thời gian, trạng thái; tải về, xóa, phục hồi | **Có** | Sao lưu cơ sở dữ liệu | Kịch bản 2.6.2 |
| I.5.c1 | Phục hồi kèm tệp tài liệu số | **Có** | Bổ sung 04/09/2026: bản sao lưu có kèm tệp thì lượt phục hồi tải chúng trở lại kho đối tượng ngay sau `pg_restore` và báo số tệp; trước đó phải chạy `mc mirror` bằng tay | Kịch bản 2.6.13 |
| I.5.d | Phục hồi có cảnh báo 2 bước, yêu cầu nhập lại mật khẩu, ghi log | **Có** | Sao lưu → Phục hồi. Lượt phục hồi **chạy ở tiến trình nền**, hộp thoại theo dõi tiến độ tại chỗ và không đóng được khi đang chạy; tiến độ đọc từ bộ nhớ đệm vì chính cơ sở dữ liệu đang bị ghi đè | Kịch bản 2.6.3, 2.6.4, 2.6.6, 2.6.11 |
| I.5.e | Gọi `pg_dump` / `pg_restore` thật qua process | **Có** | `PostgresBackupService`; PostgreSQL client đóng gói sẵn trong ảnh API | `docs/03-sao-luu-phuc-hoi.md` mục 1 |
| I.5.f | Sao lưu kèm tệp MinIO | **Có** | Tùy chọn *Sao lưu kèm tệp tài liệu số* — chép mọi object của bucket tài liệu và ảnh về `<bản sao lưu>-files/<bucket>/…` (`ObjectStorageMirror`, từ 04/09/2026; trước đó chỉ ghi README — sổ lỗi J4) | Kịch bản 2.6.1; `BackupTests.Sao_luu_kem_tep_tai_lieu_so…` |

---

## B2. Danh mục nghiệp vụ (mục 4.2 và II.9)

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| DM.1 | Đầy đủ các bảng danh mục theo mục 4.2 | **Có** | 20 danh mục: dạng tài liệu, vật mang tin, ngôn ngữ, nước xuất bản, nhà xuất bản, tác giả, đề mục chủ đề, từ khóa, khung phân loại, tùng thư, bộ sưu tập, loại bạn đọc, khoa, ngành, môn học, loại vi phạm, nhà cung cấp, nguồn kinh phí, bộ sưu tập số, chuyên mục tin | Kịch bản DM.1 |
| DM.2 | CRUD cho mọi danh mục | **Có** | Danh mục → chọn danh mục → Thêm mới / Sửa / Xóa | Kịch bản DM.2 → DM.5 |
| DM.3 | Danh mục phân cấp (đề mục, phân loại, bộ sưu tập) | **Có** | Chọn cấp trên bằng cây; chặn chuyển một giá trị vào bên dưới cấp con của chính nó | Kịch bản DM.6, DM.7 |
| DM.4 | Import danh mục từ Excel | **Có** | Tệp mẫu có sheet hướng dẫn; bước kiểm tra trước không ghi dữ liệu; dòng trùng mã sẽ cập nhật thay vì tạo mới | Kịch bản DM.8 → DM.10 |
| DM.5 | Export danh mục ra Excel | **Có** | Tệp xuất dùng đúng tiêu đề cột của tệp mẫu nên sửa xong nhập lại được | Kịch bản DM.11 |
| DM.6 | Gộp trùng, cập nhật toàn bộ biểu ghi liên quan | **Có** | Tìm trùng theo tên đã bỏ dấu; hiển thị số bản ghi đang dùng của từng giá trị; gộp chuyển hết tham chiếu rồi mới xóa. Từ 04/09/2026 gộp còn **sửa chính biểu ghi MARC** và cột phẳng rút từ nó, nên tên cũ không còn ở danh sách, dạng ISBD, phích mục lục hay tệp xuất ISO 2709; mỗi biểu ghi bị sửa có một phiên bản trong lịch sử | Kịch bản DM.12, DM.14 |
| DM.7 | Chặn xóa giá trị đang được sử dụng | **Có** | Thông báo nêu rõ số bản ghi đang dùng, hoặc số giá trị con còn lại | Kịch bản DM.5 |
| DM.8 | Nạp sẵn danh mục chuẩn quốc tế | **Có** | 21 ngôn ngữ (ISO 639-2), 24 mã nước (MARC), 14 dạng tài liệu, 8 vật mang tin, bảng tóm tắt DDC (10 lớp + 89 phân lớp), 6 loại bạn đọc, 2 thư viện, 4 kho | Kịch bản 2.1.11 |
| DM.9 | Danh mục tự tạo từ trường MARC 21 | **Có** | Khai báo danh mục bằng tag và trường con nguồn (ví dụ 260$a cho nơi xuất bản); quét toàn bộ biểu ghi bằng chính PostgreSQL để rút giá trị duy nhất, gộp các cách viết trùng (kết quả gộp sống sót qua lần quét sau). Danh mục bật cờ "hiện làm bộ lọc" xuất hiện thành một nhóm lọc trên trang tra cứu kèm số đếm, bấm vào thì kết quả thu hẹp thật (bổ sung 04/09/2026) | Kịch bản DM.13 → DM.17 |

---

## B3. Khổ mẫu MARC 21 và định dạng trao đổi (mục 3.1, 3.2, 3.5 và II.5)

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| MC.1 | Lưu biểu ghi đúng cấu trúc MARC 21, không phẳng hóa thành cột | **Có** | Đầu biểu 24 ký tự, trường điều khiển 001–009, trường dữ liệu 010–999 với hai chỉ thị và trường con lặp lại được; lưu dạng `jsonb` ở cột `bib.bib_records.marc_json` | Kịch bản MARC.4; Unit — `MarcJsonTests` |
| MC.2 | Hỗ trợ đầy đủ danh sách trường tối thiểu của mục 3.1 | **Có** | Bộ định nghĩa 220 trường, tên tiếng Việt, kèm ý nghĩa từng giá trị chỉ thị và từng trường con | Kịch bản MARC.1; Integration — `MarcTests` |
| MC.3 | Parser và serializer ISO 2709 tự viết, không dùng thư viện ngoài | **Có** | Dự án `LibraryConnect.Marc` không phụ thuộc gói ngoài nào; đọc và ghi đầy đủ đầu biểu, danh mục, vùng dữ liệu và ba ký tự phân cách | Kịch bản MARC.12 → MARC.14 |
| MC.4 | **Độ dài tính theo byte UTF-8, không theo ký tự** | **Có** | Mọi độ dài trong danh mục và trong đầu biểu đều đếm byte của chuỗi UTF-8; có kiểm thử riêng đối chiếu số byte với số ký tự của chuỗi tiếng Việt | Kịch bản MARC.14; Unit — `Iso2709Tests` |
| MC.5 | Round-trip ISO 2709 với dữ liệu tiếng Việt có dấu | **Có** | Xuất rồi đọc lại cho ra biểu ghi giống hệt: đầu biểu, thứ tự trường, chỉ thị và mọi dấu tiếng Việt | Kịch bản MARC.13; Unit — `Iso2709Tests`; Integration — `MarcTests` |
| MC.5b | Kiểm tính hợp lệ bằng **công cụ độc lập**, không dùng bộ đọc của chính sản phẩm | **Có** | Xuất toàn kho ra ISO 2709 và MARCXML rồi cho `pymarc` 5.4 (bộ đọc MARC của giới thư viện, viết độc lập) đọc và đối chiếu luật MARC 21 | **7.675/7.675 biểu ghi hợp lệ, 0 lỗi**; nhan đề khớp 100% giữa hai định dạng; 6.292 nhan đề tiếng Việt có dấu qua vòng xuất–nhập không vỡ. Đây là bằng chứng cho mục 2.4 của E-HSMT |
| MC.5c | Trường dài quá giới hạn của ISO 2709 không làm hỏng cả lô xuất | **Có** | Trường lặp được thì chia thành nhiều lần lặp, cắt ở chỗ giáp từ nên không đứt giữa chữ tiếng Việt; trường không lặp được thì bỏ đúng biểu ghi ấy và báo lý do trên tiêu đề phản hồi | Unit — `Iso2709LongFieldTests` |
| MC.6 | Chịu được tệp do phần mềm khác xuất sai | **Có** | Chấp nhận dấu xuống dòng giữa các biểu ghi, dấu BOM, độ dài sai trong đầu biểu; khôi phục được biểu ghi có vị trí sai trong danh mục; báo riêng từng biểu ghi hỏng kèm số thứ tự và vị trí byte | Kịch bản MARC.18, MARC.19 |
| MC.7 | Đọc biểu ghi MARC-8 từ máy chủ nước ngoài | **Có** | Giải mã bộ Basic Latin và Extended Latin (ANSEL) — đủ cho tiếng Việt và mọi ngôn ngữ chữ Latinh — đưa dấu phụ về sau chữ cái rồi chuẩn hóa NFC. Các bộ chữ Hy Lạp, Kirin, Ả Rập, Hán–Nhật–Hàn bị từ chối kèm hướng dẫn yêu cầu máy chủ nguồn trả UTF-8, thay vì đọc sai thành ký tự vô nghĩa | Kịch bản MARC.20 |
| MC.8 | Nhận đúng bảng mã kể cả khi biểu ghi khai sai | **Có** | Kiểm tra lại chính dữ liệu thay vì tin đầu biểu vị trí 09: tệp giải mã được theo UTF-8 nghiêm ngặt thì hiểu là UTF-8 | Kịch bản MARC.21 |
| MC.9 | Nhập và xuất MARCXML theo lược đồ MARC21slim | **Có** | Không gian tên `http://www.loc.gov/MARC21/slim`; đọc được cả biểu ghi lồng trong phản hồi SRU hoặc OAI-PMH | Kịch bản MARC.15, MARC.16 |
| MC.10 | Trình soạn MARC trên giao diện | **Có** | Soạn đầu biểu theo từng vị trí có ý nghĩa; gợi ý trường và trường con theo bộ định nghĩa; chọn chỉ thị theo ý nghĩa; tách chuỗi trường con dán từ hệ thống khác | Kịch bản MARC.4 → MARC.7 |
| MC.11 | Kiểm tra biểu ghi theo bộ định nghĩa (II.5) | **Có** | Phân biệt lỗi chặn lưu với cảnh báo; mỗi thông báo chỉ đúng trường, đúng lần xuất hiện và đúng trường con để giao diện tô sáng | Kịch bản MARC.8 → MARC.11 |
| MC.12 | Cho phép thư viện khai báo trường dùng riêng | **Có** | Thêm, sửa, tắt trường trong bộ định nghĩa; trường bắt buộc không xóa được; trường 001–009 buộc phải là trường điều khiển | Kịch bản MARC.23 → MARC.25 |
| MC.13 | Phát hiện sớm biểu ghi không xuất được | **Có** | Kiểm tra ngay lúc biên mục các giới hạn của ISO 2709: một trường tối đa 9.999 byte, một biểu ghi tối đa 99.999 byte | Kịch bản MARC.22 |

---

## B4. Phân hệ II — Biên mục

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| II.1 | Cài đặt giá trị ngầm định cho trường MARC 21 | **Có** | Bảng giá trị ngầm định theo dạng tài liệu, chỉ định được cả trường con lẫn vị trí ký tự của trường điều khiển; giá trị lấy được từ tham số hệ thống nên đổi tham số là biểu ghi mới đổi theo | Kịch bản BM.1, BM.2 |
| II.2 | Thêm mới ấn phẩm bằng trình soạn MARC chuyên nghiệp | **Có** | Bảng nhập theo dòng Tag / Ind1 / Ind2 / trường con; gợi ý tên trường tiếng Việt; chọn chỉ thị theo ý nghĩa; tách chuỗi trường con dán từ hệ thống khác; kiểm tra tại chỗ; chọn mẫu biên mục theo dạng tài liệu; **trình hướng dẫn nhập 008 theo từng vị trí**; **nhân bản trường bằng nút hoặc Ctrl+D**; **sắp xếp lại trường bằng kéo thả hoặc nút lên/xuống**; **xem trước ISBD ngay khi chưa lưu**; **lấy biểu ghi từ Z39.50 / theo ISBN ngay trên biểu mẫu**; Ctrl+S để lưu | Kịch bản BM.4 → BM.6, BM.10, BM.34 → BM.36 |
| II.2b | Tạo đăng ký cá biệt sau khi lưu biểu ghi | **Có** | Tạo theo lô, mã vạch và số ĐKCB sinh liền nhau, ký hiệu xếp giá theo quy tắc cấu hình được của thư viện hoặc riêng của từng kho | Kịch bản BM.13 |
| II.3 | Cập nhật, xóa, xem chi tiết ấn phẩm | **Có** | Bốn tab chi tiết; lịch sử giữ mọi phiên bản, xem khác biệt theo từng trường, khôi phục được; xóa mềm bắt buộc nhập lý do và bị chặn khi còn đăng ký cá biệt hoặc tài liệu số | Kịch bản BM.11, BM.12, BM.14, BM.15 Tab thứ tư có **lịch sử lưu thông** (`GET /cataloging/bibs/{id}/loans`, phân trang máy chủ, lọc phiếu chưa trả, tìm theo mã phiếu / mã vạch / bạn đọc) từ 04/09/2026 — trước đó chỉ có lịch sử sửa đổi; `CirculationTests.Lich_su_luu_thong_cua_bieu_ghi…`. |
| II.4 | Hàng đợi biên mục chi tiết | **Có** | Bảng công việc năm cột kèm số việc; phân công hàng loạt với độ ưu tiên và hạn xử lý; cảnh báo quá hạn; duyệt hoặc trả lại kèm lý do bắt buộc; thống kê năng suất theo cán bộ | Kịch bản BM.17 → BM.20 |
| II.5 | Cập nhật mẫu và trường biên mục | **Có** | Bộ định nghĩa 220 trường MARC 21 (mục B3) và mẫu biên mục theo dạng tài liệu, có mẫu mặc định | Kịch bản BM.3, và mục B3 |
| II.5 | Import bộ định nghĩa MARC 21 chuẩn | **Có** | Bổ sung 04/09/2026: hai nút trên màn hình Định nghĩa trường — "Nạp trường còn thiếu" (an toàn, chạy lúc nào cũng được) và "Khôi phục bộ chuẩn" (ghi đè, có hỏi lại). Trường thư viện tự thêm được giữ nguyên ở cả hai | Kịch bản BM.51 |
| II.6 | Nhập dữ liệu từ biểu ghi ISO 2709 | **Có** | Luồng bốn bước: chọn tệp, xem trước kèm đối chiếu trùng, chọn cách xử lý trùng và nơi để bản sách, chạy nền có tiến trình và nhật ký lỗi từng biểu ghi. Bốn cách xử lý trùng và ba cách đối chiếu | Kịch bản BM.21 → BM.27 |
| II.6b | Xuất ISO 2709 | **Có** | Xuất theo danh sách tick chọn hoặc theo đúng bộ lọc đang xem, ra .mrc hoặc MARCXML | Kịch bản BM.28, BM.29 |
| II.7 | Nhập dữ liệu từ chuẩn Z39.50 | **Có** | Máy khách Z39.50 tự viết (BER/ASN.1, Init/Search/Present/Close, truy vấn RPN theo bộ thuộc tính Bib-1); tra song song nhiều máy chủ, đối chiếu với kho của mình rồi mở trình soạn MARC để hiệu đính trước khi lưu. Máy chủ nào từ chối bước Present thì hệ thống tự chuyển sang lối SRU của cùng thư viện | Kịch bản BM.42 → BM.45 và mục B10. Đã tra cứu thật tới Thư viện Quốc hội Mỹ: 11.528 kết quả cho "Nhan đề = Vietnam", lấy về được biểu ghi MARC21 |
| II.8 | Nhập dữ liệu từ Excel | **Có** | Tệp mẫu có sheet hướng dẫn; hệ thống đoán ánh xạ từ tên cột; ánh xạ sửa được và lưu lại thành hồ sơ dùng lại; một ô nhiều giá trị tách thành nhiều lần lặp của trường; chạy nền, báo lỗi theo số dòng bảng tính | Kịch bản BM.30 → BM.34 |
| II.9 | Quản lý danh mục (chỉ mục) | **Có** | Danh mục có sẵn ở mục B2; danh mục tự tạo từ trường MARC ở dòng DM.9 | Kịch bản BM.35 → BM.37 |
| II.11 | Ảnh bìa tài liệu | **Có** | Bốn lớp tra ảnh thật, dừng ở lớp đầu tiên có kết quả: ảnh cán bộ tải lên → trường 856 của biểu ghi → Google Books theo ISBN → Open Library theo ISBN; ảnh tải về kho đối tượng của mình, không dẫn thẳng sang máy chủ nguồn. Biểu ghi không tra được ảnh thì máy chủ dựng bìa SVG từ nhan đề, tác giả, năm và dạng tài liệu, mỗi dạng một tông màu riêng | Unit — `CoverImageBuilderTests`. Đo trên kho thật: 444/7.675 biểu ghi có ISBN (5,8%), nên bìa dựng sẵn là ảnh bìa chính của phần lớn kho |
| II.10 | Xử lý phích (thẻ mục lục) | **Có** | Trình thiết kế kéo thả theo milimét, khổ chuẩn 12,5 × 7,5 cm hoặc tùy chỉnh, mỗi ô ánh xạ tới trường MARC hoặc trường tổng hợp; bốn loại phích; in hàng loạt ra PDF, xếp nhiều phích trên A4 hoặc mỗi phích một trang | Kịch bản BM.38 → BM.41 |

---

## C. Yêu cầu phi chức năng

| # | Yêu cầu | Đáp ứng | Chức năng tương ứng | Chứng minh |
|---|---|---|---|---|
| C1 | Mã quyền dạng `MODULE.ENTITY.ACTION` | **Có** | 161 mã quyền, kiểm tra định dạng bằng test tự động | Unit — `PermissionCatalogueTests` |
| C2 | Backend kiểm tra quyền độc lập với frontend, trả HTTP 403 rõ ràng | **Có** | Attribute `[RequirePermission]` trên từng endpoint | Kịch bản 2.3.2 → 2.3.4 |
| C3 | Ghi nhật ký đăng nhập, đăng xuất, đăng nhập thất bại, thay đổi quyền, thay đổi tham số, sao lưu/phục hồi, xuất dữ liệu | **Có** | Ghi tự động và ghi tường minh | `docs/02-tai-lieu-quan-tri.md` mục 3.1 |
| C4 | Lưu diff dạng jsonb | **Có** | Cột `old_value`, `new_value` kiểu `jsonb` trong `sys.audit_logs` | Kịch bản 2.3.10 |
| C5 | Phân trang server-side toàn bộ, không tải hết dữ liệu về client | **Có** | Mọi endpoint danh sách trả `{ items, totalCount, page, pageSize }` | Kịch bản 2.1.x, I.1.1 |
| C6 | Cache Redis cho danh mục, kết quả tra cứu, cấu hình | **Có** | `RedisCacheService`, tự suy giảm sang cache nội bộ khi Redis mất kết nối; danh sách + cây danh mục đệm 10 phút và xoá theo tiền tố khi ghi, hai trang đầu tra cứu OPAC đệm 60 giây, tham số hệ thống và quyền đệm 15 phút | Mã nguồn `Infrastructure/Services/RedisCacheService.cs` |
| C7 | Response nén gzip/brotli | **Có** | `UseResponseCompression` với Brotli và Gzip | Kiểm tra header `Content-Encoding` |
| C8 | Security headers (CSP, X-Frame-Options, X-Content-Type-Options), HSTS khi chạy HTTPS | **Có** | `SecurityHeadersMiddleware` | Kiểm tra header phản hồi |
| C9 | Mật khẩu băm BCrypt work factor ≥ 12 | **Có** | `BCryptPasswordHasher` | Unit — `BCryptPasswordHasherTests` |
| C9b | Quét virus tệp tải lên (ClamAV, tùy chọn) | **Có** | `IVirusScanner` đứng ở cổng vào duy nhất của mọi tệp người dùng tải lên; `ClamAvScanner` nói thẳng giao thức INSTREAM của clamd. Tắt mặc định; bật bằng `LC_ClamAv__Enabled=true` cộng `docker compose --profile antivirus up -d`. Bật mà không nối được clamd thì **từ chối tệp**, không lặng lẽ cho qua | `DigitalTests.Tep_nhiem_virus_bi_tu_choi_ngay_o_cong_vao`; `.env.example`, `docs/04` |
| C10 | Chống SQL Injection | **Có** | Truy vấn tham số hóa qua EF Core; không ghép chuỗi SQL |  |
| C11 | Rate limiting cho endpoint đăng nhập và API công khai | **Có** | Cấu hình được qua `LC_RateLimit__*`; áp cho cả cửa cán bộ `/api/auth/login` lẫn cửa bạn đọc `/api/reader/auth/login` (`RateLimitTests`, 2 bài) | `docs/04-cai-dat-cau-hinh.md` mục 4.2 Áp cho cả `/api/reader/auth/login`. |
| C12 | Không log thông tin nhạy cảm | **Có** | Interceptor loại bỏ mật khẩu, khóa bí mật, token khỏi nhật ký | Kịch bản 2.3.11 |
| C13 | Structured logging, log rotation | **Có** | Serilog JSON, luân chuyển theo ngày, giữ 90 tệp | `docs/02-tai-lieu-quan-tri.md` mục 4.2 |
| C14 | Background jobs (Hangfire), dashboard bảo vệ bằng quyền admin | **Có** | 3 tác vụ định kỳ; `/hangfire` yêu cầu quyền `SYSTEM.JOB.VIEW` | `docs/02-tai-lieu-quan-tri.md` mục 4.3 |
| C15 | Font hỗ trợ đầy đủ dấu tiếng Việt | **Có** | Be Vietnam Pro / Inter trên giao diện; Lato nhúng sẵn trong báo cáo PDF | Kịch bản 2.3.14 |
| C16 | Nút lệnh, bố cục màn hình danh sách thống nhất | **Có** | Component dùng chung `PageHeader`, `FilterBar`; bố cục lọc → bảng → phân trang | Kiểm tra trực quan các màn hình |
| C17 | Thông báo lỗi tiếng Việt rõ nghĩa, hiển thị dưới đúng ô nhập | **Có** | Middleware xử lý lỗi tập trung trả lỗi theo từng trường; giao diện ánh xạ vào form | Kịch bản 2.3.15 |
| C18 | Responsive: quản trị tối thiểu 1366×768 | **Có** | Thiết kế cho 1366×768; từ 992px trở xuống menu chuyển thành ngăn kéo, các khối xếp dọc và bảng nhiều cột cuộn trong khung của nó — mở được trên điện thoại mà không phải cuộn ngang cả trang | Kiểm 40 màn hình quản trị ở bề ngang 390px |
| C19 | Tra cứu OPAC trả kết quả dưới 1 giây với 500.000 biểu ghi | **Có** | Mọi trường tra cứu được gộp sẵn vào một cột có chỉ mục ba ký tự; câu hỏi rộng dừng đếm ở 10.000 và ghi "hơn 10.000 kết quả" | Kịch bản HN.1–HN.5, đo được 0,5 s |
| C20 | Hỗ trợ 200 người dùng đồng thời | **Có** | Kho kết nối chặn dưới hạn của PostgreSQL, bộ nhớ chia sẻ đủ cho truy vấn song song | Kịch bản HN.6, HN.7: 200 bạn đọc, 0 lỗi, trung vị 840 ms |
| C21 | Nâng cấp phiên bản trên kho dữ liệu lớn | **Có** | Migration chạy với hạn thời gian riêng, dựng lại cột tra cứu bằng câu lệnh gộp | Kịch bản HN.9: 140 s trên 500.000 biểu ghi |
| C22 | Buộc đổi mật khẩu ở lần đăng nhập đầu | **Có** | Máy chủ chặn mọi lượt gọi của tài khoản còn mật khẩu tạm, không chỉ giao diện ẩn màn hình | Integration — `PermissionAndAuditTests`, kịch bản 2.3.16 |
| C23 | Báo cáo thống kê chung toàn hệ thống | **Có** | Màn hình Báo cáo thống kê gộp chỉ tiêu của bảy phân hệ theo kỳ, kèm biểu đồ xu hướng 12 tháng, phân bố kho và mục lục dẫn tới từng báo cáo chi tiết; xuất được Excel và PDF | Integration — `SystemReportTests` |

---

## B5. Phân hệ III — Bổ sung và Kho

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| III.1 | Yêu cầu đặt mua ấn phẩm đơn bản | **Có** | Form đề nghị có người đề nghị, đơn vị, lý do, nguồn kinh phí; thêm từng đầu sách kèm số lượng và đơn giá dự kiến |
| III.1 | Yêu cầu đặt mua ấn phẩm định kỳ | **Có** | Cùng màn hình, chọn loại "Ấn phẩm định kỳ" là bảng đổi sang cột ISSN, kỳ hạn, số kỳ/năm (điền sẵn theo kỳ hạn), thời gian đặt từ tháng/năm đến tháng/năm, đơn giá/kỳ; số kỳ và thành tiền tự tính ngay khi gõ, máy chủ tính lại cùng công thức (số bản × số kỳ × đơn giá kỳ) cho cả giá trị duyệt |
| III.1 | Tra nhanh tài liệu thư viện đã có | **Có** | Tra theo ISBN trước, không có thì so nhan đề đã bỏ dấu; dòng trùng được đánh dấu ngay lúc lưu để người duyệt nhìn thấy |
| III.1 | Nhập danh sách đề nghị từ Excel | **Có** | Tệp mẫu có sheet hướng dẫn; nhà cung cấp so theo tên không dấu; dòng lỗi báo theo đúng số dòng trong tệp |
| III.1 | Duyệt yêu cầu, duyệt từng dòng, từ chối kèm lý do | **Có** | Sửa được số lượng duyệt từng dòng; duyệt thiếu thì yêu cầu thành "Duyệt một phần" |
| III.1 | Quy trình duyệt nhiều cấp | **Có** | Hai tham số: `ACQ.APPROVAL_LEVELS` là số cấp, `ACQ.APPROVAL_GROUPS` khai nhóm duyệt từng cấp theo thứ tự. Người ngoài nhóm bị từ chối, và một người không duyệt được hai cấp liên tiếp. Yêu cầu chỉ thành "Đã duyệt" sau khi qua đủ số cấp |
| III.1 | Thông báo tới người duyệt khi gửi duyệt | **Có** | Chuông thông báo trên thanh trên của giao diện quản trị; gửi cho nhóm duyệt cấp kế tiếp, hoặc cho mọi cán bộ có quyền duyệt khi cấp ấy chưa gắn nhóm. Kết quả duyệt hoặc từ chối báo ngược cho người đề nghị |
| III.1 | Tạo đơn đặt từ yêu cầu đã duyệt, gộp và nhóm theo NCC | **Có** | Một đơn cho mỗi nhà cung cấp; dòng đã nằm trong đơn trước đó được bỏ qua |
| III.1 | In đơn đặt hàng theo mẫu | **Có** | Qua trình kết xuất biểu mẫu chung, mẫu `DH-DATHANG` |
| III.1 | Theo dõi giao hàng, nhận từng phần, cảnh báo quá hạn | **Có** | Trạng thái đơn là hệ quả của số thực nhận; cảnh báo quá hạn theo tham số `ACQ.ORDER_OVERDUE_DAYS` |
| III.1 | Biên bản bàn giao, in PDF, đính kèm bản scan | **Có** | Số liệu lấy từ số thực nhận của đơn; bản scan lưu ở kho đối tượng, không nằm dưới thư mục web |
| III.1 | Biên bản có danh sách tài liệu, số lượng, tình trạng | **Có** | Bổ sung 04/09/2026: bảng chi tiết là của chính biên bản, chép sang lúc lập nên sửa đơn đặt về sau không làm đổi tờ giấy đã ký. Cột tình trạng nhập trên màn hình và in ra biên bản. Biên bản không gắn đơn đặt (biếu tặng, nộp lưu chiểu) cũng có bảng chi tiết |
| III.1 | Báo cáo duyệt mua | **Có** | Theo trạng thái, đơn vị đề nghị, theo tháng; tỷ lệ duyệt và tổng kinh phí duyệt; biểu đồ tròn/cột cho từng lát cắt; xuất Excel và PDF (`kind=PurchaseApproval`) |
| III.1 | Quản lý nhà cung cấp và lịch sử giao dịch | **Có** | CRUD ở màn hình danh mục, kèm ô đánh giá 1–5 sao; lịch sử giao dịch, tỷ lệ giao đủ, số đơn chưa giao đủ và số sao đã chấm ở màn hình báo cáo |
| III.2 | Biên mục sơ lược tuân thủ MARC 21 | **Có** | Mười trường, lưu thành biểu ghi MARC 21 mức biên mục 3, tự đẩy vào hàng đợi biên mục chi tiết. Nhập nhanh liên tục ở menu Bổ sung › Biên mục sơ lược: lưu xong giữ nguyên kho, dạng tài liệu, nhà xuất bản, xóa phần của cuốn vừa nhập, trả tiêu điểm về ô nhan đề, đếm "đã nhập N" kèm mã vạch đã sinh |
| III.2 | Xếp giá, sinh ký hiệu tự động, xếp giá hàng loạt | **Có** | Quy tắc theo kho, không có thì theo tham số chung; xếp giá cho danh sách tick chọn hoặc cho toàn bộ kết quả lọc |
| III.2 | Bản đồ kho trực quan | **Có** | Lưới giá theo hàng/cột, tô mức lấp đầy; số bản đếm lại từ bảng ấn phẩm |
| III.2 | In mã vạch — CODE39, CODE128, QR | **Có** | ZXing sinh ma trận, SkiaSharp đóng PNG ở 300 dpi; chọn theo danh sách, theo khoảng ĐKCB hoặc theo bộ lọc |
| III.2 | In nhãn gáy sách | **Có** | Cùng trình thiết kế, ký hiệu xếp giá tách sẵn thành ba dòng; khối logo thư viện đặt theo milimét, ảnh lấy từ tham số `LIBRARY.LOGO_URL` lúc in |
| III.2 | Mẫu tem, xem trước, xuất PDF đúng khổ tờ tem | **Có** | Số cột, số hàng, lề trên và lề trái lấy đúng từ mẫu; màn hình chặn mẫu vượt khổ A4. Xem trước là ảnh mô phỏng một tem (5 điểm ảnh/mm) với mã vạch thật do máy chủ dựng và logo thật — trong trình thiết kế với dữ liệu mẫu, trong hộp in với ấn phẩm đầu tiên đang chọn |
| III.2 | Báo cáo bổ sung, ĐKCB hủy bỏ, tổng quát, tổng hợp (pivot) | **Có** | Bốn báo cáo, đều xuất được Excel và PDF từ đúng bộ lọc đang xem; báo cáo tổng quát có ba biểu đồ (kho, dạng tài liệu, tình trạng) đổi được cột/tròn và xuất tệp riêng (`kind=Overview`) |
| III.3 | Thông tin thư viện / cơ sở | **Có** | CRUD kèm địa chỉ, giờ mở cửa, người phụ trách, tọa độ |
| III.3 | Thông tin kho, giá, ngăn, quy tắc ký hiệu | **Có** | CRUD kho và giá; mã giá duy nhất trong phạm vi kho |
| III.4 | Đóng kho khi bắt đầu kiểm kê | **Có** | Kho đóng thì ngưng nhận chuyển kho; trạng thái từng kho hiện ngay trên màn hình kiểm kê |
| III.4 | Kho đóng thì ngưng cho mượn/trả tại kho đó, cảnh báo trên màn hình lưu thông | **Có** | Bổ sung 04/09/2026: quầy từ chối ghi mượn bản thuộc kho đang đóng (cả khi gọi thẳng API, HTTP 409); ghi trả vẫn nhận nhưng báo giữ ở quầy; banner trên Quầy lưu thông liệt kê kho đang đóng. Kịch bản LT.30–LT.31 |
| III.4 | Tạo kỳ kiểm kê, snapshot danh sách kỳ vọng | **Có** | Phạm vi toàn kho / theo khoảng ĐKCB / theo dạng tài liệu; danh sách chốt ngay lúc tạo kỳ |
| III.4 | Phân công cán bộ cho kỳ kiểm kê | **Có** | Bổ sung 04/09/2026: phân công theo tài khoản, người được phân công nhận thông báo và tra được kỳ của mình. Sửa được giữa kỳ. Vẫn ghi thêm được tên người ngoài danh sách tài khoản để in lên biên bản |
| III.4 | Quét barcode liên tục, phản hồi khớp / thừa / sai kho | **Có** | Ô quét giữ tiêu điểm sau mỗi lần quét; nhật ký quét hiện ngay bên dưới |
| III.4 | Nhập tệp quét từ máy đọc rời | **Có** | Mỗi dòng một mã, hoặc CSV lấy cột đầu |
| III.4 | Tiến độ realtime | **Có** | Đếm số bản kỳ vọng đã quét; mã lạ không đẩy tiến độ vượt tổng; kỳ đang chạy tự nạp lại tiến độ mỗi 5 giây để thấy cả lượt quét từ máy rời và điện thoại |
| III.4 | Đóng kỳ, đối chiếu, sinh kết quả | **Có** | Chốt kỳ mở lại kho và ra bốn nhóm kết quả |
| III.4 | Báo cáo kết quả, xuất Excel, lập quyết định từ danh sách thiếu | **Có** | Lọc theo nhóm kết quả, xuất Excel, và lập thẳng quyết định ghi mất cho các bản thiếu |
| III.5 | Xếp giá chưa kiểm nhận / trong kho / thanh lý | **Có** | Các thẻ đếm theo trạng thái lọc thẳng danh sách |
| III.5 | Chuyển kho đơn lẻ và hàng loạt, in phiếu, lịch sử | **Có** | Mỗi lần chuyển sinh một số phiếu; hộp chuyển kho có ô quét mã vạch liên tục gom thêm bản ngoài danh sách đã tick; chuyển xong mời in phiếu ngay; ngăn "Phiếu chuyển kho" liệt kê mọi phiếu đã lập kèm nút in lại; lịch sử hiện trên chính bản sách. Quyết định thanh lý cũng in được ngay sau khi lập và từ chi tiết bản đã thanh lý |
| III.5 | Kiểm nhận và mở khóa, khóa lại kèm lý do | **Có** | Chưa kiểm nhận thì không mở khóa được; khóa lại bắt buộc ghi lý do |
| III.6 | Trình thiết kế biểu mẫu dùng chung | **Có** | Một bộ kết xuất cho sáu loại chứng từ; chọn nguồn dữ liệu, cột bảng, khổ giấy, logo, ô ký |
| III.7 | Thống kê theo dạng tài liệu, vật mang tin, thời gian, ngôn ngữ | **Có** | Chín chiều thống kê, nhóm thời gian theo ngày/tháng/quý/năm; đều có bảng, biểu đồ Recharts cột/tròn đủ mọi dòng theo số bản, số đầu hoặc giá trị (màu phân loại, không dùng màu ngữ nghĩa), và xuất Excel/PDF |

---

## B6. Phân hệ IV — Ấn phẩm định kỳ

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| IV.1 | Tìm báo, tạp chí theo tên, ISSN, NXB, kỳ hạn, ngôn ngữ, kho, trạng thái đặt | **Có** | Tìm theo tên bỏ dấu vẫn ra; cột tình trạng nhận số ngay trên danh sách |
| IV.1 | Xem nhanh tình trạng nhận số dạng lưới theo năm, tô màu | **Có** | Lưới các số theo năm, tô theo trạng thái: dự kiến, đã nhận, thiếu, đang khiếu nại, đã đóng tập |
| IV.2 | Nhập mục lục bài trích: nhan đề, tác giả, trang từ–đến, tóm tắt, từ khóa | **Có** | Bảng nhập trực tiếp trên bàn làm việc của từng số |
| IV.2 | Sinh biểu ghi MARC riêng cho bài trích, liên kết ấn phẩm mẹ qua trường 773 | **Có** | Leader vị trí 07 = 'a', trường 773 mang $t tên tạp chí, $g định vị số và trang, $x ISSN. Biểu ghi được xuất bản ngay nên bạn đọc tra được bằng chính tên bài — trước 04/09/2026 nó ở trạng thái Nháp và trang tra cứu không thấy |
| IV.2 | Import mục lục từ Excel | **Có** | Tệp mẫu có sheet hướng dẫn; dòng lỗi báo theo đúng số dòng trong tệp |
| IV.3 | Sinh số: chọn nhiều đầu báo, chọn khoảng thời gian | **Có** | Mỗi đầu báo sinh theo kỳ hạn của chính nó; số đã có được bỏ qua nên chạy lại không nhân đôi |
| IV.3 | Ghi nhận: bảng các số đến hạn, tick nhận hàng loạt | **Có** | Màn hình Ấn phẩm định kỳ › Bổ sung tổng thể liệt kê số đến hạn của mọi đầu báo trong một bảng; tick nhận hàng loạt, số lượng và ngày nhận nhập riêng từng dòng; kho để trống thì mỗi số vào kho của đầu báo nó |
| IV.3 | Kiểm tra: đối chiếu dự kiến với đã nhận, liệt kê số thiếu | **Có** | Tab "Đối chiếu số thiếu" (bộ lọc `unresolvedOnly`: quá hạn, đã ghi thiếu, đang khiếu nại) gom theo đầu báo với số cũ nhất chưa về; đánh dấu thiếu hàng loạt |
| IV.3 | Tạo phiếu khiếu nại gửi nhà cung cấp | **Có** | Lập cho nhiều số của nhiều đầu báo một lần, mỗi số một phiếu gửi tới nhà cung cấp của đầu báo ấy; sinh số phiếu tự động, nội dung soạn sẵn kèm tên số và ngày phát hành dự kiến; ghi nhận phản hồi, hủy khiếu nại thì số quay lại danh sách thiếu |
| IV.4 | Phân kho: chọn kho, giá, ký hiệu xếp giá cho đầu báo | **Có** | Khai ngay trên form đầu báo; số nhận về lấy mặc định từ đây |
| IV.4 | Định kỳ: dạng chu kỳ, số kỳ/năm, ngày phát hành, quy tắc đánh số, năm và số bắt đầu, kỳ nghỉ | **Có** | Mười kỳ hạn; ngày phát hành theo thứ trong tuần hoặc ngày trong tháng; ba cách đánh số. Kỳ nghỉ khai được theo tháng, theo **thứ trong tuần** (nhật báo nghỉ Chủ nhật) và theo **khoảng ngày** lặp hằng năm hoặc riêng một năm, đủ cho kỳ nghỉ Tết (bổ sung 04/09/2026) |
| IV.4 | Sinh số theo cấu hình, cho sửa tay từng số trước khi chốt | **Có** | Bước xem trước không ghi gì vào cơ sở dữ liệu; sửa được số, tập và ngày của từng dòng rồi mới chốt |
| IV.4 | Ghi nhận từng số: ngày nhận, số lượng, sinh barcode, ghi vào kho | **Có** | Mỗi bản nhận về thành một ĐKCB thật có mã vạch, vào kho ở trạng thái cho mượn ngay |
| IV.4 | Kiểm tra: lưới tình trạng, đánh dấu số thiếu, tạo khiếu nại | **Có** | Cùng bàn làm việc, không phải chuyển màn hình |
| IV.4 | Đóng tập: chọn khoảng số, sinh ĐKCB mới, số lẻ chuyển "đã đóng tập" | **Có** | Chọn năm rồi "từ số → đến số" (thứ tự theo ngày phát hành, số không có trong năm bị chặn), màn hình báo trước sẽ đóng những số nào; tập đóng là một ấn phẩm mới có mã vạch và ký hiệu xếp giá riêng; số lẻ giữ nguyên trong sổ nhận số để đối chiếu khi kiểm kê |
| IV.4 | In nhãn gáy tập | **Có** | Nút "In nhãn gáy tập" ngay trên dòng tập đã đóng, dùng dịch vụ in nhãn của Phân hệ III với ĐKCB của tập |
| IV.4 | Tổng hợp tình hình nhận số theo năm | **Có** | Bảng số kỳ dự kiến, đã nhận, thiếu, đã đóng tập, tỷ lệ nhận và giá trị |
| IV.5 | Báo cáo tổng hợp (số đầu báo, số kỳ đã nhận, giá trị) | **Có** | Chiều "Tổng hợp" |
| IV.5 | Báo cáo theo môn loại (DDC) | **Có** | Gộp theo lớp trăm của DDC, kèm tên lớp bằng tiếng Việt |
| IV.5 | Báo cáo theo mức định kỳ | **Có** | Chiều "Mức định kỳ" |
| IV.5 | Báo cáo theo ngôn ngữ | **Có** | Chiều "Ngôn ngữ"; thêm hai chiều nhà cung cấp và kho để quyết toán tiền đặt báo |
| IV.5 | Báo cáo có bảng, đồ họa và xuất PDF/Excel | **Có** | Bảng kèm thanh tỷ trọng; xuất Excel và PDF từ đúng bộ lọc đang xem |

---

## B7. Phân hệ VI — Bạn đọc

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| VI.1 | Danh sách: tìm theo số thẻ, mã SV, họ tên, CCCD, email, điện thoại | **Có** | Một ô tìm kiếm nhận mọi thứ cán bộ có trong tay; họ tên gõ không dấu vẫn ra |
| VI.1 | Lọc theo loại bạn đọc, khoa, ngành, lớp, khóa, trạng thái thẻ | **Có** | Thêm bộ lọc tình trạng: thẻ hết hạn, sắp hết hạn, còn nợ phí, đang giữ tài liệu, chưa từng mượn |
| VI.1 | Thêm/sửa đầy đủ trường của mục 4.7 | **Có** | Form một màn hình, kiểm tra trùng số thẻ và mã sinh viên ngay khi lưu |
| VI.1 | Upload ảnh có cắt ảnh, chụp ảnh từ webcam | **Có** | Cắt theo khung 3×4 ngay trên màn hình: kéo ảnh, phóng to, rồi mới tải lên; chụp thẳng từ webcam qua trình duyệt. Ảnh được kiểm tra bằng chữ ký nhị phân nên đổi đuôi tệp không qua được |
| VI.1 | Sinh số thẻ tự động theo quy tắc cấu hình | **Có** | Lấy tiền tố, độ dài và cách đánh lại số theo năm từ Tham số hệ thống |
| VI.1 | Tab lịch sử: đang mượn, lịch sử mượn trả, tiền phạt, vi phạm, lượt vào thư viện, tài liệu số | **Có** | Sáu tab trong hồ sơ; số liệu đọc thẳng từ sổ mượn, sổ phạt, sổ ra vào và nhật ký tài liệu số |
| VI.1 | Gia hạn thẻ đơn lẻ và hàng loạt theo bộ lọc | **Có** | Thẻ còn hạn thì cộng tiếp vào hạn cũ, thẻ đã hết hạn thì tính từ hôm nay; danh sách hàng loạt dựng lại ở máy chủ từ chính bộ lọc đang xem |
| VI.1 | Tạm khóa / mở khóa kèm lý do | **Có** | Bắt buộc ghi lý do khi khóa; khóa có thời hạn hoặc khóa tới khi mở lại |
| VI.1 | Cấp lại thẻ, giữ lịch sử thẻ cũ | **Có** | Thẻ cũ chuyển sang "đã thu hồi" và vẫn nằm trong sổ cấp thẻ, vì sổ mượn trả cũ ghi theo số thẻ cũ; thẻ hỏng thì giữ nguyên số, thẻ mất thì cấp số mới |
| VI.1 | Cấp lại thẻ từ giao diện: nút, hộp thoại lý do, lịch sử thẻ cũ/mới | **Có** | Bổ sung 04/09/2026: trước đó chỉ có API. Hộp thoại phân biệt thẻ mất (số mới) và thẻ hỏng (giữ số); tab "Thẻ đã cấp" hiện cả thẻ thu hồi lẫn thẻ mới. Kịch bản BD.51 |
| VI.1 | Chuyển trạng thái ra trường hàng loạt theo khóa | **Có** | Lọc theo khóa rồi áp dụng cho toàn bộ kết quả |
| VI.1 | Kiểm tra công nợ trước khi cho ra trường | **Có** | Còn tài liệu chưa trả hoặc còn nợ phí thì bị giữ lại kèm lý do từng người; có màn hình xác nhận công nợ cho giấy xác nhận trả sách |
| VI.2 | Thiết kế mẫu thẻ: kéo thả, mặt trước và mặt sau | **Có** | Kéo trực tiếp từng ô trên khung xem trước đúng khổ thẻ, hoặc gõ số milimét cho chuẩn |
| VI.2 | Khổ CR80 (85,6 × 54 mm) hoặc tùy chỉnh | **Có** | Mẫu CR80 nạp sẵn khi cài đặt, in được ngay từ ngày đầu |
| VI.2 | Đặt ảnh nền, logo, ảnh bạn đọc, các trường thông tin, mã vạch/QR số thẻ | **Có** | Màu nền và dải màu đầu thẻ, ô logo lấy từ tham số hệ thống, ô ảnh bạn đọc, 18 trường thông tin, mã vạch Code 128 / Code 39 / QR |
| VI.2 | In hàng loạt theo bộ lọc, xem trước, xuất PDF đúng khổ | **Có** | Hai kiểu in: mỗi thẻ một trang đúng khổ cho máy in thẻ nhựa, hoặc xếp nhiều thẻ trên tờ A4 để cắt; trang mặt sau đảo thứ tự cột để lật giấy in hai mặt là khớp |
| VI.2 | Đếm số lần in mỗi thẻ | **Có** | Xem trước không tính là một lần in |
| VI.2 | Xem trước thẻ trên màn hình in hàng loạt và trong hồ sơ | **Có** | Bổ sung 04/09/2026: nút "Xem trước (không tính lần in)" gửi `preview: true`; phép thử `Previewing_a_card_does_not_count_as_a_print` giữ đúng số lần in. Kịch bản BD.54 |
| VI.3 | CRUD loại bạn đọc kèm chính sách, thời hạn thẻ, phí thẻ | **Có** | Danh mục loại bạn đọc có hạn thẻ, phí làm thẻ, tiền đặt cọc |
| VI.3 | CRUD khoa, ngành, lớp, khóa học, loại vi phạm | **Có** | Năm danh mục, đủ nhập xuất Excel và gộp trùng như mọi danh mục khác |
| VI.4 | Import Excel: tệp mẫu, ánh xạ cột, validate, bảng lỗi, chạy nền, log lỗi | **Có** | Tệp mẫu có sheet hướng dẫn từng cột; ánh xạ cột lưu lại cho lần sau; bước kiểm tra không ghi gì vào hệ thống; nhập chạy nền có thanh tiến độ; nhật ký lỗi tải về dạng Excel để sửa rồi nhập lại |
| VI.4 | Bảng lỗi sửa được tại chỗ rồi nhập lại | **Có** | Bổ sung 04/09/2026: trước đó bảng lỗi chỉ đọc. Lưới sửa nguyên ô của dòng lỗi, "Kiểm tra lại" và "Nhập các dòng đã sửa" qua `POST /api/readers/import/rows` — cùng bộ xử lý với nhập tệp nên luật kiểm tra chỉ có một. Kịch bản BD.52 |
| VI.4 | Xử lý trùng mã sinh viên | **Có** | Ba cách: báo lỗi, bỏ qua, hoặc cập nhật hồ sơ đang có — nhập lại danh sách lớp đã sửa không sinh hồ sơ trùng |
| VI.4 | Import ảnh hàng loạt từ ZIP đặt tên theo mã SV | **Có** | Khớp theo mã sinh viên rồi tới số thẻ; ảnh không khớp hoặc không phải ảnh được liệt kê riêng |
| VI.4 | Export danh sách bạn đọc ra Excel theo bộ lọc | **Có** | Xuất đúng bộ lọc đang xem, có ghi nhật ký ai xuất và bao nhiêu hồ sơ |
| VI.4 | Đồng bộ từ hệ thống quản lý đào tạo qua API, cấu hình mapping | **Có** | Endpoint `POST /api/readers/sync` nhận dữ liệu theo tên trường của phía đào tạo; bảng ánh xạ khai được trên giao diện; khớp theo mã sinh viên nên gọi lại là cập nhật chứ không nhân đôi; có chế độ chạy thử |
| VI.4 | Màn hình đồng bộ từ hệ thống đào tạo: ánh xạ trường, dán/tải JSON, thử rồi ghi | **Có** | Bổ sung 04/09/2026: trước đó chỉ có API và bảng ánh xạ; nay thao tác trọn trên màn hình Nhập xuất dữ liệu. Kịch bản BD.53 |
| VI.5 | Số lượng bạn đọc theo loại / khoa / ngành / khóa / trạng thái, bảng và biểu đồ | **Có** | Bảy chiều thống kê (thêm lớp và giới tính), bảng có dòng tổng cộng và biểu đồ tròn |
| VI.5 | Bạn đọc mới đăng ký theo thời gian | **Có** | Gộp theo ngày, tháng, quý hoặc năm; biểu đồ đường kèm cột cộng dồn |
| VI.5 | Thẻ sắp hết hạn / đã hết hạn | **Có** | Ba con số tổng quan và danh sách cần nhắc gia hạn kèm số ngày còn lại; bạn đọc đã ra trường không nằm trong danh sách nhắc |
| VI.5 | Bạn đọc chưa từng mượn / bạn đọc tích cực | **Có** | Hai mặt của cùng một báo cáo, có biểu đồ cột cho nhóm mượn nhiều nhất |
| VI.5 | Báo cáo xuất được ra PDF/Excel | **Có** | Cả bốn báo cáo, xuất đúng bộ lọc đang hiển thị |

---

## B8. Phân hệ VII — Lưu thông

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| VII.1 | Ma trận chính sách: Loại bạn đọc × Dạng tài liệu × Kho | **Có** | Một chính sách khai được cả ba chiều hoặc bỏ trống chiều nào không cần; màn hình có ô thử để chọn ba chiều và xem ngay chính sách nào thắng |
| VII.1 | Số lượng mượn tối đa, số ngày mượn, số lần và số ngày gia hạn | **Có** | Sáu chính sách nạp sẵn khi cài đặt: sinh viên 3 bản/14 ngày, học viên 5/21, nghiên cứu sinh 7/30, giảng viên 10/60, cán bộ 5/30, khách 2 bản đọc tại chỗ |
| VII.1 | Tiền phạt/ngày quá hạn, số ngày ân hạn | **Có** | Ngày ân hạn không tính tiền; ngày nghỉ trong khoảng quá hạn cũng không tính, vì thư viện đóng cửa thì bạn đọc không trả được |
| VII.1 | Số đặt giữ tối đa, số ngày giữ chỗ, cho phép mượn / gia hạn / đặt giữ | **Có** | Ba công tắc riêng trên từng chính sách; loại "khách" tắt mượn về nhà nên chỉ đọc tại chỗ |
| VII.1 | Độ ưu tiên khi nhiều chính sách cùng khớp | **Có** | Xếp theo độ ưu tiên khai tay trước, rồi tới chính sách khai cụ thể hơn (nhiều chiều hơn), cuối cùng mới tới tên — nên không bao giờ có hai chính sách hòa nhau |
| VII.1 | Lịch nghỉ lễ; hạn trả rơi vào ngày nghỉ đẩy sang ngày làm việc kế tiếp | **Có** | Ngày nghỉ khai một lần cho mọi năm (Tết dương, Quốc khánh…) hoặc khai riêng từng năm; thêm ngày nghỉ hằng tuần lấy từ tham số `CIRCULATION.WEEKLY_CLOSED_DAYS` (mặc định Chủ nhật) |
| VII.1 | Không tính phạt ngày nghỉ | **Có** | Cùng một bộ lịch dùng cho cả tính hạn trả lẫn đếm ngày phạt |
| VII.2 | Ghi mượn: quét thẻ hiện thông tin, ảnh, số sách đang mượn, cảnh báo | **Có** | Ô quét nhận cả số thẻ lẫn mã sinh viên; cảnh báo chia hai loại — loại chặn hẳn (thẻ hết hạn, đang khóa, nợ phí quá ngưỡng, đủ hạn mức) và loại chỉ nhắc |
| VII.2 | Quét barcode ĐKCB liên tục, mỗi lần quét kiểm tra chính sách | **Có** | Mỗi lần quét gọi máy chủ kiểm tra và trả về hạn trả đã tính sẵn; quét trùng trong cùng lượt bị chặn; bản đang có người mượn hoặc đang giữ cho người khác cũng bị chặn kèm lý do |
| VII.2 | Hoàn tất và in phiếu mượn | **Có** | Phiếu mượn, phiếu trả, biên lai phạt và giấy xác nhận trả sách dùng chung trình thiết kế biểu mẫu của Phase 6, bốn mẫu nạp sẵn khi cài đặt |
| VII.2 | Thao tác bằng bàn phím và máy quét, không cần chuột | **Có** | F2 về ô quét thẻ, F3 về ô quét mã vạch, F4 hoàn tất, Esc bỏ lượt; con trỏ tự về ô quét sau mỗi lần thành công |
| VII.2 | Phản hồi bằng âm thanh thành công / lỗi | **Có** | Hai tiếng bíp khác cao độ dựng bằng Web Audio, không cần tệp âm thanh nên không phụ thuộc mạng |
| VII.2 | Ghi trả: hiện thông tin mượn, tính tiền phạt nếu quá hạn | **Có** | Quét mã vạch là trả, không cần quét thẻ trước; tiền phạt tính ngay và lập thành khoản thu |
| VII.2 | Có người đặt giữ thì cảnh báo giữ sách và báo cho người đặt | **Có** | Bản trả về được gán thẳng cho người đầu hàng đợi, chuyển sang trạng thái "Đặt giữ" và gửi thông báo; màn hình hiện rõ đang giữ cho ai |
| VII.2 | Gia hạn: kiểm tra số lần, người đặt giữ, quá hạn | **Có** | Ba điều kiện kiểm ở máy chủ; hạn mới tính từ hôm nay chứ không nối vào hạn cũ, và không vượt quá hạn thẻ |
| VII.2 | Đặt giữ theo biểu ghi hoặc theo ĐKCB, xếp hàng đợi, thông báo khi có sách | **Có** | Hàng đợi tự đánh lại số khi có phiếu bị hủy hoặc đã nhận; phiếu quá hạn nhận tự hết hiệu lực bằng tác vụ nền hằng ngày |
| VII.2 | Thu tiền phạt, in biên lai, miễn giảm kèm lý do và quyền hạn | **Có** | Thu được nhiều lần cho tới khi hết nợ; miễn giảm bắt buộc ghi lý do và đòi quyền riêng `CIRCULATION.FINE.WAIVE`; biên lai in ra có dòng số tiền bằng chữ |
| VII.2 | Ghi nhận ra / vào thư viện bằng quét thẻ tại cổng | **Có** | Một máy quét dùng chung: lần quét đầu là vào, lần sau là ra, cán bộ không phải bấm chọn |
| VII.3 | Sơ đồ tủ trực quan theo khu vực, màu theo trạng thái | **Có** | 20 tủ nạp sẵn chia hai khu A và B; bấm thẳng vào ô tủ để giao hoặc nhận lại |
| VII.3 | Giao tủ: quét thẻ, chọn tủ trống, giao chìa / mã | **Có** | Một bạn đọc chỉ giữ một tủ tại một thời điểm |
| VII.3 | Trả tủ bằng quét thẻ hoặc nhập số tủ | **Có** | Cả hai cách đều nhận |
| VII.3 | Cảnh báo tủ quá giờ chưa trả, báo hỏng tủ | **Có** | Ngưỡng quá giờ lấy từ tham số `CIRCULATION.LOCKER_MAX_HOURS`; ô tủ quá giờ viền đỏ; tủ hỏng khóa lại không giao được |
| VII.4 | Thiết kế mẫu phiếu mượn, phiếu trả, biên lai phạt, giấy xác nhận trả sách | **Có** | Dùng lại trình thiết kế biểu mẫu chung thay vì làm bộ mẫu riêng — quyết định ghi ở `docs/00-quyet-dinh-ky-thuat.md` |
| VII.4 | Giấy xác nhận trả sách in từ hồ sơ bạn đọc, chặn khi còn nợ | **Có** | Bổ sung 04/09/2026: nút "In giấy xác nhận" trong hồ sơ; còn tài liệu hay nợ phí thì nút khóa kèm lý do; lối in riêng `GET /api/readers/{id}/clearance/print` chỉ đòi quyền xem bạn đọc. Kịch bản BD.55 |
| VII.4 | Chọn mẫu mặc định, in trực tiếp hoặc xuất PDF | **Có** | In thẳng từ màn hình quầy ngay sau khi ghi mượn / ghi trả |
| VII.5.1 | Báo cáo bạn đọc ra vào thư viện, biểu đồ giờ cao điểm | **Có** | Chia 24 khung giờ; lọc theo loại bạn đọc và cổng |
| VII.5.2 | Báo cáo bạn đọc đang mượn sách trong thư viện | **Có** | Danh sách hiện tại kèm số ngày còn lại của từng phiếu |
| VII.5.3 | Báo cáo lịch sử bạn đọc mượn sách | **Có** | Tra theo một bạn đọc hoặc theo khoảng thời gian |
| VII.5.4 | Báo cáo mượn quá hạn kèm số ngày, tiền phạt dự kiến, gửi email nhắc hàng loạt | **Có** | Chia bốn mức trễ (1–7, 8–30, 31–90, trên 90 ngày); nút gửi nhắc hàng loạt gửi đúng những phiếu đang lọc |
| VII.5.5 | Báo cáo sử dụng tủ đựng đồ (tần suất, thời lượng trung bình) | **Có** | Kèm số lượt quá giờ theo từng tủ để biết tủ nào hay bị giữ lâu |
| VII.5.6 | Thống kê bạn đọc mượn tài liệu nhiều nhất | **Có** | Chọn số lượng đứng đầu, lọc theo loại bạn đọc và khoa |
| VII.5.7 | Thống kê ấn phẩm được mượn nhiều nhất | **Có** | Lọc theo dạng tài liệu, kho và môn loại |
| VII.5 | Mỗi báo cáo có bảng, biểu đồ, xuất PDF và Excel | **Có** | Bảy báo cáo đều đủ ba dạng đầu ra, xuất đúng bộ lọc đang xem |
| XI.4 | Nhóm `/api/reader/*` phần lưu thông | **Có** | Đăng nhập bằng số thẻ, thẻ điện tử, sách đang mượn, lịch sử, xin gia hạn, đặt giữ và hủy đặt giữ, tra tiền phạt, mượn tự phục vụ — mỗi endpoint đều có kiểm thử tích hợp |
| XI.2 | Mượn tự phục vụ có xác thực vị trí | **Có** | Tắt sẵn khi cài đặt; bật bằng `CIRCULATION.SELF_CHECKOUT_ENABLED`; ba cách xác thực vị trí theo `CIRCULATION.SELF_CHECKOUT_VERIFY_MODE`: không kiểm, Wi-Fi thư viện, hoặc quét mã QR trạm mượn ký bằng khoá của thư viện (màn hình Lưu thông → Trạm mượn in được mã); phiếu xác thực có hạn 15 phút; kiểm thử tích hợp cho cả hai chế độ và bảy mã lỗi |

---

## B9. Phân hệ V — Tài liệu số

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| V.1 | Cây bộ sưu tập phân cấp (Giáo trình, Luận văn, Luận án, Đề tài NCKH, Bài giảng…) | **Có** | Sáu nhánh gốc nạp sẵn khi cài đặt, thêm bớt và lồng nhau tùy ý; hệ thống chặn việc kéo một nhánh xuống dưới chính nhánh con của nó |
| V.1 | Upload PDF, DOCX, EPUB, MP4, MP3, ảnh | **Có** | Kiểu tệp nhận bằng chữ ký nhị phân chứ không bằng phần mở rộng, nên đổi đuôi tệp không qua được |
| V.1 | Upload nhiều tệp, upload theo mảnh cho tệp lớn, hiển thị tiến trình, tiếp tục khi gián đoạn | **Có** | Tệp lớn được cắt thành mảnh; mỗi lần gửi xong máy chủ trả về danh sách mảnh đã nhận nên đứt mạng thì gửi tiếp phần còn thiếu. Gửi lại một mảnh đã có không làm hỏng phiên |
| V.1 | Gắn tài liệu số vào biểu ghi thư mục, một biểu ghi nhiều tệp | **Có** | Gắn khi tải lên hoặc sửa lại sau; nhập hàng loạt tự khớp theo số ĐKCB, số kiểm soát 001 hoặc ISBN |
| V.1 | Tự trích số trang, sinh thumbnail trang bìa, tính checksum SHA-256 | **Có** | Chạy nền ngay sau khi tải lên nên người tải không phải ngồi chờ; mã kiểm tra dùng để đối chiếu khi bàn giao dữ liệu |
| V.1 | Tạo bản preview N trang đầu | **Có** | Số trang xem thử khai theo từng tài liệu, mặc định lấy từ `DIGITAL.PREVIEW_PAGES`; giới hạn được kiểm ở máy chủ chứ không phải ở giao diện |
| V.1 | OCR văn bản (Tesseract, tiếng Việt) để tìm kiếm toàn văn | **Có** | Tệp có sẵn lớp chữ thì rút thẳng; tệp quét thì nhận dạng bằng Tesseract tiếng Việt cài trong máy chủ, giới hạn số trang để một cuốn dày không chiếm máy hàng giờ |
| V.1 | Đặt mức truy cập Công khai / Nội bộ / Hạn chế / Cấm | **Có** | Bốn mức đúng như đặc tả; tài liệu mới lấy mức mặc định theo bộ sưu tập chứa nó |
| V.1 | Cấu hình cho tải về, cho in, số trang xem thử, bật watermark | **Có** | Bốn công tắc trên từng tài liệu |
| V.1 | Trình đọc trực tuyến trên trình duyệt | **Có** | Lật trang bằng nút hoặc phím mũi tên, phóng to thu nhỏ, hiện rõ đang được đọc toàn văn hay chỉ xem thử |
| V.1 | Chặn tải và in bằng cách stream từng trang dạng ảnh | **Có** | Nội dung không bao giờ đi xuống trình duyệt dưới dạng tệp: mỗi trang là một ảnh do máy chủ kết xuất, nên máy bạn đọc không có tệp gốc để lưu lại |
| V.1 | Watermark động (tên bạn đọc, thời gian, IP) trên từng trang | **Có** | Chữ chìm lát kín cả trang theo đường chéo nên cắt một khúc ảnh vẫn còn dấu vết |
| V.1 | Tìm kiếm toàn văn trong nội dung tài liệu số | **Có** | Gõ không dấu vẫn ra kết quả, kèm đoạn trích quanh chỗ khớp để biết vì sao tài liệu này ra |
| V.2 | Bạn đọc gửi yêu cầu từ OPAC/Mobile kèm lý do sử dụng | **Có** | `POST /api/reader/digital/{id}/request`, bắt buộc ghi lý do; gửi trùng khi lần trước còn treo thì bị chặn |
| V.2 | Cán bộ xem danh sách chờ duyệt kèm thông tin bạn đọc và tài liệu | **Có** | Hàng đợi xếp phiếu chờ duyệt lên trước, hiện cả loại bạn đọc và khoa để cán bộ có căn cứ xét |
| V.2 | Duyệt: đặt thời hạn truy cập, số lần xem tối đa, có cho tải không | **Có** | Ba tham số ngay trên hộp duyệt; lời duyệt cho tải thắng chính sách chung của tài liệu, vì đó là quyết định riêng cho bạn đọc này |
| V.2 | Từ chối kèm lý do | **Có** | Bắt buộc ghi lý do, bạn đọc nhìn thấy đúng lý do đó khi mở lại tài liệu |
| V.2 | Tự động gửi email/thông báo cho bạn đọc | **Có** | Gửi ngay sau khi duyệt hoặc từ chối, kèm thời hạn và số lượt xem được cấp |
| V.2 | Quyền truy cập tự hết hạn theo thời hạn đã đặt | **Có** | Tác vụ nền chạy hằng ngày đóng các quyền hết hạn; hết lượt xem cũng tự khép lại. Cán bộ thu hồi tay được kèm lý do |
| V.2 | Nhật ký truy cập chi tiết: ai xem, tài liệu nào, trang nào, thời điểm, IP | **Có** | Ghi cả lần mở tài liệu lẫn từng trang lật; lọc theo tài liệu, bạn đọc, hành động và khoảng thời gian |
| V.3 | Import hàng loạt từ tệp nén, khớp tệp với biểu ghi theo tên tệp hoặc mã | **Có** | Khớp lần lượt theo số ĐKCB, số kiểm soát 001 rồi ISBN; nút "Kiểm tra trước" chạy đúng đường đi thật nhưng không ghi gì vào hệ thống |
| V.3 | Export metadata (Excel / Dublin Core) kèm file, đóng gói ZIP | **Có** | Gói gồm thư mục `files/` chứa tệp gốc và `metadata/` chứa danh mục Excel cùng tệp Dublin Core đúng không gian tên chuẩn Gói còn có `metadata/marcxml.xml` (bổ sung 04/09/2026). |
| V.3 | Xuất được toàn bộ dữ liệu khi kết thúc hợp đồng (mục 4 E-HSMT) | **Có** | Bỏ trống bộ lọc là xuất cả kho; phần biểu ghi MARC xuất từ Phân hệ II (ISO 2709 và MARCXML), phần bạn đọc và giao dịch xuất từ các phân hệ tương ứng |
| V.4 | Số lượng tài liệu theo bộ sưu tập, định dạng, mức truy cập | **Có** | Bảng và biểu đồ, kèm số tài liệu đã tìm được toàn văn |
| V.4 | Lượt xem / lượt tải theo thời gian, theo tài liệu, theo bạn đọc | **Có** | Gộp theo ngày, tháng, quý hoặc năm; hai bảng xếp hạng tài liệu và bạn đọc |
| V.4 | Dung lượng lưu trữ đã dùng | **Có** | Tách riêng dung lượng bản gốc và bản dẫn xuất để biết chi phí thật của kho |
| V.4 | Thống kê yêu cầu truy cập hạn chế, thời gian xử lý trung bình | **Có** | Tổng, chờ duyệt, đã duyệt, từ chối, hết hạn và số giờ xử lý trung bình |
| V.4 | Báo cáo có bảng, biểu đồ, xuất PDF và Excel | **Có** | Cả bốn báo cáo, xuất đúng bộ lọc đang hiển thị, có ghi nhật ký ai xuất |
| XI.4 | Nhóm `/api/reader/digital/*` | **Có** | Danh sách tài liệu xem được, chi tiết kèm quyền của chính người gọi, mở trình đọc, xem từng trang, tải về, gửi yêu cầu, tra trạng thái yêu cầu và lịch sử xem — mỗi endpoint đều có kiểm thử tích hợp |

### B14. Phân hệ XI — Ứng dụng di động (Flutter, `mobile/`)

| Mã | Yêu cầu E-HSMT | Đáp ứng | Tên chức năng / chứng minh |
|---|---|---|---|
| XI.1.1 | Tra cứu cơ bản, nâng cao, theo ISBN, quét mã vạch, quét QR | **Có** | Màn hình Tra cứu (phạm vi, gợi ý, gõ không dấu, facet, sắp xếp, nâng cao VÀ/HOẶC/KHÔNG), màn hình Quét mã nhận ISBN-10/13, ĐKCB, QR (mobile_scanner) kèm nhập tay — MB.10, MB.11, MB.30, MB.31 |
| XI.1.2 | Duyệt theo chủ đề, đề mục, tác giả | **Có** | Duyệt danh mục: cây chủ đề và phân loại DDC bung từng cấp, tác giả A–Z, bộ sưu tập, lọc tại chỗ không dấu — MB.15 |
| XI.1.3 | Duyệt theo chuyên ngành, môn học | **Có** | Ngành → môn → tài liệu kèm nhãn giáo trình / tham khảo — MB.15 |
| XI.1.4 | Danh mục luận văn / luận án; ấn phẩm định kỳ | **Có** | Hai danh sách phân trang có ô tìm — MB.15 |
| XI.1.5 | Chi tiết tài liệu, tình trạng sẵn có, vị trí kho | **Có** | Năm thẻ: Thông tin (ISBD), Bản in (kho · giá · tình trạng), Tài liệu số, MARC dạng bảng, Nhận xét; nút đặt giữ đổi theo tình trạng thật — MB.10, MB.12 |
| XI.1.6 | Tin tức, thông tin thư viện, giờ mở cửa, bản đồ | **Có** | Trang chủ, tin tức và trang tĩnh dựng HTML, nút Gọi và Chỉ đường — MB.13, MB.14 |
| XI.2.1 | Đọc tài liệu số trong ứng dụng; tải về vùng ngoại tuyến mã hoá, tự hết hạn | **Có** | Trình đọc trang ảnh máy chủ đóng chữ chìm, tìm trong văn bản, đánh dấu trang; gói AES-256-CBC lưu mã hoá, tự hết hạn — MB.21, MB.22 |
| XI.2.2 | Mượn sách giấy tự phục vụ có xác thực vị trí | **Có** | Xác thực NONE / Wi-Fi / QR trạm rồi quét mã vạch liên tiếp, phiếu tóm tắt; máy chủ kiểm chính sách — MB.19, MB.20 |
| XI.2.3 | Đặt giữ chỗ, vị trí hàng đợi, thông báo khi sẵn sàng | **Có** | Đặt giữ từ chi tiết, thẻ Đặt giữ trong Sách của tôi kèm vị trí và hủy; thông báo HOLD_READY của máy chủ — MB.17, MB.32 |
| XI.2.4 | Gửi yêu cầu tài liệu số hạn chế; lịch sử xem/tải | **Có** | Nút Gửi yêu cầu kèm lý do, thẻ Yêu cầu và Lịch sử — MB.23 |
| XI.2.5 | Lịch sử mượn trả; xin gia hạn | **Có** | Sách của tôi: Đang mượn (gia hạn do máy chủ quyết, hiện đúng câu từ chối), Lịch sử có lọc — MB.16, MB.32 |
| XI.2.6 | Đổi mật mã; gia hạn thẻ | **Có** | Tài khoản: đổi mật khẩu, cập nhật liên hệ; màn hình Thẻ có nút gửi yêu cầu gia hạn và danh sách yêu cầu — MB.18, MB.25 |
| XI.2.7 | Thẻ thư viện điện tử (mã vạch/QR) | **Có** | Mã vạch Code 128 + QR số thẻ, sáng màn hình, đọc được khi mất mạng, thẻ hết hiệu lực ẩn mã — MB.18 |
| XI.2.8 | Thông báo đẩy (FCM) | **Có một phần** | Dịch vụ đẩy đăng ký token, hiện thông báo, mở đúng màn hình; danh sách và cài đặt loại thông báo chạy thật — MB.24. Nhận đẩy thật chưa kiểm vì môi trường phát triển chưa có cấu hình Firebase |
| XI.2.9 | Chế độ ngoại tuyến: kết quả tra cứu gần đây và thẻ điện tử | **Có** | Bản lưu kèm giờ cho thẻ, đang mượn, kết quả tra cứu, cài đặt thư viện; dải "Không có kết nối" — MB.18, MB.28 |
| XI.3.1 | Dùng chung REST API, JWT, tự làm mới token | **Có** | `ApiClient` (Dio) làm mới token qua `/reader/auth/refresh`, hết phiên thì về đăng nhập — MB.09 |
| XI.3.2 | Sáng/tối, cỡ chữ điều chỉnh được | **Có** | Cài đặt lưu trên máy, cỡ chữ nhân với cỡ chữ hệ điều hành; soi tràn chữ ở 160% — MB.27 |
| XI.3.3 | Đồng bộ dữ liệu trung tâm (mục 2.7) | **Có** | Sửa trên web thấy ngay trên ứng dụng; `updatedSince` + `serverTime` cho đồng bộ delta — MB.33 |
| XI.3.4 | Build APK và IPA, hướng dẫn cấu hình endpoint | **Có một phần** | APK và AAB dựng được (`flutter build apk --release`, `appbundle`), ký bằng khoá phát hành trong `key.properties`; **bản iOS dựng được** trên máy Mac của GitHub Actions (`flutter build ios --release --no-codesign`, Xcode 26.3 — `docs/06`, MB.34) và chạy thật trên iPhone Simulator, kể cả ba luồng ghi dữ liệu vào máy chủ thật (MB.35–MB.40); IPA ký để phát hành còn thiếu vì chưa có tài khoản Apple Developer. Endpoint qua `--dart-define` — `mobile/README.md` |

---

## B10. Liên thư viện — Z39.50, SRU và OAI-PMH

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| 3.3a | Z39.50 Client: kết nối TCP tới host:port của server đích | **Có** | Đã tra cứu thật tới máy chủ của Thư viện Quốc hội Mỹ (`lx2.loc.gov:210/LCDB`) và lấy biểu ghi về |
| 3.3a | Encode/decode BER của ASN.1 | **Có** | Bộ mã hóa BER tự viết trong `LibraryConnect.Marc/Z3950`, có kiểm thử vòng tròn cho số nguyên, chuỗi tiếng Việt, định danh đối tượng, phần tử lồng nhau, độ dài dạng ngắn, dạng dài và dạng không xác định |
| 3.3a | Các PDU Init / Search / Present / Close | **Có** | Đủ bốn, đúng thẻ lớp ngữ cảnh mà đặc tả quy định; mỗi con số trên đường truyền đều có một kiểm thử riêng để không tái phạm |
| 3.3a | Query Type-1 (RPN) với bộ thuộc tính Bib-1 | **Có** | Đủ sáu loại thuộc tính trên mỗi mệnh đề, nối được bằng AND / OR / AND-NOT |
| 3.3a | Use attribute 1, 4, 7, 8, 21, 1016 | **Có** | Tác giả 1, nhan đề 4, ISBN 7, ISSN 8, chủ đề 21, bất kỳ 1016 — đúng danh sách đặc tả nêu, thêm nhà xuất bản 1018 |
| 3.3a | Record syntax USMARC / MARC21 | **Có** | Chọn được USMARC, UNIMARC hoặc MARCXML cho từng máy chủ; biểu ghi MARC-8 tự chuyển sang Unicode |
| 3.3a | Cấu hình danh sách server đích trong giao diện | **Có** | Tên, địa chỉ, cổng, cơ sở dữ liệu, tài khoản, bảng mã, cú pháp biểu ghi, thời gian chờ, bật tắt, thứ tự hiển thị |
| 3.3a | Server mẫu Library of Congress để test | **Có** | Nạp sẵn khi cài đặt, cùng một lối SRU dự phòng và một máy chủ của Đại học Yale |
| 3.3a | Nút kiểm tra kết nối | **Có** | Bắt tay đầy đủ rồi tra thử một từ khóa, vì máy chủ mở cổng mà từ chối phiên là chuyện thường; kết quả lưu lại để nhìn danh sách là biết nơi nào hỏng |
| 3.3b | Z39.50 Server: lắng nghe TCP, xử lý Init/Search/Present | **Có** | Chạy như một dịch vụ nền trong chính máy chủ API, tra thẳng vào kho thư mục, trả biểu ghi ISO 2709; `docker compose up` công bố cổng 210 (`Z3950_PORT`, trong container 2100), bật/tắt bằng `ILL.Z3950_SERVER_ENABLED` |
| 3.3b | Cấu hình bật/tắt, giới hạn IP | **Có** | `ILL.Z3950_SERVER_ENABLED`, `ILL.Z3950_SERVER_PORT`, `ILL.Z3950_ALLOWED_IPS`; mặc định tắt vì đây là cổng mở ra ngoài không có mật khẩu |
| 3.3 | SRU/SRW song song làm giải pháp tương đương | **Có** | `/sru?operation=searchRetrieve&version=1.2&query=…&recordSchema=marcxml` đúng như đặc tả ghi; gọi trần trụi thì trả về bản tự khai liệt kê các chỉ mục tra được |
| 3.3 | SRU trả MARCXML | **Có** | Trả được cả MARCXML lẫn Dublin Core; truy vấn CQL sai cú pháp trả về phần chẩn đoán đúng chuẩn chứ không phải lỗi máy chủ |
| 3.3 | Tra cứu được bằng tiếng Việt | **Có** | Thư viện bạn gõ không dấu vẫn ra sách tiếng Việt, dùng chung cơ chế tra cứu không dấu của cả sản phẩm |
| 3.4 | OAI-PMH provider, 6 verb | **Có** | `/oai` hỗ trợ Identify, ListMetadataFormats, ListSets, ListIdentifiers, ListRecords, GetRecord; nhận cả GET lẫn POST như chuẩn đòi |
| 3.4 | Metadata prefix oai_dc và marc21 | **Có** | Cả hai, đúng không gian tên chuẩn |
| 3.4 | resumptionToken phân trang | **Có** | Thẻ mang theo cả điều kiện lọc và có chữ ký, nên lượt sau không phải gửi lại tham số và thẻ bị sửa tay thì bị từ chối |
| 3.4 | Lọc theo from / until | **Có** | Nhận cả dạng ngày lẫn dạng có giờ; không có biểu ghi nào khớp thì trả đúng mã `noRecordsMatch` chứ không phải danh sách rỗng |
| 3.4 | Bộ (set) để lấy riêng từng phần kho | **Có** | Chia theo dạng tài liệu, mã bộ dạng `doctype:MÃ` |
| 3.4 | Harvester: cấu hình nguồn, lịch chạy định kỳ | **Có** | Khai nguồn trên giao diện, có nút hỏi thử xem kho tự khai những gì; tác vụ nền chạy hằng đêm theo `ILL.HARVEST_CRON` |
| 3.4 | Map Dublin Core sang MARC 21 | **Có** | Dựng biểu ghi MARC tối thiểu nhưng hợp lệ, đánh dấu mức mô tả chưa đầy đủ và đưa vào hàng đợi biên mục để cán bộ hiệu đính |
| 3.4 | Chạy lại không tạo bản trùng | **Có** | Nhận ra biểu ghi đã thu về bằng mã định danh của kho gốc; chạy lại thì bỏ qua |
| 3.5 | MARCXML dùng cho SRU và OAI-PMH | **Có** | Cùng một bộ đọc/ghi MARCXML của Phase 4, nên biểu ghi phát ra đọc lại được bằng chính sản phẩm |
| II.7 | Màn hình tra cứu nhiều server đích, tra song song | **Có** | Chọn một hoặc nhiều máy chủ; song song giữa các nơi khác nhau nhưng tuần tự trong cùng một nơi, vì mở hai phiên cùng lúc thì máy chủ bỏ tập kết quả của phiên sau |
| II.7 | Kết quả hiển thị theo từng server | **Có** | Mỗi máy chủ một khối riêng kèm số kết quả và thời gian trả lời; nơi nào hỏng thì báo riêng chỗ đó, các nơi còn lại vẫn có kết quả |
| II.7 | So sánh với biểu ghi đã có trong hệ thống | **Có** | Đối chiếu theo ISBN rồi tới nhan đề, biểu ghi đã có được đánh dấu ngay trên danh sách để khỏi nhập trùng |
| II.7 | Nhập vào hệ thống rồi mở trình soạn MARC để hiệu đính | **Có** | Bỏ số kiểm soát của thư viện bạn, ghi nguồn vào trường 035, rồi mở trình soạn MARC — chưa lưu gì vào kho cho tới khi cán bộ bấm lưu |
| IX.5 | Bạn đọc tra sang thư viện khác từ trang tra cứu | **Có** | Máy chủ khai được cờ "cho bạn đọc tra sang"; trang OPAC dùng đúng cờ này, xem mục B12 |

---

## B11. Phân hệ VIII — Quản trị nội dung

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| VIII.1 | Cấu hình chung: tên thư viện, logo, favicon, ảnh banner, slogan, địa chỉ, điện thoại, email, giờ mở cửa, mạng xã hội | **Có** | Một màn hình "Thông tin trang thư viện" gom cả hai kho cấu hình: tên/địa chỉ/logo nằm ở tham số hệ thống vì cả phần mềm dùng chung, còn khẩu hiệu, giờ mở cửa, mạng xã hội nằm ở bảng riêng của trang tra cứu. Mỗi ô ghi rõ nó được lưu ở đâu |
| VIII.1 | Giờ mở cửa từng cơ sở | **Có** | Bổ sung 04/09/2026: giờ mở cửa nhập theo từng cơ sở ở màn hình Thư viện và API công khai trả về cùng địa chỉ, điện thoại, toạ độ. Chân trang liệt kê theo cơ sở, trang Liên hệ có khối "Các cơ sở" kèm lối chỉ đường riêng; ô chữ tự do trong cấu hình trang còn dùng để ghi ngoại lệ |
| VIII.1 | Quản lý trang tĩnh (Giới thiệu, Nội quy, Hướng dẫn, Liên hệ, Hỏi đáp) | **Có** | Năm trang nạp sẵn khi cài đặt với nội dung viết theo lệ chung của thư viện đại học Việt Nam; thêm, sửa, xóa, đăng/gỡ từ giao diện |
| VIII.1 | Trình soạn thảo WYSIWYG, chèn ảnh/file/bảng/video | **Có** | Thanh công cụ đủ chữ đậm, nghiêng, gạch chân, hai cấp tiêu đề, hai kiểu danh sách, liên kết, ảnh, bảng, khung video nhúng, hoàn tác, và một nút xem thẳng mã HTML. Ảnh tải lên kho đối tượng ngay lúc chọn nên bài viết chỉ chứa đường dẫn Chèn được cả tệp PDF / Word / Excel, nhận dạng bằng chữ ký nhị phân (04/09/2026). |
| VIII.1 | Nội dung soạn thảo phải an toàn (yêu cầu 6.4) | **Có** | HTML được lọc ngay khi lưu, không phải lúc hiển thị: thẻ script, thuộc tính sự kiện, giao thức `javascript:`, thuộc tính style đều bị bỏ; khung video chỉ giữ từ YouTube và Vimeo |
| VIII.1 | Quản lý menu điều hướng: cây menu, link nội bộ/ngoài, icon, hiển thị/ẩn | **Có** | Cây nhiều cấp, đặt được mục cha, thứ tự, cửa sổ mở, bật tắt. Tắt mục cha thì cả nhánh bên dưới ẩn theo; đặt cha nằm dưới chính nó bị chặn để cây không thành vòng |
| VIII.1 | Quản lý banner/slider trang chủ: ảnh, link, thứ tự, thời gian hiển thị | **Có** | Ba vị trí (trình chiếu trang chủ, cột bên, chân trang), khoảng ngày hiển thị, thứ tự, bật tắt; trang chủ chỉ lấy banner đang trong khoảng ngày |
| VIII.1 | Quản lý liên kết website (thư viện bạn, CSDL trực tuyến) | **Có** | Có nhóm hiển thị, mô tả, thứ tự; nạp sẵn bốn liên kết công khai khi cài đặt |
| VIII.2 | CRUD tin: tiêu đề, slug, tóm tắt, nội dung, ảnh đại diện, chuyên mục, thẻ, tin nổi bật, lên lịch xuất bản | **Có** | Đủ; đường dẫn bỏ trống thì tự sinh từ tiêu đề và tự tránh trùng, tóm tắt bỏ trống thì lấy đoạn đầu của bài |
| VIII.2 | Lên lịch xuất bản | **Có** | Đặt mốc tương lai thì bài chỉ hiện trên trang tra cứu khi tới giờ; danh sách quản trị ghi rõ "Hẹn ngày giờ" |
| VIII.2 | Quản lý chuyên mục tin | **Có** | Dùng chung màn hình danh mục của hệ thống (`news-categories`), nên có sẵn nhập xuất Excel và gộp trùng |
| IX.1 | SEO: thẻ meta phía máy chủ | **Có** | Nginx rẽ `/tai-lieu/<id>`, `/tin-tuc/<slug>`, `/trang/<slug>` sang API **khi User-Agent là máy thu thập**; API lấy `index.html` của trang tra cứu qua HTTP nội bộ (có bộ đệm) rồi chèn `<title>`, `description` và Open Graph. Người dùng thật vẫn nhận tệp tĩnh, không thêm chặng nào | Kiểm bằng `curl`: máy thu thập nhận "Thư viện mở cửa thứ Bảy từ tháng 9 – …" cùng `og:description`, người thật nhận "Tra cứu thư viện" |
| IX.2 | Chia sẻ trang chi tiết tài liệu | **Có** | Nút *Chia sẻ*: dùng Web Share API khi trình duyệt có, không thì chép liên kết | Kiểm trên trình duyệt thật ở `/tai-lieu/<id>` |
| VIII.2 | Thư viện ảnh hiển thị trên trang tra cứu | **Có** | Route `/thu-vien-anh` gọi `/api/public/galleries`; chưa có album thì báo rõ thay vì trang trắng | Kiểm trên trình duyệt thật |
| VIII.2 | Quản lý thư viện ảnh (album sự kiện) | **Có** | Album có ngày diễn ra, ảnh bìa, danh sách ảnh sắp xếp được kèm chú thích; ảnh bìa bỏ trống thì lấy ảnh đầu tiên |
| VIII.2 | Thống kê lượt xem tin | **Có** | Tổng số bài, số đã đăng, số bản nháp, tổng lượt xem, phân bổ theo chuyên mục và danh sách bài xem nhiều nhất |
| — | Kiểm duyệt nhận xét bạn đọc | **Có** | Nhận xét gửi từ trang tra cứu vào hàng chờ; duyệt, bỏ duyệt hoặc xóa. Sửa lại nhận xét đã duyệt thì phải duyệt lại |

---

## B12. Phân hệ IX — Tra cứu (OPAC)

| # | Yêu cầu của E-HSMT | Đáp ứng | Thực hiện |
|---|---|---|---|
| IX.1 | Trang thông tin điện tử là SPA riêng, công khai, responsive | **Có** | `frontend-opac` chạy trong container riêng sau Nginx ở đường dẫn gốc; giao diện co giãn từ điện thoại tới màn hình rộng |
| IX.1 | Trang chủ: ô tìm kiếm lớn, banner, sách mới, sách mượn nhiều, tin tức, liên kết nhanh | **Có** | Đủ các khối; ba khối sách mới, sách mượn nhiều và mục tìm ở thư viện khác bật tắt được bằng cấu hình |
| IX.1 | Trang tin tức, trang tĩnh, trang liên hệ | **Có** | Tin tức có lọc theo chuyên mục và tìm kiếm; trang Liên hệ ghép thêm thông tin liên hệ, giờ mở cửa và bản đồ nhúng từ cấu hình |
| IX.1 | SEO: thẻ meta, sitemap.xml, robots.txt | **Có** | `/sitemap.xml` do máy chủ sinh động, liệt kê trang tĩnh, bản tin, tài liệu đã xuất bản và đủ mười một trang duyệt công khai (bổ sung 04/09/2026); `/robots.txt` mở phần tra cứu và chặn khu quản trị cùng các trang cá nhân của bạn đọc |
| IX.2 | Tìm kiếm cơ bản, chọn phạm vi | **Có** | Tám phạm vi: tất cả, nhan đề, tác giả, chủ đề, từ khóa, nhà xuất bản, ISBN/ISSN, ký hiệu xếp giá |
| IX.2 | Gợi ý tự động khi gõ | **Có** | Gợi ý nhan đề, tác giả và chủ đề; chờ tới ký tự thứ hai và có nhịp dừng để một câu mười chữ không thành mười lượt truy vấn |
| IX.2 | **Tìm được cả khi gõ không dấu** | **Có** | Toàn bộ tra cứu đi qua hàm bỏ dấu của cơ sở dữ liệu; "co so du lieu" ra "Cơ sở dữ liệu" |
| IX.2 | Tìm kiếm nâng cao: nhiều điều kiện AND/OR/NOT, chọn trường cho từng điều kiện | **Có** | Tối đa mười mệnh đề, mỗi mệnh đề chọn phạm vi riêng; ghép thành một biểu thức duy nhất để dịch xuống một câu lệnh |
| IX.2 | Tìm kiếm nâng cao lọc theo năm, ngôn ngữ, dạng tài liệu, kho, có tài liệu số | **Có** | Bổ sung 04/09/2026: ba ô chọn ngôn ngữ, dạng tài liệu và kho lấy danh sách từ chính bộ đếm facet của kho, kèm số lượng — bạn đọc chỉ thấy giá trị thật sự có tài liệu |
| IX.2 | Lọc theo năm xuất bản, ngôn ngữ, dạng tài liệu, kho, có tài liệu số | **Có** | Đủ, dùng chung bộ lọc với tìm kiếm cơ bản và với bộ đếm facet |
| IX.2 | Duyệt theo Chủ đề / Đề mục / Tác giả / Phân loại / Bộ sưu tập / Ngành / Môn học | **Có** | Chủ đề và phân loại duyệt theo cây; tác giả duyệt theo chữ cái đầu; ngành mở xuống môn học rồi tới tài liệu của môn, danh sách tài liệu có phân trang. Môn học có thêm nhánh duyệt riêng kèm dải A–Z (bổ sung 04/09/2026). Số đếm ở nhánh cha cộng dồn cả nhánh con |
| IX.2 | Kết quả: phân trang, sắp xếp, bộ lọc facet đếm số lượng | **Có** | Năm cách sắp xếp; bảy nhóm facet đếm trên đúng tập kết quả hiện tại, không phải trên toàn kho |
| IX.2 | Chi tiết: ảnh bìa, ISBD, tóm tắt, chủ đề bấm được, **danh sách ĐKCB kèm trạng thái và vị trí kho** | **Có** | Bảng bản in đặt cột tình trạng lên đầu, kèm ký hiệu xếp giá, kho, giá, thư viện; bản đang có người mượn hiện cả hạn trả dự kiến |
| IX.2 | Nút đặt giữ, xem tài liệu số, xem MARC | **Có** | Đủ ba; biểu ghi MARC hiện dưới dạng đọc được ngay trên trang |
| IX.2 | Xuất trích dẫn APA/MLA/Chicago/BibTeX/RIS/EndNote | **Có** | Đủ sáu kiểu, chép được vào bộ nhớ tạm hoặc tải thành tệp để nạp vào phần mềm quản lý tài liệu tham khảo |
| IX.2 | Tài liệu liên quan | **Có** | Cùng chủ đề trước, không đủ thì lấy thêm cùng nhánh phân loại; chỉ gợi ý tài liệu thư viện phục vụ được |
| IX.2 | Lưu tìm kiếm, đánh dấu yêu thích, giỏ tài liệu, gửi email danh sách | **Có** | Giỏ tài liệu giữ ở máy người dùng nên chưa đăng nhập vẫn gom được; gửi email thì cần tài khoản và thư đi tới đúng địa chỉ trong hồ sơ bạn đọc |
| IX.3 | Bạn đọc đăng nhập bằng số thẻ và mật khẩu | **Có** | Dùng chung `/api/reader/auth/login` với ứng dụng di động đợt sau |
| IX.3 | Trang cá nhân: sách đang mượn kèm hạn trả và nút gia hạn, lịch sử, đặt giữ, tiền phạt, tài liệu số, thông báo | **Có** | Tám thẻ trên một màn hình; mọi con số đều do máy chủ tính, giao diện chỉ hiển thị |
| IX.3 | Đăng ký mượn (đặt giữ) có kiểm tra hạn mức | **Có** | Máy chủ kiểm chính sách lưu thông và trả về vị trí trong hàng đợi |
| IX.3 | Gửi yêu cầu gia hạn sách | **Có** | Nút gia hạn ngay trên dòng sách đang mượn, hết lượt thì nút tự khóa và ghi rõ đã dùng mấy trên mấy lượt |
| IX.3 | Đổi mật khẩu, cập nhật thông tin liên hệ | **Có** | Chỉ ba trường liên hệ; họ tên, mã sinh viên, khoa là dữ liệu nhà trường quản lý nên không cho tự sửa |
| IX.3 | Gia hạn thẻ thư viện | **Có** | Gửi yêu cầu kèm lý do và xem trạng thái xử lý; đang có yêu cầu chờ thì không gửi thêm được |
| IX.4 | Bộ lọc riêng cho tài liệu số | **Có** | Trang riêng liệt kê chính các tài liệu số, kể cả tài liệu không gắn với biểu ghi thư mục nào |
| IX.4 | Xem trước, đọc trực tuyến, tải về theo quyền | **Có** | Trình đọc lật từng trang, mỗi trang là ảnh do máy chủ dựng và đóng chữ chìm tên bạn đọc, thời điểm và địa chỉ máy; tài liệu không cho tải thì không có nút tải |
| IX.4 | Gửi yêu cầu truy cập tài liệu hạn chế | **Có** | Ngay trên trình đọc, kèm ô ghi lý do sử dụng |
| IX.5 | Tab "Tìm ở thư viện khác", tra song song Z39.50/SRU | **Có** | Chỉ tra ở máy chủ cán bộ đã bật cờ cho bạn đọc; đã tra thật tới Thư viện Quốc hội Mỹ qua cả hai lối |
| IX.5 | Kết quả gộp có ghi rõ nguồn | **Có** | Bổ sung 04/09/2026: bảng gộp mọi thư viện đứng trước, xếp theo nhan đề, cột "Nguồn" ghi tên từng nơi. Bảng theo từng máy chủ giữ bên dưới vì nó nói được nơi nào không tra được và mất bao lâu; cuốn nào thư viện mình đã có thì có liên kết mở thẳng sang trang chi tiết |

---

## B13. Phân hệ X — Tài liệu môn học

| # | Yêu cầu E-HSMT | Đáp ứng | Chức năng trong sản phẩm |
|---|---|---|---|
| X.1 | Quản lý ngành: mã ngành, tên, khoa quản lý, bậc đào tạo, mô tả | **Có** | Danh mục → Ngành đào tạo. Ô "Khoa quản lý" là ô chọn lấy thẳng từ danh mục Khoa, không phải gõ tay nên không lệch tên giữa hai bảng |
| X.1 | Import ngành từ Excel | **Có** | Bổ sung 04/09/2026: cột "Khoa quản lý" nhận mã hoặc tên khoa (so sau khi bỏ dấu và bỏ hoa thường); gõ sai thì dòng ấy báo lỗi chứ không im lặng bỏ qua, tên trùng nhau thì đòi gõ mã. Áp cho mọi cột tham chiếu của mọi danh mục |
| X.1 | Import ngành từ Excel | **Có** | Dùng chung khung nhập Excel của danh mục: tải tệp mẫu, kiểm tra thử trước, xem bảng lỗi rồi mới nhập thật |
| X.2 | Quản lý môn học: mã môn, tên, số tín chỉ, ngành, học kỳ, giảng viên, mô tả | **Có** | Danh mục → Môn học cho phần thông tin chung; màn hình Gán tài liệu cho môn học sửa được danh sách ngành của từng môn |
| X.2 | Gán môn học vào nhiều ngành (quan hệ nhiều-nhiều) | **Có** | Một môn thuộc bao nhiêu ngành cũng được; dữ liệu mẫu để sẵn Tin học đại cương dùng chung cho sáu ngành |
| X.3 | Màn hình 2 cột: chọn môn bên trái, tìm và gán tài liệu bên phải | **Có** | Bên trái lọc theo ngành, theo từ khóa và theo cờ "chỉ môn chưa có tài liệu"; bên phải tìm tài liệu theo nhan đề, tác giả hoặc ISBN rồi tick chọn nhiều cuốn một lượt |
| X.3 | Phân loại liên kết: Giáo trình chính / Tham khảo bắt buộc / Tham khảo thêm | **Có** | Đủ ba mức, mỗi mức một màu; sửa mức ngay trên dòng, không phải bỏ ra gán lại |
| X.3 | Gán hàng loạt | **Có** | Chọn nhiều tài liệu rồi gán một lần; gán lại cuốn đã có thì cập nhật mức độ chứ không sinh thêm dòng trùng |
| X.3 | Import danh mục tài liệu môn học từ Excel | **Có** | Tệp mẫu có sẵn tiêu đề tiếng Việt; đối chiếu tài liệu theo ISBN, số kiểm soát hoặc số ĐKCB; dòng hỏng bị bỏ qua kèm lý do chứ không chặn cả tệp |
| X.3 | Trên OPAC: duyệt Ngành → Môn học → tài liệu, biết ngay còn bản rảnh không | **Có** | Trang Duyệt theo ngành đào tạo; mỗi tài liệu hiện mức độ và số bản còn rảnh tại thời điểm xem |
| X.3 | Báo cáo môn học chưa có tài liệu | **Có** | Một thẻ riêng trong Báo cáo tài liệu môn học, lọc được theo ngành |
| X.3 | Báo cáo tài liệu được gán nhiều môn nhất | **Có** | Kèm cột số bản còn rảnh, tô đỏ khi số bản ít hơn số môn đang dùng chung — đúng chỗ thư viện cần bổ sung thêm bản |
| X.3 | Báo cáo mức độ đáp ứng tài liệu theo ngành | **Có** | Bảng kèm biểu đồ cột, tỷ lệ làm tròn một chữ số thập phân; xuất được Excel và PDF |
| X.3 | Ba dạng đầu ra cho báo cáo: bảng, đồ họa, xuất tệp | **Có** | Bảng và biểu đồ trên màn hình; hai nút Xuất Excel và Xuất PDF dùng chung bộ lọc đang đặt |
| 6.6 | Thao tác được bằng bàn phím | **Có** | Thẻ ngành và dòng môn học ở cả trang quản trị lẫn trang tra cứu nhận được phím Tab, mở bằng Enter hoặc Space |

---

## D. Trao đổi dữ liệu và các phân hệ còn lại

| # | Yêu cầu | Đáp ứng | Ghi chú |
|---|---|---|---|
| D1 | MARC 21, ISO 2709, MARCXML | **Có** | Chi tiết ở mục B3 |
| D2 | Z39.50 client và server, SRU/SRW | **Có** | Chi tiết ở mục B10. Đã tra cứu thật tới Thư viện Quốc hội Mỹ |
| D3 | OAI-PMH provider và harvester | **Có** | Chi tiết ở mục B10 |
| D4 | Phân hệ II — Biên mục | **Có** | Chi tiết ở mục B4, riêng II.7 ở mục B10 |
| D5 | Phân hệ III — Bổ sung và Kho | **Có** | Chi tiết ở mục B5 |
| D6 | Phân hệ IV — Ấn phẩm định kỳ | **Có** | Chi tiết ở mục B6 |
| D7 | Phân hệ V — Tài liệu số | **Có** | Chi tiết ở mục B9 |
| D8 | Phân hệ VI — Bạn đọc | **Có** | Chi tiết ở mục B7 |
| D9 | Phân hệ VII — Lưu thông | **Có** | Chi tiết ở mục B8 |
| D10 | Phân hệ VIII — Quản trị nội dung | **Có** | Chi tiết ở mục B11 |
| D11 | Phân hệ IX — Tra cứu OPAC | **Có** | Chi tiết ở mục B12. Trang tra cứu là ứng dụng riêng, chạy ở đường dẫn gốc |
| D12 | Phân hệ X — Tài liệu môn học | **Có** | Chi tiết ở mục B13 |
| D13 | Phân hệ XI — Ứng dụng di động | **Có** (Android dựng và kiểm trên máy ảo; iOS dựng và chạy trên iPhone Simulator của máy Mac GitHub Actions) | Chi tiết ở mục B14. Ứng dụng Flutter trong `mobile/`, gọi nhóm `/api/reader/*`, `/api/search/*`, `/api/browse/*`, `/api/public/*`; 76 phép thử đơn vị/widget và 12 luồng đầu-cuối trên máy ảo Android với máy chủ Docker thật (`docs/06`, MB.01–MB.33), thêm bảy kịch bản iOS MB.34–MB.40 (dựng, tra cứu, đăng nhập, chế độ tối, đặt giữ, mượn tự phục vụ, gia hạn — ba kịch bản cuối ghi thật vào máy chủ bằng bạn đọc kiểm thử riêng) |
