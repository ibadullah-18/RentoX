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
