"use server";

import { getT } from "@/lib/i18n/server";
import { authed } from "@/lib/authed";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type FieldRequest = {
  fieldId: string;
  optionIds: string[];
  textValue?: string;
  numericValue?: number;
  flagValue?: boolean;
  calendarValue?: string;
  customValue?: string;
};

export type DraftInput = {
  categoryId: string;
  title: string;
  description: string;
  price: number;
  rentalPeriodUnit: number;
  cityId: string;
  districtId?: string;
  fields: FieldRequest[];
};

export type Outcome<T = object> =
  | ({ ok: true } & T)
  | { ok: false; unauthorized: boolean; message: string };

async function failure(status: number, message: string): Promise<Outcome<never>> {
  if (status === 401) return { ok: false, unauthorized: true, message: "" };
  if (status === 429) {
    return {
      ok: false,
      unauthorized: false,
      message: await tr("Çox tez-tez cəhd olunub. Bir az gözləyib yenidən yoxlayın."),
    };
  }
  if (status === 0) {
    return {
      ok: false,
      unauthorized: false,
      message: await tr("Serverə qoşulmaq olmadı. İnternetinizi yoxlayın."),
    };
  }
  // The API's own explanation (English) is shown when it is a clear rule.
  return { ok: false, unauthorized: false, message: message && message !== "unknown" ? message : await tr("Əməliyyat alınmadı. Yenidən cəhd edin.") };
}

/** Creates the listing as a draft and returns its id. */
export async function createDraft(input: DraftInput): Promise<Outcome<{ id: string }>> {
  const res = await authed<{ id: string }>("/api/listings", {
    method: "POST",
    body: {
      categoryId: input.categoryId,
      title: input.title.trim(),
      description: input.description.trim(),
      price: input.price,
      currency: "AZN",
      rentalPeriodUnit: input.rentalPeriodUnit,
      fields: input.fields,
      cityId: input.cityId,
      districtId: input.districtId ?? null,
    },
  });
  return res.ok ? { ok: true, id: res.data.id } : failure(res.status, res.message);
}

const ID = /^[0-9a-f-]{36}$/i;

/**
 * Sends the draft on: it goes live, to moderation, or asks for the
 * activation fee, depending on the rules (status ids as in the backend).
 */
export async function submitListing(id: string): Promise<Outcome<{ status: number }>> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed<{ status: number }>(`/api/listings/${id}/submit`, { method: "POST" });
  return res.ok ? { ok: true, status: res.data.status } : failure(res.status, res.message);
}

/** Pays the activation fee from the wallet. */
export async function payAndActivate(id: string): Promise<Outcome<{ remainingBalance: number }>> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Elan tapılmadı.") };
  const res = await authed<{ remainingBalance: number }>(
    `/api/listings/${id}/pay-and-activate`,
    { method: "POST" },
  );
  return res.ok
    ? { ok: true, remainingBalance: res.data.remainingBalance }
    : failure(res.status, res.message);
}

/** How many photos the server already holds (to not upload one twice on a retry). */
export async function uploadedPhotoCount(id: string): Promise<number | null> {
  if (!ID.test(id)) return null;
  const res = await authed<{ images?: unknown[] }>(`/api/listings/mine/${id}`, {
    query: { language: "az" },
  });
  return res.ok ? (res.data.images?.length ?? 0) : null;
}
