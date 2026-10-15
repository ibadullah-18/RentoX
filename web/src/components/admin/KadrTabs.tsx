"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { adminHref } from "@/lib/admin-path";

const tabs = [
  { sub: "/kadr", label: "Bu gün", exact: true },
  { sub: "/kadr/cedvel", label: "Cədvəl" },
  { sub: "/kadr/icazeler", label: "İcazələr", badge: "leaves" as const },
  { sub: "/kadr/muracietler", label: "Müraciətlər", badge: "requests" as const },
  { sub: "/kadr/isciler", label: "İşçilər, maaş", owner: true },
];

/** The tabs of the staff pages; the numbers are what waits for an answer. */
export function KadrTabs({ owner, counts }: { owner: boolean; counts: { leaves: number; requests: number } }) {
  const path = usePathname();
  return (
    <nav aria-label="Kadr" className="no-print strip">
      {tabs
        .filter((t) => !t.owner || owner)
        .map((t) => {
          const href = adminHref(t.sub);
          const active = t.exact ? path === href : path.startsWith(href);
          const count = t.badge ? counts[t.badge] : 0;
          return (
            <Link
              key={t.sub}
              href={href}
              aria-current={active ? "page" : undefined}
              className={`flex shrink-0 items-center gap-2 rounded-lg border px-3.5 py-1.5 text-sm font-semibold ${
                active ? "border-primary bg-primary text-white" : "border-line bg-surface hover:border-primary"
              }`}
            >
              {t.label}
              {count > 0 && (
                <span className={`flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-[11px] font-bold ${active ? "bg-white/25" : "bg-accent text-white"}`}>
                  {count}
                </span>
              )}
            </Link>
          );
        })}
    </nav>
  );
}
