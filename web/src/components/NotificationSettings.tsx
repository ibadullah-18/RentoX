"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { setAnnouncements } from "@/app/kabinet/profil/actions";
import { useT } from "@/lib/i18n/client";

/** One switch: the general messages from RentoX. Replies to your requests and notes on your listings are not affected. */
export function NotificationSettings({ initial }: { initial: boolean }) {
  const t = useT();
  const router = useRouter();
  const [on, setOn] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function toggle() {
    const next = !on;
    setOn(next);
    setBusy(true);
    setError(null);
    const res = await setAnnouncements(next);
    setBusy(false);
    if (res.ok) return;
    setOn(!next);
    if (res.unauthorized) router.push("/giris?next=/kabinet/profil");
    else setError(res.message);
  }

  return (
    <section className="rounded-xl border border-line bg-surface p-4">
      <h2 className="text-[15px] font-extrabold">{t("Bildirişlər")}</h2>
      <div className="mt-3 flex items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="text-sm font-semibold">{t("Saytdan gələn ümumi xəbərlər")}</p>
          <p className="mt-0.5 text-xs text-ink-2">
            {t("Yeniliklər və hamıya göndərilən məlumatlar. Sizə yazılan şəxsi mesajlar və elanlarınız barədə bildirişlər həmişə gəlir.")}
          </p>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={on}
          aria-label={t("Saytdan gələn ümumi xəbərlər")}
          disabled={busy}
          onClick={() => void toggle()}
          className={`relative h-6 w-11 shrink-0 rounded-full transition-colors disabled:opacity-60 ${on ? "bg-primary" : "bg-line"}`}
        >
          <span className={`absolute left-0.5 top-0.5 size-5 rounded-full bg-white shadow transition-transform ${on ? "translate-x-5" : ""}`} />
        </button>
      </div>
      {error && (
        <p role="alert" className="mt-2 text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </section>
  );
}
