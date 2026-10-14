"use client";

import { useT } from "@/lib/i18n/client";
import { useEffect, useRef } from "react";
import { Loader2 } from "lucide-react";

/** Our own confirmation window (not the browser's), with a clear main button. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  destructive = false,
  busy = false,
  onConfirm,
  onClose,
}: {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  destructive?: boolean;
  busy?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const t = useT();
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => {
        if (e.target === ref.current && !busy) onClose();
      }}
      className="m-auto w-[min(92vw,420px)] rounded-2xl border border-line bg-surface p-0 text-ink backdrop:bg-black/60"
    >
      <div className="space-y-3 p-6">
        <h2 className="text-lg font-extrabold">{title}</h2>
        <p className="text-ink-2">{message}</p>
        <div className="flex gap-2 pt-2">
          <button
            type="button"
            disabled={busy}
            onClick={onClose}
            className="h-11 flex-1 rounded-xl border border-line font-bold hover:border-primary disabled:opacity-60"
          >
            {t("Ləğv et")}</button>
          <button
            type="button"
            disabled={busy}
            onClick={onConfirm}
            className={`flex h-11 flex-1 items-center justify-center gap-2 rounded-xl font-bold text-white disabled:opacity-60 ${
              destructive ? "bg-danger hover:opacity-90" : "bg-primary hover:bg-primary-press"
            }`}
          >
            {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  );
}
