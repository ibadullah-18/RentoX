"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

export type FinanceDone = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const known: [RegExp, string][] = [
  [/amount must be more than zero/i, "Məbləğ sıfırdan böyük olmalıdır (ən çoxu iki onluq)."],
  [/title is required/i, "Təyinatı yazın (ən çoxu 120 simvol)."],
  [/in the future/i, "Xərc gələcək tarixdə ola bilməz."],
  [/reason is required/i, "Ləğv səbəbini yazın."],
  [/already voided/i, "Bu xərc artıq ləğv edilib."],
  [/full name is required/i, "Ad, soyad, ata adı boş ola bilməz."],
];

const denied: FinanceDone = { ok: false, unauthorized: true, message: "" };

/** Only the owner (SuperAdmin) writes here; the API checks too. */
async function owner(): Promise<boolean> {
  return (await getRoles()).includes("SuperAdmin");
}

async function run(path: string, method: "POST" | "PUT", body: unknown): Promise<FinanceDone> {
  const res = await authed(path, { method, body });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return denied;
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  revalidatePath("/admin/maliyye");
  revalidatePath("/admin/maliyye/xercler");
  revalidatePath("/admin/maliyye/akt");
  return { ok: true };
}

export type ExpenseInput = { category: number; title: string; amount: number; day: string; note: string };

export async function addExpense(input: ExpenseInput): Promise<FinanceDone> {
  if (!(await owner())) return denied;
  if (![1, 2, 3, 4].includes(input.category)) return { ok: false, unauthorized: false, message: "Xərcin növünü seçin." };
  if (!input.title.trim()) return { ok: false, unauthorized: false, message: "Təyinatı yazın." };
  if (!Number.isFinite(input.amount) || input.amount <= 0 || Math.round(input.amount * 100) / 100 !== input.amount) {
    return { ok: false, unauthorized: false, message: "Məbləğ sıfırdan böyük olmalıdır (ən çoxu iki onluq)." };
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(input.day)) return { ok: false, unauthorized: false, message: "Tarixi seçin." };
  return run("/api/admin/finance/expenses", "POST", {
    category: input.category,
    title: input.title.trim(),
    amount: input.amount,
    day: input.day,
    note: input.note.trim() || null,
  });
}

export async function voidExpense(id: string, reason: string): Promise<FinanceDone> {
  if (!(await owner())) return denied;
  if (!/^[0-9a-f-]{36}$/i.test(id)) return { ok: false, unauthorized: false, message: "Xərc tapılmadı." };
  if (!reason.trim()) return { ok: false, unauthorized: false, message: "Ləğv səbəbini yazın." };
  return run(`/api/admin/finance/expenses/${id}/void`, "POST", { reason: reason.trim() });
}

export type ProfileInput = { fullName: string; voen: string; phone: string; email: string; address: string };

export async function saveFinanceProfile(input: ProfileInput): Promise<FinanceDone> {
  if (!(await owner())) return denied;
  if (!input.fullName.trim()) return { ok: false, unauthorized: false, message: "Ad, soyad, ata adını yazın." };
  const blank = (v: string) => v.trim() || null;
  return run("/api/admin/finance/profile", "PUT", {
    fullName: input.fullName.trim(),
    voen: blank(input.voen),
    phone: blank(input.phone),
    email: blank(input.email),
    address: blank(input.address),
  });
}
