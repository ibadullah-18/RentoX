"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Crown, Gift, Loader2, TrendingUp } from "lucide-react";
import { giftListing, giftStoreVip } from "@/app/admin/(panel)/gift-actions";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { adminHref } from "@/lib/admin-path";

type Pending = { kind: "bump" } | { kind: "vip" } | { kind: "store" };

/**
 * The owner gives a VIP or a bump for free, in one click. No money is taken from the person and none is added to the
 * income. The person gets a notification that it is a gift from RentoX.
 */
export function GiftPanel({ target, id, defaultDays }: { target: "listing" | "store"; id: string; defaultDays: number }) {
  const router = useRouter();
  const [days, setDays] = useState(String(defaultDays));
  const [asking, setAsking] = useState<Pending | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const n = Number(days);
  const valid = Number.isInteger(n) && n >= 1 && n <= 365;

  async function confirm() {
    if (!asking) return;
    setBusy(true);
    setMessage(null);
    const res =
      asking.kind === "store"
        ? await giftStoreVip(id, n)
        : await giftListing(id, asking.kind === "vip" ? 2 : 1, asking.kind === "vip" ? n : null);
    setBusy(false);
    setAsking(null);
    if (res.ok) {
      setMessage({ ok: true, text: res.message });
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ ok: false, text: res.message });
  }

  const button =
    "flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-line bg-surface px-3 text-sm font-bold hover:border-primary disabled:opacity-50";

  return (
    <section className="space-y-3 rounded-xl border border-warning/40 bg-warning-soft/40 p-4">
      <div className="flex items-center gap-2">
        <Gift className="size-5 text-warning" aria-hidden />
        <h2 className="font-extrabold">Pulsuz hədiyyə (RentoX-dan)</h2>
      </div>
      <p className="text-xs text-ink-2">
        Sahibindən pul çıxılmır və maliyyədə gəlir yazılmır. Sahibə “RentoX tərəfindən hədiyyə” bildirişi gedir.
      </p>

      <label className="block">
        <span className="mb-1 block text-xs font-bold">VIP müddəti (gün)</span>
        <input
          value={days}
          onChange={(e) => setDays(e.target.value)}
          inputMode="numeric"
          disabled={busy}
          className="h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary"
        />
      </label>

      {target === "listing" ? (
        <div className="grid gap-2">
          <button type="button" disabled={busy || !valid} onClick={() => setAsking({ kind: "vip" })} className={button}>
            <Crown className="size-4 text-warning" aria-hidden />
            VIP hədiyyə et
          </button>
          <button type="button" disabled={busy} onClick={() => setAsking({ kind: "bump" })} className={button}>
            <TrendingUp className="size-4 text-primary" aria-hidden />
            İrəli çək (pulsuz)
          </button>
        </div>
      ) : (
        <button type="button" disabled={busy || !valid} onClick={() => setAsking({ kind: "store" })} className={button}>
          <Crown className="size-4 text-warning" aria-hidden />
          VIP mağaza hədiyyə et
        </button>
      )}

      {busy && <Loader2 className="size-4 animate-spin text-ink-2" aria-hidden />}
      {message && (
        <p role="status" className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
          {message.text}
        </p>
      )}

      <ConfirmDialog
        open={asking !== null}
        title="Pulsuz hədiyyə edilsin?"
        message={
          asking?.kind === "bump"
            ? "Elan pulsuz irəli çəkiləcək. Sahibindən pul çıxılmayacaq."
            : `${n} günlük VIP pulsuz veriləcək. Sahibindən pul çıxılmayacaq və gəlir yazılmayacaq.`
        }
        confirmLabel="Hədiyyə et"
        busy={busy}
        onClose={() => setAsking(null)}
        onConfirm={() => void confirm()}
      />
    </section>
  );
}
