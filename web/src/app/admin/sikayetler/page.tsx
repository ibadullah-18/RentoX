import Link from "next/link";
import { ChevronRight, Flag } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { authed } from "@/lib/authed";
import type { ReportSummary } from "@/lib/admin-types";
import { reportReason, reportStatus } from "@/lib/admin";
import type { Paged } from "@/lib/types";

const tone: Record<number, string> = {
  1: "bg-warning-soft text-warning",
  2: "bg-primary-soft text-primary",
  3: "bg-success-soft text-success",
  4: "bg-line text-ink-2",
};

export default async function ReportsPage({ searchParams }: PageProps<"/admin/sikayetler">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const status = one(sp.status);

  const result = await authed<Paged<ReportSummary>>("/api/admin/content-reports", {
    query: { page, pageSize: 20, status },
  });
  const data = result.ok ? result.data : null;

  const filters: [string | undefined, string][] = [
    [undefined, "Hamısı"],
    ...Object.entries(reportStatus).map(([v, label]): [string, string] => [v, label]),
  ];

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-extrabold">Şikayətlər (elan və mağaza)</h1>

      <div className="strip">
        {filters.map(([value, label]) => (
          <Link
            key={label}
            href={value ? `/admin/sikayetler?status=${value}` : "/admin/sikayetler"}
            className={`shrink-0 rounded-lg border px-4 py-2 text-sm font-semibold ${
              status === value ? "border-primary bg-primary text-white" : "border-line bg-surface hover:border-primary"
            }`}
          >
            {label}
          </Link>
        ))}
      </div>

      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-16 text-center">
          <Flag className="size-12 text-primary" aria-hidden />
          <p className="font-bold">Şikayət yoxdur</p>
        </div>
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((r) => (
              <li key={r.id}>
                <Link
                  href={`/admin/sikayetler/${r.id}`}
                  className="flex items-center gap-3 rounded-xl border border-line bg-surface p-4 hover:border-primary"
                >
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-bold">{r.targetTitle ?? "—"}</span>
                    <span className="text-sm text-ink-2">
                      {r.targetType === 1 ? "Elan" : "Mağaza"} · {reportReason[r.reason] ?? "—"} ·{" "}
                      {new Date(r.createdAtUtc).toLocaleString("az")}
                    </span>
                  </span>
                  <span className={`rounded-md px-2.5 py-1 text-xs font-bold ${tone[r.status] ?? ""}`}>
                    {reportStatus[r.status] ?? "—"}
                  </span>
                  <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => {
              const q = new URLSearchParams();
              if (status) q.set("status", status);
              if (p > 1) q.set("page", String(p));
              const text = q.toString();
              return text ? `/admin/sikayetler?${text}` : "/admin/sikayetler";
            }}
          />
        </>
      )}
    </div>
  );
}
