import { AdminShell } from "@/components/admin/AdminShell";
import { adminHref } from "@/lib/admin-path";
import { adminNav } from "@/lib/admin-nav";
import { getDashboard, requireAdmin } from "@/lib/admin";
import { getRoles, getSections } from "@/lib/session";

export default async function AdminLayout({ children }: LayoutProps<"/admin">) {
  await requireAdmin(adminHref());

  const [roles, dashboard, sections] = await Promise.all([getRoles(), getDashboard(), getSections()]);
  const isSuper = roles.includes("SuperAdmin");

  // The work that is waiting, shown as numbers next to the menu items.
  const waiting = (module: string, status: number) =>
    dashboard.ok ? (dashboard.data.statusCounts.find((s) => s.module === module && s.status === status)?.count ?? 0) : 0;
  const badges = {
    listings: waiting("listings", 2),
    stores: waiting("stores", 2),
    support: waiting("support", 1),
  };

  const groups = adminNav
    .map((g) => ({
      ...g,
      items: g.items.filter((i) => (!i.superOnly || isSuper) && (!i.section || sections === null || sections.includes(i.section))),
    }))
    .filter((g) => g.items.length > 0);

  return (
    <AdminShell groups={groups} badges={badges} who={{ name: "RentoX", role: isSuper ? "SuperAdmin" : "Admin" }}>
      {children}
    </AdminShell>
  );
}
