import Link from "next/link";
import { LifeBuoy, PartyPopper } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { Chips, Empty, LoadProblem, PageHead, Pill, type Tone } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { priorityLabel } from "@/lib/admin-labels";
import type { AdminTicket } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import { ago, urgency, urgencyClass } from "@/lib/report-view";
import { ticketCategory, ticketStatus } from "@/lib/support";
import type { Paged } from "@/lib/types";

const statusTone: Record<number, Tone> = { 1: "primary", 2: "warning", 3: "success", 4: "muted" };
const priorityTone: Record<number, Tone> = { 1: "muted", 2: "muted", 3: "warning", 4: "danger" };

/** The support queue: open requests first, the ones waiting for an answer from us at the top. */
export default async function AdminSupportPage({ searchParams }: PageProps<"/admin/destek">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  // What an admin opens this page for is the open work, so that is the default.
  const view = one(sp.status) ?? "aktiv";

  const query =
    view === "aktiv" ? { open: "true" } : view === "all" ? {} : { status: view };

  const [result, openCount, newCount, progressCount] = await Promise.all([
    authed<Paged<AdminTicket>>("/api/admin/support/tickets", { query: { page, pageSize: 20, ...query } }),
    count({ open: "true" }),
    count({ status: 1 }),
    count({ status: 2 }),
  ]);

  const link = (status: string, p?: number) => {
    const q = new URLSearchParams();
    if (status !== "aktiv") q.set("status", status);
    if (p && p > 1) q.set("page", String(p));
    const text = q.toString();
    return adminHref(text ? `/destek?${text}` : "/destek");
  };

  const chips = [
    { label: `Aktiv${openCount ? ` (${openCount})` : ""}`, href: link("aktiv"), value: "aktiv" },
    { label: `Açıq${newCount ? ` (${newCount})` : ""}`, href: link("1"), value: "1" },
    { label: `İşlənir${progressCount ? ` (${progressCount})` : ""}`, href: link("2"), value: "2" },
    { label: "Həll olundu", href: link("3"), value: "3" },
    { label: "Bağlı", href: link("4"), value: "4" },
    { label: "Hamısı", href: link("all"), value: "all" },
  ];

  const data = result.ok ? result.data : null;

  return (
    <div className="space-y-5">
      <PageHead
        title="Dəstək müraciətləri"
        hint="Cavab gözləyənlər yuxarıdadır; onların içində ən təcili, sonra ən çox gözləyən birincidir."
      />
      <Chips items={chips} current={view} />

      {!result.ok ? (
        <LoadProblem what="Müraciətlər" status={result.status} />
      ) : !data || data.items.length === 0 ? (
        view === "aktiv" ? (
          <Empty icon={PartyPopper} text="Aktiv müraciət yoxdur. Hamısına cavab verilib." />
        ) : (
          <Empty icon={LifeBuoy} text="Bu vəziyyətdə müraciət yoxdur" />
        )
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((t) => {
              const open = t.status === 1 || t.status === 2;
              const waitingForUs = open && t.lastMessageIsAdmin === false;
              const last = t.lastMessageAtUtc ?? t.updatedAtUtc;
              return (
                <li key={t.id}>
                  <Link href={adminHref(`/destek/${t.id}`)} className="group flex gap-3 rounded-xl border border-line bg-surface p-3 hover:border-primary">
                    <Avatar name={t.userName} />
                    <span className="min-w-0 flex-1 space-y-1">
                      <span className="flex items-start justify-between gap-2">
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-extrabold">{t.userName ?? "İstifadəçi"}</span>
                          <span className="block truncate font-semibold">{t.subject}</span>
                        </span>
                        <span className="flex shrink-0 flex-col items-end gap-1">
                          <Pill tone={statusTone[t.status]}>{ticketStatus[t.status] ?? "—"}</Pill>
                          {waitingForUs && <span className="rounded-full bg-accent px-2 py-0.5 text-[11px] font-bold text-white">Cavab gözləyir</span>}
                        </span>
                      </span>

                      {t.lastMessagePreview && (
                        <span className="block truncate text-sm text-ink-2">
                          <span className="font-semibold">{t.lastMessageIsAdmin ? "Dəstək: " : `${(t.userName ?? "İstifadəçi").split(" ")[0]}: `}</span>
                          {t.lastMessagePreview}
                        </span>
                      )}

                      <span className="flex flex-wrap items-center gap-1.5 text-xs">
                        <span className="rounded-md bg-bg px-1.5 py-0.5 font-semibold text-ink-2">{ticketCategory[t.category] ?? "Digər"}</span>
                        {t.priority >= 3 && <Pill tone={priorityTone[t.priority]}>{priorityLabel[t.priority]}</Pill>}
                        {(t.messageCount ?? 0) > 1 && <span className="text-ink-2">{t.messageCount} mesaj</span>}
                        <span className={waitingForUs ? urgencyClass[urgency(last)] : "text-ink-2"}>
                          · {ago(last)}
                          {waitingForUs && urgency(last) === "late" && " — uzun gözləyir"}
                        </span>
                      </span>
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(p) => link(view, p)} />
        </>
      )}
    </div>
  );
}

/** How many requests are in a state (one tiny request: only the total is read). */
async function count(query: Record<string, string | number>): Promise<number> {
  const res = await authed<Paged<unknown>>("/api/admin/support/tickets", { query: { page: 1, pageSize: 1, ...query } });
  return res.ok ? res.data.totalCount : 0;
}
