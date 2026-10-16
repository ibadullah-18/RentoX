import { getT } from "@/lib/i18n/server";
import Image from "next/image";
import Link from "next/link";
import { Crown, Store } from "lucide-react";
import type { StoreCard } from "@/lib/types";

/** A store in a list: round-cornered logo, name, how many listings it has. A paid (VIP) store carries a crown badge. */
export async function StoreTile({ store, vip = false }: { store: StoreCard; vip?: boolean }) {
  const t = await getT();
  return (
    <Link
      href={`/store/${store.slug}`}
      className={`flex items-center gap-3 rounded-xl border bg-surface p-3 hover:border-primary ${vip ? "border-warning/50" : "border-line"}`}
    >
      <span className="relative flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-primary-soft text-primary">
        {store.logoImageUrl ? (
          <Image src={store.logoImageUrl} alt="" fill unoptimized sizes="64px" className="object-cover" />
        ) : (
          <Store className="size-7" aria-hidden />
        )}
      </span>
      <span className="min-w-0">
        {vip && (
          <span className="mb-0.5 inline-flex items-center gap-1 rounded-md bg-warning-soft px-1.5 py-0.5 text-[11px] font-extrabold text-warning">
            <Crown className="size-3 fill-warning" aria-hidden />
            {t("VIP mağaza")}
          </span>
        )}
        <span className="block truncate font-extrabold">{store.name}</span>
        {store.description && <span className="line-clamp-1 text-sm text-ink-2">{store.description}</span>}
        {store.activeListingCount != null && (
          <span className="text-xs font-semibold text-primary">{t("{n} elan", { n: store.activeListingCount })}</span>
        )}
      </span>
    </Link>
  );
}
