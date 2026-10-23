import { AccessClosed } from "@/components/admin/AccessClosed";
import { AdminShell } from "@/components/admin/AdminShell";
import { SupportRealtimeProvider } from "@/components/support/SupportRealtime";
import { adminHref } from "@/lib/admin-path";
import { adminNav } from "@/lib/admin-nav";
import { getDashboard, requireAdmin } from "@/lib/admin";
import { getReportCounts } from "@/lib/admin-reports";
import type { LeaveRequest, MyDay, StaffRequest } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { brandLogos, getBranding } from "@/lib/api";
import { canSeeHr, getRoles, getSections } from "@/lib/session";

export default async function AdminLayout({ children }: LayoutProps<"/admin">) {
  await requireAdmin(adminHref());

  const roles = await getRoles();
  const isOwner = roles.includes("SuperAdmin");

  // The owner can close a staff member's access: they get this page and nothing else.
  if (!isOwner) {
    const access = await authed<{ allowed: boolean }>("/api/admin/hr/me/access");
    if (access.ok && !access.data.allowed) return <AccessClosed />;
  }

  const [dashboard, sections, workDay, hrWaiting] = await Promise.all([
    getDashboard(),
    getSections(),
    // A staff member's own day (start and finish); the owner has no hours to keep.
    isOwner ? Promise.resolve(null) : authed<MyDay>("/api/admin/hr/me/day"),
    // What waits for the person who looks after the staff: days off to decide and requests to answer.
    (async () => {
      if (!(await canSeeHr())) return 0;
      const [leaves, requests] = await Promise.all([
        authed<LeaveRequest[]>("/api/admin/hr/leaves", { query: { status: 1 } }),
        authed<StaffRequest[]>("/api/admin/hr/requests", { query: { status: 1, ...(isOwner ? { scope: "owner" } : {}) } }),
      ]);
      return (leaves.ok ? leaves.data.length : 0) + (requests.ok ? requests.data.length : 0);
    })(),
  ]);
  // Reports waiting (listing/store and chat together) are the number on the menu.
  const reportCounts = sections === null || sections.includes("reports") ? await getReportCounts() : null;
  const isSuper = roles.includes("SuperAdmin");

  // The work that is waiting, shown as numbers next to the menu items.
  const waiting = (module: string, status: number) =>
    dashboard.ok ? (dashboard.data.statusCounts.find((s) => s.module === module && s.status === status)?.count ?? 0) : 0;
  const badges = {
    listings: waiting("listings", 2),
    stores: waiting("stores", 2),
    support: waiting("support", 1),
    reports: reportCounts ? reportCounts.content.waiting + reportCounts.chat.waiting : 0,
    hr: hrWaiting,
  };

  const groups = adminNav
    .map((g) => ({
      ...g,
      items: g.items.filter(
        (i) =>
          (!i.superOnly || isSuper) &&
          (!i.selfOnly || !isSuper) &&
          // The staff and branding sections are only ever given on purpose; "no section chosen" does not include them.
          (!i.section ||
            (i.section === "hr" || i.section === "branding"
              ? isSuper || (sections?.includes(i.section) ?? false)
              : sections === null || sections.includes(i.section))),
      ),
    }))
    .filter((g) => g.items.length > 0);

  const branding = await getBranding();
  const logos = brandLogos(branding, "header");

  return (
    <SupportRealtimeProvider>
      <AdminShell brand={{ light: logos.light, dark: logos.dark, name: branding.siteName, text: branding.useTextLogo }} groups={groups} badges={badges} who={{ name: "RentoX", role: isSuper ? "SuperAdmin" : "Admin" }} workDay={workDay && workDay.ok ? workDay.data : null}>
        {children}
      </AdminShell>
    </SupportRealtimeProvider>
  );
}
