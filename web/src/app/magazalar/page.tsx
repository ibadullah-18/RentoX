import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { Search, Store } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { StoreTile } from "@/components/StoreTile";
import { getStores } from "@/lib/api";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getT();
  return {
    title: t("Mağazalar"),
    description: t("RentoX-dakı mağazalar: elanlara və məlumata baxın, izləyin."),
  };
}

export default async function StoresPage({ searchParams }: PageProps<"/magazalar">) {
  const t = await getT();
  const sp = await searchParams;
  const q = (Array.isArray(sp.q) ? sp.q[0] : sp.q)?.trim() ?? "";
  const raw = Array.isArray(sp.page) ? sp.page[0] : sp.page;
  const page = Math.max(1, Number.parseInt(raw ?? "1", 10) || 1);

  const result = await getStores(q || undefined, page);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-extrabold">{t("Mağazalar")}</h1>
        <form action="/magazalar" role="search" className="flex h-11 items-center overflow-hidden rounded-xl border border-line bg-surface focus-within:border-primary sm:w-80">
          <Search className="ml-4 size-5 shrink-0 text-muted" aria-hidden />
          <input
            name="q"
            defaultValue={q}
            placeholder={t("Mağaza axtarın")}
            aria-label={t("Mağaza axtarın")}
            className="h-full min-w-0 flex-1 bg-transparent px-3 outline-none placeholder:text-muted"
          />
        </form>
      </div>

      {result.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-16 text-center">
          <Store className="size-12 text-primary" aria-hidden />
          <h2 className="text-lg font-extrabold">{t("Mağaza tapılmadı")}</h2>
          <p className="text-ink-2">{t("Başqa söz yazmağa çalışın.")}</p>
        </div>
      ) : (
        <>
          <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {result.items.map((s) => (
              <li key={s.storeId}>
                <StoreTile store={s} />
              </li>
            ))}
          </ul>
          <Pagination
            page={result.page}
            totalPages={result.totalPages}
            hrefFor={(p) => {
              const params = new URLSearchParams();
              if (q) params.set("q", q);
              if (p > 1) params.set("page", String(p));
              const text = params.toString();
              return text ? `/magazalar?${text}` : "/magazalar";
            }}
          />
        </>
      )}
    </div>
  );
}
