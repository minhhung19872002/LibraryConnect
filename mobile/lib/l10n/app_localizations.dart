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
