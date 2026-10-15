"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type ContentSaved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

export type PageFormInput = {
  titleAz: string;
  titleRu: string;
  titleEn: string;
  bodyAz: string;
  bodyRu: string;
  bodyEn: string;
};

export type SiteInfoInput = {
  operatorName: string;
  voen: string;
  address: string;
  email: string;
  phone: string;
  hotline: string;
  whatsApp: string;
  instagram: string;
  tikTok: string;
  facebook: string;
};

const SLUGS = ["terms", "privacy", "about", "cooperation", "rules", "faq"];

const known: [RegExp, string][] = [
  [/mark \{\{(\w+)\}\} is unknown/i, "Mətndə tanınmayan işarə var. İcazə verilənlər: {{operatorName}}, {{voen}}, {{address}}, {{email}}, {{phone}}, {{hotline}}."],
  [/page title is required/i, "Başlığı üç dildə də yazın."],
  [/page text is required/i, "Mətni üç dildə də yazın."],
  [/title cannot exceed/i, "Başlıq 150 simvoldan uzun ola bilməz."],
  [/text cannot exceed/i, "Mətn çox uzundur (ən çoxu 60 000 simvol)."],
  [/e-mail address is not valid/i, "E-poçt ünvanı düzgün deyil."],
  [/(phone|hotline) number is not valid/i, "Telefon nömrəsi düzgün deyil (yalnız rəqəm, +, boşluq, tire, mötərizə)."],
  [/whatsapp number is not valid/i, "WhatsApp nömrəsini ölkə kodu ilə yazın, məsələn +994501234567."],
  [/(instagram|tiktok|facebook) link must be/i, "Link həmin şəbəkənin öz ünvanı olmalıdır (məsələn instagram.com/hesab) və ya sadəcə hesabın adı."],
  [/(instagram|tiktok|facebook) must be a profile name/i, "Hesabın adını (hərf, rəqəm, nöqtə, _ və -) və ya linki yazın."],
  [/cannot exceed \d+ characters/i, "Bir sahə çox uzundur."],
];

async function allowed(): Promise<ContentSaved | null> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const sections = await getSections();
  return sections === null || sections.includes("catalog") ? null : { ok: false, unauthorized: true, message: "" };
}

function fail(status: number, message: string): ContentSaved {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
  return {
    ok: false,
    unauthorized: false,
    message: known.find(([re]) => re.test(message))?.[1] ?? "Əməliyyat alınmadı. Yenidən cəhd edin.",
  };
}

function done() {
  // The site keeps the pages for a short time: drop that copy so the change shows now.
  updateTag("pages");
  revalidatePath("/admin/sehifeler");
  revalidatePath("/admin/elaqe");
}

export async function savePage(slug: string, input: PageFormInput): Promise<ContentSaved> {
  const denied = await allowed();
  if (denied) return denied;
  if (!SLUGS.includes(slug)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  if (!input.titleAz.trim() || !input.titleRu.trim() || !input.titleEn.trim()) {
    return { ok: false, unauthorized: false, message: "Başlığı üç dildə də yazın." };
  }
  if (!input.bodyAz.trim() || !input.bodyRu.trim() || !input.bodyEn.trim()) {
    return { ok: false, unauthorized: false, message: "Mətni üç dildə də yazın." };
  }

  const res = await authed(`/api/admin/pages/${slug}`, { method: "PUT", body: input });
  if (!res.ok) return fail(res.status, res.message);
  done();
  return { ok: true };
}

export async function saveSiteInfo(input: SiteInfoInput): Promise<ContentSaved> {
  const denied = await allowed();
  if (denied) return denied;

  const clean = Object.fromEntries(
    Object.entries(input).map(([key, value]) => [key, value.trim() === "" ? null : value.trim()]),
  );
  const res = await authed("/api/admin/site-info", { method: "PUT", body: clean });
  if (!res.ok) return fail(res.status, res.message);
  done();
  return { ok: true };
}
