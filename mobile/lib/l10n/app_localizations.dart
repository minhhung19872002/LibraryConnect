import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_vi.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of L10n
/// returned by `L10n.of(context)`.
///
/// Applications need to include `L10n.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: L10n.localizationsDelegates,
///   supportedLocales: L10n.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the L10n.supportedLocales
/// property.
abstract class L10n {
  L10n(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static L10n of(BuildContext context) {
    return Localizations.of<L10n>(context, L10n)!;
  }

  static const LocalizationsDelegate<L10n> delegate = _L10nDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('vi'),
  ];

  /// No description provided for @appName.
  ///
  /// In vi, this message translates to:
  /// **'LibraryConnect'**
  String get appName;

  /// No description provided for @tabHome.
  ///
  /// In vi, this message translates to:
  /// **'Trang chủ'**
  String get tabHome;

  /// No description provided for @tabSearch.
  ///
  /// In vi, this message translates to:
  /// **'Tra cứu'**
  String get tabSearch;

  /// No description provided for @tabScan.
  ///
  /// In vi, this message translates to:
  /// **'Quét mã'**
  String get tabScan;

  /// No description provided for @tabMyLibrary.
  ///
  /// In vi, this message translates to:
  /// **'Sách của tôi'**
  String get tabMyLibrary;

  /// No description provided for @tabAccount.
  ///
  /// In vi, this message translates to:
  /// **'Tài khoản'**
  String get tabAccount;

  /// No description provided for @loginTitle.
  ///
  /// In vi, this message translates to:
  /// **'Đăng nhập bạn đọc'**
  String get loginTitle;

  /// No description provided for @loginSubtitle.
  ///
  /// In vi, this message translates to:
  /// **'Dùng số thẻ thư viện và mật khẩu được cấp.'**
  String get loginSubtitle;

  /// No description provided for @cardNumber.
  ///
  /// In vi, this message translates to:
  /// **'Số thẻ thư viện'**
  String get cardNumber;

  /// No description provided for @password.
  ///
  /// In vi, this message translates to:
  /// **'Mật khẩu'**
  String get password;

  /// No description provided for @rememberCard.
  ///
  /// In vi, this message translates to:
  /// **'Ghi nhớ số thẻ'**
  String get rememberCard;

  /// No description provided for @signIn.
  ///
  /// In vi, this message translates to:
  /// **'Đăng nhập'**
  String get signIn;

  /// No description provided for @signOut.
  ///
  /// In vi, this message translates to:
  /// **'Đăng xuất'**
  String get signOut;

  /// No description provided for @forgotPassword.
  ///
  /// In vi, this message translates to:
  /// **'Quên mật khẩu?'**
  String get forgotPassword;

  /// No description provided for @forgotPasswordHelp.
  ///
  /// In vi, this message translates to:
  /// **'Ứng dụng không tự đặt lại mật khẩu. Vui lòng mang thẻ tới quầy hoặc liên hệ thư viện để được cấp lại.'**
  String get forgotPasswordHelp;

  /// No description provided for @cardNumberRequired.
  ///
  /// In vi, this message translates to:
  /// **'Nhập số thẻ thư viện.'**
  String get cardNumberRequired;

  /// No description provided for @passwordRequired.
  ///
  /// In vi, this message translates to:
  /// **'Nhập mật khẩu.'**
  String get passwordRequired;

  /// No description provided for @continueAsGuest.
  ///
  /// In vi, this message translates to:
  /// **'Tra cứu không cần đăng nhập'**
  String get continueAsGuest;

  /// No description provided for @searchHint.
  ///
  /// In vi, this message translates to:
  /// **'Nhập nhan đề, tác giả, từ khóa…'**
  String get searchHint;

  /// No description provided for @searchHintNoAccent.
  ///
  /// In vi, this message translates to:
  /// **'Gõ không dấu vẫn tìm thấy.'**
  String get searchHintNoAccent;

  /// No description provided for @retry.
  ///
  /// In vi, this message translates to:
  /// **'Thử lại'**
  String get retry;

  /// No description provided for @offlineTitle.
  ///
  /// In vi, this message translates to:
  /// **'Không có kết nối'**
  String get offlineTitle;

  /// No description provided for @offlineBody.
  ///
  /// In vi, this message translates to:
  /// **'Ứng dụng đang dùng dữ liệu đã lưu. Hãy kết nối mạng để cập nhật.'**
  String get offlineBody;

  /// No description provided for @loading.
  ///
  /// In vi, this message translates to:
  /// **'Đang tải…'**
  String get loading;

  /// No description provided for @welcome.
  ///
  /// In vi, this message translates to:
  /// **'Xin chào, {name}'**
  String welcome(String name);

  /// No description provided for @libraryInfo.
  ///
  /// In vi, this message translates to:
  /// **'Thông tin thư viện'**
  String get libraryInfo;

  /// No description provided for @openingHours.
  ///
  /// In vi, this message translates to:
  /// **'Giờ mở cửa'**
  String get openingHours;

  /// No description provided for @call.
  ///
  /// In vi, this message translates to:
  /// **'Gọi'**
  String get call;

  /// No description provided for @directions.
  ///
  /// In vi, this message translates to:
  /// **'Chỉ đường'**
  String get directions;

  /// No description provided for @settings.
  ///
  /// In vi, this message translates to:
  /// **'Cài đặt'**
  String get settings;

  /// No description provided for @theme.
  ///
  /// In vi, this message translates to:
  /// **'Giao diện'**
  String get theme;

  /// No description provided for @themeSystem.
  ///
  /// In vi, this message translates to:
  /// **'Theo hệ thống'**
  String get themeSystem;

  /// No description provided for @themeLight.
  ///
  /// In vi, this message translates to:
  /// **'Sáng'**
  String get themeLight;

  /// No description provided for @themeDark.
  ///
  /// In vi, this message translates to:
  /// **'Tối'**
  String get themeDark;

  /// No description provided for @language.
  ///
  /// In vi, this message translates to:
  /// **'Ngôn ngữ'**
  String get language;

  /// No description provided for @version.
  ///
  /// In vi, this message translates to:
  /// **'Phiên bản'**
  String get version;

  /// No description provided for @sessionExpired.
  ///
  /// In vi, this message translates to:
  /// **'Phiên đăng nhập đã hết. Vui lòng đăng nhập lại.'**
  String get sessionExpired;

  /// No description provided for @updateRequiredTitle.
  ///
  /// In vi, this message translates to:
  /// **'Cần cập nhật ứng dụng'**
  String get updateRequiredTitle;

  /// No description provided for @updateRequiredBody.
  ///
  /// In vi, this message translates to:
  /// **'Phiên bản đang dùng ({current}) đã cũ, thư viện yêu cầu từ {min} trở lên.'**
  String updateRequiredBody(String current, String min);

  /// No description provided for @update.
  ///
  /// In vi, this message translates to:
  /// **'Cập nhật'**
  String get update;

  /// No description provided for @mustChangePassword.
  ///
  /// In vi, this message translates to:
  /// **'Bạn đang dùng mật khẩu tạm, hãy đổi mật khẩu trước khi tiếp tục.'**
  String get mustChangePassword;

  /// No description provided for @poweredBy.
  ///
  /// In vi, this message translates to:
  /// **'Vận hành bởi LibraryConnect'**
  String get poweredBy;

  /// No description provided for @searchTitle.
  ///
  /// In vi, this message translates to:
  /// **'Tra cứu'**
  String get searchTitle;

  /// No description provided for @scopeAll.
  ///
  /// In vi, this message translates to:
  /// **'Tất cả'**
  String get scopeAll;

  /// No description provided for @scopeTitle.
  ///
  /// In vi, this message translates to:
  /// **'Nhan đề'**
  String get scopeTitle;

  /// No description provided for @scopeAuthor.
  ///
  /// In vi, this message translates to:
  /// **'Tác giả'**
  String get scopeAuthor;

  /// No description provided for @scopeSubject.
  ///
  /// In vi, this message translates to:
  /// **'Chủ đề'**
  String get scopeSubject;

  /// No description provided for @scopeKeyword.
  ///
  /// In vi, this message translates to:
  /// **'Từ khóa'**
  String get scopeKeyword;

  /// No description provided for @scopeIsbn.
  ///
  /// In vi, this message translates to:
  /// **'ISBN'**
  String get scopeIsbn;

  /// No description provided for @scopePublisher.
  ///
  /// In vi, this message translates to:
  /// **'Nhà xuất bản'**
  String get scopePublisher;

  /// No description provided for @scopeCallNumber.
  ///
  /// In vi, this message translates to:
  /// **'Ký hiệu xếp giá'**
  String get scopeCallNumber;

  /// No description provided for @recentSearches.
  ///
  /// In vi, this message translates to:
  /// **'Tìm gần đây'**
  String get recentSearches;

  /// No description provided for @clearAll.
  ///
  /// In vi, this message translates to:
  /// **'Xóa hết'**
  String get clearAll;

  /// No description provided for @suggestions.
  ///
  /// In vi, this message translates to:
  /// **'Gợi ý'**
  String get suggestions;

  /// No description provided for @resultCount.
  ///
  /// In vi, this message translates to:
  /// **'{count} kết quả'**
  String resultCount(int count);

  /// No description provided for @resultCountCapped.
  ///
  /// In vi, this message translates to:
  /// **'Hơn {count} kết quả'**
  String resultCountCapped(int count);

  /// No description provided for @noResults.
  ///
  /// In vi, this message translates to:
  /// **'Không tìm thấy tài liệu nào.'**
  String get noResults;

  /// No description provided for @noResultsHint.
  ///
  /// In vi, this message translates to:
  /// **'Thử từ khóa ngắn hơn, gõ không dấu, hoặc bỏ bớt bộ lọc.'**
  String get noResultsHint;

  /// No description provided for @sortLabel.
  ///
  /// In vi, this message translates to:
  /// **'Sắp xếp'**
  String get sortLabel;

  /// No description provided for @sortRelevance.
  ///
  /// In vi, this message translates to:
  /// **'Liên quan nhất'**
  String get sortRelevance;

  /// No description provided for @sortNewest.
  ///
  /// In vi, this message translates to:
  /// **'Mới nhất'**
  String get sortNewest;

  /// No description provided for @sortTitle.
  ///
  /// In vi, this message translates to:
  /// **'Nhan đề'**
  String get sortTitle;

  /// No description provided for @sortAuthor.
  ///
  /// In vi, this message translates to:
  /// **'Tác giả'**
  String get sortAuthor;

  /// No description provided for @sortPopular.
  ///
  /// In vi, this message translates to:
  /// **'Được mượn nhiều'**
  String get sortPopular;

  /// No description provided for @filters.
  ///
  /// In vi, this message translates to:
  /// **'Bộ lọc'**
  String get filters;

  /// No description provided for @applyFilters.
  ///
  /// In vi, this message translates to:
  /// **'Áp dụng'**
  String get applyFilters;

  /// No description provided for @clearFilters.
  ///
  /// In vi, this message translates to:
  /// **'Bỏ lọc'**
  String get clearFilters;

  /// No description provided for @advancedSearch.
  ///
  /// In vi, this message translates to:
  /// **'Tra cứu nâng cao'**
  String get advancedSearch;

  /// No description provided for @addClause.
  ///
  /// In vi, this message translates to:
  /// **'Thêm điều kiện'**
  String get addClause;

  /// No description provided for @connectorAnd.
  ///
  /// In vi, this message translates to:
  /// **'VÀ'**
  String get connectorAnd;

  /// No description provided for @connectorOr.
  ///
  /// In vi, this message translates to:
  /// **'HOẶC'**
  String get connectorOr;

  /// No description provided for @connectorNot.
  ///
  /// In vi, this message translates to:
  /// **'KHÔNG'**
  String get connectorNot;

  /// No description provided for @yearFrom.
  ///
  /// In vi, this message translates to:
  /// **'Từ năm'**
  String get yearFrom;

  /// No description provided for @yearTo.
  ///
  /// In vi, this message translates to:
  /// **'Đến năm'**
  String get yearTo;

  /// No description provided for @onlyDigital.
  ///
  /// In vi, this message translates to:
  /// **'Chỉ tài liệu số'**
  String get onlyDigital;

  /// No description provided for @onlyAvailable.
  ///
  /// In vi, this message translates to:
  /// **'Chỉ còn bản rảnh'**
  String get onlyAvailable;

  /// No description provided for @searchAction.
  ///
  /// In vi, this message translates to:
  /// **'Tìm'**
  String get searchAction;

  /// No description provided for @loadMoreError.
  ///
  /// In vi, this message translates to:
  /// **'Không tải thêm được. Chạm để thử lại.'**
  String get loadMoreError;

  /// No description provided for @availableCopies.
  ///
  /// In vi, this message translates to:
  /// **'{count} bản sẵn sàng'**
  String availableCopies(int count);

  /// No description provided for @allOnLoan.
  ///
  /// In vi, this message translates to:
  /// **'Hết bản, đang cho mượn'**
  String get allOnLoan;

  /// No description provided for @noCopies.
  ///
  /// In vi, this message translates to:
  /// **'Chưa có bản in'**
  String get noCopies;

  /// No description provided for @digitalCount.
  ///
  /// In vi, this message translates to:
  /// **'{count} tài liệu số'**
  String digitalCount(int count);

  /// No description provided for @scanTitle.
  ///
  /// In vi, this message translates to:
  /// **'Quét mã'**
  String get scanTitle;

  /// No description provided for @scanHint.
  ///
  /// In vi, this message translates to:
  /// **'Đưa mã ISBN, mã vạch ĐKCB hoặc mã QR vào khung.'**
  String get scanHint;

  /// No description provided for @scanTorch.
  ///
  /// In vi, this message translates to:
  /// **'Đèn pin'**
  String get scanTorch;

  /// No description provided for @scanSwitchCamera.
  ///
  /// In vi, this message translates to:
  /// **'Đổi camera'**
  String get scanSwitchCamera;

  /// No description provided for @scanNotFound.
  ///
  /// In vi, this message translates to:
  /// **'Không tìm thấy tài liệu cho mã {code}.'**
  String scanNotFound(String code);

  /// No description provided for @scanManualSearch.
  ///
  /// In vi, this message translates to:
  /// **'Tra cứu thủ công'**
  String get scanManualSearch;

  /// No description provided for @scanEnterCode.
  ///
  /// In vi, this message translates to:
  /// **'Nhập mã bằng tay'**
  String get scanEnterCode;

  /// No description provided for @scanLookingUp.
  ///
  /// In vi, this message translates to:
  /// **'Đang tra mã {code}…'**
  String scanLookingUp(String code);

  /// No description provided for @scanFoundCopy.
  ///
  /// In vi, this message translates to:
  /// **'Bản in {barcode} · {status}'**
  String scanFoundCopy(String barcode, String status);

  /// No description provided for @scanIsbnMany.
  ///
  /// In vi, this message translates to:
  /// **'{count} tài liệu cùng ISBN {isbn}'**
  String scanIsbnMany(int count, String isbn);

  /// No description provided for @scanCameraDenied.
  ///
  /// In vi, this message translates to:
  /// **'Chưa được phép dùng camera. Bạn vẫn có thể nhập mã bằng tay.'**
  String get scanCameraDenied;

  /// No description provided for @scanOpen.
  ///
  /// In vi, this message translates to:
  /// **'Mở chi tiết'**
  String get scanOpen;

  /// No description provided for @scanStationCode.
  ///
  /// In vi, this message translates to:
  /// **'Đây là mã trạm mượn tự phục vụ, dùng ở màn hình Mượn tự phục vụ.'**
  String get scanStationCode;

  /// No description provided for @detailTabInfo.
  ///
  /// In vi, this message translates to:
  /// **'Thông tin'**
  String get detailTabInfo;

  /// No description provided for @detailTabItems.
  ///
  /// In vi, this message translates to:
  /// **'Bản in'**
  String get detailTabItems;

  /// No description provided for @detailTabDigital.
  ///
  /// In vi, this message translates to:
  /// **'Tài liệu số'**
  String get detailTabDigital;

  /// No description provided for @detailTabMarc.
  ///
  /// In vi, this message translates to:
  /// **'MARC'**
  String get detailTabMarc;

  /// No description provided for @detailTabReviews.
  ///
  /// In vi, this message translates to:
  /// **'Nhận xét'**
  String get detailTabReviews;

  /// No description provided for @holdAction.
  ///
  /// In vi, this message translates to:
  /// **'Đặt giữ chỗ'**
  String get holdAction;

  /// No description provided for @queueAction.
  ///
  /// In vi, this message translates to:
  /// **'Xếp hàng đợi'**
  String get queueAction;

  /// No description provided for @holdPlaced.
  ///
  /// In vi, this message translates to:
  /// **'Đã đặt giữ. Thư viện sẽ báo khi sách sẵn sàng.'**
  String get holdPlaced;

  /// No description provided for @holdQueued.
  ///
  /// In vi, this message translates to:
  /// **'Đã xếp hàng, bạn đứng thứ {position}.'**
  String holdQueued(int position);

  /// No description provided for @citeAction.
  ///
  /// In vi, this message translates to:
  /// **'Trích dẫn'**
  String get citeAction;

  /// No description provided for @shareAction.
  ///
  /// In vi, this message translates to:
  /// **'Chia sẻ'**
  String get shareAction;

  /// No description provided for @favoriteAction.
  ///
  /// In vi, this message translates to:
  /// **'Yêu thích'**
  String get favoriteAction;

  /// No description provided for @favoriteAdded.
  ///
  /// In vi, this message translates to:
  /// **'Đã thêm vào yêu thích.'**
  String get favoriteAdded;

  /// No description provided for @favoriteRemoved.
  ///
  /// In vi, this message translates to:
  /// **'Đã bỏ khỏi yêu thích.'**
  String get favoriteRemoved;

  /// No description provided for @copyAction.
  ///
  /// In vi, this message translates to:
  /// **'Sao chép'**
  String get copyAction;

  /// No description provided for @copied.
  ///
  /// In vi, this message translates to:
  /// **'Đã sao chép.'**
  String get copied;

  /// No description provided for @abstractLabel.
  ///
  /// In vi, this message translates to:
  /// **'Tóm tắt'**
  String get abstractLabel;

  /// No description provided for @subjectsLabel.
  ///
  /// In vi, this message translates to:
  /// **'Chủ đề'**
  String get subjectsLabel;

  /// No description provided for @keywordsLabel.
  ///
  /// In vi, this message translates to:
  /// **'Từ khóa'**
  String get keywordsLabel;

  /// No description provided for @isbdLabel.
  ///
  /// In vi, this message translates to:
  /// **'Mô tả thư mục'**
  String get isbdLabel;

  /// No description provided for @relatedLabel.
  ///
  /// In vi, this message translates to:
  /// **'Tài liệu liên quan'**
  String get relatedLabel;

  /// No description provided for @externalLinks.
  ///
  /// In vi, this message translates to:
  /// **'Toàn văn ở nơi khác'**
  String get externalLinks;

  /// No description provided for @noItems.
  ///
  /// In vi, this message translates to:
  /// **'Tài liệu chưa có bản in nào.'**
  String get noItems;

  /// No description provided for @noDigital.
  ///
  /// In vi, this message translates to:
  /// **'Không có tài liệu số.'**
  String get noDigital;

  /// No description provided for @noReviews.
  ///
  /// In vi, this message translates to:
  /// **'Chưa có nhận xét nào.'**
  String get noReviews;

  /// No description provided for @marcUnreadable.
  ///
  /// In vi, this message translates to:
  /// **'Không đọc được biểu ghi MARC.'**
  String get marcUnreadable;

  /// No description provided for @leaderLabel.
  ///
  /// In vi, this message translates to:
  /// **'Đầu biểu ghi'**
  String get leaderLabel;

  /// No description provided for @dueBack.
  ///
  /// In vi, this message translates to:
  /// **'Hạn trả {date}'**
  String dueBack(String date);

  /// No description provided for @writeReview.
  ///
  /// In vi, this message translates to:
  /// **'Viết nhận xét'**
  String get writeReview;

  /// No description provided for @sendReview.
  ///
  /// In vi, this message translates to:
  /// **'Gửi'**
  String get sendReview;

  /// No description provided for @reviewSent.
  ///
  /// In vi, this message translates to:
  /// **'Đã gửi nhận xét, chờ thư viện duyệt.'**
  String get reviewSent;

  /// No description provided for @reviewHint.
  ///
  /// In vi, this message translates to:
  /// **'Cảm nhận của bạn về tài liệu'**
  String get reviewHint;

  /// No description provided for @signInToContinue.
  ///
  /// In vi, this message translates to:
  /// **'Đăng nhập để tiếp tục'**
  String get signInToContinue;

  /// No description provided for @pagesLabel.
  ///
  /// In vi, this message translates to:
  /// **'Số trang'**
  String get pagesLabel;

  /// No description provided for @publisherLabel.
  ///
  /// In vi, this message translates to:
  /// **'Nhà xuất bản'**
  String get publisherLabel;

  /// No description provided for @publishYearLabel.
  ///
  /// In vi, this message translates to:
  /// **'Năm xuất bản'**
  String get publishYearLabel;

  /// No description provided for @editionLabel.
  ///
  /// In vi, this message translates to:
  /// **'Lần xuất bản'**
  String get editionLabel;

  /// No description provided for @documentTypeLabel.
  ///
  /// In vi, this message translates to:
  /// **'Dạng tài liệu'**
  String get documentTypeLabel;

  /// No description provided for @callNumberLabel.
  ///
  /// In vi, this message translates to:
  /// **'Ký hiệu'**
  String get callNumberLabel;

  /// No description provided for @ddcLabel.
  ///
  /// In vi, this message translates to:
  /// **'Phân loại DDC'**
  String get ddcLabel;

  /// No description provided for @seriesLabel.
  ///
  /// In vi, this message translates to:
  /// **'Tùng thư'**
  String get seriesLabel;

  /// No description provided for @requiresRequest.
  ///
  /// In vi, this message translates to:
  /// **'Cần xin phép'**
  String get requiresRequest;

  /// No description provided for @controlNumberLabel.
  ///
  /// In vi, this message translates to:
  /// **'Số kiểm soát'**
  String get controlNumberLabel;

  /// No description provided for @averageRating.
  ///
  /// In vi, this message translates to:
  /// **'Trung bình {rating} / 5'**
  String averageRating(String rating);

  /// No description provided for @citationStyle.
  ///
  /// In vi, this message translates to:
  /// **'Chuẩn trích dẫn'**
  String get citationStyle;

  /// No description provided for @cannotShare.
  ///
  /// In vi, this message translates to:
  /// **'Không chia sẻ được trên máy này.'**
  String get cannotShare;

  /// No description provided for @scanFromHome.
  ///
  /// In vi, this message translates to:
  /// **'Quét mã sách'**
  String get scanFromHome;

  /// No description provided for @searchFromHome.
  ///
  /// In vi, this message translates to:
  /// **'Tìm nhan đề, tác giả, ISBN…'**
  String get searchFromHome;

  /// No description provided for @itemsInStock.
  ///
  /// In vi, this message translates to:
  /// **'{count} bản in'**
  String itemsInStock(int count);

  /// No description provided for @browseTitle.
  ///
  /// In vi, this message translates to:
  /// **'Duyệt danh mục'**
  String get browseTitle;

  /// No description provided for @browseSubjects.
  ///
  /// In vi, this message translates to:
  /// **'Chủ đề'**
  String get browseSubjects;

  /// No description provided for @browseClassifications.
  ///
  /// In vi, this message translates to:
  /// **'Phân loại DDC'**
  String get browseClassifications;

  /// No description provided for @browseAuthors.
  ///
  /// In vi, this message translates to:
  /// **'Tác giả'**
  String get browseAuthors;

  /// No description provided for @browseCollections.
  ///
  /// In vi, this message translates to:
  /// **'Bộ sưu tập'**
  String get browseCollections;

  /// No description provided for @browseMajors.
  ///
  /// In vi, this message translates to:
  /// **'Ngành đào tạo'**
  String get browseMajors;

  /// No description provided for @browseCourses.
  ///
  /// In vi, this message translates to:
  /// **'Môn học'**
  String get browseCourses;

  /// No description provided for @browseTheses.
  ///
  /// In vi, this message translates to:
  /// **'Luận văn / Luận án'**
  String get browseTheses;

  /// No description provided for @browseSerials.
  ///
  /// In vi, this message translates to:
  /// **'Ấn phẩm định kỳ'**
  String get browseSerials;

  /// No description provided for @browseFilterHint.
  ///
  /// In vi, this message translates to:
  /// **'Lọc trong danh sách…'**
  String get browseFilterHint;

  /// No description provided for @browseEmpty.
  ///
  /// In vi, this message translates to:
  /// **'Không có mục nào.'**
  String get browseEmpty;

  /// No description provided for @bibCountLabel.
  ///
  /// In vi, this message translates to:
  /// **'{count} tài liệu'**
  String bibCountLabel(int count);

  /// No description provided for @filteringBy.
  ///
  /// In vi, this message translates to:
  /// **'Đang lọc: {label}'**
  String filteringBy(String label);

  /// No description provided for @newBooks.
  ///
  /// In vi, this message translates to:
  /// **'Sách mới bổ sung'**
  String get newBooks;

  /// No description provided for @popularBooks.
  ///
  /// In vi, this message translates to:
  /// **'Được mượn nhiều'**
  String get popularBooks;

  /// No description provided for @latestNews.
  ///
  /// In vi, this message translates to:
  /// **'Tin tức'**
  String get latestNews;

  /// No description provided for @viewAll.
  ///
  /// In vi, this message translates to:
  /// **'Xem tất cả'**
  String get viewAll;

  /// No description provided for @quickLinks.
  ///
  /// In vi, this message translates to:
  /// **'Liên kết hữu ích'**
  String get quickLinks;

  /// No description provided for @statistics.
  ///
  /// In vi, this message translates to:
  /// **'Kho tài liệu'**
  String get statistics;

  /// No description provided for @statBibs.
  ///
  /// In vi, this message translates to:
  /// **'biểu ghi'**
  String get statBibs;

  /// No description provided for @statItems.
  ///
  /// In vi, this message translates to:
  /// **'bản in'**
  String get statItems;

  /// No description provided for @statDigital.
  ///
  /// In vi, this message translates to:
  /// **'tài liệu số'**
  String get statDigital;

  /// No description provided for @statReaders.
  ///
  /// In vi, this message translates to:
  /// **'bạn đọc'**
  String get statReaders;

  /// No description provided for @callAction.
  ///
  /// In vi, this message translates to:
  /// **'Gọi'**
  String get callAction;

  /// No description provided for @directionsAction.
  ///
  /// In vi, this message translates to:
  /// **'Chỉ đường'**
  String get directionsAction;

  /// No description provided for @newsTitle.
  ///
  /// In vi, this message translates to:
  /// **'Tin tức'**
  String get newsTitle;

  /// No description provided for @newsEmpty.
  ///
  /// In vi, this message translates to:
  /// **'Chưa có tin nào.'**
  String get newsEmpty;

  /// No description provided for @pagesTitle.
  ///
  /// In vi, this message translates to:
  /// **'Thông tin thư viện'**
  String get pagesTitle;

  /// No description provided for @allCategories.
  ///
  /// In vi, this message translates to:
  /// **'Tất cả'**
  String get allCategories;

  /// No description provided for @relatedNews.
  ///
  /// In vi, this message translates to:
  /// **'Tin liên quan'**
  String get relatedNews;

  /// No description provided for @viewCount.
  ///
  /// In vi, this message translates to:
  /// **'{count} lượt xem'**
  String viewCount(int count);

  /// No description provided for @receivedIssues.
  ///
  /// In vi, this message translates to:
  /// **'{count} số đã nhận'**
  String receivedIssues(int count);

  /// No description provided for @latestIssue.
  ///
  /// In vi, this message translates to:
  /// **'Số mới nhất: {no} ({date})'**
  String latestIssue(String no, String date);

  /// No description provided for @courseDocsTitle.
  ///
  /// In vi, this message translates to:
  /// **'Tài liệu môn học'**
  String get courseDocsTitle;

  /// No description provided for @thesesHint.
  ///
  /// In vi, this message translates to:
  /// **'Tìm luận văn, luận án…'**
  String get thesesHint;

  /// No description provided for @serialsHint.
  ///
  /// In vi, this message translates to:
  /// **'Tìm báo, tạp chí…'**
  String get serialsHint;

  /// No description provided for @topAuthors.
  ///
  /// In vi, this message translates to:
  /// **'Tác giả có nhiều tài liệu'**
  String get topAuthors;

  /// No description provided for @letterAll.
  ///
  /// In vi, this message translates to:
  /// **'Nổi bật'**
  String get letterAll;

  /// No description provided for @browseShortcuts.
  ///
  /// In vi, this message translates to:
  /// **'Duyệt theo'**
  String get browseShortcuts;

  /// No description provided for @featured.
  ///
  /// In vi, this message translates to:
  /// **'Nổi bật'**
  String get featured;

  /// No description provided for @cardTitle.
  ///
  /// In vi, this message translates to:
  /// **'Thẻ thư viện'**
  String get cardTitle;

  /// No description provided for @cardNumberLabel.
  ///
  /// In vi, this message translates to:
  /// **'Số thẻ'**
  String get cardNumberLabel;

  /// No description provided for @readerType.
  ///
  /// In vi, this message translates to:
  /// **'Loại bạn đọc'**
  String get readerType;

  /// No description provided for @faculty.
  ///
  /// In vi, this message translates to:
  /// **'Khoa'**
  String get faculty;

  /// No description provided for @classLabel.
  ///
  /// In vi, this message translates to:
  /// **'Lớp'**
  String get classLabel;

  /// No description provided for @cardExpiry.
  ///
  /// In vi, this message translates to:
  /// **'Hạn thẻ'**
  String get cardExpiry;

  /// No description provided for @cardStatus.
  ///
  /// In vi, this message translates to:
  /// **'Trạng thái'**
  String get cardStatus;

  /// No description provided for @cardActive.
  ///
  /// In vi, this message translates to:
  /// **'Đang hoạt động'**
  String get cardActive;

  /// No description provided for @cardInactiveNote.
  ///
  /// In vi, this message translates to:
  /// **'Thẻ không còn hiệu lực nên không hiện mã. Liên hệ thư viện để gia hạn hoặc mở khóa.'**
  String get cardInactiveNote;

  /// No description provided for @cardOfflineNote.
  ///
  /// In vi, this message translates to:
  /// **'Không có mạng — đang hiện bản lưu trên máy, cập nhật lúc {time}.'**
  String cardOfflineNote(String time);

  /// No description provided for @cardRenewRequest.
  ///
  /// In vi, this message translates to:
  /// **'Gửi yêu cầu gia hạn thẻ'**
  String get cardRenewRequest;

  /// No description provided for @cardRenewReason.
  ///
  /// In vi, this message translates to:
  /// **'Lý do (không bắt buộc)'**
  String get cardRenewReason;

  /// No description provided for @cardRenewSent.
  ///
  /// In vi, this message translates to:
  /// **'Đã gửi yêu cầu gia hạn thẻ. Thư viện sẽ xử lý và báo lại.'**
  String get cardRenewSent;

  /// No description provided for @cardRenewals.
  ///
  /// In vi, this message translates to:
  /// **'Yêu cầu đã gửi'**
  String get cardRenewals;

  /// No description provided for @cardShowAtDesk.
  ///
  /// In vi, this message translates to:
  /// **'Đưa mã này cho quầy thủ thư hoặc cổng ra vào.'**
  String get cardShowAtDesk;

  /// No description provided for @warningsLabel.
  ///
  /// In vi, this message translates to:
  /// **'Lưu ý'**
  String get warningsLabel;

  /// No description provided for @loanCountLabel.
  ///
  /// In vi, this message translates to:
  /// **'Đang mượn {count}'**
  String loanCountLabel(int count);

  /// No description provided for @finesOwed.
  ///
  /// In vi, this message translates to:
  /// **'Nợ phạt {amount}'**
  String finesOwed(String amount);

  /// No description provided for @myLibraryTitle.
  ///
  /// In vi, this message translates to:
  /// **'Sách của tôi'**
  String get myLibraryTitle;

  /// No description provided for @currentLoans.
  ///
  /// In vi, this message translates to:
  /// **'Đang mượn'**
  String get currentLoans;

  /// No description provided for @loanHistory.
  ///
  /// In vi, this message translates to:
  /// **'Lịch sử'**
  String get loanHistory;

  /// No description provided for @holdsTab.
  ///
  /// In vi, this message translates to:
  /// **'Đặt giữ'**
  String get holdsTab;

  /// No description provided for @finesTab.
  ///
  /// In vi, this message translates to:
  /// **'Tiền phạt'**
  String get finesTab;

  /// No description provided for @dueIn.
  ///
  /// In vi, this message translates to:
  /// **'Còn {days} ngày'**
  String dueIn(int days);

  /// No description provided for @dueToday.
  ///
  /// In vi, this message translates to:
  /// **'Hạn trả hôm nay'**
  String get dueToday;

  /// No description provided for @overdueBy.
  ///
  /// In vi, this message translates to:
  /// **'Quá hạn {days} ngày'**
  String overdueBy(int days);

  /// No description provided for @dueOn.
  ///
  /// In vi, this message translates to:
  /// **'Hạn trả {date}'**
  String dueOn(String date);

  /// No description provided for @renewAction.
  ///
  /// In vi, this message translates to:
  /// **'Gia hạn'**
  String get renewAction;

  /// No description provided for @renewedTo.
  ///
  /// In vi, this message translates to:
  /// **'Đã gia hạn, hạn trả mới {date}.'**
  String renewedTo(String date);

  /// No description provided for @renewCount.
  ///
  /// In vi, this message translates to:
  /// **'Đã gia hạn {count}/{max}'**
  String renewCount(int count, int max);

  /// No description provided for @noLoans.
  ///
  /// In vi, this message translates to:
  /// **'Bạn không mượn cuốn nào.'**
  String get noLoans;

  /// No description provided for @noHistory.
  ///
  /// In vi, this message translates to:
  /// **'Chưa có lịch sử mượn.'**
  String get noHistory;

  /// No description provided for @noHolds.
  ///
  /// In vi, this message translates to:
  /// **'Không có đặt giữ nào.'**
  String get noHolds;

  /// No description provided for @noFines.
  ///
  /// In vi, this message translates to:
  /// **'Không có khoản phạt nào.'**
  String get noFines;

  /// No description provided for @cancelHold.
  ///
  /// In vi, this message translates to:
  /// **'Hủy đặt giữ'**
  String get cancelHold;

  /// No description provided for @cancelHoldConfirm.
  ///
  /// In vi, this message translates to:
  /// **'Hủy đặt giữ \"{title}\"?'**
  String cancelHoldConfirm(String title);

  /// No description provided for @holdCancelled.
  ///
  /// In vi, this message translates to:
  /// **'Đã hủy đặt giữ.'**
  String get holdCancelled;

  /// No description provided for @queuePosition.
  ///
  /// In vi, this message translates to:
  /// **'Thứ {position} trong hàng đợi'**
  String queuePosition(int position);

  /// No description provided for @holdReady.
  ///
  /// In vi, this message translates to:
  /// **'Sẵn sàng nhận'**
  String get holdReady;

  /// No description provided for @holdWaiting.
  ///
  /// In vi, this message translates to:
  /// **'Đang chờ'**
  String get holdWaiting;

  /// No description provided for @holdFulfilled.
  ///
  /// In vi, this message translates to:
  /// **'Đã nhận'**
  String get holdFulfilled;

  /// No description provided for @holdExpired.
  ///
  /// In vi, this message translates to:
  /// **'Hết hạn'**
  String get holdExpired;

  /// No description provided for @holdCancelledStatus.
  ///
  /// In vi, this message translates to:
  /// **'Đã hủy'**
  String get holdCancelledStatus;

  /// No description provided for @pickupAt.
  ///
  /// In vi, this message translates to:
  /// **'Nhận tại {place}'**
  String pickupAt(String place);

  /// No description provided for @holdExpires.
  ///
  /// In vi, this message translates to:
  /// **'Giữ đến {date}'**
  String holdExpires(String date);

  /// No description provided for @totalOutstanding.
  ///
  /// In vi, this message translates to:
  /// **'Còn phải nộp'**
  String get totalOutstanding;

  /// No description provided for @totalPaid.
  ///
  /// In vi, this message translates to:
  /// **'Đã nộp'**
  String get totalPaid;

  /// No description provided for @finePaymentGuide.
  ///
  /// In vi, this message translates to:
  /// **'Thanh toán tại quầy thủ thư. Ứng dụng không thu tiền.'**
  String get finePaymentGuide;

  /// No description provided for @fineTypeOverdue.
  ///
  /// In vi, this message translates to:
  /// **'Quá hạn'**
  String get fineTypeOverdue;

  /// No description provided for @fineTypeLost.
  ///
  /// In vi, this message translates to:
  /// **'Mất'**
  String get fineTypeLost;

  /// No description provided for @fineTypeDamaged.
  ///
  /// In vi, this message translates to:
  /// **'Hỏng'**
  String get fineTypeDamaged;

  /// No description provided for @fineTypeOther.
  ///
  /// In vi, this message translates to:
  /// **'Khác'**
  String get fineTypeOther;

  /// No description provided for @filterAll.
  ///
  /// In vi, this message translates to:
  /// **'Tất cả'**
  String get filterAll;

  /// No description provided for @filter30Days.
  ///
  /// In vi, this message translates to:
  /// **'30 ngày qua'**
  String get filter30Days;

  /// No description provided for @filterThisYear.
  ///
  /// In vi, this message translates to:
  /// **'Năm nay'**
  String get filterThisYear;

  /// No description provided for @historySearchHint.
  ///
  /// In vi, this message translates to:
  /// **'Tìm theo nhan đề, mã vạch…'**
  String get historySearchHint;

  /// No description provided for @returnedOn.
  ///
  /// In vi, this message translates to:
  /// **'Trả ngày {date}'**
  String returnedOn(String date);

  /// No description provided for @borrowedOn.
  ///
  /// In vi, this message translates to:
  /// **'Mượn ngày {date}'**
  String borrowedOn(String date);

  /// No description provided for @estimatedFine.
  ///
  /// In vi, this message translates to:
  /// **'Phạt dự kiến {amount}'**
  String estimatedFine(String amount);

  /// No description provided for @cancelAction.
  ///
  /// In vi, this message translates to:
  /// **'Hủy'**
  String get cancelAction;

  /// No description provided for @confirmAction.
  ///
  /// In vi, this message translates to:
  /// **'Đồng ý'**
  String get confirmAction;
}

class _L10nDelegate extends LocalizationsDelegate<L10n> {
  const _L10nDelegate();

  @override
  Future<L10n> load(Locale locale) {
    return SynchronousFuture<L10n>(lookupL10n(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'vi'].contains(locale.languageCode);

  @override
  bool shouldReload(_L10nDelegate old) => false;
}

L10n lookupL10n(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return L10nEn();
    case 'vi':
      return L10nVi();
  }

  throw FlutterError(
    'L10n.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
