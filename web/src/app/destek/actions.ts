"use server";

import { getT } from "@/lib/i18n/server";
import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type Outcome<T = object> =
  | ({ ok: true } & T)
  | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;
const SUBJECT_MAX = 160;
const BODY_MAX = 4000;

async function fail(status: number): Promise<Outcome<never>> {
  if (status === 401) return { ok: false, unauthorized: true, message: "" };
  if (status === 429) {
    return { ok: false, unauthorized: false, message: await tr("Çox tez-tez cəhd olunub. Bir az gözləyin.") };
  }
  return { ok: false, unauthorized: false, message: await tr("Göndərilmədi. Yenidən cəhd edin.") };
}

/** Opens a request and returns its id. */
export async function createTicket(input: {
  category: number;
  subject: string;
  message: string;
}): Promise<Outcome<{ id: string }>> {
  const subject = input.subject.trim();
  const message = input.message.trim();
  if (!subject || subject.length > SUBJECT_MAX) {
    return { ok: false, unauthorized: false, message: await tr("Mövzunu yazın (ən çoxu {n} simvol).", { n: SUBJECT_MAX }) };
  }
  if (!message || message.length > BODY_MAX) {
    return { ok: false, unauthorized: false, message: await tr("Mesajı yazın (ən çoxu {n} simvol).", { n: BODY_MAX }) };
  }
  if (![1, 2, 3, 4, 5, 6, 7].includes(input.category)) {
    return { ok: false, unauthorized: false, message: await tr("Mövzu növünü seçin.") };
  }

  const res = await authed<{ id: string }>("/api/support/tickets", {
    method: "POST",
    body: { category: input.category, subject, initialMessage: message },
  });
  revalidatePath("/destek");
  return res.ok ? { ok: true, id: res.data.id } : fail(res.status);
}

export async function replyTicket(id: string, body: string): Promise<Outcome> {
  const text = body.trim();
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: await tr("Müraciət tapılmadı.") };
  if (!text || text.length > BODY_MAX) {
    return { ok: false, unauthorized: false, message: await tr("Mesajı yazın.") };
  }
  const res = await authed(`/api/support/tickets/${id}/messages`, { method: "POST", body: { body: text } });
  revalidatePath(`/destek/${id}`);
  revalidatePath("/destek");
  return res.ok ? { ok: true } : fail(res.status);
}
