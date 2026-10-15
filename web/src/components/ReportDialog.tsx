"use client";

import { useEffect, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { CheckCircle2, ChevronRight, Flag, Loader2 } from "lucide-react";
import { sendReport, type ReportKind } from "@/app/sikayet/actions";
import { useT } from "@/lib/i18n/client";
import { tx } from "@/lib/i18n/locales";

type Reason = { id: number; label: string };

// Same reasons as the app. A listing or store: 1 spam, 2 fraud, 3 prohibited, 4 misleading, 5 wrong category
// (not for a store), 6 other. A conversation: 1 spam, 2 fraud, 3 harassment, 4 prohibited, 5 other.
const REASONS: Record<ReportKind, Reason[]> = {
  listing: [
    { id: 1, label: tx("Spam və ya təkrar elan") },
    { id: 2, label: tx("Fırıldaq şübhəsi") },
    { id: 3, label: tx("Qadağan olunmuş məzmun") },
    { id: 4, label: tx("Yanıldıcı məlumat və ya şəkil") },
    { id: 5, label: tx("Yanlış kateqoriya") },
    { id: 6, label: tx("Digər") },
  ],
  store: [
    { id: 1, label: tx("Spam və ya təkrar elan") },
    { id: 2, label: tx("Fırıldaq şübhəsi") },
    { id: 3, label: tx("Qadağan olunmuş məzmun") },
    { id: 4, label: tx("Yanıldıcı məlumat və ya şəkil") },
    { id: 6, label: tx("Digər") },
  ],
  chat: [
    { id: 1, label: tx("Spam") },
    { id: 2, label: tx("Dələduzluq") },
    { id: 3, label: tx("Təhqir və ya təzyiq") },
    { id: 4, label: tx("Qadağan olunmuş məzmun") },
    { id: 5, label: tx("Digər") },
  ],
};

const TITLE: Record<ReportKind, string> = {
  listing: tx("Elanı şikayət et"),
  store: tx("Mağazanı şikayət et"),
  chat: tx("Söhbəti şikayət et"),
};

const HINT: Record<ReportKind, string> = {
  listing: tx("Elanla bağlı problem var? Bizə bildirin."),
  store: tx("Mağaza ilə bağlı problem var? Bizə bildirin."),
  chat: "",
};

/**
 * "Report a problem": a button (a row on a listing or store page, a small flag in a conversation) that opens a
 * window to choose the reason and explain. Signed-out people are sent to sign in first.
 */
export function ReportDialog({
  kind,
  targetId,
  variant = "row",
}: {
  kind: ReportKind;
  targetId: string;
  variant?: "row" | "icon";
}) {
  const t = useT();
  const router = useRouter();
  const path = usePathname();
  const ref = useRef<HTMLDialogElement>(null);
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState<number | null>(null);
  const [details, setDetails] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sent, setSent] = useState(false);

  const other = REASONS[kind].at(-1)!.id;

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  function show() {
    setReason(null);
    setDetails("");
    setError(null);
    setSent(false);
    setOpen(true);
  }

  async function submit() {
    if (reason === null || busy) return;
    if (reason === other && !details.trim()) {
      setError(t("Zəhmət olmasa nə baş verdiyini yazın"));
      return;
    }
    setBusy(true);
    setError(null);
    const result = await sendReport(kind, targetId, reason, details);
    setBusy(false);
    if (result.ok) setSent(true);
    else if (result.unauthorized) router.push(`/giris?next=${encodeURIComponent(path)}`);
    else setError(result.message);
  }

  return (
    <>
      {variant === "row" ? (
        <button
          type="button"
          onClick={show}
          className="flex w-full items-center gap-3 rounded-2xl border border-line bg-surface p-4 text-left hover:border-primary"
        >
          <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary">
            <Flag className="size-5" aria-hidden />
          </span>
          <span className="min-w-0 flex-1">
            <span className="block font-bold">{t("Problem bildir")}</span>
            {HINT[kind] && <span className="block text-sm text-ink-2">{t(HINT[kind])}</span>}
          </span>
          <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
        </button>
      ) : (
        <button
          type="button"
          onClick={show}
          aria-label={t("Şikayət et")}
          title={t("Şikayət et")}
          className="flex size-10 items-center justify-center rounded-full text-ink-2 hover:bg-primary-soft hover:text-danger"
        >
          <Flag className="size-5" aria-hidden />
        </button>
      )}

      <dialog
        ref={ref}
        onClose={() => setOpen(false)}
        onClick={(e) => {
          if (e.target === ref.current && !busy) setOpen(false);
        }}
        className="m-auto w-[min(94vw,460px)] rounded-2xl border border-line bg-surface p-0 text-ink backdrop:bg-black/60"
      >
        {sent ? (
          <div className="space-y-4 p-6 text-center">
            <CheckCircle2 className="mx-auto size-12 text-success" aria-hidden />
            <p className="font-bold">{t("Şikayətiniz göndərildi. Təşəkkür edirik.")}</p>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="h-11 w-full rounded-xl bg-primary font-bold text-white hover:bg-primary-press"
            >
              {t("Bağla")}
            </button>
          </div>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void submit();
            }}
            className="space-y-4 p-6"
          >
            <h2 className="text-lg font-extrabold">{t(TITLE[kind])}</h2>

            <fieldset className="space-y-1.5">
              <legend className="mb-2 text-sm font-bold">{t("Səbəbi seçin")}</legend>
              {REASONS[kind].map((r) => (
                <label
                  key={r.id}
                  className={`flex cursor-pointer items-center gap-3 rounded-xl border px-3.5 py-2.5 text-[15px] ${
                    reason === r.id ? "border-primary bg-primary-soft font-semibold" : "border-line hover:border-primary"
                  }`}
                >
                  <input
                    type="radio"
                    name="reason"
                    value={r.id}
                    checked={reason === r.id}
                    onChange={() => setReason(r.id)}
                    disabled={busy}
                    className="size-4 accent-primary"
                  />
                  {t(r.label)}
                </label>
              ))}
            </fieldset>

            <textarea
              value={details}
              onChange={(e) => setDetails(e.target.value)}
              maxLength={1000}
              rows={3}
              disabled={busy}
              placeholder={t("İstəsəniz, nə baş verdiyini yazın")}
              className="w-full resize-none rounded-xl border border-line bg-surface px-3.5 py-3 text-[16px] outline-none focus:border-primary disabled:opacity-60"
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
                className="h-11 flex-1 rounded-xl border border-line font-bold hover:border-primary disabled:opacity-60"
              >
                {t("Ləğv et")}
              </button>
              <button
                type="submit"
                disabled={busy || reason === null}
                className="flex h-11 flex-1 items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-50"
              >
                {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
                {t("Göndər")}
              </button>
            </div>
          </form>
        )}
      </dialog>
    </>
  );
}
