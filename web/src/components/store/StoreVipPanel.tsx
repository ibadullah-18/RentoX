"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Crown, Gift, Loader2 } from "lucide-react";
import { buyStoreVip } from "@/app/magaza/actions";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { useT } from "@/lib/i18n/client";
import type { StoreVipStatus } from "@/lib/types";

/** dd.mm.yyyy from the UTC date: written by hand so the server and the browser print the same text. */
const day = (iso: string) => `${iso.slice(8, 10)}.${iso.slice(5, 7)}.${iso.slice(0, 4)}`;

/**
 * The offer on the owner's store page: put the store on the home page for the price and time the admin set,
 * paid from the wallet. While it runs it shows until when, and the owner can add another period.
 */
export function StoreVipPanel({ vip, leftMs }: { vip: StoreVipStatus; leftMs: number }) {
  const t = useT();
  const router = useRouter();
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!vip.isOnSale && !vip.isVip) return null;

  const totalHours = Math.max(0, Math.floor(leftMs / 3_600_000));
  const left = totalHours >= 24 ? t("{days} gün {hours} saat qalıb", { days: Math.floor(totalHours / 24), hours: totalHours % 24 }) : t("{hours} saat qalıb", { hours: totalHours });
  const price = `${vip.price.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: 2 })} ${vip.currency}`;

  async function buy() {
    setBusy(true);
    setError(null);
    const res = await buyStoreVip(crypto.randomUUID());
    setBusy(false);
    setAsking(false);
    if (res.ok) router.refresh();
    else if (res.unauthorized) router.push("/giris?next=/magaza");
    else setError(res.message);
  }

  return (
    <section className="space-y-3 rounded-2xl border border-warning/40 bg-warning-soft/40 p-5">
      <div className="flex items-start gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-warning/20 text-warning">
          <Crown className="size-5 fill-warning" aria-hidden />
        </span>
        <div className="min-w-0 flex-1">
          <h2 className="text-lg font-extrabold">{vip.name}</h2>
          {vip.isVip && vip.vipUntilUtc ? (
            <div className="space-y-1 text-sm">
              <p className="font-semibold text-success">{t("Mağazanız ana səhifədə göstərilir: {date} tarixinədək", { date: day(vip.vipUntilUtc) })}</p>
              <p className="inline-flex rounded-md bg-warning-soft px-2 py-0.5 font-extrabold text-warning">{left}</p>
              {vip.isGift && (
                <p className="inline-flex items-center gap-1.5 rounded-md bg-primary-soft px-2 py-0.5 font-bold text-primary">
                  <Gift className="size-4" aria-hidden />
                  {t("RentoX tərəfindən hədiyyə")}
                </p>
              )}
              <p className="text-ink-2">{t("VIP mağaza ana səhifədə, bütün elanlardan əvvəl göstərilir. Müddət bitəndən sonra adi mağaza kimi qalır; vaxtında uzatsanız fasilə olmur.")}</p>
            </div>
          ) : (
            <p className="text-sm text-ink-2">{vip.offerMessage || t("Mağazanızı ana səhifədə göstərin və daha çox müştəri qazanın.")}</p>
          )}
        </div>
      </div>

      {vip.isOnSale && (
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="button"
            onClick={() => setAsking(true)}
            disabled={busy}
            className="flex h-11 items-center gap-2 rounded-xl bg-primary px-5 font-bold text-white hover:bg-primary-press disabled:opacity-60"
          >
            {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {vip.isVip ? t("Müddəti uzat") : t("VIP et")} · {vip.freeServices ? t("Pulsuz (RentoX tərəfindən)") : price}
          </button>
          <span className="text-sm text-ink-2">{t("{days} gün", { days: vip.durationDays })}</span>
          <Link href="/pul-kisesi" className="text-sm font-bold text-primary hover:underline">
            {t("Pul kisəsi")}
          </Link>
        </div>
      )}
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}

      <ConfirmDialog
        open={asking}
        title={vip.name}
        message={
          vip.freeServices
            ? t("Bu sizə RentoX tərəfindən pulsuz verilir. Pul kisənizdən heç nə çıxılmayacaq, mağaza {days} gün ana səhifədə göstəriləcək.", { days: vip.durationDays })
            : t("Pul kisənizdən {price} çıxılacaq və mağaza {days} gün ana səhifədə göstəriləcək.", { price, days: vip.durationDays })
        }
        confirmLabel={vip.freeServices ? t("Pulsuz götür") : t("Ödə")}
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void buy()}
      />
    </section>
  );
}
