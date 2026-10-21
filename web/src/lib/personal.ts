import "server-only";
import { authed } from "./authed";
import { getLocale } from "./i18n/server";
import type {
  AppNotification,
  SupportTicket,
  ChatMessage,
  FollowStatus,
  MyStore,
  StoreStats,
  StoreVipStatus,
  Conversation,
  ListingSummary,
  OwnedDetails,
  OwnedSummary,
  Paged,
  Wallet,
  WalletTransaction,
} from "./types";

export type Account = {
  userId?: string;
  phoneNumber: string;
  fullName: string;
  bio?: string | null;
  /** The address of the profile photo; empty when the person has none. */
  profileImageUrl?: string | null;
};

export async function getAccount() {
  return authed<Account>("/api/account/me");
}

export async function getFavorites(page = 1, pageSize = 24) {
  return authed<Paged<ListingSummary>>("/api/favorites", {
    query: { language: await getLocale(), page, pageSize },
  });
}

/** Ids of everything the signed-in person has favourited (for the hearts). */
export async function getFavoriteIds(): Promise<string[]> {
  const ids: string[] = [];
  // The hearts only need the first pages; very long lists are cut.
  for (let page = 1; page <= 6; page++) {
    const res = await getFavorites(page, 50);
    if (!res.ok) break;
    ids.push(...res.data.items.map((i) => i.id));
    if (res.data.page >= res.data.totalPages) break;
  }
  return ids;
}

export function getMyListings(page = 1, pageSize = 20) {
  return authed<Paged<OwnedSummary>>("/api/listings/mine", {
    query: { page, pageSize },
  });
}

export async function getMyListing(id: string) {
  return authed<OwnedDetails>(`/api/listings/mine/${encodeURIComponent(id)}`, {
    query: { language: await getLocale() },
  });
}

export function getWallet() {
  return authed<Wallet>("/api/wallet");
}

export function getTransactions(page = 1, pageSize = 20) {
  return authed<Paged<WalletTransaction>>("/api/wallet/transactions", {
    query: { page, pageSize },
  });
}

export function getConversations(page = 1, pageSize = 50) {
  return authed<Paged<Conversation>>("/api/conversations", { query: { page, pageSize } });
}

/** Newest first; page 2 holds the older ones. */
export function getMessages(conversationId: string, page = 1, pageSize = 30) {
  return authed<Paged<ChatMessage>>(`/api/conversations/${encodeURIComponent(conversationId)}/messages`, {
    query: { page, pageSize },
  });
}

export async function getUnreadMessages(): Promise<number> {
  const res = await authed<{ count: number }>("/api/conversations/unread-count");
  return res.ok ? (res.data.count ?? 0) : 0;
}

/** The signed-in person's store; 404 (not ok) when they have none. */
export function getMyStore() {
  return authed<MyStore>("/api/stores/mine");
}

export function getMyStoreVip() {
  return authed<StoreVipStatus>("/api/stores/mine/vip");
}

export function getMyStoreStats() {
  return authed<StoreStats>("/api/stores/mine/stats");
}

export function getFollowStatus(storeId: string) {
  return authed<FollowStatus>(`/api/stores/${encodeURIComponent(storeId)}/follow-status`);
}

export function getNotifications(page = 1, pageSize = 20) {
  return authed<Paged<AppNotification>>("/api/notifications", { query: { page, pageSize } });
}

export async function getUnreadNotifications(): Promise<number> {
  const res = await authed<{ count: number }>("/api/notifications/unread-count");
  return res.ok ? (res.data.count ?? 0) : 0;
}

export function getTickets(page = 1, pageSize = 20) {
  return authed<Paged<SupportTicket>>("/api/support/tickets", { query: { page, pageSize } });
}

export function getTicket(id: string) {
  return authed<SupportTicket>(`/api/support/tickets/${encodeURIComponent(id)}`);
}
