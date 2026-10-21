import { Card, LoadProblem, PageHead } from "@/components/admin/ui";
import { ExpenseForm, ProfileForm, VoidExpenseButton } from "@/components/admin/FinanceForms";
import { MonthForm } from "@/components/admin/MonthForm";
import { adminHref } from "@/lib/admin-path";
import { getExpenses, getFinanceProfile, monthParam } from "@/lib/admin-finance";
import { bakuTodayIso, fmtMonth, fmtNumeric } from "@/lib/dates";
import { getRoles } from "@/lib/session";

const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const label: Record<number, string> = { 1: "Büdcə", 2: "Vergi", 3: "Digər xərc", 4: "Şəxsi xərc" };

/** The expenses of a month. Anyone with the finance section reads them; only the owner writes and voids. */
export default async function ExpensesPage({ searchParams }: PageProps<"/admin/maliyye/xercler">) {
  const today = bakuTodayIso();
  const month = monthParam((await searchParams).ay) ?? today.slice(0, 7);
  const [expenses, profile, roles] = await Promise.all([getExpenses(month), getFinanceProfile(), getRoles()]);
  if (!expenses.ok) return <LoadProblem what="Xərclər" status={expenses.status} />;
  const isOwner = roles.includes("SuperAdmin");

  const live = expenses.data.filter((e) => !e.isVoid);
  const total = (category: number) => live.filter((e) => e.category === category).reduce((s, e) => s + e.amount, 0);

  return (
    <div className="space-y-6">
      <PageHead
        title="Xərclər"
        hint={`${fmtMonth(month)}: büdcələr, vergilər, digər və şəxsi xərclər. İşçilərin maaşı “Kadr” bölməsindəki maaş cədvəlindən avtomatik götürülür.`}
        back={{ href: adminHref("/maliyye"), label: "Maliyyə" }}
        actions={<MonthForm value={month} />}
      />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {[1, 2, 3, 4].map((c) => (
          <div key={c} className="rounded-xl border border-line bg-surface p-4">
            <p className="text-sm text-ink-2">{label[c]}</p>
            <p className="mt-1 text-xl font-extrabold">{money.format(total(c))} AZN</p>
          </div>
        ))}
      </div>

      {isOwner ? (
        <ExpenseForm today={today} />
      ) : (
        <p className="rounded-xl border border-line bg-surface px-4 py-3 text-sm text-ink-2">
          Xərcləri yalnız rəhbər yazır və ləğv edir. Siz baxa bilərsiniz.
        </p>
      )}

      <Card title="Yazılmış xərclər">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[720px] text-left text-sm">
            <thead className="border-b border-line text-ink-2">
              <tr>
                <th className="px-4 py-3 font-semibold">Tarix</th>
                <th className="px-4 py-3 font-semibold">Növ</th>
                <th className="px-4 py-3 font-semibold">Nəyə görə</th>
                <th className="px-4 py-3 text-right font-semibold">Məbləğ</th>
                <th className="px-4 py-3 font-semibold">Yazan</th>
                {isOwner && <th className="px-4 py-3" />}
              </tr>
            </thead>
            <tbody>
              {expenses.data.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-ink-2">
                    Bu ay xərc yazılmayıb.
                  </td>
                </tr>
              )}
              {expenses.data.map((e) => (
                <tr key={e.id} className={`border-b border-line last:border-b-0 ${e.isVoid ? "text-ink-2" : ""}`}>
                  <td className="whitespace-nowrap px-4 py-2.5">{fmtNumeric(e.day, false)}</td>
                  <td className="px-4 py-2.5">{label[e.category] ?? e.category}</td>
                  <td className="px-4 py-2.5">
                    <span className={e.isVoid ? "line-through" : ""}>{e.title}</span>
                    {e.note && <span className="block text-xs text-ink-2">{e.note}</span>}
                    {e.isVoid && (
                      <span className="block text-xs font-semibold text-danger">
                        Ləğv edilib{e.voidedByName ? `: ${e.voidedByName}` : ""} — {e.voidReason}
                      </span>
                    )}
                  </td>
                  <td className={`px-4 py-2.5 text-right font-semibold ${e.isVoid ? "line-through" : ""}`}>{money.format(e.amount)}</td>
                  <td className="px-4 py-2.5">{e.createdByName ?? "—"}</td>
                  {isOwner && <td className="px-4 py-2.5 text-right">{!e.isVoid && <VoidExpenseButton id={e.id} />}</td>}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {profile.ok &&
        (isOwner ? (
          <ProfileForm
            key={JSON.stringify(profile.data)}
            initial={{
              fullName: profile.data.fullName,
              voen: profile.data.voen ?? "",
              phone: profile.data.phone ?? "",
              email: profile.data.email ?? "",
              address: profile.data.address ?? "",
            }}
          />
        ) : null)}
    </div>
  );
}
