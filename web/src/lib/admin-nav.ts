// The admin menu: what exists in the backend today, in the order of a work day.
// `badge` names the number shown next to the item (work waiting).
export type AdminNavItem = {
  id: string;
  /** Path inside the panel ("" is the front page). */
  sub: string;
  label: string;
  icon: "dashboard" | "staff" | "users" | "catalog" | "listings" | "stores" | "reports" | "chats" | "support" | "finance" | "audit" | "notify" | "places" | "settings" | "banned" | "banners";
  badge?: "listings" | "stores" | "support";
  superOnly?: boolean;
  /** The section an Admin must have to see this item. */
  section?: string;
};

export type AdminNavGroup = { title: string; items: AdminNavItem[] };

export const adminNav: AdminNavGroup[] = [
  {
    title: "Ümumi",
    items: [{ id: "dashboard", sub: "", label: "İcmal", icon: "dashboard" }],
  },
  {
    title: "Moderasiya",
    items: [
      { id: "listings", sub: "/elanlar", label: "Elanlar", icon: "listings", badge: "listings", section: "listings" },
      { id: "stores", sub: "/magazalar", label: "Mağazalar", icon: "stores", badge: "stores", section: "stores" },
      { id: "banned", sub: "/yasaq-sozler", label: "Qadağan sözlər", icon: "banned", section: "listings" },
      { id: "settings", sub: "/ayarlar", label: "Ayarlar", icon: "settings", section: "listings" },
    ],
  },
  {
    title: "Şikayətlər",
    items: [
      { id: "reports", sub: "/sikayetler", label: "Elan və mağaza", icon: "reports", section: "reports" },
      { id: "chats", sub: "/yazisma-sikayetleri", label: "Yazışmalar", icon: "chats", section: "reports" },
    ],
  },
  {
    title: "Kataloq",
    items: [
      { id: "catalog", sub: "/kateqoriyalar", label: "Kateqoriyalar və sahələr", icon: "catalog", section: "catalog" },
      { id: "banners", sub: "/bannerler", label: "Ana səhifə bannerləri", icon: "banners", section: "catalog" },
      { id: "places", sub: "/yerler", label: "Şəhər və rayonlar", icon: "places", section: "catalog" },
    ],
  },
  {
    title: "İstifadəçilər",
    items: [
      { id: "users", sub: "/istifadeciler", label: "İstifadəçilər", icon: "users", section: "users" },
      { id: "notify", sub: "/bildirisler", label: "Bildiriş göndər", icon: "notify", section: "users" },
      { id: "support", sub: "/destek", label: "Dəstək müraciətləri", icon: "support", badge: "support", section: "support" },
    ],
  },
  {
    title: "Maliyyə",
    items: [
      { id: "finance", sub: "/maliyye", label: "Maliyyə icmalı", icon: "finance", section: "finance" },
      { id: "tariffs", sub: "/tariflar", label: "Tariflər", icon: "finance", section: "finance" },
    ],
  },
  {
    title: "Sistem",
    items: [
      { id: "staff", sub: "/isciler", label: "İşçilər və icazələr", icon: "staff", superOnly: true },
      { id: "audit", sub: "/audit", label: "Əməliyyat tarixçəsi", icon: "audit", superOnly: true },
    ],
  },
];
