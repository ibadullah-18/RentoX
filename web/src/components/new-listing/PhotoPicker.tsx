"use client";

import { useT } from "@/lib/i18n/client";
import { useEffect, useRef, useState } from "react";
import { ImagePlus, Star, X } from "lucide-react";

export const MAX_PHOTOS = 15;
export const MAX_BYTES = 10 * 1024 * 1024;
const TYPES = ["image/jpeg", "image/png", "image/webp"];

export type Photo = { id: string; file: File; url: string };

/** Picks photos (click or drag); the first one is the cover. */
export function PhotoPicker({
  photos,
  disabled,
  onChange,
  max = MAX_PHOTOS,
}: {
  photos: Photo[];
  disabled: boolean;
  onChange: (next: Photo[]) => void;
  /** How many photos may be added here (less when others are already saved). */
  max?: number;
}) {
  const t = useT();
  const input = useRef<HTMLInputElement>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [over, setOver] = useState(false);
  const current = useRef(photos);
  useEffect(() => {
    current.current = photos;
  });

  // Preview addresses are freed when the page is left.
  useEffect(
    () => () => current.current.forEach((p) => URL.revokeObjectURL(p.url)),
    [],
  );

  function add(files: FileList | File[]) {
    const accepted: Photo[] = [];
    let skipped: string | null = null;

    for (const file of Array.from(files)) {
      if (photos.length + accepted.length >= max) {
        skipped = t("Ən çoxu {n} şəkil olar.", { n: MAX_PHOTOS });
        break;
      }
      if (!TYPES.includes(file.type)) {
        skipped = t("Yalnız JPEG, PNG və WebP şəkilləri qəbul olunur.");
        continue;
      }
      if (file.size === 0 || file.size > MAX_BYTES) {
        skipped = t("Hər şəkil ən çoxu 10 MB ola bilər.");
        continue;
      }
      accepted.push({
        id: crypto.randomUUID(),
        file,
        url: URL.createObjectURL(file),
      });
    }
    setNotice(skipped);
    if (accepted.length > 0) onChange([...photos, ...accepted]);
  }

  function remove(id: string) {
    const gone = photos.find((p) => p.id === id);
    if (gone) URL.revokeObjectURL(gone.url);
    onChange(photos.filter((p) => p.id !== id));
  }

  function makeCover(id: string) {
    const picked = photos.find((p) => p.id === id);
    if (picked) onChange([picked, ...photos.filter((p) => p.id !== id)]);
  }

  return (
    <div className="space-y-3">
      <div
        onDragOver={(e) => {
          e.preventDefault();
          if (!disabled) setOver(true);
        }}
        onDragLeave={() => setOver(false)}
        onDrop={(e) => {
          e.preventDefault();
          setOver(false);
          if (!disabled) add(e.dataTransfer.files);
        }}
        className={`rounded-xl border-2 border-dashed p-4 transition-colors ${
          over ? "border-primary bg-primary-soft" : "border-line bg-surface"
        }`}
      >
        <ul className="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-5">
          {photos.map((p, i) => (
            <li key={p.id} className="relative aspect-square overflow-hidden rounded-lg bg-primary-soft">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={p.url} alt={t("Şəkil {n}", { n: i + 1 })} className="size-full object-cover" />
              {i === 0 ? (
                <span className="absolute bottom-1 left-1 flex items-center gap-1 rounded-md bg-ink/70 px-1.5 py-0.5 text-[11px] font-bold text-white">
                  <Star className="size-3 fill-warning text-warning" aria-hidden />
                  {t("Əsas")}</span>
              ) : (
                <button
                  type="button"
                  disabled={disabled}
                  onClick={() => makeCover(p.id)}
                  className="absolute bottom-1 left-1 rounded-md bg-ink/70 px-1.5 py-0.5 text-[11px] font-bold text-white hover:bg-ink"
                >
                  {t("Əsas et")}</button>
              )}
              <button
                type="button"
                disabled={disabled}
                onClick={() => remove(p.id)}
                aria-label={t("Şəkli sil")}
                className="absolute right-1 top-1 flex size-7 items-center justify-center rounded-full bg-ink/70 text-white hover:bg-ink"
              >
                <X className="size-4" />
              </button>
            </li>
          ))}
          {photos.length < max && (
            <li>
              <button
                type="button"
                disabled={disabled}
                onClick={() => input.current?.click()}
                className="flex aspect-square w-full flex-col items-center justify-center gap-1 rounded-lg border border-line text-primary hover:border-primary"
              >
                <ImagePlus className="size-7" aria-hidden />
                <span className="text-xs font-semibold">{t("Şəkil əlavə et")}</span>
              </button>
            </li>
          )}
        </ul>
        <input
          ref={input}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          hidden
          onChange={(e) => {
            if (e.target.files) add(e.target.files);
            e.target.value = "";
          }}
        />
      </div>
      <p className="text-xs text-ink-2">
        {photos.length} / {max} {t("· JPEG, PNG, WebP · hər biri ən çoxu 10 MB. Birinci şəkil elanın üz qabığıdır.")}</p>
      {notice && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {notice}
        </p>
      )}
    </div>
  );
}
