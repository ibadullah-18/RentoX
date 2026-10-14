import { getLocale, getT } from "@/lib/i18n/server";
import Image from "next/image";
import Link from "next/link";
import { Crown, ImageIcon } from "lucide-react";
import { formatPrice } from "@/lib/format";
import type { ListingSummary } from "@/lib/types";
import { FavoriteButton } from "./FavoriteButton";

/**
 * One card, like in the app: the photo runs to the top and the sides with no
 * frame; the outline starts where the photo ends, around the text below it.
 */
export async function ListingCard({
  listing,
  priority = false,
}: {
  listing: ListingSummary;
  priority?: boolean;
}) {
  const t = await getT();
  const locale = await getLocale();
  const place = listing.location ?? listing.categoryName;

  return (
    <div className="relative">
    <Link
      href={`/listing/${listing.id}`}
      className="group block overflow-hidden rounded-lg bg-surface transition-transform duration-150 active:scale-[0.98]"
    >
      <div className="relative aspect-[4/3] overflow-hidden bg-primary-soft">
        {listing.coverImageUrl ? (
          <Image
            src={listing.coverImageUrl}
            alt={listing.title}
            fill
            unoptimized
            priority={priority}
            sizes="(min-width: 1024px) 20vw, (min-width: 640px) 33vw, 50vw"
            className="object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex size-full items-center justify-center text-primary">
            <ImageIcon className="size-9" aria-hidden />
          </div>
        )}
        {listing.isVip && (
          <span className="absolute left-2 top-2 flex items-center gap-1 rounded-md bg-surface/80 px-2 py-1 text-[11.5px] font-extrabold text-ink backdrop-blur">
            <Crown className="size-3.5 fill-warning text-warning" aria-hidden />
            {t("VIP")}
          </span>
        )}
      </div>
      <div className="rounded-b-lg border border-t-0 border-line px-2.5 py-2">
        <h3 className="truncate text-[14.5px] font-semibold">{listing.title}</h3>
        <p className="mt-0.5 truncate text-[14.5px] font-extrabold text-primary">
          {formatPrice(listing.price, listing.currency, listing.rentalPeriodUnit, t, locale)}
        </p>
        {place && (
          <p className="mt-0.5 truncate text-[12.5px] text-ink-2">{place}</p>
        )}
      </div>
    </Link>
    <FavoriteButton listingId={listing.id} className="absolute right-1 top-1" />
    </div>
  );
}
