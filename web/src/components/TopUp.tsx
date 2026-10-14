"use client";

import { useT } from "@/lib/i18n/client";
import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Plus } from "lucide-react";
import { demoTopUp } from "@/app/pul-kisesi/actions";

const AMOUNTS = [10, 20, 50];

/** Demo top-up. The same key is kept until it succeeds, so a retry never charges twice. */
export function TopUp() {
  const t = useT();
  const router = useRouter();
  const [amount, setAmount] = useState(20);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
  const key = useRef<string | null>(null);

  async function go() {
    if (busy) return;
    setBusy(true);
    setMessage(null);
    key.current ??= crypto.randomUUID();
    const result = await demoTopUp(amount, key.current);
    setBusy(false);
    if (result.ok) {
      key.current = null;
      setMessage({ ok: true, text: t("Balans artırıldı.") });
      router.refresh();
    } else if (result.unauthorized) {
      router.push("/giris?next=/pul-kisesi");
    } else {
      setMessage({ ok: false, text: result.message });
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        {AMOUNTS.map((a) => (
          <button
            key={a}
            type="button"
            disabled={busy}
            aria-pressed={amount === a}
            onClick={() => {
              setAmount(a);
              key.current = null;
            }}
            className={`rounded-lg border px-5 py-2.5 font-semibold ${
              amount === a ? "border-primary bg-primary text-white" : "border-line bg-surface hover:border-primary"
            }`}
          >
            {a} {t("AZN")}</button>
        ))}
      </div>
      <button
        type="button"
        onClick={go}
        disabled={busy}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-line bg-surface font-bold hover:border-primary disabled:opacity-60 sm:w-auto sm:px-8"
      >
        {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Plus className="size-5" aria-hidden />}
        {t("Balansı artır (sınaq)")}</button>
      {message && (
        <p
          role={message.ok ? "status" : "alert"}
          className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}
        >
          {message.text}
        </p>
      )}
      <p className="text-xs text-ink-2">{t("Sınaq rejimi: real pul çıxmır.")}</p>
    </div>
  );
}
