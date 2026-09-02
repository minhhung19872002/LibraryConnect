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

  @override
  String get searchTitle => 'Tra cứu';

  @override
  String get scopeAll => 'Tất cả';

  @override
  String get scopeTitle => 'Nhan đề';

  @override
  String get scopeAuthor => 'Tác giả';

  @override
  String get scopeSubject => 'Chủ đề';

  @override
  String get scopeKeyword => 'Từ khóa';

  @override
  String get scopeIsbn => 'ISBN';

  @override
  String get scopePublisher => 'Nhà xuất bản';

  @override
  String get scopeCallNumber => 'Ký hiệu xếp giá';

  @override
  String get recentSearches => 'Tìm gần đây';

  @override
  String get clearAll => 'Xóa hết';

  @override
  String get suggestions => 'Gợi ý';

  @override
  String resultCount(int count) {
    return '$count kết quả';
  }

  @override
  String resultCountCapped(int count) {
    return 'Hơn $count kết quả';
  }

  @override
  String get noResults => 'Không tìm thấy tài liệu nào.';

  @override
  String get noResultsHint =>
      'Thử từ khóa ngắn hơn, gõ không dấu, hoặc bỏ bớt bộ lọc.';

  @override
  String get sortLabel => 'Sắp xếp';

  @override
  String get sortRelevance => 'Liên quan nhất';

  @override
  String get sortNewest => 'Mới nhất';

  @override
  String get sortTitle => 'Nhan đề';

  @override
  String get sortAuthor => 'Tác giả';

  @override
  String get sortPopular => 'Được mượn nhiều';

  @override
  String get filters => 'Bộ lọc';

  @override
  String get applyFilters => 'Áp dụng';

  @override
  String get clearFilters => 'Bỏ lọc';

  @override
  String get advancedSearch => 'Tra cứu nâng cao';

  @override
  String get addClause => 'Thêm điều kiện';

  @override
  String get connectorAnd => 'VÀ';

  @override
  String get connectorOr => 'HOẶC';

  @override
  String get connectorNot => 'KHÔNG';

  @override
  String get yearFrom => 'Từ năm';

  @override
  String get yearTo => 'Đến năm';

  @override
  String get onlyDigital => 'Chỉ tài liệu số';

  @override
  String get onlyAvailable => 'Chỉ còn bản rảnh';

  @override
  String get searchAction => 'Tìm';

  @override
  String get loadMoreError => 'Không tải thêm được. Chạm để thử lại.';

  @override
  String availableCopies(int count) {
    return '$count bản sẵn sàng';
  }

  @override
  String get allOnLoan => 'Hết bản, đang cho mượn';

  @override
  String get noCopies => 'Chưa có bản in';

  @override
  String digitalCount(int count) {
    return '$count tài liệu số';
  }

  @override
  String get scanTitle => 'Quét mã';

  @override
  String get scanHint => 'Đưa mã ISBN, mã vạch ĐKCB hoặc mã QR vào khung.';

  @override
  String get scanTorch => 'Đèn pin';

  @override
  String get scanSwitchCamera => 'Đổi camera';

  @override
  String scanNotFound(String code) {
    return 'Không tìm thấy tài liệu cho mã $code.';
  }

  @override
  String get scanManualSearch => 'Tra cứu thủ công';

  @override
  String get scanEnterCode => 'Nhập mã bằng tay';

  @override
  String scanLookingUp(String code) {
    return 'Đang tra mã $code…';
  }

  @override
  String scanFoundCopy(String barcode, String status) {
    return 'Bản in $barcode · $status';
  }

  @override
  String scanIsbnMany(int count, String isbn) {
    return '$count tài liệu cùng ISBN $isbn';
  }

  @override
  String get scanCameraDenied =>
      'Chưa được phép dùng camera. Bạn vẫn có thể nhập mã bằng tay.';

  @override
  String get scanOpen => 'Mở chi tiết';

  @override
  String get scanStationCode =>
      'Đây là mã trạm mượn tự phục vụ, dùng ở màn hình Mượn tự phục vụ.';

  @override
  String get detailTabInfo => 'Thông tin';

  @override
  String get detailTabItems => 'Bản in';

  @override
  String get detailTabDigital => 'Tài liệu số';

  @override
  String get detailTabMarc => 'MARC';

  @override
  String get detailTabReviews => 'Nhận xét';

  @override
  String get holdAction => 'Đặt giữ chỗ';

  @override
  String get queueAction => 'Xếp hàng đợi';

  @override
  String get holdPlaced => 'Đã đặt giữ. Thư viện sẽ báo khi sách sẵn sàng.';

  @override
  String holdQueued(int position) {
    return 'Đã xếp hàng, bạn đứng thứ $position.';
  }

  @override
  String get citeAction => 'Trích dẫn';

  @override
  String get shareAction => 'Chia sẻ';

  @override
  String get favoriteAction => 'Yêu thích';

  @override
  String get favoriteAdded => 'Đã thêm vào yêu thích.';

  @override
  String get favoriteRemoved => 'Đã bỏ khỏi yêu thích.';

  @override
  String get copyAction => 'Sao chép';

  @override
  String get copied => 'Đã sao chép.';

  @override
  String get abstractLabel => 'Tóm tắt';

  @override
  String get subjectsLabel => 'Chủ đề';

  @override
  String get keywordsLabel => 'Từ khóa';

  @override
  String get isbdLabel => 'Mô tả thư mục';

  @override
  String get relatedLabel => 'Tài liệu liên quan';

  @override
  String get externalLinks => 'Toàn văn ở nơi khác';

  @override
  String get noItems => 'Tài liệu chưa có bản in nào.';

  @override
  String get noDigital => 'Không có tài liệu số.';

  @override
  String get noReviews => 'Chưa có nhận xét nào.';

  @override
  String get marcUnreadable => 'Không đọc được biểu ghi MARC.';

  @override
  String get leaderLabel => 'Đầu biểu ghi';

  @override
  String dueBack(String date) {
    return 'Hạn trả $date';
  }

  @override
  String get writeReview => 'Viết nhận xét';

  @override
  String get sendReview => 'Gửi';

  @override
  String get reviewSent => 'Đã gửi nhận xét, chờ thư viện duyệt.';

  @override
  String get reviewHint => 'Cảm nhận của bạn về tài liệu';

  @override
  String get signInToContinue => 'Đăng nhập để tiếp tục';

  @override
  String get pagesLabel => 'Số trang';

  @override
  String get publisherLabel => 'Nhà xuất bản';

  @override
  String get publishYearLabel => 'Năm xuất bản';

  @override
  String get editionLabel => 'Lần xuất bản';

  @override
  String get documentTypeLabel => 'Dạng tài liệu';

  @override
  String get callNumberLabel => 'Ký hiệu';

  @override
  String get ddcLabel => 'Phân loại DDC';

  @override
  String get seriesLabel => 'Tùng thư';

  @override
  String get requiresRequest => 'Cần xin phép';

  @override
  String get controlNumberLabel => 'Số kiểm soát';

  @override
  String averageRating(String rating) {
    return 'Trung bình $rating / 5';
  }

  @override
  String get citationStyle => 'Chuẩn trích dẫn';

  @override
  String get cannotShare => 'Không chia sẻ được trên máy này.';

  @override
  String get scanFromHome => 'Quét mã sách';

  @override
  String get searchFromHome => 'Tìm nhan đề, tác giả, ISBN…';

  @override
  String itemsInStock(int count) {
    return '$count bản in';
  }
}
