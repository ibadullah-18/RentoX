import { Card, LoadProblem, PageHead, dateTime, shortId } from "@/components/admin/ui";
import { TariffForm } from "@/components/admin/TariffForm";
import type { TariffInput } from "@/app/admin/(panel)/tariff-actions";
import { authed } from "@/lib/authed";

type History = { id: string; values: TariffInput & Record<string, number>; changedByUserId: string; changedAtUtc: string }[];

const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** The prices of the paid services, and every change made to them. */
export default async function TariffsPage() {
  const [current, history] = await Promise.all([
    authed<TariffInput>("/api/admin/tariffs"),
    authed<History>("/api/admin/tariffs/history", { query: { take: 20 } }),
  ]);
  if (!current.ok) return <LoadProblem what="Tariflər" status={current.status} />;

  return (
    <div className="space-y-6">
      <PageHead
        title="Tariflər"
        hint="Elan və xidmət qiymətləri. Dəyişiklik kodu yeniləmədən qüvvəyə minir və hər dəyişiklik tarixçədə saxlanır."
      />

      <TariffForm key={JSON.stringify(current.data)} current={current.data} />

      <Card title="Dəyişikliklərin tarixçəsi">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] text-left text-sm">
            <thead className="border-b border-line text-ink-2">
              <tr>
                <th className="px-4 py-3 font-semibold">Vaxt</th>
                <th className="px-4 py-3 font-semibold">Kim</th>
                <th className="px-4 py-3 text-right font-semibold">Pulsuz</th>
                <th className="px-4 py-3 text-right font-semibold">Aktivləşmə</th>
                <th className="px-4 py-3 text-right font-semibold">Yuxarı</th>
                <th className="px-4 py-3 text-right font-semibold">VIP</th>
                <th className="px-4 py-3 text-right font-semibold">VIP gün</th>
              </tr>
            </thead>
            <tbody>
              {(!history.ok || history.data.length === 0) && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-ink-2">
                    Hələ dəyişiklik edilməyib: ilkin qiymətlər keçərlidir.
                  </td>
                </tr>
              )}
              {history.ok &&
                history.data.map((h) => (
                  <tr key={h.id} className="border-b border-line last:border-b-0">
                    <td className="whitespace-nowrap px-4 py-2.5">{dateTime(h.changedAtUtc)}</td>
                    <td className="px-4 py-2.5 font-mono text-xs">{shortId(h.changedByUserId)}</td>
                    <td className="px-4 py-2.5 text-right">{h.values.freeActiveListingLimit}</td>
                    <td className="px-4 py-2.5 text-right">{money.format(h.values.activationFee)}</td>
                    <td className="px-4 py-2.5 text-right">{money.format(h.values.bumpPrice)}</td>
                    <td className="px-4 py-2.5 text-right">{money.format(h.values.vipPrice)}</td>
                    <td className="px-4 py-2.5 text-right">{h.values.vipDurationDays}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
