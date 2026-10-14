import type { Metadata } from "next";
import Link from "next/link";
import { requireAdmin } from "@/lib/admin";

export const metadata: Metadata = { title: "Admin", robots: { index: false, follow: false } };

const tabs = [
  { href: "/admin", label: "Panel" },
  { href: "/admin/elanlar", label: "Elanlar" },
  { href: "/admin/magazalar", label: "Mağazalar" },
  { href: "/admin/destek", label: "Dəstək" },
  { href: "/admin/sikayetler", label: "Şikayətlər" },
];

export default async function AdminLayout({ children }: LayoutProps<"/admin">) {
  await requireAdmin("/admin");

  return (
    <div className="space-y-6">
      <nav aria-label="Admin" className="strip border-b border-line pb-3">
        {tabs.map((t) => (
          <Link
            key={t.href}
            href={t.href}
            className="shrink-0 rounded-lg px-4 py-2 text-sm font-bold hover:bg-primary-soft hover:text-primary"
          >
            {t.label}
          </Link>
        ))}
      </nav>
      {children}
    </div>
  );
}
