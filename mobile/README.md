# LibraryConnect Mobile — Phân hệ XI (Phase 15)

Ứng dụng bạn đọc của **LibraryConnect**, viết bằng Flutter, gọi vào nhóm endpoint `/api/reader/*`,
`/api/search/*`, `/api/browse/*`, `/api/public/*` của máy chủ đã có. Ứng dụng **không chứa logic
nghiệp vụ**: hạn trả, tiền phạt, điều kiện gia hạn, hạn mức mượn, quyền đọc tài liệu số đều do máy
chủ quyết; ứng dụng hiển thị và gửi yêu cầu.

| Hạng mục | Giá trị |
|---|---|
| Flutter package | `libraryconnect_mobile` |
| Application ID Android / Bundle ID iOS | `vn.bluestar.libraryconnect` |
| Nền tảng | Android 8.0+ (API 26), iOS 14+ |
| Flutter / Dart | 3.47 / 3.13 (kênh stable) |

## Cấu trúc

```
lib/
├── main.dart, app.dart          # ProviderScope, MaterialApp.router, chủ đề, đa ngôn ngữ, chặn phiên bản cũ
├── core/
│   ├── api/                     # ApiClient (Dio + tự làm mới token), ApiException (câu tiếng Việt)
│   ├── auth/                    # TokenStore (secure storage), AuthController (riverpod)
│   ├── config/                  # Env (--dart-define), settings & app-version providers
│   ├── router/                  # go_router, chuyển hướng theo trạng thái đăng nhập
│   ├── theme/                   # Bảng màu giấy ngà / xanh rêu, Be Vietnam Pro + Lora, viên trạng thái
│   └── widgets/                 # AppShell (thanh điều hướng dưới)
├── features/<tên>/{data,presentation}  # auth, home, account, search, scan, bib, …
├── l10n/                        # app_vi.arb (mặc định), app_en.arb → sinh mã bằng gen-l10n
└── shared/models/               # Model freezed + json_serializable
```

## Cấu hình endpoint và môi trường

Mọi thứ đi qua `--dart-define`; không có `localhost` nào trong mã:

| Biến | Ý nghĩa | Mặc định |
|---|---|---|
| `LC_PROFILE` | `dev` \| `staging` \| `prod` | `dev` |
| `LC_API_BASE_URL` | Địa chỉ API, kết thúc bằng `/api` | `http://10.0.2.2/api` (máy ảo Android → máy phát triển) |
| `LC_APP_VERSION` | Phiên bản gửi lên máy chủ để so với `minVersion` | `1.0.0` |
| `LC_CERT_PINS` | Ghim chứng chỉ (SHA-256 khoá công khai, base64, cách nhau bằng dấu phẩy) | trống = không ghim |

```bash
# Máy ảo Android, máy chủ chạy bằng docker compose trên máy phát triển
flutter run -d emulator-5554 --dart-define=LC_PROFILE=dev --dart-define=LC_API_BASE_URL=http://10.0.2.2/api

# Điện thoại thật cùng Wi-Fi: thay bằng IP của máy phát triển
flutter run --dart-define=LC_API_BASE_URL=http://192.168.1.20/api

# Bản phát hành
flutter build apk --release --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.example.edu.vn/api
flutter build appbundle --release --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.example.edu.vn/api
```

Bản **debug** cho phép HTTP không mã hoá (để gọi máy chủ phát triển); bản **release** chỉ HTTPS.

## Firebase (thông báo đẩy)

Ứng dụng chạy được **không cần** Firebase: thiếu tệp cấu hình thì phần thông báo đẩy tắt lặng lẽ,
mọi thứ khác vẫn hoạt động. Để bật:

1. Tạo dự án Firebase, thêm ứng dụng Android `vn.bluestar.libraryconnect` và iOS cùng ID.
2. Đặt `google-services.json` vào `android/app/` và `GoogleService-Info.plist` vào `ios/Runner/`.
3. Phía máy chủ khai `LC_Fcm__ProjectId` và `LC_Fcm__ServiceAccountFile` (xem `.env.example`).

## Chạy kiểm thử

```bash
flutter analyze                      # 0 issue
dart format --set-exit-if-changed lib test
flutter test                         # unit + widget
flutter test integration_test -d <device> --dart-define=LC_API_BASE_URL=http://10.0.2.2/api   # luồng đầu-cuối, cần máy chủ Docker đang chạy
```

## Dựng Android — những chỗ đã vấp

- Gradle 9.3.1 / AGP 9.1.0 / Kotlin 2.4.0 theo đúng mẫu `flutter create` của Flutter 3.47; AGP 9 tích hợp Kotlin nên
  `app/build.gradle.kts` không còn `kotlin-android`.
- `kotlin.incremental=false` trong `android/gradle.properties`: Pub cache (ổ C:) và thư mục build (ổ D:) khác ổ, Kotlin
  incremental ném "different roots" cho mọi plugin Kotlin. Cùng ổ thì bỏ dòng này được.
- `flutter_secure_storage` giữ bản 10: bản 11 đòi `compileSdk 37` mà SDK chỉ có `android-37.0`, Gradle không tìm thấy.
- `flutter_local_notifications` cần `isCoreLibraryDesugaringEnabled = true` + `desugar_jdk_libs`.
- APK debug gộp mọi ABI nặng ~195 MB; máy ảo phải còn hơn 400 MB trống. Máy ảo thử nghiệm: `LC_Pixel` (Android 16,
  Pixel 9, ổ dữ liệu 8 GB), tạo bằng `avdmanager create avd -n LC_Pixel -k "system-images;android-36;google_apis_playstore;x86_64" -d pixel_9`.

## Sinh mã

```bash
dart run build_runner build --delete-conflicting-outputs   # freezed, json_serializable, drift
flutter gen-l10n                                            # tự chạy khi `flutter pub get`
```

## Tiến độ theo bước (PROMPT-MOBILE-LIBRARYCONNECT.md mục 8)

| Bước | Nội dung | Trạng thái |
|---|---|---|
| 1 | Backend bổ sung 3.1–3.6 | Xong (commit `8575c34`) |
| 2 | Khung dự án: cấu trúc, chủ đề, định tuyến, API client, đăng nhập, đa ngôn ngữ | Xong — đăng nhập đầu-cuối xanh trên máy ảo Android 16 (MB.09) |
| 3 | Tra cứu (gợi ý, không dấu, facet, sắp xếp, nâng cao, tìm gần đây) + quét mã + chi tiết 5 thẻ | Xong — MB.10–MB.12; camera thật chưa kiểm |
| 4–10 | Danh mục, trang chủ, tin tức, thẻ, sách của tôi, tự mượn, tài liệu số, thông báo, đóng gói | Chưa |

Chưa bước nào chạy được trên **máy thật**: máy phát triển không có điện thoại kết nối. Mỗi bước ghi rõ điều này trong báo cáo.
