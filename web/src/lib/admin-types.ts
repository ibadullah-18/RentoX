export type Activity = { metric: string; total: number; last7Days: number; last30Days: number };
export type StatusCount = { module: string; status: number; count: number };
export type WalletBalance = { currency: string; walletCount: number; totalBalance: number };
export type WalletActivity = {
  currency: string;
  direction: number;
  type: number;
  transactionCount: number;
  totalAmount: number;
  last7DaysAmount: number;
  last30DaysAmount: number;
};

export type TopListing = { id: string; title: string; status: number; viewCount: number; favoriteCount: number };

export type Dashboard = {
  generatedAtUtc: string;
  activity: Activity[];
  statusCounts: StatusCount[];
  walletBalances: WalletBalance[];
  walletActivity: WalletActivity[];
  mostViewedListings: TopListing[];
  mostFavoritedListings: TopListing[];
};

export type PendingListing = {
  id: string;
  ownerId: string;
  title: string;
  price: number;
  currency: string;
  rentalPeriodUnit: number;
  coverImageUrl?: string | null;
  imageCount: number;
  createdAtUtc: string;
};

export type ModerationListing = {
  id: string;
  ownerId: string;
  ownerFullName: string;
  phoneNumber: string;
  title: string;
  description: string;
  price: number;
  currency: string;
  rentalPeriodUnit: number;
  status: number;
  createdAtUtc: string;
  images: { id: string; url: string; displayOrder: number; isCover: boolean }[];
  fields: {
    label?: string;
    key?: string;
    type: number;
    textValue?: string | null;
    numericValue?: number | null;
    flagValue?: boolean | null;
    calendarValue?: string | null;
    customValue?: string | null;
    selections?: { label?: string; value?: string }[];
  }[];
};

export type AdminTicket = {
  id: string;
  userId: string;
  category: number;
  subject: string;
  priority: number;
  status: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  messages?: { id: string; senderId: string; isAdmin: boolean; body: string; sentAtUtc: string }[];
};

export type ReportSummary = {
  id: string;
  targetType: number;
  targetId: string;
  targetTitle?: string | null;
  reporterId: string;
  reason: number;
  status: number;
  createdAtUtc: string;
};

export type ReportDetails = ReportSummary & {
  targetOwnerId?: string | null;
  details?: string | null;
  reviewedAtUtc?: string | null;
  resolutionNote?: string | null;
  openReportsOnTarget: number;
};

export type ChatReportSummary = {
  id: string;
  conversationId: string;
  listingId: string;
  listingTitle: string;
  reporterId: string;
  otherUserId: string;
  reason: number;
  status: number;
  createdAtUtc: string;
};

export type ChatReportDetails = {
  report: {
    id: string;
    conversationId: string;
    reporterId: string;
    reason: number;
    details?: string | null;
    evidenceMessageId?: string | null;
    status: number;
    createdAtUtc: string;
    reviewedByUserId?: string | null;
    reviewedAtUtc?: string | null;
    resolutionNote?: string | null;
  };
  listingId: string;
  listingTitle: string;
  buyerId: string;
  sellerId: string;
  messages: {
    id: string;
    senderId: string;
    body: string;
    sentAtUtc: string;
    readAtUtc?: string | null;
    images: { id: string; url?: string }[];
  }[];
};

export type AuditItem = {
  id: string;
  operationId: string;
  actorUserId: string;
  targetId: string;
  action: number;
  previousValue?: number | null;
  currentValue?: number | null;
  relatedEntityId?: string | null;
  occurredAtUtc: string;
};

export type AuditPage = {
  items: AuditItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
