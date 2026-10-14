import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { History } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { TopUp } from "@/components/TopUp";
import { getTransactions, getWallet } from "@/lib/personal";
import { dateFmt, formatNumber } from "@/lib/format";
import { tx as mark, type Locale, type T } from "@/lib/i18n/locales";
import { getSession } from "@/lib/session";
import type { WalletTransaction } from "@/lib/types";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Pul kisəsi"), robots: { index: false } };
}

const typeLabel: Record<number, string> = {
  1: mark("Balans artırıldı"),
  2: mark("Elanın aktivləşdirilməsi"),
  3: mark("Elanın irəli çəkilməsi"),
  4: mark("Geri qaytarma"),
  5: mark("Bonus"),
  6: mark("Düzəliş"),
};

function dayLabel(date: Date, t: T, locale: Locale): string {
  const start = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  const days = Math.round((start(new Date()) - start(date)) / 86_400_000);
  if (days === 0) return t("Bu gün");
  if (days === 1) return t("Dünən");
  return dateFmt(locale, { day: "numeric", month: "long", year: "numeric" }).format(date);
}

export default async function WalletPage({ searchParams }: PageProps<"/pul-kisesi">) {
  const t = await getT();
  const locale = await getLocale();
  if (!(await getSession())) redirect("/giris?next=/pul-kisesi");

  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const [wallet, history] = await Promise.all([getWallet(), getTransactions(page)]);
  if (!wallet.ok && wallet.status === 401) redirect("/giris?next=/pul-kisesi");

  const items = history.ok ? history.data.items : [];
  const groups: { day: string; rows: WalletTransaction[] }[] = [];
  for (const tx of items) {
    const day = dayLabel(new Date(tx.occurredAtUtc), t, locale);
    const last = groups.at(-1);
    if (last?.day === day) last.rows.push(tx);
    else groups.push({ day, rows: [tx] });
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      <h1 className="text-2xl font-extrabold">{t("Pul kisəsi")}</h1>

      <section className="rounded-2xl bg-primary p-6 text-white">
        <p className="text-sm font-semibold opacity-80">{t("Balansınız")}</p>
        <p className="mt-1 text-4xl font-extrabold">
          {wallet.ok ? formatNumber(wallet.data.balance, locale) : "—"} {t("AZN")}</p>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-extrabold">{t("Balansı artır")}</h2>
        <TopUp />
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-extrabold">{t("Tarixçə")}</h2>
        {groups.length === 0 ? (
          <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-12 text-center">
            <History className="size-10 text-primary" aria-hidden />
            <p className="font-bold">{t("Hələ əməliyyat yoxdur")}</p>
            <p className="text-sm text-ink-2">{t("Balans artırdıqda və ya ödəniş etdikdə burada görünəcək.")}</p>
          </div>
        ) : (
          groups.map((g) => (
            <div key={g.day}>
              <h3 className="mb-2 mt-4 text-sm font-bold text-ink-2">{g.day}</h3>
              <ul className="space-y-2">
                {g.rows.map((tx) => {
                  const credit = tx.direction === 1;
                  return (
                    <li
                      key={tx.id}
                      className="flex items-center justify-between gap-3 rounded-xl border border-line bg-surface px-4 py-3"
                    >
                      <div className="min-w-0">
                        <p className="truncate font-bold">{typeLabel[tx.type] ? t(typeLabel[tx.type]) : t("Əməliyyat")}</p>
                        <p className="text-xs text-ink-2">{dateFmt(locale, { hour: "2-digit", minute: "2-digit" }).format(new Date(tx.occurredAtUtc))}</p>
                      </div>
                      <div className="text-right">
                        <p className={`font-extrabold ${credit ? "text-success" : "text-danger"}`}>
                          {credit ? "+" : "−"}
                          {formatNumber(tx.amount, locale)} {t("AZN")}</p>
                        <p className="text-xs text-ink-2">{t("Qalıq:")} {formatNumber(tx.balanceAfter, locale)} {t("AZN")}</p>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))
        )}
        {history.ok && (
          <Pagination
            page={history.data.page}
            totalPages={history.data.totalPages}
            hrefFor={(p) => (p === 1 ? "/pul-kisesi" : `/pul-kisesi?page=${p}`)}
          />
        )}
      </section>
    </div>
  );
}
