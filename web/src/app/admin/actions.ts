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

function fail(status: number): Done {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
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
  return res.ok ? { ok: true } : fail(res.status);
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

/** status: 2 reviewing, 3 resolved, 4 rejected. */
export async function reviewReport(id: string, status: number, note: string): Promise<Done> {
  if (!ID.test(id) || ![2, 3, 4].includes(status)) return bad;
  return act(`/api/admin/content-reports/${id}`, "PUT", { status, resolutionNote: note.trim() || null }, [
    `/admin/sikayetler/${id}`,
    "/admin/sikayetler",
  ]);
}
