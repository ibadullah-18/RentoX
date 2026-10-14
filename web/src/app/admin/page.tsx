import Link from "next/link";
import { authed } from "@/lib/authed";
import type { Dashboard } from "@/lib/admin-types";
import { money, statusLabel } from "@/lib/status";

const metricLabel: Record<string, string> = {
  users: "İstifadəçilər",
  listings: "Elanlar",
  conversations: "Söhbətlər",
  messages: "Mesajlar",
  supportTickets: "Dəstək müraciətləri",
  stores: "Mağazalar",
};

const num = new Intl.NumberFormat("az");

export default async function AdminDashboard() {
  const result = await authed<Dashboard>("/api/admin/analytics/dashboard");
  if (!result.ok) {
    return <p className="rounded-xl bg-danger/10 px-4 py-3 font-semibold text-danger">Statistika yüklənmədi.</p>;
  }
  const d = result.data;

  // Listings waiting for a decision are what an admin looks at first.
  const waiting = (module: string, status: number) =>
    d.statusCounts.find((s) => s.module === module && s.status === status)?.count ?? 0;

  const queues = [
    { href: "/admin/elanlar", label: "Yoxlama gözləyən elanlar", count: waiting("listings", 2) },
    { href: "/admin/magazalar", label: "Yoxlama gözləyən mağazalar", count: waiting("stores", 2) },
  ];

  return (
    <div className="space-y-8">
      <section className="grid gap-3 sm:grid-cols-2">
        {queues.map((q) => (
          <Link
            key={q.href}
            href={q.href}
            className="flex items-center justify-between rounded-xl border border-line bg-surface p-5 hover:border-primary"
          >
            <span className="font-bold">{q.label}</span>
            <span className={`text-3xl font-extrabold ${q.count > 0 ? "text-warning" : "text-ink-2"}`}>{q.count}</span>
          </Link>
        ))}
      </section>

      <section>
        <h2 className="mb-3 text-lg font-extrabold">Fəaliyyət</h2>
        <div className="overflow-x-auto rounded-xl border border-line bg-surface">
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
      </section>

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

      {d.walletBalances.length > 0 && (
        <section>
          <h2 className="mb-3 text-lg font-extrabold">Pul kisələri</h2>
          <ul className="grid gap-2 sm:grid-cols-2">
            {d.walletBalances.map((w) => (
              <li key={w.currency} className="rounded-xl border border-line bg-surface p-4">
                <p className="text-sm text-ink-2">{num.format(w.walletCount)} pul kisəsi</p>
                <p className="text-2xl font-extrabold">
                  {money.format(w.totalBalance)} {w.currency}
                </p>
              </li>
            ))}
          </ul>
        </section>
      )}

      {d.mostViewedListings.length > 0 && (
        <section>
          <h2 className="mb-3 text-lg font-extrabold">Ən çox baxılan elanlar</h2>
          <ol className="space-y-2">
            {d.mostViewedListings.map((l) => (
              <li key={l.id}>
                <Link
                  href={`/listing/${l.id}`}
                  className="flex items-center justify-between gap-3 rounded-xl border border-line bg-surface px-4 py-3 hover:border-primary"
                >
                  <span className="truncate font-semibold">{l.title}</span>
                  <span className="shrink-0 text-sm text-ink-2">
                    {num.format(l.viewCount)} baxış · {num.format(l.favoriteCount)} ♥
                  </span>
                </Link>
              </li>
            ))}
          </ol>
        </section>
      )}

      <p className="text-xs text-ink-2">
        Yenilənib: {new Date(d.generatedAtUtc).toLocaleString("az")}
      </p>
    </div>
  );
}
