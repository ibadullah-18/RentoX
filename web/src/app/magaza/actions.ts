"use server";

import { getT } from "@/lib/i18n/server";
import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { storeBody, validateStoreForm, type StoreFormValues } from "@/lib/store-form";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type Saved =
  | { ok: true }
  | { ok: false; unauthorized: boolean; message: string };

async function fail(status: number): Promise<Saved> {
  if (status === 401) return { ok: false, unauthorized: true, message: "" };
  if (status === 409) {
    return { ok: false, unauthorized: false, message: await tr("Sizin artıq mağazanız var və ya bu ad tutulub.") };
  }
  if (status === 429) {
    return { ok: false, unauthorized: false, message: await tr("Çox tez-tez cəhd olunub. Bir az gözləyin.") };
  }
  if (status === 0) return { ok: false, unauthorized: false, message: await tr("Serverə qoşulmaq olmadı.") };
  return { ok: false, unauthorized: false, message: await tr("Əməliyyat alınmadı. Yenidən cəhd edin.") };
}

const invalid = async (): Promise<Saved> => ({ ok: false, unauthorized: false, message: await tr("Xanaları yoxlayın.") });

export async function createStore(form: StoreFormValues): Promise<Saved> {
  if (Object.keys(validateStoreForm(form, await getT())).length > 0) return invalid();
  const res = await authed("/api/stores", { method: "POST", body: storeBody(form) });
  revalidatePath("/magaza");
  revalidatePath("/kabinet");
  return res.ok ? { ok: true } : fail(res.status);
}

export async function updateStore(form: StoreFormValues): Promise<Saved> {
  if (Object.keys(validateStoreForm(form, await getT())).length > 0) return invalid();
  const res = await authed("/api/stores/mine", { method: "PUT", body: storeBody(form) });
  revalidatePath("/magaza");
  return res.ok ? { ok: true } : fail(res.status);
}

export type ContactInput = {
  phone: string;
  email: string;
  address: string;
  whatsApp: string;
  whatsAppMessage: string;
  instagram: string;
  tiktok: string;
  youtube: string;
  facebook: string;
  website: string;
};

/** The contact side (phone, networks, WhatsApp, hours): changeable at any time, a live store stays live. */
export async function updateStoreContact(
  input: ContactInput,
  hours: { day: number; openMinute: number; closeMinute: number }[],
): Promise<Saved> {
  const opt = (v: string) => (v.trim() ? v.trim() : null);
  if (input.phone.trim().length < 7) return { ok: false, unauthorized: false, message: await tr("Telefonu yazın.") };

  const res = await authed("/api/stores/mine/contact", {
    method: "PUT",
    body: {
      phoneNumber: input.phone.trim(),
      email: opt(input.email),
      address: opt(input.address),
      instagramUrl: opt(input.instagram),
      tiktokUrl: opt(input.tiktok),
      facebookUrl: opt(input.facebook),
      youtubeUrl: opt(input.youtube),
      websiteUrl: opt(input.website),
      whatsAppNumber: opt(input.whatsApp),
      whatsAppMessage: opt(input.whatsAppMessage),
      workingHours: hours,
    },
  });
  revalidatePath("/magaza");
  if (res.ok) return { ok: true };
  if (res.status === 400 && /whatsapp/i.test(res.message)) {
    return { ok: false, unauthorized: false, message: await tr("WhatsApp nömrəsi düzgün deyil.") };
  }
  return fail(res.status);
}

/** Sends the store for review (it needs a logo first). */
export async function submitStore(): Promise<Saved> {
  const res = await authed("/api/stores/mine/submit", { method: "POST" });
  revalidatePath("/magaza");
  revalidatePath("/kabinet");
  return res.ok ? { ok: true } : fail(res.status);
}

export async function removeStoreImage(kind: "logo" | "cover"): Promise<Saved> {
  const res = await authed(`/api/stores/mine/${kind}`, { method: "DELETE" });
  revalidatePath("/magaza");
  return res.ok ? { ok: true } : fail(res.status);
}

/** Pays for the VIP place from the wallet. The key stops a double charge when the button is pressed twice. */
export async function buyStoreVip(key: string): Promise<Saved> {
  if (!/^[A-Za-z0-9-]{8,64}$/.test(key)) {
    return { ok: false, unauthorized: false, message: await tr("Əməliyyat alınmadı. Yenidən cəhd edin.") };
  }
  const res = await authed("/api/stores/mine/vip", { method: "POST", body: { idempotencyKey: key } });
  revalidatePath("/magaza");
  revalidatePath("/");
  if (res.ok) return { ok: true };
  if (res.status === 401) return { ok: false, unauthorized: true, message: "" };
  if (res.status === 429) return { ok: false, unauthorized: false, message: await tr("Çox tez-tez cəhd olunub. Bir az gözləyin.") };
  if (res.status === 0) return { ok: false, unauthorized: false, message: await tr("Serverə qoşulmaq olmadı.") };
  if (/balance|insufficient|fund/i.test(res.message)) {
    return { ok: false, unauthorized: false, message: await tr("Balansınız çatmır. Pul kisəsini artırın.") };
  }
  return { ok: false, unauthorized: false, message: await tr("Əməliyyat alınmadı. Yenidən cəhd edin.") };
}

/** The name and description of a live store. */
export async function updateStoreInfo(name: string, description: string): Promise<Saved> {
  if (name.trim().length < 2 || name.trim().length > 100 || description.trim().length < 10 || description.trim().length > 2000) {
    return invalid();
  }
  const res = await authed("/api/stores/mine/info", { method: "PUT", body: { name: name.trim(), description: description.trim() } });
  revalidatePath("/magaza");
  revalidatePath("/kabinet");
  return res.ok ? { ok: true } : fail(res.status);
}
