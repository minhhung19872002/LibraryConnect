// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Vietnamese (`vi`).
class L10nVi extends L10n {
  L10nVi([String locale = 'vi']) : super(locale);

  @override
  String get appName => 'LibraryConnect';

  @override
  String get tabHome => 'Trang chủ';

  @override
  String get tabSearch => 'Tra cứu';

  @override
  String get tabScan => 'Quét mã';

  @override
  String get tabMyLibrary => 'Sách của tôi';

  @override
  String get tabAccount => 'Tài khoản';

  @override
  String get loginTitle => 'Đăng nhập bạn đọc';

  @override
  String get loginSubtitle => 'Dùng số thẻ thư viện và mật khẩu được cấp.';

  @override
  String get cardNumber => 'Số thẻ thư viện';

  @override
  String get password => 'Mật khẩu';

  @override
  String get rememberCard => 'Ghi nhớ số thẻ';

  @override
  String get signIn => 'Đăng nhập';

  @override
  String get signOut => 'Đăng xuất';

  @override
  String get forgotPassword => 'Quên mật khẩu?';

  @override
  String get forgotPasswordHelp =>
      'Ứng dụng không tự đặt lại mật khẩu. Vui lòng mang thẻ tới quầy hoặc liên hệ thư viện để được cấp lại.';

  @override
  String get cardNumberRequired => 'Nhập số thẻ thư viện.';

  @override
  String get passwordRequired => 'Nhập mật khẩu.';

  @override
  String get continueAsGuest => 'Tra cứu không cần đăng nhập';

  @override
  String get searchHint => 'Nhập nhan đề, tác giả, từ khóa…';

  @override
  String get searchHintNoAccent => 'Gõ không dấu vẫn tìm thấy.';

  @override
  String get retry => 'Thử lại';

  @override
  String get offlineTitle => 'Không có kết nối';

  @override
  String get offlineBody =>
      'Ứng dụng đang dùng dữ liệu đã lưu. Hãy kết nối mạng để cập nhật.';

  @override
  String get loading => 'Đang tải…';

  @override
  String welcome(String name) {
    return 'Xin chào, $name';
  }

  @override
  String get libraryInfo => 'Thông tin thư viện';

  @override
  String get openingHours => 'Giờ mở cửa';

  @override
  String get call => 'Gọi';

  @override
  String get directions => 'Chỉ đường';

  @override
  String get settings => 'Cài đặt';

  @override
  String get theme => 'Giao diện';

  @override
  String get themeSystem => 'Theo hệ thống';

  @override
  String get themeLight => 'Sáng';

  @override
  String get themeDark => 'Tối';

  @override
  String get language => 'Ngôn ngữ';

  @override
  String get version => 'Phiên bản';

  @override
  String get sessionExpired =>
      'Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.';

  @override
  String get updateRequiredTitle => 'Cần cập nhật ứng dụng';

  @override
  String updateRequiredBody(String current, String min) {
    return 'Phiên bản đang dùng ($current) đã cũ, thư viện yêu cầu từ $min trở lên.';
  }

  @override
  String get update => 'Cập nhật';

  @override
  String get mustChangePassword =>
      'Bạn đang dùng mật khẩu tạm, hãy đổi mật khẩu trước khi tiếp tục.';

  @override
  String get poweredBy => 'Vận hành bởi LibraryConnect';
}
