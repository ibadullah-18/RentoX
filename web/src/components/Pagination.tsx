import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";

/** Page links (real URLs, so search engines can follow them). */
export async function Pagination({
  page,
  totalPages,
  hrefFor,
}: {
  page: number;
  totalPages: number;
  hrefFor: (page: number) => string;
}) {
  const t = await getT();
  if (totalPages <= 1) return null;

  const pages = new Set<number>([1, totalPages]);
  for (let p = page - 2; p <= page + 2; p++) {
    if (p > 1 && p < totalPages) pages.add(p);
  }
  const sorted = [...pages].sort((a, b) => a - b);

  const cell =
    "flex size-10 items-center justify-center rounded-lg text-sm font-semibold";

  return (
    <nav aria-label={t("Səhifələr")} className="mt-8 flex flex-wrap justify-center gap-1.5">
      {page > 1 && (
        <Link href={hrefFor(page - 1)} aria-label={t("Əvvəlki")} className={`${cell} border border-line bg-surface`}>
          <ChevronLeft className="size-5" />
        </Link>
      )}
      {sorted.map((p, i) => (
        <span key={p} className="contents">
          {i > 0 && p - sorted[i - 1] > 1 && (
            <span className={`${cell} text-muted`}>…</span>
          )}
          <Link
            href={hrefFor(p)}
            aria-current={p === page ? "page" : undefined}
            className={`${cell} ${
              p === page
                ? "bg-primary text-white"
                : "border border-line bg-surface hover:border-primary"
            }`}
          >
            {p}
          </Link>
        </span>
      ))}
      {page < totalPages && (
        <Link href={hrefFor(page + 1)} aria-label={t("Növbəti")} className={`${cell} border border-line bg-surface`}>
          <ChevronRight className="size-5" />
        </Link>
      )}
    </nav>
  );
}
