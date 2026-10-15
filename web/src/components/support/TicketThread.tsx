"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { ChevronLeft, LifeBuoy, Loader2, Send } from "lucide-react";
import { replyTicket } from "@/app/destek/actions";
import { dateFmt } from "@/lib/format";
import { useLocale, useT } from "@/lib/i18n/client";
import { useSupportLive, useTypingFrom, useTypingSender, type LiveSupportMessage } from "./SupportRealtime";

export type ThreadMessage = LiveSupportMessage & {
  /** The time and the day as the server wrote them; a message that arrives live is written in the browser. */
  time?: string;
  day?: string;
};

const BAKU = "Asia/Baku";

/**
 * The request as a chat room: the words of the person on the right, support on the left, the box to write below.
 * Everything is live: answers appear as they are sent, "support is typing…" shows while they write, and a
 * change of state (closed, in progress) refreshes the page. The window scrolls by itself, the page does not.
 */
export function TicketThread({
  ticketId,
  subject,
  category,
  messages: saved,
  status,
  statusLabel,
  statusTone,
}: {
  ticketId: string;
  subject: string;
  category: string;
  messages: ThreadMessage[];
  status: number;
  statusLabel: string;
  statusTone: string;
}) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();
  const live = useSupportLive();
  const typingSender = useTypingSender(ticketId);
  const staffTyping = useTypingFrom(ticketId, true);
  const scroller = useRef<HTMLDivElement>(null);
  const box = useRef<HTMLTextAreaElement>(null);
  const [arrived, setArrived] = useState<ThreadMessage[]>([]);
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const messages = [...saved, ...arrived.filter((m) => !saved.some((x) => x.id === m.id))].sort((a, b) =>
    a.sentAtUtc.localeCompare(b.sentAtUtc),
  );

  useEffect(
    () =>
      live.subscribe((event) => {
        if (event.type === "message" && event.ticketId === ticketId) {
          setArrived((list) => (list.some((m) => m.id === event.message.id) ? list : [...list, event.message]));
        } else if ((event.type === "changed" && event.ticketId === ticketId) || event.type === "reconnected") {
          router.refresh();
        }
      }),
    [live, ticketId, router],
  );

  // The newest message and the "typing" line are always in view.
  useEffect(() => {
    const el = scroller.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [messages.length, staffTyping]);

  const dayOf = (m: ThreadMessage) =>
    m.day ?? dateFmt(locale, { day: "numeric", month: "long", timeZone: BAKU }).format(new Date(m.sentAtUtc));
  const timeOf = (m: ThreadMessage) =>
    m.time ?? dateFmt(locale, { hour: "2-digit", minute: "2-digit", timeZone: BAKU }).format(new Date(m.sentAtUtc));

  const days = messages.map(dayOf);
  const closed = status === 4;

  async function send() {
    const body = text.trim();
    if (!body || busy) return;
    setBusy(true);
    setError(null);
    typingSender.stopped();
    const result = await replyTicket(ticketId, body);
    setBusy(false);
    if (result.ok) {
      setText("");
      router.refresh();
      box.current?.focus();
    } else if (result.unauthorized) router.push(`/giris?next=/destek/${ticketId}`);
    else setError(result.message);
  }

  return (
    <div className="mx-auto flex h-[calc(100dvh-var(--rx-header,4rem))] min-h-[420px] max-w-2xl flex-col overflow-hidden border-line bg-surface max-md:border-t md:h-[calc(100dvh-var(--rx-header,4rem)-2.5rem)] md:rounded-2xl md:border">
      <header className="flex items-center gap-2 border-b border-line px-2 py-2.5 md:px-3">
        <Link
          href="/destek"
          aria-label={t("Dəstək")}
          className="flex size-10 shrink-0 items-center justify-center rounded-full text-ink-2 hover:bg-primary-soft hover:text-primary"
        >
          <ChevronLeft className="size-6" aria-hidden />
        </Link>
        <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary">
          <LifeBuoy className="size-5" aria-hidden />
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate font-extrabold leading-tight">{subject}</p>
          <p className={`truncate text-xs font-semibold ${staffTyping ? "text-success" : "text-ink-2"}`}>
            {staffTyping ? `${t("RentoX dəstək")} ${t("yazır…")}` : `${t("RentoX dəstək")} · ${category}`}
          </p>
        </div>
        <span className={`rounded-md px-2.5 py-1 text-xs font-bold ${statusTone}`}>{statusLabel}</span>
      </header>

      <div ref={scroller} className="flex-1 space-y-1 overflow-y-auto bg-bg px-3 py-4 md:px-5">
        {messages.map((m, i) => {
          const newDay = i === 0 || days[i] !== days[i - 1];
          const startsRun = newDay || messages[i - 1].isAdmin !== m.isAdmin;
          return (
            <div key={m.id}>
              {newDay && (
                <div className="my-3 flex justify-center">
                  <span className="rounded-lg bg-surface px-3 py-1 text-xs font-semibold text-ink-2">{days[i]}</span>
                </div>
              )}
              <div className={`flex flex-col ${m.isAdmin ? "items-start" : "items-end"} ${startsRun ? "pt-2" : ""}`}>
                {startsRun && m.isAdmin && (
                  <span className="mb-0.5 px-1 text-[11px] font-bold text-primary">{t("RentoX dəstək")}</span>
                )}
                <div
                  className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-[15px] ${
                    m.isAdmin ? "rounded-bl-md border border-line bg-surface" : "rounded-br-md bg-primary text-white"
                  }`}
                >
                  {m.body}
                  <span className={`ml-3 inline-block translate-y-0.5 text-[10px] ${m.isAdmin ? "text-ink-2" : "text-white/70"}`}>
                    {timeOf(m)}
                  </span>
                </div>
              </div>
            </div>
          );
        })}
        {staffTyping && (
          <div className="flex items-center gap-2 pt-2 text-xs font-semibold text-ink-2" aria-live="polite">
            <span className="flex gap-0.5" aria-hidden>
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2 [animation-delay:-0.3s]" />
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2 [animation-delay:-0.15s]" />
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2" />
            </span>
            {t("RentoX dəstək yazır…")}
          </div>
        )}
      </div>

      <div className="border-t border-line bg-surface p-3">
        {closed ? (
          <p className="rounded-xl bg-line px-4 py-3 text-center text-sm font-semibold text-ink-2">
            {t("Bu müraciət bağlanıb. Yeni sualınız varsa, yeni müraciət açın.")}
          </p>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void send();
            }}
            className="flex items-end gap-2"
          >
            <textarea
              ref={box}
              value={text}
              onChange={(e) => {
                setText(e.target.value);
                typingSender.changed(e.target.value);
              }}
              onBlur={typingSender.stopped}
              onKeyDown={(e) => {
                // Enter sends; Shift+Enter makes a new line.
                if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
                  e.preventDefault();
                  void send();
                }
              }}
              rows={Math.min(5, Math.max(1, text.split("\n").length))}
              maxLength={4000}
              disabled={busy}
              placeholder={t("Cavabınızı yazın")}
              className="min-h-[3rem] flex-1 resize-none rounded-xl border border-line bg-surface px-3.5 py-3 text-[16px] outline-none focus:border-primary disabled:opacity-60"
            />
            <button
              type="submit"
              disabled={busy || !text.trim()}
              aria-label={t("Göndər")}
              className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-primary text-white hover:bg-primary-press disabled:opacity-50"
            >
              {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
            </button>
          </form>
        )}
        {error && (
          <p role="alert" className="mt-2 text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </div>
    </div>
  );
}
