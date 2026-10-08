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
}
