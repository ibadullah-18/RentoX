"use client";

import { useT } from "@/lib/i18n/client";
import { useState } from "react";
import { Check, Share2 } from "lucide-react";

/** The phone's share sheet when there is one, otherwise "copy the link". */
/** `className` turns it into a small round icon button (on top of a photo). */
export function ShareButton({ title, className }: { title: string; className?: string }) {
  const t = useT();
  const [copied, setCopied] = useState(false);

  async function share() {
    const url = window.location.href;
    try {
      if (navigator.share) {
        await navigator.share({ title, url });
        return;
      }
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // The person closed the share sheet: nothing to do.
    }
  }

  return (
    <button
      type="button"
      onClick={share}
      aria-label={t("Paylaş")}
      title={copied ? t("Link kopyalandı") : t("Paylaş")}
      className={
        className ??
        "flex h-10 items-center gap-2 rounded-lg border border-line bg-surface px-4 text-sm font-semibold hover:border-primary"
      }
    >
      {copied ? <Check className="size-[18px]" aria-hidden /> : <Share2 className="size-[18px]" aria-hidden />}
      {!className && (copied ? t("Link kopyalandı") : t("Paylaş"))}
    </button>
  );
}
