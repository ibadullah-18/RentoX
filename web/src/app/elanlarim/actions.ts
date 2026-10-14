"use server";

import { getT } from "@/lib/i18n/server";
import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type Done =
  | { ok: true; message?: string }
  | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;

async function fail(status: number, message: string): Promise<Done> {
  if (status === 401) return { ok: false, unauthorized: true, message: "" };
  if (status === 429) {
    return { ok: false, unauthorized: false, message: await tr("Çox tez-tez cəhd olunub. Bir az gözləyin.") };
  }
  if (status === 0) {
    return { ok: false, unauthorized: false, message: await tr("Serverə qoşulmaq olmadı.") };
  }
  return {
    ok: false,
    unauthorized: false,
    message: status === 402 || /balance|insufficient|fund/i.test(message)
      ? await tr("Balansınız çatmır. Pul kisəsini artırın.")
      : await tr("Əməliyyat alınmadı. Yenidən cəhd edin."),
  };
}

async function post(id: string, action: string, body?: unknown): Promise<Done> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed(`/api/listings/${id}/${action}`, { method: "POST", body });
  revalidatePath("/elanlarim");
  revalidatePath(`/elanlarim/${id}`);
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export async function deactivate(id: string): Promise<Done> {
  return post(id, "deactivate");
}
export async function reactivate(id: string): Promise<Done> {
  return post(id, "reactivate");
}
export async function submit(id: string): Promise<Done> {
  return post(id, "submit");
}
export async function reopen(id: string): Promise<Done> {
  return post(id, "reopen");
}
export async function renew(id: string): Promise<Done> {
  return post(id, "renew");
}
export async function payAndActivate(id: string): Promise<Done> {
  return post(id, "pay-and-activate");
}

/** type: 1 = bump (move to the top), 2 = VIP. The key stops a double charge. */
export async function promote(id: string, type: 1 | 2, key: string): Promise<Done> {
  if (!/^[A-Za-z0-9-]{8,64}$/.test(key)) {
    return { ok: false, unauthorized: false, message: await tr("Əməliyyat alınmadı.") };
  }
  return post(id, "promotions", { type, idempotencyKey: key });
}

export async function remove(id: string): Promise<Done> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed(`/api/listings/${id}`, { method: "DELETE" });
  revalidatePath("/elanlarim");
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export type DetailsInput = {
  title: string;
  description: string;
  price: number;
  rentalPeriodUnit: number;
  cityId: string;
  districtId?: string;
};

/** Saves title, description, price, period and place (drafts and rejected listings only). */
export async function updateDetails(id: string, input: DetailsInput): Promise<Done> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed(`/api/listings/${id}`, {
    method: "PUT",
    body: {
      title: input.title.trim(),
      description: input.description.trim(),
      price: input.price,
      currency: "AZN",
      rentalPeriodUnit: input.rentalPeriodUnit,
      cityId: input.cityId,
      districtId: input.districtId ?? null,
    },
  });
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

/** Replaces ALL the category field values of the listing. */
export async function updateFields(id: string, fields: unknown[]): Promise<Done> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed(`/api/listings/${id}/fields`, { method: "PUT", body: { fields } });
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export async function deleteImage(id: string, imageId: string): Promise<Done> {
  if (!ID.test(id) || !ID.test(imageId)) return { ok: false, unauthorized: false, message: await tr("Şəkil tapılmadı.") };
  const res = await authed(`/api/listings/${id}/images/${imageId}`, { method: "DELETE" });
  return res.ok ? { ok: true } : fail(res.status, res.message);
}

export async function setCover(id: string, imageId: string): Promise<Done> {
  if (!ID.test(id) || !ID.test(imageId)) return { ok: false, unauthorized: false, message: await tr("Şəkil tapılmadı.") };
  const res = await authed(`/api/listings/${id}/images/${imageId}/cover`, { method: "PATCH" });
  return res.ok ? { ok: true } : fail(res.status, res.message);
}
