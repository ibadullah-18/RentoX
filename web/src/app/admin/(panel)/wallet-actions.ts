"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type WalletAdjusted =
  | { ok: true; balanceAfter: number }
  | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;

const known: [RegExp, string][] = [
  [/cannot change your own wallet/i, "Öz pul kisənizi dəyişə bilməzsiniz."],
  [/balance is insufficient/i, "Kisədə bu qədər pul yoxdur."],
  [/amount must be between/i, "Məbləğ 0.01 ilə 1000 arasında, ən çoxu iki onluq ilə olmalıdır."],
  [/reason must be/i, "Səbəb 5 ilə 300 simvol arasında olmalıdır."],
  [/not allowed/i, "Bu növ düzəliş icazəli deyil."],
  [/user was not found/i, "İstifadəçi tapılmadı."],
];

/** Adds money to or takes it from a wallet. A finance-section Admin only; the API checks too. */
export async function adjustWallet(
  userId: string,
  requestId: string,
  credit: boolean,
  type: number,
  amount: number,
  reason: string,
): Promise<WalletAdjusted> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const sections = await getSections();
  if (sections !== null && !sections.includes("finance")) return { ok: false, unauthorized: true, message: "" };
  if (!ID.test(userId) || !ID.test(requestId)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };

  const text = reason.trim();
  if (text.length < 5 || text.length > 300) {
    return { ok: false, unauthorized: false, message: "Səbəb 5 ilə 300 simvol arasında olmalıdır." };
  }
  if (!Number.isFinite(amount) || amount <= 0 || amount > 1000 || Math.round(amount * 100) / 100 !== amount) {
    return { ok: false, unauthorized: false, message: "Məbləğ 0.01 ilə 1000 arasında, ən çoxu iki onluq ilə olmalıdır." };
  }

  const res = await authed<{ balanceAfter: number }>(`/api/admin/users/${userId}/wallet-adjustments`, {
    method: "POST",
    body: { requestId, credit, type, amount, reason: text },
  });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const mapped = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: mapped ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }

  revalidatePath(`/admin/istifadeciler/${userId}`);
  revalidatePath("/admin/maliyye");
  return { ok: true, balanceAfter: res.data.balanceAfter };
}
