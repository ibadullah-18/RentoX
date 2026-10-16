import { Search, X } from "lucide-react";
import Link from "next/link";
import { getT } from "@/lib/i18n/server";

const field = "h-10 w-full rounded-lg border border-line bg-surface px-3 text-sm outline-none focus:border-primary";

/** A search and price range over the listings of one store. A plain form: the address carries the choice. */
export async function StoreFilter({ q, min, max, active }: { q?: string; min?: string; max?: string; active: boolean }) {
  const t = await getT();
  return (
    <form method="get" className="flex flex-wrap items-center gap-2 rounded-xl border border-line bg-surface p-2.5">
      <label className="relative min-w-40 flex-1">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-ink-2" aria-hidden />
        <input name="q" defaultValue={q} maxLength={80} placeholder={t("Mağazada axtar")} className={`${field} pl-9`} />
      </label>
      <input name="min" defaultValue={min} inputMode="decimal" placeholder={t("Qiymət: dan")} aria-label={t("Qiymət: dan")} className={`${field} w-28`} />
      <input name="max" defaultValue={max} inputMode="decimal" placeholder={t("Qiymət: dək")} aria-label={t("Qiymət: dək")} className={`${field} w-28`} />
      <button type="submit" className="h-10 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press">
        {t("Tətbiq et")}
      </button>
      {active && (
        <Link href="?" scroll={false} aria-label={t("Təmizlə")} className="flex size-10 items-center justify-center rounded-lg border border-line text-ink-2 hover:border-primary">
          <X className="size-4" aria-hidden />
        </Link>
      )}
    </form>
  );
}
