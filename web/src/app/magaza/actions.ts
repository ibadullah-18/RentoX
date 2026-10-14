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
