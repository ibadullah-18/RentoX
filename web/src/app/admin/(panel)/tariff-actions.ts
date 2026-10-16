"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections } from "@/lib/session";

export type TariffSaved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

export type TariffInput = {
  freeActiveListingLimit: number;
  activationFee: number;
  bumpPrice: number;
  vipPrice: number;
  vipDurationDays: number;
};

const known: [RegExp, string][] = [
  [/free listing limit/i, "Pulsuz elan limiti 0 ilə 100 arasında olmalıdır."],
  [/vip duration/i, "VIP müddəti 1 ilə 90 gün arasında olmalıdır."],
  [/must be between 0 and 1000/i, "Qiymət 0 ilə 1000 arasında, ən çoxu iki onluq ilə olmalıdır."],
];

function money(n: number) {
  return Number.isFinite(n) && n >= 0 && n <= 1000 && Math.round(n * 100) / 100 === n;
}

/** Puts new prices in force. A finance Admin only; the API checks too. */
export async function saveTariff(input: TariffInput): Promise<TariffSaved> {
  const sections = await getSections();
  if (sections !== null && !sections.includes("finance")) return { ok: false, unauthorized: true, message: "" };

  if (!Number.isInteger(input.freeActiveListingLimit) || input.freeActiveListingLimit < 0 || input.freeActiveListingLimit > 100) {
    return { ok: false, unauthorized: false, message: "Pulsuz elan limiti 0 ilə 100 arasında tam ədəd olmalıdır." };
  }
  if (!Number.isInteger(input.vipDurationDays) || input.vipDurationDays < 1 || input.vipDurationDays > 90) {
    return { ok: false, unauthorized: false, message: "VIP müddəti 1 ilə 90 gün arasında tam ədəd olmalıdır." };
  }
  if (![input.activationFee, input.bumpPrice, input.vipPrice].every(money)) {
    return { ok: false, unauthorized: false, message: "Qiymətlər 0 ilə 1000 arasında, ən çoxu iki onluq ilə olmalıdır." };
  }

  const res = await authed("/api/admin/tariffs", { method: "PUT", body: input });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }

  // The site keeps the prices a few seconds: drop that copy so the change shows now.
  updateTag("tariffs");
  revalidatePath("/admin/tariflar");
  return { ok: true };
}

export type StoreVipInput = {
  name: string;
  price: number;
  durationDays: number;
  offerMessage: string;
  offerIntervalHours: number;
  isEnabled: boolean;
};

/** Puts a new version of the VIP store product in force. A finance Admin only; the API checks too. */
export async function saveStoreVip(input: StoreVipInput): Promise<TariffSaved> {
  const sections = await getSections();
  if (sections !== null && !sections.includes("finance")) return { ok: false, unauthorized: true, message: "" };

  if (!input.name.trim() || input.name.trim().length > 60) {
    return { ok: false, unauthorized: false, message: "Ad boş ola bilməz və 60 simvoldan çox olmamalıdır." };
  }
  if (!Number.isFinite(input.price) || input.price < 0 || input.price > 10000 || Math.round(input.price * 100) / 100 !== input.price) {
    return { ok: false, unauthorized: false, message: "Qiymət 0 ilə 10000 arasında, ən çoxu iki onluq ilə olmalıdır." };
  }
  if (!Number.isInteger(input.durationDays) || input.durationDays < 1 || input.durationDays > 365) {
    return { ok: false, unauthorized: false, message: "Müddət 1 ilə 365 gün arasında tam ədəd olmalıdır." };
  }
  if (!Number.isInteger(input.offerIntervalHours) || input.offerIntervalHours < 1 || input.offerIntervalHours > 2160) {
    return { ok: false, unauthorized: false, message: "Təklif aralığı 1 ilə 2160 saat arasında tam ədəd olmalıdır." };
  }
  if (input.offerMessage.length > 400) {
    return { ok: false, unauthorized: false, message: "Təklif mətni 400 simvoldan çox olmamalıdır." };
  }

  const res = await authed("/api/admin/store-vip", { method: "PUT", body: { ...input, name: input.name.trim(), offerMessage: input.offerMessage.trim() } });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }

  revalidatePath("/admin/tariflar");
  return { ok: true };
}

export type OffersSent = { ok: true; sent: number; skipped: number } | { ok: false; unauthorized: boolean; message: string };

/** Sends the VIP store offer to the owners of stores that are not VIP (each at most once per interval). */
export async function sendStoreVipOffers(): Promise<OffersSent> {
  const sections = await getSections();
  if (sections !== null && !sections.includes("finance")) return { ok: false, unauthorized: true, message: "" };

  const res = await authed<{ sent: number; skippedRecently: number }>("/api/admin/store-vip/offers", { method: "POST" });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return {
      ok: false,
      unauthorized: false,
      message: /turn the vip/i.test(res.message) ? "Əvvəlcə VIP mağaza məhsulunu aktiv edin." : "Əməliyyat alınmadı. Yenidən cəhd edin.",
    };
  }
  return { ok: true, sent: res.data.sent, skipped: res.data.skippedRecently };
}
