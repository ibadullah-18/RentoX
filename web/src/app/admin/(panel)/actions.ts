"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { isAdmin } from "@/lib/session";

export type Done = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;

/** Who may call these: the API checks the token, this just avoids pointless calls. */
async function allowed(): Promise<Done | null> {
  return (await isAdmin()) ? null : { ok: false, unauthorized: true, message: "" };
}

/** The API's explanations (English) that an admin should read in Azerbaijani. */
const known: [RegExp, string][] = [
  [/resolution note is required/i, "Qərarı bağlamaq üçün qeyd yazın."],
  [/completed report cannot be changed/i, "Bağlanmış şikayət artıq dəyişdirilə bilməz."],
  [/not found|does not exist/i, "Qeyd tapılmadı (silinmiş ola bilər)."],
  [/reject.*reason|reason.*required/i, "Rədd səbəbini yazın."],
  [/transition|cannot be (approved|rejected)|not pending|invalid status/i, "Bu status üçün bu əməliyyat mümkün deyil."],
  [/staff accounts cannot be blocked/i, "İşçi (admin) hesabı bloklana bilməz. Əvvəl admin icazəsini geri alın."],
  [/cannot block yourself/i, "Özünüzü bloklaya bilməzsiniz."],
  [/only active or suspended accounts can be blocked/i, "Bu hesab artıq bloklanıb və ya silinib."],
  [/only blocked or suspended accounts can be unblocked/i, "Bu hesab bloklanmayıb."],
  [/reason is required to block/i, "Blok səbəbini yazın."],
];

function fail(status: number, message = ""): Done {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
  const text = known.find(([re]) => re.test(message))?.[1];
  if (text) return { ok: false, unauthorized: false, message: text };
  if (status === 0) return { ok: false, unauthorized: false, message: "Serverə qoşulmaq olmadı." };
  return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yenidən cəhd edin." };
}

async function act(
  path: string,
  method: "POST" | "PUT" | "PATCH",
  body: unknown,
  paths: string[],
): Promise<Done> {
  const denied = await allowed();
  if (denied) return denied;
  const res = await authed(path, { method, body });
  for (const p of paths) revalidatePath(p);
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

const bad: Done = { ok: false, unauthorized: false, message: "Yanlış sorğu." };

export async function approveListing(id: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  return act(`/api/admin/listings/${id}/approve`, "POST", undefined, ["/admin/elanlar"]);
}

export async function rejectListing(id: string, reason: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  const text = reason.trim();
  if (!text) return { ok: false, unauthorized: false, message: "Rədd səbəbini yazın." };
  return act(`/api/admin/listings/${id}/reject`, "POST", { reason: text.slice(0, 1000) }, ["/admin/elanlar"]);
}

export type BulkDone =
  | { ok: true; done: number; failed: { id: string; message: string }[] }
  | { ok: false; unauthorized: boolean; message: string };

const BULK_LIMIT = 50;

/** Approves or rejects several waiting listings, one by one; one failing does not stop the rest. */
export async function bulkReviewListings(
  ids: string[],
  decision: "approve" | "reject",
  reason: string,
): Promise<BulkDone> {
  const denied = await allowed();
  if (denied && !denied.ok) return { ok: false, unauthorized: denied.unauthorized, message: denied.message };
  const unique = [...new Set(ids)];
  if (unique.length === 0 || unique.length > BULK_LIMIT || !unique.every((id) => ID.test(id))) {
    return { ok: false, unauthorized: false, message: `Bir dəfəyə ən çox ${BULK_LIMIT} elan seçilə bilər.` };
  }
  const text = reason.trim();
  if (decision === "reject" && !text) return { ok: false, unauthorized: false, message: "Rədd səbəbini yazın." };

  let done = 0;
  const failed: { id: string; message: string }[] = [];
  for (const id of unique) {
    const res =
      decision === "approve"
        ? await authed(`/api/admin/listings/${id}/approve`, { method: "POST" })
        : await authed(`/api/admin/listings/${id}/reject`, { method: "POST", body: { reason: text.slice(0, 1000) } });
    if (res.ok) {
      done++;
      continue;
    }
    // The session ended or the permission is gone: nothing else will work either.
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const failure = fail(res.status, res.message);
    failed.push({ id, message: failure.ok ? "" : failure.message });
  }
  revalidatePath("/admin/elanlar");
  return { ok: true, done, failed };
}

/** Takes a listing that is already live (paused, pending or unpaid too) down; the owner is told the reason. */
export async function takeDownListing(id: string, reason: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  const text = reason.trim();
  if (!text) return { ok: false, unauthorized: false, message: "Dayandırma səbəbini yazın." };
  return act(`/api/admin/listings/${id}/take-down`, "POST", { reason: text.slice(0, 1000) }, [
    "/admin/elanlar",
    `/admin/elanlar/${id}`,
  ]);
}

export async function approveStore(id: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  return act(`/api/admin/stores/${id}/approve`, "POST", undefined, ["/admin/magazalar"]);
}

export async function rejectStore(id: string, reason: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  const text = reason.trim();
  if (!text) return { ok: false, unauthorized: false, message: "Rədd səbəbini yazın." };
  return act(`/api/admin/stores/${id}/reject`, "POST", { reason: text.slice(0, 1000) }, ["/admin/magazalar"]);
}

/** Suspends a live store; the owner is told the reason. */
export async function suspendStore(id: string, reason: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  const text = reason.trim();
  if (!text) return { ok: false, unauthorized: false, message: "Dayandırma səbəbini yazın." };
  return act(`/api/admin/stores/${id}/suspend`, "POST", { reason: text.slice(0, 1000) }, [
    "/admin/magazalar",
    `/admin/magazalar/${id}`,
  ]);
}

export async function reinstateStore(id: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  return act(`/api/admin/stores/${id}/reinstate`, "POST", undefined, ["/admin/magazalar", `/admin/magazalar/${id}`]);
}

/** Blocks an account: sign-in stops, sessions end, listings come down, the store is suspended. */
export async function blockUser(id: string, reason: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  const text = reason.trim();
  if (!text) return { ok: false, unauthorized: false, message: "Blok səbəbini yazın." };
  return act(`/api/admin/users/${id}/block`, "POST", { reason: text.slice(0, 1000) }, [
    "/admin/istifadeciler",
    `/admin/istifadeciler/${id}`,
  ]);
}

export async function unblockUser(id: string): Promise<Done> {
  if (!ID.test(id)) return bad;
  return act(`/api/admin/users/${id}/unblock`, "POST", undefined, ["/admin/istifadeciler", `/admin/istifadeciler/${id}`]);
}

export async function replyAsAdmin(ticketId: string, body: string): Promise<Done> {
  if (!ID.test(ticketId)) return bad;
  const text = body.trim();
  if (!text || text.length > 4000) return { ok: false, unauthorized: false, message: "Mesajı yazın." };
  return act(`/api/admin/support/tickets/${ticketId}/messages`, "POST", { body: text }, [
    `/admin/destek/${ticketId}`,
    "/admin/destek",
  ]);
}

export async function setTicketStatus(ticketId: string, status: number): Promise<Done> {
  if (!ID.test(ticketId) || ![1, 2, 3, 4].includes(status)) return bad;
  return act(`/api/admin/support/tickets/${ticketId}/status`, "PATCH", { status }, [
    `/admin/destek/${ticketId}`,
    "/admin/destek",
  ]);
}

export async function setTicketPriority(ticketId: string, priority: number): Promise<Done> {
  if (!ID.test(ticketId) || ![1, 2, 3, 4].includes(priority)) return bad;
  return act(`/api/admin/support/tickets/${ticketId}/priority`, "PATCH", { priority }, [
    `/admin/destek/${ticketId}`,
  ]);
}

