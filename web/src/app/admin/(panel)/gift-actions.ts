"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

export type GiftDone = { ok: true; message: string } | { ok: false; unauthorized: boolean; message: string };

const known: [RegExp, string][] = [
  [/already exists for this listing/i, "Bu elan artıq VIP-dir."],
  [/only an unexpired active listing/i, "Yalnız aktiv və vaxtı bitməmiş elana hədiyyə etmək olar."],
  [/ends too soon/i, "Elanın bitməsinə çox az qalıb, VIP hədiyyə etmək mənasızdır."],
  [/only an active store/i, "Yalnız aktiv mağazaya hədiyyə etmək olar."],
  [/number of days/i, "Gün sayı 1 ilə 365 arasında olmalıdır."],
  [/was not found/i, "Tapılmadı."],
];

/** Only the owner (SuperAdmin) gives gifts; the API checks too. */
async function run(path: string, body: unknown, done: (title: string) => string): Promise<GiftDone> {
  if (!(await getRoles()).includes("SuperAdmin")) return { ok: false, unauthorized: true, message: "" };
  const res = await authed<{ title: string }>(path, { method: "POST", body });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  revalidatePath("/admin/elanlar");
  revalidatePath("/admin/magazalar");
  revalidatePath("/admin/maliyye/akt");
  return { ok: true, message: done(res.data.title) };
}

const GUID = /^[0-9a-f-]{36}$/i;

/** type: 1 = bump, 2 = VIP. */
export async function giftListing(listingId: string, type: 1 | 2, days: number | null): Promise<GiftDone> {
  if (!GUID.test(listingId)) return { ok: false, unauthorized: false, message: "Elan tapılmadı." };
  if (days !== null && (!Number.isInteger(days) || days < 1 || days > 365)) {
    return { ok: false, unauthorized: false, message: "Gün sayı 1 ilə 365 arasında tam ədəd olmalıdır." };
  }
  return run("/api/admin/gifts/listing-promotions", { listingId, type, days }, () =>
    type === 2 ? "VIP hədiyyə edildi. Elan sahibinə bildiriş getdi." : "İrəli çəkmə hədiyyə edildi. Elan sahibinə bildiriş getdi.",
  );
}

export async function giftStoreVip(storeId: string, days: number | null): Promise<GiftDone> {
  if (!GUID.test(storeId)) return { ok: false, unauthorized: false, message: "Mağaza tapılmadı." };
  if (days !== null && (!Number.isInteger(days) || days < 1 || days > 365)) {
    return { ok: false, unauthorized: false, message: "Gün sayı 1 ilə 365 arasında tam ədəd olmalıdır." };
  }
  return run("/api/admin/gifts/store-vip", { storeId, days }, () => "VIP mağaza hədiyyə edildi. Mağaza sahibinə bildiriş getdi.");
}

/** Every paid service of one person free (or back to normal). Only the owner; the API checks too. */
export async function setFreeServices(userId: string, services: number): Promise<GiftDone> {
  if (!GUID.test(userId)) return { ok: false, unauthorized: false, message: "İstifadəçi tapılmadı." };
  if (!Number.isInteger(services) || services < 0 || services > 15) return { ok: false, unauthorized: false, message: "Seçim yanlışdır." };
  if (!(await getRoles()).includes("SuperAdmin")) return { ok: false, unauthorized: true, message: "" };
  const res = await authed("/api/admin/gifts/free-services", { method: "PUT", body: { userId, services } });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  revalidatePath(`/admin/istifadeciler/${userId}`);
  return {
    ok: true,
    message:
      services === 0
        ? "Bütün pulsuz imkanlar söndürüldü. Adi qiymətlər tətbiq olunur."
        : "Seçimlər saxlanıldı. İstifadəçiyə bildiriş getdi.",
  };
}
