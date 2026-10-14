"use client";

import { useT } from "@/lib/i18n/client";
import { usePathname, useSearchParams } from "next/navigation";
import { useState } from "react";
import { ArrowLeft, SlidersHorizontal } from "lucide-react";

/**
 * The filters sit open beside the results on a computer. On a phone a
 * button opens them as their own window over the page; applying them (a new
 * address) closes it.
 */
export function FilterShell({
  label,
  active,
  children,
}: {
  label: string;
  /** Some filter is on: the button says so. */
  active: boolean;
  children: React.ReactNode;
}) {
  const t = useT();
  const [open, setOpen] = useState(false);
  const where = `${usePathname()}?${useSearchParams()}`;
  const [seen, setSeen] = useState(where);
  if (seen !== where) {
    setSeen(where);
    setOpen(false);
  }

  return (
    <div>
      <button
        type="button"
        aria-expanded={open}
        onClick={() => setOpen(true)}
        className="flex h-10 items-center gap-2 rounded-lg border border-line bg-surface px-3.5 text-[14px] font-bold lg:hidden"
      >
        <SlidersHorizontal className="size-4" aria-hidden />
        {label}
        {active && <span className="size-2 rounded-full bg-accent" aria-label={t("Filtr aktivdir")} />}
      </button>

      <div
        role={open ? "dialog" : undefined}
        aria-modal={open ? true : undefined}
        aria-label={label}
        className={open ? "max-lg:fixed max-lg:inset-0 max-lg:z-[95] max-lg:flex max-lg:flex-col max-lg:bg-bg" : "max-lg:hidden"}
      >
        <div className="flex items-center gap-2 border-b border-line bg-surface px-3 py-2.5 lg:hidden">
          <button
            type="button"
            onClick={() => setOpen(false)}
            aria-label={t("Geri")}
            className="flex size-9 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft"
          >
            <ArrowLeft className="size-5" />
          </button>
          <span className="font-extrabold">{label}</span>
        </div>
        <div className="max-lg:min-h-0 max-lg:flex-1 max-lg:overflow-y-auto max-lg:px-4 max-lg:pt-4">{children}</div>
      </div>
    </div>
  );
}
