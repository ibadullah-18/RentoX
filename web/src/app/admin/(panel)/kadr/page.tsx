import Link from "next/link";
import { CalendarCheck, TriangleAlert } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { Card, Empty, LoadProblem, PageHead } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { bakuToday, bakuTime, dayStatus, dayText, duration, hhmm, type DayBoardRow } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";

const shiftDay = (day: string, by: number) => new Date(Date.parse(`${day}T00:00:00Z`) + by * 86_400_000).toISOString().slice(0, 10);

/** Who came to work today: the red box is the ones who did not. */
export default async function KadrTodayPage({ searchParams }: PageProps<"/admin/kadr">) {
  const raw = (await searchParams).gun;
  const asked = Array.isArray(raw) ? raw[0] : raw;
  const today = bakuToday();
  const day = asked && /^\d{4}-\d{2}-\d{2}$/.test(asked) ? asked : today;

  const board = await authed<DayBoardRow[]>("/api/admin/hr/board", { query: { day } });
  if (!board.ok) return <LoadProblem what="İşçilərin siyahısı" status={board.status} />;
  const rows = board.data;

  const count = (s: number) => rows.filter((r) => r.status === s).length;
  const absent = rows.filter((r) => r.status === 3);
  const chips = [
    { s: 1, n: count(1) },
    { s: 2, n: count(2) },
    { s: 3, n: count(3) },
    { s: 4, n: count(4) },
  ];

  return (
    <div className="space-y-5">
      <PageHead
        title="İşçilər bu gün"
        hint={`${dayText(day)} · kim işə gəlib, kim gecikib, kim gəlməyib. Qırmızı: iş günüdür, başlamayıb və icazəsi yoxdur.`}
        actions={
          <>
            <Link href={adminHref(`/kadr?gun=${shiftDay(day, -1)}`)} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
              ‹ Əvvəlki gün
            </Link>
            {day !== today && (
              <Link href={adminHref("/kadr")} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
                Bu gün
              </Link>
            )}
            <Link href={adminHref(`/kadr?gun=${shiftDay(day, 1)}`)} className="flex h-9 items-center rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary">
              Sonrakı gün ›
            </Link>
          </>
        }
      />

      <ul className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {chips.map(({ s, n }) => (
          <li key={s} className={`rounded-xl px-4 py-3 ${s === 3 && n > 0 ? "bg-danger text-white" : "border border-line bg-surface"}`}>
            <p className="text-2xl font-extrabold">{n}</p>
            <p className={`text-sm font-semibold ${s === 3 && n > 0 ? "text-white/90" : "text-ink-2"}`}>{dayStatus[s].label}</p>
          </li>
        ))}
      </ul>

      {absent.length > 0 && (
        <div role="alert" className="flex items-start gap-3 rounded-xl border border-danger bg-danger/10 px-4 py-3">
          <TriangleAlert className="mt-0.5 size-5 shrink-0 text-danger" aria-hidden />
          <div>
            <p className="font-extrabold text-danger">İşə gəlməyənlər ({absent.length})</p>
            <p className="text-sm">{absent.map((r) => r.fullName).join(", ")}</p>
          </div>
        </div>
      )}

      {rows.length === 0 ? (
        <Empty icon={CalendarCheck} text="Hələ işçi (Admin) yoxdur. «İşçilər və icazələr» bölməsindən əlavə edin." />
      ) : (
        <Card title={`İşçilər (${rows.length})`}>
          <ul className="divide-y divide-line">
            {rows.map((r) => {
              const info = dayStatus[r.status] ?? dayStatus[6];
              const red = r.status === 3;
              return (
                <li key={r.userId} className={`flex flex-wrap items-center gap-3 px-4 py-3 ${red ? "bg-danger/10" : ""}`}>
                  <Avatar name={r.fullName} />
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-extrabold">
                      {r.fullName}
                      {!r.loginAllowed && <span className="ml-2 rounded-md bg-danger/10 px-1.5 py-0.5 text-[11px] font-bold text-danger">giriş bağlıdır</span>}
                    </p>
                    <p className="text-sm text-ink-2">
                      İş saatı {hhmm(r.workStartMinute)}–{hhmm(r.workEndMinute)}
                    </p>
                  </div>
                  <div className="text-right text-sm">
                    {r.checkInAtUtc ? (
                      <>
                        <p>
                          Başladı <b>{bakuTime(r.checkInAtUtc)}</b>
                          {r.checkOutAtUtc && (
                            <>
                              {" "}
                              · bitdi <b>{bakuTime(r.checkOutAtUtc)}</b>
                            </>
                          )}
                        </p>
                        <p className="text-ink-2">{r.checkOutAtUtc ? `işlədi: ${duration(r.workedMinutes)}` : `${duration(r.workedMinutes)} işləyir`}</p>
                      </>
                    ) : (
                      <p className="text-ink-2">Başlamayıb</p>
                    )}
                  </div>
                  <span className={`rounded-md px-2.5 py-1 text-xs font-bold ${info.chip}`}>{info.label}</span>
                </li>
              );
            })}
          </ul>
        </Card>
      )}
    </div>
  );
}
