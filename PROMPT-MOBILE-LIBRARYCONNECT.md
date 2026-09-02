# PROMPT BUILD PHASE 15 — ỨNG DỤNG DI ĐỘNG LIBRARYCONNECT

> Đưa toàn bộ nội dung này cho Claude Code. Đây là Phase 15 trong lộ trình đã định ở
> `PROMPT-BUILD-LIBRARYCONNECT.md`, thực hiện Phân hệ XI của E-HSMT.

---

## 0. BỐI CẢNH VÀ NGUYÊN TẮC

Phần web đã hoàn tất 14 phase. Nhóm endpoint `/api/reader/*` đã được xây và kiểm chứng ở
Phase 14 (báo cáo ghi nhận 34/34 dòng hợp đồng API trả lời đúng). Phase này **chỉ viết
ứng dụng khách**, gọi vào API sẵn có.

**Đọc trước khi code:**
- `PROMPT-BUILD-LIBRARYCONNECT.md` mục 0.1 (định danh sản phẩm), 0.2, và Phân hệ XI
- `docs/05-api-reference.md` chương "API cho ứng dụng khách" — đây là hợp đồng, bám sát nó
- `docs/00-quyet-dinh-ky-thuat.md` — giữ nhất quán quy ước với phần web

**Nguyên tắc bất di bất dịch:** ứng dụng **không chứa logic nghiệp vụ**. Không tự tính hạn
trả, tiền phạt, điều kiện gia hạn, hạn mức mượn. Mọi quy tắc do máy chủ quyết, app chỉ hiển
thị và gửi yêu cầu. Nếu phát hiện một quy tắc nào đó buộc phải tính ở client mới hiển thị
đúng, đó là **thiếu sót của API** — bổ sung endpoint hoặc trường trả về ở backend, không
tính ở app.

**Khi gặp chỗ chưa rõ:** không hỏi. Suy theo quy ước phần web → tra chuẩn → chọn phương án
đơn giản nhất chạy được thật và cấu hình được → ghi vào `docs/00-quyet-dinh-ky-thuat.md`.

**Không stub, không mock, không màn hình giả.** Mọi màn hình phải gọi API thật và hiển thị
dữ liệu thật từ hệ thống đang chạy trên Docker.

---

## 1. ĐỊNH DANH VÀ STACK

| Hạng mục | Giá trị |
|---|---|
| Tên hiển thị | LibraryConnect |
| Flutter package | `libraryconnect_mobile` |
| Application ID Android | `vn.bluestar.libraryconnect` |
| Bundle ID iOS | `vn.bluestar.libraryconnect` |
| Thư mục | `mobile/` (hiện đang rỗng, có README) |
| Flutter | 3.x, Dart 3, null-safety |
| Nền tảng đích | Android 8.0+ (API 26), iOS 14+ |

**Gói bắt buộc dùng:**

| Mục đích | Gói |
|---|---|
| Quản lý trạng thái | `flutter_riverpod` |
| Gọi HTTP | `dio` + interceptor tự làm mới token |
| Điều hướng | `go_router` |
| Lưu bảo mật (token, thẻ) | `flutter_secure_storage` |
| Lưu cục bộ (cache, offline) | `drift` (SQLite) |
| Quét mã vạch/QR | `mobile_scanner` |
| Sinh mã vạch thẻ | `barcode_widget` |
| Đọc PDF | `pdfx` hoặc `syncfusion_flutter_pdfviewer` |
| Thông báo đẩy | `firebase_messaging` + `flutter_local_notifications` |
| Kết nối mạng | `connectivity_plus` |
| Wi-Fi SSID (xác thực vị trí) | `network_info_plus` |
| Sinh mã | `freezed` + `json_serializable` |
| Đa ngôn ngữ | `flutter_localizations` + ARB |

**Không dùng** gói trả phí hoặc gói cần đăng ký khóa thương mại, trừ Firebase (miễn phí ở
mức cần dùng).

---

## 2. CẤU TRÚC DỰ ÁN

```
mobile/
├── lib/
│   ├── main.dart
│   ├── app.dart                    # MaterialApp, theme, router
│   ├── core/
│   │   ├── api/                    # Dio client, interceptor, error mapping
│   │   ├── auth/                   # Token store, refresh, session
│   │   ├── config/                 # Endpoint, môi trường, hằng số
│   │   ├── db/                     # Drift schema cho cache offline
│   │   ├── theme/                  # Màu, typography (Be Vietnam Pro)
│   │   ├── l10n/                   # ARB tiếng Việt (mặc định) + tiếng Anh
│   │   └── widgets/                # Component dùng chung
│   ├── features/
│   │   ├── auth/                   # Đăng nhập, đổi mật khẩu
│   │   ├── home/                   # Trang chủ, tin tức
│   │   ├── search/                 # Tra cứu cơ bản/nâng cao/quét mã
│   │   ├── browse/                 # Duyệt danh mục
│   │   ├── bib_detail/             # Chi tiết tài liệu
│   │   ├── my_library/             # Đang mượn, lịch sử, đặt giữ, phạt
│   │   ├── self_checkout/          # Mượn tự phục vụ
│   │   ├── digital/                # Tài liệu số, trình đọc
│   │   ├── card/                   # Thẻ thư viện điện tử
│   │   ├── notifications/          # Thông báo
│   │   └── profile/                # Tài khoản, cài đặt
│   └── shared/models/              # Model dùng chung, sinh bằng freezed
├── test/                           # Unit + widget test
├── integration_test/               # Test đầu-cuối trên máy ảo
├── android/ ios/
└── README.md                       # Hướng dẫn cấu hình endpoint, build
```

Mỗi feature theo cấu trúc: `data/` (repository, DTO) → `domain/` (model, use case nếu cần)
→ `presentation/` (screen, widget, controller).

---

## 3. BACKEND CẦN BỔ SUNG TRƯỚC — LÀM ĐẦU TIÊN

Sáu việc backend chưa có vì phần web không cần. Làm xong mới viết app.

### 3.1. Gửi thông báo đẩy (FCM)
`INotificationSender` hiện chỉ có implementation email. Viết thêm implementation FCM:
- Bảng `sys.device_tokens`: `user_id`, `reader_id`, `token`, `platform`, `app_version`,
  `last_seen_at`, `is_active`
- Endpoint `POST/DELETE /api/reader/devices` đăng ký và hủy token
- Móc vào các job đang có: sắp đến hạn trả (trước N ngày, N cấu hình được), đã quá hạn,
  sách đặt giữ đã sẵn sàng, yêu cầu truy cập tài liệu số được duyệt/từ chối, gia hạn thẻ
  được duyệt, tin tức mới (tùy chọn bật/tắt)
- Cấu hình FCM server key qua biến môi trường `LC_FCM_*`, để trống thì bỏ qua lặng lẽ
  (không làm hỏng job)
- Token chết (FCM trả `NotRegistered`) thì tự đánh dấu `is_active = false`

### 3.2. Xác thực vị trí cho mượn tự phục vụ
E-HSMT yêu cầu "tự vào kho chọn sách và quét mượn tài liệu". Phải chống mượn từ xa:
- Tham số `CIRCULATION.SELF_CHECKOUT_VERIFY_MODE`: `NONE` | `WIFI_SSID` | `QR_STATION`
- Chế độ `WIFI_SSID`: tham số danh sách SSID hợp lệ; app gửi SSID hiện tại lên, máy chủ đối chiếu
- Chế độ `QR_STATION`: bảng `cir.checkout_stations` (mã trạm, kho, vị trí, `is_active`);
  màn hình admin sinh và in QR trạm; QR chứa mã trạm + chữ ký; app quét trước khi mượn,
  hiệu lực cấu hình được (mặc định 15 phút)
- `POST /api/reader/loans/self-checkout` nhận thêm `verificationToken`; thiếu hoặc sai thì
  từ chối kèm mã lỗi rõ ràng
- Ghi nhật ký mọi lần mượn tự phục vụ, có cột phân biệt với mượn tại quầy

### 3.3. Tải tài liệu số về đọc offline
- `POST /api/reader/digital/{id}/offline-package`: kiểm quyền, trả gói tài liệu kèm khóa
  giải mã và hạn dùng
- Hạn dùng theo cấu hình (mặc định 7 ngày), hết hạn app tự xóa
- Chỉ cấp cho tài liệu `allow_download = true` hoặc đã được duyệt truy cập
- Ghi nhật ký vào `digital_access_logs` với action `OFFLINE_DOWNLOAD`
- Tài liệu không cho tải thì trả 403 với thông điệp tiếng Việt rõ nghĩa

### 3.4. Đồng bộ delta
Mọi endpoint danh sách nhận thêm `?updatedSince=<ISO8601>` để app chỉ tải phần thay đổi.
Áp dụng cho: danh mục, biểu ghi, tin tức, thông báo, lịch sử mượn. Trả kèm `serverTime` để
app dùng làm mốc lần sau.

### 3.5. Ảnh theo kích thước
Endpoint ảnh bìa và avatar nhận `?w=` và `?h=`, resize và cache phía máy chủ. App tải bản
nhỏ trong danh sách, bản lớn ở trang chi tiết. Tránh app tải ảnh full-size trên 3G.

### 3.6. Kiểm tra phiên bản ứng dụng
`GET /api/public/app-version` trả `minVersion`, `latestVersion`, `updateUrl`, `forceUpdate`.
Tham số cấu hình được. App khởi động kiểm tra, thấp hơn `minVersion` thì chặn và hiện màn
hình yêu cầu cập nhật.

---

## 4. ĐẶC TẢ MÀN HÌNH

### 4.1. Không cần đăng nhập

**Trang chủ**
Ô tìm kiếm lớn, nút quét mã nổi bật, sách mới bổ sung (carousel ngang), tin tức mới nhất,
lối tắt tới các danh mục duyệt, thông tin thư viện (giờ mở cửa, địa chỉ, nút gọi, nút chỉ đường).

**Tra cứu**
- Cơ bản: một ô + chọn phạm vi (Tất cả / Nhan đề / Tác giả / Chủ đề / ISBN / Từ khóa),
  gợi ý khi gõ, **tìm được khi gõ không dấu**
- Nâng cao: nhiều điều kiện AND/OR/NOT, lọc năm, ngôn ngữ, dạng tài liệu, kho, chỉ tài liệu số
- Kết quả: danh sách cuộn vô hạn, ảnh bìa nhỏ, nhãn tình trạng sẵn có; bộ lọc facet mở
  bằng bottom sheet; sắp xếp
- Lưu tìm kiếm gần đây (cục bộ), xóa được

**Quét mã**
Một màn hình quét chung, tự nhận diện: ISBN-10/13, mã vạch ĐKCB, QR. Đèn pin, chọn camera,
quét liên tiếp không cần đóng mở lại. Quét xong nhảy thẳng tới chi tiết tài liệu; không tìm
thấy thì hiện mã đã quét và nút tra cứu thủ công.

**Duyệt danh mục**
Chủ đề · Đề mục · Tác giả · Phân loại DDC · Chuyên ngành đào tạo → Môn học · Luận văn/Luận án
· Ấn phẩm định kỳ. Dạng cây có thể bung, kèm số lượng.

**Chi tiết tài liệu**
Ảnh bìa lớn, thông tin thư mục dạng ISBD, tóm tắt, tác giả và chủ đề bấm được để tìm tiếp.
Tabs: Thông tin · Bản in trong kho (kèm tình trạng và vị trí kho/giá) · Tài liệu số · MARC
(dạng bảng, **không phải JSON thô**) · Nhận xét.
Nút hành động thay đổi theo trạng thái thật: có bản rảnh → Đặt giữ chỗ; hết bản → Xếp hàng
đợi kèm vị trí; **0 ĐKCB → ẩn nút, chỉ hiện tài liệu số nếu có**.
Xuất trích dẫn (APA/MLA/Chicago/BibTeX), chia sẻ, thêm yêu thích.

**Tin tức và trang tĩnh**
Danh sách tin, chi tiết tin, các trang Giới thiệu / Nội quy / Hướng dẫn / Liên hệ.

### 4.2. Sau đăng nhập

**Đăng nhập**
Số thẻ + mật khẩu. Ghi nhớ số thẻ. Sinh trắc học (vân tay/khuôn mặt) cho lần sau, tùy chọn.
Quên mật khẩu → hướng dẫn liên hệ thư viện (không tự đặt lại).

**Thẻ thư viện điện tử**
Hiển thị mã vạch và QR số thẻ cỡ lớn, tự tăng độ sáng màn hình khi mở. Ảnh, họ tên, loại
bạn đọc, hạn thẻ, trạng thái. **Hoạt động khi không có mạng** (đọc từ secure storage).
Thẻ hết hạn hoặc bị khóa thì hiện rõ trạng thái, không hiện mã.

**Sách của tôi**
- Đang mượn: danh sách kèm hạn trả, đếm ngược, tô cảnh báo khi gần hạn/quá hạn, nút Gia hạn
  (máy chủ quyết cho hay không, app chỉ hiện kết quả)
- Lịch sử mượn trả: lọc theo thời gian, tìm kiếm
- Đặt giữ: trạng thái, vị trí hàng đợi, hạn nhận, nút hủy
- Tiền phạt: danh sách, tổng tiền, hướng dẫn thanh toán (app **không** xử lý thanh toán)

**Mượn tự phục vụ**
Luồng: mở màn hình → xác thực vị trí (quét QR trạm hoặc kiểm SSID theo cấu hình máy chủ) →
quét barcode sách → máy chủ kiểm chính sách → hiện kết quả. Quét liên tiếp nhiều cuốn, mỗi
cuốn phản hồi ngay bằng màu + rung + âm thanh. Kết thúc hiện phiếu mượn tóm tắt.
Mọi lỗi (thẻ hết hạn, quá hạn mức, sách đang có người giữ, sách chưa kiểm nhận) hiển thị
đúng thông điệp máy chủ trả về, tiếng Việt.

**Tài liệu số**
Danh sách tài liệu được phép truy cập. Trình đọc PDF trong app: cuộn/lật trang, mục lục,
phóng to, đánh dấu trang, tìm trong văn bản.
- Tài liệu có watermark: hiển thị watermark động do máy chủ đóng, **không tự vẽ ở client**
- Tài liệu cấm tải: chặn chụp màn hình (`FLAG_SECURE` trên Android; iOS phát hiện và cảnh báo)
- Tài liệu cho tải: tải gói offline mã hóa, tự hết hạn, có danh sách quản lý và xóa
- Tài liệu hạn chế: nút Gửi yêu cầu truy cập kèm ô lý do; xem trạng thái yêu cầu đã gửi
- Lịch sử xem/tải

**Thông báo**
Danh sách, đánh dấu đã đọc, bấm vào nhảy tới đúng màn hình liên quan. Cài đặt bật/tắt từng
loại thông báo.

**Tài khoản**
Thông tin cá nhân, cập nhật email/điện thoại, đổi mật khẩu, gửi yêu cầu gia hạn thẻ và xem
trạng thái, cài đặt (sáng/tối/theo hệ thống, cỡ chữ, ngôn ngữ), đăng xuất, thông tin phiên bản.

---

## 5. YÊU CẦU PHI CHỨC NĂNG

**Ngoại tuyến**
Hoạt động được khi mất mạng: thẻ điện tử, danh sách đang mượn (bản cache gần nhất, có ghi
rõ thời điểm cập nhật), tài liệu số đã tải, kết quả tra cứu gần đây. Mọi màn hình cần mạng
mà không có mạng phải hiện trạng thái rõ ràng kèm nút thử lại — **không được màn hình trắng
hoặc quay vòng vô tận**.

**Bảo mật**
- Token trong `flutter_secure_storage`, không trong `SharedPreferences`
- Gói tài liệu offline mã hóa AES, khóa lưu trong secure storage
- Chặn chụp màn hình ở trình đọc tài liệu cấm tải và ở màn hình thẻ (tùy chọn)
- Không ghi log dữ liệu nhạy cảm ở bản release
- Certificate pinning nếu cấu hình bật

**Hiệu năng**
Khởi động lạnh dưới 3 giây. Danh sách cuộn 60fps với 1.000 mục. Ảnh tải lười, cache đĩa.
Không tải lại toàn bộ khi quay lại màn hình trước.

**Giao diện**
Font Be Vietnam Pro. Chế độ sáng/tối. Cỡ chữ điều chỉnh được và **tôn trọng cài đặt cỡ chữ
của hệ điều hành**. Vùng chạm tối thiểu 48dp. Hỗ trợ trình đọc màn hình cho luồng chính.
Toàn bộ chuỗi trong ARB, mặc định tiếng Việt.

**Xử lý lỗi**
Mọi lỗi API hiển thị thông điệp tiếng Việt từ máy chủ. Lỗi mạng, hết phiên, 403, 429 mỗi
loại có cách xử lý riêng. Hết phiên thì tự làm mới token; không được thì đưa về đăng nhập
mà không mất dữ liệu đang nhập.

---

## 6. KIỂM THỬ — ĐỐI CHIẾU MỤC 2.7 E-HSMT

E-HSMT mục 2.7 kiểm: *đăng nhập, tra cứu, quét mã, tài khoản, đặt giữ, gia hạn, tài liệu số,
lịch sử giao dịch, thông báo và đồng bộ dữ liệu trung tâm*. Viết test phủ đủ:

| Loại | Yêu cầu |
|---|---|
| Unit test | Repository, mapping DTO, xử lý lỗi, logic hiển thị (không phải logic nghiệp vụ) |
| Widget test | Mọi màn hình render được ở 3 trạng thái: đang tải, có dữ liệu, lỗi/rỗng |
| Integration test | 12 luồng đầu-cuối chạy trên máy ảo, gọi backend Docker thật |

**12 luồng integration bắt buộc:**
1. Đăng nhập → xem thẻ điện tử → đăng xuất
2. Tra cứu cơ bản gõ **không dấu** → mở chi tiết → xem bản in trong kho
3. Tra cứu nâng cao nhiều điều kiện → lọc facet → sắp xếp
4. Quét barcode ĐKCB → nhảy đúng tài liệu
5. Quét ISBN → tìm thấy hoặc báo không có, đúng thông điệp
6. Đặt giữ chỗ → xem trong Sách của tôi → hủy
7. Gia hạn thành công; và gia hạn bị từ chối (cuốn có người đặt giữ) → hiện đúng lý do
8. Mượn tự phục vụ: xác thực vị trí đúng → mượn được; xác thực sai → bị từ chối
9. Mở tài liệu số công khai → đọc; tài liệu hạn chế → gửi yêu cầu → xem trạng thái
10. Tải tài liệu offline → bật chế độ máy bay → vẫn đọc được
11. Nhận thông báo đẩy → bấm vào → nhảy đúng màn hình
12. Đồng bộ: sửa dữ liệu trên web → mở app → thấy thay đổi (kiểm `updatedSince`)

**Kiểm chứng thủ công bắt buộc** — chạy trên **máy thật**, không chỉ máy ảo:
- Quét mã vạch trên sách thật, đủ sáng và thiếu sáng
- Thẻ điện tử quét được bằng máy quét ở quầy
- Chế độ máy bay ở mọi màn hình
- Xoay ngang màn hình
- Cỡ chữ hệ thống đặt lớn nhất — kiểm tra tràn chữ
- Chế độ tối

Cập nhật `docs/06-kich-ban-kiem-thu.md` thêm nhóm kịch bản `MB.xx`, và
`docs/07-bang-dap-ung-ky-thuat.md` mục Phân hệ XI chuyển từ "Đợt sau" sang "Có".

---

## 7. ĐÓNG GÓI VÀ BÀN GIAO

- Build được **APK** (release, ký bằng keystore riêng) và **AAB** cho Google Play
- Build được **IPA** — nếu môi trường không có macOS thì cấu hình sẵn và ghi rõ trong README
  các bước cần làm trên máy Mac
- Cấu hình endpoint qua `--dart-define`, có sẵn profile `dev` / `staging` / `prod`,
  không hardcode `localhost`
- Icon và splash mang thương hiệu LibraryConnect, sinh bằng `flutter_launcher_icons`
  và `flutter_native_splash`
- `mobile/README.md`: yêu cầu môi trường, cách cấu hình endpoint và Firebase, cách build
  từng nền tảng, cách chạy test
- Bổ sung chương "Hướng dẫn sử dụng ứng dụng di động" vào `docs/01-huong-dan-su-dung.md`,
  có ảnh chụp màn hình

---

## 8. THỨ TỰ THỰC HIỆN

| Bước | Nội dung | Nghiệm thu |
|---|---|---|
| 1 | Backend bổ sung mục 3.1–3.6 | Gọi được bằng curl, có integration test |
| 2 | Khung dự án: cấu trúc, theme, router, API client, auth, l10n | Đăng nhập thật được, token tự làm mới |
| 3 | Tra cứu + quét mã + chi tiết tài liệu | Quét barcode sách thật ra đúng tài liệu |
| 4 | Duyệt danh mục + trang chủ + tin tức | Dữ liệu thật từ backend |
| 5 | Thẻ điện tử + Sách của tôi + đặt giữ + gia hạn | Máy quét ở quầy đọc được thẻ |
| 6 | Mượn tự phục vụ | Xác thực vị trí chặn đúng khi ở ngoài |
| 7 | Tài liệu số + trình đọc + offline | Chế độ máy bay vẫn đọc được |
| 8 | Thông báo đẩy + tài khoản + cài đặt | Nhận được đẩy thật từ backend |
| 9 | Ngoại tuyến, hiệu năng, chế độ tối, cỡ chữ | Chạy trên máy thật, không tràn chữ |
| 10 | Kiểm thử đầy đủ + đóng gói + tài liệu | 12 luồng xanh, APK cài được |

Mỗi bước xong: build 0 warning, test xanh, chạy thật trên máy ảo và **ít nhất một máy thật**,
commit và push, báo ngắn 5 dòng (nội dung, số test, commit, lỗi thật bắt được, quyết định
tự chốt).

---

## 9. CỔNG CHẤT LƯỢNG

Một bước chỉ được coi là xong khi đủ:
1. `flutter analyze` 0 issue; `dart format` sạch
2. Unit + widget + integration test xanh, test mới viết cho đúng bước đó
3. Đã chạy thật, tự xem tận mắt trên máy ảo và máy thật — không chỉ chạy test
4. Đã cập nhật `docs/06-kich-ban-kiem-thu.md` và `docs/07-bang-dap-ung-ky-thuat.md`
5. Commit + push, message ghi rõ bước
6. Báo cáo trung thực: bắt được lỗi thật thì ghi, không bắt được thì nói không bắt được

**Bắt đầu bước 1 ngay.**
