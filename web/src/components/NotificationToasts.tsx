"use client";

import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { markNotificationRead } from "@/app/bildirisler/actions";
import type { AppNotification } from "@/lib/types";
import { describe } from "./NotificationList";
import { ToastStack, type ToastItem } from "./ui/ToastStack";

const MAX_VISIBLE = 3;

async function readToken(): Promise<string> {
  const res = await fetch("/bff/hub-token", { cache: "no-store" });
  if (!res.ok) throw new Error("no token");
  return ((await res.json()) as { token: string }).token;
}

/**
 * A new notification appears at the bottom right at once (no reload), stays 8 seconds and goes away;
 * the X closes it sooner, and pointing at it holds it. The numbers in the header are refreshed too.
 */
export function NotificationToasts() {
  const router = useRouter();
  const [items, setItems] = useState<(ToastItem & { n: AppNotification; href: string | null })[]>([]);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const hub = new HubConnectionBuilder()
      .withUrl("/hubs/notifications", { accessTokenFactory: readToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.None)
      .build();

    const refresh = () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
      refreshTimer.current = setTimeout(() => router.refresh(), 600);
    };

    hub.on("NotificationCreated", (n: AppNotification) => {
      const { Icon, tone, href } = describe(n);
      setItems((list) =>
        list.some((x) => x.id === n.id) ? list : [...list, { id: n.id, title: n.title, body: n.body, Icon, tone, n, href }].slice(-MAX_VISIBLE),
      );
      refresh();
    });
    hub.onreconnected(refresh);

    let closed = false;
    const start = () =>
      hub.start().catch(() => {
        if (!closed) setTimeout(start, 5000);
      });
    void start();

    return () => {
      closed = true;
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
      void hub.stop();
    };
  }, [router]);

  return (
    <ToastStack
      items={items}
      onClose={(id) => setItems((list) => list.filter((x) => x.id !== id))}
      onOpen={(item) => {
        const found = items.find((x) => x.id === item.id);
        setItems((list) => list.filter((x) => x.id !== item.id));
        if (!found) return;
        void markNotificationRead(found.n.id);
        if (found.href) router.push(found.href);
        router.refresh();
      }}
    />
  );
}
