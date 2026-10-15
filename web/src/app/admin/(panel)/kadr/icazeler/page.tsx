import { CalendarOff } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { Chips, Empty, LoadProblem, PageHead, Pill, dateTime } from "@/components/admin/ui";
import { LeaveDecision } from "@/components/admin/HrForms";
import { adminHref } from "@/lib/admin-path";
import { dayText, leaveStatusLabel, leaveStatusTone, type LeaveRequest } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

const views = [
  { value: "1", label: "Gözləyən" },
  { value: "2", label: "Təsdiqlənmiş" },
  { value: "3", label: "Rədd edilmiş" },
  { value: "all", label: "Hamısı" },
];

/** Days off the staff asked for: the owner approves or refuses; a request past the monthly limit is marked red. */
export default async function KadrLeavesPage({ searchParams }: PageProps<"/admin/kadr/icazeler">) {
  const raw = (await searchParams).status;
  const view = (Array.isArray(raw) ? raw[0] : raw) ?? "1";
  const owner = (await getRoles()).includes("SuperAdmin");

  const result = await authed<LeaveRequest[]>("/api/admin/hr/leaves", { query: view === "all" ? {} : { status: view } });
  if (!result.ok) return <LoadProblem what="İcazə sorğuları" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="İcazə sorğuları"
        hint="İşçilər istirahət gününü əvvəlcədən seçib səbəb yazır. Siz təsdiq edəndə həmin günlər «icazəlidir» olur və cədvəldə qırmızı görünmür. Ayda icazə limiti aşılırsa, sorğuda qırmızı xəbərdarlıq olur."
      />
      <Chips
        current={view}
        items={views.map((v) => ({ label: v.label, value: v.value, href: adminHref(`/kadr/icazeler?status=${v.value}`) }))}
      />

      {result.data.length === 0 ? (
        <Empty icon={CalendarOff} text="Bu siyahıda sorğu yoxdur" />
      ) : (
        <ul className="space-y-3">
          {result.data.map((l) => (
            <li key={l.id} className={`flex flex-wrap items-start gap-4 rounded-xl border bg-surface p-4 ${l.overLimit && l.status === 1 ? "border-danger" : "border-line"}`}>
              <Avatar name={l.fullName} />
              <div className="min-w-0 flex-1 space-y-1">
                <p className="flex flex-wrap items-center gap-2 font-extrabold">
                  {l.fullName}
                  <Pill tone={leaveStatusTone[l.status]}>{leaveStatusLabel[l.status]}</Pill>
                  {l.overLimit && <Pill tone="danger">Aylıq limit aşılır ({l.dayOffLimit} gün)</Pill>}
                </p>
                <p className="text-sm">
                  <b>{l.fromDay === l.toDay ? dayText(l.fromDay) : `${dayText(l.fromDay)} – ${dayText(l.toDay)}`}</b> · {l.workingDays} iş günü
                </p>
                <p className="text-sm text-ink-2">Səbəb: {l.reason}</p>
                <p className="text-xs text-ink-2">
                  Sorğu: {dateTime(l.createdAtUtc)}
                  {l.decidedAtUtc ? ` · qərar: ${dateTime(l.decidedAtUtc)}` : ""}
                  {l.decisionNote ? ` · qeyd: ${l.decisionNote}` : ""}
                </p>
              </div>
              {owner && l.status === 1 && <LeaveDecision id={l.id} />}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
