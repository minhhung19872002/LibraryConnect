# LibraryConnect Mobile — Phân hệ XI (Phase 15)

Ứng dụng bạn đọc của **LibraryConnect**, viết bằng Flutter, gọi vào nhóm endpoint `/api/reader/*`,
`/api/search/*`, `/api/browse/*`, `/api/public/*` của máy chủ đã có. Ứng dụng **không chứa logic
nghiệp vụ**: hạn trả, tiền phạt, điều kiện gia hạn, hạn mức mượn, quyền đọc tài liệu số đều do máy
chủ quyết; ứng dụng hiển thị và gửi yêu cầu.

| Hạng mục | Giá trị |
|---|---|
| Flutter package | `libraryconnect_mobile` |
| Application ID Android / Bundle ID iOS | `vn.bluestar.libraryconnect` |
| Nền tảng | Android 8.0+ (API 26), iOS 15.5+ |
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

# Bản phát hành — PHẢI có android/key.properties + libraryconnect-release.jks (khoá ký phát hành, tạo 03/09/2026;
# bản sao ngoài kho mã ở máy phát triển: %USERPROFILE%\.libraryconnect\; CI lấy từ secrets LC_ANDROID_*).
# Không có tệp thì Gradle ký khoá debug của máy — bản ấy KHÔNG cài đè được lên bản đã phát hành.
flutter build apk --release --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.example.edu.vn/api
flutter build appbundle --release --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.example.edu.vn/api
# Ghim chứng chỉ (tuỳ chọn): --dart-define=LC_CERT_PINS=<SHA-256 base64 của chứng chỉ máy chủ>
# Đầu ra: build/app/outputs/flutter-apk/app-release.apk, build/app/outputs/bundle/release/app-release.aab

# iOS (cần máy Mac + Xcode; bundle id vn.bluestar.libraryconnect, Info.plist đã khai quyền camera/Face ID/vị trí)
# Ngưỡng iOS tối thiểu 15.5 — do mobile_scanner 7 và Firebase iOS SDK 12 đòi; đặt ở ios/Podfile,
# ios/Flutter/AppFrameworkInfo.plist và IPHONEOS_DEPLOYMENT_TARGET trong project.pbxproj.
flutter build ios --release --no-codesign   # kiểm biên dịch, không cần tài khoản Apple
flutter build ipa --release --dart-define=LC_PROFILE=prod --dart-define=LC_API_BASE_URL=https://thuvien.example.edu.vn/api
```

Biểu tượng ứng dụng: `assets/icon/app_icon.png` (+ `app_icon_foreground.png` cho adaptive icon); đổi
ảnh rồi chạy `dart run flutter_launcher_icons`.

Bản **debug** cho phép HTTP không mã hoá (để gọi máy chủ phát triển); bản **release** chỉ HTTPS.

## Firebase (thông báo đẩy)

Ứng dụng chạy được **không cần** Firebase: thiếu tệp cấu hình thì phần thông báo đẩy tắt lặng lẽ,
mọi thứ khác vẫn hoạt động. Để bật:

1. Tạo dự án Firebase, thêm ứng dụng Android `vn.bluestar.libraryconnect` và iOS cùng ID.
2. Đặt `google-services.json` vào `android/app/` và `GoogleService-Info.plist` vào `ios/Runner/`.
3. Phía máy chủ khai `LC_Fcm__ProjectId` và `LC_Fcm__ServiceAccountFile` (xem `.env.example`).

## 12 luồng đầu-cuối (PROMPT-MOBILE mục 6) ↔ tệp phép thử

| Luồng | Tệp trong `integration_test/` |
|---|---|
| 1 Đăng nhập → thẻ → đăng xuất | `login_flow_test.dart`, `my_library_flow_test.dart` |
| 2 Tra cứu không dấu → chi tiết → bản in | `search_scan_flow_test.dart` |
| 3 Nâng cao → facet → sắp xếp; 5 Quét ISBN | `catalog_flows_test.dart` |
| 4 Quét ĐKCB | `search_scan_flow_test.dart` (nhập tay cùng đường với camera) |
| 6 Đặt giữ → hủy; 7 Gia hạn; 12 Đồng bộ `updatedSince` | `holds_renew_sync_flow_test.dart` (từ chối gia hạn: `my_library_flow_test.dart`) |
| 8 Mượn tự phục vụ | `self_checkout_flow_test.dart` |
| 9 Tài liệu số công khai / hạn chế; 10 Gói ngoại tuyến | `digital_flow_test.dart` |
| 11 Thông báo → mở đúng màn hình | `account_notifications_flow_test.dart` (đẩy FCM thật chưa kiểm) |
| Trang chủ / duyệt / tin; chế độ tối + chữ lớn | `home_browse_news_flow_test.dart`, `ui_modes_flow_test.dart` |

## Chạy kiểm thử

```bash
flutter analyze                      # 0 issue
dart format --set-exit-if-changed lib test
flutter test                         # unit + widget
flutter test integration_test -d <device> --dart-define=LC_API_BASE_URL=http://10.0.2.2/api   # luồng đầu-cuối, cần máy chủ Docker đang chạy
# Lưu ý: `flutter test` gỡ ứng dụng sau khi chạy. Muốn giữ phiên trên máy ảo để kiểm bằng adb (tắt mạng…)
# thì cài bản thường (`flutter build apk --debug` + `adb install -r`) rồi đăng nhập trên đó.
```

## Dựng Android — những chỗ đã vấp

- Gradle 9.3.1 / AGP 9.1.0 / Kotlin 2.4.0 theo đúng mẫu `flutter create` của Flutter 3.47; AGP 9 tích hợp Kotlin nên
  `app/build.gradle.kts` không còn `kotlin-android`.
- `kotlin.incremental=false` trong `android/gradle.properties`: Pub cache (ổ C:) và thư mục build (ổ D:) khác ổ, Kotlin
  incremental ném "different roots" cho mọi plugin Kotlin. Cùng ổ thì bỏ dòng này được.
- `flutter_secure_storage` giữ bản 10 và `permission_handler` giữ bản 12: bản mới hơn đòi `compileSdk 37` mà SDK chỉ có `android-37.0`, Gradle không tìm thấy.
- `flutter_local_notifications` cần `isCoreLibraryDesugaringEnabled = true` + `desugar_jdk_libs`.
- Vừa chạy `flutter drive`/`flutter test integration_test` xong mà dựng bản release báo
  `package dev.flutter.plugins.integration_test does not exist`: `GeneratedPluginRegistrant.java` còn
  dòng đăng ký plugin của bản thử; chạy lại lệnh build (Flutter sinh lại tệp) là hết.
- APK debug gộp mọi ABI nặng ~195 MB; máy ảo phải còn hơn 400 MB trống. Máy ảo thử nghiệm: `LC_Pixel` (Android 16,
  Pixel 9, ổ dữ liệu 8 GB), tạo bằng `avdmanager create avd -n LC_Pixel -k "system-images;android-36;google_apis_playstore;x86_64" -d pixel_9`.

- **iOS dựng và chạy trên máy Mac của GitHub Actions** (`.github/workflows/ios.yml`): job `ios-build`
  dựng bản phát hành không ký, job `ios-simulator` chạy `integration_test/ios_smoke_test.dart` trên
  iPhone Simulator với máy chủ thật rồi tải ảnh chụp lên artifact `ios-screenshots`. Máy Mac ấy
  không có Docker nên không dựng được máy chủ riêng: chỉ chạy các luồng không đổi dữ liệu.
- **Soi ở bề rộng 360dp + cỡ chữ 1,3 trước khi phát hành** (lỗi I2 sổ lỗi): máy ảo Pixel 9 rộng 411dp nên
  giấu hết lỗi tràn của điện thoại thật. Trên máy ảo: `adb shell wm density 480` và
  `adb shell settings put system font_scale 1.3`; xong thì `wm density reset` + `font_scale 1.0`.

## Ảnh chụp màn hình làm bằng chứng

Phép thử đầu-cuối tự chụp qua `flutter drive` (trình điều khiển `test_driver/integration_test.dart` ghi
vào `build/screenshots/`); chạy bằng `flutter test` thì các điểm chụp bị bỏ qua. Ảnh đã chọn nằm ở
`docs/images/mobile/`.

```bash
flutter drive --driver=test_driver/integration_test.dart   --target=integration_test/digital_flow_test.dart -d emulator-5556   --dart-define=LC_API_BASE_URL=http://10.0.2.2/api
```

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
| 4 | Trang chủ đầy đủ, bảy danh mục duyệt (cây, A–Z, ngành → môn → tài liệu, luận văn, ấn phẩm định kỳ), tin tức, trang tĩnh | Xong — MB.13–MB.15 |
| 5 | Thẻ điện tử (mã vạch + QR, ngoại tuyến, yêu cầu gia hạn thẻ), Sách của tôi (đang mượn + gia hạn, lịch sử, đặt giữ + hủy, tiền phạt) | Xong — MB.16–MB.18 |
| 6 | Mượn tự phục vụ: xác thực NONE / Wi-Fi / QR trạm, quét sách liên tiếp, phiếu tóm tắt | Xong — MB.19–MB.20 |
| 7 | Tài liệu số: danh sách + toàn văn, trình đọc trang ảnh có chữ chìm, tìm trong văn bản, đánh dấu trang, gói ngoại tuyến mã hoá tự hết hạn, xin quyền, lịch sử, chặn chụp màn hình (Android) | Xong — MB.21–MB.23 |
| 8 | Thông báo (danh sách, mở đúng màn hình, cài đặt loại), dịch vụ đẩy FCM (tắt lặng lẽ khi thiếu Firebase), tài khoản đầy đủ, khoá sinh trắc học, cài đặt lưu máy | Xong — MB.24–MB.26; đẩy thật chưa kiểm |
| 9 | Ngoại tuyến (dải mất mạng, bộ đệm đang mượn/tra cứu/cài đặt, phông đóng gói), ghim chứng chỉ, chế độ tối + cỡ chữ lớn soi tràn chữ, đo khởi động lạnh | Xong — MB.27–MB.29; máy thật chưa đo |
| 10 | 12 luồng đầu-cuối (MB.09–MB.33), APK/AAB release, biểu tượng, ký bằng `key.properties`, tài liệu 01/06/07 | Xong |
| 11 | iOS: dựng bản phát hành không ký + chạy trên iPhone Simulator (MB.34–MB.36) qua `.github/workflows/ios.yml` | Xong — máy iPhone thật và IPA ký chưa có |

Chưa bước nào chạy được trên **máy thật**: máy phát triển không có điện thoại kết nối. Mỗi bước ghi rõ điều này trong báo cáo.
Android đã cài thử trên một máy Samsung của người dùng (lỗi I2 sổ lỗi sinh ra từ đó).
