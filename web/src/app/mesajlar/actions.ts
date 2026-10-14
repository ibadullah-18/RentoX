"use server";

import { getT } from "@/lib/i18n/server";
import { authed } from "@/lib/authed";
import { getConversations, getMessages } from "@/lib/personal";
import type { ChatMessage, Conversation, Paged } from "@/lib/types";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type Fail = { ok: false; unauthorized: boolean; message: string };
export type Got<T> = ({ ok: true } & T) | Fail;

const ID = /^[0-9a-f-]{36}$/i;
const notFound = async (): Promise<Fail> => ({ ok: false, unauthorized: false, message: await tr("Söhbət tapılmadı.") });

async function fail(status: number): Promise<Fail> {
  if (status === 401) return { ok: false, unauthorized: true, message: "" };
  if (status === 429) {
    return { ok: false, unauthorized: false, message: await tr("Çox tez-tez mesaj göndərilir. Bir az gözləyin.") };
  }
  if (status === 403) {
    return { ok: false, unauthorized: false, message: await tr("Bu söhbətə mesaj göndərmək mümkün deyil.") };
  }
  if (status === 0) {
    return { ok: false, unauthorized: false, message: await tr("Serverə qoşulmaq olmadı.") };
  }
  return { ok: false, unauthorized: false, message: await tr("Əməliyyat alınmadı. Yenidən cəhd edin.") };
}

/** Starts (or continues) the conversation with a listing's owner by sending the first message. */
export async function startConversation(
  listingId: string,
  body: string,
): Promise<Got<{ conversationId: string }>> {
  const text = body.trim();
  if (!ID.test(listingId) || !text) {
    return { ok: false, unauthorized: false, message: await tr("Mesajı yazın.") };
  }
  const res = await authed<{ conversationId: string }>("/api/conversations", {
    method: "POST",
    body: { listingId, body: text.slice(0, 4000) },
  });
  return res.ok ? { ok: true, conversationId: res.data.conversationId } : fail(res.status);
}

export async function sendMessage(conversationId: string, body: string): Promise<Got<{ message: ChatMessage }>> {
  const text = body.trim();
  if (!ID.test(conversationId)) return notFound();
  if (!text) return { ok: false, unauthorized: false, message: await tr("Mesajı yazın.") };

  const res = await authed<ChatMessage>(`/api/conversations/${conversationId}/messages`, {
    method: "POST",
    body: { body: text.slice(0, 4000) },
  });
  return res.ok ? { ok: true, message: res.data } : fail(res.status);
}

export async function markRead(conversationId: string): Promise<void> {
  if (!ID.test(conversationId)) return;
  await authed(`/api/conversations/${conversationId}/read`, { method: "PATCH" });
}

export async function olderMessages(
  conversationId: string,
  page: number,
): Promise<Got<{ messages: Paged<ChatMessage> }>> {
  if (!ID.test(conversationId) || page < 2) return notFound();
  const res = await getMessages(conversationId, page);
  return res.ok ? { ok: true, messages: res.data } : fail(res.status);
}

export async function refreshInbox(): Promise<Got<{ conversations: Conversation[] }>> {
  const res = await getConversations(1, 50);
  return res.ok ? { ok: true, conversations: res.data.items } : fail(res.status);
}

export async function latestMessages(conversationId: string): Promise<Got<{ messages: ChatMessage[] }>> {
  if (!ID.test(conversationId)) return notFound();
  const res = await getMessages(conversationId, 1, 30);
  return res.ok ? { ok: true, messages: res.data.items } : fail(res.status);
}

export type Standing = { online: boolean; blockedByMe: boolean; canSend: boolean };

/** Whether the other person is online and whether sending is allowed. */
export async function standing(conversationId: string): Promise<Standing> {
  const none: Standing = { online: false, blockedByMe: false, canSend: true };
  if (!ID.test(conversationId)) return none;

  const [presence, block] = await Promise.all([
    authed<{ isOnline?: boolean }>(`/api/conversations/${conversationId}/presence`),
    authed<{ isBlockedByMe?: boolean; canSendMessages?: boolean }>(`/api/conversations/${conversationId}/block`),
  ]);
  return {
    online: presence.ok ? (presence.data.isOnline ?? false) : false,
    blockedByMe: block.ok ? (block.data.isBlockedByMe ?? false) : false,
    canSend: block.ok ? (block.data.canSendMessages ?? true) : true,
  };
}

export async function setBlock(conversationId: string, on: boolean): Promise<Got<object>> {
  if (!ID.test(conversationId)) return notFound();
  const res = await authed(`/api/conversations/${conversationId}/block`, { method: on ? "PUT" : "DELETE" });
  return res.ok ? { ok: true } : fail(res.status);
}
