// The staff pages: what the API sends, and how it is written on the screen.

export type Schedule = { workStartMinute: number; workEndMinute: number; workDaysMask: number; monthlyDayOffLimit: number };

export type StaffProfile = {
  userId: string;
  fullName: string;
  phone?: string | null;
  monthlySalaryInQepik: number;
  loginAllowed: boolean;
  schedule: Schedule;
  note?: string | null;
  sections: string[];
};

export type DayBoardRow = {
  userId: string;
  fullName: string;
  status: number;
  checkInAtUtc?: string | null;
  checkOutAtUtc?: string | null;
  workedMinutes: number;
  workStartMinute: number;
  workEndMinute: number;
  loginAllowed: boolean;
};

export type MonthSheet = {
  year: number;
  month: number;
  daysInMonth: number;
  rows: { userId: string; fullName: string; days: number[]; present: number; late: number; absent: number; leave: number }[];
};

export type MyDay = {
  isStaff: boolean;
  today: string;
  status: number;
  schedule: Schedule;
  checkInAtUtc?: string | null;
  checkOutAtUtc?: string | null;
  workedMinutes: number;
  dayOffUsedThisMonth: number;
  hasWatcher: boolean;
};

export type LeaveRequest = {
  id: string;
  userId: string;
  fullName: string;
  fromDay: string;
  toDay: string;
  workingDays: number;
  overLimit: boolean;
  reason: string;
  status: number;
  decisionNote?: string | null;
  createdAtUtc: string;
  decidedAtUtc?: string | null;
  dayOffLimit: number;
};

export type PayrollSheet = {
  userId: string;
  fullName: string;
  month: string;
  baseInQepik: number;
  bonusInQepik: number;
  deductionsInQepik: number;
  advancesInQepik: number;
  paymentsInQepik: number;
  remainingInQepik: number;
  lines: { id: string; kind: number; amountInQepik: number; note?: string | null; createdAtUtc: string; createdByName?: string | null; isVoid: boolean }[];
};

export type StaffRequest = {
  id: string;
  fromUserId: string;
  fromName: string;
  subject: string;
  body: string;
  toOwner: boolean;
  status: number;
  answer?: string | null;
  handledByName?: string | null;
  createdAtUtc: string;
  handledAtUtc?: string | null;
};

/** One colour per kind of day, the same on the board, in the table and in "my day". */
export const dayStatus: Record<number, { label: string; chip: string; cell: string; short: string }> = {
  1: { label: "İşdədir", chip: "bg-success-soft text-success", cell: "bg-success/80 text-white", short: "✓" },
  2: { label: "Gecikib", chip: "bg-warning-soft text-warning", cell: "bg-warning text-white", short: "G" },
  3: { label: "İşə gəlməyib", chip: "bg-danger text-white", cell: "bg-danger text-white", short: "×" },
  4: { label: "İcazəlidir", chip: "bg-primary-soft text-primary", cell: "bg-primary/70 text-white", short: "İ" },
  5: { label: "Çıxış günü", chip: "bg-line text-ink-2", cell: "bg-line text-ink-2", short: "" },
  6: { label: "Hələ başlamayıb", chip: "bg-line text-ink-2", cell: "bg-line text-ink-2", short: "…" },
  7: { label: "Gələcək", chip: "bg-line text-ink-2", cell: "bg-bg text-ink-2", short: "" },
};

export const leaveStatusLabel: Record<number, string> = { 1: "Gözləyir", 2: "Təsdiqləndi", 3: "Rədd edildi", 4: "Ləğv edildi" };
export const leaveStatusTone: Record<number, "warning" | "success" | "danger" | "muted"> = { 1: "warning", 2: "success", 3: "danger", 4: "muted" };

export const requestStatusLabel: Record<number, string> = { 1: "Açıq", 2: "Sizə yönləndirilib", 3: "Cavablandı", 4: "Rədd edildi" };
export const requestStatusTone: Record<number, "warning" | "success" | "danger" | "primary"> = { 1: "warning", 2: "primary", 3: "success", 4: "danger" };

export const payKind: Record<number, { label: string; sign: string; tone: string }> = {
  1: { label: "Kəsinti", sign: "−", tone: "text-danger" },
  2: { label: "Avans", sign: "−", tone: "text-warning" },
  3: { label: "Ödəniş", sign: "−", tone: "text-primary" },
  4: { label: "Mükafat", sign: "+", tone: "text-success" },
};

export const WEEKDAYS = ["B.e.", "Ç.a.", "Çər.", "C.a.", "Cümə", "Şən.", "Baz."];

/** 540 → "09:00". */
export const hhmm = (minutes: number) => `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;

/** "09:30" → 570 (or null when it is not a time). */
export function toMinutes(text: string): number | null {
  const m = /^(\d{1,2}):(\d{2})$/.exec(text.trim());
  if (!m) return null;
  const h = Number(m[1]);
  const min = Number(m[2]);
  return h > 24 || min > 59 ? null : h * 60 + min;
}

/** 123456 qəpik → "1 234.56". */
export const money = (qepik: number) =>
  `${qepik < 0 ? "−" : ""}${(Math.abs(qepik) / 100).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/,/g, " ")}`;

/** "6 saat 20 dəq". */
export function duration(minutes: number): string {
  if (minutes <= 0) return "—";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${h > 0 ? `${h} saat ` : ""}${m > 0 || h === 0 ? `${m} dəq` : ""}`.trim();
}

const bakuParts = (iso: string) => {
  const d = new Date(new Date(iso).getTime() + 4 * 3600_000);
  return { y: d.getUTCFullYear(), mo: d.getUTCMonth() + 1, d: d.getUTCDate(), h: d.getUTCHours(), mi: d.getUTCMinutes() };
};

/** A moment in Baku time: "14.10.2026 09:12". */
export function bakuDateTime(iso?: string | null): string {
  if (!iso) return "—";
  const p = bakuParts(iso);
  const two = (n: number) => String(n).padStart(2, "0");
  return `${two(p.d)}.${two(p.mo)}.${p.y} ${two(p.h)}:${two(p.mi)}`;
}

export function bakuTime(iso?: string | null): string {
  if (!iso) return "—";
  const p = bakuParts(iso);
  return `${String(p.h).padStart(2, "0")}:${String(p.mi).padStart(2, "0")}`;
}

/** Today in Baku, as "2026-10-14". */
export const bakuToday = () => new Date(Date.now() + 4 * 3600_000).toISOString().slice(0, 10);

/** "2026-10-14" → "14.10.2026". */
export const dayText = (iso: string) => iso.split("-").reverse().join(".");

export const MONTHS = ["Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun", "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"];

/** The month in the address ("2026-10") or this month in Baku. */
export function thisMonth(param?: string | string[] | undefined): { year: number; month: number; key: string } {
  const raw = Array.isArray(param) ? param[0] : param;
  const m = /^(\d{4})-(\d{2})$/.exec(raw ?? "");
  if (m && Number(m[2]) >= 1 && Number(m[2]) <= 12) return { year: Number(m[1]), month: Number(m[2]), key: raw! };
  const p = bakuParts(new Date().toISOString());
  return { year: p.y, month: p.mo, key: `${p.y}-${String(p.mo).padStart(2, "0")}` };
}

export function shiftMonth(year: number, month: number, by: number): string {
  const d = new Date(Date.UTC(year, month - 1 + by, 1));
  return `${d.getUTCFullYear()}-${String(d.getUTCMonth() + 1).padStart(2, "0")}`;
}
