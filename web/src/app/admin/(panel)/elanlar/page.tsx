import Image from "next/image";
import Link from "next/link";
import { ClipboardList, ImageIcon, Search } from "lucide-react";
import { Chips, Empty, LoadProblem, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { listingStatusLabel } from "@/lib/admin-labels";
import type { AdminListingItem } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import { formatPrice, ownerImage } from "@/lib/format";
import type { Paged, RentalPeriodId } from "@/lib/types";

const tone: Record<number, Tone> = {
  1: "primary",
  2: "warning",
  3: "success",
  4: "danger",
  5: "muted",
  6: "muted",
  7: "muted",
  8: "warning",
};

/**
 * Every listing, whatever its status. By default the ones waiting for a
 * decision (what the day starts with); the chips and the search find the rest.
 */
export default async function ListingsAdminPage({ searchParams }: PageProps<"/admin/elanlar">) {
  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const status = one(sp.status) ?? "2";
  const q = (one(sp.q) ?? "").trim();

  const result = await authed<Paged<AdminListingItem>>("/api/admin/listings", {
    query: { page, pageSize: 20, status: status === "all" ? undefined : status, q: q || undefined },
  });
  const data = result.ok ? result.data : null;

  const link = (over: { status?: string; page?: number }) => {
    const query = new URLSearchParams();
    const s = over.status ?? status;
    if (s !== "2") query.set("status", s);
    if (q) query.set("q", q);
    if (over.page && over.page > 1) query.set("page", String(over.page));
    const text = query.toString();
    return adminHref(text ? `/elanlar?${text}` : "/elanlar");
  };

  const chips = [
    { label: "Yoxlamada", href: link({ status: "2" }), value: "2" },
    { label: "Hamısı", href: link({ status: "all" }), value: "all" },
    ...Object.entries(listingStatusLabel)
      .filter(([v]) => v !== "2")
      .map(([v, label]) => ({ label, href: link({ status: v }), value: v })),
  ];

  return (
    <div className="space-y-5">
      <PageHead title="Elanlar" hint="Başlıq, elan sahibinin adı və ya telefonu ilə axtarın; statusa görə süzün." />

      <form action={adminHref("/elanlar")} className="no-print flex gap-2">
        {status !== "2" && <input type="hidden" name="status" value={status} />}
        <div className="flex h-10 min-w-0 flex-1 items-center gap-2 rounded-lg border border-line bg-surface px-3 focus-within:border-primary">
          <Search className="size-4 shrink-0 text-muted" aria-hidden />
          <input
            name="q"
            defaultValue={q}
            placeholder="Başlıq, ad və ya telefon"
            className="min-w-0 flex-1 bg-transparent text-[15px] outline-none"
          />
        </div>
        <button type="submit" className="h-10 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press">
          Axtar
        </button>
      </form>

      <Chips items={chips} current={status} />

      {!result.ok ? (
        <LoadProblem what="Elanlar" status={result.status} />
      ) : !data || data.items.length === 0 ? (
        <Empty icon={ClipboardList} text={q ? "Heç nə tapılmadı" : "Bu vəziyyətdə elan yoxdur"} />
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((l) => (
              <li key={l.id}>
                <Link
                  href={adminHref(`/elanlar/${l.id}`)}
                  className="flex gap-3 rounded-xl border border-line bg-surface p-3 hover:border-primary"
                >
                  <span className="relative size-16 shrink-0 overflow-hidden rounded-lg bg-primary-soft">
                    {l.coverImageId ? (
                      <Image
                        src={ownerImage(`/api/listing-images/${l.coverImageId}`)!}
                        alt=""
                        fill
                        unoptimized
                        sizes="64px"
                        className="object-cover"
                      />
                    ) : (
                      <span className="flex size-full items-center justify-center text-primary">
                        <ImageIcon className="size-5" aria-hidden />
                      </span>
                    )}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="flex items-start justify-between gap-2">
                      <span className="truncate font-bold">{l.title}</span>
                      <Pill tone={tone[l.status]}>{listingStatusLabel[l.status] ?? l.status}</Pill>
                    </span>
                    <span className="block text-sm font-extrabold text-primary">
                      {formatPrice(l.price, l.currency, l.rentalPeriodUnit as RentalPeriodId)}
                    </span>
                    <span className="block truncate text-xs text-ink-2">
                      {l.ownerName}
                      {l.ownerPhone ? ` · ${l.ownerPhone}` : ""} · {l.imageCount} şəkil · {l.viewCount} baxış · {dateTime(l.createdAtUtc)}
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
