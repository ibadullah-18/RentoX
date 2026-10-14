import {
  Bike,
  Building2,
  Car,
  Drill,
  Grid2x2,
  PartyPopper,
  Truck,
  type LucideIcon,
} from "lucide-react";

/** Same slug keywords as the mobile app (AppIcons.categoryIcon). */
export function categoryIcon(slug: string): LucideIcon {
  const s = slug.toLowerCase();
  const has = (keys: string[]) => keys.some((k) => s.includes(k));

  if (has(["moto", "bike", "scooter"])) return Bike;
  if (has(["car", "auto", "vehicle", "neqliyyat", "transport"])) return Car;
  if (has(["apartment", "home", "house", "real", "menzil", "villa"]))
    return Building2;
  if (has(["equip", "machin", "texnika", "construction"])) return Truck;
  if (has(["event", "tedbir", "tent"])) return PartyPopper;
  if (has(["tool", "alet"])) return Drill;
  return Grid2x2;
}

/** Soft tints that cycle through the categories, like the app's strip. */
export const tints = [
  "bg-primary-soft",
  "bg-success-soft",
  "bg-accent-soft",
  "bg-warning-soft",
];
