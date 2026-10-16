"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { updateStoreInfo } from "@/app/magaza/actions";
import { useT } from "@/lib/i18n/client";

const input =
  "w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

/** The name and the description of a live store: they can be rewritten at any time, the store stays live. */
export function StoreInfoForm({ name, description }: { name: string; description: string }) {
  const t = useT();
  const router = useRouter();
  const [v, setV] = useState({ name, description });
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const changed = v.name.trim() !== name || v.description.trim() !== description;
  const valid = v.name.trim().length >= 2 && v.description.trim().length >= 10;

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setMessage(null);
    const res = await updateStoreInfo(v.name, v.description);
    setBusy(false);
    if (res.ok) {
      setMessage({ ok: true, text: t("Saxlandı.") });
      router.refresh();
    } else if (res.unauthorized) router.push("/giris?next=/magaza");
    else setMessage({ ok: false, text: res.message });
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-2xl border border-line bg-surface p-5">
      <div>
        <h2 className="text-lg font-extrabold">{t("Ad və təsvir")}</h2>
        <p className="text-sm text-ink-2">{t("Mağazanın adını və təsvirini istənilən vaxt dəyişə bilərsiniz. Mağaza aktiv qalır.")}</p>
      </div>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">{t("Mağazanın adı")}</span>
        <input
          value={v.name}
          onChange={(e) => {
            setV((s) => ({ ...s, name: e.target.value }));
            setMessage(null);
          }}
          maxLength={100}
          disabled={busy}
          className={`${input} h-11`}
        />
      </label>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">{t("Təsvir")}</span>
        <textarea
          value={v.description}
          onChange={(e) => {
            setV((s) => ({ ...s, description: e.target.value }));
            setMessage(null);
          }}
          rows={4}
          maxLength={2000}
          disabled={busy}
          className={`${input} py-3`}
        />
      </label>
      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !changed || !valid}
          className="flex h-11 items-center gap-2 rounded-xl bg-primary px-6 font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {t("Saxla")}
        </button>
        {message && (
          <p role="status" className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
            {message.text}
          </p>
        )}
      </div>
    </form>
  );
}
