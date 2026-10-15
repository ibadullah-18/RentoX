"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { sendAnnouncement } from "@/app/admin/(panel)/announce-actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";

const input =
  "w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

/** A message to one person (`userId`) or, without it, to everyone with an active account. */
export function AnnouncementForm({ userId }: { userId?: string }) {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [link, setLink] = useState("");
  const [audience, setAudience] = useState(1);
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState<number | null>(null);

  async function send() {
    setBusy(true);
    setError(null);
    setSent(null);
    const res = await sendAnnouncement(userId ?? null, audience, title, body, link);
    setBusy(false);
    setAsking(false);
    if (res.ok) {
      setSent(res.recipients);
      setTitle("");
      setBody("");
      setLink("");
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (title.trim() && body.trim()) setAsking(true);
      }}
      className="space-y-4 rounded-xl border border-line bg-surface p-4 md:p-5"
    >
      {!userId && (
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Kimə</span>
          <select value={audience} onChange={(e) => setAudience(Number(e.target.value))} disabled={busy} className={`${input} h-10`}>
            <option value={1}>Bütün aktiv istifadəçilərə</option>
            <option value={2}>Aktiv mağaza sahiblərinə</option>
            <option value={3}>Aktiv elanı olan satıcılara</option>
          </select>
        </label>
      )}
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Başlıq</span>
        <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={160} disabled={busy} className={`${input} h-10`} />
      </label>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Mətn</span>
        <textarea value={body} onChange={(e) => setBody(e.target.value)} maxLength={1000} rows={4} disabled={busy} className={`${input} py-2`} />
        <span className="mt-1 block text-right text-xs text-ink-2">{body.length} / 1000</span>
      </label>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Keçid (ixtiyari)</span>
        <input value={link} onChange={(e) => setLink(e.target.value)} placeholder="/destek" disabled={busy} className={`${input} h-10`} />
        <span className="mt-1 block text-xs text-ink-2">Saytın daxilində bir səhifə, məsələn /destek. Boş qala bilər.</span>
      </label>

      {!userId && (
        <p className="rounded-lg bg-warning-soft px-3 py-2 text-xs text-ink">
          Mesaj seçilmiş qrupa gedəcək və geri götürülə bilməz.
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !title.trim() || !body.trim()}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Göndər
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
        {sent !== null && (
          <p role="status" className="text-sm font-semibold text-success">
            {sent === 1 ? "Mesaj göndərildi." : `Mesaj ${sent} nəfərə göndərildi.`}
          </p>
        )}
      </div>

      <ConfirmDialog
        open={asking}
        title={userId ? "Mesaj göndərilsin?" : audience === 1 ? "Hamıya göndərilsin?" : "Qrupa göndərilsin?"}
        message={userId ? "İstifadəçi bildirişi dərhal alacaq." : audience === 1 ? "Bütün aktiv istifadəçilər bildirişi dərhal alacaq." : "Seçilmiş qrupdakı istifadəçilər bildirişi dərhal alacaq."}
        confirmLabel="Göndər"
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void send()}
      />
    </form>
  );
}
