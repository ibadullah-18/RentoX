import { formatNumber } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Eye, MapPin, Package, Store, Users } from "lucide-react";
import { FollowButton } from "@/components/FollowButton";
import { MessageButton } from "@/components/MessageButton";
import { ReportDialog } from "@/components/ReportDialog";
import { SplitListings } from "@/components/store/SplitListings";
import { StoreFilter } from "@/components/store/StoreFilter";
import { StoreContact } from "@/components/store/StoreContact";
import { StoreHours } from "@/components/store/StoreHours";
import { StoreView } from "@/components/store/StoreTrack";
import { getStore, getStoreListings } from "@/lib/api";
import { getFollowStatus, getMyStore } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { defaultWhatsAppMessage } from "@/lib/store-contact";

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

/** A store as a visitor sees it: who it is, how to reach it, when it is open, and its listings (VIP first). */
export default async function StorePage({ params, searchParams }: PageProps<"/store/[slug]">) {
  const t = await getT();
  const locale = await getLocale();
  const { slug } = await params;
  const query = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v)?.trim() || undefined;
  const num = (v: string | string[] | undefined) => {
    const s = one(v)?.replace(",", ".");
    return s && Number.isFinite(Number(s)) && Number(s) >= 0 ? s : undefined;
  };
  const filter = { search: one(query.q)?.slice(0, 80), minPrice: num(query.min), maxPrice: num(query.max) };
  const filtered = Boolean(filter.search || filter.minPrice || filter.maxPrice);

  const [store, session] = await Promise.all([getStore(slug), getSession()]);
  if (!store) notFound();

  const [listings, follow, mine] = await Promise.all([
    getStoreListings(slug, 1, 50, filter),
    session ? getFollowStatus(store.id) : Promise.resolve(null),
    session ? getMyStore() : Promise.resolve(null),
  ]);
  // Nobody follows their own store.
  const isOwner = mine?.ok === true && mine.data.id === store.id;
  const status = follow?.ok ? follow.data : { isFollowing: false, followerCount: store.followerCount ?? 0 };
  // "Write to the store" starts a conversation about one of its listings: the newest.
  const aboutListing = listings.items[0];

  return (
    <div className="space-y-6">
      <StoreView slug={store.slug} />

      <section className="overflow-hidden rounded-2xl border border-line bg-surface">
        <div className="relative aspect-[3/1] bg-primary-soft sm:aspect-[4/1]">
          {store.coverImageUrl && (
            <Image src={store.coverImageUrl} alt="" fill unoptimized priority sizes="1240px" className="object-cover" />
          )}
        </div>
        <div className="space-y-5 p-5 md:p-6">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
            <span className="relative -mt-16 flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl border-4 border-surface bg-primary-soft text-primary md:-mt-20 md:size-28">
              {store.logoImageUrl ? (
                <Image src={store.logoImageUrl} alt={store.name} fill unoptimized sizes="112px" className="object-cover" />
              ) : (
                <Store className="size-10" aria-hidden />
              )}
            </span>
            <div className="min-w-0 flex-1">
              <h1 className="text-2xl font-extrabold leading-tight md:text-3xl">{store.name}</h1>
              <ul className="mt-1 flex flex-wrap gap-x-5 gap-y-1 text-sm text-ink-2">
                {/* A visitor sees the live count beside the follow button; the owner has no such button. */}
                {isOwner && (
                  <li className="flex items-center gap-1.5">
                    <Users className="size-4" aria-hidden />
                    <b className="text-ink">{formatNumber(status.followerCount, locale)}</b> {t("izləyici")}
                  </li>
                )}
                <li className="flex items-center gap-1.5">
                  <Package className="size-4" aria-hidden />
                  <b className="text-ink">{formatNumber(store.activeListingCount ?? listings.totalCount, locale)}</b> {t("elan")}
                </li>
                <li className="flex items-center gap-1.5">
                  <Eye className="size-4" aria-hidden />
                  <b className="text-ink">{formatNumber(store.totalViewCount ?? 0, locale)}</b> {t("baxış")}
                </li>
              </ul>
            </div>
          </div>

          <p className="whitespace-pre-line leading-relaxed text-ink-2">{store.description}</p>

          {store.address && (
            <p className="flex items-start gap-2 text-sm">
              <MapPin className="mt-0.5 size-4 shrink-0 text-primary" aria-hidden />
              {store.address}
            </p>
          )}

          <div className="flex flex-wrap items-center gap-2.5">
            {isOwner ? (
              <Link href="/magaza" className="flex h-11 items-center rounded-xl border border-line px-5 font-bold hover:border-primary">
                {t("Mağazanı idarə et")}
              </Link>
            ) : (
              <>
                <FollowButton storeId={store.id} slug={store.slug} following={status.isFollowing} followers={status.followerCount} />
                {aboutListing && (
                  <MessageButton
                    listingId={aboutListing.id}
                    title={aboutListing.title}
                    label={t("Mağazaya yaz")}
                    storeSlug={store.slug}
                    defaultText={t(defaultWhatsAppMessage)}
                    outline
                  />
                )}
              </>
            )}
          </div>

          <StoreContact
            slug={store.slug}
            phone={store.phoneNumber}
            whatsAppNumber={store.whatsAppNumber}
            whatsAppMessage={store.whatsAppMessage}
            instagramUrl={store.instagramUrl}
            tiktokUrl={store.tiktokUrl}
            facebookUrl={store.facebookUrl}
            youtubeUrl={store.youtubeUrl}
            websiteUrl={store.websiteUrl}
          />
        </div>
      </section>

      <StoreHours hours={store.workingHours} />

      <StoreFilter q={filter.search} min={filter.minPrice} max={filter.maxPrice} active={filtered} />

      {listings.items.length === 0 ? (
        <p className="rounded-xl border border-line bg-surface py-12 text-center text-ink-2">{filtered ? t("Heç nə tapılmadı") : t("Mağazanın hələ aktiv elanı yoxdur.")}</p>
      ) : (
        <SplitListings items={listings.items} />
      )}

      {!isOwner && <ReportDialog kind="store" targetId={store.id} />}
    </div>
  );
}
