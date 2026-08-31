# LibraryConnect Mobile — Phân hệ XI (đợt sau)

Thư mục này **chưa có code** trong đợt build hiện tại.

## Phạm vi đợt sau
Ứng dụng Flutter 3.x (`libraryconnect_mobile`), build cả APK và IPA:
- Application ID Android / Bundle ID iOS: `vn.bluestar.libraryconnect`
- Tên hiển thị: LibraryConnect

## Hợp đồng API đã sẵn sàng
Toàn bộ nghiệp vụ mobile cần dùng đã được hiện thực và kiểm thử ở backend trong nhóm
endpoint `/api/reader/*`, `/api/search/*`, `/api/browse/*`, `/api/public/*`.

Xem chương **"API cho ứng dụng khách"** trong `docs/05-api-reference.md` và Swagger UI
tại `http://<host>/swagger`.

Backend đã chuẩn bị sẵn:
- Bảng `sys.device_tokens` để lưu FCM token.
- Interface `INotificationSender` (implementation email trước, FCM bổ sung sau).
- CORS cấu hình qua biến môi trường `LC_CORS_ORIGINS`.
