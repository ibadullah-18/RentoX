"use client";

import { useRouter } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { useT } from "@/lib/i18n/client";

/**
 * "Back" as in any shop: returns to the page the person came from. When
 * there is none (a link opened in a new tab) it goes to `fallback`.
 */
export function BackButton({
  fallback,
  className = "flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-primary hover:opacity-80",
}: {
  fallback: string;
  className?: string;
}) {
  const t = useT();
  const router = useRouter();

  return (
    <button
      type="button"
      aria-label={t("Geri")}
      onClick={() => {
        if (window.history.length > 1) router.back();
        else router.push(fallback);
      }}
      className={className}
    >
      <ChevronLeft className="size-5" aria-hidden />
    </button>
  );
}
