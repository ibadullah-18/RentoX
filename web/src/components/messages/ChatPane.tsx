"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import { dateFmt } from "@/lib/format";
import type { Locale, T } from "@/lib/i18n/locales";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import {
  AlertCircle,
  Ban,
  Check,
  CheckCheck,
  ChevronLeft,
  ImagePlus,
  Loader2,
  Send,
  X,
} from "lucide-react";
import {
  latestMessages,
  markRead,
  olderMessages,
  sendMessage,
  setBlock,
  standing,
} from "@/app/mesajlar/actions";
import type { ChatMessage, Conversation } from "@/lib/types";
import { ConfirmDialog } from "../ConfirmDialog";
import { useInbox } from "./MessagesShell";
import { useRealtime } from "./Realtime";
import { ReportDialog } from "../ReportDialog";
import { UserAvatar } from "./UserAvatar";

/** A message as shown: the server's, or one still being sent. */
type Item = ChatMessage & {
  /** The key of the message while it was being sent: the bubble stays the same when the server confirms it. */
  tempKey?: string;
  /** Present while a message of mine is not confirmed yet. */
  local?: { state: "sending" | "failed"; photos: File[]; previews: string[] };
};

const MAX_PHOTOS = 5;
const MAX_BYTES = 10 * 1024 * 1024;
const TYPES = ["image/jpeg", "image/png", "image/webp"];

function dayLabel(iso: string, t: T, locale: Locale): string {
  const d = new Date(iso);
  const start = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate()).getTime();
  const days = Math.round((start(new Date()) - start(d)) / 86_400_000);
  if (days === 0) return t("Bu gün");
  if (days === 1) return t("Dünən");
  return dateFmt(locale, { day: "numeric", month: "long", year: "numeric" }).format(d);
}

/** The API's private photo address → the site's own route that adds the token. */
function photoUrl(conversationId: string, image: { id: string; url: string }): string {
  return `/bff/chat-image/${conversationId}/${image.id}`;
}

/** Oldest first, no duplicates (the same message can arrive twice). */
function merge(current: Item[], incoming: ChatMessage[]): Item[] {
  const byId = new Map(current.map((m) => [m.id, m]));
  for (const m of incoming) if (!byId.has(m.id)) byId.set(m.id, m);
  return [...byId.values()].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
}

export function ChatPane({
  conversation,
  initial,
  hasMore: initialHasMore,
  me,
}: {
  conversation: Conversation;
  /** Newest first, as the API returns them. */
  initial: ChatMessage[];
  hasMore: boolean;
  me: string;
}) {
  const t = useT();
  const locale = useLocale();
  const cid = conversation.id;
  const router = useRouter();
  const inbox = useInbox();
  const realtime = useRealtime();

  const [items, setItems] = useState<Item[]>(() => merge([], initial));
  const [hasMore, setHasMore] = useState(initialHasMore);
  const [olderPage, setOlderPage] = useState(2);
  const [loadingOlder, setLoadingOlder] = useState(false);

  const [text, setText] = useState("");
  const [photos, setPhotos] = useState<{ file: File; url: string }[]>([]);
  const [notice, setNotice] = useState<string | null>(null);

  const [online, setOnline] = useState(false);
  const [typing, setTyping] = useState(false);
  const [blocked, setBlocked] = useState({ byMe: false, canSend: true });
  const [askBlock, setAskBlock] = useState(false);
  const [viewer, setViewer] = useState<string | null>(null);

  const scroller = useRef<HTMLDivElement>(null);
  const stick = useRef(true); // keep the newest message in view
  const keepFrom = useRef<number | null>(null); // scroll height before older messages were added
  const typingTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastTypingSent = useRef(0);
  const fileInput = useRef<HTMLInputElement>(null);
  const box = useRef<HTMLTextAreaElement>(null);

  // ---- state of the other person and of the block -----------------------------
  const refreshStanding = useCallback(async () => {
    const s = await standing(cid);
    setOnline(s.online);
    setBlocked({ byMe: s.blockedByMe, canSend: s.canSend });
  }, [cid]);

  useEffect(() => {
    // One call to the server for who is online and who is blocked.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void refreshStanding();
    const timer = setInterval(() => void refreshStanding(), 60_000);
    return () => clearInterval(timer);
  }, [refreshStanding]);

  // ---- reading -----------------------------------------------------------------
  const read = useCallback(() => {
    inbox.clearUnread(cid);
    void markRead(cid);
  }, [cid, inbox]);

  useEffect(() => {
    if (conversation.unreadCount > 0) read();
  }, [conversation.unreadCount, read]);

  // ---- live events --------------------------------------------------------------
  useEffect(
    () =>
      realtime.subscribe((event) => {
        if (event.type === "reconnected") {
          void latestMessages(cid).then((r) => {
            if (r.ok) setItems((cur) => merge(cur, r.messages));
          });
          void refreshStanding();
          return;
        }
        if (event.type === "message" && event.message.conversationId === cid) {
          const m = event.message;
          setItems((cur) => merge(cur, [m]));
          if (m.senderId !== me) {
            setTyping(false);
            read();
          }
        }
        if (event.type === "read" && event.conversationId === cid && event.readByUserId !== me) {
          setItems((cur) =>
            cur.map((m) => (m.senderId === me && !m.readAtUtc ? { ...m, readAtUtc: event.readAtUtc } : m)),
          );
        }
        if (event.type === "typing" && event.conversationId === cid && event.userId !== me) {
          setTyping(event.typing);
          if (typingTimer.current) clearTimeout(typingTimer.current);
          // A "typing" that never gets its "stopped" must not stay forever.
          if (event.typing) typingTimer.current = setTimeout(() => setTyping(false), 6000);
        }
      }),
    [realtime, cid, me, read, refreshStanding],
  );

  useEffect(() => () => {
    if (typingTimer.current) clearTimeout(typingTimer.current);
  }, []);

  // ---- scrolling -----------------------------------------------------------------
  useLayoutEffect(() => {
    const el = scroller.current;
    if (!el) return;
    if (keepFrom.current !== null) {
      // Older messages were added above: stay where the reader was.
      el.scrollTop = el.scrollHeight - keepFrom.current;
      keepFrom.current = null;
    } else if (stick.current) {
      el.scrollTop = el.scrollHeight;
    }
  }, [items]);

  // The writing box grows with the text (up to a limit) and shrinks after sending.
  useLayoutEffect(() => {
    const el = box.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 128)}px`;
    el.style.overflowY = el.scrollHeight > 128 ? "auto" : "hidden";
  }, [text]);

  function onScroll() {
    const el = scroller.current;
    if (el) stick.current = el.scrollHeight - el.scrollTop - el.clientHeight < 120;
  }

  async function loadOlder() {
    if (loadingOlder) return;
    setLoadingOlder(true);
    const res = await olderMessages(cid, olderPage);
    setLoadingOlder(false);
    if (!res.ok) return;
    keepFrom.current = scroller.current?.scrollHeight ?? 0;
    setItems((cur) => merge(cur, res.messages.items));
    setOlderPage(olderPage + 1);
    setHasMore(res.messages.page < res.messages.totalPages);
  }

  // ---- typing -------------------------------------------------------------------
  function typed(value: string) {
    setText(value);
    const now = Date.now();
    if (value && now - lastTypingSent.current > 3000) {
      lastTypingSent.current = now;
      realtime.startTyping(cid);
    }
    if (!value) {
      lastTypingSent.current = 0;
      realtime.stopTyping(cid);
    }
  }

  // ---- photos -------------------------------------------------------------------
  function pick(files: FileList | null) {
    if (!files) return;
    const next = [...photos];
    let problem: string | null = null;
    for (const file of Array.from(files)) {
      if (next.length >= MAX_PHOTOS) {
        problem = t("Bir mesajda ən çoxu {n} şəkil göndərmək olar.", { n: MAX_PHOTOS });
        break;
      }
      if (!TYPES.includes(file.type)) problem = t("Yalnız JPEG, PNG və WebP şəkilləri qəbul olunur.");
      else if (file.size === 0 || file.size > MAX_BYTES) problem = t("Hər şəkil ən çoxu 10 MB ola bilər.");
      else next.push({ file, url: URL.createObjectURL(file) });
    }
    setNotice(problem);
    setPhotos(next);
  }

  function dropPhoto(i: number) {
    URL.revokeObjectURL(photos[i].url);
    setPhotos(photos.filter((_, n) => n !== i));
  }

  // ---- sending ------------------------------------------------------------------
  async function deliver(tempId: string, body: string, files: File[]) {
    try {
      let message: ChatMessage | null = null;
      let unauthorized = false;

      if (files.length === 0) {
        const res = await sendMessage(cid, body);
        if (res.ok) message = res.message;
        else if (res.unauthorized) unauthorized = true;
        else setNotice(res.message);
      } else {
        const form = new FormData();
        if (body) form.set("body", body);
        files.forEach((f) => form.append("files", f, f.name));
        const res = await fetch(`/bff/conversations/${cid}/images`, { method: "POST", body: form });
        if (res.ok) message = (await res.json()) as ChatMessage;
        else if (res.status === 401) unauthorized = true;
        else setNotice(res.status === 429 ? t("Çox tez-tez mesaj göndərilir. Bir az gözləyin.") : t("Mesaj göndərilmədi."));
      }

      if (unauthorized) {
        router.push(`/giris?next=/mesajlar/${cid}`);
        return;
      }
      if (!message) throw new Error("not sent");

      const sent: Item = { ...message, tempKey: tempId };
      setItems((cur) => merge(cur.filter((m) => m.id !== tempId), [sent]));
      inbox.touch(cid, sent.body || "📷", sent.sentAtUtc);
    } catch {
      setItems((cur) =>
        cur.map((m) => (m.id === tempId && m.local ? { ...m, local: { ...m.local, state: "failed" } } : m)),
      );
    }
  }

  function send() {
    const body = text.trim();
    if ((!body && photos.length === 0) || !blocked.canSend) return;

    const tempId = `local-${crypto.randomUUID()}`;
    const files = photos.map((p) => p.file);
    const previews = photos.map((p) => p.url);

    stick.current = true;
    setItems((cur) => [
      ...cur,
      {
        id: tempId,
        conversationId: cid,
        senderId: me,
        body,
        sentAtUtc: new Date().toISOString(),
        local: { state: "sending", photos: files, previews },
      },
    ]);
    setText("");
    setPhotos([]);
    setNotice(null);
    lastTypingSent.current = 0;
    realtime.stopTyping(cid);
    void deliver(tempId, body, files);
  }

  function retry(item: Item) {
    if (!item.local) return;
    const { photos: files } = item.local;
    setItems((cur) =>
      cur.map((m) => (m.id === item.id && m.local ? { ...m, local: { ...m.local, state: "sending" } } : m)),
    );
    void deliver(item.id, item.body, files);
  }

  function discard(item: Item) {
    item.local?.previews.forEach((u) => URL.revokeObjectURL(u));
    setItems((cur) => cur.filter((m) => m.id !== item.id));
  }

  async function toggleBlock() {
    const next = !blocked.byMe;
    const res = await setBlock(cid, next);
    setAskBlock(false);
    if (res.ok) await refreshStanding();
    else if (!res.unauthorized) setNotice(res.message);
  }

  // ---- drawing ------------------------------------------------------------------
  const rows = useMemo(() => {
    const out: { item: Item; day: string | null; lastOfRun: boolean; startsRun: boolean }[] = [];
    items.forEach((item, i) => {
      const prev = items[i - 1];
      const next = items[i + 1];
      const day = dayLabel(item.sentAtUtc, t, locale);
      const newDay = !prev || dayLabel(prev.sentAtUtc, t, locale) !== day;
      const sameAsPrev = prev && !newDay && prev.senderId === item.senderId;
      const sameAsNext = next && dayLabel(next.sentAtUtc, t, locale) === day && next.senderId === item.senderId;
      out.push({ item, day: newDay ? day : null, startsRun: !sameAsPrev, lastOfRun: !sameAsNext });
    });
    return out;
  }, [items, t, locale]);

  const status = typing ? t("yazır…") : online ? "Onlayn" : "Oflayn";

  return (
    <>
      <header className="flex items-center gap-3 border-b border-line px-3 py-3 md:px-4">
        <Link
          href="/mesajlar"
          aria-label={t("Geri")}
          className="flex size-10 items-center justify-center rounded-full hover:bg-primary-soft md:hidden"
        >
          <ChevronLeft className="size-6" />
        </Link>
        <UserAvatar userId={conversation.otherUserId} size={44} hasPhoto={conversation.otherHasPhoto !== false} />
        <div className="min-w-0 flex-1">
          <Link
            href={`/listing/${conversation.listingId}`}
            className="block truncate font-extrabold hover:text-primary"
          >
            {conversation.listingTitle}
          </Link>
          <p className={`text-xs font-semibold ${typing || online ? "text-success" : "text-ink-2"}`}>{status}</p>
        </div>
        <ReportDialog kind="chat" targetId={cid} variant="icon" />
        <button
          type="button"
          onClick={() => setAskBlock(true)}
          aria-label={blocked.byMe ? t("Blokdan çıxar") : "Blokla"}
          title={blocked.byMe ? t("Blokdan çıxar") : "Blokla"}
          className="flex size-10 items-center justify-center rounded-full text-ink-2 hover:bg-primary-soft hover:text-danger"
        >
          <Ban className="size-5" />
        </button>
      </header>

      <div ref={scroller} onScroll={onScroll} className="min-h-0 flex-1 overflow-y-auto bg-bg px-3 py-4 md:px-5">
        {hasMore && (
          <div className="mb-4 flex justify-center">
            <button
              type="button"
              onClick={loadOlder}
              disabled={loadingOlder}
              className="flex h-9 items-center gap-2 rounded-lg border border-line bg-surface px-4 text-sm font-semibold hover:border-primary disabled:opacity-60"
            >
              {loadingOlder && <Loader2 className="size-4 animate-spin" aria-hidden />}
              {t("Əvvəlki mesajlar")}</button>
          </div>
        )}

        {rows.length === 0 && <p className="py-10 text-center text-ink-2">{t("Söhbəti başladın")}</p>}

        {rows.map(({ item, day, startsRun, lastOfRun }) => (
          <div key={item.tempKey ?? item.id}>
            {day && (
              <div className="my-4 flex justify-center">
                <span className="rounded-lg bg-surface px-3 py-1 text-xs font-semibold text-ink-2">{day}</span>
              </div>
            )}
            <Bubble
              item={item}
              cid={cid}
              mine={item.senderId === me}
              spaced={startsRun}
              showTime={lastOfRun}
              onOpen={setViewer}
              onRetry={() => retry(item)}
              onDiscard={() => discard(item)}
            />
          </div>
        ))}
      </div>

      {photos.length > 0 && (
        <ul className="flex gap-2 overflow-x-auto border-t border-line bg-surface px-3 py-2">
          {photos.map((p, i) => (
            <li key={p.url} className="relative size-16 shrink-0 overflow-hidden rounded-lg">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={p.url} alt="" className="size-full object-cover" />
              <button
                type="button"
                onClick={() => dropPhoto(i)}
                aria-label={t("Şəkli sil")}
                className="absolute right-0.5 top-0.5 flex size-5 items-center justify-center rounded-full bg-ink/70 text-white"
              >
                <X className="size-3" />
              </button>
            </li>
          ))}
        </ul>
      )}

      {notice && (
        <p role="alert" className="border-t border-line bg-surface px-4 py-2 text-sm font-semibold text-danger">
          {notice}
        </p>
      )}

      {blocked.canSend ? (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            send();
          }}
          className="flex items-end gap-2 border-t border-line bg-surface p-2.5 md:p-3"
        >
          <button
            type="button"
            onPointerDown={(e) => e.preventDefault()}
            onClick={() => fileInput.current?.click()}
            aria-label={t("Şəkil əlavə et")}
            className="flex size-11 shrink-0 items-center justify-center rounded-xl text-ink-2 hover:bg-primary-soft hover:text-primary"
          >
            <ImagePlus className="size-6" />
          </button>
          <input
            ref={fileInput}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            multiple
            hidden
            onChange={(e) => {
              pick(e.target.files);
              e.target.value = "";
            }}
          />
          <textarea
            ref={box}
            value={text}
            onChange={(e) => typed(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                send();
              }
            }}
            rows={1}
            maxLength={4000}
            placeholder={t("Mesaj yazın")}
            className="max-h-32 min-h-11 flex-1 resize-none overflow-y-hidden rounded-lg border border-line bg-bg px-4 py-2.5 text-[16px] outline-none focus:border-primary"
          />
          <button
            type="submit"
            onPointerDown={(e) => e.preventDefault()}
            disabled={!text.trim() && photos.length === 0}
            aria-label={t("Göndər")}
            className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary text-white hover:bg-primary-press disabled:opacity-50"
          >
            <Send className="size-5" />
          </button>
        </form>
      ) : (
        <p className="border-t border-line bg-surface px-4 py-4 text-center text-sm font-semibold text-ink-2">
          {t("Bu söhbətə mesaj göndərmək mümkün deyil.")}</p>
      )}

      <ConfirmDialog
        open={askBlock}
        title={blocked.byMe ? t("Blokdan çıxarılsın?") : t("İstifadəçi bloklansın?")}
        message={
          blocked.byMe
            ? t("Bu istifadəçi yenidən sizə yaza biləcək.")
            : t("Bu istifadəçi sizə yaza bilməyəcək. İstədiyiniz vaxt blokdan çıxara bilərsiniz.")
        }
        confirmLabel={blocked.byMe ? t("Blokdan çıxar") : "Blokla"}
        destructive={!blocked.byMe}
        onClose={() => setAskBlock(false)}
        onConfirm={toggleBlock}
      />

      {viewer && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90"
          onClick={() => setViewer(null)}
        >
          <button
            type="button"
            onClick={() => setViewer(null)}
            aria-label={t("Bağla")}
            className="absolute right-4 top-4 flex size-11 items-center justify-center rounded-full bg-white/15 text-white"
          >
            <X className="size-6" />
          </button>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={viewer}
            alt=""
            onClick={(e) => e.stopPropagation()}
            className="max-h-[90vh] max-w-[92vw] object-contain"
          />
        </div>
      )}
    </>
  );
}

/**
 * A message. Like WhatsApp on iPhone: a photo sent alone has no frame; a
 * photo sent with words is one card as wide as the photo, the words directly
 * under it.
 */
function Bubble({
  item,
  cid,
  mine,
  spaced,
  showTime,
  onOpen,
  onRetry,
  onDiscard,
}: {
  item: Item;
  cid: string;
  mine: boolean;
  spaced: boolean;
  showTime: boolean;
  onOpen: (url: string) => void;
  onRetry: () => void;
  onDiscard: () => void;
}) {
  const t = useT();
  const locale = useLocale();
  const sources = item.local ? item.local.previews : (item.images ?? []).map((i) => photoUrl(cid, i));
  const hasImages = sources.length > 0;
  const hasText = item.body.length > 0;
  const sending = item.local?.state === "sending";
  const failed = item.local?.state === "failed";

  const imageWidth = sources.length === 1 ? 240 : 242; // two columns of 120 and a 2 px gap
  const tile = sources.length === 1 ? 240 : 120;

  const shape = mine ? "rounded-2xl rounded-br-md" : "rounded-2xl rounded-bl-md";
  const framed = !(hasImages && !hasText);

  return (
    <div className={`flex flex-col ${mine ? "items-end" : "items-start"} ${spaced ? "mt-3" : "mt-0.5"}`}>
      <div
        style={hasImages ? { width: `min(${imageWidth}px, 78%)` } : undefined}
        className={`max-w-[78%] overflow-hidden ${sending ? "opacity-70" : ""} ${
          framed
            ? `${shape} ${mine ? "bg-primary text-white" : "border border-line bg-surface"} ${hasImages ? "" : "px-3.5 py-2.5"}`
            : "rounded-2xl"
        }`}
      >
        {hasImages && (
          <div className="flex flex-wrap gap-0.5">
            {sources.map((src, i) => (
              <button
                key={src}
                type="button"
                onClick={() => !item.local && onOpen(src)}
                style={{
                  width: sources.length === 1 ? "100%" : `calc(50% - 1px)`,
                  aspectRatio: "1 / 1",
                  maxWidth: tile,
                }}
                className="block overflow-hidden"
                aria-label={t("Şəkil {n}", { n: i + 1 })}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={src} alt="" className="size-full object-cover" />
              </button>
            ))}
          </div>
        )}
        {hasText && (
          <p className={`whitespace-pre-wrap break-words text-[15px] leading-snug ${hasImages ? "px-3 pb-2.5 pt-2" : ""}`}>
            {item.body}
          </p>
        )}
      </div>

      {failed ? (
        <p className="mt-1 flex items-center gap-2 text-xs font-semibold text-danger">
          <AlertCircle className="size-4" aria-hidden />
          {t("Göndərilmədi")}<button type="button" onClick={onRetry} className="font-bold text-primary hover:underline">
            {t("Yenidən")}</button>
          <button type="button" onClick={onDiscard} className="font-bold text-primary hover:underline">
            {t("Ləğv et")}</button>
        </p>
      ) : (
        showTime && (
          <p className="mt-0.5 flex items-center gap-1 px-1 text-[11px] text-ink-2">
            {dateFmt(locale, { hour: "2-digit", minute: "2-digit" }).format(new Date(item.sentAtUtc))}
            {mine &&
              !sending &&
              (item.readAtUtc ? (
                <CheckCheck className="size-4 text-primary" aria-label={t("Oxunub")} />
              ) : (
                <Check className="size-4" aria-label={t("Göndərilib")} />
              ))}
          </p>
        )
      )}
    </div>
  );
}
