"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { authed } from "@/lib/authed";
import { getT } from "@/lib/i18n/server";
import { LOCALE_COOKIE, type Locale } from "@/lib/i18n/locales";
import { cookies } from "next/headers";
import { clearSession } from "@/lib/session";

export type Saved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

/** The account's language as the API numbers it. */
const LANGUAGE_ID: Record<Locale, number> = { az: 1, ru: 2, en: 3 };

export async function updateProfile(input: { fullName: string; bio: string; language: Locale }): Promise<Saved> {
  const t = await getT();
  const fullName = input.fullName.trim();
  const bio = input.bio.trim();
  if (fullName.length < 2 || fullName.length > 100) {
    return { ok: false, unauthorized: false, message: t("Adınızı yazın (2–100 hərf).") };
  }
  if (bio.length > 500) {
    return { ok: false, unauthorized: false, message: t("Haqqınızda ən çoxu {n} simvol yazın.", { n: 500 }) };
  }

  const res = await authed("/api/account/me", {
    method: "PUT",
    body: { fullName, bio: bio || null, preferredLanguage: LANGUAGE_ID[input.language] ?? 1 },
  });
  if (!res.ok) {
    if (res.status === 401) return { ok: false, unauthorized: true, message: "" };
    return {
      ok: false,
      unauthorized: false,
      message: res.status === 0 ? t("Serverə qoşulmaq olmadı.") : t("Əməliyyat alınmadı. Yenidən cəhd edin."),
    };
  }

  // The site speaks the language the person chose for the account.
  (await cookies()).set(LOCALE_COOKIE, input.language, { path: "/", maxAge: 60 * 60 * 24 * 365, sameSite: "lax" });
  revalidatePath("/kabinet");
  return { ok: true };
}

/** Deletes the account for good (the API anonymises it) and signs out. */
export async function deleteAccount(): Promise<Saved> {
  const t = await getT();
  const res = await authed("/api/account/me", { method: "DELETE", body: { confirm: true } });
  if (!res.ok && res.status !== 404) {
    if (res.status === 401) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: t("Əməliyyat alınmadı. Yenidən cəhd edin.") };
  }
  await clearSession();
  redirect("/");
}

/** Turns the general messages from RentoX (news, notices to everyone) on or off. Personal ones always come. */
export async function setAnnouncements(enabled: boolean): Promise<Saved> {
  const t = await getT();
  const res = await authed("/api/account/notification-settings", {
    method: "PUT",
    body: { announcementsEnabled: enabled },
  });
  if (!res.ok) {
    if (res.status === 401) return { ok: false, unauthorized: true, message: "" };
    return {
      ok: false,
      unauthorized: false,
      message: res.status === 0 ? t("Serverə qoşulmaq olmadı.") : t("Əməliyyat alınmadı. Yenidən cəhd edin."),
    };
  }
  return { ok: true };
}
