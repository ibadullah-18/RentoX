"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { ClipboardCheck, Flag, LifeBuoy, MessageSquareText, Store } from "lucide-react";
import { useSupportLive } from "@/components/support/SupportRealtime";
import { ToastStack, type ToastItem } from "@/components/ui/ToastStack";
import { adminHref } from "@/lib/admin-path";
import { playMessageSound, playNotificationSound, unlockSound } from "@/lib/sounds";

type AdminToast = ToastItem & { href: string };

const MAX_VISIBLE = 4;

/**
 * Listens to the live line while the panel is open. A new report or support request plays the long chime and
 * a new message from a person plays the short one; each shows a card at the bottom right (8 seconds, X to close)
 * that says which one it is and leads to it. Every change refreshes the page, so the numbers on the menu and
 * the lists are right without reloading.
 */
export function AdminLive() {
  const live = useSupportLive();
  const router = useRouter();
  const pathname = usePathname();
  const pathRef = useRef(pathname);
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  // A person who is both staff and the owner of a request is told twice; the sound and the card come once.
  const heard = useRef(new Set<string>());
  const [toasts, setToasts] = useState<AdminToast[]>([]);

  useEffect(() => {
    pathRef.current = pathname;
  }, [pathname]);

  useEffect(() => unlockSound(), []);

  useEffect(() => {
    const refresh = () => {
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
      refreshTimer.current = setTimeout(() => router.refresh(), 700);
    };
    const show = (toast: AdminToast) =>
      setToasts((list) => (list.some((x) => x.id === toast.id) ? list : [...list, toast].slice(-MAX_VISIBLE)));
    const once = (key: string) => {
      if (heard.current.has(key)) return false;
      heard.current.add(key);
      return true;
    };

    const stop = live.subscribe((event) => {
      if (event.type === "ticket") {
        if (once(`t-${event.ticketId}`)) {
          playNotificationSound();
          show({
            id: `t-${event.ticketId}`,
            title: "Yeni dəstək müraciəti",
            body: `${event.userName ?? "İstifadəçi"}: ${event.subject}`,
            Icon: LifeBuoy,
            tone: "text-primary",
            href: adminHref(`/destek/${event.ticketId}`),
          });
        }
        refresh();
      } else if (event.type === "report") {
        if (once(`r-${event.reportId}`)) {
          playNotificationSound();
          const chat = event.kind === "chat";
          show({
            id: `r-${event.reportId}`,
            title: chat ? "Yeni yazışma şikayəti" : "Yeni elan/mağaza şikayəti",
            body: "Baxılmağı gözləyir",
            Icon: Flag,
            tone: "text-danger",
            href: adminHref(chat ? `/yazisma-sikayetleri/${event.reportId}` : `/sikayetler/${event.reportId}`),
          });
        }
        refresh();
      } else if (event.type === "review") {
        if (once(`v-${event.kind}-${event.id}`)) {
          playNotificationSound();
          const store = event.kind === "store";
          show({
            id: `v-${event.kind}-${event.id}`,
            title: store ? "Yoxlama gözləyən yeni mağaza" : "Yoxlama gözləyən yeni elan",
            body: event.title,
            Icon: store ? Store : ClipboardCheck,
            tone: "text-warning",
            href: adminHref(store ? `/magazalar/${event.id}` : `/elanlar/${event.id}`),
          });
        }
        refresh();
      } else if (event.type === "message") {
        if (!event.message.isAdmin && once(`m-${event.message.id}`)) {
          playMessageSound();
          // Already reading that conversation: the message is right there.
          if (!pathRef.current.includes(event.ticketId)) {
            show({
              id: `m-${event.message.id}`,
              title: "Dəstəkdə yeni mesaj",
              body: `${event.message.senderName ?? "İstifadəçi"}: ${event.message.body}`,
              Icon: MessageSquareText,
              tone: "text-primary",
              href: adminHref(`/destek/${event.ticketId}`),
            });
          }
        }
        refresh();
      } else if (event.type === "changed" || event.type === "reconnected") {
        refresh();
      }
    });

    return () => {
      stop();
      if (refreshTimer.current) clearTimeout(refreshTimer.current);
    };
  }, [live, router]);

  return (
    <ToastStack
      items={toasts}
      bottomOffset="bottom-5"
      onClose={(id) => setToasts((list) => list.filter((x) => x.id !== id))}
      onOpen={(item) => {
        const found = toasts.find((x) => x.id === item.id);
        setToasts((list) => list.filter((x) => x.id !== item.id));
        if (found) router.push(found.href);
      }}
    />
  );
}
