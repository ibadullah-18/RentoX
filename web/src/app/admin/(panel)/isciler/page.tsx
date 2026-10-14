import { notFound } from "next/navigation";
import { StaffManager } from "@/components/admin/StaffManager";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import type { StaffMember } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import { getRoles, getSession } from "@/lib/session";

/** Who may use the panel. Only for the SuperAdmin. */
export default async function StaffPage() {
  const roles = await getRoles();
  if (!roles.includes("SuperAdmin")) notFound();

  const [session, staff] = await Promise.all([getSession(), authed<StaffMember[]>("/api/admin/staff")]);
  if (!staff.ok) return <LoadProblem what="İşçilər" status={staff.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="İşçilər və icazələr"
        hint="Admin icazəsi verilən şəxs panelə daxil ola bilir; SuperAdmin burada dəyişmir. Hər dəyişiklik əməliyyat tarixçəsinə düşür."
      />
      <StaffManager members={staff.data} me={session?.userId ?? ""} />
    </div>
  );
}
