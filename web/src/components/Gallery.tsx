"use client";

import { useT } from "@/lib/i18n/client";
import Image from "next/image";
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { ChevronLeft, ChevronRight, ImageIcon, X } from "lucide-react";
import type { ListingImage } from "@/lib/types";
import { BackButton } from "./BackButton";
import { FavoriteButton } from "./FavoriteButton";
import { ShareButton } from "./ShareButton";

const round =
  "flex size-8 items-center justify-center rounded-full bg-black/30 text-white backdrop-blur-sm transition-colors hover:bg-black/50";

/**
 * The photos of a listing. They swipe sideways (arrows only on a computer);
 * a tap opens them full screen. Back, share and favourite sit on top of the
 * photo, as in the shopping apps.
 */
export function Gallery({
  images,
  title,
  listingId,
  backHref,
}: {
  images: ListingImage[];
  title: string;
  listingId: string;
  backHref: string;
}) {
  const t = useT();
  const track = useRef<HTMLDivElement>(null);
  const [index, setIndex] = useState(0);
  const [open, setOpen] = useState(false);
  const count = images.length;

  const goTo = useCallback((i: number) => {
    const el = track.current;
    if (el) el.scrollTo({ left: i * el.clientWidth, behavior: "smooth" });
  }, []);

  function onScroll() {
    const el = track.current;
    if (el && el.clientWidth > 0) setIndex(Math.round(el.scrollLeft / el.clientWidth));
  }

  const overlay = (
    <>
      <div className="absolute left-3 top-3 z-10">
        <BackButton fallback={backHref} className={round} />
      </div>
      <div className="absolute right-3 top-3 z-10 flex gap-2">
        <ShareButton title={title} className={round} />
        <FavoriteButton listingId={listingId} className={round} plain />
      </div>
    </>
  );

  if (count === 0) {
    return (
      <div className="relative flex aspect-[4/3] items-center justify-center rounded-xl md:aspect-[16/9] bg-primary-soft text-primary">
        {overlay}
        <ImageIcon className="size-12" aria-hidden />
      </div>
    );
  }

  const arrow =
    "absolute top-1/2 z-10 flex size-8 -translate-y-1/2 items-center justify-center rounded-full bg-surface/85 text-ink shadow-sm hover:bg-surface max-md:hidden";

  return (
    <div>
      <div className="relative overflow-hidden rounded-xl bg-primary-soft max-md:-mx-4 max-md:rounded-none">
        <div ref={track} onScroll={onScroll} className="snap-track flex aspect-[4/3] snap-x snap-mandatory overflow-x-auto md:aspect-[16/9]">
          {images.map((img, i) => (
            <button
              key={img.id}
              type="button"
              onClick={() => setOpen(true)}
              aria-label={t("Böyüt")}
              className="relative h-full w-full shrink-0 snap-center cursor-zoom-in"
            >
              <Image
                src={img.url}
                alt={`${title} — ${i + 1}/${count}`}
                fill
                unoptimized
                priority={i === 0}
                sizes="(min-width: 1024px) 60vw, 100vw"
                className="object-cover"
              />
            </button>
          ))}
        </div>

        {overlay}

        {count > 1 && (
          <>
            {index > 0 && (
              <button type="button" onClick={() => goTo(index - 1)} aria-label={t("Əvvəlki şəkil")} className={`${arrow} left-3`}>
                <ChevronLeft className="size-4" />
              </button>
            )}
            {index < count - 1 && (
              <button type="button" onClick={() => goTo(index + 1)} aria-label={t("Növbəti şəkil")} className={`${arrow} right-3`}>
                <ChevronRight className="size-4" />
              </button>
            )}
            <span className="absolute bottom-3 right-3 rounded-full bg-black/40 px-2.5 py-0.5 text-[11px] font-semibold text-white backdrop-blur-sm">
              {index + 1} / {count}
            </span>
          </>
        )}
      </div>

      {count > 1 && (
        <ul className="strip mt-2 max-md:hidden">
          {images.map((img, i) => (
            <li key={img.id} className="shrink-0">
              <button
                type="button"
                onClick={() => goTo(i)}
                aria-label={t("Şəkil {n}", { n: i + 1 })}
                aria-current={i === index}
                className={`relative block size-12 overflow-hidden rounded-md border-2 md:size-14 ${
                  i === index ? "border-primary" : "border-transparent opacity-70"
                }`}
              >
                <Image src={img.url} alt="" fill unoptimized sizes="80px" className="object-cover" />
              </button>
            </li>
          ))}
        </ul>
      )}

      {open && <Viewer images={images} title={title} start={index} onClose={() => setOpen(false)} />}
    </div>
  );
}

/** Full screen: swipe on a phone; arrows, the keyboard and Escape on a computer. */
function Viewer({
  images,
  title,
  start,
  onClose,
}: {
  images: ListingImage[];
  title: string;
  start: number;
  onClose: () => void;
}) {
  const t = useT();
  const track = useRef<HTMLDivElement>(null);
  const [index, setIndex] = useState(start);
  const count = images.length;

  useLayoutEffect(() => {
    const el = track.current;
    if (el) el.scrollLeft = start * el.clientWidth;
  }, [start]);

  const goTo = useCallback((i: number) => {
    const el = track.current;
    if (el) el.scrollTo({ left: i * el.clientWidth, behavior: "smooth" });
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowLeft") goTo(Math.max(0, index - 1));
      if (e.key === "ArrowRight") goTo(Math.min(count - 1, index + 1));
    };
    document.addEventListener("keydown", onKey);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = "";
    };
  }, [onClose, goTo, index, count]);

  const arrow =
    "absolute top-1/2 flex size-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/15 text-white hover:bg-white/25 max-md:hidden";

  return (
    <div role="dialog" aria-modal="true" aria-label={title} className="fixed inset-0 z-[100] bg-black">
      <div
        ref={track}
        onScroll={() => {
          const el = track.current;
          if (el && el.clientWidth > 0) setIndex(Math.round(el.scrollLeft / el.clientWidth));
        }}
        className="snap-track flex h-full snap-x snap-mandatory overflow-x-auto"
      >
        {images.map((img, i) => (
          <div key={img.id} onClick={onClose} className="flex h-full w-full shrink-0 snap-center items-center justify-center">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={img.url}
              alt={`${title} — ${i + 1}/${count}`}
              onClick={(e) => e.stopPropagation()}
              className="max-h-full max-w-full object-contain"
            />
          </div>
        ))}
      </div>
      <button
        type="button"
        onClick={onClose}
        aria-label={t("Bağla")}
        className="absolute right-3 top-3 flex size-10 items-center justify-center rounded-full bg-white/15 text-white hover:bg-white/25"
      >
        <X className="size-5" />
      </button>
      {count > 1 && (
        <>
          {index > 0 && (
            <button type="button" onClick={() => goTo(index - 1)} aria-label={t("Əvvəlki şəkil")} className={`${arrow} left-4`}>
              <ChevronLeft className="size-6" />
            </button>
          )}
          {index < count - 1 && (
            <button type="button" onClick={() => goTo(index + 1)} aria-label={t("Növbəti şəkil")} className={`${arrow} right-4`}>
              <ChevronRight className="size-6" />
            </button>
          )}
          <span className="absolute bottom-5 left-1/2 -translate-x-1/2 rounded-full bg-white/15 px-3 py-1 text-xs font-semibold text-white">
            {index + 1} / {count}
          </span>
        </>
      )}
    </div>
  );
}
