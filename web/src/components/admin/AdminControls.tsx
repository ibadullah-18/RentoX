"use client";

import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Check, Loader2, X } from "lucide-react";
import type { Done } from "@/app/admin/actions";
import { ConfirmDialog } from "../ConfirmDialog";

/** Approve / reject (with a reason) for a listing or a store. */
export function ModerationActions({
  approve,
  reject,
  backHref,
  noun,
}: {
  approve: () => Promise<Done>;
  reject: (reason: string) => Promise<Done>;
  /** Where to go once it is decided. */
  backHref: string;
  noun: string;
}) {
  const t = useT();
  const router = useRouter();
  const [busy, setBusy] = useState<"approve" | "reject" | null>(null);
  const [asking, setAsking] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function run(kind: "approve" | "reject", call: () => Promise<Done>) {
    setBusy(kind);
    setError(null);
    const result = await call();
    setBusy(null);
    if (result.ok) {
      router.push(backHref);
      router.refresh();
    } else if (result.unauthorized) {
      router.push("/giris");
    } else {
      setError(result.message);
    }
  }

  return (
    <div className="space-y-3">
      {error && (
        <p role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy !== null}
        onClick={() => void run("approve", approve)}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-success font-bold text-white hover:opacity-90 disabled:opacity-60"
      >
        {busy === "approve" ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Check className="size-5" aria-hidden />}
        {t("Təsdiq et")}</button>
      <button
        type="button"
        disabled={busy !== null}
        onClick={() => setAsking(true)}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-danger font-bold text-danger hover:bg-danger/10 disabled:opacity-60"
      >
        <X className="size-5" aria-hidden />
        {t("Rədd et")}</button>

      <ConfirmDialog
        open={asking}
        title={`${noun} rədd edilsin?`}
        message="Rədd səbəbi sahibinə bildiriş kimi göndəriləcək."
        confirmLabel="Rədd et"
        destructive
        busy={busy === "reject"}
        onClose={() => setAsking(false)}
        onConfirm={() => {
          if (!reason.trim()) {
            setError("Rədd səbəbini yazın.");
            setAsking(false);
            return;
          }
          setAsking(false);
          void run("reject", () => reject(reason));
        }}
      />

      <label className="block">
        <span className="mb-1.5 block text-sm font-bold">{t("Rədd səbəbi")}</span>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          maxLength={1000}
          placeholder={t("Rədd edəcəksinizsə, səbəbi yazın")}
          className="w-full resize-none rounded-xl border border-line bg-surface px-4 py-3 text-[16px] outline-none focus:border-primary"
        />
      </label>
    </div>
  );
}
