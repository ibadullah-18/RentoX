"use server";

import { getT } from "@/lib/i18n/server";
import { authed } from "@/lib/authed";

export type ReportKind = "listing" | "store" | "chat";
export type ReportOutcome = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;
const DETAILS_MAX = 1000;

const path: Record<ReportKind, (id: string) => string> = {
  listing: (id) => `/api/listings/${id}/reports`,
  store: (id) => `/api/stores/${id}/reports`,
  chat: (id) => `/api/conversations/${id}/reports`,
};

// The reasons the API knows: a listing or store has 1–6 (6 = other), a conversation 1–5 (5 = other).
const OTHER: Record<ReportKind, number> = { listing: 6, store: 6, chat: 5 };

/** A person reports a listing, a store or a conversation. "Other" must be explained. */
export async function sendReport(
  kind: ReportKind,
  targetId: string,
  reason: number,
  details: string,
): Promise<ReportOutcome> {
  const t = await getT();
  const text = details.trim();
  const max = OTHER[kind];

  if (!ID.test(targetId) || !Number.isInteger(reason) || reason < 1 || reason > max) {
    return { ok: false, unauthorized: false, message: t("Göndərilmədi. Yenidən cəhd edin.") };
  }
  if (reason === max && !text) {
    return { ok: false, unauthorized: false, message: t("Zəhmət olmasa nə baş verdiyini yazın") };
  }
  if (text.length > DETAILS_MAX) {
    return { ok: false, unauthorized: false, message: t("Göndərilmədi. Yenidən cəhd edin.") };
  }

  const res = await authed(path[kind](targetId), {
    method: "POST",
    body: kind === "chat" ? { reason, details: text || null, evidenceMessageId: null } : { reason, details: text || null },
  });
  if (res.ok) return { ok: true };

  if (res.status === 401) return { ok: false, unauthorized: true, message: "" };
  // The API says a repeated report is refused; any other refusal is a plain failure.
  if (res.status === 400 && /already/i.test(res.message)) {
    return { ok: false, unauthorized: false, message: t("Bunu artıq bildirmisiniz, baxırıq.") };
  }
  return { ok: false, unauthorized: false, message: t("Göndərilmədi. Yenidən cəhd edin.") };
}
