"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useSyncExternalStore } from "react";
import { Info, Megaphone, TriangleAlert, X } from "lucide-react";
import { useT } from "@/lib/i18n/client";
import type { Branding } from "@/lib/types";
import { isChatRoute } from "./ChromeGate";

const KEY = "rx_notice_closed";
const CHANGED = "rx-notice-closed";

function subscribe(onChange: () => void) {
  window.addEventListener(CHANGED, onChange);
  window.addEventListener("storage", onChange);
  return () => {
    window.removeEventListener(CHANGED, onChange);
    window.removeEventListener("storage", onChange);
  };
}

/** Kept here too, so the cross still works where the browser does not let the site store anything. */
let closedHere: string | null = null;

/** The text of the notice this browser closed. */
function closedText(): string | null {
  try {
    return localStorage.getItem(KEY) ?? closedHere;
  } catch {
    return closedHere;
  }
}

const tones = [
  { box: "bg-primary-soft text-primary", Icon: Info },
  { box: "bg-warning-soft text-ink", Icon: TriangleAlert },
  { box: "bg-success-soft text-ink", Icon: Megaphone },
];

/** The owner's notice at the very top ("maintenance tonight", "new: ..."). A visitor can close it; a new text shows again. */
export function AnnouncementBar({ branding }: { branding: Branding }) {
  const t = useT();
  const pathname = usePathname();
  const text = branding.announcementEnabled ? branding.announcementText?.trim() : null;
  // On the server the notice is shown; a browser that closed this very text hides it at once.
  const closed = useSyncExternalStore(subscribe, closedText, () => null) === text;

  // A chat window fills the whole screen under the header: no notice above it.
  if (!text || closed || isChatRoute(pathname)) return null;

  const { box, Icon } = tones[branding.announcementTone] ?? tones[0];
  const link = branding.announcementLink?.trim();
  const body = <span className="min-w-0 flex-1 text-center">{text}</span>;
  const inner = link?.startsWith("/") ? (
    <Link href={link} className="flex min-w-0 flex-1 items-center justify-center gap-2 underline-offset-2 hover:underline">
      <Icon className="size-4 shrink-0" aria-hidden />
      {body}
    </Link>
  ) : link ? (
    <a href={link} rel="noopener noreferrer" className="flex min-w-0 flex-1 items-center justify-center gap-2 underline-offset-2 hover:underline">
      <Icon className="size-4 shrink-0" aria-hidden />
      {body}
    </a>
  ) : (
    <div className="flex min-w-0 flex-1 items-center justify-center gap-2">
      <Icon className="size-4 shrink-0" aria-hidden />
      {body}
    </div>
  );

  return (
    <div role="status" className={`rx-notice flex items-center gap-2 px-4 py-2 text-sm font-semibold ${box}`}>
      {inner}
      <button
        type="button"
        aria-label={t("Bağla")}
        onClick={() => {
          closedHere = text;
          try {
            localStorage.setItem(KEY, text);
          } catch {
            /* not remembered: it shows again next time */
          }
          window.dispatchEvent(new Event(CHANGED));
        }}
        className="flex size-7 shrink-0 items-center justify-center rounded-md hover:bg-black/10"
      >
        <X className="size-4" aria-hidden />
      </button>
    </div>
  );
}
