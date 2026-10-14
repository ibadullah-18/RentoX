"use client";

import { Printer } from "lucide-react";

/** Prints (or saves as PDF) the page as it is; the menu and buttons are left out. */
export function PrintButton() {
  return (
    <button
      type="button"
      onClick={() => window.print()}
      className="flex h-9 items-center gap-2 rounded-lg border border-line bg-surface px-3 text-sm font-semibold hover:border-primary"
    >
      <Printer className="size-4" aria-hidden />
      Çap et
    </button>
  );
}
