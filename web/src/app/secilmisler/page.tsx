import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Heart } from "lucide-react";
import { ListingGrid } from "@/components/ListingGrid";
import { MoreButton } from "@/components/MoreButton";
import { getFavorites } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Seçilmişlər"), robots: { index: false } };
}

export default async function FavoritesPage({ searchParams }: PageProps<"/secilmisler">) {
  const t = await getT();
  if (!(await getSession())) redirect("/giris?next=/secilmisler");

  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const upTo = Math.min(page, 10);
  const results = await Promise.all(Array.from({ length: upTo }, (_, i) => getFavorites(i + 1)));
  if (results.some((r) => !r.ok && r.status === 401)) redirect("/giris?next=/secilmisler");

  const first = results[0];
  const data = first.ok ? { ...first.data, items: results.flatMap((r) => (r.ok ? r.data.items : [])), page: upTo } : null;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">{t("Seçilmişlər")}</h1>

      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-line bg-surface py-16 text-center">
          <Heart className="size-12 text-primary" aria-hidden />
          <h2 className="text-lg font-extrabold">{t("Hələ seçilmiş elan yoxdur")}</h2>
          <p className="max-w-sm text-ink-2">
            {t("Bəyəndiyiniz elanın ürəyinə basın, burada görünəcək.")}</p>
          <Link
            href="/"
            className="mt-2 flex h-11 items-center rounded-xl bg-primary px-8 font-bold text-white hover:bg-primary-press"
          >
            {t("Elanlara bax")}</Link>
        </div>
      ) : (
        <>
          <ListingGrid items={data.items} />
          <MoreButton href={data.page < data.totalPages && data.page < 10 ? `/secilmisler?page=${data.page + 1}` : null} />
        </>
      )}
    </div>
  );
}
