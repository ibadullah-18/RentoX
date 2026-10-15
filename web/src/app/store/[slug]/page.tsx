import { formatNumber } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Globe, MapPin, Phone, Store } from "lucide-react";
import { FollowButton } from "@/components/FollowButton";
import { ReportDialog } from "@/components/ReportDialog";
import { ListingGrid } from "@/components/ListingGrid";
import { Pagination } from "@/components/Pagination";
import { getStore, getStoreListings } from "@/lib/api";
import { getFollowStatus, getMyStore } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata({ params }: PageProps<"/store/[slug]">): Promise<Metadata> {
  const t = await getT();
  const { slug } = await params;
  const store = await getStore(slug);
  if (!store) return { title: t("Mağaza tapılmadı"), robots: { index: false } };

  const description = store.description.slice(0, 200);
  return {
    title: store.name,
    description,
    alternates: { canonical: `/store/${store.slug}` },
    openGraph: {
      title: store.name,
      description,
      images: store.logoImageUrl ? [{ url: store.logoImageUrl }] : undefined,
    },
  };
}

const socials: [keyof Pick<NonNullable<Awaited<ReturnType<typeof getStore>>>, "websiteUrl" | "instagramUrl" | "tiktokUrl" | "facebookUrl">, string][] = [
  ["websiteUrl", "Sayt"],
  ["instagramUrl", "Instagram"],
  ["tiktokUrl", "TikTok"],
  ["facebookUrl", "Facebook"],
];

export default async function StorePage({ params, searchParams }: PageProps<"/store/[slug]">) {
  const t = await getT();
  const locale = await getLocale();
  const { slug } = await params;
  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);

  const [store, session] = await Promise.all([getStore(slug), getSession()]);
  if (!store) notFound();

  const [listings, follow, mine] = await Promise.all([
    getStoreListings(slug, page),
    session ? getFollowStatus(store.id) : Promise.resolve(null),
    session ? getMyStore() : Promise.resolve(null),
  ]);
  // Nobody follows their own store.
  const isOwner = mine?.ok === true && mine.data.id === store.id;
  const status = follow?.ok ? follow.data : { isFollowing: false, followerCount: 0 };

  return (
    <div className="space-y-8">
      <section className="overflow-hidden rounded-2xl border border-line bg-surface">
        <div className="relative aspect-[3/1] bg-primary-soft sm:aspect-[4/1]">
          {store.coverImageUrl && (
            <Image src={store.coverImageUrl} alt="" fill unoptimized priority sizes="1240px" className="object-cover" />
          )}
        </div>
        <div className="flex flex-col gap-5 p-5 md:flex-row md:items-start md:p-6">
          <span className="relative -mt-14 flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-surface bg-primary-soft text-primary md:-mt-16 md:size-28">
            {store.logoImageUrl ? (
              <Image src={store.logoImageUrl} alt={store.name} fill unoptimized sizes="112px" className="object-cover" />
            ) : (
              <Store className="size-10" aria-hidden />
            )}
          </span>

          <div className="min-w-0 flex-1 space-y-3">
            <h1 className="text-2xl font-extrabold md:text-3xl">{store.name}</h1>
            <p className="whitespace-pre-line leading-relaxed text-ink-2">{store.description}</p>

            <ul className="flex flex-wrap gap-x-5 gap-y-2 text-sm">
              {store.phoneNumber && (
                <li>
                  <a href={`tel:${store.phoneNumber}`} className="flex items-center gap-1.5 font-semibold hover:text-primary">
                    <Phone className="size-4" aria-hidden />
                    {store.phoneNumber}
                  </a>
                </li>
              )}
              {store.address && (
                <li className="flex items-center gap-1.5 text-ink-2">
                  <MapPin className="size-4" aria-hidden />
                  {store.address}
                </li>
              )}
              {socials.map(([key, label]) =>
                store[key] ? (
                  <li key={key}>
                    <a
                      href={store[key]!}
                      target="_blank"
                      rel="noopener noreferrer nofollow"
                      className="flex items-center gap-1.5 font-semibold text-primary hover:underline"
                    >
                      <Globe className="size-4" aria-hidden />
                      {label}
                    </a>
                  </li>
                ) : null,
              )}
            </ul>

            {isOwner ? (
              <Link
                href="/magaza"
                className="inline-flex h-11 items-center rounded-xl border border-line px-5 font-bold hover:border-primary"
              >
                {t("Mağazanı idarə et")}</Link>
            ) : (
              <FollowButton
                storeId={store.id}
                slug={store.slug}
                following={status.isFollowing}
                followers={status.followerCount}
              />
            )}
          </div>
        </div>
      </section>

      {!isOwner && <ReportDialog kind="store" targetId={store.id} />}

      <section>
        <h2 className="mb-4 text-xl font-extrabold">
          {t("Elanlar")}{" "}
          <span className="text-base font-semibold text-ink-2">({formatNumber(listings.totalCount, locale)})</span>
        </h2>
        {listings.items.length === 0 ? (
          <p className="rounded-xl border border-line bg-surface py-12 text-center text-ink-2">
            {t("Mağazanın hələ aktiv elanı yoxdur.")}</p>
        ) : (
          <>
            <ListingGrid items={listings.items} />
            <Pagination
              page={listings.page}
              totalPages={listings.totalPages}
              hrefFor={(p) => (p === 1 ? `/store/${slug}` : `/store/${slug}?page=${p}`)}
            />
          </>
        )}
      </section>
    </div>
  );
}
