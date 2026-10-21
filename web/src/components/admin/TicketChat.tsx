"use client";

import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Loader2, MessageSquareText, Send } from "lucide-react";
import { replyAsAdmin, setTicketStatus } from "@/app/admin/(panel)/actions";
import { useSupportLive, useTypingFrom, useTypingSender } from "@/components/support/SupportRealtime";
import { AttachButton, AttachPreview, MessagePhotos, postPhotos, useAttachments } from "@/components/support/Attachments";
import { adminHref } from "@/lib/admin-path";

export type ChatMessage = {
  id: string;
  isAdmin: boolean;
  body: string;
  sentAtUtc: string;
  senderName?: string | null;
  images?: { id: string; contentType: string; sizeBytes: number; displayOrder: number }[];
};

/** Answers support writes again and again; one click puts the text in the box (then it can be changed). */
const QUICK_REPLIES: { label: string; text: string }[] = [
  { label: "Qəbul edildi", text: "Salam! Müraciətiniz üçün təşəkkür edirik. Məsələni araşdırırıq və tezliklə sizə qayıdacağıq." },
  { label: "Əlavə məlumat istə", text: "Problemi dəqiq anlamaq üçün zəhmət olmasa elanın adını və ya nömrəsini, mümkünsə ekran görüntüsünü göndərin." },
  { label: "Balans yoxlandı", text: "Pul kisəsi əməliyyatınız yoxlanıldı və lazım olan düzəliş edildi. Əməliyyat tarixçənizdə görə bilərsiniz." },
  { label: "Elan rədd edildi", text: "Elanınız qaydalara uyğun olmadığı üçün yayımlanmayıb. Səbəb elanın səhifəsində yazılıb: düzəldib yenidən göndərə bilərsiniz." },
  { label: "Həll olundu", text: "Məsələ həll olundu. Başqa sualınız olarsa, bu müraciətə yazmaqdan çəkinməyin." },
  { label: "Cavab gözlənilir", text: "Cavabınızı gözləyirik. 3 gün ərzində cavab olmasa, müraciət bağlanacaq." },
];

// Written by hand and in Baku time, so the server and the browser always print the same text (no locale data needed).
const MONTHS = ["yanvar", "fevral", "mart", "aprel", "may", "iyun", "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr"];
const bakuOf = (iso: string) => new Date(new Date(iso).getTime() + 4 * 3600_000);
const dayOf = (iso: string) => {
  const d = bakuOf(iso);
  return `${d.getUTCDate()} ${MONTHS[d.getUTCMonth()]} ${d.getUTCFullYear()}`;
};
const timeOf = (iso: string) => {
  const d = bakuOf(iso);
  return `${String(d.getUTCHours()).padStart(2, "0")}:${String(d.getUTCMinutes()).padStart(2, "0")}`;
};

/**
 * The conversation with the person, as a chat: their words on the left, ours on the right, the box to answer
 * below it. The latest message is always in view. A closed request cannot be answered until it is reopened.
 */
export function TicketChat({
  id,
  status,
  userName,
  messages: saved,
}: {
  id: string;
  status: number;
  userName?: string | null;
  messages: ChatMessage[];
}) {
  const router = useRouter();
  const liveLine = useSupportLive();
  const typingSender = useTypingSender(id);
  const personTyping = useTypingFrom(id, false);
  // Messages that came in live; the page refresh brings the saved ones, and the two are joined by id.
  const [arrived, setArrived] = useState<ChatMessage[]>([]);
  const messages = [...saved, ...arrived.filter((m) => !saved.some((x) => x.id === m.id))].sort((a, b) =>
    a.sentAtUtc.localeCompare(b.sentAtUtc),
  );
  const scroller = useRef<HTMLDivElement>(null);
  const box = useRef<HTMLTextAreaElement>(null);
  const [text, setText] = useState("");
  // After the answer the request moves on: a new one becomes "being worked on" by default.
  const [after, setAfter] = useState<string>(status === 1 ? "2" : "keep");
  const [showReplies, setShowReplies] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const attach = useAttachments({
    type: "Yalnız JPG, PNG və ya WebP şəkil göndərmək olar.",
    size: "Hər şəkil ən çoxu 10 MB ola bilər.",
    count: "Bir mesajda ən çoxu 5 şəkil göndərmək olar.",
  });

  const closed = status === 4;

  useEffect(
    () =>
      liveLine.subscribe((event) => {
        if (event.type !== "message" || event.ticketId !== id) return;
        setArrived((list) => (list.some((m) => m.id === event.message.id) ? list : [...list, event.message]));
      }),
    [liveLine, id],
  );

  // Always show the newest message (and the "is typing" line under it).
  useEffect(() => {
    const el = scroller.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [messages.length, personTyping]);

  async function send() {
    const body = text.trim();
    if ((!body && attach.files.length === 0) || busy) return;
    setBusy(true);
    setError(null);
    let res: { ok: true } | { ok: false; unauthorized: boolean; message: string };
    if (attach.files.length > 0) {
      const sent = await postPhotos(`/bff/admin-support/${id}/images`, attach.files, body);
      res = sent.ok
        ? { ok: true }
        : {
            ok: false,
            unauthorized: sent.status === 401,
            message: sent.status === 429 ? "Çox tez-tez cəhd olunub. Bir az gözləyin." : "Şəkil göndərilmədi. Yenidən cəhd edin.",
          };
    } else {
      res = await replyAsAdmin(id, body);
    }
    if (!res.ok) {
      setBusy(false);
      if (res.unauthorized) router.push(adminHref("/giris"));
      else setError(res.message);
      return;
    }
    attach.clear();
    if (after !== "keep" && Number(after) !== status) {
      const moved = await setTicketStatus(id, Number(after));
      if (!moved.ok && !moved.unauthorized) setError(`Cavab göndərildi, amma status dəyişmədi: ${moved.message}`);
    }
    setBusy(false);
    setText("");
    typingSender.stopped();
    setShowReplies(false);
    router.refresh();
    box.current?.focus();
  }

  async function reopen() {
    setBusy(true);
    setError(null);
    const res = await setTicketStatus(id, 2);
    setBusy(false);
    if (res.ok) router.refresh();
    else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  // A date line above the first message of a day; a name above the first message of a run from the same side.
  const startsDay = messages.map((m, i) => i === 0 || dayOf(m.sentAtUtc) !== dayOf(messages[i - 1].sentAtUtc));
  const sideOf = (m: ChatMessage) => `${m.isAdmin ? "a" : "u"}-${m.senderName ?? ""}`;
  const startsRun = messages.map((m, i) => startsDay[i] || sideOf(m) !== sideOf(messages[i - 1]));

  return (
    <div className="flex h-[calc(100dvh-14rem)] min-h-[460px] flex-col overflow-hidden rounded-xl border border-line bg-surface">
      <div ref={scroller} className="flex-1 space-y-1 overflow-y-auto bg-bg p-4">
        {messages.length === 0 && <p className="py-10 text-center text-sm text-ink-2">Mesaj yoxdur</p>}
        {messages.map((m, i) => {
          const day = dayOf(m.sentAtUtc);
          const showDay = startsDay[i];
          const showName = startsRun[i];
          const name = m.isAdmin ? (m.senderName ?? "Dəstək") : (m.senderName ?? userName ?? "İstifadəçi");
          return (
            <div key={m.id}>
              {showDay && <p className="my-3 text-center text-[11px] font-bold uppercase tracking-wide text-ink-2">{day}</p>}
              <div className={`flex flex-col ${m.isAdmin ? "items-end" : "items-start"} ${showName ? "pt-2" : ""}`}>
                {showName && <span className="mb-0.5 px-1 text-[11px] font-bold text-ink-2">{m.isAdmin ? `${name} (dəstək)` : name}</span>}
                <div
                  className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3.5 py-2 text-[15px] ${
                    m.isAdmin ? "rounded-br-md bg-primary text-white" : "rounded-bl-md border border-line bg-surface"
                  }`}
                >
                  <MessagePhotos sources={(m.images ?? []).map((i) => `/bff/admin-support-image/${id}/${i.id}`)} />
                  {m.body}
                  <span className={`ml-3 inline-block translate-y-0.5 text-[10px] ${m.isAdmin ? "text-white/70" : "text-ink-2"}`}>{timeOf(m.sentAtUtc)}</span>
                </div>
              </div>
            </div>
          );
        })}
        {personTyping && (
          <div className="flex items-center gap-2 pt-2 text-xs font-semibold text-ink-2" aria-live="polite">
            <span className="flex gap-0.5" aria-hidden>
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2 [animation-delay:-0.3s]" />
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2 [animation-delay:-0.15s]" />
              <span className="size-1.5 animate-bounce rounded-full bg-ink-2" />
            </span>
            {userName ?? "İstifadəçi"} yazır…
          </div>
        )}
      </div>

      <div className="border-t border-line bg-surface p-3">
        {closed ? (
          <div className="flex flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-ink-2">Müraciət bağlıdır: cavab yazmaq olmur.</p>
            <button
              type="button"
              onClick={() => void reopen()}
              disabled={busy}
              className="h-10 rounded-lg border border-primary px-4 text-sm font-bold text-primary hover:bg-primary-soft disabled:opacity-60"
            >
              Yenidən aç
            </button>
          </div>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void send();
            }}
            className="space-y-2"
          >
            {showReplies && (
              <div className="flex flex-wrap gap-1.5 rounded-lg bg-bg p-2">
                {QUICK_REPLIES.map((r) => (
                  <button
                    key={r.label}
                    type="button"
                    title={r.text}
                    aria-label={r.label}
                    onClick={() => {
                      setText((t) => (t.trim() ? `${t.trim()}\n${r.text}` : r.text));
                      box.current?.focus();
                    }}
                    className="rounded-full border border-line bg-surface px-3 py-1 text-xs font-semibold hover:border-primary hover:text-primary"
                  >
                    {r.label}
                  </button>
                ))}
              </div>
            )}

            <AttachPreview attach={attach} disabled={busy} removeLabel="Şəkli sil" />

            <div className="flex items-end gap-2">
              <AttachButton attach={attach} disabled={busy} label="Şəkil əlavə et" />
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
                rows={Math.min(6, Math.max(2, text.split("\n").length))}
                maxLength={4000}
                disabled={busy}
                placeholder="Cavab yazın… (Enter: göndər, Shift+Enter: yeni sətir)"
                className="min-h-[3.25rem] flex-1 resize-none rounded-xl border border-line bg-surface px-3.5 py-2.5 text-[15px] outline-none focus:border-primary"
              />
              <button
                type="submit"
                disabled={busy || (!text.trim() && attach.files.length === 0)}
                aria-label="Göndər"
                className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-primary text-white hover:bg-primary-press disabled:opacity-50"
              >
                {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
              </button>
            </div>

            <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
              <button type="button" onClick={() => setShowReplies((v) => !v)} className="flex items-center gap-1 font-bold text-primary hover:underline">
                <MessageSquareText className="size-4" aria-hidden />
                Hazır cavablar
              </button>
              <label className="flex items-center gap-1.5 font-semibold text-ink-2">
                Göndərəndən sonra:
                <select
                  value={after}
                  onChange={(e) => setAfter(e.target.value)}
                  disabled={busy}
                  className="rounded-md border border-line bg-surface px-1.5 py-1 text-xs font-bold text-ink"
                >
                  <option value="keep">statusu saxla</option>
                  {status !== 2 && <option value="2">«İşlənir» et</option>}
                  <option value="3">«Həll olundu» et</option>
                  <option value="4">Bağla</option>
                </select>
              </label>
            </div>
          </form>
        )}
        {(error ?? attach.problem) && (
          <p role="alert" className="mt-2 text-sm font-semibold text-danger">
            {error ?? attach.problem}
          </p>
        )}
      </div>
    </div>
  );
}
