"use client";

import { BrandMark } from "@/components/BrandMark";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import {
  Briefcase,
  CalendarCheck,
  ClipboardCheck,
  FolderTree,
  LogOut,
  Flag,
  LayoutDashboard,
  LifeBuoy,
  Megaphone,
  Map as MapIcon,
  GalleryHorizontal,
  FileText,
  Menu,
  Contact,
  OctagonAlert,
  SlidersHorizontal,
  Search,
  MessagesSquare,
  ScrollText,
  ShieldCheck,
  Store,
  Users,
  Wallet,
  X,
} from "lucide-react";
import { adminLogout } from "@/app/admin/giris/actions";
import { AdminLive } from "@/components/admin/AdminLive";
import { WorkDay } from "@/components/admin/HrForms";
import type { MyDay } from "@/lib/admin-hr";
import { ADMIN_SEGMENT, adminHref } from "@/lib/admin-path";
import type { AdminNavGroup } from "@/lib/admin-nav";

const icons = {
  dashboard: LayoutDashboard,
  catalog: FolderTree,
  users: Users,
  staff: ShieldCheck,
  listings: ClipboardCheck,
  stores: Store,
  reports: Flag,
  chats: MessagesSquare,
  support: LifeBuoy,
  finance: Wallet,
  audit: ScrollText,
  notify: Megaphone,
  places: MapIcon,
  settings: SlidersHorizontal,
  banned: OctagonAlert,
  banners: GalleryHorizontal,
  pages: FileText,
  contact: Contact,
  hr: CalendarCheck,
  mywork: Briefcase,
};

/**
 * The frame of the admin panel: a menu on the left (a drawer on a phone),
 * the name of the person who is signed in, and the page on the right.
 */
export function AdminShell({
  groups,
  badges,
  who,
  workDay,
  brand,
  children,
}: {
  groups: AdminNavGroup[];
  badges: Record<string, number>;
  who: { name: string; role: string };
  /** The logo as the owner set it (the same pictures as on the site). */
  brand: { light: string; dark: string; name: string; text: boolean };
  /** A staff member's day (start and finish); null for the owner. */
  workDay?: MyDay | null;
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);

  // "/" jumps to the search box (unless a field already has the cursor).
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const el = e.target as HTMLElement | null;
      const typing = el && (el.tagName === "INPUT" || el.tagName === "TEXTAREA" || el.tagName === "SELECT" || el.isContentEditable);
      if (e.key === "/" && !typing && !e.metaKey && !e.ctrlKey) {
        e.preventDefault();
        searchRef.current?.focus();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const [seen, setSeen] = useState(pathname);
  if (seen !== pathname) {
    setSeen(pathname);
    setOpen(false);
  }

  const base = `/${ADMIN_SEGMENT}`;
  const isActive = (sub: string) => (sub === "" ? pathname === base : pathname.startsWith(`${base}${sub}`));

  const menu = (
    <nav aria-label="Admin" className="flex h-full flex-col">
      <div className="flex items-center justify-between px-4 py-4">
        <Link href={adminHref()} className="flex items-center gap-2 text-lg font-extrabold tracking-tight">
          <BrandMark light={brand.light} dark={brand.dark} name={brand.name} height={32} text={brand.text} />
          <span className="rounded-md bg-primary-soft px-1.5 py-0.5 text-[10px] font-bold uppercase text-primary">admin</span>
        </Link>
        <button
          type="button"
          onClick={() => setOpen(false)}
          aria-label="Bağla"
          className="flex size-9 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft lg:hidden"
        >
          <X className="size-5" />
        </button>
      </div>

      <form action={adminHref("/axtaris")} className="px-3 pb-3">
        <div className="flex h-9 items-center gap-2 rounded-lg border border-line bg-bg px-2.5 focus-within:border-primary">
          <Search className="size-4 shrink-0 text-muted" aria-hidden />
          <input
            ref={searchRef}
            name="q"
            type="search"
            placeholder="Axtar  ( / )"
            aria-label="Axtarış"
            className="min-w-0 flex-1 bg-transparent text-[14px] outline-none"
          />
        </div>
      </form>

      <div className="min-h-0 flex-1 space-y-5 overflow-y-auto px-3 pb-4">
        {groups.map((g) => (
          <div key={g.title}>
            <p className="mb-1 px-2 text-[11px] font-bold uppercase tracking-wide text-muted">{g.title}</p>
            <ul className="space-y-0.5">
              {g.items.map((item) => {
                const Icon = icons[item.icon];
                const active = isActive(item.sub);
                const count = item.badge ? (badges[item.badge] ?? 0) : 0;
                return (
                  <li key={item.id}>
                    <Link
                      href={adminHref(item.sub)}
                      aria-current={active ? "page" : undefined}
                      className={`flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-[14px] font-semibold ${
                        active ? "bg-primary text-white" : "text-ink-2 hover:bg-primary-soft hover:text-primary"
                      }`}
                    >
                      <Icon className="size-[18px] shrink-0" aria-hidden />
                      <span className="flex-1 truncate">{item.label}</span>
                      {count > 0 && (
                        <span
                          className={`flex h-5 min-w-5 items-center justify-center rounded-full px-1.5 text-[11px] font-bold ${
                            active ? "bg-white/25 text-white" : "bg-accent text-white"
                          }`}
                        >
                          {count > 99 ? "99+" : count}
                        </span>
                      )}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </div>

      <div className="space-y-2 border-t border-line p-3">
        {workDay?.isStaff && <WorkDay day={workDay} />}
        <div className="px-1">
          <p className="truncate text-sm font-bold">{who.name}</p>
          <p className="text-xs text-ink-2">{who.role}</p>
        </div>
        <form action={adminLogout}>
          <button
            type="submit"
            className="flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-[13px] font-semibold text-ink-2 hover:bg-danger/10 hover:text-danger"
          >
            <LogOut className="size-4" aria-hidden />
            Çıxış
          </button>
        </form>
      </div>
    </nav>
  );

  return (
    <div className="min-h-dvh bg-bg lg:grid lg:grid-cols-[256px_minmax(0,1fr)]">
      <AdminLive />
      <aside className="no-print sticky top-0 hidden h-dvh border-r border-line bg-surface lg:block">{menu}</aside>

      <header className="sticky top-0 z-40 flex items-center gap-3 border-b border-line bg-surface px-3 py-2.5 lg:hidden">
        <button
          type="button"
          onClick={() => setOpen(true)}
          aria-label="Menyu"
          className="flex size-10 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft"
        >
          <Menu className="size-5" />
        </button>
        <span className="flex items-center gap-2 font-extrabold tracking-tight">
          <BrandMark light={brand.light} dark={brand.dark} name={brand.name} height={30} text={brand.text} />
          <span className="text-xs font-bold uppercase text-primary">admin</span>
        </span>
      </header>

      {open && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button type="button" aria-label="Bağla" onClick={() => setOpen(false)} className="absolute inset-0 bg-black/50" />
          <div className="absolute inset-y-0 left-0 w-72 max-w-[85vw] bg-surface shadow-xl">{menu}</div>
        </div>
      )}

      <main className="min-w-0 px-4 py-6 md:px-8 md:py-8">
        <div className="mx-auto max-w-[1100px]">{children}</div>
      </main>
    </div>
  );
}
