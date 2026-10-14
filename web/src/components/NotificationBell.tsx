"use client";

import { useState } from "react";
import { Bell, Loader2 } from "lucide-react";
import { useT } from "@/lib/i18n/client";
import type { AppNotification } from "@/lib/types";
import { NoNotifications, NotificationList } from "./NotificationList";
import { Popup } from "./ui/Popup";

/** The bell: a small window with the latest notifications, not a whole page. */
export function NotificationBell({ unread, signedIn }: { unread: number; signedIn: boolean }) {
  const t = useT();
  const [items, setItems] = useState<AppNotification[] | null>(null);
  const [failed, setFailed] = useState(false);
  // A new load every time the window opens, so it is never stale.
  const [round, setRound] = useState(0);

  function onOpenChange(open: boolean) {
    if (!open || !signedIn) return;
    setFailed(false);
    setItems(null);
    setRound((r) => r + 1);
    fetch("/bff/notifications", { cache: "no-store" })
      .then((r) => (r.ok ? (r.json() as Promise<AppNotification[]>) : Promise.reject()))
      .then(setItems)
      .catch(() => setFailed(true));
  }

  return (
    <Popup
      ariaLabel={t("Bildirişlər")}
      phone="top"
      panelClassName="md:w-[380px]"
      onOpenChange={onOpenChange}
      buttonClassName="relative flex size-10 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft hover:text-primary"
      button={() => (
        <>
          <Bell className="size-5" aria-hidden />
          {unread > 0 && (
            <span className="absolute right-0 top-0 flex h-[17px] min-w-[17px] items-center justify-center rounded-full bg-accent px-1 text-[10px] font-bold text-white">
              {unread > 99 ? "99+" : unread}
            </span>
          )}
        </>
      )}
    >
      {(close) =>
        !signedIn ? (
          <p className="px-4 py-8 text-center text-sm text-ink-2">{t("Elan vermək, mesaj yazmaq və seçilmişlər üçün.")}</p>
        ) : failed ? (
          <p className="px-4 py-8 text-center text-sm text-ink-2">{t("Xəta baş verdi")}</p>
        ) : items === null ? (
          <div className="flex justify-center py-10">
            <Loader2 className="size-6 animate-spin text-primary" aria-label={t("Yüklənir…")} />
          </div>
        ) : items.length === 0 ? (
          <NoNotifications compact />
        ) : (
          <NotificationList key={round} initial={items} compact onNavigate={close} />
        )
      }
    </Popup>
  );
}
