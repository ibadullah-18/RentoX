"use client";

import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, Send } from "lucide-react";
import { submitStore } from "@/app/magaza/actions";

/** Sends the store for review. It needs a logo, so the button says so when there is none. */
export function SubmitStore({ hasLogo }: { hasLogo: boolean }) {
  const t = useT();
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function go() {
    setBusy(true);
    setError(null);
    const result = await submitStore();
    setBusy(false);
    if (result.ok) router.refresh();
    else if (result.unauthorized) router.push("/giris?next=/magaza");
    else setError(result.message);
  }

  return (
    <div className="space-y-2">
      <button
        type="button"
        onClick={go}
        disabled={busy || !hasLogo}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-success font-bold text-white hover:opacity-90 disabled:opacity-50"
      >
        {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
        {t("Moderasiyaya göndər")}</button>
      {!hasLogo && <p className="text-sm text-ink-2">{t("Göndərmək üçün əvvəlcə loqo əlavə edin.")}</p>}
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  );
}
