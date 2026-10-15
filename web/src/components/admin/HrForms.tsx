"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Check, Loader2, LogIn, LogOut, Send, X } from "lucide-react";
import {
  addPayLine,
  cancelLeave,
  checkIn,
  checkOut,
  decideLeave,
  handleStaffRequest,
  requestLeave,
  saveStaffProfile,
  sendStaffRequest,
  voidPayLine,
  type HrDone,
} from "@/app/admin/(panel)/hr-actions";
import { adminHref } from "@/lib/admin-path";
import { WEEKDAYS, bakuTime, dayStatus, duration, hhmm, toMinutes, type MyDay, type StaffProfile } from "@/lib/admin-hr";

const field =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";
const area =
  "w-full resize-none rounded-lg border border-line bg-surface px-3 py-2 text-[15px] outline-none focus:border-primary disabled:opacity-60";
const primary =
  "flex h-10 items-center justify-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50";
const quiet =
  "flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary hover:text-primary disabled:opacity-60";

/** Runs one action, shows the answer, refreshes the page. */
function useDo() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<string | null>(null);

  async function run(action: () => Promise<HrDone>, message?: string): Promise<boolean> {
    setBusy(true);
    setError(null);
    setDone(null);
    const res = await action();
    setBusy(false);
    if (res.ok) {
      if (message) setDone(message);
      router.refresh();
      return true;
    }
    if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
    return false;
  }

  return { busy, error, done, run, clear: () => setError(null) };
}

function Feedback({ error, done }: { error: string | null; done: string | null }) {
  return (
    <>
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      {done && (
        <p role="status" className="text-sm font-semibold text-success">
          {done}
        </p>
      )}
    </>
  );
}

// ---- the day: start and finish ---------------------------------------------------------------------------

/** "İşə başla" / "İşi bitir", with the time of each. In the menu it is a small box, on "my day" a big one. */
export function WorkDay({ day, big = false }: { day: MyDay; big?: boolean }) {
  const { busy, error, run } = useDo();
  const started = Boolean(day.checkInAtUtc);
  const finished = Boolean(day.checkOutAtUtc);
  const info = dayStatus[day.status] ?? dayStatus[6];

  return (
    <div className={big ? "space-y-3 p-5" : "space-y-2 rounded-xl border border-line bg-bg p-3"}>
      <div className="flex items-center justify-between gap-2">
        <span className={`rounded-md px-2 py-0.5 text-xs font-bold ${info.chip}`}>{info.label}</span>
        <span className="text-xs text-ink-2">
          {hhmm(day.schedule.workStartMinute)}–{hhmm(day.schedule.workEndMinute)}
        </span>
      </div>

      {started && (
        <p className={big ? "text-sm" : "text-xs"}>
          Başladı: <b>{bakuTime(day.checkInAtUtc)}</b>
          {finished && (
            <>
              {" "}
              · Bitdi: <b>{bakuTime(day.checkOutAtUtc)}</b> · {duration(day.workedMinutes)}
            </>
          )}
          {!finished && <> · {duration(day.workedMinutes)}</>}
        </p>
      )}

      {!started && (
        <button type="button" disabled={busy} onClick={() => void run(checkIn)} className={`${primary} w-full bg-success hover:bg-success`}>
          {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <LogIn className="size-4" aria-hidden />}
          İşə başla
        </button>
      )}
      {started && !finished && (
        <button type="button" disabled={busy} onClick={() => void run(checkOut)} className={`${primary} w-full`}>
          {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <LogOut className="size-4" aria-hidden />}
          İşi bitir
        </button>
      )}
      {finished && <p className="text-xs font-semibold text-success">Bu günkü iş bitdi. Sağ olun!</p>}
      {error && (
        <p role="alert" className="text-xs font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

// ---- days off ----------------------------------------------------------------------------------------------

const tomorrow = () => new Date(Date.now() + 4 * 3600_000 + 86_400_000).toISOString().slice(0, 10);

export function LeaveRequestForm({ used, limit }: { used: number; limit: number }) {
  const { busy, error, done, run } = useDo();
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [reason, setReason] = useState("");

  async function send() {
    const ok = await run(() => requestLeave(from, to || from, reason), "Sorğu göndərildi. Cavab bildiriş kimi gələcək.");
    if (ok) {
      setFrom("");
      setTo("");
      setReason("");
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void send();
      }}
      className="space-y-3 p-4"
    >
      <p className="text-sm text-ink-2">
        Ayda <b>{limit}</b> iş günü icazə götürmək olar. Bu ay artıq <b>{used}</b> gün istənilib. Limit aşılsa, sorğu yenə gedir, amma rəhbərə
        «limit aşılır» işarəsi ilə çatır.
      </p>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm font-bold">
          Neçənci gündən
          <input type="date" min={tomorrow()} value={from} onChange={(e) => setFrom(e.target.value)} disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
        <label className="block text-sm font-bold">
          Neçənci günə qədər
          <input type="date" min={from || tomorrow()} value={to} onChange={(e) => setTo(e.target.value)} disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
      </div>
      <label className="block text-sm font-bold">
        Səbəb
        <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={2} maxLength={500} disabled={busy} className={`${area} mt-1 font-normal`} />
      </label>
      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy || !from || !reason.trim()} className={primary}>
          {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Send className="size-4" aria-hidden />}
          Sorğu göndər
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

export function CancelLeaveButton({ id }: { id: string }) {
  const { busy, error, run } = useDo();
  return (
    <span className="flex flex-col items-end gap-1">
      <button type="button" disabled={busy} onClick={() => void run(() => cancelLeave(id))} className={quiet}>
        {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <X className="size-4" aria-hidden />}
        Ləğv et
      </button>
      {error && <span className="text-xs font-semibold text-danger">{error}</span>}
    </span>
  );
}

/** The owner decides on a day off. A refusal needs its reason. */
export function LeaveDecision({ id }: { id: string }) {
  const { busy, error, run } = useDo();
  const [note, setNote] = useState("");
  return (
    <div className="w-full space-y-2 sm:w-72">
      <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={500} placeholder="Qeyd (rədd üçün məcburi)" disabled={busy} className={field} />
      <div className="flex gap-2">
        <button type="button" disabled={busy} onClick={() => void run(() => decideLeave(id, true, note))} className={`${primary} h-9 flex-1 bg-success hover:bg-success`}>
          <Check className="size-4" aria-hidden />
          Təsdiq et
        </button>
        <button type="button" disabled={busy} onClick={() => void run(() => decideLeave(id, false, note))} className={`${quiet} flex-1 justify-center text-danger`}>
          <X className="size-4" aria-hidden />
          Rədd et
        </button>
      </div>
      {error && <p className="text-xs font-semibold text-danger">{error}</p>}
    </div>
  );
}

// ---- requests --------------------------------------------------------------------------------------------

export function StaffRequestForm({ hasWatcher }: { hasWatcher: boolean }) {
  const { busy, error, done, run } = useDo();
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");
  const [toOwner, setToOwner] = useState(!hasWatcher);

  async function send() {
    const ok = await run(() => sendStaffRequest(subject, body, toOwner), "Müraciət göndərildi.");
    if (ok) {
      setSubject("");
      setBody("");
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void send();
      }}
      className="space-y-3 p-4"
    >
      <label className="block text-sm font-bold">
        Mövzu
        <input value={subject} onChange={(e) => setSubject(e.target.value)} maxLength={160} disabled={busy} className={`${field} mt-1 font-normal`} />
      </label>
      <label className="block text-sm font-bold">
        Mesaj
        <textarea value={body} onChange={(e) => setBody(e.target.value)} rows={3} maxLength={2000} disabled={busy} className={`${area} mt-1 font-normal`} />
      </label>
      <label className="flex cursor-pointer items-start gap-2 text-sm">
        <input type="checkbox" checked={toOwner} onChange={(e) => setToOwner(e.target.checked)} disabled={busy} className="mt-0.5 size-4 accent-primary" />
        <span>
          <b>Birbaşa rəhbərə göndər.</b>{" "}
          <span className="text-ink-2">
            {hasWatcher
              ? "Seçməsəniz, işçilərə baxan şəxs cavab verir və ya məsələni rəhbərə yönləndirir."
              : "İşçilərə baxan ayrıca şəxs təyin olunmayıb: müraciət rəhbərə gedir."}
          </span>
        </span>
      </label>
      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy || !subject.trim() || !body.trim()} className={primary}>
          {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Send className="size-4" aria-hidden />}
          Göndər
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

/** Answer, refuse or (for the watcher) pass a request to the owner. */
export function RequestHandler({ id, canForward }: { id: string; canForward: boolean }) {
  const { busy, error, run } = useDo();
  const [note, setNote] = useState("");
  return (
    <div className="w-full space-y-2 sm:w-80">
      <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={2} maxLength={2000} placeholder="Cavab və ya qeyd" disabled={busy} className={area} />
      <div className="flex flex-wrap gap-2">
        <button type="button" disabled={busy} onClick={() => void run(() => handleStaffRequest(id, 1, note))} className={`${primary} h-9`}>
          <Check className="size-4" aria-hidden />
          Cavab ver
        </button>
        <button type="button" disabled={busy} onClick={() => void run(() => handleStaffRequest(id, 2, note))} className={`${quiet} text-danger`}>
          <X className="size-4" aria-hidden />
          Rədd et
        </button>
        {canForward && (
          <button type="button" disabled={busy} onClick={() => void run(() => handleStaffRequest(id, 3, note))} className={quiet}>
            <Send className="size-4" aria-hidden />
            Rəhbərə yönləndir
          </button>
        )}
      </div>
      {error && <p className="text-xs font-semibold text-danger">{error}</p>}
    </div>
  );
}

// ---- the owner: profile and pay ----------------------------------------------------------------------------

/** Pay, the panel switch, the working hours and days, the monthly days off. */
export function ProfileForm({ profile }: { profile: StaffProfile }) {
  const { busy, error, done, run } = useDo();
  const [salary, setSalary] = useState(String(profile.monthlySalaryInQepik / 100));
  const [allowed, setAllowed] = useState(profile.loginAllowed);
  const [start, setStart] = useState(hhmm(profile.schedule.workStartMinute));
  const [end, setEnd] = useState(hhmm(profile.schedule.workEndMinute));
  const [mask, setMask] = useState(profile.schedule.workDaysMask);
  const [limit, setLimit] = useState(String(profile.schedule.monthlyDayOffLimit));
  const [note, setNote] = useState(profile.note ?? "");

  async function save() {
    const a = toMinutes(start);
    const b = toMinutes(end);
    if (a === null || b === null) return;
    await run(
      () =>
        saveStaffProfile(profile.userId, {
          salary: Number(salary.replace(",", ".")),
          loginAllowed: allowed,
          workStartMinute: a,
          workEndMinute: b,
          workDaysMask: mask,
          monthlyDayOffLimit: Number(limit),
          note,
        }),
      "Yadda saxlanıldı.",
    );
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-4 p-4"
    >
      <label className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3 ${allowed ? "border-line" : "border-danger bg-danger/5"}`}>
        <input type="checkbox" checked={allowed} onChange={(e) => setAllowed(e.target.checked)} disabled={busy} className="mt-1 size-4 accent-primary" />
        <span className="text-sm">
          <b>{allowed ? "Panelə daxil ola bilər" : "Panelə giriş bağlıdır"}</b>
          <span className="block text-ink-2">
            Giriş bağlananda şəxs Admin olaraq qalır, amma admin panelə və onun sorğularına çıxışı olmur, bütün cihazlardan çıxarılır. İstənilən vaxt yenidən
            aça bilərsiniz.
          </span>
        </span>
      </label>

      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block text-sm font-bold">
          Aylıq maaş (AZN)
          <input value={salary} onChange={(e) => setSalary(e.target.value)} inputMode="decimal" disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
        <label className="block text-sm font-bold">
          Ayda icazə günü
          <input value={limit} onChange={(e) => setLimit(e.target.value)} inputMode="numeric" disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
        <label className="block text-sm font-bold">
          İşə başlama saatı
          <input type="time" value={start} onChange={(e) => setStart(e.target.value)} disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
        <label className="block text-sm font-bold">
          İşi bitirmə saatı
          <input type="time" value={end} onChange={(e) => setEnd(e.target.value)} disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
      </div>

      <fieldset>
        <legend className="mb-1 text-sm font-bold">İş günləri</legend>
        <div className="flex flex-wrap gap-2">
          {WEEKDAYS.map((name, i) => {
            const on = (mask & (1 << i)) !== 0;
            return (
              <label
                key={name}
                className={`flex h-9 min-w-14 cursor-pointer items-center justify-center rounded-lg border px-3 text-sm font-semibold ${
                  on ? "border-primary bg-primary text-white" : "border-line"
                }`}
              >
                <input type="checkbox" className="sr-only" checked={on} onChange={() => setMask(mask ^ (1 << i))} disabled={busy} />
                {name}
              </label>
            );
          })}
        </div>
      </fieldset>

      <label className="block text-sm font-bold">
        Qeyd (yalnız siz görürsünüz)
        <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={2} maxLength={500} disabled={busy} className={`${area} mt-1 font-normal`} />
      </label>

      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy || toMinutes(start) === null || toMinutes(end) === null} className={primary}>
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Yadda saxla
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

const lineKinds = [
  { id: 1, label: "Kəsinti (səbəb məcburi)" },
  { id: 2, label: "Avans (qismən əvvəlcədən verilən)" },
  { id: 3, label: "Ödəniş (maaş verildi)" },
  { id: 4, label: "Mükafat (əlavə)" },
];

export function PayLineForm({ userId, month, remaining }: { userId: string; month: string; remaining: number }) {
  const { busy, error, done, run } = useDo();
  const [kind, setKind] = useState(3);
  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");

  async function add() {
    const ok = await run(() => addPayLine(userId, month, kind, Number(amount.replace(",", ".")), note), "Sətir əlavə olundu. İşçiyə bildiriş getdi.");
    if (ok) {
      setAmount("");
      setNote("");
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void add();
      }}
      className="space-y-3 border-t border-line p-4"
    >
      <div className="grid gap-3 sm:grid-cols-[1fr_140px]">
        <label className="block text-sm font-bold">
          Nə edilir
          <select value={kind} onChange={(e) => setKind(Number(e.target.value))} disabled={busy} className={`${field} mt-1 font-normal`}>
            {lineKinds.map((k) => (
              <option key={k.id} value={k.id}>
                {k.label}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-bold">
          Məbləğ (AZN)
          <input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" disabled={busy} className={`${field} mt-1 font-normal`} />
        </label>
      </div>
      <label className="block text-sm font-bold">
        Qeyd {kind === 1 ? "(səbəb)" : "(istəyə görə)"}
        <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={300} disabled={busy} className={`${field} mt-1 font-normal`} />
      </label>
      {(kind === 2 || kind === 3) && remaining > 0 && (
        <button type="button" onClick={() => setAmount(String(remaining / 100))} className="text-xs font-bold text-primary hover:underline">
          Qalan məbləği yaz ({(remaining / 100).toFixed(2)} AZN)
        </button>
      )}
      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy || !amount.trim()} className={primary}>
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Əlavə et
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

export function VoidPayLineButton({ id }: { id: string }) {
  const { busy, error, run } = useDo();
  return (
    <span className="flex flex-col items-end">
      <button type="button" disabled={busy} onClick={() => void run(() => voidPayLine(id))} className="text-xs font-bold text-danger hover:underline disabled:opacity-60">
        {busy ? "…" : "Ləğv et"}
      </button>
      {error && <span className="text-[11px] text-danger">{error}</span>}
    </span>
  );
}
