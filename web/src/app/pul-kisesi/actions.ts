"use server";

import { getT } from "@/lib/i18n/server";
import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type TopUp =
  | { ok: true }
  | { ok: false; unauthorized: boolean; message: string };

/**
 * Adds demo money. The backend only allows this while developing; in
 * production real payments will replace it.
 */
export async function demoTopUp(amount: number, key: string): Promise<TopUp> {
  if (![10, 20, 50].includes(amount) || !/^[A-Za-z0-9-]{8,64}$/.test(key)) {
    return { ok: false, unauthorized: false, message: await tr("Məbləğ düzgün deyil.") };
  }
  const res = await authed("/api/wallet/demo-top-up", {
    method: "POST",
    body: { amount, idempotencyKey: key },
  });
  revalidatePath("/pul-kisesi");
  revalidatePath("/kabinet");
  if (res.ok) return { ok: true };
  return {
    ok: false,
    unauthorized: res.status === 401,
    message: await tr("Balansı artırmaq alınmadı. Bu yalnız sınaq rejimində mümkündür."),
  };
}
