import type { ListingSummary } from "@/lib/types";
import { ListingCard } from "./ListingCard";

/** 2 columns on a phone, up to 5 on a wide screen. */
export function ListingGrid({
  items,
  priorityCount = 0,
}: {
  items: ListingSummary[];
  priorityCount?: number;
}) {
  return (
    <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:gap-4 lg:grid-cols-4 xl:grid-cols-5">
      {items.map((item, i) => (
        // A VIP listing comes round again in a long feed, so the place is part of the key.
        <li key={`${item.id}-${i}`} className="rx-reveal">
          <ListingCard listing={item} priority={i < priorityCount} />
        </li>
      ))}
    </ul>
  );
}
