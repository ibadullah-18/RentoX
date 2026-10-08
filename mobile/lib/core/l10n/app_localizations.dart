import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_az.dart';
import 'app_localizations_en.dart';
import 'app_localizations_ru.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppL10n
/// returned by `AppL10n.of(context)`.
///
/// Applications need to include `AppL10n.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppL10n.localizationsDelegates,
///   supportedLocales: AppL10n.supportedLocales,
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
/// be consistent with the languages listed in the AppL10n.supportedLocales
/// property.
abstract class AppL10n {
  AppL10n(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppL10n of(BuildContext context) {
    return Localizations.of<AppL10n>(context, AppL10n)!;
  }

  static const LocalizationsDelegate<AppL10n> delegate = _AppL10nDelegate();

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
    Locale('az'),
    Locale('en'),
    Locale('ru'),
  ];

  /// No description provided for @appName.
  ///
  /// In az, this message translates to:
  /// **'RentoX'**
  String get appName;

  /// No description provided for @tagline.
  ///
  /// In az, this message translates to:
  /// **'Hər şey kirayə üçün'**
  String get tagline;

  /// No description provided for @loginTitle.
  ///
  /// In az, this message translates to:
  /// **'Xoş gəldiniz'**
  String get loginTitle;

  /// No description provided for @loginSubtitle.
  ///
  /// In az, this message translates to:
  /// **'Davam etmək üçün telefon nömrənizi daxil edin. Sizə təsdiq kodu göndərəcəyik.'**
  String get loginSubtitle;

  /// No description provided for @phoneLabel.
  ///
  /// In az, this message translates to:
  /// **'Telefon nömrəsi'**
  String get phoneLabel;

  /// No description provided for @phoneHint.
  ///
  /// In az, this message translates to:
  /// **'50 123 45 67'**
  String get phoneHint;

  /// No description provided for @phoneInvalid.
  ///
  /// In az, this message translates to:
  /// **'Nömrə 9 rəqəmdən ibarət olmalıdır'**
  String get phoneInvalid;

  /// No description provided for @continueAction.
  ///
  /// In az, this message translates to:
  /// **'Davam et'**
  String get continueAction;

  /// No description provided for @otpTitle.
  ///
  /// In az, this message translates to:
  /// **'Kodu daxil edin'**
  String get otpTitle;

  /// No description provided for @otpSubtitle.
  ///
  /// In az, this message translates to:
  /// **'{phone} nömrəsinə göndərilən 6 rəqəmli kodu yazın.'**
  String otpSubtitle(String phone);

  /// No description provided for @otpResendIn.
  ///
  /// In az, this message translates to:
  /// **'Yenidən göndər: {seconds} san'**
  String otpResendIn(int seconds);

  /// No description provided for @otpResend.
  ///
  /// In az, this message translates to:
  /// **'Kodu yenidən göndər'**
  String get otpResend;

  /// No description provided for @otpWrong.
  ///
  /// In az, this message translates to:
  /// **'Kod yanlışdır və ya vaxtı bitib'**
  String get otpWrong;

  /// No description provided for @registerTitle.
  ///
  /// In az, this message translates to:
  /// **'Hesab yarat'**
  String get registerTitle;

  /// No description provided for @registerSubtitle.
  ///
  /// In az, this message translates to:
  /// **'Bu nömrə ilə hesab tapılmadı. Adınızı yazın, qeydiyyatı tamamlayaq.'**
  String get registerSubtitle;

  /// No description provided for @fullNameLabel.
  ///
  /// In az, this message translates to:
  /// **'Ad və soyad'**
  String get fullNameLabel;

  /// No description provided for @fullNameHint.
  ///
  /// In az, this message translates to:
  /// **'Ad Soyad'**
  String get fullNameHint;

  /// No description provided for @fullNameRequired.
  ///
  /// In az, this message translates to:
  /// **'Adınızı daxil edin'**
  String get fullNameRequired;

  /// No description provided for @registerAction.
  ///
  /// In az, this message translates to:
  /// **'Qeydiyyatı tamamla'**
  String get registerAction;

  /// No description provided for @errorNetwork.
  ///
  /// In az, this message translates to:
  /// **'İnternet əlaqəsi yoxdur. Yenidən yoxlayın.'**
  String get errorNetwork;

  /// No description provided for @errorTooManyRequests.
  ///
  /// In az, this message translates to:
  /// **'Çox tez-tez cəhd etdiniz. Bir az gözləyib yenidən yoxlayın.'**
  String get errorTooManyRequests;

  /// No description provided for @errorGeneric.
  ///
  /// In az, this message translates to:
  /// **'Xəta baş verdi. Bir az sonra yenidən cəhd edin.'**
  String get errorGeneric;

  /// No description provided for @retry.
  ///
  /// In az, this message translates to:
  /// **'Yenidən cəhd et'**
  String get retry;

  /// No description provided for @navHome.
  ///
  /// In az, this message translates to:
  /// **'Ana səhifə'**
  String get navHome;

  /// No description provided for @navFavorites.
  ///
  /// In az, this message translates to:
  /// **'Favori'**
  String get navFavorites;

  /// No description provided for @favoritesTitle.
  ///
  /// In az, this message translates to:
  /// **'Favorilər'**
  String get favoritesTitle;

  /// No description provided for @favoritesCount.
  ///
  /// In az, this message translates to:
  /// **'{count} elan'**
  String favoritesCount(int count);

  /// No description provided for @favoritesEmpty.
  ///
  /// In az, this message translates to:
  /// **'Hələ favori elan yoxdur'**
  String get favoritesEmpty;

  /// No description provided for @favoritesEmptyHint.
  ///
  /// In az, this message translates to:
  /// **'Bəyəndiyin elanları ürək düyməsi ilə burada saxla.'**
  String get favoritesEmptyHint;

  /// No description provided for @browseListings.
  ///
  /// In az, this message translates to:
  /// **'Elanlara bax'**
  String get browseListings;

  /// No description provided for @favoriteAdd.
  ///
  /// In az, this message translates to:
  /// **'Favorilərə əlavə et'**
  String get favoriteAdd;

  /// No description provided for @favoriteRemove.
  ///
  /// In az, this message translates to:
  /// **'Favorilərdən çıxar'**
  String get favoriteRemove;

  /// No description provided for @favoriteFailed.
  ///
  /// In az, this message translates to:
  /// **'Dəyişiklik saxlanmadı. Yenidən cəhd edin.'**
  String get favoriteFailed;

  /// No description provided for @navSearch.
  ///
  /// In az, this message translates to:
  /// **'Axtarış'**
  String get navSearch;

  /// No description provided for @navCreate.
  ///
  /// In az, this message translates to:
  /// **'Elan yarat'**
  String get navCreate;

  /// No description provided for @navMessages.
  ///
  /// In az, this message translates to:
  /// **'Mesajlar'**
  String get navMessages;

  /// No description provided for @navProfile.
  ///
  /// In az, this message translates to:
  /// **'Profil'**
  String get navProfile;

  /// No description provided for @searchHint.
  ///
  /// In az, this message translates to:
  /// **'Nə kirayə axtarırsınız?'**
  String get searchHint;

  /// No description provided for @categories.
  ///
  /// In az, this message translates to:
  /// **'Kateqoriyalar'**
  String get categories;

  /// No description provided for @seeAll.
  ///
  /// In az, this message translates to:
  /// **'Hamısına bax'**
  String get seeAll;

  /// No description provided for @vipListings.
  ///
  /// In az, this message translates to:
  /// **'VIP elanlar'**
  String get vipListings;

  /// No description provided for @forYou.
  ///
  /// In az, this message translates to:
  /// **'Sənə uyğun'**
  String get forYou;

  /// No description provided for @emptyListings.
  ///
  /// In az, this message translates to:
  /// **'Hələ elan yoxdur'**
  String get emptyListings;

  /// No description provided for @emptyListingsHint.
  ///
  /// In az, this message translates to:
  /// **'Tezliklə burada yeni elanlar görünəcək.'**
  String get emptyListingsHint;

  /// No description provided for @comingSoon.
  ///
  /// In az, this message translates to:
  /// **'Tezliklə'**
  String get comingSoon;

  /// No description provided for @comingSoonHint.
  ///
  /// In az, this message translates to:
  /// **'Bu bölmə üzərində işləyirik.'**
  String get comingSoonHint;

  /// No description provided for @vip.
  ///
  /// In az, this message translates to:
  /// **'VIP'**
  String get vip;

  /// No description provided for @signIn.
  ///
  /// In az, this message translates to:
  /// **'Daxil ol'**
  String get signIn;

  /// No description provided for @notifications.
  ///
  /// In az, this message translates to:
  /// **'Bildirişlər'**
  String get notifications;

  /// No description provided for @filters.
  ///
  /// In az, this message translates to:
  /// **'Filtrlər'**
  String get filters;

  /// No description provided for @trustTitle.
  ///
  /// In az, this message translates to:
  /// **'Doğrulanmış istifadəçilər'**
  String get trustTitle;

  /// No description provided for @trustSubtitle.
  ///
  /// In az, this message translates to:
  /// **'Təhlükəsiz kirayə, güvənli insanlar'**
  String get trustSubtitle;

  /// No description provided for @listingNotFound.
  ///
  /// In az, this message translates to:
  /// **'Elan tapılmadı'**
  String get listingNotFound;

  /// No description provided for @listingNotFoundHint.
  ///
  /// In az, this message translates to:
  /// **'Elan silinib və ya müddəti bitib.'**
  String get listingNotFoundHint;

  /// No description provided for @descriptionTitle.
  ///
  /// In az, this message translates to:
  /// **'Təsvir'**
  String get descriptionTitle;

  /// No description provided for @specsTitle.
  ///
  /// In az, this message translates to:
  /// **'Xüsusiyyətlər'**
  String get specsTitle;

  /// No description provided for @ownerTitle.
  ///
  /// In az, this message translates to:
  /// **'Elan sahibi'**
  String get ownerTitle;

  /// No description provided for @callAction.
  ///
  /// In az, this message translates to:
  /// **'Zəng et'**
  String get callAction;

  /// No description provided for @messageAction.
  ///
  /// In az, this message translates to:
  /// **'Mesaj yaz'**
  String get messageAction;

  /// No description provided for @yourListing.
  ///
  /// In az, this message translates to:
  /// **'Bu sizin elanınızdır'**
  String get yourListing;

  /// No description provided for @viewsCount.
  ///
  /// In az, this message translates to:
  /// **'{count} baxış'**
  String viewsCount(int count);

  /// No description provided for @publishedToday.
  ///
  /// In az, this message translates to:
  /// **'Bu gün'**
  String get publishedToday;

  /// No description provided for @publishedYesterday.
  ///
  /// In az, this message translates to:
  /// **'Dünən'**
  String get publishedYesterday;

  /// No description provided for @publishedDaysAgo.
  ///
  /// In az, this message translates to:
  /// **'{days} gün əvvəl'**
  String publishedDaysAgo(int days);

  /// No description provided for @yes.
  ///
  /// In az, this message translates to:
  /// **'Bəli'**
  String get yes;

  /// No description provided for @no.
  ///
  /// In az, this message translates to:
  /// **'Xeyr'**
  String get no;

  /// No description provided for @messageSheetTitle.
  ///
  /// In az, this message translates to:
  /// **'Elan sahibinə yaz'**
  String get messageSheetTitle;

  /// No description provided for @messageHint.
  ///
  /// In az, this message translates to:
  /// **'Mesajınızı yazın'**
  String get messageHint;

  /// No description provided for @messageDefault.
  ///
  /// In az, this message translates to:
  /// **'Salam, bu elan hələ aktualdır?'**
  String get messageDefault;

  /// No description provided for @sendAction.
  ///
  /// In az, this message translates to:
  /// **'Göndər'**
  String get sendAction;

  /// No description provided for @messageSent.
  ///
  /// In az, this message translates to:
  /// **'Mesajınız göndərildi'**
  String get messageSent;

  /// No description provided for @messageFailed.
  ///
  /// In az, this message translates to:
  /// **'Mesaj göndərilmədi. Yenidən cəhd edin.'**
  String get messageFailed;

  /// No description provided for @callFailed.
  ///
  /// In az, this message translates to:
  /// **'Zəng başladıla bilmədi'**
  String get callFailed;

  /// No description provided for @photoCounter.
  ///
  /// In az, this message translates to:
  /// **'{current} / {total}'**
  String photoCounter(int current, int total);

  /// No description provided for @searchRecent.
  ///
  /// In az, this message translates to:
  /// **'Son axtarışlar'**
  String get searchRecent;

  /// No description provided for @clearAll.
  ///
  /// In az, this message translates to:
  /// **'Hamısını sil'**
  String get clearAll;

  /// No description provided for @allCategories.
  ///
  /// In az, this message translates to:
  /// **'Hamısı'**
  String get allCategories;

  /// No description provided for @resultsFound.
  ///
  /// In az, this message translates to:
  /// **'{count} elan tapıldı'**
  String resultsFound(int count);

  /// No description provided for @noResults.
  ///
  /// In az, this message translates to:
  /// **'Heç nə tapılmadı'**
  String get noResults;

  /// No description provided for @noResultsHint.
  ///
  /// In az, this message translates to:
  /// **'Başqa söz yoxlayın və ya filtrləri dəyişin.'**
  String get noResultsHint;

  /// No description provided for @clearFilters.
  ///
  /// In az, this message translates to:
  /// **'Filtrləri təmizlə'**
  String get clearFilters;

  /// No description provided for @clearSearch.
  ///
  /// In az, this message translates to:
  /// **'Təmizlə'**
  String get clearSearch;

  /// No description provided for @priceFilter.
  ///
  /// In az, this message translates to:
  /// **'Qiymət'**
  String get priceFilter;

  /// No description provided for @priceMin.
  ///
  /// In az, this message translates to:
  /// **'Ən az'**
  String get priceMin;

  /// No description provided for @priceMax.
  ///
  /// In az, this message translates to:
  /// **'Ən çox'**
  String get priceMax;

  /// No description provided for @applyAction.
  ///
  /// In az, this message translates to:
  /// **'Tətbiq et'**
  String get applyAction;

  /// No description provided for @resetAction.
  ///
  /// In az, this message translates to:
  /// **'Sıfırla'**
  String get resetAction;

  /// No description provided for @priceRangeInvalid.
  ///
  /// In az, this message translates to:
  /// **'Ən az qiymət ən çoxdan böyük ola bilməz'**
  String get priceRangeInvalid;

  /// No description provided for @backAction.
  ///
  /// In az, this message translates to:
  /// **'Geri'**
  String get backAction;

  /// No description provided for @createTitle.
  ///
  /// In az, this message translates to:
  /// **'Yeni elan'**
  String get createTitle;

  /// No description provided for @stepOf.
  ///
  /// In az, this message translates to:
  /// **'Addım {current} / {total}'**
  String stepOf(int current, int total);

  /// No description provided for @stepPhotos.
  ///
  /// In az, this message translates to:
  /// **'Şəkillər'**
  String get stepPhotos;

  /// No description provided for @stepDetails.
  ///
  /// In az, this message translates to:
  /// **'Məlumat'**
  String get stepDetails;

  /// No description provided for @stepSpecs.
  ///
  /// In az, this message translates to:
  /// **'Xüsusiyyətlər və qiymət'**
  String get stepSpecs;

  /// No description provided for @stepReview.
  ///
  /// In az, this message translates to:
  /// **'Yoxla və göndər'**
  String get stepReview;

  /// No description provided for @photosHint.
  ///
  /// In az, this message translates to:
  /// **'İlk şəkil əsas şəkil olacaq. Ən azı 1, ən çox {max} şəkil əlavə edin.'**
  String photosHint(int max);

  /// No description provided for @addPhotos.
  ///
  /// In az, this message translates to:
  /// **'Şəkil əlavə et'**
  String get addPhotos;

  /// No description provided for @takePhoto.
  ///
  /// In az, this message translates to:
  /// **'Şəkil çək'**
  String get takePhoto;

  /// No description provided for @photosCount.
  ///
  /// In az, this message translates to:
  /// **'{count} / {max}'**
  String photosCount(int count, int max);

  /// No description provided for @coverLabel.
  ///
  /// In az, this message translates to:
  /// **'Əsas'**
  String get coverLabel;

  /// No description provided for @makeCover.
  ///
  /// In az, this message translates to:
  /// **'Əsas şəkil et'**
  String get makeCover;

  /// No description provided for @removePhoto.
  ///
  /// In az, this message translates to:
  /// **'Sil'**
  String get removePhoto;

  /// No description provided for @photosRequired.
  ///
  /// In az, this message translates to:
  /// **'Ən azı bir şəkil əlavə edin'**
  String get photosRequired;

  /// No description provided for @photoUnsupported.
  ///
  /// In az, this message translates to:
  /// **'Dəstəklənməyən fayllar: {count} (yalnız JPG, PNG, WebP)'**
  String photoUnsupported(int count);

  /// No description provided for @photoTooLarge.
  ///
  /// In az, this message translates to:
  /// **'10 MB-dan böyük şəkillər: {count}'**
  String photoTooLarge(int count);

  /// No description provided for @photoOverLimit.
  ///
  /// In az, this message translates to:
  /// **'Ən çox {max} şəkil əlavə etmək olar'**
  String photoOverLimit(int max);

  /// No description provided for @categoryLabel.
  ///
  /// In az, this message translates to:
  /// **'Kateqoriya'**
  String get categoryLabel;

  /// No description provided for @chooseCategory.
  ///
  /// In az, this message translates to:
  /// **'Kateqoriya seçin'**
  String get chooseCategory;

  /// No description provided for @categoryRequired.
  ///
  /// In az, this message translates to:
  /// **'Kateqoriya məcburidir'**
  String get categoryRequired;

  /// No description provided for @titleLabel.
  ///
  /// In az, this message translates to:
  /// **'Elanın başlığı'**
  String get titleLabel;

  /// No description provided for @titleHint.
  ///
  /// In az, this message translates to:
  /// **'Məsələn, Toyota Camry 2023'**
  String get titleHint;

  /// No description provided for @titleRequired.
  ///
  /// In az, this message translates to:
  /// **'Başlıq yazın'**
  String get titleRequired;

  /// No description provided for @descriptionLabel.
  ///
  /// In az, this message translates to:
  /// **'Təsvir'**
  String get descriptionLabel;

  /// No description provided for @descriptionHint.
  ///
  /// In az, this message translates to:
  /// **'Elanınız haqqında ətraflı yazın'**
  String get descriptionHint;

  /// No description provided for @descriptionRequired.
  ///
  /// In az, this message translates to:
  /// **'Təsvir yazın'**
  String get descriptionRequired;

  /// No description provided for @priceLabel.
  ///
  /// In az, this message translates to:
  /// **'Qiymət'**
  String get priceLabel;

  /// No description provided for @priceAmount.
  ///
  /// In az, this message translates to:
  /// **'Məbləğ (AZN)'**
  String get priceAmount;

  /// No description provided for @pricePer.
  ///
  /// In az, this message translates to:
  /// **'Kirayə müddəti'**
  String get pricePer;

  /// No description provided for @priceRequired.
  ///
  /// In az, this message translates to:
  /// **'Qiyməti yazın'**
  String get priceRequired;

  /// No description provided for @priceInvalid.
  ///
  /// In az, this message translates to:
  /// **'Düzgün məbləğ yazın'**
  String get priceInvalid;

  /// No description provided for @fieldRequired.
  ///
  /// In az, this message translates to:
  /// **'Bu sahə məcburidir'**
  String get fieldRequired;

  /// No description provided for @fieldInvalidNumber.
  ///
  /// In az, this message translates to:
  /// **'Düzgün rəqəm yazın'**
  String get fieldInvalidNumber;

  /// No description provided for @fieldNotWhole.
  ///
  /// In az, this message translates to:
  /// **'Tam ədəd yazın'**
  String get fieldNotWhole;

  /// No description provided for @fieldOther.
  ///
  /// In az, this message translates to:
  /// **'Digər'**
  String get fieldOther;

  /// No description provided for @fieldCustomHint.
  ///
  /// In az, this message translates to:
  /// **'Özünüz yazın'**
  String get fieldCustomHint;

  /// No description provided for @fieldPickDate.
  ///
  /// In az, this message translates to:
  /// **'Tarix seçin'**
  String get fieldPickDate;

  /// No description provided for @specsEmpty.
  ///
  /// In az, this message translates to:
  /// **'Bu kateqoriya üçün əlavə xüsusiyyət tələb olunmur.'**
  String get specsEmpty;

  /// No description provided for @reviewTitle.
  ///
  /// In az, this message translates to:
  /// **'Elanı yoxlayın'**
  String get reviewTitle;

  /// No description provided for @reviewNote.
  ///
  /// In az, this message translates to:
  /// **'Göndərdikdən sonra elan moderator yoxlamasından keçir və təsdiqlənəndə yayımlanır. Pulsuz limit aşılarsa, aktivləşdirmək üçün ödəniş tələb oluna bilər.'**
  String get reviewNote;

  /// No description provided for @nextAction.
  ///
  /// In az, this message translates to:
  /// **'Davam et'**
  String get nextAction;

  /// No description provided for @sendListing.
  ///
  /// In az, this message translates to:
  /// **'Elanı göndər'**
  String get sendListing;

  /// No description provided for @creatingListing.
  ///
  /// In az, this message translates to:
  /// **'Elan yaradılır…'**
  String get creatingListing;

  /// No description provided for @uploadingPhotos.
  ///
  /// In az, this message translates to:
  /// **'Şəkillər yüklənir ({current} / {total})…'**
  String uploadingPhotos(int current, int total);

  /// No description provided for @submittingForReview.
  ///
  /// In az, this message translates to:
  /// **'Yoxlamaya göndərilir…'**
  String get submittingForReview;

  /// No description provided for @submitFailed.
  ///
  /// In az, this message translates to:
  /// **'Elan göndərilmədi'**
  String get submitFailed;

  /// No description provided for @draftSavedHint.
  ///
  /// In az, this message translates to:
  /// **'Elan qaralama kimi yadda saxlanıb. Yenidən cəhd etsəniz, qaldığı yerdən davam edəcək.'**
  String get draftSavedHint;

  /// No description provided for @discardDraft.
  ///
  /// In az, this message translates to:
  /// **'Qaralamanı sil'**
  String get discardDraft;

  /// No description provided for @leaveTitle.
  ///
  /// In az, this message translates to:
  /// **'Elan yarımçıq qalacaq'**
  String get leaveTitle;

  /// No description provided for @leaveBody.
  ///
  /// In az, this message translates to:
  /// **'Daxil etdiyiniz məlumatlar itəcək.'**
  String get leaveBody;

  /// No description provided for @leaveStay.
  ///
  /// In az, this message translates to:
  /// **'Qal'**
  String get leaveStay;

  /// No description provided for @leaveExit.
  ///
  /// In az, this message translates to:
  /// **'Çıx'**
  String get leaveExit;

  /// No description provided for @doneTitle.
  ///
  /// In az, this message translates to:
  /// **'Elanınız göndərildi'**
  String get doneTitle;

  /// No description provided for @doneBody.
  ///
  /// In az, this message translates to:
  /// **'Elanınız yoxlamadadır. Təsdiqlənəndə bildiriş alacaqsınız.'**
  String get doneBody;

  /// No description provided for @viewMyListings.
  ///
  /// In az, this message translates to:
  /// **'Elanlarıma bax'**
  String get viewMyListings;

  /// No description provided for @createAnother.
  ///
  /// In az, this message translates to:
  /// **'Yeni elan yarat'**
  String get createAnother;

  /// No description provided for @statusDraft.
  ///
  /// In az, this message translates to:
  /// **'Qaralama'**
  String get statusDraft;

  /// No description provided for @statusPending.
  ///
  /// In az, this message translates to:
  /// **'Yoxlamada'**
  String get statusPending;

  /// No description provided for @statusActive.
  ///
  /// In az, this message translates to:
  /// **'Aktiv'**
  String get statusActive;

  /// No description provided for @statusRejected.
  ///
  /// In az, this message translates to:
  /// **'Rədd edildi'**
  String get statusRejected;

  /// No description provided for @statusExpired.
  ///
  /// In az, this message translates to:
  /// **'Müddəti bitib'**
  String get statusExpired;

  /// No description provided for @statusDeactivated.
  ///
  /// In az, this message translates to:
  /// **'Deaktiv'**
  String get statusDeactivated;

  /// No description provided for @statusDeleted.
  ///
  /// In az, this message translates to:
  /// **'Silinib'**
  String get statusDeleted;

  /// No description provided for @statusPaymentRequired.
  ///
  /// In az, this message translates to:
  /// **'Ödəniş gözləyir'**
  String get statusPaymentRequired;

  /// No description provided for @myListingsTitle.
  ///
  /// In az, this message translates to:
  /// **'Elanlarım'**
  String get myListingsTitle;

  /// No description provided for @myListingsEmpty.
  ///
  /// In az, this message translates to:
  /// **'Hələ elanınız yoxdur'**
  String get myListingsEmpty;

  /// No description provided for @myListingsEmptyHint.
  ///
  /// In az, this message translates to:
  /// **'İlk elanınızı yaradın və kirayə verməyə başlayın.'**
  String get myListingsEmptyHint;

  /// No description provided for @rejectedReason.
  ///
  /// In az, this message translates to:
  /// **'Rədd səbəbi'**
  String get rejectedReason;

  /// No description provided for @submitForReview.
  ///
  /// In az, this message translates to:
  /// **'Yoxlamaya göndər'**
  String get submitForReview;

  /// No description provided for @payActivate.
  ///
  /// In az, this message translates to:
  /// **'Ödə və aktivləşdir'**
  String get payActivate;

  /// No description provided for @makeVip.
  ///
  /// In az, this message translates to:
  /// **'VIP et'**
  String get makeVip;

  /// No description provided for @deactivateAction.
  ///
  /// In az, this message translates to:
  /// **'Deaktiv et'**
  String get deactivateAction;

  /// No description provided for @reactivateAction.
  ///
  /// In az, this message translates to:
  /// **'Yenidən aktiv et'**
  String get reactivateAction;

  /// No description provided for @expiresOn.
  ///
  /// In az, this message translates to:
  /// **'Bitmə tarixi: {date}'**
  String expiresOn(String date);

  /// No description provided for @actionDone.
  ///
  /// In az, this message translates to:
  /// **'Dəyişiklik saxlanıldı'**
  String get actionDone;

  /// No description provided for @actionFailed.
  ///
  /// In az, this message translates to:
  /// **'Əməliyyat alınmadı. Yenidən cəhd edin.'**
  String get actionFailed;

  /// No description provided for @paymentTitle.
  ///
  /// In az, this message translates to:
  /// **'Ödəniş'**
  String get paymentTitle;

  /// No description provided for @payForActivation.
  ///
  /// In az, this message translates to:
  /// **'Elanın aktivləşdirilməsi'**
  String get payForActivation;

  /// No description provided for @payForVip.
  ///
  /// In az, this message translates to:
  /// **'VIP elan ({days} gün)'**
  String payForVip(int days);

  /// No description provided for @paymentAmount.
  ///
  /// In az, this message translates to:
  /// **'Məbləğ'**
  String get paymentAmount;

  /// No description provided for @walletBalance.
  ///
  /// In az, this message translates to:
  /// **'Balansınız'**
  String get walletBalance;

  /// No description provided for @insufficientBalance.
  ///
  /// In az, this message translates to:
  /// **'Balans kifayət etmir'**
  String get insufficientBalance;

  /// No description provided for @topUpDemo.
  ///
  /// In az, this message translates to:
  /// **'Balansı artır (demo)'**
  String get topUpDemo;

  /// No description provided for @topUpDone.
  ///
  /// In az, this message translates to:
  /// **'Balans artırıldı'**
  String get topUpDone;

  /// No description provided for @demoNote.
  ///
  /// In az, this message translates to:
  /// **'Demo rejim: real pul çıxmır.'**
  String get demoNote;

  /// No description provided for @payNow.
  ///
  /// In az, this message translates to:
  /// **'Ödə: {amount}'**
  String payNow(String amount);

  /// No description provided for @paymentSuccessActivation.
  ///
  /// In az, this message translates to:
  /// **'Ödəniş keçdi. Elanınız aktivdir.'**
  String get paymentSuccessActivation;

  /// No description provided for @paymentSuccessVip.
  ///
  /// In az, this message translates to:
  /// **'Ödəniş keçdi. Elanınız VIP oldu.'**
  String get paymentSuccessVip;

  /// No description provided for @paymentFailed.
  ///
  /// In az, this message translates to:
  /// **'Ödəniş alınmadı. Yenidən cəhd edin.'**
  String get paymentFailed;

  /// No description provided for @walletTitle.
  ///
  /// In az, this message translates to:
  /// **'Pul kisəsi'**
  String get walletTitle;

  /// No description provided for @vipActive.
  ///
  /// In az, this message translates to:
  /// **'VIP elan aktivdir'**
  String get vipActive;

  /// No description provided for @unitHour.
  ///
  /// In az, this message translates to:
  /// **'saat'**
  String get unitHour;

  /// No description provided for @unitDay.
  ///
  /// In az, this message translates to:
  /// **'gün'**
  String get unitDay;

  /// No description provided for @unitWeek.
  ///
  /// In az, this message translates to:
  /// **'həftə'**
  String get unitWeek;

  /// No description provided for @unitMonth.
  ///
  /// In az, this message translates to:
  /// **'ay'**
  String get unitMonth;

  /// No description provided for @unitEvent.
  ///
  /// In az, this message translates to:
  /// **'tədbir'**
  String get unitEvent;

  /// No description provided for @unitNegotiable.
  ///
  /// In az, this message translates to:
  /// **'razılaşma ilə'**
  String get unitNegotiable;

  /// No description provided for @signOut.
  ///
  /// In az, this message translates to:
  /// **'Çıxış'**
  String get signOut;
}

class _AppL10nDelegate extends LocalizationsDelegate<AppL10n> {
  const _AppL10nDelegate();

  @override
  Future<AppL10n> load(Locale locale) {
    return SynchronousFuture<AppL10n>(lookupAppL10n(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['az', 'en', 'ru'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppL10nDelegate old) => false;
}

AppL10n lookupAppL10n(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'az':
      return AppL10nAz();
    case 'en':
      return AppL10nEn();
    case 'ru':
      return AppL10nRu();
  }

  throw FlutterError(
    'AppL10n.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
