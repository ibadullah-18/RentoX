import { fmtNumeric } from "@/lib/dates";
import Link from "next/link";
import { FileText, Info, Receipt } from "lucide-react";
import { Card, PageHead } from "@/components/admin/ui";
import { PrintButton } from "@/components/admin/PrintButton";
import { walletDirection, walletType } from "@/lib/admin-labels";
import { getDashboard } from "@/lib/admin";
import { adminHref } from "@/lib/admin-path";
import { getFinanceSummary, type FinancePeriod } from "@/lib/admin-finance";

const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const count = new Intl.NumberFormat("az");

/** The money side of the site: wallet balances and what moved in and out. */
export default async function FinancePage() {
  const [result, summary] = await Promise.all([getDashboard(), getFinanceSummary()]);
  if (!result.ok) {
    return <p className="rounded-xl bg-danger/10 px-4 py-3 font-semibold text-danger">Məlumat yüklənmədi.</p>;
  }
  const d = result.data;
  const currencies = [...new Set([...d.walletBalances.map((w) => w.currency), ...d.walletActivity.map((w) => w.currency)])];

  return (
    <div className="space-y-6">
      <PageHead
        title="Maliyyə icmalı"
        hint="İstifadəçilərin pul kisələri və platforma xidmətləri (aktivləşdirmə, irəli çəkmə, VIP). Elanın kirayə qiyməti platformanın gəliri deyil."
        actions={<PrintButton />}
      />

      {summary.ok && (
        <section className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-lg font-extrabold">Qazanc</h2>
            <div className="no-print flex gap-2">
              <Link href={adminHref("/maliyye/xercler")} className="flex h-9 items-center gap-2 rounded-lg border border-line bg-surface px-3 text-sm font-semibold hover:border-primary">
                <Receipt className="size-4" aria-hidden />
                Xərclər
              </Link>
              <Link href={adminHref("/maliyye/akt")} className="flex h-9 items-center gap-2 rounded-lg bg-primary px-3 text-sm font-bold text-white hover:bg-primary-press">
                <FileText className="size-4" aria-hidden />
                Aylıq akt
              </Link>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {summary.data.periods.map((p) => (
              <PeriodCard key={p.name} p={p} currency={summary.data.currency} />
            ))}
          </div>
          <p className="text-xs text-ink-2">
            Qazanc yalnız xidmətlərə (elan haqqı, irəli çəkmə, VIP, VIP mağaza) xərclənən puldur, geri qaytarılanlar çıxılır. Pul kisəsinə artırılan məbləğ
            qazanc deyil. İşçi maaşı aylıq hesablanır, ona görə xalis yalnız ay və ümumi üçün göstərilir.
          </p>
        </section>
      )}

      <div className="flex gap-3 rounded-xl border border-warning/40 bg-warning-soft p-4 text-sm">
        <Info className="mt-0.5 size-5 shrink-0 text-warning" aria-hidden />
        <p>
          Balans artırmaları sınaq (demo) rejimində də yarana bilər, ona görə “Daxilolma” cəmlərini bankdan daxil olan real pul kimi
          oxumayın. Bank əməliyyatları ilə tutuşdurma, xərclər və tarix aralığı üzrə hesabat hələ yoxdur.
        </p>
      </div>

      {currencies.length === 0 && <p className="text-ink-2">Hələ əməliyyat yoxdur.</p>}

      {currencies.map((cur) => {
        const balance = d.walletBalances.find((w) => w.currency === cur);
        const rows = d.walletActivity.filter((w) => w.currency === cur);
        const total = (direction: number, key: "totalAmount" | "last7DaysAmount" | "last30DaysAmount") =>
          rows.filter((r) => r.direction === direction).reduce((sum, r) => sum + r[key], 0);

        return (
          <section key={cur} className="space-y-3">
            <h2 className="text-lg font-extrabold">{cur}</h2>

            <div className="grid gap-3 sm:grid-cols-3">
              <Stat label="İstifadəçi balansları cəmi" value={`${money.format(balance?.totalBalance ?? 0)} ${cur}`} hint={`${count.format(balance?.walletCount ?? 0)} pul kisəsi`} />
              <Stat label="Daxilolma (cəmi)" value={`${money.format(total(1, "totalAmount"))} ${cur}`} hint={`30 gün: ${money.format(total(1, "last30DaysAmount"))}`} />
              <Stat label="Çıxılma (cəmi)" value={`${money.format(total(2, "totalAmount"))} ${cur}`} hint={`30 gün: ${money.format(total(2, "last30DaysAmount"))}`} />
            </div>

            <Card>
              <div className="overflow-x-auto">
                <table className="w-full min-w-[640px] text-left text-sm">
                  <thead className="border-b border-line text-ink-2">
                    <tr>
                      <th className="px-4 py-3 font-semibold">Növ</th>
                      <th className="px-4 py-3 font-semibold">İstiqamət</th>
                      <th className="px-4 py-3 text-right font-semibold">Say</th>
                      <th className="px-4 py-3 text-right font-semibold">Cəmi</th>
                      <th className="px-4 py-3 text-right font-semibold">7 gün</th>
                      <th className="px-4 py-3 text-right font-semibold">30 gün</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rows.map((r) => (
                      <tr key={`${r.direction}-${r.type}`} className="border-b border-line last:border-b-0">
                        <td className="px-4 py-3 font-semibold">{walletType[r.type] ?? `Növ ${r.type}`}</td>
                        <td className={`px-4 py-3 ${r.direction === 1 ? "text-success" : "text-danger"}`}>
                          {walletDirection[r.direction] ?? r.direction}
                        </td>
                        <td className="px-4 py-3 text-right">{count.format(r.transactionCount)}</td>
                        <td className="px-4 py-3 text-right font-semibold">{money.format(r.totalAmount)}</td>
                        <td className="px-4 py-3 text-right">{money.format(r.last7DaysAmount)}</td>
                        <td className="px-4 py-3 text-right">{money.format(r.last30DaysAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Card>
          </section>
        );
      })}

      <p className="text-xs text-ink-2">Yenilənib: {fmtNumeric(d.generatedAtUtc)}</p>
    </div>
  );
}

const periodTitle: Record<string, string> = { today: "Bu gün", week: "Bu həftə", month: "Bu ay", all: "Ümumi" };

function PeriodCard({ p, currency }: { p: FinancePeriod; currency: string }) {
  return (
    <div className="rounded-xl border border-line bg-surface p-4">
      <p className="text-sm font-semibold text-ink-2">{periodTitle[p.name] ?? p.name}</p>
      <p className="mt-1 text-2xl font-extrabold">
        {money.format(p.income)} <span className="text-sm font-bold text-ink-2">{currency}</span>
      </p>
      <p className="text-xs text-ink-2">qazanc{p.refunds > 0 ? ` (geri qaytarılan ${money.format(p.refunds)})` : ""}</p>
      <dl className="mt-3 space-y-1 border-t border-line pt-3 text-sm">
        <div className="flex justify-between gap-2">
          <dt className="text-ink-2">Xərc</dt>
          <dd className="font-semibold">{money.format(p.businessExpenses)}</dd>
        </div>
        {p.staffCost !== null && (
          <div className="flex justify-between gap-2">
            <dt className="text-ink-2">İşçilər</dt>
            <dd className="font-semibold">{money.format(p.staffCost)}</dd>
          </div>
        )}
        {p.netIncome !== null && (
          <div className="flex justify-between gap-2">
            <dt className="font-bold">Xalis</dt>
            <dd className={`font-extrabold ${p.netIncome < 0 ? "text-danger" : "text-success"}`}>{money.format(p.netIncome)}</dd>
          </div>
        )}
      </dl>
    </div>
  );
}

function Stat({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-xl border border-line bg-surface p-4">
      <p className="text-sm text-ink-2">{label}</p>
      <p className="mt-1 text-2xl font-extrabold">{value}</p>
      {hint && <p className="mt-0.5 text-xs text-ink-2">{hint}</p>}
    </div>
  );
}
