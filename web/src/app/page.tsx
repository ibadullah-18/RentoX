import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { Crown } from "lucide-react";
import { CategoryTiles } from "@/components/CategoryTiles";
import { ListingGrid } from "@/components/ListingGrid";
import { StoreTile } from "@/components/StoreTile";
import { getCategories, getListings, getStores } from "@/lib/api";

export default async function HomePage() {
  const t = await getT();
  const [categories, listings, stores] = await Promise.all([
    getCategories(),
    getListings({ pageSize: 30 }),
    getStores(undefined, 1, 6),
  ]);

  const vip = listings.items.filter((l) => l.isVip).slice(0, 5);
  const rest = listings.items.filter((l) => !vip.includes(l));

  return (
    <div className="space-y-10">
      <section aria-label={t("Kateqoriyalar")}>
        <CategoryTiles categories={categories} showAll />
      </section>

      {vip.length > 0 && (
        <section>
          <h2 className="mb-4 flex items-center gap-2 text-xl font-extrabold md:text-2xl">
            <Crown className="size-6 fill-warning text-warning" aria-hidden />
            {t("Premium elanlar")}
          </h2>
          <ListingGrid items={vip} priorityCount={5} />
        </section>
      )}

      {stores.items.length > 0 && (
        <section>
          <div className="mb-4 flex items-baseline justify-between">
            <h2 className="text-xl font-extrabold md:text-2xl">{t("Mağazalar")}</h2>
            <Link href="/magazalar" className="text-sm font-bold text-primary hover:underline">
              {t("Hamısına bax")}
            </Link>
          </div>
          <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {stores.items.map((s) => (
              <li key={s.storeId}>
                <StoreTile store={s} />
              </li>
            ))}
          </ul>
        </section>
      )}

      <section>
        <div className="mb-4 flex items-baseline justify-between">
          <h2 className="text-xl font-extrabold md:text-2xl">{t("Son elanlar")}</h2>
          <Link href="/search" className="text-sm font-bold text-primary hover:underline">
            {t("Hamısına bax")}
          </Link>
        </div>
        <ListingGrid items={rest} priorityCount={vip.length === 0 ? 5 : 0} />
        {listings.totalPages > 1 && (
          <div className="mt-8 flex justify-center">
            <Link
              href="/search?page=2"
              className="flex h-11 items-center rounded-xl border border-line bg-surface px-8 font-bold hover:border-primary"
            >
              {t("Daha çox göstər")}
            </Link>
          </div>
        )}
      </section>
    </div>
  );
}
