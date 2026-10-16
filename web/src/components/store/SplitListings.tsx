import { Crown } from "lucide-react";
import { getT } from "@/lib/i18n/server";
import type { ListingSummary } from "@/lib/types";
import { ListingCard } from "../ListingCard";
import { ExpandableGrid } from "./ExpandableGrid";

const VIP_FIRST = 4;
const REGULAR_FIRST = 6;

/**
 * The store's listings in two parts: the VIP ones first (4 at the start) and the ordinary ones (6 at the start).
 * "Show all" opens the rest of each part.
 */
export async function SplitListings({ items }: { items: ListingSummary[] }) {
  const t = await getT();
  const vip = items.filter((l) => l.isVip);
  const regular = items.filter((l) => !l.isVip);
  const labels = { showAll: t("Hamısını göstər"), showLess: t("Daha az göstər") };

  return (
    <div className="space-y-8">
      {vip.length > 0 && (
        <section className="space-y-4">
          <h2 className="flex items-center gap-2 text-xl font-extrabold">
            <Crown className="size-5 text-warning" aria-hidden />
            {t("VIP elanlar")} <span className="text-base font-semibold text-ink-2">({vip.length})</span>
          </h2>
          <ExpandableGrid first={VIP_FIRST} {...labels}>
            {vip.map((l) => (
              <li key={l.id}>
                <ListingCard listing={l} />
              </li>
            ))}
          </ExpandableGrid>
        </section>
      )}

      {regular.length > 0 && (
        <section className="space-y-4">
          <h2 className="text-xl font-extrabold">
            {t("Elanlar")} <span className="text-base font-semibold text-ink-2">({regular.length})</span>
          </h2>
          <ExpandableGrid first={REGULAR_FIRST} {...labels}>
            {regular.map((l) => (
              <li key={l.id}>
                <ListingCard listing={l} />
              </li>
            ))}
          </ExpandableGrid>
        </section>
      )}
    </div>
  );
}
