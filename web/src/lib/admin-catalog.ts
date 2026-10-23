// What the admin catalogue screens read from the API (see AdminReadController).
export type Tr = { language: number; text: string };

export type AdminCategory = {
  id: string;
  parentId: string | null;
  slug: string;
  iconUrl: string | null;
  displayOrder: number;
  isActive: boolean;
  rentalPeriods: number[];
  translations: Tr[];
};

export type AdminFieldOption = {
  id: string;
  value: string;
  displayOrder: number;
  isActive: boolean;
  translations: Tr[];
};

export type AdminField = {
  id: string;
  categoryId: string;
  key: string;
  type: number;
  isRequired: boolean;
  isFilterable: boolean;
  isSearchable: boolean;
  allowCustomValue: boolean;
  appliesToDescendants: boolean;
  displayOrder: number;
  isActive: boolean;
  translations: Tr[];
  options: AdminFieldOption[];
};

export type AdminListingItem = {
  id: string;
  ownerId: string;
  ownerName: string;
  ownerPhone?: string | null;
  categoryId: string;
  title: string;
  price: number;
  currency: string;
  rentalPeriodUnit: number;
  status: number;
  coverImageId?: string | null;
  imageCount: number;
  viewCount: number;
  createdAtUtc: string;
  expiresAtUtc?: string | null;
};

/** The three languages the catalogue is written in. */
export const LANGS = [
  { id: 1, code: "az", label: "Azərbaycanca" },
  { id: 2, code: "ru", label: "Русский" },
  { id: 3, code: "en", label: "English" },
] as const;

export const fieldTypeLabel: Record<number, string> = {
  1: "Mətn",
  2: "Tam ədəd",
  3: "Kəsr ədəd",
  4: "Bəli / xeyr",
  5: "Seçim (biri)",
  6: "Seçim (bir neçəsi)",
  7: "Tarix",
};

export const isSelectType = (type: number) => type === 5 || type === 6;

export const periodLabel: Record<number, string> = {
  1: "Saatlıq",
  2: "Günlük",
  3: "Həftəlik",
  4: "Aylıq",
  5: "Tədbirlik",
  6: "Razılaşma ilə",
};

export const trText = (list: Tr[], language: number) => list.find((t) => t.language === language)?.text ?? "";

/** The name to show for a category or field: Azerbaijani, else anything there is. */
export const trName = (list: Tr[]) => trText(list, 1) || list[0]?.text || "—";

/** A link-safe word from a name: "Avtomobil markası" → "avtomobil-markasi". */
export function slugify(text: string, joiner = "-"): string {
  const map: Record<string, string> = { ə: "e", ı: "i", ö: "o", ü: "u", ğ: "g", ş: "s", ç: "c" };
  return text
    .toLowerCase()
    .replace(/[əıöüğşç]/g, (c) => map[c] ?? c)
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[^a-z0-9]+/g, joiner)
    .replace(new RegExp(`^${joiner}+|${joiner}+$`, "g"), "");
}

export type CategoryNode = AdminCategory & { children: CategoryNode[]; depth: number };

/** The flat list as a tree, in display order, each node knowing its depth. */
export function buildTree(list: AdminCategory[]): CategoryNode[] {
  const byParent = new Map<string | null, AdminCategory[]>();
  for (const c of list) byParent.set(c.parentId, [...(byParent.get(c.parentId) ?? []), c]);
  const walk = (parent: string | null, depth: number): CategoryNode[] =>
    (byParent.get(parent) ?? []).map((c) => ({ ...c, depth, children: walk(c.id, depth + 1) }));
  return walk(null, 0);
}

export function flatten(tree: CategoryNode[]): CategoryNode[] {
  return tree.flatMap((n) => [n, ...flatten(n.children)]);
}

/** The ids of a category and everything below it. */
export function descendantIds(tree: CategoryNode[], id: string): Set<string> {
  const out = new Set<string>();
  const visit = (node: CategoryNode, inside: boolean) => {
    const now = inside || node.id === id;
    if (now) out.add(node.id);
    node.children.forEach((c) => visit(c, now));
  };
  tree.forEach((n) => visit(n, false));
  return out;
}

export type AdminUserItem = {
  id: string;
  fullName: string;
  phone?: string | null;
  status: number;
  registeredAtUtc?: string | null;
  lastSeenAtUtc?: string | null;
  listingCount: number;
  activeListingCount: number;
  balance: number;
  currency: string;
};

export type AdminUserDetails = {
  user: AdminUserItem;
  bio?: string | null;
  preferredLanguage: number;
  statusReason?: string | null;
  /** What RentoX pays for this person, as bits: 1 listing fee, 2 VIP, 4 bump, 8 VIP store. */
  freeServices?: number;
  /** What others reported about this person (absent on an older API). */
  reports?: {
    total: number;
    open: number;
    upheld: number;
    dismissed: number;
    recent: {
      kind: "listing" | "store" | "conversation";
      id: string;
      reason: number;
      status: number;
      createdAtUtc: string;
      note?: string | null;
    }[];
  } | null;
  recentTransactions: {
    id: string;
    direction: number;
    type: number;
    amount: number;
    balanceAfter: number;
    reason: string;
    occurredAtUtc: string;
  }[];
};

export const userStatusLabel: Record<number, string> = { 1: "Aktiv", 2: "Dayandırılıb", 3: "Bloklanıb", 4: "Silinib" };

export type AdminStoreItem = {
  id: string;
  ownerId: string;
  ownerName: string;
  ownerPhone?: string | null;
  name: string;
  slug: string;
  status: number;
  rejectionReason?: string | null;
  createdAtUtc: string;
  followers?: number;
  activeListings?: number;
  profileViews?: number;
};

export type StaffMember = {
  userId: string;
  fullName: string;
  phone?: string | null;
  role: string;
  sections: string[];
  /** Devices still signed in (absent on an older API). */
  activeSessions?: number;
  lastActiveAtUtc?: string | null;
};

/** The sections of the panel (the API's names) with what the person sees. */
export const PANEL_SECTIONS: { id: string; label: string }[] = [
  { id: "listings", label: "Elanlar" },
  { id: "stores", label: "Mağazalar" },
  { id: "reports", label: "Şikayətlər (elan, mağaza, yazışma)" },
  { id: "support", label: "Dəstək müraciətləri" },
  { id: "catalog", label: "Kateqoriyalar və sahələr" },
  { id: "users", label: "İstifadəçilər (blok daxil)" },
  { id: "finance", label: "Maliyyə rəqəmləri" },
  { id: "hr", label: "Kadr (işçilərin gəlişi, icazələri, müraciətləri)" },
  { id: "branding", label: "Brend və sayt ayarları (loqo, ad, elan xəbərdarlığı, hərəkət)" },
];
