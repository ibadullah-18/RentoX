"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { saveTariff, type TariffInput } from "@/app/admin/(panel)/tariff-actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

type Field = { key: keyof TariffInput; label: string; hint: string; suffix: string };

const fields: Field[] = [
  { key: "freeActiveListingLimit", label: "Pulsuz aktiv elan limiti", hint: "Bir istifadəçinin eyni anda neçə elanı pulsuz aktiv ola bilər", suffix: "elan" },
  { key: "activationFee", label: "Aktivləşdirmə haqqı", hint: "Limit dolandan sonra hər yeni aktiv elan (və yenilənmə) üçün", suffix: "AZN" },
  { key: "bumpPrice", label: "Siyahıda yuxarı qaldırmaq", hint: "Elanı siyahının əvvəlinə çıxarmaq", suffix: "AZN" },
  { key: "vipPrice", label: "VIP", hint: "VIP olaraq irəli çəkmək", suffix: "AZN" },
  { key: "vipDurationDays", label: "VIP müddəti", hint: "VIP-in nə qədər davam etməsi", suffix: "gün" },
];

/** The five prices; saving puts them in force at once and keeps the old ones in the history. */
export function TariffForm({ current }: { current: TariffInput }) {
  const router = useRouter();
  const [values, setValues] = useState<Record<keyof TariffInput, string>>({
    freeActiveListingLimit: String(current.freeActiveListingLimit),
    activationFee: String(current.activationFee),
    bumpPrice: String(current.bumpPrice),
    vipPrice: String(current.vipPrice),
    vipDurationDays: String(current.vipDurationDays),
  });
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const parsed: TariffInput = {
    freeActiveListingLimit: Number(values.freeActiveListingLimit.replace(",", ".")),
    activationFee: Number(values.activationFee.replace(",", ".")),
    bumpPrice: Number(values.bumpPrice.replace(",", ".")),
    vipPrice: Number(values.vipPrice.replace(",", ".")),
    vipDurationDays: Number(values.vipDurationDays.replace(",", ".")),
  };
  const changed = (Object.keys(parsed) as (keyof TariffInput)[]).some((k) => parsed[k] !== current[k]);

  async function save() {
    setBusy(true);
    setError(null);
    setDone(false);
    const res = await saveTariff(parsed);
    setBusy(false);
    setAsking(false);
    if (res.ok) {
      setDone(true);
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (changed) setAsking(true);
      }}
      className="space-y-4 rounded-xl border border-line bg-surface p-4 md:p-5"
    >
      <div className="grid gap-4 sm:grid-cols-2">
        {fields.map((f) => (
          <label key={f.key} className="block">
            <span className="mb-1 block text-sm font-bold">{f.label}</span>
            <div className="flex items-center gap-2">
              <input
                value={values[f.key]}
                onChange={(e) => {
                  setValues((v) => ({ ...v, [f.key]: e.target.value }));
                  setDone(false);
                }}
                inputMode="decimal"
                disabled={busy}
                className={input}
              />
              <span className="w-10 shrink-0 text-sm text-ink-2">{f.suffix}</span>
            </div>
            <span className="mt-1 block text-xs text-ink-2">{f.hint}</span>
          </label>
        ))}
      </div>

      <p className="rounded-lg bg-warning-soft px-3 py-2 text-xs text-ink">
        Yeni qiymət dərhal qüvvəyə minir (saytda və mobil tətbiqdə bir neçə saniyəyə). Artıq ödənilmiş əməliyyatlara təsir etmir.
      </p>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !changed}
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
        {done && <p role="status" className="text-sm font-semibold text-success">Qiymətlər yeniləndi.</p>}
      </div>

      <ConfirmDialog
        open={asking}
        title="Qiymətlər dəyişdirilsin?"
        message="Yeni qiymətlər dərhal bütün istifadəçilər üçün qüvvəyə minəcək."
        confirmLabel="Dəyişdir"
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void save()}
      />
    </form>
  );
}
