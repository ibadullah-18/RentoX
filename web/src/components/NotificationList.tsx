"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import { dateFmt } from "@/lib/format";
import { useRouter } from "next/navigation";
import { useState } from "react";
import {
  AlertTriangle,
  Bell,
  Clock,
  CheckCheck,
  CheckCircle2,
  Flag,
  LifeBuoy,
  Megaphone,
  Store,
  Wallet,
  XCircle,
  type LucideIcon,
} from "lucide-react";
import { markAllNotificationsRead, markNotificationRead } from "@/app/bildirisler/actions";
import type { AppNotification } from "@/lib/types";

const GUID = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";

/** What each kind looks like and where it leads. */
function describe(n: AppNotification): { Icon: LucideIcon; tone: string; href: string | null } {
  const listing =
    new RegExp(`/listings/(${GUID})`).exec(n.actionUrl ?? "")?.[1] ?? n.relatedEntityId ?? null;

  switch (n.type) {
    case 1:
      return { Icon: CheckCircle2, tone: "text-success", href: listing ? `/elanlarim/${listing}` : "/elanlarim" };
    case 2:
      return { Icon: XCircle, tone: "text-danger", href: listing ? `/elanlarim/${listing}` : "/elanlarim" };
    case 3:
      return { Icon: Wallet, tone: "text-warning", href: listing ? `/elanlarim/${listing}` : "/elanlarim" };
    case 4:
      return { Icon: Store, tone: "text-success", href: "/magaza" };
    case 5:
      return { Icon: Store, tone: "text-danger", href: "/magaza" };
    case 6:
      return { Icon: Flag, tone: "text-primary", href: null };
    case 7:
    case 8: {
      const ticket =
        new RegExp(`/support/tickets/(${GUID})`).exec(n.actionUrl ?? "")?.[1] ?? n.relatedEntityId ?? null;
      return { Icon: LifeBuoy, tone: "text-primary", href: ticket ? `/destek/${ticket}` : "/destek" };
    }
    case 10:
      return { Icon: Clock, tone: "text-warning", href: listing ? `/elanlarim/${listing}` : "/elanlarim" };
    case 9: {
      const url = n.actionUrl ?? "";
      return { Icon: Megaphone, tone: "text-primary", href: url.startsWith("/") && !url.startsWith("//") ? url : null };
    }
    default:
      return { Icon: AlertTriangle, tone: "text-ink-2", href: null };
  }
}


/** Notifications: unread ones are tinted; a tap marks one read and goes where it points. */
export function NotificationList({
  initial,
  compact = false,
  onNavigate,
}: {
  initial: AppNotification[];
  /** Inside the bell's small window: rows instead of cards. */
  compact?: boolean;
  onNavigate?: () => void;
}) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();
  const [items, setItems] = useState(initial);
  const unread = items.filter((n) => !n.readAtUtc).length;

  async function open(n: AppNotification, href: string | null) {
    if (!n.readAtUtc) {
      setItems((list) => list.map((x) => (x.id === n.id ? { ...x, readAtUtc: new Date().toISOString() } : x)));
      await markNotificationRead(n.id);
      router.refresh(); // the bell's number
    }
    if (href) {
      onNavigate?.();
      router.push(href);
    }
  }

  async function readAll() {
    const now = new Date().toISOString();
    setItems((list) => list.map((x) => (x.readAtUtc ? x : { ...x, readAtUtc: now })));
    await markAllNotificationsRead();
    router.refresh();
  }

  return (
    <div className={compact ? "" : "space-y-4"}>
      {unread > 0 && (
        <button
          type="button"
          onClick={readAll}
          className={`flex items-center gap-2 text-sm font-bold text-primary hover:underline ${compact ? "w-full border-b border-line px-4 py-3" : ""}`}
        >
          <CheckCheck className="size-4" aria-hidden />
          {t("Hamısını oxunmuş et ({n})", { n: unread })}
        </button>
      )}

      <ul className={compact ? "divide-y divide-line" : "space-y-2"}>
        {items.map((n) => {
          const { Icon, tone, href } = describe(n);
          return (
            <li key={n.id}>
              <button
                type="button"
                onClick={() => void open(n, href)}
                className={
                  compact
                    ? `flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-primary-soft ${n.readAtUtc ? "" : "bg-primary-soft/60"}`
                    : `flex w-full items-start gap-3 rounded-xl border p-4 text-left transition-colors hover:border-primary ${
                        n.readAtUtc ? "border-line bg-surface" : "border-primary/40 bg-primary-soft"
                      }`
                }
              >
                <Icon className={`mt-0.5 size-6 shrink-0 ${tone}`} aria-hidden />
                <span className="min-w-0 flex-1">
                  <span className="flex items-start justify-between gap-2">
                    <span className="font-bold">{n.title}</span>
                    {!n.readAtUtc && <span className="mt-1.5 size-2.5 shrink-0 rounded-full bg-primary" aria-label={t("Oxunmayıb")} />}
                  </span>
                  <span className="mt-0.5 block text-sm text-ink-2">{n.body}</span>
                  <span className="mt-1 block text-xs text-ink-2">{dateFmt(locale, { day: "numeric", month: "long", hour: "2-digit", minute: "2-digit" }).format(new Date(n.createdAtUtc))}</span>
                </span>
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

export function NoNotifications({ compact = false }: { compact?: boolean }) {
  const t = useT();
  return (
    <div
      className={`flex flex-col items-center gap-2 text-center ${compact ? "px-4 py-10" : "rounded-xl border border-line bg-surface py-16"}`}
    >
      <Bell className="size-12 text-primary" aria-hidden />
      <h2 className="text-lg font-extrabold">{t("Bildiriş yoxdur")}</h2>
      <p className="max-w-sm text-ink-2">{t("Elanınız yoxlanılanda və ya mağaza haqqında xəbər olanda burada görünəcək.")}</p>
    </div>
  );
}
