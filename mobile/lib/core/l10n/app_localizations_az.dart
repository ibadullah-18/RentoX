// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Azerbaijani (`az`).
class AppL10nAz extends AppL10n {
  AppL10nAz([String locale = 'az']) : super(locale);

  @override
  String get appName => 'RentoX';

  @override
  String get tagline => 'Hər şey kirayə üçün';

  @override
  String get loginTitle => 'Xoş gəldiniz';

  @override
  String get loginSubtitle =>
      'Davam etmək üçün telefon nömrənizi daxil edin. Sizə təsdiq kodu göndərəcəyik.';

  @override
  String get phoneLabel => 'Telefon nömrəsi';

  @override
  String get phoneHint => '50 123 45 67';

  @override
  String get phoneInvalid => 'Nömrə 9 rəqəmdən ibarət olmalıdır';

  @override
  String get continueAction => 'Davam et';

  @override
  String get otpTitle => 'Kodu daxil edin';

  @override
  String otpSubtitle(String phone) {
    return '$phone nömrəsinə göndərilən 6 rəqəmli kodu yazın.';
  }

  @override
  String otpResendIn(int seconds) {
    return 'Yenidən göndər: $seconds san';
  }

  @override
  String get otpResend => 'Kodu yenidən göndər';

  @override
  String get otpWrong => 'Kod yanlışdır və ya vaxtı bitib';

  @override
  String get registerTitle => 'Hesab yarat';

  @override
  String get registerSubtitle =>
      'Bu nömrə ilə hesab tapılmadı. Adınızı yazın, qeydiyyatı tamamlayaq.';

  @override
  String get fullNameLabel => 'Ad və soyad';

  @override
  String get fullNameHint => 'Ad Soyad';

  @override
  String get fullNameRequired => 'Adınızı daxil edin';

  @override
  String get registerAction => 'Qeydiyyatı tamamla';

  @override
  String get errorNetwork => 'İnternet əlaqəsi yoxdur. Yenidən yoxlayın.';

  @override
  String get errorTooManyRequests =>
      'Çox tez-tez cəhd etdiniz. Bir az gözləyib yenidən yoxlayın.';

  @override
  String get errorGeneric => 'Xəta baş verdi. Bir az sonra yenidən cəhd edin.';

  @override
  String get retry => 'Yenidən cəhd et';

  @override
  String get navHome => 'Ana səhifə';

  @override
  String get navFavorites => 'Favori';

  @override
  String get favoritesTitle => 'Favorilər';

  @override
  String favoritesCount(int count) {
    return '$count elan';
  }

  @override
  String get favoritesEmpty => 'Hələ favori elan yoxdur';

  @override
  String get favoritesEmptyHint =>
      'Bəyəndiyin elanları ürək düyməsi ilə burada saxla.';

  @override
  String get browseListings => 'Elanlara bax';

  @override
  String get favoriteAdd => 'Favorilərə əlavə et';

  @override
  String get favoriteRemove => 'Favorilərdən çıxar';

  @override
  String get favoriteFailed => 'Dəyişiklik saxlanmadı. Yenidən cəhd edin.';

  @override
  String get navSearch => 'Axtarış';

  @override
  String get navCreate => 'Elan yarat';

  @override
  String get navMessages => 'Mesajlar';

  @override
  String get navProfile => 'Profil';

  @override
  String get searchHint => 'Nə kirayə axtarırsınız?';

  @override
  String get categories => 'Kateqoriyalar';

  @override
  String get seeAll => 'Hamısına bax';

  @override
  String get vipListings => 'VIP elanlar';

  @override
  String get forYou => 'Sənə uyğun';

  @override
  String get emptyListings => 'Hələ elan yoxdur';

  @override
  String get emptyListingsHint => 'Tezliklə burada yeni elanlar görünəcək.';

  @override
  String get comingSoon => 'Tezliklə';

  @override
  String get comingSoonHint => 'Bu bölmə üzərində işləyirik.';

  @override
  String get vip => 'VIP';

  @override
  String get signIn => 'Daxil ol';

  @override
  String get notifications => 'Bildirişlər';

  @override
  String get filters => 'Filtrlər';

  @override
  String get trustTitle => 'Doğrulanmış istifadəçilər';

  @override
  String get trustSubtitle => 'Təhlükəsiz kirayə, güvənli insanlar';

  @override
  String get listingNotFound => 'Elan tapılmadı';

  @override
  String get listingNotFoundHint => 'Elan silinib və ya müddəti bitib.';

  @override
  String get descriptionTitle => 'Təsvir';

  @override
  String get specsTitle => 'Xüsusiyyətlər';

  @override
  String get ownerTitle => 'Elan sahibi';

  @override
  String get callAction => 'Zəng et';

  @override
  String get messageAction => 'Mesaj yaz';

  @override
  String get yourListing => 'Bu sizin elanınızdır';

  @override
  String viewsCount(int count) {
    return '$count baxış';
  }

  @override
  String get publishedToday => 'Bu gün';

  @override
  String get publishedYesterday => 'Dünən';

  @override
  String publishedDaysAgo(int days) {
    return '$days gün əvvəl';
  }

  @override
  String get yes => 'Bəli';

  @override
  String get no => 'Xeyr';

  @override
  String get messageSheetTitle => 'Elan sahibinə yaz';

  @override
  String get messageHint => 'Mesajınızı yazın';

  @override
  String get messageDefault => 'Salam, bu elan hələ aktualdır?';

  @override
  String get sendAction => 'Göndər';

  @override
  String get messageSent => 'Mesajınız göndərildi';

  @override
  String get messageFailed => 'Mesaj göndərilmədi. Yenidən cəhd edin.';

  @override
  String get callFailed => 'Zəng başladıla bilmədi';

  @override
  String photoCounter(int current, int total) {
    return '$current / $total';
  }

  @override
  String get searchRecent => 'Son axtarışlar';

  @override
  String get clearAll => 'Hamısını sil';

  @override
  String get allCategories => 'Hamısı';

  @override
  String resultsFound(int count) {
    return '$count elan tapıldı';
  }

  @override
  String get noResults => 'Heç nə tapılmadı';

  @override
  String get noResultsHint => 'Başqa söz yoxlayın və ya filtrləri dəyişin.';

  @override
  String get clearFilters => 'Filtrləri təmizlə';

  @override
  String get clearSearch => 'Təmizlə';

  @override
  String get priceFilter => 'Qiymət';

  @override
  String get priceMin => 'Ən az';

  @override
  String get priceMax => 'Ən çox';

  @override
  String get applyAction => 'Tətbiq et';

  @override
  String get resetAction => 'Sıfırla';

  @override
  String get priceRangeInvalid => 'Ən az qiymət ən çoxdan böyük ola bilməz';

  @override
  String get backAction => 'Geri';

  @override
  String get createTitle => 'Yeni elan';

  @override
  String stepOf(int current, int total) {
    return 'Addım $current / $total';
  }

  @override
  String get stepPhotos => 'Şəkillər';

  @override
  String get stepDetails => 'Məlumat';

  @override
  String get stepSpecs => 'Xüsusiyyətlər və qiymət';

  @override
  String get stepReview => 'Yoxla və göndər';

  @override
  String photosHint(int max) {
    return 'İlk şəkil əsas şəkil olacaq. Ən azı 1, ən çox $max şəkil əlavə edin.';
  }

  @override
  String get addPhotos => 'Şəkil əlavə et';

  @override
  String get takePhoto => 'Şəkil çək';

  @override
  String photosCount(int count, int max) {
    return '$count / $max';
  }

  @override
  String get coverLabel => 'Əsas';

  @override
  String get makeCover => 'Əsas şəkil et';

  @override
  String get removePhoto => 'Sil';

  @override
  String get photosRequired => 'Ən azı bir şəkil əlavə edin';

  @override
  String photoUnsupported(int count) {
    return 'Dəstəklənməyən fayllar: $count (yalnız JPG, PNG, WebP)';
  }

  @override
  String photoTooLarge(int count) {
    return '10 MB-dan böyük şəkillər: $count';
  }

  @override
  String photoOverLimit(int max) {
    return 'Ən çox $max şəkil əlavə etmək olar';
  }

  @override
  String get categoryLabel => 'Kateqoriya';

  @override
  String get chooseCategory => 'Kateqoriya seçin';

  @override
  String get categoryRequired => 'Kateqoriya məcburidir';

  @override
  String get titleLabel => 'Elanın başlığı';

  @override
  String get titleHint => 'Məsələn, Toyota Camry 2023';

  @override
  String get titleRequired => 'Başlıq yazın';

  @override
  String get descriptionLabel => 'Təsvir';

  @override
  String get descriptionHint => 'Elanınız haqqında ətraflı yazın';

  @override
  String get descriptionRequired => 'Təsvir yazın';

  @override
  String get priceLabel => 'Qiymət';

  @override
  String get priceAmount => 'Məbləğ (AZN)';

  @override
  String get pricePer => 'Kirayə müddəti';

  @override
  String get priceRequired => 'Qiyməti yazın';

  @override
  String get priceInvalid => 'Düzgün məbləğ yazın';

  @override
  String get fieldRequired => 'Bu sahə məcburidir';

  @override
  String get fieldInvalidNumber => 'Düzgün rəqəm yazın';

  @override
  String get fieldNotWhole => 'Tam ədəd yazın';

  @override
  String get fieldOther => 'Digər';

  @override
  String get fieldCustomHint => 'Özünüz yazın';

  @override
  String get fieldPickDate => 'Tarix seçin';

  @override
  String get specsEmpty => 'Bu kateqoriya üçün əlavə xüsusiyyət tələb olunmur.';

  @override
  String get reviewTitle => 'Elanı yoxlayın';

  @override
  String get reviewNote =>
      'Göndərdikdən sonra elan moderator yoxlamasından keçir və təsdiqlənəndə yayımlanır. Pulsuz limit aşılarsa, aktivləşdirmək üçün ödəniş tələb oluna bilər.';

  @override
  String get nextAction => 'Davam et';

  @override
  String get sendListing => 'Elanı göndər';

  @override
  String get creatingListing => 'Elan yaradılır…';

  @override
  String uploadingPhotos(int current, int total) {
    return 'Şəkillər yüklənir ($current / $total)…';
  }

  @override
  String get submittingForReview => 'Yoxlamaya göndərilir…';

  @override
  String get submitFailed => 'Elan göndərilmədi';

  @override
  String get draftSavedHint =>
      'Elan qaralama kimi yadda saxlanıb. Yenidən cəhd etsəniz, qaldığı yerdən davam edəcək.';

  @override
  String get discardDraft => 'Qaralamanı sil';

  @override
  String get leaveTitle => 'Elan yarımçıq qalacaq';

  @override
  String get leaveBody => 'Daxil etdiyiniz məlumatlar itəcək.';

  @override
  String get leaveStay => 'Qal';

  @override
  String get leaveExit => 'Çıx';

  @override
  String get doneTitle => 'Elanınız göndərildi';

  @override
  String get doneBody =>
      'Elanınız yoxlamadadır. Təsdiqlənəndə bildiriş alacaqsınız.';

  @override
  String get viewMyListings => 'Elanlarıma bax';

  @override
  String get createAnother => 'Yeni elan yarat';

  @override
  String get statusDraft => 'Qaralama';

  @override
  String get statusPending => 'Yoxlamada';

  @override
  String get statusActive => 'Aktiv';

  @override
  String get statusRejected => 'Rədd edildi';

  @override
  String get statusExpired => 'Müddəti bitib';

  @override
  String get statusDeactivated => 'Deaktiv';

  @override
  String get statusDeleted => 'Silinib';

  @override
  String get statusPaymentRequired => 'Ödəniş gözləyir';

  @override
  String get myListingsTitle => 'Elanlarım';

  @override
  String get myListingsEmpty => 'Hələ elanınız yoxdur';

  @override
  String get myListingsEmptyHint =>
      'İlk elanınızı yaradın və kirayə verməyə başlayın.';

  @override
  String get rejectedReason => 'Rədd səbəbi';

  @override
  String get submitForReview => 'Yoxlamaya göndər';

  @override
  String get payActivate => 'Ödə və aktivləşdir';

  @override
  String get makeVip => 'VIP et';

  @override
  String get deactivateAction => 'Deaktiv et';

  @override
  String get reactivateAction => 'Yenidən aktiv et';

  @override
  String expiresOn(String date) {
    return 'Bitmə tarixi: $date';
  }

  @override
  String get actionDone => 'Dəyişiklik saxlanıldı';

  @override
  String get actionFailed => 'Əməliyyat alınmadı. Yenidən cəhd edin.';

  @override
  String get paymentTitle => 'Ödəniş';

  @override
  String get payForActivation => 'Elanın aktivləşdirilməsi';

  @override
  String payForVip(int days) {
    return 'VIP elan ($days gün)';
  }

  @override
  String get paymentAmount => 'Məbləğ';

  @override
  String get walletBalance => 'Balansınız';

  @override
  String get insufficientBalance => 'Balans kifayət etmir';

  @override
  String get topUpDemo => 'Balansı artır (demo)';

  @override
  String get topUpDone => 'Balans artırıldı';

  @override
  String get demoNote => 'Demo rejim: real pul çıxmır.';

  @override
  String payNow(String amount) {
    return 'Ödə: $amount';
  }

  @override
  String get paymentSuccessActivation => 'Ödəniş keçdi. Elanınız aktivdir.';

  @override
  String get paymentSuccessVip => 'Ödəniş keçdi. Elanınız VIP oldu.';

  @override
  String get paymentFailed => 'Ödəniş alınmadı. Yenidən cəhd edin.';

  @override
  String get walletTitle => 'Pul kisəsi';

  @override
  String get vipActive => 'VIP elan aktivdir';

  @override
  String get messagesTitle => 'Mesajlar';

  @override
  String get messagesAll => 'Hamısı';

  @override
  String get messagesUnreadFilter => 'Oxunmamış';

  @override
  String get messagesEmpty => 'Hələ mesajınız yoxdur';

  @override
  String get messagesEmptyHint =>
      'Elan sahibinə yazın, söhbət burada görünəcək.';

  @override
  String get messagesUnreadEmpty => 'Oxunmamış mesaj yoxdur';

  @override
  String get chatUser => 'İstifadəçi';

  @override
  String get chatTyping => 'yazır…';

  @override
  String get chatOnline => 'Onlayn';

  @override
  String get chatOffline => 'Oflayn';

  @override
  String get chatInputHint => 'Mesaj yazın';

  @override
  String get chatEmpty => 'Söhbəti başladın';

  @override
  String get chatPhoto => 'Şəkil';

  @override
  String get chatAttach => 'Şəkil əlavə et';

  @override
  String get chatToday => 'Bu gün';

  @override
  String get chatYesterday => 'Dünən';

  @override
  String get chatViewListing => 'Elana bax';

  @override
  String get chatSendFailed => 'Göndərilmədi';

  @override
  String get chatRetry => 'Yenidən göndər';

  @override
  String get chatDiscard => 'Sil';

  @override
  String get chatTooLong => 'Mesaj çox uzundur (ən çox 2000 simvol)';

  @override
  String get chatBlock => 'İstifadəçini əngəllə';

  @override
  String get chatUnblock => 'Əngəli ləğv et';

  @override
  String get chatBlockedByMe => 'Bu istifadəçini əngəlləmisiniz';

  @override
  String get chatCannotSend => 'Bu söhbətə mesaj yazmaq mümkün deyil';

  @override
  String get chatBlockedDone => 'İstifadəçi əngəlləndi';

  @override
  String get chatUnblockedDone => 'Əngəl ləğv edildi';

  @override
  String get chatReport => 'Şikayət et';

  @override
  String get chatReportTitle => 'Şikayətin səbəbi';

  @override
  String get chatReportDetails => 'Əlavə məlumat (istəyə görə)';

  @override
  String get chatReportSend => 'Göndər';

  @override
  String get chatReportSent => 'Şikayət göndərildi';

  @override
  String get reportSpam => 'Spam';

  @override
  String get reportFraud => 'Dələduzluq';

  @override
  String get reportHarassment => 'Təhqir və ya təzyiq';

  @override
  String get reportProhibited => 'Qadağan olunmuş məzmun';

  @override
  String get reportOther => 'Digər';

  @override
  String get chatReconnecting => 'Bağlantı bərpa olunur…';

  @override
  String get notificationsMarkAll => 'Hamısını oxu';

  @override
  String get notificationsEmpty => 'Bildiriş yoxdur';

  @override
  String get notificationsEmptyHint =>
      'Elanlarınız və hesabınızla bağlı yeniliklər burada görünəcək.';

  @override
  String get notificationView => 'Bax';

  @override
  String get profileEditTitle => 'Profili redaktə et';

  @override
  String get profileAddName => 'Adınızı əlavə edin';

  @override
  String get profileName => 'Ad və soyad';

  @override
  String get profileNameHint => 'Məsələn, Murad Əliyev';

  @override
  String get profileNameInvalid => 'Ad 2 ilə 100 simvol arasında olmalıdır';

  @override
  String get profileBio => 'Haqqınızda';

  @override
  String get profileBioHint => 'Qısa məlumat (istəyə görə)';

  @override
  String get profilePhone => 'Telefon nömrəsi';

  @override
  String get profileSave => 'Yadda saxla';

  @override
  String get profileSaved => 'Profil yeniləndi';

  @override
  String get profilePhotoAdd => 'Şəkil əlavə et';

  @override
  String get profilePhotoChange => 'Şəkli dəyiş';

  @override
  String get profilePhotoTooLarge => 'Şəkil 5 MB-dan böyük ola bilməz';

  @override
  String get languageTitle => 'Dil';

  @override
  String get cancelAction => 'Ləğv et';

  @override
  String get logoutAllTitle => 'Bütün cihazlardan çıx';

  @override
  String get logoutAllMessage =>
      'Hesabınız bütün cihazlarda sistemdən çıxarılacaq. Yenidən daxil olmaq lazım olacaq.';

  @override
  String get logoutAllConfirm => 'Çıx';

  @override
  String get editListingTitle => 'Elanı redaktə et';

  @override
  String get editAction => 'Redaktə et';

  @override
  String get editSave => 'Yadda saxla';

  @override
  String get editSaved => 'Elan yeniləndi. İndi yoxlamaya göndərə bilərsiniz.';

  @override
  String get editNote =>
      'Dəyişikliklərdən sonra elan yenidən yoxlamaya göndərilməlidir.';

  @override
  String get editNotAllowed => 'Bu elan hazırda redaktə oluna bilməz';

  @override
  String get editNotAllowedHint =>
      'Yalnız qaralama və rədd edilmiş elanlar redaktə olunur.';

  @override
  String get editPhotosTitle => 'Şəkillər';

  @override
  String get editPhotoRemoveTitle => 'Şəkil silinsin?';

  @override
  String get editPhotoNeeded =>
      'Yoxlamaya göndərmək üçün ən azı bir şəkil lazımdır';

  @override
  String get deleteListingAction => 'Elanı sil';

  @override
  String get deleteListingTitle => 'Elanı silək?';

  @override
  String get deleteListingMessage =>
      'Elan silinəcək. Bu əməliyyatı geri qaytarmaq olmur.';

  @override
  String get deleteListingConfirm => 'Sil';

  @override
  String get listingDeleted => 'Elan silindi';

  @override
  String get renewAction => 'Elanı yenilə';

  @override
  String get renewHint =>
      'Elanın müddəti bitib. 30 gün üçün yeniləyə bilərsiniz.';

  @override
  String get payForRenewal => 'Elanın yenilənməsi (30 gün)';

  @override
  String get paymentSuccessRenew => 'Elan yeniləndi və yenidən aktivdir';

  @override
  String get bumpAction => 'Yuxarı qaldır';

  @override
  String get payForBump => 'Elanı siyahıda yuxarı qaldırmaq';

  @override
  String get paymentSuccessBump => 'Elan yuxarı qaldırıldı';

  @override
  String get storeMine => 'Mağazam';

  @override
  String get storeFollowing => 'İzlədiyim mağazalar';

  @override
  String get storeNone => 'Hələ mağazanız yoxdur';

  @override
  String get storeNoneHint =>
      'Mağaza açın: bütün elanlarınız bir səhifədə, izləyiciləriniz də olsun.';

  @override
  String get storeOpen => 'Mağaza aç';

  @override
  String get storeCreateTitle => 'Mağaza aç';

  @override
  String get storeEditTitle => 'Mağazanı redaktə et';

  @override
  String get storeSave => 'Yadda saxla';

  @override
  String get storeCreated =>
      'Mağaza yaradıldı. İndi logo əlavə edib yoxlamaya göndərin.';

  @override
  String get storeSaved => 'Mağaza yeniləndi';

  @override
  String get storeSubmitNeedsLogo => 'Yoxlamaya göndərmək üçün logo lazımdır';

  @override
  String get storeSubmitted => 'Mağaza yoxlamaya göndərildi';

  @override
  String get storePendingInfo =>
      'Mağazanız yoxlanılır. Təsdiqlənəndə hamıya görünəcək.';

  @override
  String get storeActiveInfo => 'Mağazanız yayımdadır.';

  @override
  String get storeSuspendedInfo =>
      'Mağazanız dayandırılıb. Əlavə məlumat üçün dəstəyə yazın.';

  @override
  String get storeSuspended => 'Dayandırılıb';

  @override
  String get storeView => 'Mağazaya bax';

  @override
  String get storeEditOnlyDraft =>
      'Yalnız qaralama və rədd edilmiş mağaza redaktə olunur.';

  @override
  String get storeName => 'Mağazanın adı';

  @override
  String get storeDescription => 'Haqqında';

  @override
  String get storePhone => 'Əlaqə nömrəsi';

  @override
  String get storeEmail => 'E-poçt (istəyə görə)';

  @override
  String get storeAddress => 'Ünvan (istəyə görə)';

  @override
  String get storeInstagram => 'Instagram linki (istəyə görə)';

  @override
  String get storeTiktok => 'TikTok linki (istəyə görə)';

  @override
  String get storeFacebook => 'Facebook linki (istəyə görə)';

  @override
  String get storeWebsite => 'Vebsayt (istəyə görə)';

  @override
  String get storeErrShort => 'Çox qısadır';

  @override
  String get storeErrLong => 'Çox uzundur';

  @override
  String get storeErrEmail => 'Düzgün e-poçt yazın';

  @override
  String get storeErrUrl => 'Düzgün link yazın (https://...)';

  @override
  String get storeLogo => 'Logo';

  @override
  String get storeCover => 'Qapaq şəkli';

  @override
  String get storeImageTooLarge =>
      'Şəkil çox böyükdür (logo ≤ 5 MB, qapaq ≤ 10 MB)';

  @override
  String get storeFollow => 'İzlə';

  @override
  String get storeFollowingNow => 'İzləyirsiniz';

  @override
  String get storeListingsTitle => 'Mağazanın elanları';

  @override
  String get storeNoListings => 'Mağazanın aktiv elanı yoxdur';

  @override
  String get storeNotFound => 'Mağaza tapılmadı';

  @override
  String get storeContactTitle => 'Əlaqə';

  @override
  String get storeFollowingEmpty => 'Hələ heç bir mağazanı izləmirsiniz';

  @override
  String get storeFollowingEmptyHint =>
      'Bəyəndiyiniz mağazaları izləyin, onlar burada görünəcək.';

  @override
  String storeFollowers(int count) {
    return '$count izləyici';
  }

  @override
  String storeListingsCount(int count) {
    return '$count elan';
  }

  @override
  String storeViews(int count) {
    return '$count baxış';
  }

  @override
  String get openLinkFailed => 'Link açıla bilmədi';

  @override
  String get supportTitle => 'Dəstək';

  @override
  String get supportNew => 'Yeni müraciət';

  @override
  String get supportEmpty => 'Hələ müraciətiniz yoxdur';

  @override
  String get supportEmptyHint =>
      'Sualınız və ya probleminiz varsa bizə yazın. Cavab bildiriş kimi gələcək.';

  @override
  String get supportNewTitle => 'Dəstəyə yazın';

  @override
  String get supportCategory => 'Mövzu';

  @override
  String get supportSubject => 'Qısa başlıq';

  @override
  String get supportSubjectHint => 'Məsələn, ödənişim keçmədi';

  @override
  String get supportMessage => 'Probleminizi yazın';

  @override
  String get supportMessageHint => 'Nə baş verdi? Nə gözləyirdiniz?';

  @override
  String get supportSend => 'Göndər';

  @override
  String get supportSent => 'Müraciətiniz göndərildi';

  @override
  String get supportErrSubject => 'Başlıq yazın (ən çox 160 simvol)';

  @override
  String get supportErrBody => 'Mesaj yazın (ən çox 4000 simvol)';

  @override
  String get supportReplyHint => 'Cavab yazın';

  @override
  String get supportTeam => 'Dəstək komandası';

  @override
  String get supportClosedInfo =>
      'Bu müraciət bağlanıb. Yeni sualınız varsa yeni müraciət yaradın.';

  @override
  String get supportResolvedInfo =>
      'Həll olunub. Yazsanız müraciət yenidən açılacaq.';

  @override
  String get supportCatGeneral => 'Ümumi sual';

  @override
  String get supportCatAccount => 'Hesab';

  @override
  String get supportCatListing => 'Elan';

  @override
  String get supportCatPayment => 'Ödəniş';

  @override
  String get supportCatStore => 'Mağaza';

  @override
  String get supportCatTechnical => 'Texniki problem';

  @override
  String get supportCatOther => 'Digər';

  @override
  String get supportStOpen => 'Açıq';

  @override
  String get supportStInProgress => 'İşlənir';

  @override
  String get supportStResolved => 'Həll olunub';

  @override
  String get supportStClosed => 'Bağlanıb';

  @override
  String get reportProblem => 'Problem bildir';

  @override
  String get reportListingHint => 'Elanla bağlı problem var? Bizə bildirin.';

  @override
  String get reportStoreHint => 'Mağaza ilə bağlı problem var? Bizə bildirin.';

  @override
  String supportRefListing(String title) {
    return 'Elan: $title';
  }

  @override
  String supportRefStore(String title) {
    return 'Mağaza: $title';
  }

  @override
  String get unitHour => 'saat';

  @override
  String get unitDay => 'gün';

  @override
  String get unitWeek => 'həftə';

  @override
  String get unitMonth => 'ay';

  @override
  String get unitEvent => 'tədbir';

  @override
  String get unitNegotiable => 'razılaşma ilə';

  @override
  String get signOut => 'Çıxış';

  @override
  String get searchTabListings => 'Elanlar';

  @override
  String get searchTabStores => 'Mağazalar';

  @override
  String storesFound(int count) {
    return '$count mağaza tapıldı';
  }

  @override
  String get noStoresFound => 'Mağaza tapılmadı';

  @override
  String get noStoresHint => 'Başqa söz yoxlayın.';

  @override
  String get ownerStoreLabel => 'Mağaza';

  @override
  String get searchSuggestionCategory => 'Kateqoriya';

  @override
  String get filtersPickCategory =>
      'Daha çox filtr üçün əvvəl kateqoriya seçin.';

  @override
  String get filterDateFrom => 'Başlanğıc';

  @override
  String get filterDateTo => 'Son';

  @override
  String get storesTitle => 'Mağazalar';

  @override
  String get sellerType => 'Satıcı növü';

  @override
  String get sellerAll => 'Hamısı';

  @override
  String get sellerStore => 'Mağaza';

  @override
  String get sellerIndividual => 'Fərdi';

  @override
  String get sortTitle => 'Sıralama';

  @override
  String get sortDate => 'Tarix üzrə';

  @override
  String get sortPriceAsc => 'Əvvəl ucuz';

  @override
  String get sortPriceDesc => 'Əvvəl baha';

  @override
  String get similarListings => 'Oxşar elanlar';

  @override
  String get similarMore => 'Daha çox';

  @override
  String get reportListingTitle => 'Elanı şikayət et';

  @override
  String get reportStoreTitle => 'Mağazanı şikayət et';

  @override
  String get reportWhy => 'Səbəbi seçin';

  @override
  String get reportReasonSpam => 'Spam və ya təkrar elan';

  @override
  String get reportReasonFraud => 'Fırıldaq şübhəsi';

  @override
  String get reportReasonProhibited => 'Qadağan olunmuş məzmun';

  @override
  String get reportReasonMisleading => 'Yanıldıcı məlumat və ya şəkil';

  @override
  String get reportReasonWrongCategory => 'Yanlış kateqoriya';

  @override
  String get reportReasonOther => 'Digər';

  @override
  String get reportDetailsHint => 'İstəsəniz, nə baş verdiyini yazın';

  @override
  String get reportDetailsRequired => 'Zəhmət olmasa nə baş verdiyini yazın';

  @override
  String get reportSend => 'Göndər';

  @override
  String get reportSent => 'Şikayətiniz göndərildi. Təşəkkür edirik.';

  @override
  String get reportAlready => 'Bunu artıq bildirmisiniz, baxırıq.';

  @override
  String get reportFailed => 'Göndərilmədi. Yenidən cəhd edin.';

  @override
  String get deleteAccount => 'Hesabı sil';

  @override
  String get deleteAccountTitle => 'Hesabı silmək istəyirsiniz?';

  @override
  String get deleteAccountMessage =>
      'Bütün elanlarınız və mağazanız silinəcək, favorilər və izləmələr itəcək, balansdakı vəsait geri qaytarılmayacaq. Mesajlaşmalar qarşı tərəfdə qalır, amma adınız görünmür. Bu əməliyyatı geri qaytarmaq olmaz.';

  @override
  String deleteAccountType(String word) {
    return 'Təsdiq üçün \"$word\" yazın';
  }

  @override
  String get deleteAccountWord => 'SİL';

  @override
  String get deleteAccountConfirm => 'Həmişəlik sil';

  @override
  String get deleteAccountFailed => 'Hesab silinmədi. Yenidən cəhd edin.';
}
