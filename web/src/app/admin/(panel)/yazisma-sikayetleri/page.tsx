import Link from "next/link";
import { ChevronRight, MessagesSquare } from "lucide-react";
import { Chips, Empty, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { chatReason, reportStatus } from "@/lib/admin-labels";
import type { ChatReportSummary } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import type { Paged } from "@/lib/types";

const tone: Record<number, Tone> = { 1: "warning", 2: "primary", 3: "success", 4: "muted" };

export default async function ChatReportsPage({ searchParams }: PageProps<"/admin/yazisma-sikayetleri">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const status = one(sp.status);

  const result = await authed<Paged<ChatReportSummary>>("/api/admin/conversation-reports", {
    query: { page, pageSize: 20, status },
  });
  const data = result.ok ? result.data : null;

  const chips = [
    { label: "Hamısı", href: adminHref("/yazisma-sikayetleri"), value: undefined },
    ...Object.entries(reportStatus).map(([v, label]) => ({
      label,
      href: adminHref(`/yazisma-sikayetleri?status=${v}`),
      value: v,
    })),
  ];

  return (
    <div className="space-y-5">
      <PageHead
        title="Yazışma şikayətləri"
        hint="İstifadəçilərin söhbətlər barədə göndərdiyi şikayətlər. Yalnız şikayət edilən söhbətə baxmaq mümkündür."
      />
      <Chips items={chips} current={status} />

      {!data || data.items.length === 0 ? (
        <Empty icon={MessagesSquare} text="Şikayət yoxdur" />
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((r) => (
              <li key={r.id}>
                <Link
                  href={adminHref(`/yazisma-sikayetleri/${r.id}`)}
                  className="flex items-center gap-3 rounded-xl border border-line bg-surface p-4 hover:border-primary"
                >
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-bold">{r.listingTitle}</span>
                    <span className="text-sm text-ink-2">
                      {chatReason[r.reason] ?? "—"} · {dateTime(r.createdAtUtc)}
                    </span>
                  </span>
                  <Pill tone={tone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>
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
              return adminHref(text ? `/yazisma-sikayetleri?${text}` : "/yazisma-sikayetleri");
            }}
          />
        </>
      )}
    </div>
  );
}
