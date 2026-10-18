"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Crown, Loader2, Pause, Pencil, Play, RefreshCw, Rocket, Send, Trash2, Wallet } from "lucide-react";
import {
  deactivate,
  payAndActivate,
  promote,
  reactivate,
  remove,
  renew,
  reopen,
  submit,
  type Done,
} from "@/app/elanlarim/actions";
import type { Prices } from "@/lib/tariffs";
import { formatNumber } from "@/lib/format";
import { Status } from "@/lib/types";
import { ConfirmDialog } from "./ConfirmDialog";

type Ask = "deactivate" | "reopen" | "delete" | null;

const button =
  "flex h-11 w-full items-center justify-center gap-2 rounded-xl px-5 text-[15px] font-bold disabled:opacity-60";
const solid = `${button} bg-primary text-white hover:bg-primary-press`;
const outline = `${button} border border-line bg-surface hover:border-primary`;

/** What the owner may do with the listing, by its status. */
export function ManageActions({
  id,
  status,
  balance,
  prices,
  freeVip = false,
  freeBump = false,
}: {
  id: string;
  /** The effective status (an unrenewed old listing is "expired"). */
  status: number;
  balance: number;
  /** The prices in force (an admin can change them). */
  prices: Prices;
  /** RentoX pays for this person's VIP / bump: they cost nothing. */
  freeVip?: boolean;
  freeBump?: boolean;
}) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
  const [ask, setAsk] = useState<Ask>(null);

  async function run(name: string, call: () => Promise<Done>, success: string) {
    setBusy(name);
    setMessage(null);
    const result = await call();
    setBusy(null);
    setAsk(null);
    if (result.ok) {
      setMessage({ ok: true, text: success });
      router.refresh();
    } else if (result.unauthorized) {
      router.push(`/giris?next=/elanlarim/${id}`);
    } else {
      setMessage({ ok: false, text: result.message });
    }
  }

  const key = () => crypto.randomUUID();
  const spin = (name: string) => busy === name && <Loader2 className="size-5 animate-spin" aria-hidden />;
  const disabled = busy !== null;

  const canPromote = status === Status.active;
  const lacking = (price: number) => balance < price;

  return (
    <div className="space-y-3">
      {message && (
        <p
          role={message.ok ? "status" : "alert"}
          className={`rounded-xl px-4 py-3 text-sm font-semibold ${
            message.ok ? "bg-success-soft text-success" : "bg-danger/10 text-danger"
          }`}
        >
          {message.text}
        </p>
      )}

      {(status === Status.draft || status === Status.rejected) && (
        <>
          <button
            type="button"
            disabled={disabled}
            onClick={() => run("submit", () => submit(id), t("Elan yoxlamaya göndərildi."))}
            className={solid}
          >
            {spin("submit") || <Send className="size-5" aria-hidden />}
            {t("Moderasiyaya göndər")}</button>
          <Link href={`/elanlarim/${id}/redakte`} className={outline}>
            <Pencil className="size-5" aria-hidden />
            {t("Redaktə et")}</Link>
        </>
      )}

      {status === Status.paymentRequired && (
        <button
          type="button"
          disabled={disabled || lacking(prices.activation)}
          onClick={() => run("pay", () => payAndActivate(id), t("Elan aktivləşdirildi."))}
          className={solid}
        >
          {spin("pay") || <Wallet className="size-5" aria-hidden />}
          {prices.activation} {t("AZN ödə və aktiv et")}</button>
      )}

      {canPromote && (
        <>
          <button
            type="button"
            disabled={disabled || (!freeVip && lacking(prices.vip))}
            onClick={() =>
              run("vip", () => promote(id, 2, key()), freeVip ? t("VIP sizə RentoX tərəfindən pulsuz verildi: {n} gün.", { n: prices.vipDays }) : t("Elan {n} gün VIP olaraq irəli çəkildi.", { n: prices.vipDays }))
            }
            className={solid}
          >
            {spin("vip") || <Crown className="size-5" aria-hidden />}
            {freeVip
              ? t("VIP et — pulsuz (RentoX tərəfindən)")
              : t("VIP et — {price} AZN / {days} gün", { price: prices.vip, days: prices.vipDays })}
          </button>
          <button
            type="button"
            disabled={disabled || (!freeBump && lacking(prices.bump))}
            onClick={() => run("bump", () => promote(id, 1, key()), freeBump ? t("Elan RentoX tərəfindən pulsuz yuxarı qaldırıldı.") : t("Elan siyahıda yuxarı qaldırıldı."))}
            className={outline}
          >
            {spin("bump") || <Rocket className="size-5" aria-hidden />}
            {freeBump ? t("Yuxarı qaldır — pulsuz (RentoX tərəfindən)") : t("Yuxarı qaldır — {price} AZN", { price: prices.bump })}
          </button>
          {(freeVip || freeBump) && (
            <p className="rounded-xl bg-primary-soft px-4 py-3 text-sm font-semibold text-primary">
              {t("VIP və yuxarı qaldırma sizdə RentoX tərəfindən pulsuz verilir.")}
            </p>
          )}
          {((!freeBump && lacking(prices.bump)) || (!freeVip && lacking(prices.vip))) && (
            <p className="text-sm text-ink-2">
              {t("Balansınız {balance} AZN.", { balance: formatNumber(balance, locale) })}{" "}
              <Link href="/pul-kisesi" className="font-bold text-primary hover:underline">
                {t("Pul kisəsini artırın")}</Link>
              .
            </p>
          )}
          <button type="button" disabled={disabled} onClick={() => setAsk("reopen")} className={outline}>
            <Pencil className="size-5" aria-hidden />
            {t("Redaktə et")}</button>
          <button type="button" disabled={disabled} onClick={() => setAsk("deactivate")} className={outline}>
            <Pause className="size-5" aria-hidden />
            {t("Dayandır")}</button>
        </>
      )}

      {status === Status.deactivated && (
        <>
          <button
            type="button"
            disabled={disabled}
            onClick={() => run("reactivate", () => reactivate(id), t("Elan yenidən aktivdir."))}
            className={solid}
          >
            {spin("reactivate") || <Play className="size-5" aria-hidden />}
            {t("Yenidən aktiv et")}</button>
          <button type="button" disabled={disabled} onClick={() => setAsk("reopen")} className={outline}>
            <Pencil className="size-5" aria-hidden />
            {t("Redaktə et")}</button>
        </>
      )}

      {status === Status.expired && (
        <button
          type="button"
          disabled={disabled}
          onClick={() => run("renew", () => renew(id), t("Elan {days} gün yeniləndi.", { days: prices.lifetimeDays }))}
          className={solid}
        >
          {spin("renew") || <RefreshCw className="size-5" aria-hidden />}
          {t("{days} gün yenilə", { days: prices.lifetimeDays })}</button>
      )}

      <button
        type="button"
        disabled={disabled}
        onClick={() => setAsk("delete")}
        className={`${button} text-danger hover:bg-danger/10`}
      >
        <Trash2 className="size-5" aria-hidden />
        {t("Elanı sil")}</button>

      <ConfirmDialog
        open={ask === "deactivate"}
        title={t("Elan dayandırılsın?")}
        message={t("Elan axtarışdan və siyahılardan çıxacaq. İstədiyiniz vaxt yenidən aktiv edə bilərsiniz.")}
        confirmLabel={t("Dayandır")}
        busy={busy === "deactivate"}
        onClose={() => setAsk(null)}
        onConfirm={() => run("deactivate", () => deactivate(id), t("Elan dayandırıldı."))}
      />
      <ConfirmDialog
        open={ask === "reopen"}
        title={t("Redaktə üçün elan qaralamaya qayıdacaq")}
        message={t("Elan siyahılardan çıxacaq, dəyişikliklərdən sonra yenidən yoxlamaya göndərməlisiniz.")}
        confirmLabel={t("Davam et")}
        busy={busy === "reopen"}
        onClose={() => setAsk(null)}
        onConfirm={async () => {
          setBusy("reopen");
          const result = await reopen(id);
          setBusy(null);
          setAsk(null);
          if (result.ok) router.push(`/elanlarim/${id}/redakte`);
          else setMessage({ ok: false, text: result.ok ? "" : result.message });
        }}
      />
      <ConfirmDialog
        open={ask === "delete"}
        title={t("Elan silinsin?")}
        message={t("Bu əməliyyatı geri qaytarmaq olmaz.")}
        confirmLabel="Sil"
        destructive
        busy={busy === "delete"}
        onClose={() => setAsk(null)}
        onConfirm={async () => {
          setBusy("delete");
          const result = await remove(id);
          setBusy(null);
          setAsk(null);
          if (result.ok) router.replace("/elanlarim");
          else setMessage({ ok: false, text: result.ok ? "" : result.message });
        }}
      />
    </div>
  );
}
