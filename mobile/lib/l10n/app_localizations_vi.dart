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
  String get tabMyLibrary => 'Tủ sách';

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
  String get scanCameraUnavailable =>
      'Không mở được camera. Bạn vẫn có thể nhập mã bằng tay.';

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

  @override
  String get browseTitle => 'Duyệt danh mục';

  @override
  String get browseSubjects => 'Chủ đề';

  @override
  String get browseClassifications => 'Phân loại DDC';

  @override
  String get browseAuthors => 'Tác giả';

  @override
  String get browseCollections => 'Bộ sưu tập';

  @override
  String get browseMajors => 'Ngành đào tạo';

  @override
  String get browseCourses => 'Môn học';

  @override
  String get browseTheses => 'Luận văn / Luận án';

  @override
  String get browseSerials => 'Ấn phẩm định kỳ';

  @override
  String get browseFilterHint => 'Lọc trong danh sách…';

  @override
  String get browseEmpty => 'Không có mục nào.';

  @override
  String bibCountLabel(int count) {
    return '$count tài liệu';
  }

  @override
  String filteringBy(String label) {
    return 'Đang lọc: $label';
  }

  @override
  String get newBooks => 'Sách mới bổ sung';

  @override
  String get popularBooks => 'Được mượn nhiều';

  @override
  String get latestNews => 'Tin tức';

  @override
  String get viewAll => 'Xem tất cả';

  @override
  String get quickLinks => 'Liên kết hữu ích';

  @override
  String get statistics => 'Kho tài liệu';

  @override
  String get statBibs => 'biểu ghi';

  @override
  String get statItems => 'bản in';

  @override
  String get statDigital => 'tài liệu số';

  @override
  String get statReaders => 'bạn đọc';

  @override
  String get callAction => 'Gọi';

  @override
  String get directionsAction => 'Chỉ đường';

  @override
  String get newsTitle => 'Tin tức';

  @override
  String get newsEmpty => 'Chưa có tin nào.';

  @override
  String get pagesTitle => 'Thông tin thư viện';

  @override
  String get allCategories => 'Tất cả';

  @override
  String get relatedNews => 'Tin liên quan';

  @override
  String viewCount(int count) {
    return '$count lượt xem';
  }

  @override
  String receivedIssues(int count) {
    return '$count số đã nhận';
  }

  @override
  String latestIssue(String no, String date) {
    return 'Số mới nhất: $no ($date)';
  }

  @override
  String get courseDocsTitle => 'Tài liệu môn học';

  @override
  String get thesesHint => 'Tìm luận văn, luận án…';

  @override
  String get serialsHint => 'Tìm báo, tạp chí…';

  @override
  String get topAuthors => 'Tác giả có nhiều tài liệu';

  @override
  String get letterAll => 'Nổi bật';

  @override
  String get browseShortcuts => 'Duyệt theo';

  @override
  String get featured => 'Nổi bật';

  @override
  String get cardTitle => 'Thẻ thư viện';

  @override
  String get cardNumberLabel => 'Số thẻ';

  @override
  String get readerType => 'Loại bạn đọc';

  @override
  String get faculty => 'Khoa';

  @override
  String get classLabel => 'Lớp';

  @override
  String get cardExpiry => 'Hạn thẻ';

  @override
  String get cardStatus => 'Trạng thái';

  @override
  String get cardActive => 'Đang hoạt động';

  @override
  String get cardInactiveNote =>
      'Thẻ không còn hiệu lực nên không hiện mã. Liên hệ thư viện để gia hạn hoặc mở khóa.';

  @override
  String cardOfflineNote(String time) {
    return 'Không có mạng — đang hiện bản lưu trên máy, cập nhật lúc $time.';
  }

  @override
  String get cardRenewRequest => 'Gửi yêu cầu gia hạn thẻ';

  @override
  String get cardRenewReason => 'Lý do (không bắt buộc)';

  @override
  String get cardRenewSent =>
      'Đã gửi yêu cầu gia hạn thẻ. Thư viện sẽ xử lý và báo lại.';

  @override
  String get cardRenewals => 'Yêu cầu đã gửi';

  @override
  String get cardShowAtDesk => 'Đưa mã này cho quầy thủ thư hoặc cổng ra vào.';

  @override
  String get warningsLabel => 'Lưu ý';

  @override
  String loanCountLabel(int count) {
    return 'Đang mượn $count';
  }

  @override
  String finesOwed(String amount) {
    return 'Nợ phạt $amount';
  }

  @override
  String get myLibraryTitle => 'Sách của tôi';

  @override
  String get currentLoans => 'Đang mượn';

  @override
  String get loanHistory => 'Lịch sử';

  @override
  String get holdsTab => 'Đặt giữ';

  @override
  String get finesTab => 'Tiền phạt';

  @override
  String dueIn(int days) {
    return 'Còn $days ngày';
  }

  @override
  String get dueToday => 'Hạn trả hôm nay';

  @override
  String overdueBy(int days) {
    return 'Quá hạn $days ngày';
  }

  @override
  String dueOn(String date) {
    return 'Hạn trả $date';
  }

  @override
  String get renewAction => 'Gia hạn';

  @override
  String get renewalPending =>
      'Đã gửi yêu cầu gia hạn. Hạn trả đổi sau khi thư viện duyệt.';

  @override
  String renewedTo(String date) {
    return 'Đã gia hạn, hạn trả mới $date.';
  }

  @override
  String renewCount(int count, int max) {
    return 'Đã gia hạn $count/$max';
  }

  @override
  String get noLoans => 'Bạn không mượn cuốn nào.';

  @override
  String get noHistory => 'Chưa có lịch sử mượn.';

  @override
  String get noHolds => 'Không có đặt giữ nào.';

  @override
  String get noFines => 'Không có khoản phạt nào.';

  @override
  String get cancelHold => 'Hủy đặt giữ';

  @override
  String cancelHoldConfirm(String title) {
    return 'Hủy đặt giữ \"$title\"?';
  }

  @override
  String get holdCancelled => 'Đã hủy đặt giữ.';

  @override
  String queuePosition(int position) {
    return 'Thứ $position trong hàng đợi';
  }

  @override
  String get holdReady => 'Sẵn sàng nhận';

  @override
  String get holdWaiting => 'Đang chờ';

  @override
  String get holdFulfilled => 'Đã nhận';

  @override
  String get holdExpired => 'Hết hạn';

  @override
  String get holdCancelledStatus => 'Đã hủy';

  @override
  String pickupAt(String place) {
    return 'Nhận tại $place';
  }

  @override
  String holdExpires(String date) {
    return 'Giữ đến $date';
  }

  @override
  String get totalOutstanding => 'Còn phải nộp';

  @override
  String get totalPaid => 'Đã nộp';

  @override
  String get finePaymentGuide =>
      'Thanh toán tại quầy thủ thư. Ứng dụng không thu tiền.';

  @override
  String get fineTypeOverdue => 'Quá hạn';

  @override
  String get fineTypeLost => 'Mất';

  @override
  String get fineTypeDamaged => 'Hỏng';

  @override
  String get fineTypeOther => 'Khác';

  @override
  String get filterAll => 'Tất cả';

  @override
  String get filter30Days => '30 ngày qua';

  @override
  String get filterThisYear => 'Năm nay';

  @override
  String get historySearchHint => 'Tìm theo nhan đề, mã vạch…';

  @override
  String returnedOn(String date) {
    return 'Trả ngày $date';
  }

  @override
  String borrowedOn(String date) {
    return 'Mượn ngày $date';
  }

  @override
  String estimatedFine(String amount) {
    return 'Phạt dự kiến $amount';
  }

  @override
  String get cancelAction => 'Hủy';

  @override
  String get confirmAction => 'Đồng ý';

  @override
  String get selfCheckoutTitle => 'Mượn tự phục vụ';

  @override
  String get selfCheckoutIntro =>
      'Tự vào kho chọn sách, quét mã vạch trên gáy sách để mượn. Máy chủ kiểm chính sách từng cuốn.';

  @override
  String get selfCheckoutDisabled =>
      'Thư viện chưa mở chức năng mượn tự phục vụ.';

  @override
  String get verifyStepTitle => 'Bước 1 · Xác nhận bạn đang ở thư viện';

  @override
  String get verifyWifiHint =>
      'Hãy nối vào Wi-Fi của thư viện rồi bấm kiểm tra.';

  @override
  String get verifyWifiAction => 'Kiểm tra Wi-Fi';

  @override
  String verifyWifiCurrent(String ssid) {
    return 'Wi-Fi hiện tại: $ssid';
  }

  @override
  String get verifyWifiUnknown =>
      'Không đọc được tên Wi-Fi. Cấp quyền vị trí cho ứng dụng và bật định vị.';

  @override
  String get verifyQrHint => 'Quét mã QR trạm dán ở cửa kho.';

  @override
  String get verifyQrAction => 'Quét mã trạm';

  @override
  String get verifyQrManual => 'Nhập nội dung mã trạm';

  @override
  String get verifyNoneHint => 'Thư viện không yêu cầu xác thực vị trí.';

  @override
  String get verifyStart => 'Bắt đầu';

  @override
  String get verifying => 'Đang xác thực…';

  @override
  String verifiedAt(String place) {
    return 'Đã xác thực tại $place';
  }

  @override
  String get verifiedPlain => 'Đã xác thực vị trí';

  @override
  String verifiedUntil(String time) {
    return 'Hiệu lực đến $time';
  }

  @override
  String get verifyExpired => 'Phiếu xác thực đã hết hiệu lực, xác thực lại.';

  @override
  String get scanBooksTitle => 'Bước 2 · Quét mã vạch sách';

  @override
  String get scanBooksHint =>
      'Đưa mã vạch trên gáy sách vào khung. Quét liên tiếp nhiều cuốn.';

  @override
  String get enterBarcode => 'Nhập mã vạch';

  @override
  String checkoutOk(String date) {
    return 'Đã mượn · hạn trả $date';
  }

  @override
  String get checkoutFailed => 'Từ chối';

  @override
  String get finishAction => 'Kết thúc';

  @override
  String borrowedCount(int count) {
    return 'Đã mượn $count cuốn';
  }

  @override
  String rejectedCount(int count) {
    return '$count cuốn bị từ chối';
  }

  @override
  String get slipTitle => 'Phiếu mượn';

  @override
  String slipCode(String code) {
    return 'Số phiếu $code';
  }

  @override
  String get slipEmpty => 'Chưa mượn cuốn nào.';

  @override
  String get newSession => 'Mượn tiếp';

  @override
  String alreadyScanned(String barcode) {
    return 'Mã $barcode đã quét rồi.';
  }

  @override
  String checkingBarcode(String barcode) {
    return 'Đang kiểm $barcode…';
  }

  @override
  String get openMyLibrary => 'Xem Sách của tôi';

  @override
  String get digitalTitle => 'Tài liệu số';

  @override
  String get digitalSearchHint => 'Tìm tài liệu số…';

  @override
  String get digitalFullText => 'Tìm trong toàn văn';

  @override
  String get digitalAll => 'Tất cả';

  @override
  String get digitalTabLibrary => 'Thư viện';

  @override
  String get digitalTabOffline => 'Ngoại tuyến';

  @override
  String get digitalTabRequests => 'Yêu cầu';

  @override
  String get digitalTabHistory => 'Lịch sử';

  @override
  String get accessPublic => 'Công khai';

  @override
  String get accessInternal => 'Nội bộ';

  @override
  String get accessRestricted => 'Hạn chế';

  @override
  String get accessForbidden => 'Cấm';

  @override
  String get readAction => 'Đọc';

  @override
  String get downloadOffline => 'Tải đọc ngoại tuyến';

  @override
  String get downloadingPackage => 'Đang tải gói…';

  @override
  String offlineSaved(String date) {
    return 'Đã lưu để đọc ngoại tuyến, hạn đến $date.';
  }

  @override
  String get offlineExpired => 'Gói đã hết hạn';

  @override
  String offlineExpires(String date) {
    return 'Hết hạn $date';
  }

  @override
  String get offlineDelete => 'Xóa khỏi máy';

  @override
  String get offlineDeleted => 'Đã xóa gói ngoại tuyến.';

  @override
  String get offlineEmpty => 'Chưa có tài liệu nào tải về máy.';

  @override
  String get offlineReadNote =>
      'Bản ngoại tuyến — đọc không cần mạng, tự hết hạn.';

  @override
  String get offlineNoSearch => 'Bản ngoại tuyến không tìm được trong văn bản.';

  @override
  String get requestAccess => 'Gửi yêu cầu truy cập';

  @override
  String get requestReasonHint => 'Lý do sử dụng (bắt buộc)';

  @override
  String get requestSent => 'Đã gửi yêu cầu, thư viện sẽ duyệt và báo lại.';

  @override
  String get requestStatusPending => 'Chờ duyệt';

  @override
  String get requestStatusApproved => 'Đã duyệt';

  @override
  String get requestStatusRejected => 'Từ chối';

  @override
  String get requestStatusExpired => 'Hết hạn';

  @override
  String get requestStatusRevoked => 'Đã thu hồi';

  @override
  String get requestsEmpty => 'Chưa gửi yêu cầu nào.';

  @override
  String get historyEmpty => 'Chưa xem hay tải tài liệu số nào.';

  @override
  String get actionView => 'Xem';

  @override
  String get actionDownload => 'Tải';

  @override
  String get actionPrint => 'In';

  @override
  String get actionOfflineDownload => 'Tải ngoại tuyến';

  @override
  String pagesLabel2(int count) {
    return '$count trang';
  }

  @override
  String previewOnly(int count) {
    return 'Chỉ xem thử $count trang đầu';
  }

  @override
  String pageOf(int page, int total) {
    return 'Trang $page/$total';
  }

  @override
  String get goToPage => 'Đến trang';

  @override
  String get bookmarkAdd => 'Đánh dấu trang';

  @override
  String get bookmarkRemove => 'Bỏ đánh dấu';

  @override
  String get bookmarks => 'Trang đã đánh dấu';

  @override
  String get bookmarksEmpty => 'Chưa đánh dấu trang nào.';

  @override
  String get findInText => 'Tìm trong văn bản';

  @override
  String get findHint => 'Nhập từ cần tìm…';

  @override
  String get findNoHit => 'Không thấy trong phần được xem.';

  @override
  String findHits(int count) {
    return '$count chỗ khớp';
  }

  @override
  String get watermarkNote =>
      'Trang có chữ chìm số thẻ của bạn, không chia sẻ.';

  @override
  String get secureNote => 'Tài liệu không cho tải: đã chặn chụp màn hình.';

  @override
  String get noPermission => 'Bạn chưa được phép đọc tài liệu này.';

  @override
  String sizeLabel(String size) {
    return '$size';
  }

  @override
  String get digitalOpenError => 'Không mở được tài liệu.';

  @override
  String get collectionLabel => 'Bộ sưu tập';

  @override
  String get checksumMismatch => 'Tệp tải về không khớp mã kiểm, đã bỏ.';

  @override
  String loadingPage(int page) {
    return 'Đang tải trang $page…';
  }

  @override
  String get digitalSignInHint =>
      'Đăng nhập để xem tài liệu nội bộ và gửi yêu cầu truy cập.';

  @override
  String get notificationsTitle => 'Thông báo';

  @override
  String get notificationsEmpty => 'Chưa có thông báo nào.';

  @override
  String get unreadOnly => 'Chỉ chưa đọc';

  @override
  String get markAllRead => 'Đọc hết';

  @override
  String get notificationSettings => 'Cài đặt thông báo';

  @override
  String get notificationSettingsHint =>
      'Tắt loại nào thì không nhận email và thông báo đẩy loại đó; dòng trong ứng dụng vẫn ghi.';

  @override
  String get notificationSettingsSaved => 'Đã lưu cài đặt thông báo.';

  @override
  String get pushDisabledNote =>
      'Thông báo đẩy chưa bật trên máy này (thiếu cấu hình Firebase). Thông báo vẫn xem được ở đây.';

  @override
  String get pushEnabledNote => 'Thiết bị đã đăng ký nhận thông báo đẩy.';

  @override
  String get profileTitle => 'Hồ sơ';

  @override
  String get editContact => 'Cập nhật liên hệ';

  @override
  String get emailLabel => 'Email';

  @override
  String get phoneLabel => 'Điện thoại';

  @override
  String get addressLabel => 'Địa chỉ';

  @override
  String get contactSaved => 'Đã cập nhật thông tin liên hệ.';

  @override
  String get changePassword => 'Đổi mật khẩu';

  @override
  String get currentPassword => 'Mật khẩu hiện tại';

  @override
  String get newPassword => 'Mật khẩu mới';

  @override
  String get confirmPassword => 'Nhập lại mật khẩu mới';

  @override
  String get passwordMismatch => 'Hai mật khẩu mới không khớp.';

  @override
  String get passwordChanged => 'Đã đổi mật khẩu.';

  @override
  String get saveAction => 'Lưu';

  @override
  String get biometricLock => 'Mở khóa bằng vân tay / khuôn mặt';

  @override
  String get biometricLockHint =>
      'Lần mở ứng dụng sau phải xác thực sinh trắc học mới vào được tài khoản.';

  @override
  String get biometricUnavailable => 'Máy này chưa có sinh trắc học.';

  @override
  String get biometricPrompt => 'Xác thực để mở tài khoản thư viện';

  @override
  String get unlockAction => 'Mở khóa';

  @override
  String get lockedNote => 'Tài khoản đang khóa bằng sinh trắc học.';

  @override
  String versionInfo(String app, String min) {
    return 'Phiên bản $app · máy chủ yêu cầu tối thiểu $min';
  }

  @override
  String get languageVietnamese => 'Tiếng Việt';

  @override
  String get languageEnglish => 'English';

  @override
  String get textSize => 'Cỡ chữ';

  @override
  String get studentCode => 'Mã sinh viên';

  @override
  String get majorLabel => 'Ngành';

  @override
  String get courseYearLabel => 'Khóa';

  @override
  String get serverLabel => 'Máy chủ';

  @override
  String get a11yLibraryLogo => 'Biểu tượng thư viện';

  @override
  String a11yCardBarcode(String number) {
    return 'Mã vạch thẻ thư viện, số thẻ $number';
  }

  @override
  String a11yCardQr(String number) {
    return 'Mã QR thẻ thư viện, số thẻ $number';
  }

  @override
  String a11yCover(String title) {
    return 'Ảnh bìa: $title';
  }

  @override
  String get a11yScannerView =>
      'Khung quét mã. Hướng máy ảnh vào mã vạch hoặc mã QR.';

  @override
  String get a11yCheckoutScannerView =>
      'Khung quét mã vạch sách. Hướng máy ảnh vào mã vạch trên gáy sách.';

  @override
  String get a11yOpenDetail => 'Mở chi tiết tài liệu';

  @override
  String a11yReaderPage(int page, int total) {
    return 'Trang $page trên $total của tài liệu';
  }

  @override
  String get warehouseLabel => 'Kho';

  @override
  String get advancedFiltersHint =>
      'Danh mục lấy từ bộ đếm của máy chủ; để \"Tất cả\" là không lọc.';

  @override
  String get outline => 'Mục lục';

  @override
  String get outlineEmpty => 'Tài liệu này không có mục lục.';

  @override
  String get outlineOffline => 'Mục lục lưu kèm gói ngoại tuyến.';
}
