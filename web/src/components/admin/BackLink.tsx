"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { ADMIN_SEGMENT } from "@/lib/admin-path";

type NavigationLike = {
  currentEntry?: { index: number } | null;
  entries?: () => { url?: string | null }[];
};

/** True when the page before this one (in this tab) is in the admin panel, so "back" stays inside it. */
function cameFromPanel(): boolean {
  try {
    const nav = (window as unknown as { navigation?: NavigationLike }).navigation;
    const entries = nav?.entries?.();
    const index = nav?.currentEntry?.index ?? -1;
    if (entries && index > 0) {
      const url = new URL(entries[index - 1].url ?? "", location.origin);
      return url.origin === location.origin && url.pathname.startsWith(`/${ADMIN_SEGMENT}`);
    }
    return history.length > 1 && document.referrer.startsWith(`${location.origin}/${ADMIN_SEGMENT}`);
  } catch {
    return false;
  }
}

/**
 * "Back": goes to the page the person really came from (a listing, a report, a search), not to a fixed list.
 * Opened straight from a link or a new tab, it goes to [href] instead.
 */
export function BackLink({ href, title, label = "Geri", className }: { href: string; title?: string; label?: string; className?: string }) {
  const router = useRouter();
  return (
    <Link
      href={href}
      title={title ? `${label}: ${title}` : label}
      onClick={(e) => {
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) return;
        if (cameFromPanel()) {
          e.preventDefault();
          router.back();
        }
      }}
      className={
        className ??
        "no-print inline-flex h-9 w-fit items-center gap-1 rounded-lg border border-line bg-surface pl-2 pr-3 text-sm font-bold text-ink-2 hover:border-primary hover:text-primary"
      }
    >
      <ChevronLeft className="size-4" aria-hidden />
      {label}
    </Link>
  );
}
