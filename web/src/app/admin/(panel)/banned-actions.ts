"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type BannedDone = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const known: [RegExp, string][] = [
  [/already in the list/i, "Bu söz artıq siyahıdadır."],
  [/banned word must be/i, "Söz 2 ilə 50 simvol arasında olmalıdır."],
  [/only one .* is allowed/i, "Ulduz (*) yalnız sözün sonunda və bir dənə ola bilər."],
  [/cannot hold more/i, "Siyahı doludur (ən çoxu 500 söz). Lazımsızları silin."],
];

async function allowed(): Promise<BannedDone | null> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const sections = await getSections();
  return sections === null || sections.includes("listings") ? null : { ok: false, unauthorized: true, message: "" };
}

export async function addBannedWord(word: string): Promise<BannedDone> {
  const denied = await allowed();
  if (denied) return denied;
  const text = word.trim();
  if (text.length < 2) return { ok: false, unauthorized: false, message: "Söz ən azı 2 simvol olmalıdır." };

  const res = await authed("/api/admin/banned-words", { method: "POST", body: { word: text } });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return {
      ok: false,
      unauthorized: false,
      message: known.find(([re]) => re.test(res.message))?.[1] ?? "Əməliyyat alınmadı. Yenidən cəhd edin.",
    };
  }
  revalidatePath("/admin/yasaq-sozler");
  return { ok: true };
}

export async function removeBannedWord(id: string): Promise<BannedDone> {
  const denied = await allowed();
  if (denied) return denied;
  if (!/^[0-9a-f-]{36}$/i.test(id)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };

  const res = await authed(`/api/admin/banned-words/${id}`, { method: "DELETE" });
  if (!res.ok && res.status !== 404) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  revalidatePath("/admin/yasaq-sozler");
  return { ok: true };
}
