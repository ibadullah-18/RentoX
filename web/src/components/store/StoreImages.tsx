"use client";

import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { ImagePlus, Loader2, Store, Trash2 } from "lucide-react";
import { removeStoreImage } from "@/app/magaza/actions";
import { StoreRules } from "@/lib/store-form";

type Kind = "logo" | "cover";

const TYPES = ["image/jpeg", "image/png", "image/webp"];

/** The owner's logo and cover: shown through the site's own route (not public yet), with upload and remove. */
export function StoreImages({
  storeId,
  hasLogo,
  hasCover,
  editable,
}: {
  storeId: string;
  hasLogo: boolean;
  hasCover: boolean;
  editable: boolean;
}) {
  const t = useT();
  const router = useRouter();
  const [version, setVersion] = useState(0);
  const [busy, setBusy] = useState<Kind | null>(null);
  const [error, setError] = useState<string | null>(null);
  const logoInput = useRef<HTMLInputElement>(null);
  const coverInput = useRef<HTMLInputElement>(null);

  async function upload(kind: Kind, file: File | undefined) {
    if (!file) return;
    const limit = kind === "logo" ? StoreRules.logoMaxBytes : StoreRules.coverMaxBytes;
    if (!TYPES.includes(file.type)) return setError(t("Yalnız JPEG, PNG və WebP şəkilləri qəbul olunur."));
    if (file.size === 0 || file.size > limit) {
      return setError(t("Şəkil ən çoxu {n} MB ola bilər.", { n: limit / 1024 / 1024 }));
    }

    setError(null);
    setBusy(kind);
    const form = new FormData();
    form.set("file", file, file.name);
    try {
      const res = await fetch(`/bff/stores/mine/${kind}`, { method: "POST", body: form });
      if (res.status === 401) return router.push("/giris?next=/magaza");
      if (!res.ok) return setError(t("Şəkil yüklənmədi. Yenidən cəhd edin."));
      setVersion((v) => v + 1);
      router.refresh();
    } catch {
      setError(t("Serverə qoşulmaq olmadı."));
    } finally {
      setBusy(null);
    }
  }

  async function remove(kind: Kind) {
    setBusy(kind);
    setError(null);
    const res = await removeStoreImage(kind);
    setBusy(null);
    if (res.ok) {
      setVersion((v) => v + 1);
      router.refresh();
    } else if (res.unauthorized) router.push("/giris?next=/magaza");
    else setError(res.message);
  }

  const src = (kind: Kind) => `/bff/store-image/${storeId}/${kind}?v=${version}`;
  const button =
    "flex h-10 items-center gap-2 rounded-lg border border-line bg-surface px-3 text-sm font-semibold hover:border-primary disabled:opacity-60";

  return (
    <div className="space-y-3">
      <div className="relative aspect-[3/1] overflow-hidden rounded-xl bg-primary-soft sm:aspect-[4/1]">
        {hasCover ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={src("cover")} alt={t("Üz qabığı")} className="size-full object-cover" />
        ) : (
          <div className="flex size-full items-center justify-center text-sm font-semibold text-primary">
            {t("Üz qabığı yoxdur")}</div>
        )}
        {busy === "cover" && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/40 text-white">
            <Loader2 className="size-8 animate-spin" aria-hidden />
          </div>
        )}
      </div>
      {editable && (
        <div className="flex flex-wrap gap-2">
          <button type="button" disabled={busy !== null} onClick={() => coverInput.current?.click()} className={button}>
            <ImagePlus className="size-4" aria-hidden />
            {t("Üz qabığı seç")}</button>
          {hasCover && (
            <button type="button" disabled={busy !== null} onClick={() => remove("cover")} className={`${button} text-danger`}>
              <Trash2 className="size-4" aria-hidden />
              {t("Sil")}</button>
          )}
          <input
            ref={coverInput}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            hidden
            onChange={(e) => {
              void upload("cover", e.target.files?.[0]);
              e.target.value = "";
            }}
          />
        </div>
      )}

      <div className="flex items-center gap-4">
        <span className="relative flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-primary-soft text-primary">
          {hasLogo ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={src("logo")} alt={t("Loqo")} className="size-full object-cover" />
          ) : (
            <Store className="size-9" aria-hidden />
          )}
          {busy === "logo" && (
            <span className="absolute inset-0 flex items-center justify-center bg-black/40 text-white">
              <Loader2 className="size-7 animate-spin" aria-hidden />
            </span>
          )}
        </span>
        {editable && (
          <div className="flex flex-wrap gap-2">
            <button type="button" disabled={busy !== null} onClick={() => logoInput.current?.click()} className={button}>
              <ImagePlus className="size-4" aria-hidden />
              {t("Loqo seç")}</button>
            {hasLogo && (
              <button type="button" disabled={busy !== null} onClick={() => remove("logo")} className={`${button} text-danger`}>
                <Trash2 className="size-4" aria-hidden />
                {t("Sil")}</button>
            )}
            <input
              ref={logoInput}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              hidden
              onChange={(e) => {
                void upload("logo", e.target.files?.[0]);
                e.target.value = "";
              }}
            />
          </div>
        )}
      </div>

      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  );
}
