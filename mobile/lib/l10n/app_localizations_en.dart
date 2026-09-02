// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class L10nEn extends L10n {
  L10nEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'LibraryConnect';

  @override
  String get tabHome => 'Home';

  @override
  String get tabSearch => 'Search';

  @override
  String get tabScan => 'Scan';

  @override
  String get tabMyLibrary => 'My books';

  @override
  String get tabAccount => 'Account';

  @override
  String get loginTitle => 'Reader sign in';

  @override
  String get loginSubtitle =>
      'Use your library card number and the password you were given.';

  @override
  String get cardNumber => 'Library card number';

  @override
  String get password => 'Password';

  @override
  String get rememberCard => 'Remember card number';

  @override
  String get signIn => 'Sign in';

  @override
  String get signOut => 'Sign out';

  @override
  String get forgotPassword => 'Forgot password?';

  @override
  String get forgotPasswordHelp =>
      'The app cannot reset passwords. Please bring your card to the desk or contact the library.';

  @override
  String get cardNumberRequired => 'Enter your library card number.';

  @override
  String get passwordRequired => 'Enter your password.';

  @override
  String get continueAsGuest => 'Search without signing in';

  @override
  String get searchHint => 'Title, author, keyword…';

  @override
  String get searchHintNoAccent => 'Typing without diacritics works too.';

  @override
  String get retry => 'Retry';

  @override
  String get offlineTitle => 'No connection';

  @override
  String get offlineBody =>
      'Showing saved data. Connect to the network to refresh.';

  @override
  String get loading => 'Loading…';

  @override
  String welcome(String name) {
    return 'Hello, $name';
  }

  @override
  String get libraryInfo => 'Library information';

  @override
  String get openingHours => 'Opening hours';

  @override
  String get call => 'Call';

  @override
  String get directions => 'Directions';

  @override
  String get settings => 'Settings';

  @override
  String get theme => 'Theme';

  @override
  String get themeSystem => 'System';

  @override
  String get themeLight => 'Light';

  @override
  String get themeDark => 'Dark';

  @override
  String get language => 'Language';

  @override
  String get version => 'Version';

  @override
  String get sessionExpired =>
      'Your session has expired. Please sign in again.';

  @override
  String get updateRequiredTitle => 'Update required';

  @override
  String updateRequiredBody(String current, String min) {
    return 'This version ($current) is out of date; the library requires $min or newer.';
  }

  @override
  String get update => 'Update';

  @override
  String get mustChangePassword =>
      'You are using a temporary password; please change it before continuing.';

  @override
  String get poweredBy => 'Powered by LibraryConnect';
}
