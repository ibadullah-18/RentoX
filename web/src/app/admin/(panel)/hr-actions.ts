"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";
import { canSeeHr, getRoles, isAdmin } from "@/lib/session";

export type HrDone = { ok: true } | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;
const DAY = /^\d{4}-\d{2}-\d{2}$/;
const MONTH = /^\d{4}-\d{2}$/;

const known: [RegExp, string][] = [
  [/already started today/i, "Bu gün artıq işə başlamısınız."],
  [/have not started today/i, "Bu gün işə başlamamısınız."],
  [/already finished/i, "Gün artıq bitib."],
  [/access to the panel is closed/i, "Panelə giriş icazəniz bağlıdır."],
  [/asked for in advance/i, "İcazə əvvəlcədən istənilir: sabahı və ya sonrakı günləri seçin."],
  [/already have a request/i, "Bu günlərin bir hissəsi üçün artıq sorğunuz var."],
  [/contain no working day/i, "Seçdiyiniz günlərdə iş gününüz yoxdur."],
  [/last day cannot be before/i, "Son gün birinci gündən əvvəl ola bilməz."],
  [/at most 61 days/i, "Bir sorğu ən çoxu 61 günü əhatə edə bilər."],
  [/reason is required/i, "Səbəbi yazın."],
  [/reason cannot exceed/i, "Səbəb çox uzundur (ən çoxu 500 simvol)."],
  [/already been decided/i, "Bu sorğuya artıq qərar verilib."],
  [/started cannot be cancelled/i, "Başlamış günlər ləğv edilə bilməz."],
  [/more than what is left/i, "Məbləğ bu ay üçün qalan məbləğdən çoxdur."],
  [/reason of a deduction/i, "Kəsintinin səbəbini yazın."],
  [/amount must be more than zero/i, "Məbləğ sıfırdan böyük olmalıdır."],
  [/for the owner to decide/i, "Bu müraciətə yalnız rəhbər qərar verir."],
  [/already been answered/i, "Bu müraciətə artıq cavab verilib."],
  [/working hours are not valid/i, "İş saatları düzgün deyil: gün başlamadan əvvəl bitə bilməz."],
  [/salary is not valid/i, "Maaş düzgün deyil."],
  [/monthly days off must be/i, "İcazə günlərinin sayı 0 ilə 31 arasında olmalıdır."],
  [/not a staff member|staff members \(Admins\) only/i, "Bu şəxs işçi (Admin) deyil."],
  [/subject is required/i, "Mövzunu yazın (ən çoxu 160 simvol)."],
  [/message is required/i, "Mesajı yazın (ən çoxu 2000 simvol)."],
];

async function run(
  path: string,
  method: "POST" | "PUT" | "DELETE",
  body: unknown,
  revalidate: string[],
): Promise<HrDone> {
  const res = await authed(path, { method, body });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  for (const p of revalidate) revalidatePath(p);
  return { ok: true };
}

const denied: HrDone = { ok: false, unauthorized: true, message: "" };
const invalid = (message: string): HrDone => ({ ok: false, unauthorized: false, message });

const everywhere = ["/admin", "/admin/menim-isim", "/admin/kadr"];

// ---- a staff member, for themselves ---------------------------------------------------------------

export async function checkIn(): Promise<HrDone> {
  if (!(await isAdmin())) return denied;
  return run("/api/admin/hr/me/check-in", "POST", undefined, everywhere);
}

export async function checkOut(): Promise<HrDone> {
  if (!(await isAdmin())) return denied;
  return run("/api/admin/hr/me/check-out", "POST", undefined, everywhere);
}

export async function requestLeave(from: string, to: string, reason: string): Promise<HrDone> {
  if (!(await isAdmin())) return denied;
  if (!DAY.test(from) || !DAY.test(to)) return invalid("Tarixləri seçin.");
  if (!reason.trim()) return invalid("Səbəbi yazın.");
  return run("/api/admin/hr/me/leaves", "POST", { from, to, reason: reason.trim() }, everywhere);
}

export async function cancelLeave(id: string): Promise<HrDone> {
  if (!(await isAdmin())) return denied;
  if (!ID.test(id)) return invalid("Yanlış sorğu.");
  return run(`/api/admin/hr/me/leaves/${id}`, "DELETE", undefined, everywhere);
}

export async function sendStaffRequest(subject: string, body: string, toOwner: boolean): Promise<HrDone> {
  if (!(await isAdmin())) return denied;
  if (!subject.trim()) return invalid("Mövzunu yazın.");
  if (!body.trim()) return invalid("Mesajı yazın.");
  return run("/api/admin/hr/me/requests", "POST", { subject: subject.trim(), body: body.trim(), toOwner }, everywhere);
}

// ---- looking after the staff ----------------------------------------------------------------------

export async function handleStaffRequest(id: string, action: 1 | 2 | 3, note: string): Promise<HrDone> {
  if (!(await canSeeHr())) return denied;
  if (!ID.test(id)) return invalid("Yanlış sorğu.");
  if (action !== 3 && !note.trim()) return invalid("Cavabı və ya səbəbi yazın.");
  return run(`/api/admin/hr/requests/${id}/handle`, "POST", { action, note: note.trim() || null }, everywhere);
}

async function isOwner(): Promise<boolean> {
  return (await getRoles()).includes("SuperAdmin");
}

export async function decideLeave(id: string, approve: boolean, note: string): Promise<HrDone> {
  if (!(await isOwner())) return denied;
  if (!ID.test(id)) return invalid("Yanlış sorğu.");
  if (!approve && !note.trim()) return invalid("Rədd etmənin səbəbini yazın.");
  return run(`/api/admin/hr/leaves/${id}/decision`, "POST", { approve, note: note.trim() || null }, everywhere);
}

export type ProfileInput = {
  salary: number;
  loginAllowed: boolean;
  workStartMinute: number;
  workEndMinute: number;
  workDaysMask: number;
  monthlyDayOffLimit: number;
  note: string;
};

export async function saveStaffProfile(userId: string, input: ProfileInput): Promise<HrDone> {
  if (!(await isOwner())) return denied;
  if (!ID.test(userId)) return invalid("Yanlış sorğu.");
  if (!Number.isFinite(input.salary) || input.salary < 0 || input.salary > 100000) return invalid("Maaş 0 ilə 100 000 AZN arasında olmalıdır.");
  return run(
    `/api/admin/hr/staff/${userId}`,
    "PUT",
    {
      monthlySalaryInQepik: Math.round(input.salary * 100),
      loginAllowed: input.loginAllowed,
      workStartMinute: input.workStartMinute,
      workEndMinute: input.workEndMinute,
      workDaysMask: input.workDaysMask,
      monthlyDayOffLimit: input.monthlyDayOffLimit,
      note: input.note.trim() || null,
    },
    [...everywhere, "/admin/isciler"],
  );
}

export async function addPayLine(userId: string, month: string, kind: number, amount: number, note: string): Promise<HrDone> {
  if (!(await isOwner())) return denied;
  if (!ID.test(userId) || !MONTH.test(month)) return invalid("Yanlış sorğu.");
  if (!Number.isFinite(amount) || amount <= 0 || Math.round(amount * 100) / 100 !== amount) {
    return invalid("Məbləğ sıfırdan böyük olmalıdır (ən çoxu iki onluq).");
  }
  if (kind === 1 && !note.trim()) return invalid("Kəsintinin səbəbini yazın.");
  return run(
    `/api/admin/hr/payroll/${userId}/lines`,
    "POST",
    { month, kind, amountInQepik: Math.round(amount * 100), note: note.trim() || null },
    everywhere,
  );
}

export async function voidPayLine(lineId: string): Promise<HrDone> {
  if (!(await isOwner())) return denied;
  if (!ID.test(lineId)) return invalid("Yanlış sorğu.");
  return run(`/api/admin/hr/payroll/lines/${lineId}/void`, "POST", undefined, everywhere);
}
