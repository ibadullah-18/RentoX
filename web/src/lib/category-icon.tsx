import { createElement, type ReactElement } from "react";
import {
  Armchair,
  Baby,
  Bike,
  Book,
  Building2,
  Bus,
  Camera,
  Car,
  Dumbbell,
  Drill,
  Gamepad2,
  Grid2x2,
  House,
  KeyRound,
  Laptop,
  Luggage,
  Music,
  PartyPopper,
  PawPrint,
  Plane,
  Shirt,
  Ship,
  Smartphone,
  Trees,
  Truck,
  Tv,
  Wrench,
  type LucideIcon,
} from "lucide-react";

/**
 * The icons an admin can give a category. The key is what the catalogue
 * stores (the category's "iconUrl"); the mobile app knows the same keys
 * (AppIcons.byKey), so the three always agree.
 */
export const CATEGORY_ICONS: { key: string; label: string; Icon: LucideIcon }[] = [
  { key: "car", label: "Avtomobil", Icon: Car },
  { key: "bike", label: "Motosiklet / velosiped", Icon: Bike },
  { key: "bus", label: "Avtobus / mikroavtobus", Icon: Bus },
  { key: "truck", label: "Yük / texnika", Icon: Truck },
  { key: "boat", label: "Gəmi / qayıq", Icon: Ship },
  { key: "plane", label: "Təyyarə / səyahət", Icon: Plane },
  { key: "building", label: "Bina / ofis", Icon: Building2 },
  { key: "home", label: "Ev / mənzil", Icon: House },
  { key: "key", label: "Açar / kirayə", Icon: KeyRound },
  { key: "furniture", label: "Mebel", Icon: Armchair },
  { key: "garden", label: "Bağ / həyət", Icon: Trees },
  { key: "tools", label: "Alət", Icon: Drill },
  { key: "repair", label: "Təmir / servis", Icon: Wrench },
  { key: "event", label: "Tədbir", Icon: PartyPopper },
  { key: "camera", label: "Foto / video", Icon: Camera },
  { key: "laptop", label: "Kompüter", Icon: Laptop },
  { key: "phone", label: "Telefon", Icon: Smartphone },
  { key: "tv", label: "Televizor / ekran", Icon: Tv },
  { key: "music", label: "Musiqi / səs", Icon: Music },
  { key: "games", label: "Oyun", Icon: Gamepad2 },
  { key: "sport", label: "İdman", Icon: Dumbbell },
  { key: "clothes", label: "Geyim", Icon: Shirt },
  { key: "baby", label: "Uşaq", Icon: Baby },
  { key: "pets", label: "Heyvan", Icon: PawPrint },
  { key: "books", label: "Kitab", Icon: Book },
  { key: "luggage", label: "Baqaj / çamadan", Icon: Luggage },
];

const byKey = new Map(CATEGORY_ICONS.map((i) => [i.key, i.Icon]));

/**
 * The icon of a category: the one the admin chose, else a guess from the
 * slug (the same keywords as the mobile app's AppIcons.categoryIcon).
 */
export function categoryIcon(slug: string, iconKey?: string | null): LucideIcon {
  const chosen = iconKey ? byKey.get(iconKey.trim()) : undefined;
  if (chosen) return chosen;

  const s = slug.toLowerCase();
  const has = (keys: string[]) => keys.some((k) => s.includes(k));

  if (has(["moto", "bike", "scooter"])) return Bike;
  if (has(["car", "auto", "vehicle", "neqliyyat", "transport"])) return Car;
  if (has(["apartment", "home", "house", "real", "menzil", "villa"])) return Building2;
  if (has(["equip", "machin", "texnika", "construction"])) return Truck;
  if (has(["event", "tedbir", "tent"])) return PartyPopper;
  if (has(["tool", "alet"])) return Drill;
  return Grid2x2;
}

/** The icon as an element (so a component can draw it without making a component of its own). */
export function categoryGlyph(slug: string, iconKey: string | null | undefined, className: string): ReactElement {
  return createElement(categoryIcon(slug, iconKey), { className, "aria-hidden": true });
}

/** Soft tints that cycle through the categories, like the app's strip. */
export const tints = [
  "bg-primary-soft",
  "bg-success-soft",
  "bg-accent-soft",
  "bg-warning-soft",
];
