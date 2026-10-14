import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { LayoutGrid } from "lucide-react";
import { tints } from "@/lib/category-icon";
import { CategoryIcon } from "./CategoryIcon";
import type { Category } from "@/lib/types";
import { BackButton } from "./BackButton";

/**
 * The categories as slim chips (icon + name). Phone: one swipeable row;
 * computer: they wrap. At the top level the first chip is "all categories"
 * (a page of squares); inside a category a small back button leads in front.
 */
export async function CategoryTiles({
  categories,
  backHref,
  showAll = false,
  selectedId,
  q,
}: {
  categories: Category[];
  /** Kept when a category is picked (a search inside the category). */
  q?: string;
  /** Where "back" goes when there is no page to return to. */
  backHref?: string;
  /** Put "all categories" first. */
  showAll?: boolean;
  selectedId?: string;
}) {
  const t = await getT();
  return (
    <div className="strip -mx-4 px-4 lg:mx-0 lg:flex-wrap lg:overflow-visible lg:px-0">
      {backHref && <BackButton fallback={backHref} />}
      {showAll && (
        <Link
          href="/kateqoriyalar"
          className="flex h-9 shrink-0 snap-start items-center gap-2 rounded-lg bg-primary px-3 text-[13px] font-bold text-white hover:bg-primary-press"
        >
          <LayoutGrid className="size-[18px]" aria-hidden />
          {t("Bütün kateqoriyalar")}
        </Link>
      )}
      {categories.map((c, i) => {
                const selected = c.id === selectedId;
        return (
          <Link
            key={c.id}
            href={`/search?category=${c.id}${q ? `&q=${encodeURIComponent(q)}` : ""}`}
            className={`flex h-9 max-w-[200px] shrink-0 snap-start items-center gap-2 rounded-lg border px-3 text-[13px] font-bold ${
              tints[i % tints.length]
            } ${selected ? "border-primary text-primary" : "border-transparent"}`}
          >
            <CategoryIcon slug={c.slug} iconUrl={c.iconUrl} className="size-[18px] shrink-0" />
            <span className="truncate">{c.name}</span>
          </Link>
        );
      })}
    </div>
  );
}
