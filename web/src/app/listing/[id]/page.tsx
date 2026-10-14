import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronRight, Eye, MapPin, Store, User } from "lucide-react";
import { ExpandableText } from "@/components/ExpandableText";
import { Gallery } from "@/components/Gallery";
import { ListingGrid } from "@/components/ListingGrid";
import { MessageButton } from "@/components/MessageButton";
import { PhoneButton } from "@/components/PhoneButton";
import { categoryChain, getCategories, getListing, getSimilar } from "@/lib/api";
import { fieldValue, formatNumber, formatPrice, formatPublished, siteUrl } from "@/lib/format";
import { getSession } from "@/lib/session";

export async function generateMetadata({
  params,
}: PageProps<"/listing/[id]">): Promise<Metadata> {
  const t = await getT();
  const locale = await getLocale();
  const { id } = await params;
  const listing = await getListing(id);
  if (!listing) return { title: t("Səhifə tapılmadı"), robots: { index: false } };

  const price = formatPrice(listing.price, listing.currency, listing.rentalPeriodUnit, t, locale);
  const place = [listing.cityName, listing.districtName].filter(Boolean).join(", ");
  const description = [price, place, listing.description]
    .filter(Boolean)
    .join(" · ")
    .slice(0, 200);
  const cover = [...listing.images].sort((a, b) => a.displayOrder - b.displayOrder)[0];

  return {
    title: listing.title,
    description,
    alternates: { canonical: `/listing/${listing.id}` },
    openGraph: {
      title: listing.title,
      description,
      type: "website",
      url: `/listing/${listing.id}`,
      images: cover ? [{ url: cover.url }] : undefined,
    },
  };
}

export default async function ListingPage({ params }: PageProps<"/listing/[id]">) {
  const t = await getT();
  const { id } = await params;
  const [listing, categories, similar, session] = await Promise.all([
    getListing(id),
    getCategories(),
    getSimilar(id),
    getSession(),
  ]);
  if (!listing) notFound();

  const images = [...listing.images].sort((a, b) => a.displayOrder - b.displayOrder);
  const chain = listing.categoryId ? categoryChain(categories, listing.categoryId) : [];
  const place = [listing.cityName, listing.districtName].filter(Boolean).join(", ");
  const locale = await getLocale();
  const published = formatPublished(listing.publishedAtUtc, t, locale);
  const price = formatPrice(listing.price, listing.currency, listing.rentalPeriodUnit, t, locale);
  const store = listing.owner.store?.slug ? listing.owner.store : null;

  const rows: [string, string][] = [
    ...(listing.categoryName ? ([[t("Kateqoriya"), listing.categoryName]] as [string, string][]) : []),
    ...(place ? ([[t("Yer"), place]] as [string, string][]) : []),
    ...listing.fields.flatMap((f) => {
      const value = fieldValue(f, t, locale);
      return value ? ([[f.label ?? f.key ?? "", value]] as [string, string][]) : [];
    }),
  ];

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: listing.title,
    description: listing.description,
    image: images.map((i) => `${siteUrl()}${i.url}`),
    offers: {
      "@type": "Offer",
      price: listing.price,
      priceCurrency: listing.currency,
      availability: "https://schema.org/InStock",
      url: `${siteUrl()}/listing/${listing.id}`,
    },
  };

  return (
    <article className="space-y-8">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }}
      />

      <nav aria-label={t("Kateqoriya zənciri")} className="flex flex-wrap items-center gap-1 text-sm text-ink-2 max-md:hidden">
        <Link href="/" className="hover:text-primary">
          {t("Ana səhifə")}
        </Link>
        {chain.map((c) => (
          <span key={c.id} className="flex items-center gap-1">
            <ChevronRight className="size-4" aria-hidden />
            <Link href={`/search?category=${c.id}`} className="hover:text-primary">
              {c.name}
            </Link>
          </span>
        ))}
      </nav>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px] lg:gap-8">
        <div className="min-w-0 space-y-6">
          <Gallery
            images={images}
            title={listing.title}
            listingId={listing.id}
            backHref={listing.categoryId ? `/search?category=${listing.categoryId}` : "/"}
          />

          <div className="lg:hidden">
            <Summary title={listing.title} price={price} place={place} published={published} views={formatNumber(listing.viewCount, locale)} />
          </div>

          {rows.length > 0 && (
            <section>
              <h2 className="mb-3 text-lg font-extrabold">{t("Məlumat")}</h2>
              <dl className="overflow-hidden rounded-xl border border-line bg-surface">
                {rows.map(([label, value], i) => (
                  <div
                    key={`${label}-${i}`}
                    className="flex justify-between gap-4 border-b border-line px-4 py-3 last:border-b-0"
                  >
                    <dt className="text-ink-2">{label}</dt>
                    <dd className="text-right font-semibold">{value}</dd>
                  </div>
                ))}
              </dl>
            </section>
          )}

          {listing.description && (
            <section>
              <h2 className="mb-3 text-lg font-extrabold">{t("Təsvir")}</h2>
              <ExpandableText text={listing.description} />
            </section>
          )}
        </div>

        <aside className="space-y-4 lg:sticky lg:top-24 lg:self-start">
          <div className="hidden lg:block">
            <Summary title={listing.title} price={price} place={place} published={published} views={formatNumber(listing.viewCount, locale)} />
          </div>

          <div className="rounded-xl border border-line bg-surface p-3.5">
            <div className="flex items-center gap-3">
              <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary-soft text-primary">
                {store ? <Store className="size-5" aria-hidden /> : <User className="size-5" aria-hidden />}
              </span>
              <div className="min-w-0">
                <p className="text-xs text-ink-2">{store ? t("Mağaza") : t("Elan sahibi")}</p>
                {store ? (
                  <Link href={`/store/${store.slug}`} className="block truncate font-bold hover:text-primary">
                    {store.name}
                  </Link>
                ) : (
                  <p className="truncate font-bold">{listing.owner.fullName}</p>
                )}
              </div>
            </div>

            <div className="mt-3 grid grid-cols-2 gap-2 [&>*:only-child]:col-span-2">
              {listing.owner.phoneNumber && <PhoneButton phone={listing.owner.phoneNumber} />}
              {session?.userId === listing.owner.id ? (
                <Link
                  href={`/elanlarim/${listing.id}`}
                  className="flex h-10 w-full items-center justify-center rounded-lg border border-line px-4 text-[14px] font-bold hover:border-primary"
                >
                  {t("Elanı idarə et")}
                </Link>
              ) : (
                <MessageButton listingId={listing.id} title={listing.title} />
              )}
            </div>
          </div>
        </aside>
      </div>

      {similar.length > 0 && (
        <section>
          <h2 className="mb-4 text-xl font-extrabold">{t("Oxşar elanlar")}</h2>
          <ListingGrid items={similar.slice(0, 10)} />
        </section>
      )}
    </article>
  );
}

function Summary({
  title,
  price,
  place,
  published,
  views,
}: {
  title: string;
  price: string;
  place: string;
  published: string | null;
  views: string;
}) {
  return (
    <div className="space-y-2">
      <h1 className="text-2xl font-extrabold leading-tight md:text-[28px]">{title}</h1>
      <p className="text-2xl font-extrabold text-primary">{price}</p>
      <ul className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-ink-2">
        {place && (
          <li className="flex items-center gap-1">
            <MapPin className="size-4" aria-hidden />
            {place}
          </li>
        )}
        {published && <li>{published}</li>}
        <li className="flex items-center gap-1">
          <Eye className="size-4" aria-hidden />
          {views}
        </li>
      </ul>
    </div>
  );
}
