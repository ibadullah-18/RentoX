import { SettingsForm } from "@/components/admin/SettingsForm";
import { Card, LoadProblem, PageHead, dateTime } from "@/components/admin/ui";
import { authed } from "@/lib/authed";

type Values = { listingLifetimeDays: number; maxListingPhotos: number; minListingPhotos?: number; maxNewListingsPerDay?: number };
type History = { id: string; values: Values; changedByUserId: string; changedAtUtc: string }[];

/** The adjustable settings of the site, and every change made to them. */
export default async function SettingsPage() {
  const [current, history] = await Promise.all([
    authed<Values>("/api/admin/settings"),
    authed<History>("/api/admin/settings/history", { query: { take: 20 } }),
  ]);
  if (!current.ok) return <LoadProblem what="Ayarlar" status={current.status} />;

  return (
    <div className="space-y-6">
      <PageHead
        title="Ayarlar"
        hint="Saytın işini dəyişən dəyərlər. Dəyişiklik kodu yeniləmədən qüvvəyə minir və hər dəyişiklik tarixçədə saxlanır."
      />

      <SettingsForm key={JSON.stringify(current.data)} current={current.data} />

      <Card title="Dəyişikliklərin tarixçəsi">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] text-left text-sm">
            <thead className="border-b border-line text-ink-2">
              <tr>
                <th className="px-4 py-3 font-semibold">Vaxt</th>
                <th className="px-4 py-3 font-semibold">Kim</th>
                <th className="px-4 py-3 text-right font-semibold">Elanın müddəti</th>
                <th className="px-4 py-3 text-right font-semibold">Şəkil sayı</th>
                <th className="px-4 py-3 text-right font-semibold">Ən az şəkil</th>
                <th className="px-4 py-3 text-right font-semibold">Gündəlik limit</th>
              </tr>
            </thead>
            <tbody>
              {(!history.ok || history.data.length === 0) && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-ink-2">
                    Hələ dəyişiklik edilməyib: ilkin dəyərlər keçərlidir (30 gün, elanda ən çoxu 15 şəkil).
                  </td>
                </tr>
              )}
              {history.ok &&
                history.data.map((h) => (
                  <tr key={h.id} className="border-b border-line last:border-b-0">
                    <td className="whitespace-nowrap px-4 py-2.5">{dateTime(h.changedAtUtc)}</td>
                    <td className="px-4 py-2.5 font-mono text-xs">{h.changedByUserId.slice(0, 8)}</td>
                    <td className="px-4 py-2.5 text-right">{h.values.listingLifetimeDays} gün</td>
                    <td className="px-4 py-2.5 text-right">{h.values.maxListingPhotos ?? 15}</td>
                    <td className="px-4 py-2.5 text-right">{h.values.minListingPhotos ?? 0}</td>
                    <td className="px-4 py-2.5 text-right">{h.values.maxNewListingsPerDay ? h.values.maxNewListingsPerDay : "—"}</td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
