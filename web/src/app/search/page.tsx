import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { ChevronRight, SearchX } from "lucide-react";
import { CategoryTiles } from "@/components/CategoryTiles";
import { FilterForm } from "@/components/FilterForm";
import { ListingGrid } from "@/components/ListingGrid";
import { Pagination } from "@/components/Pagination";
import { categoryChain, getCategories, getCities, getFilterFields, getListings } from "@/lib/api";
import { redirect } from "next/navigation";
import { fieldFiltersJson, isUntidy, parseSearch, toQuery } from "@/lib/search-params";

type Raw = Record<string, string | string[] | undefined>;

export async function generateMetadata({ searchParams }: PageProps<"/search">): Promise<Metadata> {
  const t = await getT();
  const p = parseSearch((await searchParams) as Raw);
  const categories = await getCategories();
  const chain = p.category ? categoryChain(categories, p.category) : [];
  const parts = [p.q, chain.at(-1)?.name].filter(Boolean);
  return {
    title: parts.length > 0 ? parts.join(" — ") : t("Son elanlar"),
    // Searches by free text, filters or later pages are not worth indexing;
    // a plain category page is.
    robots:
      p.q || p.page > 1 || toQuery({ ...p, category: undefined }).size > 0
        ? { index: false, follow: true }
        : undefined,
    alternates: p.category ? { canonical: `/search?category=${p.category}` } : undefined,
  };
}

/** The search results: the same look as the home page, with filters that know the category. */
export default async function SearchPage({ searchParams }: PageProps<"/search">) {
  const t = await getT();
  const raw = (await searchParams) as Raw;
  const p = parseSearch(raw);
  // The filter form sends every field, empty ones too: tidy the address.
  if (isUntidy(raw)) {
    const tidy = toQuery(p, { page: p.page > 1 ? String(p.page) : undefined });
    redirect(tidy.size > 0 ? `/search?${tidy}` : "/search");
  }

  const [categories, cities] = await Promise.all([getCategories(), getCities()]);
  const chain = p.category ? categoryChain(categories, p.category) : [];
  const picked = chain.at(-1);

  // The category decides which fields can be filtered, and which periods apply.
  const fields = p.category ? await getFilterFields(p.category) : [];
  const periods = picked?.rentalPeriods?.length ? picked.rentalPeriods : [1, 2, 3, 4, 5, 6];

  const result = await getListings({
    search: p.q,
    categoryId: p.category,
    minPrice: p.minPrice,
    maxPrice: p.maxPrice,
    sort: p.sort,
    seller: p.seller,
    cityId: p.city,
    rentalPeriod: p.period,
    fieldFilters: fieldFiltersJson(p, fields),
    page: p.page,
  });

  // Inside a category the strip shows what is in it, with a "back" card one
  // level up. The last category has nothing inside, so only "back" is left.
  const strip = !picked ? categories : picked.children;
  const parent = chain.at(-2);
  const withQ = (id?: string) => {
    const q = new URLSearchParams();
    if (id) q.set("category", id);
    if (p.q) q.set("q", p.q);
    return q.size > 0 ? `/search?${q}` : "/search";
  };
  const backHref = picked ? (parent ? withQ(parent.id) : "/kateqoriyalar") : undefined;

  return (
    <div className="grid gap-8 lg:grid-cols-[280px_1fr]">
      <aside className="lg:sticky lg:top-24 lg:max-h-[calc(100dvh-7rem)] lg:self-start lg:overflow-y-auto">
        <FilterForm parsed={p} cities={cities} fields={fields} periods={periods} />
      </aside>

      <div className="min-w-0 space-y-6">
        <nav aria-label={t("Kateqoriya zənciri")} className="flex flex-wrap items-center gap-1 text-sm text-ink-2">
          <Link href="/" className="hover:text-primary">
            {t("Ana səhifə")}
          </Link>
          {chain.map((c) => (
            <span key={c.id} className="flex items-center gap-1">
              <ChevronRight className="size-4" aria-hidden />
              <Link href={withQ(c.id)} className="hover:text-primary">
                {c.name}
              </Link>
            </span>
          ))}
        </nav>

        {p.q && <h1 className="text-xl font-extrabold">{`“${p.q}”`}</h1>}

        {(strip.length > 0 || backHref) && <CategoryTiles categories={strip} backHref={backHref} showAll={!picked} q={p.q} />}

        {result.items.length === 0 ? (
          <div className="flex flex-col items-center gap-3 rounded-xl border border-line bg-surface py-16 text-center">
            <SearchX className="size-12 text-primary" aria-hidden />
            <h2 className="text-lg font-extrabold">{t("Heç nə tapılmadı")}</h2>
            <p className="text-ink-2">{t("Başqa söz yazın və ya filtrləri dəyişin.")}</p>
          </div>
        ) : (
          <>
            <ListingGrid items={result.items} />
            <Pagination
              page={result.page}
              totalPages={result.totalPages}
              hrefFor={(n) => {
                const q = toQuery(p, { page: n > 1 ? String(n) : undefined });
                return q.size > 0 ? `/search?${q}` : "/search";
              }}
            />
          </>
        )}
      </div>
    </div>
  );
}
