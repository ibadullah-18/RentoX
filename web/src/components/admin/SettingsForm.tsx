"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { saveSettings } from "@/app/admin/(panel)/settings-actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

/** The adjustable settings; saving puts them in force at once and keeps the old ones in the history. */
export function SettingsForm({ current }: { current: { listingLifetimeDays: number; maxListingPhotos: number } }) {
  const router = useRouter();
  const [days, setDays] = useState(String(current.listingLifetimeDays));
  const [photos, setPhotos] = useState(String(current.maxListingPhotos ?? 15));
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const parsed = Number(days);
  const parsedPhotos = Number(photos);
  const changed = parsed !== current.listingLifetimeDays || parsedPhotos !== (current.maxListingPhotos ?? 15);

  async function save() {
    setBusy(true);
    setError(null);
    setDone(false);
    const res = await saveSettings({ listingLifetimeDays: parsed, maxListingPhotos: parsedPhotos });
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
      <label className="block max-w-sm">
        <span className="mb-1 block text-sm font-bold">Elanın müddəti</span>
        <div className="flex items-center gap-2">
          <input
            value={days}
            onChange={(e) => {
              setDays(e.target.value);
              setDone(false);
            }}
            inputMode="numeric"
            disabled={busy}
            className={input}
          />
          <span className="w-10 shrink-0 text-sm text-ink-2">gün</span>
        </div>
        <span className="mt-1 block text-xs text-ink-2">
          Elan dərc olunandan və ya yenilənəndən sonra neçə gün saytda qalır (7 ilə 365 arası).
        </span>
      </label>

      <label className="block max-w-sm">
        <span className="mb-1 block text-sm font-bold">Elandakı şəkil sayı</span>
        <div className="flex items-center gap-2">
          <input
            value={photos}
            onChange={(e) => {
              setPhotos(e.target.value);
              setDone(false);
            }}
            inputMode="numeric"
            disabled={busy}
            className={input}
          />
          <span className="w-10 shrink-0 text-sm text-ink-2">şəkil</span>
        </div>
        <span className="mt-1 block text-xs text-ink-2">
          Bir elana ən çoxu neçə şəkil yükləmək olar (1 ilə 30 arası). Sayt və mobil tətbiq bu rəqəmi özü götürür. Azaltsanız, artıq yüklənmiş şəkillər silinmir: sadəcə yenisini əlavə etmək olmur.
        </span>
      </label>

      <p className="rounded-lg bg-warning-soft px-3 py-2 text-xs text-ink">
        Yeni müddət yalnız bundan sonra dərc olunan və ya yenilənən elanlara tətbiq olunur. Artıq yayımda olan elanların bitmə tarixi dəyişmir.
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
        {done && <p role="status" className="text-sm font-semibold text-success">Ayarlar yeniləndi.</p>}
      </div>

      <ConfirmDialog
        open={asking}
        title="Ayar dəyişdirilsin?"
        message={`Elan müddəti ${parsed} gün, elandakı şəkil sayı ${parsedPhotos} olacaq. Müddət yeni dərc olunan və yenilənən elanlara, şəkil sayı isə yeni yüklənən şəkillərə tətbiq olunacaq.`}
        confirmLabel="Dəyişdir"
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void save()}
      />
    </form>
  );
}
