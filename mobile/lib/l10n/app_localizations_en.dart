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

  @override
  String get searchTitle => 'Search';

  @override
  String get scopeAll => 'All';

  @override
  String get scopeTitle => 'Title';

  @override
  String get scopeAuthor => 'Author';

  @override
  String get scopeSubject => 'Subject';

  @override
  String get scopeKeyword => 'Keyword';

  @override
  String get scopeIsbn => 'ISBN';

  @override
  String get scopePublisher => 'Publisher';

  @override
  String get scopeCallNumber => 'Call number';

  @override
  String get recentSearches => 'Recent searches';

  @override
  String get clearAll => 'Clear all';

  @override
  String get suggestions => 'Suggestions';

  @override
  String resultCount(int count) {
    return '$count results';
  }

  @override
  String resultCountCapped(int count) {
    return 'More than $count results';
  }

  @override
  String get noResults => 'No documents found.';

  @override
  String get noResultsHint => 'Try a shorter keyword or fewer filters.';

  @override
  String get sortLabel => 'Sort';

  @override
  String get sortRelevance => 'Relevance';

  @override
  String get sortNewest => 'Newest';

  @override
  String get sortTitle => 'Title';

  @override
  String get sortAuthor => 'Author';

  @override
  String get sortPopular => 'Most borrowed';

  @override
  String get filters => 'Filters';

  @override
  String get applyFilters => 'Apply';

  @override
  String get clearFilters => 'Clear filters';

  @override
  String get advancedSearch => 'Advanced search';

  @override
  String get addClause => 'Add condition';

  @override
  String get connectorAnd => 'AND';

  @override
  String get connectorOr => 'OR';

  @override
  String get connectorNot => 'NOT';

  @override
  String get yearFrom => 'From year';

  @override
  String get yearTo => 'To year';

  @override
  String get onlyDigital => 'Digital only';

  @override
  String get onlyAvailable => 'Available only';

  @override
  String get searchAction => 'Search';

  @override
  String get loadMoreError => 'Could not load more. Tap to retry.';

  @override
  String availableCopies(int count) {
    return '$count available';
  }

  @override
  String get allOnLoan => 'All copies on loan';

  @override
  String get noCopies => 'No physical copies';

  @override
  String digitalCount(int count) {
    return '$count digital files';
  }

  @override
  String get scanTitle => 'Scan';

  @override
  String get scanHint => 'Point at an ISBN, item barcode or QR code.';

  @override
  String get scanTorch => 'Torch';

  @override
  String get scanSwitchCamera => 'Switch camera';

  @override
  String scanNotFound(String code) {
    return 'Nothing found for code $code.';
  }

  @override
  String get scanManualSearch => 'Search manually';

  @override
  String get scanEnterCode => 'Enter code by hand';

  @override
  String scanLookingUp(String code) {
    return 'Looking up $code…';
  }

  @override
  String scanFoundCopy(String barcode, String status) {
    return 'Copy $barcode · $status';
  }

  @override
  String scanIsbnMany(int count, String isbn) {
    return '$count documents share ISBN $isbn';
  }

  @override
  String get scanCameraDenied =>
      'Camera permission not granted. You can still type the code.';

  @override
  String get scanOpen => 'Open details';

  @override
  String get scanStationCode =>
      'This is a self-checkout station code; use it on the Self-checkout screen.';

  @override
  String get detailTabInfo => 'Details';

  @override
  String get detailTabItems => 'Copies';

  @override
  String get detailTabDigital => 'Digital';

  @override
  String get detailTabMarc => 'MARC';

  @override
  String get detailTabReviews => 'Reviews';

  @override
  String get holdAction => 'Place hold';

  @override
  String get queueAction => 'Join queue';

  @override
  String get holdPlaced =>
      'Hold placed. The library will notify you when it is ready.';

  @override
  String holdQueued(int position) {
    return 'Queued: you are number $position.';
  }

  @override
  String get citeAction => 'Cite';

  @override
  String get shareAction => 'Share';

  @override
  String get favoriteAction => 'Favourite';

  @override
  String get favoriteAdded => 'Added to favourites.';

  @override
  String get favoriteRemoved => 'Removed from favourites.';

  @override
  String get copyAction => 'Copy';

  @override
  String get copied => 'Copied.';

  @override
  String get abstractLabel => 'Abstract';

  @override
  String get subjectsLabel => 'Subjects';

  @override
  String get keywordsLabel => 'Keywords';

  @override
  String get isbdLabel => 'Bibliographic description';

  @override
  String get relatedLabel => 'Related documents';

  @override
  String get externalLinks => 'Full text elsewhere';

  @override
  String get noItems => 'This document has no physical copies.';

  @override
  String get noDigital => 'No digital files.';

  @override
  String get noReviews => 'No reviews yet.';

  @override
  String get marcUnreadable => 'The MARC record could not be read.';

  @override
  String get leaderLabel => 'Leader';

  @override
  String dueBack(String date) {
    return 'Due $date';
  }

  @override
  String get writeReview => 'Write a review';

  @override
  String get sendReview => 'Send';

  @override
  String get reviewSent => 'Review sent, pending library approval.';

  @override
  String get reviewHint => 'Your thoughts on this document';

  @override
  String get signInToContinue => 'Sign in to continue';

  @override
  String get pagesLabel => 'Pages';

  @override
  String get publisherLabel => 'Publisher';

  @override
  String get publishYearLabel => 'Year';

  @override
  String get editionLabel => 'Edition';

  @override
  String get documentTypeLabel => 'Document type';

  @override
  String get callNumberLabel => 'Call number';

  @override
  String get ddcLabel => 'DDC';

  @override
  String get seriesLabel => 'Series';

  @override
  String get requiresRequest => 'Request required';

  @override
  String get controlNumberLabel => 'Control number';

  @override
  String averageRating(String rating) {
    return 'Average $rating / 5';
  }

  @override
  String get citationStyle => 'Citation style';

  @override
  String get cannotShare => 'Sharing is not available on this device.';

  @override
  String get scanFromHome => 'Scan a book';

  @override
  String get searchFromHome => 'Title, author, ISBN…';

  @override
  String itemsInStock(int count) {
    return '$count copies';
  }

  @override
  String get browseTitle => 'Browse';

  @override
  String get browseSubjects => 'Subjects';

  @override
  String get browseClassifications => 'DDC classes';

  @override
  String get browseAuthors => 'Authors';

  @override
  String get browseCollections => 'Collections';

  @override
  String get browseMajors => 'Majors';

  @override
  String get browseCourses => 'Courses';

  @override
  String get browseTheses => 'Theses & dissertations';

  @override
  String get browseSerials => 'Serials';

  @override
  String get browseFilterHint => 'Filter this list…';

  @override
  String get browseEmpty => 'Nothing here.';

  @override
  String bibCountLabel(int count) {
    return '$count documents';
  }

  @override
  String filteringBy(String label) {
    return 'Filtering: $label';
  }

  @override
  String get newBooks => 'New arrivals';

  @override
  String get popularBooks => 'Most borrowed';

  @override
  String get latestNews => 'News';

  @override
  String get viewAll => 'View all';

  @override
  String get quickLinks => 'Useful links';

  @override
  String get statistics => 'Collection';

  @override
  String get statBibs => 'records';

  @override
  String get statItems => 'copies';

  @override
  String get statDigital => 'digital files';

  @override
  String get statReaders => 'readers';

  @override
  String get callAction => 'Call';

  @override
  String get directionsAction => 'Directions';

  @override
  String get newsTitle => 'News';

  @override
  String get newsEmpty => 'No news yet.';

  @override
  String get pagesTitle => 'About the library';

  @override
  String get allCategories => 'All';

  @override
  String get relatedNews => 'Related news';

  @override
  String viewCount(int count) {
    return '$count views';
  }

  @override
  String receivedIssues(int count) {
    return '$count issues received';
  }

  @override
  String latestIssue(String no, String date) {
    return 'Latest issue: $no ($date)';
  }

  @override
  String get courseDocsTitle => 'Course materials';

  @override
  String get thesesHint => 'Search theses…';

  @override
  String get serialsHint => 'Search serials…';

  @override
  String get topAuthors => 'Authors with most documents';

  @override
  String get letterAll => 'Top';

  @override
  String get browseShortcuts => 'Browse by';

  @override
  String get featured => 'Featured';
}
