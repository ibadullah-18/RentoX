"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { adjustWallet } from "@/app/admin/(panel)/wallet-actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";

const input =
  "w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

type Kind = { id: string; label: string; credit: boolean; type: number };

const kinds: Kind[] = [
  { id: "bonus", label: "Bonus əlavə et", credit: true, type: 5 },
  { id: "refund", label: "Geri qaytarma (əlavə et)", credit: true, type: 4 },
  { id: "plus", label: "Düzəliş: artır", credit: true, type: 6 },
  { id: "minus", label: "Düzəliş: azalt", credit: false, type: 6 },
];

/** Adds money to or takes it from this person's wallet. A reason is required and the person sees it in their history. */
export function WalletAdjustForm({ userId, balance, currency }: { userId: string; balance: number; currency: string }) {
  const router = useRouter();
  const [kindId, setKindId] = useState("bonus");
  const [amount, setAmount] = useState("");
  const [reason, setReason] = useState("");
  // One per form: sending the same one twice (a double click) changes the wallet once.
  const [requestId, setRequestId] = useState(() => crypto.randomUUID());
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<string | null>(null);

  const kind = kinds.find((k) => k.id === kindId) ?? kinds[0];
  const value = Number(amount.replace(",", "."));
  const valid = Number.isFinite(value) && value > 0 && value <= 1000 && Math.round(value * 100) / 100 === value && reason.trim().length >= 5;
  const after = kind.credit ? balance + value : balance - value;

  async function send() {
    setBusy(true);
    setError(null);
    setDone(null);
    const res = await adjustWallet(userId, requestId, kind.credit, kind.type, value, reason);
    setBusy(false);
    setAsking(false);
    if (res.ok) {
      setDone(`${kind.credit ? "Əlavə olundu" : "Çıxıldı"}: ${value.toFixed(2)} ${currency}. Yeni balans: ${res.balanceAfter.toFixed(2)} ${currency}.`);
      setAmount("");
      setReason("");
      setRequestId(crypto.randomUUID());
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (valid) setAsking(true);
      }}
      className="space-y-3 p-4"
    >
      <div className="grid gap-3 sm:grid-cols-[1fr_140px]">
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Əməliyyat</span>
          <select value={kindId} onChange={(e) => setKindId(e.target.value)} disabled={busy} className={`${input} h-10`}>
            {kinds.map((k) => (
              <option key={k.id} value={k.id}>
                {k.label}
              </option>
            ))}
          </select>
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Məbləğ ({currency})</span>
          <input value={amount} onChange={(e) => setAmount(e.target.value)} inputMode="decimal" placeholder="0.00" disabled={busy} className={`${input} h-10`} />
        </label>
      </div>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Səbəb</span>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={2}
          maxLength={300}
          placeholder="Məsələn: səhv ödəniş geri qaytarıldı (müraciət №…)"
          disabled={busy}
          className={`${input} py-2`}
        />
        <span className="mt-1 block text-xs text-ink-2">İstifadəçi bu mətni öz əməliyyat tarixçəsində görür. Ən azı 5 simvol. Bir əməliyyatda ən çoxu 1000 {currency}.</span>
      </label>
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !valid}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Tətbiq et
        </button>
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
      </div>

      <ConfirmDialog
        open={asking}
        title={kind.credit ? "Məbləğ əlavə edilsin?" : "Məbləğ çıxılsın?"}
        message={`${kind.label}: ${valid ? value.toFixed(2) : ""} ${currency}. Balans ${balance.toFixed(2)} → ${valid ? after.toFixed(2) : ""} ${currency}. Bu əməliyyat tarixçədə qeydə alınır və geri alına bilməz (yalnız əks əməliyyatla).`}
        confirmLabel="Təsdiq et"
        destructive={!kind.credit}
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void send()}
      />
    </form>
  );
}
