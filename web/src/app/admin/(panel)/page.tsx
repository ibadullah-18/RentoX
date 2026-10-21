import { fmtNumeric } from "@/lib/dates";
import Link from "next/link";
import { ArrowUpRight } from "lucide-react";
import { DailyBars, type DailyPoint } from "@/components/admin/DailyBars";
import { Card, PageHead } from "@/components/admin/ui";
import { PrintButton } from "@/components/admin/PrintButton";
import { adminHref } from "@/lib/admin-path";
import { trName, type AdminCategory } from "@/lib/admin-catalog";
import type { Dashboard } from "@/lib/admin-types";
import { getDashboard } from "@/lib/admin";
import { authed } from "@/lib/authed";
import { getSections } from "@/lib/session";
import { statusLabel } from "@/lib/status";
import type { Paged } from "@/lib/types";

const metricLabel: Record<string, string> = {
  users: "İstifadəçilər",
  listings: "Elanlar",
  conversations: "Söhbətlər",
  messages: "Mesajlar",
  supportTickets: "Dəstək müraciətləri",
  stores: "Mağazalar",
};

const num = new Intl.NumberFormat("az");

/** How many items wait in a list (one tiny request: only the total is read). */
async function waiting(path: string, status: number): Promise<number> {
  const res = await authed<Paged<unknown>>(path, { query: { status, page: 1, pageSize: 1 } });
  return res.ok ? res.data.totalCount : 0;
}

export default async function AdminDashboard() {
  const sections = await getSections();
  const has = (section: string) => sections === null || sections.includes(section);
  const [result, categoryCounts, categories, daily, contentReports, chatReports] = await Promise.all([
    getDashboard(),
    authed<{ categoryId: string; activeListings: number }[]>("/api/admin/analytics/categories"),
    has("catalog") ? authed<AdminCategory[]>("/api/admin/categories") : Promise.resolve(null),
    authed<DailyPoint[]>("/api/admin/analytics/daily", { query: { days: 30 } }),
    has("reports") ? waiting("/api/admin/content-reports", 1) : Promise.resolve(0),
    has("reports") ? waiting("/api/admin/conversation-reports", 1) : Promise.resolve(0),
  ]);
  if (!result.ok) {
    return <p className="rounded-xl bg-danger/10 px-4 py-3 font-semibold text-danger">Statistika yüklənmədi.</p>;
  }
  const d = result.data;

  const count = (module: string, status: number) =>
    d.statusCounts.find((s) => s.module === module && s.status === status)?.count ?? 0;

  // The work that is waiting comes first: it is what an admin starts the day with.
  const queues = [
    { section: "listings", href: adminHref("/elanlar"), label: "Yoxlama gözləyən elanlar", n: count("listings", 2) },
    { section: "stores", href: adminHref("/magazalar"), label: "Yoxlama gözləyən mağazalar", n: count("stores", 2) },
    { section: "support", href: adminHref("/destek"), label: "Açıq dəstək müraciətləri", n: count("support", 1) },
    { section: "reports", href: adminHref("/sikayetler"), label: "Gözləyən elan/mağaza şikayətləri", n: contentReports },
    { section: "reports", href: adminHref("/yazisma-sikayetleri"), label: "Gözləyən yazışma şikayətləri", n: chatReports },
  ].filter((q) => has(q.section));

  return (
    <div className="space-y-8">
      <PageHead title="İcmal" hint="Gözləyən işlər və saytın ümumi vəziyyəti." actions={<PrintButton />} />

      <section aria-label="Gözləyən işlər" className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {queues.map((q) => (
          <Link
            key={q.href}
            href={q.href}
            className="group flex items-center justify-between gap-3 rounded-xl border border-line bg-surface p-4 hover:border-primary"
          >
            <span className="text-sm font-semibold">{q.label}</span>
            <span className="flex items-center gap-1">
              <span className={`text-2xl font-extrabold ${q.n > 0 ? "text-warning" : "text-ink-2"}`}>{q.n}</span>
              <ArrowUpRight className="size-4 text-ink-2 group-hover:text-primary" aria-hidden />
            </span>
          </Link>
        ))}
      </section>

      {daily.ok && (
        <Card title="Son 30 gün">
          <DailyBars points={daily.data} />
        </Card>
      )}

      {categoryCounts.ok && categories?.ok && categoryCounts.data.length > 0 && (
        <Card title="Kateqoriyalar üzrə aktiv elanlar">
          <ul className="space-y-2 p-4">
            {categoryCounts.data.slice(0, 10).map((c) => {
              const top = categoryCounts.data[0].activeListings;
              const category = categories.data.find((x) => x.id === c.categoryId);
              return (
                <li key={c.categoryId} className="flex items-center gap-3 text-sm">
                  <span className="w-40 shrink-0 truncate font-semibold sm:w-56">{category ? trName(category.translations) : "—"}</span>
                  <span className="h-3 min-w-0 flex-1 overflow-hidden rounded-full bg-primary-soft">
                    <span className="block h-full rounded-full bg-primary" style={{ width: `${Math.max(3, (c.activeListings / top) * 100)}%` }} />
                  </span>
                  <span className="w-10 shrink-0 text-right font-bold">{num.format(c.activeListings)}</span>
                </li>
              );
            })}
          </ul>
        </Card>
      )}

      <Card title="Fəaliyyət">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-line text-ink-2">
              <tr>
                <th className="px-4 py-3 font-semibold">Göstərici</th>
                <th className="px-4 py-3 text-right font-semibold">Cəmi</th>
                <th className="px-4 py-3 text-right font-semibold">7 gün</th>
                <th className="px-4 py-3 text-right font-semibold">30 gün</th>
              </tr>
            </thead>
            <tbody>
              {d.activity.map((a) => (
                <tr key={a.metric} className="border-b border-line last:border-b-0">
                  <td className="px-4 py-3 font-semibold">{metricLabel[a.metric] ?? a.metric}</td>
                  <td className="px-4 py-3 text-right">{num.format(a.total)}</td>
                  <td className="px-4 py-3 text-right">{num.format(a.last7Days)}</td>
                  <td className="px-4 py-3 text-right">{num.format(a.last30Days)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <p className="border-t border-line px-4 py-2.5 text-xs text-ink-2">
          Elan və mağaza sayına silinmişlər də daxildir; bu, saytda görünən elanların sayı deyil.
        </p>
      </Card>

      <section>
        <h2 className="mb-3 text-lg font-extrabold">Elanların vəziyyəti</h2>
        <ul className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          {d.statusCounts
            .filter((s) => s.module === "listings" && s.count > 0)
            .map((s) => (
              <li key={s.status} className="rounded-xl border border-line bg-surface p-4">
                <p className="text-sm text-ink-2">{statusLabel[s.status] ?? `Status ${s.status}`}</p>
                <p className="text-2xl font-extrabold">{num.format(s.count)}</p>
              </li>
            ))}
        </ul>
      </section>

      <div className="grid gap-6 xl:grid-cols-2">
        <TopList title="Ən çox baxılan elanlar" items={d.mostViewedListings} unit="baxış" pick="viewCount" />
        <TopList title="Ən çox seçilmiş elanlar" items={d.mostFavoritedListings} unit="♥" pick="favoriteCount" />
      </div>

      <p className="text-xs text-ink-2">Yenilənib: {fmtNumeric(d.generatedAtUtc)}</p>
    </div>
  );
}

function TopList({
  title,
  items,
  unit,
  pick,
}: {
  title: string;
  items: Dashboard["mostViewedListings"];
  unit: string;
  pick: "viewCount" | "favoriteCount";
}) {
  if (items.length === 0) return null;
  return (
    <Card title={title}>
      <ol className="divide-y divide-line">
        {items.map((l, i) => (
          <li key={l.id}>
            <Link href={`/listing/${l.id}`} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
              <span className="w-5 text-sm font-bold text-ink-2">{i + 1}</span>
              <span className="min-w-0 flex-1 truncate text-sm font-semibold">{l.title}</span>
              <span className="shrink-0 text-sm text-ink-2">
                {num.format(l[pick])} {unit}
              </span>
            </Link>
          </li>
        ))}
      </ol>
    </Card>
  );
}
