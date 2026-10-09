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
  String get messagesTitle => 'Messages';

  @override
  String get messagesAll => 'All';

  @override
  String get messagesUnreadFilter => 'Unread';

  @override
  String get messagesEmpty => 'No messages yet';

  @override
  String get messagesEmptyHint =>
      'Write to a listing owner and the chat will show up here.';

  @override
  String get messagesUnreadEmpty => 'No unread messages';

  @override
  String get chatUser => 'User';

  @override
  String get chatTyping => 'typing…';

  @override
  String get chatOnline => 'Online';

  @override
  String get chatOffline => 'Offline';

  @override
  String get chatInputHint => 'Write a message';

  @override
  String get chatEmpty => 'Start the conversation';

  @override
  String get chatPhoto => 'Photo';

  @override
  String get chatAttach => 'Attach photos';

  @override
  String get chatToday => 'Today';

  @override
  String get chatYesterday => 'Yesterday';

  @override
  String get chatViewListing => 'View listing';

  @override
  String get chatSendFailed => 'Not sent';

  @override
  String get chatRetry => 'Send again';

  @override
  String get chatDiscard => 'Delete';

  @override
  String get chatTooLong => 'Message is too long (2000 characters max)';

  @override
  String get chatBlock => 'Block user';

  @override
  String get chatUnblock => 'Unblock';

  @override
  String get chatBlockedByMe => 'You blocked this user';

  @override
  String get chatCannotSend => 'You can\'t send messages in this chat';

  @override
  String get chatBlockedDone => 'User blocked';

  @override
  String get chatUnblockedDone => 'User unblocked';

  @override
  String get chatReport => 'Report';

  @override
  String get chatReportTitle => 'Reason for the report';

  @override
  String get chatReportDetails => 'More details (optional)';

  @override
  String get chatReportSend => 'Submit';

  @override
  String get chatReportSent => 'Report sent';

  @override
  String get reportSpam => 'Spam';

  @override
  String get reportFraud => 'Fraud';

  @override
  String get reportHarassment => 'Harassment';

  @override
  String get reportProhibited => 'Prohibited content';

  @override
  String get reportOther => 'Other';

  @override
  String get chatReconnecting => 'Reconnecting…';

  @override
  String get notificationsMarkAll => 'Mark all read';

  @override
  String get notificationsEmpty => 'No notifications';

  @override
  String get notificationsEmptyHint =>
      'Updates about your listings and account will show up here.';

  @override
  String get notificationView => 'View';

  @override
  String get profileEditTitle => 'Edit profile';

  @override
  String get profileAddName => 'Add your name';

  @override
  String get profileName => 'Full name';

  @override
  String get profileNameHint => 'For example, Murad Aliyev';

  @override
  String get profileNameInvalid => 'The name must be 2 to 100 characters';

  @override
  String get profileBio => 'About you';

  @override
  String get profileBioHint => 'A few words about you (optional)';

  @override
  String get profilePhone => 'Phone number';

  @override
  String get profileSave => 'Save';

  @override
  String get profileSaved => 'Profile updated';

  @override
  String get profilePhotoAdd => 'Add a photo';

  @override
  String get profilePhotoChange => 'Change photo';

  @override
  String get profilePhotoTooLarge => 'The photo can\'t be larger than 5 MB';

  @override
  String get languageTitle => 'Language';

  @override
  String get cancelAction => 'Cancel';

  @override
  String get logoutAllTitle => 'Sign out everywhere';

  @override
  String get logoutAllMessage =>
      'You\'ll be signed out on every device and will need to sign in again.';

  @override
  String get logoutAllConfirm => 'Sign out';

  @override
  String get editListingTitle => 'Edit listing';

  @override
  String get editAction => 'Edit';

  @override
  String get editSave => 'Save changes';

  @override
  String get editSaved => 'Listing updated. You can send it for review now.';

  @override
  String get editNote =>
      'After you save, the listing has to be sent for review again.';

  @override
  String get editNotAllowed => 'This listing can\'t be edited right now';

  @override
  String get editNotAllowedHint =>
      'Only drafts and rejected listings can be edited.';

  @override
  String get editPhotosTitle => 'Photos';

  @override
  String get editPhotoRemoveTitle => 'Remove this photo?';

  @override
  String get editPhotoNeeded =>
      'You need at least one photo to send it for review';

  @override
  String get deleteListingAction => 'Delete listing';

  @override
  String get deleteListingTitle => 'Delete this listing?';

  @override
  String get deleteListingMessage =>
      'The listing will be deleted. This can\'t be undone.';

  @override
  String get deleteListingConfirm => 'Delete';

  @override
  String get listingDeleted => 'Listing deleted';

  @override
  String get renewAction => 'Renew listing';

  @override
  String get renewHint =>
      'This listing has expired. You can renew it for another 30 days.';

  @override
  String get payForRenewal => 'Listing renewal (30 days)';

  @override
  String get paymentSuccessRenew => 'Listing renewed and active again';

  @override
  String get bumpAction => 'Bump to top';

  @override
  String get payForBump => 'Bump the listing up the list';

  @override
  String get paymentSuccessBump => 'Listing bumped';

  @override
  String get storeMine => 'My store';

  @override
  String get storeFollowing => 'Stores I follow';

  @override
  String get storeNone => 'You don\'t have a store yet';

  @override
  String get storeNoneHint =>
      'Open a store: all your listings on one page, with followers.';

  @override
  String get storeOpen => 'Open a store';

  @override
  String get storeCreateTitle => 'Open a store';

  @override
  String get storeEditTitle => 'Edit store';

  @override
  String get storeSave => 'Save';

  @override
  String get storeCreated =>
      'Store created. Add a logo and send it for review.';

  @override
  String get storeSaved => 'Store updated';

  @override
  String get storeSubmitNeedsLogo =>
      'A logo is required to send the store for review';

  @override
  String get storeSubmitted => 'Store sent for review';

  @override
  String get storePendingInfo =>
      'Your store is being reviewed. It will be public once approved.';

  @override
  String get storeActiveInfo => 'Your store is live.';

  @override
  String get storeSuspendedInfo =>
      'Your store has been suspended. Contact support for details.';

  @override
  String get storeSuspended => 'Suspended';

  @override
  String get storeView => 'View store';

  @override
  String get storeEditOnlyDraft =>
      'Only a draft or rejected store can be edited.';

  @override
  String get storeName => 'Store name';

  @override
  String get storeDescription => 'About the store';

  @override
  String get storePhone => 'Contact phone';

  @override
  String get storeEmail => 'Email (optional)';

  @override
  String get storeAddress => 'Address (optional)';

  @override
  String get storeInstagram => 'Instagram link (optional)';

  @override
  String get storeTiktok => 'TikTok link (optional)';

  @override
  String get storeFacebook => 'Facebook link (optional)';

  @override
  String get storeWebsite => 'Website (optional)';

  @override
  String get storeErrShort => 'Too short';

  @override
  String get storeErrLong => 'Too long';

  @override
  String get storeErrEmail => 'Enter a valid email';

  @override
  String get storeErrUrl => 'Enter a valid link (https://...)';

  @override
  String get storeLogo => 'Logo';

  @override
  String get storeCover => 'Cover photo';

  @override
  String get storeImageTooLarge =>
      'The image is too large (logo up to 5 MB, cover up to 10 MB)';

  @override
  String get storeFollow => 'Follow';

  @override
  String get storeFollowingNow => 'Following';

  @override
  String get storeListingsTitle => 'Store listings';

  @override
  String get storeNoListings => 'This store has no active listings';

  @override
  String get storeNotFound => 'Store not found';

  @override
  String get storeContactTitle => 'Contact';

  @override
  String get storeFollowingEmpty => 'You don\'t follow any stores yet';

  @override
  String get storeFollowingEmptyHint =>
      'Follow stores you like and they\'ll show up here.';

  @override
  String storeFollowers(int count) {
    return '$count followers';
  }

  @override
  String storeListingsCount(int count) {
    return '$count listings';
  }

  @override
  String storeViews(int count) {
    return '$count views';
  }

  @override
  String get openLinkFailed => 'Couldn\'t open the link';

  @override
  String get supportTitle => 'Support';

  @override
  String get supportNew => 'New request';

  @override
  String get supportEmpty => 'No requests yet';

  @override
  String get supportEmptyHint =>
      'Write to us with any question or problem. Replies arrive as notifications.';

  @override
  String get supportNewTitle => 'Contact support';

  @override
  String get supportCategory => 'Topic';

  @override
  String get supportSubject => 'Short title';

  @override
  String get supportSubjectHint => 'For example, my payment didn\'t go through';

  @override
  String get supportMessage => 'Describe the problem';

  @override
  String get supportMessageHint => 'What happened? What did you expect?';

  @override
  String get supportSend => 'Send';

  @override
  String get supportSent => 'Your request was sent';

  @override
  String get supportErrSubject => 'Enter a title (160 characters max)';

  @override
  String get supportErrBody => 'Write a message (4000 characters max)';

  @override
  String get supportReplyHint => 'Write a reply';

  @override
  String get supportTeam => 'Support team';

  @override
  String get supportClosedInfo =>
      'This request is closed. For anything new, open a new request.';

  @override
  String get supportResolvedInfo =>
      'Marked as resolved. Replying will reopen it.';

  @override
  String get supportCatGeneral => 'General question';

  @override
  String get supportCatAccount => 'Account';

  @override
  String get supportCatListing => 'Listing';

  @override
  String get supportCatPayment => 'Payment';

  @override
  String get supportCatStore => 'Store';

  @override
  String get supportCatTechnical => 'Technical problem';

  @override
  String get supportCatOther => 'Other';

  @override
  String get supportStOpen => 'Open';

  @override
  String get supportStInProgress => 'In progress';

  @override
  String get supportStResolved => 'Resolved';

  @override
  String get supportStClosed => 'Closed';

  @override
  String get reportProblem => 'Report a problem';

  @override
  String get reportListingHint =>
      'Something wrong with this listing? Let us know.';

  @override
  String get reportStoreHint => 'Something wrong with this store? Let us know.';

  @override
  String supportRefListing(String title) {
    return 'Listing: $title';
  }

  @override
  String supportRefStore(String title) {
    return 'Store: $title';
  }

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
