import Link from "next/link";
import { notFound } from "next/navigation";
import { CancelLeaveButton, LeaveRequestForm, StaffRequestForm, WorkDay } from "@/components/admin/HrForms";
import { Card, LoadProblem, PageHead, Pill, dateTime } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import {
  MONTHS,
  WEEKDAYS,
  bakuToday,
  dayText,
  hhmm,
  leaveStatusLabel,
  leaveStatusTone,
  money,
  payKind,
  requestStatusLabel,
  requestStatusTone,
  shiftMonth,
  thisMonth,
  type LeaveRequest,
  type MyDay,
  type PayrollSheet,
  type StaffRequest,
} from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

/** A staff member's own page: start and finish the day, ask for days off, see the pay, write to the management. */
export default async function MyWorkPage({ searchParams }: PageProps<"/admin/menim-isim">) {
  if ((await getRoles()).includes("SuperAdmin")) notFound();
  const { year, month, key } = thisMonth((await searchParams).ay);
  const today = bakuToday();

  const [day, leaves, pay, requests] = await Promise.all([
    authed<MyDay>("/api/admin/hr/me/day"),
    authed<LeaveRequest[]>("/api/admin/hr/me/leaves"),
    authed<PayrollSheet>("/api/admin/hr/me/payroll", { query: { month: key } }),
    authed<StaffRequest[]>("/api/admin/hr/me/requests"),
  ]);
  if (!day.ok) return <LoadProblem what="Mənim işim" status={day.status} />;
  const d = day.data;

  return (
    <div className="space-y-5">
      <PageHead title="Mənim işim" hint="İşə başlayanda və bitirəndə düyməyə basın. İcazəni əvvəlcədən istəyin, maaşınıza və müraciətlərinizə buradan baxın." />

      <div className="grid gap-5 xl:grid-cols-2">
        <Card title="Bu gün">
          <WorkDay day={d} big />
          <p className="border-t border-line px-5 py-3 text-sm text-ink-2">
            İş günləriniz: {WEEKDAYS.filter((_, i) => (d.schedule.workDaysMask & (1 << i)) !== 0).join(", ") || "yoxdur"} · {hhmm(d.schedule.workStartMinute)}–
            {hhmm(d.schedule.workEndMinute)}. {hhmm(d.schedule.workStartMinute)}-dan 15 dəqiqə sonra başlamasanız «gecikib» yazılır, daha gec isə rəhbərə
            «başlamayıb» bildirişi gedir.
          </p>
        </Card>

        <Card title="İcazə istə">
          <LeaveRequestForm used={d.dayOffUsedThisMonth} limit={d.schedule.monthlyDayOffLimit} />
        </Card>
      </div>

      <Card title="İcazələrim">
        {leaves.ok && leaves.data.length > 0 ? (
          <ul className="divide-y divide-line">
            {leaves.data.map((l) => (
              <li key={l.id} className="flex flex-wrap items-center gap-3 px-4 py-3 text-sm">
                <span className="min-w-0 flex-1">
                  <b>{l.fromDay === l.toDay ? dayText(l.fromDay) : `${dayText(l.fromDay)} – ${dayText(l.toDay)}`}</b> · {l.workingDays} iş günü
                  <span className="block text-ink-2">Səbəb: {l.reason}</span>
                  {l.decisionNote && <span className="block text-ink-2">Rəhbər: {l.decisionNote}</span>}
                </span>
                {l.overLimit && <Pill tone="danger">limit aşılır</Pill>}
                <Pill tone={leaveStatusTone[l.status]}>{leaveStatusLabel[l.status]}</Pill>
                {(l.status === 1 || (l.status === 2 && l.fromDay > today)) && <CancelLeaveButton id={l.id} />}
              </li>
            ))}
          </ul>
        ) : (
          <p className="p-4 text-sm text-ink-2">Hələ icazə istəməmisiniz.</p>
        )}
      </Card>

      <Card title={`Maaşım · ${MONTHS[month - 1]} ${year}`}>
        <div className="flex items-center justify-between gap-2 border-b border-line px-4 py-2 text-sm">
          <Link href={adminHref(`/menim-isim?ay=${shiftMonth(year, month, -1)}`)} className="font-semibold text-primary hover:underline">
            ‹ Əvvəlki ay
          </Link>
          <Link href={adminHref(`/menim-isim?ay=${shiftMonth(year, month, 1)}`)} className="font-semibold text-primary hover:underline">
            Sonrakı ay ›
          </Link>
        </div>
        {pay.ok ? (
          <>
            <dl className="grid grid-cols-2 gap-x-6 gap-y-2 p-4 text-sm sm:grid-cols-3">
              {[
                ["Əsas maaş", pay.data.baseInQepik],
                ["Mükafat", pay.data.bonusInQepik],
                ["Kəsinti", pay.data.deductionsInQepik],
                ["Avans", pay.data.advancesInQepik],
                ["Ödənilib", pay.data.paymentsInQepik],
                ["Qalıb", pay.data.remainingInQepik],
              ].map(([label, value]) => (
                <div key={label as string}>
                  <dt className="text-ink-2">{label}</dt>
                  <dd className="font-extrabold">{money(value as number)} AZN</dd>
                </div>
              ))}
            </dl>
            {pay.data.lines.filter((l) => !l.isVoid).length > 0 && (
              <ul className="divide-y divide-line border-t border-line">
                {pay.data.lines
                  .filter((l) => !l.isVoid)
                  .map((l) => (
                    <li key={l.id} className="px-4 py-2.5 text-sm">
                      <span className={`font-bold ${payKind[l.kind]?.tone}`}>
                        {payKind[l.kind]?.label} {payKind[l.kind]?.sign}
                        {money(l.amountInQepik)} AZN
                      </span>
                      {l.note && <span className="block text-ink-2">{l.note}</span>}
                      <span className="block text-xs text-ink-2">{dateTime(l.createdAtUtc)}</span>
                    </li>
                  ))}
              </ul>
            )}
          </>
        ) : (
          <p className="p-4 text-sm text-ink-2">Maaş məlumatı yüklənmədi.</p>
        )}
      </Card>

      <div className="grid gap-5 xl:grid-cols-2">
        <Card title="Müraciət yaz">
          <StaffRequestForm hasWatcher={d.hasWatcher} />
        </Card>

        <Card title="Müraciətlərim">
          {requests.ok && requests.data.length > 0 ? (
            <ul className="divide-y divide-line">
              {requests.data.map((r) => (
                <li key={r.id} className="space-y-1 px-4 py-3 text-sm">
                  <p className="flex flex-wrap items-center gap-2 font-bold">
                    {r.subject}
                    <Pill tone={requestStatusTone[r.status]}>{requestStatusLabel[r.status]}</Pill>
                  </p>
                  <p className="whitespace-pre-wrap text-ink-2">{r.body}</p>
                  {r.answer && (
                    <p className="rounded-lg bg-bg px-3 py-2">
                      <b>{r.handledByName ?? "Cavab"}:</b> {r.answer}
                    </p>
                  )}
                  <p className="text-xs text-ink-2">{dateTime(r.createdAtUtc)}</p>
                </li>
              ))}
            </ul>
          ) : (
            <p className="p-4 text-sm text-ink-2">Hələ müraciətiniz yoxdur.</p>
          )}
        </Card>
      </div>
    </div>
  );
}
