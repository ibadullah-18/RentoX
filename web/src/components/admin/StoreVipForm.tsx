"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Crown, Loader2, Send } from "lucide-react";
import { saveStoreVip, sendStoreVipOffers, type StoreVipInput } from "@/app/admin/(panel)/tariff-actions";
import { adminHref } from "@/lib/admin-path";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

/**
 * The VIP store product: its name, price and length, the text of the offer the owners see, and how often the
 * same owner may be offered again. Saving puts it in force at once; "send the offer" notifies the owners.
 */
export function StoreVipForm({ current }: { current: StoreVipInput }) {
  const router = useRouter();
  const [v, setV] = useState({
    name: current.name,
    price: String(current.price),
    durationDays: String(current.durationDays),
    offerMessage: current.offerMessage,
    offerIntervalHours: String(current.offerIntervalHours),
    isEnabled: current.isEnabled,
  });
  const [busy, setBusy] = useState(false);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  const num = (s: string) => Number(s.replace(",", "."));
  const parsed: StoreVipInput = {
    name: v.name,
    price: num(v.price),
    durationDays: num(v.durationDays),
    offerMessage: v.offerMessage,
    offerIntervalHours: num(v.offerIntervalHours),
    isEnabled: v.isEnabled,
  };
  const changed = JSON.stringify(parsed) !== JSON.stringify(current);

  const set = (key: keyof typeof v) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setV((s) => ({ ...s, [key]: e.target.value }));
    setInfo(null);
  };

  async function save() {
    setBusy(true);
    setError(null);
    setInfo(null);
    const res = await saveStoreVip(parsed);
    setBusy(false);
    if (res.ok) {
      setInfo("VIP mağaza yeniləndi.");
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  async function send() {
    setSending(true);
    setError(null);
    setInfo(null);
    const res = await sendStoreVipOffers();
    setSending(false);
    if (res.ok) setInfo(`Təklif ${res.sent} mağaza sahibinə göndərildi. Tezliklə təklif almış ${res.skipped} sahibinə göndərilmədi.`);
    else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (changed) void save();
      }}
      className="space-y-4 rounded-xl border border-line bg-surface p-4 md:p-5"
    >
      <div className="flex items-center gap-2">
        <Crown className="size-5 text-warning" aria-hidden />
        <h2 className="text-lg font-extrabold">VIP mağaza</h2>
      </div>
      <p className="text-sm text-ink-2">
        Mağaza sahibi bu məbləği pul kisəsindən ödəyib mağazasını göstərilən müddət ana səhifədəki “Mağazalar” bölməsində göstərir. Ödəniş etməyən mağaza orada görünmür.
      </p>

      <label className="flex items-center gap-2 text-sm font-bold">
        <input
          type="checkbox"
          checked={v.isEnabled}
          onChange={(e) => {
            setV((s) => ({ ...s, isEnabled: e.target.checked }));
            setInfo(null);
          }}
          className="size-4 accent-primary"
        />
        Satışda (söndürülsə, alınmır və təklif göstərilmir)
      </label>

      <div className="grid gap-4 sm:grid-cols-3">
        <label className="block sm:col-span-3">
          <span className="mb-1 block text-sm font-bold">Ad</span>
          <input value={v.name} onChange={set("name")} maxLength={60} disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Qiymət (AZN)</span>
          <input value={v.price} onChange={set("price")} inputMode="decimal" disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Müddət (gün)</span>
          <input value={v.durationDays} onChange={set("durationDays")} inputMode="numeric" disabled={busy} className={input} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Təklif aralığı (saat)</span>
          <input value={v.offerIntervalHours} onChange={set("offerIntervalHours")} inputMode="numeric" disabled={busy} className={input} />
        </label>
        <label className="block sm:col-span-3">
          <span className="mb-1 block text-sm font-bold">Təklif mətni</span>
          <textarea
            value={v.offerMessage}
            onChange={set("offerMessage")}
            rows={3}
            maxLength={400}
            disabled={busy}
            placeholder="Boş olsa, qiymət və müddət ilə hazır mətn göndərilir"
            className="w-full rounded-lg border border-line bg-surface px-3 py-2 text-[15px] outline-none focus:border-primary disabled:opacity-60"
          />
          <span className="mt-1 block text-xs text-ink-2">
            Bu mətn mağaza sahibinin “Mağazam” səhifəsində və bildirişdə görünür. Eyni sahibə təklif bu aralıqdan tez təkrar göndərilmir.
          </span>
        </label>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !changed}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Saxla
        </button>
        <button
          type="button"
          onClick={() => void send()}
          disabled={sending || busy || changed || !current.isEnabled}
          title={changed ? "Əvvəlcə dəyişiklikləri saxlayın" : undefined}
          className="flex h-10 items-center gap-2 rounded-lg border border-line px-5 text-sm font-bold hover:border-primary disabled:opacity-50"
        >
          {sending ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Send className="size-4" aria-hidden />}
          Təklifi göndər
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
        {info && (
          <p role="status" className="text-sm font-semibold text-success">
            {info}
          </p>
        )}
      </div>
    </form>
  );
}
