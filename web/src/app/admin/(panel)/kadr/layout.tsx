import { KadrTabs } from "@/components/admin/KadrTabs";
import { requireHr } from "@/lib/admin";
import type { LeaveRequest, StaffRequest } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

/** Looking after the staff: the owner, or an Admin who was given this section on purpose. */
export default async function KadrLayout({ children }: { children: React.ReactNode }) {
  await requireHr();
  const owner = (await getRoles()).includes("SuperAdmin");

  const [leaves, requests] = await Promise.all([
    authed<LeaveRequest[]>("/api/admin/hr/leaves", { query: { status: 1 } }),
    authed<StaffRequest[]>("/api/admin/hr/requests", { query: { status: 1, ...(owner ? { scope: "owner" } : {}) } }),
  ]);

  return (
    <div className="space-y-5">
      <KadrTabs
        owner={owner}
        counts={{
          leaves: leaves.ok ? leaves.data.length : 0,
          requests: requests.ok ? requests.data.length : 0,
        }}
      />
      {children}
    </div>
  );
}
