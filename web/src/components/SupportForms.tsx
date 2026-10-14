"use client";

import { Select } from "@/components/ui/Select";
import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, Send } from "lucide-react";
import { createTicket, replyTicket } from "@/app/destek/actions";
import { ticketCategory } from "@/lib/support";

const input =
  "w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

/** A new support request. */
export function NewTicketForm({ initialCategory = 1 }: { initialCategory?: number }) {
  const t = useT();
  const router = useRouter();
  const [category, setCategory] = useState(initialCategory);
  const [subject, setSubject] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function send() {
    if (busy) return;
    setBusy(true);
    setError(null);
    const result = await createTicket({ category, subject, message });
    if (result.ok) {
      router.push(`/destek/${result.id}`);
      return;
    }
    setBusy(false);
    if (result.unauthorized) router.push("/giris?next=/destek/yeni");
    else setError(result.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void send();
      }}
      className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6"
    >
      <div className="block">
        <span className="mb-2 block text-sm font-bold">{t("Növ")}</span>
        <Select
          value={String(category)}
          onChange={(v) => setCategory(Number(v))}
          disabled={busy}
          ariaLabel={t("Növ")}
          className="h-11"
          options={Object.entries(ticketCategory).map(([id, label]) => ({ value: id, label: t(label) }))}
        />
      </div>

      <label className="block">
        <span className="mb-2 block text-sm font-bold">{t("Mövzu")}</span>
        <input
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          maxLength={160}
          disabled={busy}
          className={`${input} h-11`}
        />
      </label>

      <label className="block">
        <span className="mb-2 block text-sm font-bold">{t("Mesaj")}</span>
        <textarea
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          maxLength={4000}
          rows={6}
          disabled={busy}
          className={`${input} py-3`}
        />
      </label>

      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}

      <button
        type="submit"
        disabled={busy}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-60"
      >
        {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
        {t("Göndər")}</button>
    </form>
  );
}

/** The answer box under a request. */
export function ReplyForm({ ticketId }: { ticketId: string }) {
  const t = useT();
  const router = useRouter();
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function send() {
    if (busy || !text.trim()) return;
    setBusy(true);
    setError(null);
    const result = await replyTicket(ticketId, text);
    setBusy(false);
    if (result.ok) {
      setText("");
      router.refresh();
    } else if (result.unauthorized) router.push(`/giris?next=/destek/${ticketId}`);
    else setError(result.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void send();
      }}
      className="space-y-3"
    >
      <textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        maxLength={4000}
        rows={3}
        disabled={busy}
        placeholder={t("Cavabınızı yazın")}
        className={`${input} py-3`}
      />
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="submit"
        disabled={busy || !text.trim()}
        className="flex h-11 items-center gap-2 rounded-xl bg-primary px-6 font-bold text-white hover:bg-primary-press disabled:opacity-60"
      >
        {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
        {t("Göndər")}</button>
    </form>
  );
}
