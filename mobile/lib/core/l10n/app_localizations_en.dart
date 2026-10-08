// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppL10nEn extends AppL10n {
  AppL10nEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'RentoX';

  @override
  String get tagline => 'Everything for rent';

  @override
  String get loginTitle => 'Welcome';

  @override
  String get loginSubtitle =>
      'Enter your phone number to continue. We\'ll send you a verification code.';

  @override
  String get phoneLabel => 'Phone number';

  @override
  String get phoneHint => '50 123 45 67';

  @override
  String get phoneInvalid => 'The number must contain 9 digits';

  @override
  String get continueAction => 'Continue';

  @override
  String get otpTitle => 'Enter the code';

  @override
  String otpSubtitle(String phone) {
    return 'Type the 6-digit code sent to $phone.';
  }

  @override
  String otpResendIn(int seconds) {
    return 'Resend in ${seconds}s';
  }

  @override
  String get otpResend => 'Resend code';

  @override
  String get otpWrong => 'The code is incorrect or has expired';

  @override
  String get registerTitle => 'Create account';

  @override
  String get registerSubtitle =>
      'No account found for this number. Enter your name to finish signing up.';

  @override
  String get fullNameLabel => 'Full name';

  @override
  String get fullNameHint => 'Full name';

  @override
  String get fullNameRequired => 'Enter your name';

  @override
  String get registerAction => 'Complete sign up';

  @override
  String get errorNetwork => 'No internet connection. Please try again.';

  @override
  String get errorTooManyRequests =>
      'Too many attempts. Wait a moment and try again.';

  @override
  String get errorGeneric => 'Something went wrong. Try again in a moment.';

  @override
  String get retry => 'Try again';

  @override
  String get navHome => 'Home';

  @override
  String get navFavorites => 'Favorites';

  @override
  String get favoritesTitle => 'Favorites';

  @override
  String favoritesCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count listings',
      one: '1 listing',
    );
    return '$_temp0';
  }

  @override
  String get favoritesEmpty => 'No favorites yet';

  @override
  String get favoritesEmptyHint =>
      'Tap the heart on a listing to keep it here.';

  @override
  String get browseListings => 'Browse listings';

  @override
  String get favoriteAdd => 'Add to favorites';

  @override
  String get favoriteRemove => 'Remove from favorites';

  @override
  String get favoriteFailed => 'Couldn\'t save the change. Try again.';

  @override
  String get navSearch => 'Search';

  @override
  String get navCreate => 'Post';

  @override
  String get navMessages => 'Messages';

  @override
  String get navProfile => 'Profile';

  @override
  String get searchHint => 'What do you want to rent?';

  @override
  String get categories => 'Categories';

  @override
  String get seeAll => 'See all';

  @override
  String get vipListings => 'VIP listings';

  @override
  String get forYou => 'For you';

  @override
  String get emptyListings => 'No listings yet';

  @override
  String get emptyListingsHint => 'New listings will appear here soon.';

  @override
  String get comingSoon => 'Coming soon';

  @override
  String get comingSoonHint => 'We\'re working on this section.';

  @override
  String get vip => 'VIP';

  @override
  String get signIn => 'Sign in';

  @override
  String get notifications => 'Notifications';

  @override
  String get filters => 'Filters';

  @override
  String get trustTitle => 'Verified users';

  @override
  String get trustSubtitle => 'Safe rentals, trusted people';

  @override
  String get listingNotFound => 'Listing not found';

  @override
  String get listingNotFoundHint => 'It may have been removed or has expired.';

  @override
  String get descriptionTitle => 'Description';

  @override
  String get specsTitle => 'Details';

  @override
  String get ownerTitle => 'Posted by';

  @override
  String get callAction => 'Call';

  @override
  String get messageAction => 'Message';

  @override
  String get yourListing => 'This is your listing';

  @override
  String viewsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count views',
      one: '1 view',
    );
    return '$_temp0';
  }

  @override
  String get publishedToday => 'Today';

  @override
  String get publishedYesterday => 'Yesterday';

  @override
  String publishedDaysAgo(int days) {
    String _temp0 = intl.Intl.pluralLogic(
      days,
      locale: localeName,
      other: '$days days ago',
      one: '1 day ago',
    );
    return '$_temp0';
  }

  @override
  String get yes => 'Yes';

  @override
  String get no => 'No';

  @override
  String get messageSheetTitle => 'Message the owner';

  @override
  String get messageHint => 'Write your message';

  @override
  String get messageDefault => 'Hi, is this still available?';

  @override
  String get sendAction => 'Send';

  @override
  String get messageSent => 'Message sent';

  @override
  String get messageFailed => 'Couldn\'t send the message. Try again.';

  @override
  String get callFailed => 'Couldn\'t start the call';

  @override
  String photoCounter(int current, int total) {
    return '$current / $total';
  }

  @override
  String get searchRecent => 'Recent searches';

  @override
  String get clearAll => 'Clear all';

  @override
  String get allCategories => 'All';

  @override
  String resultsFound(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count listings found',
      one: '1 listing found',
      zero: 'No listings found',
    );
    return '$_temp0';
  }

  @override
  String get noResults => 'Nothing found';

  @override
  String get noResultsHint => 'Try another word or change the filters.';

  @override
  String get clearFilters => 'Clear filters';

  @override
  String get clearSearch => 'Clear';

  @override
  String get priceFilter => 'Price';

  @override
  String get priceMin => 'Min';

  @override
  String get priceMax => 'Max';

  @override
  String get applyAction => 'Apply';

  @override
  String get resetAction => 'Reset';

  @override
  String get priceRangeInvalid => 'Min price can\'t be higher than max price';

  @override
  String get backAction => 'Back';

  @override
  String get createTitle => 'New listing';

  @override
  String stepOf(int current, int total) {
    return 'Step $current of $total';
  }

  @override
  String get stepPhotos => 'Photos';

  @override
  String get stepDetails => 'Details';

  @override
  String get stepSpecs => 'Features and price';

  @override
  String get stepReview => 'Review and send';

  @override
  String photosHint(int max) {
    return 'The first photo is the cover. Add at least 1 and up to $max photos.';
  }

  @override
  String get addPhotos => 'Add photos';

  @override
  String get takePhoto => 'Take a photo';

  @override
  String photosCount(int count, int max) {
    return '$count / $max';
  }

  @override
  String get coverLabel => 'Cover';

  @override
  String get makeCover => 'Make cover';

  @override
  String get removePhoto => 'Remove';

  @override
  String get photosRequired => 'Add at least one photo';

  @override
  String photoUnsupported(int count) {
    return 'Unsupported files: $count (JPG, PNG, WebP only)';
  }

  @override
  String photoTooLarge(int count) {
    return 'Photos over 10 MB: $count';
  }

  @override
  String photoOverLimit(int max) {
    return 'You can add up to $max photos';
  }

  @override
  String get categoryLabel => 'Category';

  @override
  String get chooseCategory => 'Choose a category';

  @override
  String get categoryRequired => 'Category is required';

  @override
  String get titleLabel => 'Listing title';

  @override
  String get titleHint => 'For example, Toyota Camry 2023';

  @override
  String get titleRequired => 'Enter a title';

  @override
  String get descriptionLabel => 'Description';

  @override
  String get descriptionHint => 'Describe your listing in detail';

  @override
  String get descriptionRequired => 'Enter a description';

  @override
  String get priceLabel => 'Price';

  @override
  String get priceAmount => 'Amount (AZN)';

  @override
  String get pricePer => 'Rental period';

  @override
  String get priceRequired => 'Enter a price';

  @override
  String get priceInvalid => 'Enter a valid amount';

  @override
  String get fieldRequired => 'This field is required';

  @override
  String get fieldInvalidNumber => 'Enter a valid number';

  @override
  String get fieldNotWhole => 'Enter a whole number';

  @override
  String get fieldOther => 'Other';

  @override
  String get fieldCustomHint => 'Type your own';

  @override
  String get fieldPickDate => 'Pick a date';

  @override
  String get specsEmpty => 'This category needs no extra details.';

  @override
  String get reviewTitle => 'Review your listing';

  @override
  String get reviewNote =>
      'After you send it, a moderator reviews the listing and it goes live once approved. If your free limit is used up, activating it may require a payment.';

  @override
  String get nextAction => 'Continue';

  @override
  String get sendListing => 'Send listing';

  @override
  String get creatingListing => 'Creating your listing…';

  @override
  String uploadingPhotos(int current, int total) {
    return 'Uploading photos ($current of $total)…';
  }

  @override
  String get submittingForReview => 'Sending for review…';

  @override
  String get submitFailed => 'The listing was not sent';

  @override
  String get draftSavedHint =>
      'Your listing is saved as a draft. Trying again continues where it stopped.';

  @override
  String get discardDraft => 'Discard draft';

  @override
  String get leaveTitle => 'Leave this listing?';

  @override
  String get leaveBody => 'What you entered will be lost.';

  @override
  String get leaveStay => 'Stay';

  @override
  String get leaveExit => 'Leave';

  @override
  String get doneTitle => 'Your listing was sent';

  @override
  String get doneBody =>
      'It is under review. You will be notified once it is approved.';

  @override
  String get viewMyListings => 'View my listings';

  @override
  String get createAnother => 'Create another';

  @override
  String get statusDraft => 'Draft';

  @override
  String get statusPending => 'In review';

  @override
  String get statusActive => 'Active';

  @override
  String get statusRejected => 'Rejected';

  @override
  String get statusExpired => 'Expired';

  @override
  String get statusDeactivated => 'Deactivated';

  @override
  String get statusDeleted => 'Deleted';

  @override
  String get statusPaymentRequired => 'Payment required';

  @override
  String get myListingsTitle => 'My listings';

  @override
  String get myListingsEmpty => 'No listings yet';

  @override
  String get myListingsEmptyHint =>
      'Create your first listing and start renting out.';

  @override
  String get rejectedReason => 'Rejection reason';

  @override
  String get submitForReview => 'Send for review';

  @override
  String get payActivate => 'Pay and activate';

  @override
  String get makeVip => 'Make VIP';

  @override
  String get deactivateAction => 'Deactivate';

  @override
  String get reactivateAction => 'Reactivate';

  @override
  String expiresOn(String date) {
    return 'Expires on $date';
  }

  @override
  String get actionDone => 'Saved';

  @override
  String get actionFailed => 'That did not work. Try again.';

  @override
  String get paymentTitle => 'Payment';

  @override
  String get payForActivation => 'Listing activation';

  @override
  String payForVip(int days) {
    return 'VIP listing ($days days)';
  }

  @override
  String get paymentAmount => 'Amount';

  @override
  String get walletBalance => 'Your balance';

  @override
  String get insufficientBalance => 'Not enough balance';

  @override
  String get topUpDemo => 'Top up (demo)';

  @override
  String get topUpDone => 'Balance topped up';

  @override
  String get demoNote => 'Demo mode: no real money is charged.';

  @override
  String payNow(String amount) {
    return 'Pay $amount';
  }

  @override
  String get paymentSuccessActivation =>
      'Payment received. Your listing is active.';

  @override
  String get paymentSuccessVip => 'Payment received. Your listing is now VIP.';

  @override
  String get paymentFailed => 'The payment failed. Try again.';

  @override
  String get walletTitle => 'Wallet';

  @override
  String get vipActive => 'VIP listing is active';

  @override
  String get unitHour => 'hour';

  @override
  String get unitDay => 'day';

  @override
  String get unitWeek => 'week';

  @override
  String get unitMonth => 'month';

  @override
  String get unitEvent => 'event';

  @override
  String get unitNegotiable => 'negotiable';

  @override
  String get signOut => 'Sign out';
}
