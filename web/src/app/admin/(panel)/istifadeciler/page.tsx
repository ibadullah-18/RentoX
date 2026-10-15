import Link from "next/link";
import { ChevronRight, Download, Search, Users } from "lucide-react";
import { Chips, Empty, LoadProblem, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { userStatusLabel, type AdminUserItem } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import type { Paged } from "@/lib/types";

const tone: Record<number, Tone> = { 1: "success", 2: "warning", 3: "danger", 4: "muted" };
const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Everyone with an account: found by name or phone. */
export default async function UsersAdminPage({ searchParams }: PageProps<"/admin/istifadeciler">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const status = one(sp.status);
  const q = (one(sp.q) ?? "").trim();

  const result = await authed<Paged<AdminUserItem>>("/api/admin/users", {
    query: { page, pageSize: 20, status, q: q || undefined },
  });
  const data = result.ok ? result.data : null;

  const link = (over: { status?: string | null; page?: number }) => {
    const query = new URLSearchParams();
    const s = over.status === undefined ? status : over.status;
    if (s) query.set("status", s);
    if (q) query.set("q", q);
    if (over.page && over.page > 1) query.set("page", String(over.page));
    const text = query.toString();
    return adminHref(text ? `/istifadeciler?${text}` : "/istifadeciler");
  };

  const exportQuery = `?${new URLSearchParams({ ...(status ? { status } : {}), ...(q ? { q } : {}) })}`;

  const chips = [
    { label: "Hamısı", href: link({ status: null }), value: undefined },
    ...Object.entries(userStatusLabel).map(([v, label]) => ({ label, href: link({ status: v }), value: v })),
  ];

  return (
    <div className="space-y-5">
      <PageHead
        title="İstifadəçilər"
        hint="Ad və ya telefon ilə axtarın. CSV düyməsi cari axtarış və süzgəcin nəticəsini fayla yazır (ən çox 2000 sətir)."
        actions={
          <a
            href={`/bff/admin-export/users${exportQuery}`}
            className="no-print flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-bold hover:border-primary hover:text-primary"
          >
            <Download className="size-4" aria-hidden />
            CSV
          </a>
        }
      />

      <form action={adminHref("/istifadeciler")} className="no-print flex gap-2">
        {status && <input type="hidden" name="status" value={status} />}
        <div className="flex h-10 min-w-0 flex-1 items-center gap-2 rounded-lg border border-line bg-surface px-3 focus-within:border-primary">
          <Search className="size-4 shrink-0 text-muted" aria-hidden />
          <input name="q" defaultValue={q} placeholder="Ad və ya telefon" className="min-w-0 flex-1 bg-transparent text-[15px] outline-none" />
        </div>
        <button type="submit" className="h-10 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press">
          Axtar
        </button>
      </form>

      <Chips items={chips} current={status} />

      {!result.ok ? (
        <LoadProblem what="İstifadəçilər" status={result.status} />
      ) : !data || data.items.length === 0 ? (
        <Empty icon={Users} text="İstifadəçi tapılmadı" />
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((u) => (
              <li key={u.id}>
                <Link
                  href={adminHref(`/istifadeciler/${u.id}`)}
                  className="flex items-center gap-3 rounded-xl border border-line bg-surface p-3.5 hover:border-primary"
                >
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-2">
                      <span className="truncate font-bold">{u.fullName}</span>
                      <Pill tone={tone[u.status]}>{userStatusLabel[u.status] ?? u.status}</Pill>
                    </span>
                    <span className="block truncate text-sm text-ink-2">
                      {u.phone ?? "—"} · {u.activeListingCount} aktiv / {u.listingCount} elan · balans {money.format(u.balance)} {u.currency}
                    </span>
                    <span className="block text-xs text-ink-2">Qeydiyyat: {dateTime(u.registeredAtUtc)}</span>
                  </span>
                  <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
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
