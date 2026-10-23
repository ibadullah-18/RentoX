"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { canSeeBranding } from "@/lib/session";

export type BrandSaved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

export type BrandFormInput = {
  siteName: string;
  footerText: string;
  useTextLogo: boolean;
  headerLogoHeight: number;
  footerLogoHeight: number;
  motionLevel: number;
  defaultTheme: number;
  description: string;
  announcementEnabled: boolean;
  announcementText: string;
  announcementLink: string;
  announcementTone: number;
};

const SLOTS = ["header-logo", "header-logo-dark", "footer-logo", "footer-logo-dark", "icon", "share-image"];

const known: [RegExp, string][] = [
  [/name of the site/i, "Saytın adı 1 ilə 40 simvol arasında olmalıdır."],
  [/footer text/i, "Alt yazı 200 simvoldan uzun ola bilməz."],
  [/description/i, "Təsvir 300 simvoldan uzun ola bilməz."],
  [/notice needs/i, "Elan xəbərdarlığı üçün mətn yazın."],
  [/notice cannot/i, "Elan xəbərdarlığı 200 simvoldan uzun ola bilməz."],
  [/link of the notice/i, "Keçid saytın içindən (/...) və ya http(s) ünvanı olmalıdır."],
  [/height of the logo/i, "Loqonun hündürlüyü 20 ilə 96 piksel arasında olmalıdır."],
];

/** The pages and the cached answers that carry the logo are dropped, so the change shows now. */
function done() {
  updateTag("branding");
  revalidatePath("/", "layout");
}

async function ownerOnly(): Promise<BrandSaved | null> {
  return (await canSeeBranding()) ? null : { ok: false, unauthorized: true, message: "" };
}

export async function saveBranding(input: BrandFormInput): Promise<BrandSaved> {
  const denied = await ownerOnly();
  if (denied) return denied;
  const name = input.siteName.trim();
  if (!name || name.length > 40) return { ok: false, unauthorized: false, message: "Saytın adı 1 ilə 40 simvol arasında olmalıdır." };
  for (const h of [input.headerLogoHeight, input.footerLogoHeight]) {
    if (!Number.isInteger(h) || h < 20 || h > 96) {
      return { ok: false, unauthorized: false, message: "Loqonun hündürlüyü 20 ilə 96 piksel arasında olmalıdır." };
    }
  }

  for (const v of [input.motionLevel, input.defaultTheme, input.announcementTone]) {
    if (!Number.isInteger(v) || v < 0 || v > 2) return { ok: false, unauthorized: false, message: "Seçim düzgün deyil." };
  }

  const res = await authed("/api/admin/branding", {
    method: "PUT",
    body: {
      siteName: name,
      footerText: input.footerText.trim() || null,
      useTextLogo: input.useTextLogo,
      headerLogoHeight: input.headerLogoHeight,
      footerLogoHeight: input.footerLogoHeight,
      motionLevel: input.motionLevel,
      defaultTheme: input.defaultTheme,
      description: input.description.trim() || null,
      announcementEnabled: input.announcementEnabled,
      announcementText: input.announcementText.trim() || null,
      announcementLink: input.announcementLink.trim() || null,
      announcementTone: input.announcementTone,
    },
  });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: known.find(([re]) => re.test(res.message))?.[1] ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  done();
  return { ok: true };
}

/** Takes the owner's picture away: the one that comes with the site is shown again. */
export async function resetBrandImage(slot: string): Promise<BrandSaved> {
  const denied = await ownerOnly();
  if (denied) return denied;
  if (!SLOTS.includes(slot)) return { ok: false, unauthorized: false, message: "Şəkil tapılmadı." };
  const res = await authed(`/api/admin/branding/${slot}/image`, { method: "DELETE" });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  done();
  return { ok: true };
}

/** After a picture was uploaded (by the browser, through the BFF): drop what is cached. */
export async function brandImageChanged(): Promise<void> {
  if (await ownerOnly()) return;
  done();
}
