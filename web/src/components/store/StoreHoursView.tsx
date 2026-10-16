"use client";

import { useState } from "react";
import { ChevronDown, Clock } from "lucide-react";
import { useT } from "@/lib/i18n/client";

export type HoursRow = { name: string; text: string | null; today: boolean };

/**
 * The working hours in one line (a clock, open/closed, today's hours). Tapping it opens the whole week. The
 * rows are made on the server (the times depend on the clock); this only opens and closes them.
 */
export function StoreHoursView({ open, todayText, rows }: { open: boolean | null; todayText: string | null; rows: HoursRow[] }) {
  const t = useT();
  const [shown, setShown] = useState(false);

  return (
    <section className="rounded-2xl border border-line bg-surface">
      <button
        type="button"
        onClick={() => setShown((v) => !v)}
        aria-expanded={shown}
        className="flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-left hover:bg-bg"
      >
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary">
          <Clock className="size-5" aria-hidden />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-sm font-extrabold">{t("İş saatları")}</span>
          <span className="block truncate text-xs text-ink-2">{todayText ?? t("Bu gün bağlıdır")}</span>
        </span>
        {open !== null && (
          <span className={`shrink-0 rounded-md px-2.5 py-1 text-xs font-bold ${open ? "bg-success-soft text-success" : "bg-line text-ink-2"}`}>
            {open ? t("İndi açıqdır") : t("İndi bağlıdır")}
          </span>
        )}
        <ChevronDown className={`size-5 shrink-0 text-ink-2 transition-transform ${shown ? "rotate-180" : ""}`} aria-hidden />
      </button>

      {shown && (
        <ul className="space-y-1 border-t border-line p-3 text-sm">
          {rows.map((row) => (
            <li key={row.name} className={`flex justify-between gap-3 rounded-lg px-2.5 py-1.5 ${row.today ? "bg-primary-soft font-bold text-primary" : ""}`}>
              <span>{t(row.name)}</span>
              <span className={row.text ? "" : "text-ink-2"}>{row.text ?? t("Bağlıdır")}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
