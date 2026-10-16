// Shapes returned by the RentoX API (see backend Contracts).

export type RentalPeriodId = 1 | 2 | 3 | 4 | 5 | 6;

export type Category = {
  id: string;
  slug: string;
  name: string;
  iconUrl?: string | null;
  children: Category[];
  /** Which price periods listings of this category may use (all when empty). */
  rentalPeriods?: number[];
};

export type ListingSummary = {
  id: string;
  title: string;
  price: number;
  currency: string;
  rentalPeriodUnit: RentalPeriodId;
  isVip: boolean;
  isFavorite: boolean;
  viewCount: number;
  favoriteCount: number;
  categoryName?: string | null;
  coverImageUrl?: string | null;
  location?: string | null;
};

export type Paged<T> = {
  items: T[];
  page: number;
  totalPages: number;
  totalCount: number;
};

export type ListingImage = {
  id: string;
  url: string;
  displayOrder: number;
  isCover: boolean;
};

export type ListingField = {
  fieldId?: string;
  label?: string;
  key?: string;
  type: number;
  textValue?: string | null;
  numericValue?: number | null;
  flagValue?: boolean | null;
  calendarValue?: string | null;
  customValue?: string | null;
  selections?: { label?: string; value?: string; optionId?: string }[];
};

export type ListingOwner = {
  id: string;
  fullName: string;
  phoneNumber?: string | null;
  store?: {
    id?: string;
    name?: string;
    slug?: string;
    logoImageUrl?: string | null;
  } | null;
};

export type ListingDetails = {
  id: string;
  title: string;
  description: string;
  price: number;
  currency: string;
  rentalPeriodUnit: RentalPeriodId;
  isVip: boolean;
  viewCount: number;
  favoriteCount: number;
  publishedAtUtc?: string | null;
  categoryName?: string | null;
  categoryId?: string | null;
  cityName?: string | null;
  districtName?: string | null;
  owner: ListingOwner;
  images: ListingImage[];
  fields: ListingField[];
};

export type SearchParams = {
  q?: string;
  category?: string;
  minPrice?: string;
  maxPrice?: string;
  sort?: string;
  page?: string;
};

export type City = { id: string; name: string; hasDistricts: boolean };
export type District = { id: string; name: string };

/** FieldType ids as the backend defines them. */
export const FieldType = {
  text: 1,
  wholeNumber: 2,
  fractionalNumber: 3,
  boolean: 4,
  singleSelect: 5,
  multiSelect: 6,
  date: 7,
} as const;

export type FieldOption = { id: string; value: string; label: string };

export type FieldDefinition = {
  id: string;
  label?: string;
  key?: string;
  type: number;
  isRequired: boolean;
  allowCustomValue: boolean;
  displayOrder: number;
  options?: FieldOption[];
};

/** The backend ListingStatus ids. */
export const Status = {
  draft: 1,
  pendingReview: 2,
  active: 3,
  rejected: 4,
  expired: 5,
  deactivated: 6,
  deleted: 7,
  paymentRequired: 8,
} as const;

export type OwnedSummary = {
  id: string;
  title: string;
  price: number;
  currency: string;
  rentalPeriodUnit: RentalPeriodId;
  status: number;
  imageCount: number;
  createdAtUtc: string;
  coverImageUrl?: string | null;
  publishedAtUtc?: string | null;
  expiresAtUtc?: string | null;
};

export type OwnedDetails = {
  id: string;
  categoryId: string;
  title: string;
  description: string;
  price: number;
  currency: string;
  rentalPeriodUnit: RentalPeriodId;
  status: number;
  rejectionReason?: string | null;
  publishedAtUtc?: string | null;
  expiresAtUtc?: string | null;
  cityId?: string | null;
  cityName?: string | null;
  districtId?: string | null;
  districtName?: string | null;
  images: ListingImage[];
  fields: ListingField[];
};

export type Wallet = { balance: number; currency: string };

export type WalletTransaction = {
  id: string;
  /** 1 = money in, 2 = money out. */
  direction: number;
  /** 1 top-up, 2 listing fee, 3 promotion fee, 4 refund, 5 bonus, 6 adjustment. */
  type: number;
  amount: number;
  balanceAfter: number;
  occurredAtUtc: string;
};

export type Conversation = {
  id: string;
  listingId: string;
  listingTitle: string;
  otherUserId: string;
  unreadCount: number;
  lastMessage?: string | null;
  lastMessageAtUtc?: string | null;
};

export type ChatImage = { id: string; url: string };

export type ChatMessage = {
  id: string;
  conversationId: string;
  senderId: string;
  body: string;
  sentAtUtc: string;
  readAtUtc?: string | null;
  images?: ChatImage[];
};

export type PublicStore = {
  id: string;
  name: string;
  slug: string;
  description: string;
  phoneNumber: string;
  email?: string | null;
  address?: string | null;
  instagramUrl?: string | null;
  tiktokUrl?: string | null;
  facebookUrl?: string | null;
  websiteUrl?: string | null;
  youtubeUrl?: string | null;
  /** International digits, for example 994501234567. */
  whatsAppNumber?: string | null;
  /** The first message the visitor's WhatsApp opens with. */
  whatsAppMessage?: string | null;
  /** Open days: day 1 Monday to 7 Sunday, the hours as minutes after midnight. */
  workingHours?: { day: number; openMinute: number; closeMinute: number }[] | null;
  followerCount?: number;
  logoImageUrl?: string | null;
  coverImageUrl?: string | null;
  activeListingCount?: number;
  totalViewCount?: number;
  totalFavoriteCount?: number;
};

export type StoreStats = {
  followers: number;
  activeListings: number;
  listingViews: number;
  listingFavorites: number;
  profileViews: number;
  calls: number;
  whatsApp: number;
  messages: number;
  profileViewsLast7Days: number;
  profileViewsLast30Days: number;
  contactsLast30Days: number;
  last30Days: { day: string; views: number; calls: number; whatsApp: number; messages: number }[];
};

/** What the owner of a live store is offered: the VIP place on the home page. */
export type StoreVipStatus = {
  name: string;
  price: number;
  durationDays: number;
  offerMessage: string;
  isOnSale: boolean;
  isVip: boolean;
  vipUntilUtc?: string | null;
  currency: string;
};

/** A store in a list (search, followed). */
export type StoreCard = {
  storeId: string;
  name: string;
  slug: string;
  description?: string | null;
  logoImageUrl?: string | null;
  activeListingCount?: number;
  followerCount?: number;
};

export type MyStore = PublicStore & {
  /** 1 draft, 2 in review, 3 active, 4 rejected, 5 suspended, 6 deleted. */
  status: number;
  rejectionReason?: string | null;
};

export const StoreStatus = {
  draft: 1,
  pendingReview: 2,
  active: 3,
  rejected: 4,
  suspended: 5,
  deleted: 6,
} as const;

export type FollowStatus = { isFollowing: boolean; followerCount: number };

export type AppNotification = {
  id: string;
  /** 1 listing approved, 2 rejected, 3 payment required, 4 store approved, 5 store rejected, 6 report updated, 7 support reply, 8 support status. */
  type: number;
  title: string;
  body: string;
  createdAtUtc: string;
  relatedEntityId?: string | null;
  actionUrl?: string | null;
  readAtUtc?: string | null;
};

export type SupportMessage = { id: string; body: string; isAdmin: boolean; sentAtUtc: string };

export type SupportTicket = {
  id: string;
  /** 1 general, 2 account, 3 listing, 4 payment, 5 store, 6 technical, 7 other. */
  category: number;
  subject: string;
  /** 1 open, 2 in progress, 3 resolved, 4 closed. */
  status: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  messages?: SupportMessage[];
};

/** A banner at the top of the home page. */
export type Banner = {
  id: string;
  title: string;
  subtitle?: string | null;
  imageUrl: string;
  linkUrl?: string | null;
};

/** A fixed page (terms, privacy, about …) in the visitor's language. */
export type PageSummary = { slug: string; title: string };

export type PageContent = { slug: string; title: string; body: string; updatedAtUtc: string };

/** The operator's details and contact channels. An empty one is never shown. */
export type SiteInfo = {
  operatorName?: string | null;
  voen?: string | null;
  address?: string | null;
  email?: string | null;
  phone?: string | null;
  hotline?: string | null;
  whatsApp?: string | null;
  instagram?: string | null;
  tikTok?: string | null;
  facebook?: string | null;
};
