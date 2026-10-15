import Link from "next/link";
import { Download, Search, Store } from "lucide-react";
import { Chips, Empty, LoadProblem, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { storeStatusLabel } from "@/lib/admin-labels";
import type { AdminStoreItem } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import type { Paged } from "@/lib/types";

const tone: Record<number, Tone> = { 1: "primary", 2: "warning", 3: "success", 4: "danger", 5: "danger", 6: "muted" };

/** Every store, whatever its status. By default the ones waiting for a decision. */
export default async function StoresAdminPage({ searchParams }: PageProps<"/admin/magazalar">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const status = one(sp.status) ?? "2";
  const q = (one(sp.q) ?? "").trim();

  const result = await authed<Paged<AdminStoreItem>>("/api/admin/stores", {
    query: { page, pageSize: 20, status: status === "all" ? undefined : status, q: q || undefined },
  });
  const data = result.ok ? result.data : null;
  const exportQuery = `?${new URLSearchParams({ status, ...(q ? { q } : {}) })}`;

  const link = (over: { status?: string; page?: number }) => {
    const query = new URLSearchParams();
    const s = over.status ?? status;
    if (s !== "2") query.set("status", s);
    if (q) query.set("q", q);
    if (over.page && over.page > 1) query.set("page", String(over.page));
    const text = query.toString();
    return adminHref(text ? `/magazalar?${text}` : "/magazalar");
  };

  const chips = [
    { label: "Yoxlamada", href: link({ status: "2" }), value: "2" },
    { label: "Hamısı", href: link({ status: "all" }), value: "all" },
    ...Object.entries(storeStatusLabel)
      .filter(([v]) => v !== "2")
      .map(([v, label]) => ({ label, href: link({ status: v }), value: v })),
  ];

  return (
    <div className="space-y-5">
      <PageHead
        title="Mağazalar"
        hint="Mağaza adı, sahibin adı və ya telefonu ilə axtarın; statusa görə süzün. CSV düyməsi cari süzgəcin nəticəsini fayla yazır."
        actions={
          <a
            href={`/bff/admin-export/stores${exportQuery}`}
            className="no-print flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-bold hover:border-primary hover:text-primary"
          >
            <Download className="size-4" aria-hidden />
            CSV
          </a>
        }
      />

      <form action={adminHref("/magazalar")} className="no-print flex gap-2">
        {status !== "2" && <input type="hidden" name="status" value={status} />}
        <div className="flex h-10 min-w-0 flex-1 items-center gap-2 rounded-lg border border-line bg-surface px-3 focus-within:border-primary">
          <Search className="size-4 shrink-0 text-muted" aria-hidden />
          <input name="q" defaultValue={q} placeholder="Ad, sahib və ya telefon" className="min-w-0 flex-1 bg-transparent text-[15px] outline-none" />
        </div>
        <button type="submit" className="h-10 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press">
          Axtar
        </button>
      </form>

      <Chips items={chips} current={status} />

      {!result.ok ? (
        <LoadProblem what="Mağazalar" status={result.status} />
      ) : !data || data.items.length === 0 ? (
        <Empty icon={Store} text={q ? "Heç nə tapılmadı" : "Bu vəziyyətdə mağaza yoxdur"} />
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((s) => (
              <li key={s.id}>
                <Link
                  href={adminHref(`/magazalar/${s.id}`)}
                  className="flex items-center gap-3 rounded-xl border border-line bg-surface p-3.5 hover:border-primary"
                >
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-2">
                      <span className="truncate font-bold">{s.name}</span>
                      <Pill tone={tone[s.status]}>{storeStatusLabel[s.status] ?? s.status}</Pill>
                    </span>
                    <span className="block truncate text-sm text-ink-2">
                      {s.ownerName}
                      {s.ownerPhone ? ` · ${s.ownerPhone}` : ""} · {dateTime(s.createdAtUtc)}
                    </span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(p) => link({ page: p })} />
        </>
      )}
    </div>
  );
}
