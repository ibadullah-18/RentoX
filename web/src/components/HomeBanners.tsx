"use client";

import Image from "next/image";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import type { Banner } from "@/lib/types";

/** How long one banner stays before the next one slides in. */
const EVERY_MS = 5000;

/** One banner: the picture with the text over a dark fade; a link when the banner has one. */
function Slide({ banner, first }: { banner: Banner; first: boolean }) {
  const body = (
    <>
      <Image
        src={banner.imageUrl}
        alt=""
        fill
        unoptimized
        priority={first}
        sizes="(min-width: 1100px) 1100px, 100vw"
        className="object-cover"
      />
      <span className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" aria-hidden />
      <span className="absolute inset-x-0 bottom-0 p-4 text-white md:p-6">
        <span className="block text-lg font-extrabold leading-tight drop-shadow md:text-3xl">{banner.title}</span>
        {banner.subtitle && <span className="mt-1 block text-sm font-medium opacity-95 drop-shadow md:text-base">{banner.subtitle}</span>}
      </span>
    </>
  );
  const box = "relative block aspect-[3/1] min-h-32 w-full shrink-0 snap-center overflow-hidden rounded-xl bg-primary-soft";
  const url = banner.linkUrl ?? "";

  if (!url) return <div className={box}>{body}</div>;
  if (url.startsWith("/")) {
    return (
      <Link href={url} className={box}>
        {body}
      </Link>
    );
  }
  return (
    <a href={url} target="_blank" rel="noopener noreferrer" className={box}>
      {body}
    </a>
  );
}

/**
 * The banners of the home page: one at a time, moving on by itself every 5 seconds, always the same way (the next
 * one comes in from the right; after the last comes the first again, without going back). One dot per banner below.
 * Swiping either way or tapping a dot works too and restarts the count. The active dot fills over the 5 seconds like a timer. On a computer plain arrows sit at the sides. The loop is made with a copy of the first
 * banner after the last: when it has arrived, the strip quietly jumps back to the real first.
 */
export function HomeBanners({ banners }: { banners: Banner[] }) {
  const scroller = useRef<HTMLDivElement>(null);
  const settle = useRef<ReturnType<typeof setTimeout> | null>(null);
  const position = useRef(0);
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  // Bumped on every manual move so the 7 seconds start again.
  const [restart, setRestart] = useState(0);
  const count = banners.length;

  const show = useCallback((i: number) => {
    const el = scroller.current;
    const slide = el?.children[i] as HTMLElement | undefined;
    if (el && slide) el.scrollTo({ left: slide.offsetLeft - el.offsetLeft, behavior: "smooth" });
  }, []);

  // One 5 second count per banner: it starts again whenever the banner changes, the person moved it, or the pointer left.
  useEffect(() => {
    if (count < 2 || paused) return;
    const timer = setTimeout(() => show(position.current + 1), EVERY_MS);
    return () => clearTimeout(timer);
  }, [count, paused, restart, index, show]);

  useEffect(() => () => { if (settle.current) clearTimeout(settle.current); }, []);

  if (count === 0) return null;

  function next() {
    setRestart((n) => n + 1);
    show(position.current + 1);
  }

  function previous() {
    const el = scroller.current;
    setRestart((n) => n + 1);
    if (position.current > 0) {
      show(position.current - 1);
    } else if (el) {
      // From the first banner back to the last: jump to the copy after the last one, then slide one back.
      const copy = el.children[count] as HTMLElement | undefined;
      if (copy) el.scrollTo({ left: copy.offsetLeft - el.offsetLeft, behavior: "instant" });
      position.current = count;
      requestAnimationFrame(() => show(count - 1));
    }
  }

  function onScroll() {
    const el = scroller.current;
    const first = el?.children[0] as HTMLElement | undefined;
    if (!el || !first || first.offsetWidth === 0) return;
    const nearest = Math.min(Math.max(Math.round(el.scrollLeft / (first.offsetWidth + 12)), 0), count);
    position.current = nearest;
    setIndex(nearest % count);
    // On the copy of the first banner: when the movement stops, go back to the real one without any animation.
    if (settle.current) clearTimeout(settle.current);
    if (nearest === count) {
      settle.current = setTimeout(() => {
        el.scrollTo({ left: 0, behavior: "instant" });
        position.current = 0;
        setIndex(0);
      }, 160);
    }
  }

  return (
    <section aria-label="Banner" className="space-y-2.5" onMouseEnter={() => setPaused(true)} onMouseLeave={() => {
      setPaused(false);
      setRestart((n) => n + 1);
    }}>
      <div className="relative">
      <div
        ref={scroller}
        onScroll={onScroll}
        onTouchStart={() => setRestart((n) => n + 1)}
        className="flex snap-x snap-mandatory gap-3 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {banners.map((b, i) => (
          <Slide key={b.id} banner={b} first={i === 0} />
        ))}
        {count > 1 && <Slide key="loop-copy" banner={banners[0]} first={false} />}
      </div>

      {count > 1 && (
        <>
          <button
            type="button"
            onClick={previous}
            aria-label="‹"
            className="absolute left-2 top-1/2 hidden -translate-y-1/2 p-1 text-white drop-shadow-[0_1px_3px_rgba(0,0,0,0.6)] hover:scale-110 md:block"
          >
            <ChevronLeft className="size-9" strokeWidth={1.75} aria-hidden />
          </button>
          <button
            type="button"
            onClick={next}
            aria-label="›"
            className="absolute right-2 top-1/2 hidden -translate-y-1/2 p-1 text-white drop-shadow-[0_1px_3px_rgba(0,0,0,0.6)] hover:scale-110 md:block"
          >
            <ChevronRight className="size-9" strokeWidth={1.75} aria-hidden />
          </button>
        </>
      )}
      </div>

      {count > 1 && (
        <div className="flex justify-center gap-1.5">
          {banners.map((b, i) => (
            <button
              key={b.id}
              type="button"
              aria-label={`${i + 1} / ${count}`}
              aria-current={i === index}
              onClick={() => {
                position.current = i;
                setIndex(i);
                show(i);
                setRestart((n) => n + 1);
              }}
              className={`relative h-1.5 overflow-hidden rounded-full bg-line transition-all duration-500 ease-[cubic-bezier(0.65,0,0.35,1)] ${i === index ? "w-7" : "w-1.5 hover:bg-ink-2/40"}`}
            >
              {i === index && (
                <span
                  key={`${index}-${restart}`}
                  className="dot-fill absolute inset-y-0 left-0 rounded-full bg-primary"
                  style={{ animationDuration: `${EVERY_MS}ms`, animationPlayState: paused ? "paused" : "running" }}
                />
              )}
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
