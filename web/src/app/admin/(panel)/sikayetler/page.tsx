import Link from "next/link";
import { Flag, MessagesSquare, PartyPopper } from "lucide-react";
import { ReportRow, type ReportRowData } from "@/components/admin/ReportRow";
import { Chips, Empty, LoadProblem, PageHead } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { getReportCounts } from "@/lib/admin-reports";
import type { ChatReportSummary, ReportSummary } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import type { Paged } from "@/lib/types";

/** All reports in one place: the work waiting first, the longest waiting at the top. */
export default async function ReportsPage({ searchParams }: PageProps<"/admin/sikayetler">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const kind = one(sp.tur) === "yazisma" ? "yazisma" : "elan";
  // The queue is what an admin opens this page for, so it is the default; "all" is a choice.
  const status = one(sp.status) ?? "1";
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);

  const path = kind === "yazisma" ? "/api/admin/conversation-reports" : "/api/admin/content-reports";
  const [counts, result] = await Promise.all([
    getReportCounts(),
    authed<Paged<ReportSummary | ChatReportSummary>>(path, {
      query: { page, pageSize: 20, status: status === "all" ? undefined : status },
    }),
  ]);

  const link = (over: { tur?: string; status?: string; page?: number }) => {
    const q = new URLSearchParams();
    const t = over.tur ?? kind;
    const s = over.status ?? status;
    if (t === "yazisma") q.set("tur", "yazisma");
    if (s !== "1") q.set("status", s);
    if (over.page && over.page > 1) q.set("page", String(over.page));
    const text = q.toString();
    return adminHref(text ? `/sikayetler?${text}` : "/sikayetler");
  };

  const open = kind === "yazisma" ? counts.chat : counts.content;
  const chips = [
    { label: `Gözləyir${open.waiting ? ` (${open.waiting})` : ""}`, href: link({ status: "1", page: 1 }), value: "1" },
    { label: `Baxılır${open.reviewing ? ` (${open.reviewing})` : ""}`, href: link({ status: "2", page: 1 }), value: "2" },
    { label: "Həll olundu", href: link({ status: "3", page: 1 }), value: "3" },
    { label: "Rədd edildi", href: link({ status: "4", page: 1 }), value: "4" },
    { label: "Hamısı", href: link({ status: "all", page: 1 }), value: "all" },
  ];

  const rows: ReportRowData[] = result.ok
    ? result.data.items.map((i) =>
        kind === "yazisma"
          ? (() => {
              const r = i as ChatReportSummary;
              return {
                kind: "chat" as const,
                id: r.id,
                title: r.listingTitle,
                reason: r.reason,
                status: r.status,
                createdAtUtc: r.createdAtUtc,
                reporterName: r.reporterName,
                otherName: r.otherUserName,
                openOnSame: r.openOnConversation,
              };
            })()
          : (() => {
              const r = i as ReportSummary;
              return {
                kind: r.targetType === 1 ? ("listing" as const) : ("store" as const),
                id: r.id,
                title: r.targetTitle ?? "—",
                reason: r.reason,
                status: r.status,
                createdAtUtc: r.createdAtUtc,
                reporterName: r.reporterName,
                otherName: r.ownerName,
                openOnSame: r.openOnTarget,
                coverImageId: r.coverImageId,
              };
            })(),
      )
    : [];

  const tabs = [
    { id: "elan", label: "Elan və mağaza", icon: Flag, n: counts.content.waiting },
    { id: "yazisma", label: "Yazışmalar", icon: MessagesSquare, n: counts.chat.waiting },
  ];

  return (
    <div className="space-y-5">
      <PageHead
        title="Şikayətlər"
        hint="Gözləyənlər üstə, ən çox gözləyən birincidir. Şikayəti açın, nəyin pozulduğuna baxın və qərar verin."
      />

      <div role="tablist" aria-label="Şikayətin növü" className="grid grid-cols-2 gap-2 rounded-xl border border-line bg-surface p-1.5">
        {tabs.map((t) => (
          <Link
            key={t.id}
            href={link({ tur: t.id, status: "1", page: 1 })}
            role="tab"
            aria-selected={kind === t.id}
            className={`flex h-11 items-center justify-center gap-2 rounded-lg text-sm font-bold ${
              kind === t.id ? "bg-primary text-white" : "text-ink-2 hover:bg-primary-soft hover:text-primary"
            }`}
          >
            <t.icon className="size-4" aria-hidden />
            {t.label}
            {t.n > 0 && (
              <span className={`rounded-full px-2 py-0.5 text-xs ${kind === t.id ? "bg-white/25" : "bg-accent text-white"}`}>{t.n}</span>
            )}
          </Link>
        ))}
      </div>

      <Chips items={chips} current={status} />

      {!result.ok ? (
        <LoadProblem what="Şikayətlər" status={result.status} />
      ) : rows.length === 0 ? (
        status === "1" ? (
          <Empty icon={PartyPopper} text="Gözləyən şikayət yoxdur. Hər şey baxılıb." />
        ) : (
          <Empty icon={Flag} text="Bu vəziyyətdə şikayət yoxdur" />
        )
      ) : (
        <>
          <ul className="space-y-2">
            {rows.map((r) => (
              <ReportRow key={r.id} r={r} />
            ))}
          </ul>
          <Pagination page={result.data.page} totalPages={result.data.totalPages} hrefFor={(p) => link({ page: p })} />
        </>
      )}
    </div>
  );
}
