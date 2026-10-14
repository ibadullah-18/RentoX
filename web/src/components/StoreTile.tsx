import { getT } from "@/lib/i18n/server";
import Image from "next/image";
import Link from "next/link";
import { Store } from "lucide-react";
import type { StoreCard } from "@/lib/types";

/** A store in a list: round-cornered logo, name, how many listings it has. */
export async function StoreTile({ store }: { store: StoreCard }) {
  const t = await getT();
  return (
    <Link
      href={`/store/${store.slug}`}
      className="flex items-center gap-3 rounded-xl border border-line bg-surface p-3 hover:border-primary"
    >
      <span className="relative flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-primary-soft text-primary">
        {store.logoImageUrl ? (
          <Image src={store.logoImageUrl} alt="" fill unoptimized sizes="64px" className="object-cover" />
        ) : (
          <Store className="size-7" aria-hidden />
        )}
      </span>
      <span className="min-w-0">
        <span className="block truncate font-extrabold">{store.name}</span>
        {store.description && <span className="line-clamp-1 text-sm text-ink-2">{store.description}</span>}
        {store.activeListingCount != null && (
          <span className="text-xs font-semibold text-primary">{t("{n} elan", { n: store.activeListingCount })}</span>
        )}
      </span>
    </Link>
  );
}
