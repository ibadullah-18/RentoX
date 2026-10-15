"use client";

import { useEffect, useRef } from "react";
import { X, type LucideIcon } from "lucide-react";

export type ToastItem = {
  id: string;
  title: string;
  body: string;
  Icon: LucideIcon;
  /** Tailwind text colour of the icon. */
  tone: string;
};

const LIFETIME_MS = 8000;

/**
 * Small cards at the bottom right: each stays 8 seconds and goes away, the X closes it sooner, and pointing at
 * one holds it. A click on the text opens what it is about.
 */
export function ToastStack({
  items,
  onOpen,
  onClose,
  bottomOffset = "bottom-20 md:bottom-5",
}: {
  items: ToastItem[];
  onOpen: (item: ToastItem) => void;
  onClose: (id: string) => void;
  bottomOffset?: string;
}) {
  if (items.length === 0) return null;
  return (
    <div
      aria-live="polite"
      className={`pointer-events-none fixed inset-x-3 z-[60] flex flex-col items-end gap-2 md:inset-x-auto md:right-5 ${bottomOffset}`}
    >
      {items.map((item) => (
        <Toast key={item.id} item={item} onOpen={() => onOpen(item)} onClose={() => onClose(item.id)} />
      ))}
    </div>
  );
}

function Toast({ item, onOpen, onClose }: { item: ToastItem; onOpen: () => void; onClose: () => void }) {
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const closeRef = useRef(onClose);

  useEffect(() => {
    closeRef.current = onClose;
  }, [onClose]);

  const hold = () => {
    if (timer.current) clearTimeout(timer.current);
  };
  const arm = () => {
    hold();
    timer.current = setTimeout(() => closeRef.current(), LIFETIME_MS);
  };

  useEffect(() => {
    timer.current = setTimeout(() => closeRef.current(), LIFETIME_MS);
    return () => {
      if (timer.current) clearTimeout(timer.current);
    };
  }, []);

  const { Icon } = item;
  return (
    <div
      role="status"
      onMouseEnter={hold}
      onMouseLeave={arm}
      className="toast-in pointer-events-auto relative w-full max-w-[380px] rounded-2xl border border-line bg-surface p-3 pr-10 shadow-xl"
    >
      <button type="button" onClick={onOpen} className="flex w-full items-start gap-3 text-left">
        <span className={`mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-primary-soft ${item.tone}`}>
          <Icon className="size-5" aria-hidden />
        </span>
        <span className="min-w-0">
          <span className="block text-[14px] font-extrabold leading-snug">{item.title}</span>
          <span className="mt-0.5 line-clamp-2 block text-[13px] text-ink-2">{item.body}</span>
        </span>
      </button>
      <button
        type="button"
        onClick={onClose}
        aria-label="×"
        className="absolute right-2 top-2 flex size-7 items-center justify-center rounded-full text-ink-2 hover:bg-primary-soft hover:text-ink"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  );
}
