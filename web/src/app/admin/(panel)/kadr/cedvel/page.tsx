import Link from "next/link";
import { Card, LoadProblem, PageHead } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { MONTHS, dayStatus, shiftMonth, thisMonth, type MonthSheet } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";

const letters = ["B", "Ç", "Ç", "C", "C", "Ş", "B"]; // Monday first

/** The month as a table: a row per person, a box per day. Red is a working day without a start or a day off. */
export default async function KadrSheetPage({ searchParams }: PageProps<"/admin/kadr/cedvel">) {
  const { year, month, key } = thisMonth((await searchParams).ay);
  const sheet = await authed<MonthSheet>("/api/admin/hr/month", { query: { year, month } });
  if (!sheet.ok) return <LoadProblem what="Cədvəl" status={sheet.status} />;
  const s = sheet.data;
  const days = Array.from({ length: s.daysInMonth }, (_, i) => i + 1);
  const weekday = (d: number) => (new Date(Date.UTC(year, month - 1, d)).getUTCDay() + 6) % 7; // Monday = 0

  return (
    <div className="space-y-5">
      <PageHead
        title={`Cədvəl · ${MONTHS[month - 1]} ${year}`}
        hint="Hər sətir bir işçidir, hər xana bir gün. Qırmızı xana: iş günü idi, işçi başlamayıb və icazəsi yox idi."
        actions={
          <>
            <Link href={adminHref(`/kadr/cedvel?ay=${shiftMonth(year, month, -1)}`)} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
              ‹ Əvvəlki ay
            </Link>
            <Link href={adminHref(`/kadr/cedvel?ay=${shiftMonth(year, month, 1)}`)} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
              Sonrakı ay ›
            </Link>
          </>
        }
      />

      <ul className="flex flex-wrap gap-x-4 gap-y-1 text-xs">
        {[1, 2, 3, 4, 5].map((k) => (
          <li key={k} className="flex items-center gap-1.5">
            <span className={`size-4 rounded ${dayStatus[k].cell}`} aria-hidden />
            {dayStatus[k].label}
          </li>
        ))}
      </ul>

      <Card>
        <div className="overflow-x-auto">
          <table key={key} className="w-max min-w-full border-collapse text-center text-xs">
            <thead>
              <tr className="border-b border-line text-ink-2">
                <th className="sticky left-0 z-10 min-w-40 bg-surface px-3 py-2 text-left font-bold">İşçi</th>
                {days.map((d) => (
                  <th key={d} className={`min-w-7 px-0.5 py-1 font-semibold ${weekday(d) >= 5 ? "text-ink-2/60" : ""}`}>
                    <span className="block">{d}</span>
                    <span className="block text-[10px]">{letters[weekday(d)]}</span>
                  </th>
                ))}
                <th className="px-2 font-bold text-success">İşdə</th>
                <th className="px-2 font-bold text-warning">Gec.</th>
                <th className="px-2 font-bold text-danger">Gəlməyib</th>
                <th className="px-2 font-bold text-primary">İcazə</th>
              </tr>
            </thead>
            <tbody>
              {s.rows.length === 0 && (
                <tr>
                  <td colSpan={days.length + 5} className="px-4 py-8 text-ink-2">
                    Hələ işçi (Admin) yoxdur.
                  </td>
                </tr>
              )}
              {s.rows.map((r) => (
                <tr key={r.userId} className="border-b border-line last:border-b-0">
                  <td className="sticky left-0 z-10 bg-surface px-3 py-1.5 text-left text-sm font-bold">{r.fullName}</td>
                  {r.days.map((status, i) => (
                    <td key={i} className="p-0.5">
                      <span
                        title={`${i + 1}.${String(month).padStart(2, "0")} · ${dayStatus[status]?.label ?? ""}`}
                        className={`flex h-7 min-w-6 items-center justify-center rounded font-bold ${dayStatus[status]?.cell ?? ""}`}
                      >
                        {dayStatus[status]?.short}
                      </span>
                    </td>
                  ))}
                  <td className="px-2 font-bold">{r.present}</td>
                  <td className="px-2 font-bold">{r.late}</td>
                  <td className={`px-2 font-extrabold ${r.absent > 0 ? "text-danger" : ""}`}>{r.absent}</td>
                  <td className="px-2 font-bold">{r.leave}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
