"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections } from "@/lib/session";

export type SettingsSaved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

export type SettingsInput = {
  listingLifetimeDays: number;
  maxListingPhotos: number;
  minListingPhotos: number;
  maxNewListingsPerDay: number;
};

const known: [RegExp, string][] = [
  [/listing lifetime must be between/i, "Müddət 7 ilə 365 gün arasında olmalıdır."],
  [/least number of photos/i, "Ən az şəkil sayı ən çox şəkil sayından böyük ola bilməz."],
  [/daily limit/i, "Gündəlik elan limiti 0 ilə 200 arasında olmalıdır."],
  [/number of photos on a listing/i, "Şəkil sayı 1 ilə 30 arasında olmalıdır."],
];

/** Puts new settings in force. A listings-section Admin only; the API checks too. */
export async function saveSettings(input: SettingsInput): Promise<SettingsSaved> {
  const sections = await getSections();
  if (sections !== null && !sections.includes("listings")) return { ok: false, unauthorized: true, message: "" };

  const days = input.listingLifetimeDays;
  if (!Number.isInteger(days) || days < 7 || days > 365) {
    return { ok: false, unauthorized: false, message: "Müddət 7 ilə 365 gün arasında tam ədəd olmalıdır." };
  }

  const photos = input.maxListingPhotos;
  if (!Number.isInteger(photos) || photos < 1 || photos > 30) {
    return { ok: false, unauthorized: false, message: "Şəkil sayı 1 ilə 30 arasında tam ədəd olmalıdır." };
  }

  const least = input.minListingPhotos;
  if (!Number.isInteger(least) || least < 0 || least > photos) {
    return { ok: false, unauthorized: false, message: "Ən az şəkil sayı 0 ilə ən çox şəkil sayı arasında olmalıdır." };
  }

  const perDay = input.maxNewListingsPerDay;
  if (!Number.isInteger(perDay) || perDay < 0 || perDay > 200) {
    return { ok: false, unauthorized: false, message: "Gündəlik elan limiti 0 ilə 200 arasında tam ədəd olmalıdır." };
  }

  const res = await authed("/api/admin/settings", {
    method: "PUT",
    body: { listingLifetimeDays: days, maxListingPhotos: photos, minListingPhotos: least, maxNewListingsPerDay: perDay },
  });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }

  // The site keeps the value for a few seconds: drop that copy so the change shows now.
  updateTag("settings");
  revalidatePath("/admin/ayarlar");
  return { ok: true };
}
