"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import { dateFmt } from "@/lib/format";
import type { Locale } from "@/lib/i18n/locales";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { MessageCircle } from "lucide-react";
import { refreshInbox } from "@/app/mesajlar/actions";
import type { Conversation } from "@/lib/types";
import { RealtimeProvider, useRealtime } from "./Realtime";
import { UserAvatar } from "./UserAvatar";

type Inbox = {
  /** The chat shows these messages as read: drop the unread badge. */
  clearUnread: (conversationId: string) => void;
  /** The newest line of a conversation changed (a message I sent). */
  touch: (conversationId: string, lastMessage: string, atUtc: string) => void;
  me: string;
};

const InboxContext = createContext<Inbox>({ clearUnread: () => {}, touch: () => {}, me: "" });
export const useInbox = () => useContext(InboxContext);

function when(iso: string | null | undefined, locale: Locale): string {
  if (!iso) return "";
  const d = new Date(iso);
  const same = d.toDateString() === new Date().toDateString();
  return same
    ? dateFmt(locale, { hour: "2-digit", minute: "2-digit" }).format(d)
    : dateFmt(locale, { day: "numeric", month: "short" }).format(d);
}

/**
 * Inbox on the left, the open chat on the right (computer). On a phone one of
 * them at a time: the list, or the chat once one is open.
 */
export function MessagesShell({
  initial,
  me,
  children,
}: {
  initial: Conversation[];
  me: string;
  children: React.ReactNode;
}) {
  return (
    <RealtimeProvider>
      <Shell initial={initial} me={me}>
        {children}
      </Shell>
    </RealtimeProvider>
  );
}

function Shell({
  initial,
  me,
  children,
}: {
  initial: Conversation[];
  me: string;
  children: React.ReactNode;
}) {
  const t = useT();
  const locale = useLocale();
  const pathname = usePathname();
  const openId = pathname.startsWith("/mesajlar/") ? pathname.split("/")[2] : null;
  const realtime = useRealtime();
  const [conversations, setConversations] = useState(initial);

  const reload = useCallback(async () => {
    const res = await refreshInbox();
    if (res.ok) setConversations(res.conversations);
  }, []);

  useEffect(
    () =>
      realtime.subscribe((event) => {
        if (event.type === "reconnected") {
          void reload();
          return;
        }
        if (event.type !== "message") return;
        const m = event.message;
        setConversations((list) => {
          const known = list.find((c) => c.id === m.conversationId);
          if (!known) {
            void reload(); // a conversation that began while this page was open
            return list;
          }
          const mine = m.senderId === me;
          const line = m.body || "📷";
          const updated: Conversation = {
            ...known,
            lastMessage: line,
            lastMessageAtUtc: m.sentAtUtc,
            unreadCount: mine || m.conversationId === openId ? known.unreadCount : known.unreadCount + 1,
          };
          return [updated, ...list.filter((c) => c.id !== m.conversationId)];
        });
      }),
    [realtime, reload, me, openId],
  );

  const inbox = useMemo<Inbox>(
    () => ({
      me,
      clearUnread: (id) =>
        setConversations((list) => list.map((c) => (c.id === id && c.unreadCount > 0 ? { ...c, unreadCount: 0 } : c))),
      touch: (id, lastMessage, atUtc) =>
        setConversations((list) => {
          const known = list.find((c) => c.id === id);
          if (!known) return list;
          return [{ ...known, lastMessage, lastMessageAtUtc: atUtc }, ...list.filter((c) => c.id !== id)];
        }),
    }),
    [me],
  );

  return (
    <InboxContext.Provider value={inbox}>
      <div className="grid grid-cols-[minmax(0,1fr)] h-[calc(100dvh-13rem)] min-h-[460px] overflow-hidden rounded-2xl border border-line bg-surface md:h-[calc(100dvh-9.5rem)] md:grid-cols-[340px_minmax(0,1fr)]">
        <aside className={`${openId ? "hidden md:flex" : "flex"} min-h-0 flex-col border-line md:border-r`}>
          <h1 className="border-b border-line px-4 py-4 text-xl font-extrabold">{t("Mesajlar")}</h1>
          {conversations.length === 0 ? (
            <div className="flex flex-1 flex-col items-center justify-center gap-2 p-6 text-center">
              <MessageCircle className="size-10 text-primary" aria-hidden />
              <p className="font-bold">{t("Hələ mesajınız yoxdur")}</p>
              <p className="text-sm text-ink-2">{t("Elan sahibinə yazın, söhbət burada görünəcək.")}</p>
            </div>
          ) : (
            <ul className="min-h-0 flex-1 overflow-y-auto">
              {conversations.map((c) => (
                <li key={c.id}>
                  <Link
                    href={`/mesajlar/${c.id}`}
                    aria-current={c.id === openId ? "page" : undefined}
                    className={`flex items-center gap-3 border-b border-line px-4 py-3 hover:bg-primary-soft ${
                      c.id === openId ? "bg-primary-soft" : ""
                    }`}
                  >
                    <UserAvatar userId={c.otherUserId} size={48} />
                    <span className="min-w-0 flex-1">
                      <span className="flex items-baseline justify-between gap-2">
                        <span className="truncate font-bold">{c.listingTitle}</span>
                        <span className="shrink-0 text-xs text-ink-2">{when(c.lastMessageAtUtc, locale)}</span>
                      </span>
                      <span className="flex items-center justify-between gap-2">
                        <span className="truncate text-sm text-ink-2">{c.lastMessage ?? "—"}</span>
                        {c.unreadCount > 0 && (
                          <span className="flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-primary px-1.5 text-xs font-bold text-white">
                            {c.unreadCount}
                          </span>
                        )}
                      </span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </aside>

        <section
          className={`${openId ? "flex max-md:fixed max-md:inset-0 max-md:z-[70] max-md:h-dvh max-md:bg-surface" : "hidden md:flex"} min-h-0 min-w-0 flex-col`}
        >{children}</section>
      </div>
    </InboxContext.Provider>
  );
}
