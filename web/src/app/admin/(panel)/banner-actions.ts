"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type BannerSaved =
  | { ok: true; id?: string }
  | { ok: false; unauthorized: boolean; message: string };

export type BannerFormInput = {
  titleAz: string;
  titleRu: string;
  titleEn: string;
  subtitleAz: string;
  subtitleRu: string;
  subtitleEn: string;
  linkUrl: string;
  displayOrder: number;
  isActive: boolean;
  /** ISO UTC time, or empty. */
  startsAtUtc: string;
  endsAtUtc: string;
  /** The name of the campaign page (rentox.az/name); empty: no page. */
  pageSlug: string;
  pageBodyAz: string;
  pageBodyRu: string;
  pageBodyEn: string;
  pageButtonLabelAz: string;
  pageButtonLabelRu: string;
  pageButtonLabelEn: string;
  pageButtonUrl: string;
  openInNewTab: boolean;
  /** 1 website, 2 apps, 3 both. */
  platform: number;
  showText: boolean;
  /** 0 left, 1 centre, 2 right. */
  textAlign: number;
  darkOverlay: boolean;
  /** 0 light text, 1 dark text. */
  textTone: number;
};

const ID = /^[0-9a-f-]{36}$/i;

const known: [RegExp, string][] = [
  [/title is required/i, "Başlığı üç dildə də yazın."],
  [/title cannot exceed/i, "Başlıq 100 simvoldan uzun ola bilməz."],
  [/subtitle cannot exceed/i, "Alt yazı 200 simvoldan uzun ola bilməz."],
  [/link must be/i, "Keçid saytın daxilində (/destek) və ya tam ünvan (https://…) olmalıdır."],
  [/link cannot exceed/i, "Keçid çox uzundur."],
  [/end of the banner/i, "Bitmə vaxtı başlama vaxtından sonra olmalıdır."],
  [/display order/i, "Sıra 0 ilə 10000 arasında olmalıdır."],
  [/more than \d+ banners/i, "Bannerlərin sayı həddə çatıb. Köhnələri silin."],
  [/banner was not found/i, "Banner tapılmadı."],
  [/already has this page name/i, "Bu səhifə adı başqa kampaniyada işlədilir. Başqa ad seçin."],
  [/reserved by the site/i, "Bu ad saytın öz səhifəsinə məxsusdur. Başqa ad seçin."],
  [/name of the page must/i, "Səhifənin adı 2 ilə 60 simvol arasında olmalıdır."],
  [/name of the page can have only/i, "Səhifənin adında yalnız a-z hərfləri, rəqəmlər və tək-tək defis ola bilər."],
  [/page text/i, "Səhifənin mətni 6000 simvoldan uzun ola bilməz."],
  [/button/i, "Düymənin yazısı üç dildə yazılmalı və 40 simvoldan uzun olmamalıdır."],
];

async function allowed(): Promise<BannerSaved | null> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const sections = await getSections();
  return sections === null || sections.includes("catalog") ? null : { ok: false, unauthorized: true, message: "" };
}

function fail(status: number, message: string): BannerSaved {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
  return {
    ok: false,
    unauthorized: false,
    message: known.find(([re]) => re.test(message))?.[1] ?? "Əməliyyat alınmadı. Yenidən cəhd edin.",
  };
}

function body(i: BannerFormInput) {
  return {
    titleAz: i.titleAz.trim(),
    titleRu: i.titleRu.trim(),
    titleEn: i.titleEn.trim(),
    subtitleAz: i.subtitleAz.trim() || null,
    subtitleRu: i.subtitleRu.trim() || null,
    subtitleEn: i.subtitleEn.trim() || null,
    linkUrl: i.linkUrl.trim() || null,
    displayOrder: i.displayOrder,
    isActive: i.isActive,
    startsAtUtc: i.startsAtUtc || null,
    endsAtUtc: i.endsAtUtc || null,
    pageSlug: i.pageSlug.trim() || null,
    pageBodyAz: i.pageBodyAz.trim() || null,
    pageBodyRu: i.pageBodyRu.trim() || null,
    pageBodyEn: i.pageBodyEn.trim() || null,
    pageButtonLabelAz: i.pageButtonLabelAz.trim() || null,
    pageButtonLabelRu: i.pageButtonLabelRu.trim() || null,
    pageButtonLabelEn: i.pageButtonLabelEn.trim() || null,
    pageButtonUrl: i.pageButtonUrl.trim() || null,
    openInNewTab: i.openInNewTab,
    platform: i.platform,
    showText: i.showText,
    textAlign: i.textAlign,
    darkOverlay: i.darkOverlay,
    textTone: i.textTone,
  };
}

function check(i: BannerFormInput): string | null {
  if (!i.titleAz.trim() || !i.titleRu.trim() || !i.titleEn.trim()) return "Başlığı üç dildə də yazın.";
  if (!Number.isInteger(i.displayOrder) || i.displayOrder < 0 || i.displayOrder > 10000) return "Sıra 0 ilə 10000 arasında tam ədəd olmalıdır.";
  if (![1, 2, 3].includes(i.platform)) return "Harada göstərilməsini seçin.";
  if (![0, 1, 2].includes(i.textAlign) || ![0, 1].includes(i.textTone)) return "Yazının görünüşü yanlışdır.";
  return null;
}

function done() {
  // The site and the apps keep the list for a short time: drop that copy so the change shows now.
  updateTag("banners");
  revalidatePath("/admin/bannerler");
  // A campaign page may have changed or gone: the cached copies of the pages go too.
  revalidatePath("/", "layout");
}

export async function createBanner(input: BannerFormInput): Promise<BannerSaved> {
  const denied = await allowed();
  if (denied) return denied;
  const problem = check(input);
  if (problem) return { ok: false, unauthorized: false, message: problem };

  const res = await authed<{ id: string }>("/api/admin/banners", { method: "POST", body: body(input) });
  if (!res.ok) return fail(res.status, res.message);
  done();
  return { ok: true, id: res.data.id };
}

export async function updateBanner(id: string, input: BannerFormInput): Promise<BannerSaved> {
  const denied = await allowed();
  if (denied) return denied;
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  const problem = check(input);
  if (problem) return { ok: false, unauthorized: false, message: problem };

  const res = await authed(`/api/admin/banners/${id}`, { method: "PUT", body: body(input) });
  if (!res.ok) return fail(res.status, res.message);
  done();
  return { ok: true };
}

export async function deleteBanner(id: string): Promise<BannerSaved> {
  const denied = await allowed();
  if (denied) return denied;
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };

  const res = await authed(`/api/admin/banners/${id}`, { method: "DELETE" });
  if (!res.ok && res.status !== 404) return fail(res.status, res.message);
  done();
  return { ok: true };
}

/** After a new picture was uploaded (by the browser, through the BFF): refresh what is cached. */
export async function bannerImageChanged(): Promise<void> {
  if (await allowed()) return;
  done();
}
