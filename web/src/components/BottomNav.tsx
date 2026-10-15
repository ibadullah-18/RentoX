"use client";

import { useT } from "@/lib/i18n/client";
import { tx } from "@/lib/i18n/locales";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Heart, Home, MessageCircle, Plus, User } from "lucide-react";
import { isChatOpen } from "./ChromeGate";

const items = [
  { href: "/", label: tx("Ana səhifə"), Icon: Home },
  { href: "/secilmisler", label: tx("Seçilmişlər"), Icon: Heart },
  { href: "/yeni-elan", label: tx("Yeni elan"), Icon: Plus, main: true },
  { href: "/mesajlar", label: tx("Mesajlar"), Icon: MessageCircle },
  { href: "/kabinet", label: tx("Kabinet"), Icon: User },
];

/** Phone only: the floating bar of the app, with the big "+" in the middle. */
export function BottomNav({ unreadMessages = 0 }: { unreadMessages?: number }) {
  const t = useT();
  const pathname = usePathname();
  // Inside a conversation the composer is at the bottom: the bar steps aside, as in a messenger.
  if (isChatOpen(pathname)) return null;

  return (
    <nav
      aria-label={t("Əsas naviqasiya")}
      className="fixed inset-x-3 bottom-3 z-40 grid grid-cols-5 items-center rounded-2xl border border-line bg-surface/95 px-1.5 py-1.5 shadow-lg backdrop-blur md:hidden"
    >
      {items.map(({ href, label, Icon, main }) => {
        const active =
          href === "/" ? pathname === "/" : pathname.startsWith(href);
        if (main) {
          return (
            <span key={href} className="flex justify-center">
              <Link
                href={href}
                aria-label={t(label)}
                className="flex size-12 items-center justify-center rounded-xl bg-primary text-white shadow-md"
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
            aria-current={active ? "page" : undefined}
            className={`mx-0.5 flex min-w-0 flex-col items-center gap-0.5 rounded-xl px-0.5 py-1.5 text-[11px] font-semibold ${
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
  );
}
