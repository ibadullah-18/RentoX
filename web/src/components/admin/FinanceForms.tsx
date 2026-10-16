"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { addExpense, saveFinanceProfile, voidExpense, type ProfileInput } from "@/app/admin/(panel)/finance-actions";
import { adminHref } from "@/lib/admin-path";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

const categoryOptions: [string, string][] = [
  ["1", "Büdcə"],
  ["2", "Vergi"],
  ["3", "Digər xərc"],
  ["4", "Şəxsi xərc"],
];

/** Writes down one expense: the kind, what it was for, how much and on which day. The owner only. */
export function ExpenseForm({ today }: { today: string }) {
  const router = useRouter();
  const [category, setCategory] = useState("1");
  const [title, setTitle] = useState("");
  const [amount, setAmount] = useState("");
  const [day, setDay] = useState(today);
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    const res = await addExpense({ category: Number(category), title, amount: Number(amount.replace(",", ".")), day, note });
    setBusy(false);
    if (res.ok) {
      setTitle("");
      setAmount("");
      setNote("");
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border border-line bg-surface p-4">
      <h2 className="font-extrabold">Xərc yaz</h2>
      <div className="grid gap-3 sm:grid-cols-4">
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Növ</span>
          <select value={category} onChange={(e) => setCategory(e.target.value)} disabled={busy} className={input}>
            {categoryOptions.map(([k, v]) => (
              <option key={k} value={k}>
                {v}
              </option>
            ))}
          </select>
        </label>
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm font-bold">Nəyə görə</span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={120} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Məbləğ (AZN)</span>
          <input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Tarix</span>
          <input type="date" value={day} max={today} onChange={(e) => setDay(e.target.value)} disabled={busy} className={input} />
        </label>
        <label className="block sm:col-span-3">
          <span className="mb-1 block text-sm font-bold">Qeyd (istəyə görə)</span>
          <input value={note} onChange={(e) => setNote(e.target.value)} maxLength={400} disabled={busy} className={input} />
        </label>
      </div>
      <p className="text-xs text-ink-2">
        “Şəxsi xərc” şirkət xərci sayılmır: aktda ayrıca, xalis gəlirdən sonra göstərilir. Yazılan xərc silinmir, səhv olsa ləğv edilir.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !title.trim() || !amount}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Yaz
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </div>
    </form>
  );
}

/** Voids a mistaken expense (with the reason); the line stays in the list, crossed out. */
export function VoidExpenseButton({ id }: { id: string }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    setBusy(true);
    setError(null);
    const res = await voidExpense(id, reason);
    setBusy(false);
    if (res.ok) {
      setOpen(false);
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  if (!open) {
    return (
      <button type="button" onClick={() => setOpen(true)} className="text-xs font-bold text-danger hover:underline">
        Ləğv et
      </button>
    );
  }

  return (
    <span className="flex flex-wrap items-center gap-1.5">
      <input
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        placeholder="Səbəb"
        maxLength={400}
        className="h-8 w-40 rounded-md border border-line bg-surface px-2 text-xs outline-none focus:border-primary"
      />
      <button
        type="button"
        onClick={() => void confirm()}
        disabled={busy || !reason.trim()}
        className="h-8 rounded-md bg-danger px-2.5 text-xs font-bold text-white disabled:opacity-50"
      >
        Təsdiq
      </button>
      <button type="button" onClick={() => setOpen(false)} className="text-xs font-semibold text-ink-2 hover:underline">
        Bağla
      </button>
      {error && <span className="text-xs font-semibold text-danger">{error}</span>}
    </span>
  );
}

/** Who signs the monthly report: full name, tax number (VÖEN) and how to reach them. The owner only. */
export function ProfileForm({ initial }: { initial: ProfileInput }) {
  const router = useRouter();
  const [v, setV] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
  const set = (k: keyof ProfileInput) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setV((s) => ({ ...s, [k]: e.target.value }));
    setMessage(null);
  };

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    const res = await saveFinanceProfile(v);
    setBusy(false);
    if (res.ok) {
      setMessage({ ok: true, text: "Saxlandı." });
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ ok: false, text: res.message });
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-xl border border-line bg-surface p-4">
      <h2 className="font-extrabold">Aktı imzalayan</h2>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="block sm:col-span-2">
          <span className="mb-1 block text-sm font-bold">Soyad, ad, ata adı</span>
          <input value={v.fullName} onChange={set("fullName")} maxLength={200} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">VÖEN</span>
          <input value={v.voen} onChange={set("voen")} maxLength={200} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Telefon</span>
          <input value={v.phone} onChange={set("phone")} maxLength={200} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">E-poçt</span>
          <input value={v.email} onChange={set("email")} maxLength={200} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Ünvan</span>
          <input value={v.address} onChange={set("address")} maxLength={200} disabled={busy} className={input} />
        </label>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !v.fullName.trim()}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Saxla
        </button>
        {message && (
          <p role="status" className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
            {message.text}
          </p>
        )}
      </div>
    </form>
  );
}
