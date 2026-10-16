import { fmtDate } from "@/lib/dates";
export type DailyPoint = { date: string; newUsers: number; newListings: number };

const label = (iso: string) => fmtDate(`${iso}T00:00:00Z`, "az", { short: true, year: false });

/** One bar per day, tallest day at the top; hover (or long-press) a bar for its number. */
function Bars({ title, points, pick, color }: { title: string; points: DailyPoint[]; pick: (p: DailyPoint) => number; color: string }) {
  const max = Math.max(1, ...points.map(pick));
  const total = points.reduce((sum, p) => sum + pick(p), 0);

  return (
    <div>
      <div className="mb-2 flex items-baseline justify-between gap-2">
        <h3 className="text-sm font-bold">{title}</h3>
        <span className="text-xs text-ink-2">cəmi {total} · gündə ən çox {Math.max(0, ...points.map(pick))}</span>
      </div>
      <div className="flex h-28 items-end gap-[3px]" role="img" aria-label={`${title}: cəmi ${total}`}>
        {points.map((p) => {
          const n = pick(p);
          return (
            <div key={p.date} title={`${label(p.date)}: ${n}`} className="flex h-full min-w-0 flex-1 items-end">
              <div className={`w-full rounded-t-sm ${color}`} style={{ height: n === 0 ? 2 : `${Math.max(6, (n / max) * 100)}%`, opacity: n === 0 ? 0.25 : 1 }} />
            </div>
          );
        })}
      </div>
      <div className="mt-1 flex justify-between text-[11px] text-ink-2">
        <span>{label(points[0].date)}</span>
        <span>{label(points[points.length - 1].date)}</span>
      </div>
    </div>
  );
}

/** The last days of sign-ups and new listings, side by side. */
export function DailyBars({ points }: { points: DailyPoint[] }) {
  if (points.length === 0) return null;
  return (
    <div className="grid gap-6 p-4 md:grid-cols-2">
      <Bars title="Yeni istifadəçilər" points={points} pick={(p) => p.newUsers} color="bg-primary" />
      <Bars title="Yeni elanlar" points={points} pick={(p) => p.newListings} color="bg-success" />
    </div>
  );
}
