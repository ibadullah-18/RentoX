// ignore: unused_import
import 'package:intl/intl.dart' as intl;

import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Russian (`ru`).
class AppL10nRu extends AppL10n {
  AppL10nRu([String locale = 'ru']) : super(locale);

  @override
  String get appName => 'RentoX';

  @override
  String get tagline => 'Всё для аренды';

  @override
  String get loginTitle => 'Добро пожаловать';

  @override
  String get loginSubtitle =>
      'Введите номер телефона, чтобы продолжить. Мы отправим код подтверждения.';

  @override
  String get phoneLabel => 'Номер телефона';

  @override
  String get phoneHint => '50 123 45 67';

  @override
  String get phoneInvalid => 'Номер должен содержать 9 цифр';

  @override
  String get continueAction => 'Продолжить';

  @override
  String get otpTitle => 'Введите код';

  @override
  String otpSubtitle(String phone) {
    return 'Введите 6-значный код, отправленный на $phone.';
  }

  @override
  String otpResendIn(int seconds) {
    return 'Отправить снова через $seconds с';
  }

  @override
  String get otpResend => 'Отправить код снова';

  @override
  String get otpWrong => 'Код неверный или истёк';

  @override
  String get registerTitle => 'Создать аккаунт';

  @override
  String get registerSubtitle =>
      'Аккаунт с этим номером не найден. Введите имя, чтобы завершить регистрацию.';

  @override
  String get fullNameLabel => 'Имя и фамилия';

  @override
  String get fullNameHint => 'Имя Фамилия';

  @override
  String get fullNameRequired => 'Введите имя';

  @override
  String get registerAction => 'Завершить регистрацию';

  @override
  String get errorNetwork => 'Нет подключения к интернету. Попробуйте снова.';

  @override
  String get errorTooManyRequests =>
      'Слишком много попыток. Подождите немного и повторите.';

  @override
  String get errorGeneric => 'Что-то пошло не так. Попробуйте чуть позже.';

  @override
  String get retry => 'Повторить';

  @override
  String get navHome => 'Главная';

  @override
  String get navFavorites => 'Избранное';

  @override
  String get favoritesTitle => 'Избранное';

  @override
  String favoritesCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count объявления',
      many: '$count объявлений',
      few: '$count объявления',
      one: '$count объявление',
    );
    return '$_temp0';
  }

  @override
  String get favoritesEmpty => 'В избранном пока пусто';

  @override
  String get favoritesEmptyHint =>
      'Нажмите на сердечко в объявлении, чтобы сохранить его здесь.';

  @override
  String get browseListings => 'Смотреть объявления';

  @override
  String get favoriteAdd => 'Добавить в избранное';

  @override
  String get favoriteRemove => 'Убрать из избранного';

  @override
  String get favoriteFailed =>
      'Не удалось сохранить изменение. Попробуйте снова.';

  @override
  String get navSearch => 'Поиск';

  @override
  String get navCreate => 'Объявление';

  @override
  String get navMessages => 'Сообщения';

  @override
  String get navProfile => 'Профиль';

  @override
  String get searchHint => 'Что хотите арендовать?';

  @override
  String get categories => 'Категории';

  @override
  String get seeAll => 'Смотреть все';

  @override
  String get vipListings => 'VIP объявления';

  @override
  String get forYou => 'Для вас';

  @override
  String get emptyListings => 'Объявлений пока нет';

  @override
  String get emptyListingsHint => 'Скоро здесь появятся новые объявления.';

  @override
  String get comingSoon => 'Скоро';

  @override
  String get comingSoonHint => 'Мы работаем над этим разделом.';

  @override
  String get vip => 'VIP';

  @override
  String get signIn => 'Войти';

  @override
  String get notifications => 'Уведомления';

  @override
  String get filters => 'Фильтры';

  @override
  String get trustTitle => 'Проверенные пользователи';

  @override
  String get trustSubtitle => 'Безопасная аренда, надёжные люди';

  @override
  String get listingNotFound => 'Объявление не найдено';

  @override
  String get listingNotFoundHint => 'Возможно, оно удалено или срок истёк.';

  @override
  String get descriptionTitle => 'Описание';

  @override
  String get specsTitle => 'Характеристики';

  @override
  String get ownerTitle => 'Автор объявления';

  @override
  String get callAction => 'Позвонить';

  @override
  String get messageAction => 'Написать';

  @override
  String get yourListing => 'Это ваше объявление';

  @override
  String viewsCount(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: '$count просмотра',
      many: '$count просмотров',
      few: '$count просмотра',
      one: '$count просмотр',
    );
    return '$_temp0';
  }

  @override
  String get publishedToday => 'Сегодня';

  @override
  String get publishedYesterday => 'Вчера';

  @override
  String publishedDaysAgo(int days) {
    String _temp0 = intl.Intl.pluralLogic(
      days,
      locale: localeName,
      other: '$days дня назад',
      many: '$days дней назад',
      few: '$days дня назад',
      one: '$days день назад',
    );
    return '$_temp0';
  }

  @override
  String get yes => 'Да';

  @override
  String get no => 'Нет';

  @override
  String get messageSheetTitle => 'Написать владельцу';

  @override
  String get messageHint => 'Введите сообщение';

  @override
  String get messageDefault => 'Здравствуйте, объявление ещё актуально?';

  @override
  String get sendAction => 'Отправить';

  @override
  String get messageSent => 'Сообщение отправлено';

  @override
  String get messageFailed =>
      'Не удалось отправить сообщение. Попробуйте снова.';

  @override
  String get callFailed => 'Не удалось начать звонок';

  @override
  String photoCounter(int current, int total) {
    return '$current / $total';
  }

  @override
  String get searchRecent => 'Недавние запросы';

  @override
  String get clearAll => 'Очистить всё';

  @override
  String get allCategories => 'Все';

  @override
  String resultsFound(int count) {
    String _temp0 = intl.Intl.pluralLogic(
      count,
      locale: localeName,
      other: 'Найдено $count объявления',
      many: 'Найдено $count объявлений',
      few: 'Найдено $count объявления',
      one: 'Найдено $count объявление',
    );
    return '$_temp0';
  }

  @override
  String get noResults => 'Ничего не найдено';

  @override
  String get noResultsHint => 'Попробуйте другое слово или измените фильтры.';

  @override
  String get clearFilters => 'Сбросить фильтры';

  @override
  String get clearSearch => 'Очистить';

  @override
  String get priceFilter => 'Цена';

  @override
  String get priceMin => 'От';

  @override
  String get priceMax => 'До';

  @override
  String get applyAction => 'Применить';

  @override
  String get resetAction => 'Сбросить';

  @override
  String get priceRangeInvalid =>
      'Минимальная цена не может быть выше максимальной';

  @override
  String get backAction => 'Назад';

  @override
  String get createTitle => 'Новое объявление';

  @override
  String stepOf(int current, int total) {
    return 'Шаг $current из $total';
  }

  @override
  String get stepPhotos => 'Фото';

  @override
  String get stepDetails => 'Описание';

  @override
  String get stepSpecs => 'Характеристики и цена';

  @override
  String get stepReview => 'Проверка и отправка';

  @override
  String photosHint(int max) {
    return 'Первое фото будет обложкой. Добавьте от 1 до $max фото.';
  }

  @override
  String get addPhotos => 'Добавить фото';

  @override
  String get takePhoto => 'Сделать фото';

  @override
  String photosCount(int count, int max) {
    return '$count / $max';
  }

  @override
  String get coverLabel => 'Обложка';

  @override
  String get makeCover => 'Сделать обложкой';

  @override
  String get removePhoto => 'Удалить';

  @override
  String get photosRequired => 'Добавьте хотя бы одно фото';

  @override
  String photoUnsupported(int count) {
    return 'Неподдерживаемые файлы: $count (только JPG, PNG, WebP)';
  }

  @override
  String photoTooLarge(int count) {
    return 'Фото больше 10 МБ: $count';
  }

  @override
  String photoOverLimit(int max) {
    return 'Можно добавить не более $max фото';
  }

  @override
  String get categoryLabel => 'Категория';

  @override
  String get chooseCategory => 'Выберите категорию';

  @override
  String get categoryRequired => 'Категория обязательна';

  @override
  String get titleLabel => 'Заголовок';

  @override
  String get titleHint => 'Например, Toyota Camry 2023';

  @override
  String get titleRequired => 'Введите заголовок';

  @override
  String get descriptionLabel => 'Описание';

  @override
  String get descriptionHint => 'Подробно опишите объявление';

  @override
  String get descriptionRequired => 'Введите описание';

  @override
  String get priceLabel => 'Цена';

  @override
  String get priceAmount => 'Сумма (AZN)';

  @override
  String get pricePer => 'Период аренды';

  @override
  String get priceRequired => 'Введите цену';

  @override
  String get priceInvalid => 'Введите корректную сумму';

  @override
  String get fieldRequired => 'Обязательное поле';

  @override
  String get fieldInvalidNumber => 'Введите корректное число';

  @override
  String get fieldNotWhole => 'Введите целое число';

  @override
  String get fieldOther => 'Другое';

  @override
  String get fieldCustomHint => 'Введите своё';

  @override
  String get fieldPickDate => 'Выберите дату';

  @override
  String get specsEmpty => 'Для этой категории дополнительные поля не нужны.';

  @override
  String get reviewTitle => 'Проверьте объявление';

  @override
  String get reviewNote =>
      'После отправки объявление проходит модерацию и публикуется после одобрения. Если бесплатный лимит исчерпан, для активации может потребоваться оплата.';

  @override
  String get nextAction => 'Продолжить';

  @override
  String get sendListing => 'Отправить объявление';

  @override
  String get creatingListing => 'Создаём объявление…';

  @override
  String uploadingPhotos(int current, int total) {
    return 'Загружаем фото ($current из $total)…';
  }

  @override
  String get submittingForReview => 'Отправляем на модерацию…';

  @override
  String get submitFailed => 'Объявление не отправлено';

  @override
  String get draftSavedHint =>
      'Объявление сохранено как черновик. Повторная попытка продолжит с того же места.';

  @override
  String get discardDraft => 'Удалить черновик';

  @override
  String get leaveTitle => 'Выйти из создания?';

  @override
  String get leaveBody => 'Введённые данные будут потеряны.';

  @override
  String get leaveStay => 'Остаться';

  @override
  String get leaveExit => 'Выйти';

  @override
  String get doneTitle => 'Объявление отправлено';

  @override
  String get doneBody =>
      'Объявление на проверке. Вы получите уведомление после одобрения.';

  @override
  String get viewMyListings => 'Мои объявления';

  @override
  String get createAnother => 'Создать ещё';

  @override
  String get statusDraft => 'Черновик';

  @override
  String get statusPending => 'На проверке';

  @override
  String get statusActive => 'Активно';

  @override
  String get statusRejected => 'Отклонено';

  @override
  String get statusExpired => 'Истекло';

  @override
  String get statusDeactivated => 'Отключено';

  @override
  String get statusDeleted => 'Удалено';

  @override
  String get statusPaymentRequired => 'Ожидает оплаты';

  @override
  String get myListingsTitle => 'Мои объявления';

  @override
  String get myListingsEmpty => 'Объявлений пока нет';

  @override
  String get myListingsEmptyHint =>
      'Создайте первое объявление и начните сдавать.';

  @override
  String get rejectedReason => 'Причина отклонения';

  @override
  String get submitForReview => 'Отправить на проверку';

  @override
  String get payActivate => 'Оплатить и активировать';

  @override
  String get makeVip => 'Сделать VIP';

  @override
  String get deactivateAction => 'Отключить';

  @override
  String get reactivateAction => 'Включить снова';

  @override
  String expiresOn(String date) {
    return 'Действует до $date';
  }

  @override
  String get actionDone => 'Изменения сохранены';

  @override
  String get actionFailed => 'Не получилось. Попробуйте снова.';

  @override
  String get paymentTitle => 'Оплата';

  @override
  String get payForActivation => 'Активация объявления';

  @override
  String payForVip(int days) {
    return 'VIP-объявление ($days дн.)';
  }

  @override
  String get paymentAmount => 'Сумма';

  @override
  String get walletBalance => 'Ваш баланс';

  @override
  String get insufficientBalance => 'Недостаточно средств';

  @override
  String get topUpDemo => 'Пополнить (демо)';

  @override
  String get topUpDone => 'Баланс пополнен';

  @override
  String get demoNote => 'Демо-режим: реальные деньги не списываются.';

  @override
  String payNow(String amount) {
    return 'Оплатить $amount';
  }

  @override
  String get paymentSuccessActivation => 'Оплата прошла. Объявление активно.';

  @override
  String get paymentSuccessVip => 'Оплата прошла. Объявление стало VIP.';

  @override
  String get paymentFailed => 'Оплата не прошла. Попробуйте снова.';

  @override
  String get walletTitle => 'Кошелёк';

  @override
  String get vipActive => 'VIP-объявление активно';

  @override
  String get messagesTitle => 'Сообщения';

  @override
  String get messagesAll => 'Все';

  @override
  String get messagesUnreadFilter => 'Непрочитанные';

  @override
  String get messagesEmpty => 'Сообщений пока нет';

  @override
  String get messagesEmptyHint =>
      'Напишите владельцу объявления, и чат появится здесь.';

  @override
  String get messagesUnreadEmpty => 'Нет непрочитанных сообщений';

  @override
  String get chatUser => 'Пользователь';

  @override
  String get chatTyping => 'печатает…';

  @override
  String get chatOnline => 'В сети';

  @override
  String get chatOffline => 'Не в сети';

  @override
  String get chatInputHint => 'Напишите сообщение';

  @override
  String get chatEmpty => 'Начните разговор';

  @override
  String get chatPhoto => 'Фото';

  @override
  String get chatAttach => 'Прикрепить фото';

  @override
  String get chatToday => 'Сегодня';

  @override
  String get chatYesterday => 'Вчера';

  @override
  String get chatViewListing => 'Открыть объявление';

  @override
  String get chatSendFailed => 'Не отправлено';

  @override
  String get chatRetry => 'Отправить снова';

  @override
  String get chatDiscard => 'Удалить';

  @override
  String get chatTooLong =>
      'Сообщение слишком длинное (максимум 2000 символов)';

  @override
  String get chatBlock => 'Заблокировать';

  @override
  String get chatUnblock => 'Разблокировать';

  @override
  String get chatBlockedByMe => 'Вы заблокировали этого пользователя';

  @override
  String get chatCannotSend => 'В этот чат нельзя писать';

  @override
  String get chatBlockedDone => 'Пользователь заблокирован';

  @override
  String get chatUnblockedDone => 'Пользователь разблокирован';

  @override
  String get chatReport => 'Пожаловаться';

  @override
  String get chatReportTitle => 'Причина жалобы';

  @override
  String get chatReportDetails => 'Подробности (необязательно)';

  @override
  String get chatReportSend => 'Отправить';

  @override
  String get chatReportSent => 'Жалоба отправлена';

  @override
  String get reportSpam => 'Спам';

  @override
  String get reportFraud => 'Мошенничество';

  @override
  String get reportHarassment => 'Оскорбления или давление';

  @override
  String get reportProhibited => 'Запрещённый контент';

  @override
  String get reportOther => 'Другое';

  @override
  String get chatReconnecting => 'Восстанавливаем соединение…';

  @override
  String get notificationsMarkAll => 'Прочитать все';

  @override
  String get notificationsEmpty => 'Уведомлений нет';

  @override
  String get notificationsEmptyHint =>
      'Здесь появятся новости по вашим объявлениям и аккаунту.';

  @override
  String get notificationView => 'Открыть';

  @override
  String get profileEditTitle => 'Редактировать профиль';

  @override
  String get profileAddName => 'Добавьте своё имя';

  @override
  String get profileName => 'Имя и фамилия';

  @override
  String get profileNameHint => 'Например, Мурад Алиев';

  @override
  String get profileNameInvalid => 'Имя должно содержать от 2 до 100 символов';

  @override
  String get profileBio => 'О себе';

  @override
  String get profileBioHint => 'Пара слов о себе (необязательно)';

  @override
  String get profilePhone => 'Номер телефона';

  @override
  String get profileSave => 'Сохранить';

  @override
  String get profileSaved => 'Профиль обновлён';

  @override
  String get profilePhotoAdd => 'Добавить фото';

  @override
  String get profilePhotoChange => 'Изменить фото';

  @override
  String get profilePhotoTooLarge => 'Фото не должно быть больше 5 МБ';

  @override
  String get languageTitle => 'Язык';

  @override
  String get cancelAction => 'Отмена';

  @override
  String get logoutAllTitle => 'Выйти на всех устройствах';

  @override
  String get logoutAllMessage =>
      'Вы выйдете из аккаунта на всех устройствах и должны будете войти снова.';

  @override
  String get logoutAllConfirm => 'Выйти';

  @override
  String get editListingTitle => 'Редактирование объявления';

  @override
  String get editAction => 'Редактировать';

  @override
  String get editSave => 'Сохранить';

  @override
  String get editSaved =>
      'Объявление обновлено. Теперь его можно отправить на модерацию.';

  @override
  String get editNote =>
      'После сохранения объявление нужно снова отправить на модерацию.';

  @override
  String get editNotAllowed => 'Это объявление сейчас нельзя редактировать';

  @override
  String get editNotAllowedHint =>
      'Редактировать можно только черновики и отклонённые объявления.';

  @override
  String get editPhotosTitle => 'Фото';

  @override
  String get editPhotoRemoveTitle => 'Удалить это фото?';

  @override
  String get editPhotoNeeded =>
      'Для отправки на модерацию нужно хотя бы одно фото';

  @override
  String get deleteListingAction => 'Удалить объявление';

  @override
  String get deleteListingTitle => 'Удалить объявление?';

  @override
  String get deleteListingMessage =>
      'Объявление будет удалено. Это действие нельзя отменить.';

  @override
  String get deleteListingConfirm => 'Удалить';

  @override
  String get listingDeleted => 'Объявление удалено';

  @override
  String get renewAction => 'Продлить объявление';

  @override
  String get renewHint =>
      'Срок объявления истёк. Его можно продлить ещё на 30 дней.';

  @override
  String get payForRenewal => 'Продление объявления (30 дней)';

  @override
  String get paymentSuccessRenew => 'Объявление продлено и снова активно';

  @override
  String get bumpAction => 'Поднять наверх';

  @override
  String get payForBump => 'Поднять объявление в списке';

  @override
  String get paymentSuccessBump => 'Объявление поднято';

  @override
  String get storeMine => 'Мой магазин';

  @override
  String get storeFollowing => 'Магазины, на которые я подписан';

  @override
  String get storeNone => 'У вас пока нет магазина';

  @override
  String get storeNoneHint =>
      'Откройте магазин: все ваши объявления на одной странице и подписчики.';

  @override
  String get storeOpen => 'Открыть магазин';

  @override
  String get storeCreateTitle => 'Открыть магазин';

  @override
  String get storeEditTitle => 'Редактировать магазин';

  @override
  String get storeSave => 'Сохранить';

  @override
  String get storeCreated =>
      'Магазин создан. Добавьте логотип и отправьте на модерацию.';

  @override
  String get storeSaved => 'Магазин обновлён';

  @override
  String get storeSubmitNeedsLogo => 'Для отправки на модерацию нужен логотип';

  @override
  String get storeSubmitted => 'Магазин отправлен на модерацию';

  @override
  String get storePendingInfo =>
      'Ваш магазин на проверке. После одобрения он станет публичным.';

  @override
  String get storeActiveInfo => 'Ваш магазин опубликован.';

  @override
  String get storeSuspendedInfo =>
      'Ваш магазин приостановлен. Обратитесь в поддержку.';

  @override
  String get storeSuspended => 'Приостановлен';

  @override
  String get storeView => 'Открыть магазин';

  @override
  String get storeEditOnlyDraft =>
      'Редактировать можно только черновик или отклонённый магазин.';

  @override
  String get storeName => 'Название магазина';

  @override
  String get storeDescription => 'О магазине';

  @override
  String get storePhone => 'Контактный телефон';

  @override
  String get storeEmail => 'Эл. почта (необязательно)';

  @override
  String get storeAddress => 'Адрес (необязательно)';

  @override
  String get storeInstagram => 'Ссылка на Instagram (необязательно)';

  @override
  String get storeTiktok => 'Ссылка на TikTok (необязательно)';

  @override
  String get storeFacebook => 'Ссылка на Facebook (необязательно)';

  @override
  String get storeWebsite => 'Сайт (необязательно)';

  @override
  String get storeErrShort => 'Слишком коротко';

  @override
  String get storeErrLong => 'Слишком длинно';

  @override
  String get storeErrEmail => 'Введите корректную почту';

  @override
  String get storeErrUrl => 'Введите корректную ссылку (https://...)';

  @override
  String get storeLogo => 'Логотип';

  @override
  String get storeCover => 'Обложка';

  @override
  String get storeImageTooLarge =>
      'Файл слишком большой (логотип до 5 МБ, обложка до 10 МБ)';

  @override
  String get storeFollow => 'Подписаться';

  @override
  String get storeFollowingNow => 'Вы подписаны';

  @override
  String get storeListingsTitle => 'Объявления магазина';

  @override
  String get storeNoListings => 'У магазина нет активных объявлений';

  @override
  String get storeNotFound => 'Магазин не найден';

  @override
  String get storeContactTitle => 'Контакты';

  @override
  String get storeFollowingEmpty => 'Вы пока ни на кого не подписаны';

  @override
  String get storeFollowingEmptyHint =>
      'Подписывайтесь на понравившиеся магазины, и они появятся здесь.';

  @override
  String storeFollowers(int count) {
    return 'Подписчиков: $count';
  }

  @override
  String storeListingsCount(int count) {
    return 'Объявлений: $count';
  }

  @override
  String storeViews(int count) {
    return 'Просмотров: $count';
  }

  @override
  String get openLinkFailed => 'Не удалось открыть ссылку';

  @override
  String get supportTitle => 'Поддержка';

  @override
  String get supportNew => 'Новое обращение';

  @override
  String get supportEmpty => 'Обращений пока нет';

  @override
  String get supportEmptyHint =>
      'Напишите нам, если есть вопрос или проблема. Ответ придёт уведомлением.';

  @override
  String get supportNewTitle => 'Написать в поддержку';

  @override
  String get supportCategory => 'Тема';

  @override
  String get supportSubject => 'Краткий заголовок';

  @override
  String get supportSubjectHint => 'Например, платёж не прошёл';

  @override
  String get supportMessage => 'Опишите проблему';

  @override
  String get supportMessageHint => 'Что произошло? Что вы ожидали?';

  @override
  String get supportSend => 'Отправить';

  @override
  String get supportSent => 'Обращение отправлено';

  @override
  String get supportErrSubject => 'Введите заголовок (до 160 символов)';

  @override
  String get supportErrBody => 'Напишите сообщение (до 4000 символов)';

  @override
  String get supportReplyHint => 'Напишите ответ';

  @override
  String get supportTeam => 'Команда поддержки';

  @override
  String get supportClosedInfo =>
      'Обращение закрыто. По новому вопросу создайте новое обращение.';

  @override
  String get supportResolvedInfo =>
      'Отмечено как решённое. Ответ откроет его снова.';

  @override
  String get supportCatGeneral => 'Общий вопрос';

  @override
  String get supportCatAccount => 'Аккаунт';

  @override
  String get supportCatListing => 'Объявление';

  @override
  String get supportCatPayment => 'Платёж';

  @override
  String get supportCatStore => 'Магазин';

  @override
  String get supportCatTechnical => 'Техническая проблема';

  @override
  String get supportCatOther => 'Другое';

  @override
  String get supportStOpen => 'Открыто';

  @override
  String get supportStInProgress => 'В работе';

  @override
  String get supportStResolved => 'Решено';

  @override
  String get supportStClosed => 'Закрыто';

  @override
  String get reportProblem => 'Сообщить о проблеме';

  @override
  String get reportListingHint => 'Проблема с этим объявлением? Сообщите нам.';

  @override
  String get reportStoreHint => 'Проблема с этим магазином? Сообщите нам.';

  @override
  String supportRefListing(String title) {
    return 'Объявление: $title';
  }

  @override
  String supportRefStore(String title) {
    return 'Магазин: $title';
  }

  @override
  String get unitHour => 'час';

  @override
  String get unitDay => 'день';

  @override
  String get unitWeek => 'неделя';

  @override
  String get unitMonth => 'месяц';

  @override
  String get unitEvent => 'мероприятие';

  @override
  String get unitNegotiable => 'договорная';

  @override
  String get signOut => 'Выйти';
}
