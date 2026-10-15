import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronRight, Users } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { Card, Empty, LoadProblem, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { WEEKDAYS, hhmm, money, type StaffProfile } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

/** The staff with their pay, whether they may sign in, and their hours. Only for the owner. */
export default async function KadrStaffPage() {
  if (!(await getRoles()).includes("SuperAdmin")) notFound();

  const staff = await authed<StaffProfile[]>("/api/admin/hr/staff");
  if (!staff.ok) return <LoadProblem what="İşçilər" status={staff.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="İşçilər, maaş və giriş"
        hint="Hər işçinin maaşı, panelə giriş icazəsi, iş saatları və icazə limiti. Birinin üzərinə basıb maaş sətirləri (kəsinti, avans, ödəniş) əlavə edə bilərsiniz. Kimin Admin olacağını «İşçilər və icazələr» bölməsində seçirsiniz."
        actions={
          <Link href={adminHref("/isciler")} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
            İşçilər və icazələr
          </Link>
        }
      />

      {staff.data.length === 0 ? (
        <Empty icon={Users} text="Hələ işçi (Admin) yoxdur" />
      ) : (
        <Card>
          <ul className="divide-y divide-line">
            {staff.data.map((p) => (
              <li key={p.userId}>
                <Link href={adminHref(`/kadr/isciler/${p.userId}`)} className="flex flex-wrap items-center gap-3 px-4 py-3 hover:bg-primary-soft">
                  <Avatar name={p.fullName} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-extrabold">{p.fullName}</p>
                    <p className="text-sm text-ink-2">
                      {p.phone ?? "—"} · {hhmm(p.schedule.workStartMinute)}–{hhmm(p.schedule.workEndMinute)} ·{" "}
                      {WEEKDAYS.filter((_, i) => (p.schedule.workDaysMask & (1 << i)) !== 0).join(", ") || "iş günü yoxdur"}
                    </p>
                  </div>
                  <span className="text-right text-sm">
                    <span className="block font-extrabold">{money(p.monthlySalaryInQepik)} AZN</span>
                    <span className="block text-xs text-ink-2">ayda {p.schedule.monthlyDayOffLimit} icazə günü</span>
                  </span>
                  <Pill tone={p.loginAllowed ? "success" : "danger"}>{p.loginAllowed ? "Giriş açıqdır" : "Giriş bağlıdır"}</Pill>
                  <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
