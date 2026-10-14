"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { isValidPhone } from "@/lib/phone";
import { getRoles } from "@/lib/session";

export type StaffDone = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;

const known: [RegExp, string][] = [
  [/user was not found/i, "Bu nömrə ilə aktiv hesab tapılmadı. Şəxs əvvəl sayta qeydiyyatdan keçməlidir."],
  [/already has admin/i, "Bu şəxsin artıq admin icazəsi var."],
  [/yourself/i, "Öz icazənizi geri ala bilməzsiniz."],
  [/superadmin cannot/i, "SuperAdmin burada dəyişdirilə bilməz."],
  [/not an admin/i, "Bu şəxs admin deyil."],
];

function fail(status: number, message: string): StaffDone {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
  const text = known.find(([re]) => re.test(message))?.[1];
  return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
}

/** Only a SuperAdmin: the API checks too, this just avoids a pointless call. */
async function guard(): Promise<StaffDone | null> {
  return (await getRoles()).includes("SuperAdmin") ? null : { ok: false, unauthorized: true, message: "" };
}

export async function grantAdmin(phoneNational: string): Promise<StaffDone> {
  const denied = await guard();
  if (denied) return denied;
  if (!isValidPhone(phoneNational)) {
    return { ok: false, unauthorized: false, message: "Nömrəni tam yazın: 9 rəqəm." };
  }
  const digits = phoneNational.replace(/\D/g, "").slice(-9);
  const res = await authed("/api/admin/staff", { method: "POST", body: { phoneNumber: `+994${digits}` } });
  revalidatePath("/admin/isciler");
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export async function setSections(userId: string, sections: string[]): Promise<StaffDone> {
  const denied = await guard();
  if (denied) return denied;
  if (!ID.test(userId)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  const res = await authed(`/api/admin/staff/${userId}/sections`, { method: "PUT", body: { sections } });
  revalidatePath("/admin/isciler");
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export async function revokeAdmin(userId: string): Promise<StaffDone> {
  const denied = await guard();
  if (denied) return denied;
  if (!ID.test(userId)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  const res = await authed(`/api/admin/staff/${userId}`, { method: "DELETE" });
  revalidatePath("/admin/isciler");
  return res.ok ? { ok: true } : fail(res.status, res.message);
}
