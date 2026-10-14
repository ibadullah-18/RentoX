import Link from "next/link";
import { CheckCircle2, Store } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { authed } from "@/lib/authed";
import type { MyStore, Paged } from "@/lib/types";

export default async function PendingStoresPage({ searchParams }: PageProps<"/admin/magazalar">) {
  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const result = await authed<Paged<MyStore>>("/api/admin/stores/pending", { query: { page, pageSize: 20 } });
  const data = result.ok ? result.data : null;

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-extrabold">Yoxlama gözləyən mağazalar</h1>
      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-16 text-center">
          <CheckCircle2 className="size-12 text-success" aria-hidden />
          <p className="font-bold">Gözləyən mağaza yoxdur</p>
        </div>
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((s) => (
              <li key={s.id}>
                <Link
                  href={`/admin/magazalar/${s.id}`}
                  className="flex items-center gap-4 rounded-xl border border-line bg-surface p-3 hover:border-primary"
                >
                  <span className="flex size-14 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-primary-soft text-primary">
                    {s.logoImageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img src={`/bff/store-image/${s.id}/logo`} alt="" className="size-full object-cover" />
                    ) : (
                      <Store className="size-6" aria-hidden />
                    )}
                  </span>
                  <span className="min-w-0">
                    <span className="block truncate font-bold">{s.name}</span>
                    <span className="line-clamp-1 text-sm text-ink-2">{s.description}</span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => (p === 1 ? "/admin/magazalar" : `/admin/magazalar?page=${p}`)}
          />
        </>
      )}
    </div>
  );
}
