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
  };
}

function check(i: BannerFormInput): string | null {
  if (!i.titleAz.trim() || !i.titleRu.trim() || !i.titleEn.trim()) return "Başlığı üç dildə də yazın.";
  if (!Number.isInteger(i.displayOrder) || i.displayOrder < 0 || i.displayOrder > 10000) return "Sıra 0 ilə 10000 arasında tam ədəd olmalıdır.";
  return null;
}

function done() {
  // The site and the apps keep the list for a short time: drop that copy so the change shows now.
  updateTag("banners");
  revalidatePath("/admin/bannerler");
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
