"use client";

import { useT } from "@/lib/i18n/client";
import { useEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Loader2, MessageCircle } from "lucide-react";
import { startConversation } from "@/app/mesajlar/actions";
import { useFavorites } from "./FavoritesProvider";

/** "Mesaj yaz": signed out it leads to login first; signed in it asks for the first message. */
export function MessageButton({ listingId, title }: { listingId: string; title: string }) {
  const t = useT();
  const { signedIn } = useFavorites();
  const router = useRouter();
  const pathname = usePathname();
  const dialog = useRef<HTMLDialogElement>(null);
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const d = dialog.current;
    if (!d) return;
    if (open && !d.open) d.showModal();
    if (!open && d.open) d.close();
  }, [open]);

  async function send() {
    if (busy) return;
    if (!text.trim()) {
      setError(t("Mesajı yazın."));
      return;
    }
    setBusy(true);
    setError(null);
    const res = await startConversation(listingId, text);
    if (res.ok) {
      router.push(`/mesajlar/${res.conversationId}`);
      return;
    }
    setBusy(false);
    if (res.unauthorized) router.push(`/giris?next=${encodeURIComponent(pathname)}`);
    else setError(res.message);
  }

  return (
    <>
      <button
        type="button"
        onClick={() => (signedIn ? setOpen(true) : router.push(`/giris?next=${encodeURIComponent(pathname)}`))}
        className="flex h-10 w-full items-center justify-center gap-1.5 rounded-lg bg-primary px-3 text-[14px] font-bold text-white hover:bg-primary-press"
      >
        <MessageCircle className="size-4" aria-hidden />
        {t("Mesaj yaz")}
      </button>

      <dialog
        ref={dialog}
        onClose={() => setOpen(false)}
        onClick={(e) => {
          if (e.target === dialog.current && !busy) setOpen(false);
        }}
        className="m-auto w-[min(92vw,460px)] rounded-2xl border border-line bg-surface p-0 text-ink backdrop:bg-black/60"
      >
        <form
          onSubmit={(e) => {
            e.preventDefault();
            void send();
          }}
          className="space-y-3 p-6"
        >
          <h2 className="text-lg font-extrabold">{t("Elan sahibinə yazın")}</h2>
          <p className="truncate text-sm text-ink-2">{title}</p>
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={4}
            maxLength={4000}
            autoFocus
            placeholder={t("Salam, elan hələ aktualdır?")}
            className="w-full resize-none rounded-xl border border-line bg-bg px-4 py-3 text-[16px] outline-none focus:border-primary"
          />
          {error && (
            <p role="alert" className="text-sm font-semibold text-danger">
              {error}
            </p>
          )}
          <div className="flex gap-2">
            <button
              type="button"
              disabled={busy}
              onClick={() => setOpen(false)}
              className="h-11 flex-1 rounded-xl border border-line font-bold hover:border-primary"
            >
              {t("Ləğv et")}</button>
            <button
              type="submit"
              disabled={busy}
              className="flex h-11 flex-1 items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-60"
            >
              {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
              {t("Göndər")}</button>
          </div>
        </form>
      </dialog>
    </>
  );
}
