"use client";

import { useT } from "@/lib/i18n/client";
import { tx } from "@/lib/i18n/locales";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Heart, Home, MessageCircle, Plus, User } from "lucide-react";
import { hidesBottomBar } from "./ChromeGate";

const items = [
  { href: "/", label: tx("Ana səhifə"), Icon: Home },
  { href: "/secilmisler", label: tx("Seçilmişlər"), Icon: Heart },
  { href: "/yeni-elan", label: tx("Yeni elan"), Icon: Plus, main: true },
  { href: "/mesajlar", label: tx("Mesajlar"), Icon: MessageCircle },
  { href: "/kabinet", label: tx("Kabinet"), Icon: User },
];

/** How far a page must be moved before the bar reacts: a small shake of the finger changes nothing. */
const STEP = 12;
/** Near the top the bar is always whole. */
const TOP = 80;

/**
 * The page where the bar was folded, or null while it is whole. Reading downwards (away from the top) folds it; a move
 * up, the top and the very bottom open it. Remembering the page makes a new page start with a whole bar.
 */
function useFoldedAt(pathname: string): string | null {
  const [folded, setFolded] = useState<string | null>(null);
  const last = useRef(0);
  const frame = useRef(0);

  useEffect(() => {
    last.current = window.scrollY;

    const check = () => {
      frame.current = 0;
      const y = Math.max(0, window.scrollY);
      const atBottom = window.innerHeight + y >= document.documentElement.scrollHeight - 4;

      if (y < TOP || atBottom) {
        setFolded(null);
        last.current = y;
        return;
      }
      const moved = y - last.current;
      if (moved > STEP) {
        setFolded(pathname);
        last.current = y;
      } else if (moved < -STEP) {
        setFolded(null);
        last.current = y;
      }
    };

    const onScroll = () => {
      if (!frame.current) frame.current = requestAnimationFrame(check);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => {
      window.removeEventListener("scroll", onScroll);
      if (frame.current) cancelAnimationFrame(frame.current);
    };
  }, [pathname]);

  return folded;
}

/**
 * Phone only: the floating bar of the app. Reading downwards, the one shell stretches in from both sides, with a spring,
 * into a slim "+" (a new listing); moving up, it stretches out again and the items rise into place. The motion is in
 * the CSS (`.rx-dock`).
 */
export function BottomNav({ unreadMessages = 0 }: { unreadMessages?: number }) {
  const t = useT();
  const pathname = usePathname();
  const folded = useFoldedAt(pathname);
  // A conversation, a listing, a form: they have their own action at the bottom, the bar steps aside.
  if (hidesBottomBar(pathname)) return null;

  const collapsed = folded === pathname;

  return (
    <div className="rx-dock md:hidden" data-collapsed={collapsed}>
      <div className="rx-dock-shell">
        <nav
          aria-label={t("Əsas naviqasiya")}
          aria-hidden={collapsed}
          inert={collapsed}
          className="rx-dock-row grid grid-cols-5 items-center px-2"
        >
          {items.map(({ href, label, Icon, main }, k) => {
            const active = href === "/" ? pathname === "/" : pathname.startsWith(href);
            const style = { "--k": k } as React.CSSProperties;
            if (main) {
              return (
                <span key={href} className="flex justify-center" style={style}>
                  <Link
                    href={href}
                    aria-label={t(label)}
                    className="flex size-12 items-center justify-center rounded-xl bg-primary text-white shadow-md active:scale-95"
                  >
                    <Icon className="size-6" />
                  </Link>
                </span>
              );
            }
            return (
              <Link
                key={href}
                href={href}
                style={style}
                aria-current={active ? "page" : undefined}
                className={`mx-0.5 flex min-w-0 flex-col items-center gap-0.5 rounded-xl px-0.5 py-1.5 text-[11px] font-semibold active:scale-95 ${
                  active ? "bg-primary-soft text-primary" : "text-ink-2"
                }`}
              >
                <span className="relative">
                  <Icon className="size-5" />
                  {href === "/mesajlar" && unreadMessages > 0 && (
                    <span className="absolute -right-2 -top-1.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-accent px-1 text-[10px] font-bold text-white">
                      {unreadMessages > 99 ? "99+" : unreadMessages}
                    </span>
                  )}
                </span>
                <span className="w-full truncate text-center">{t(label)}</span>
              </Link>
            );
          })}
        </nav>

        {/* The slim "+" the shell becomes: the whole shell is the link while folded. */}
        <Link
          href="/yeni-elan"
          aria-label={t("Yeni elan")}
          aria-hidden={!collapsed}
          tabIndex={collapsed ? 0 : -1}
          className={`rx-dock-plus ${collapsed ? "" : "pointer-events-none"}`}
        >
          <Plus className="size-5" strokeWidth={2.5} aria-hidden />
        </Link>
      </div>
    </div>
  );
}
