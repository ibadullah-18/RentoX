import { getT } from "@/lib/i18n/server";
import { headers } from "next/headers";
import Link from "next/link";
import { Crown } from "lucide-react";
import { CategoryTiles } from "@/components/CategoryTiles";
import { ListingGrid } from "@/components/ListingGrid";
import { StoreTile } from "@/components/StoreTile";
import { HomeBanners } from "@/components/HomeBanners";
import { getBanners, getCategories, getListings, getVipStores } from "@/lib/api";
import type { ListingSummary } from "@/lib/types";

/** A page is whole rounds of the feed: 12 listings (4+4+4) twice on a phone, 40 (10+15+15) on a computer. */
const PAGE_SIZE_PHONE = 24;
const PAGE_SIZE_WIDE = 40;
const MAX_PAGES = 10;

/**
 * The home page: banners, the VIP stores, the categories, then one feed of listings. The feed comes in the order
 * the API makes it: on a computer 2 rows of VIP, 3 moved up, 3 ordinary (rows of five), on a phone 4, 4, 4; and again. "Show more" adds the next page.
 */
export default async function HomePage({ searchParams }: PageProps<"/">) {
  const t = await getT();
  const raw = (await searchParams).sehife;
  const wanted = Math.min(Math.max(Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1, 1), MAX_PAGES);

  const agent = (await headers()).get("user-agent") ?? "";
  const wide = !/Mobi|Android|iPhone|iPad/i.test(agent);
  const pageSize = wide ? PAGE_SIZE_WIDE : PAGE_SIZE_PHONE;

  const [banners, categories, stores, pages] = await Promise.all([
    getBanners(),
    getCategories(),
    getVipStores(6),
    Promise.all(Array.from({ length: wanted }, (_, i) => getListings({ page: i + 1, pageSize, feed: wide ? "wide" : undefined }))),
  ]);

  const items: ListingSummary[] = pages.flatMap((p) => p.items);
  const totalPages = pages[0]?.totalPages ?? 0;

  return (
    <div className="space-y-10">
      <HomeBanners banners={banners} />

      {stores.length > 0 && (
        <section>
          <div className="mb-4 flex items-baseline justify-between">
            <h2 className="flex items-center gap-2 text-xl font-extrabold md:text-2xl">
              <Crown className="size-5 fill-warning text-warning" aria-hidden />
              {t("Mağazalar")}
            </h2>
            <Link href="/magazalar" className="text-sm font-bold text-primary hover:underline">
              {t("Hamısına bax")}
            </Link>
          </div>
          <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {stores.map((s) => (
              <li key={s.storeId}>
                <StoreTile store={s} vip />
              </li>
            ))}
          </ul>
        </section>
      )}

      <section aria-label={t("Kateqoriyalar")}>
        <CategoryTiles categories={categories} showAll />
      </section>

      <section>
        <div className="mb-4 flex items-baseline justify-between">
          <h2 className="text-xl font-extrabold md:text-2xl">{t("Son elanlar")}</h2>
          <Link href="/search" className="text-sm font-bold text-primary hover:underline">
            {t("Hamısına bax")}
          </Link>
        </div>
        <ListingGrid items={items} priorityCount={5} />
        {wanted < totalPages && wanted < MAX_PAGES && (
          <div className="mt-8 flex justify-center">
            <Link
              href={`/?sehife=${wanted + 1}`}
              scroll={false}
              className="flex h-11 items-center rounded-xl border border-line bg-surface px-8 font-bold hover:border-primary"
            >
              {t("Daha çox göstər")}
            </Link>
          </div>
        )}
        {wanted >= MAX_PAGES && wanted < totalPages && (
          <div className="mt-8 flex justify-center">
            <Link href="/search?page=2" className="flex h-11 items-center rounded-xl border border-line bg-surface px-8 font-bold hover:border-primary">
              {t("Daha çox göstər")}
            </Link>
          </div>
        )}
      </section>
    </div>
  );
}
