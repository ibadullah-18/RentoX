import Link from "next/link";
import { notFound } from "next/navigation";
import { Avatar } from "@/components/admin/Avatar";
import { PayLineForm, ProfileForm, VoidPayLineButton } from "@/components/admin/HrForms";
import { Card, LoadProblem, PageHead, dateTime } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { MONTHS, money, payKind, shiftMonth, thisMonth, type PayrollHistory, type PayrollSheet, type StaffProfile } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

function Figure({ label, value, tone = "" }: { label: string; value: number; tone?: string }) {
  return (
    <div className="rounded-xl border border-line bg-bg px-4 py-3">
      <p className="text-xs font-semibold text-ink-2">{label}</p>
      <p className={`text-lg font-extrabold ${tone}`}>{money(value)} AZN</p>
    </div>
  );
}

/** One staff member: the switch for the panel, the hours, the pay and every line of it month by month. */
export default async function KadrPersonPage({ params, searchParams }: PageProps<"/admin/kadr/isciler/[id]">) {
  if (!(await getRoles()).includes("SuperAdmin")) notFound();
  const { id } = await params;
  const query = await searchParams;
  const { year, month, key } = thisMonth(query.ay);
  const shown = [12, 24, 36].includes(Number(query.aylar)) ? Number(query.aylar) : 12;

  const [profile, sheet, history] = await Promise.all([
    authed<StaffProfile>(`/api/admin/hr/staff/${encodeURIComponent(id)}`),
    authed<PayrollSheet>(`/api/admin/hr/payroll/${encodeURIComponent(id)}`, { query: { month: key } }),
    authed<PayrollHistory>(`/api/admin/hr/payroll/${encodeURIComponent(id)}/history`, { query: { months: shown } }),
  ]);
  if (!profile.ok) {
    if (profile.status === 404) notFound();
    return <LoadProblem what="İşçi" status={profile.status} />;
  }
  const p = profile.data;

  return (
    <div className="space-y-5">
      <PageHead
        title={p.fullName}
        hint={`${p.phone ?? "—"} · burada maaşı, panelə giriş icazəsini, iş saatlarını və maaş sətirlərini idarə edirsiniz.`}
        back={{ href: adminHref("/kadr/isciler"), label: "İşçilər" }}
        actions={<Avatar name={p.fullName} size="lg" />}
      />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <Card title="Profil və giriş">
          <ProfileForm key={JSON.stringify(p)} profile={p} />
        </Card>

        <Card title={`Maaş · ${MONTHS[month - 1]} ${year}`}>
          <div className="flex items-center justify-between gap-2 border-b border-line px-4 py-2 text-sm">
            <Link href={adminHref(`/kadr/isciler/${id}?ay=${shiftMonth(year, month, -1)}`)} className="font-semibold text-primary hover:underline">
              ‹ Əvvəlki ay
            </Link>
            <Link href={adminHref(`/kadr/isciler/${id}?ay=${shiftMonth(year, month, 1)}`)} className="font-semibold text-primary hover:underline">
              Sonrakı ay ›
            </Link>
          </div>

          {sheet.ok ? (
            <>
              <div className="grid grid-cols-2 gap-3 p-4">
                <Figure label="Əsas maaş" value={sheet.data.baseInQepik} />
                <Figure label="Mükafat" value={sheet.data.bonusInQepik} tone="text-success" />
                <Figure label="Kəsinti" value={sheet.data.deductionsInQepik} tone="text-danger" />
                <Figure label="Avans" value={sheet.data.advancesInQepik} tone="text-warning" />
                <Figure label="Ödənilib" value={sheet.data.paymentsInQepik} tone="text-primary" />
                <Figure label="Qalıb (ödəniləcək)" value={sheet.data.remainingInQepik} tone="text-primary" />
              </div>

              {sheet.data.lines.length > 0 && (
                <ul className="max-h-[26rem] divide-y divide-line overflow-y-auto border-t border-line">
                  {sheet.data.lines.map((l) => (
                    <li key={l.id} className={`flex items-start gap-3 px-4 py-2.5 text-sm ${l.isVoid ? "opacity-50" : ""}`}>
                      <span className="min-w-0 flex-1">
                        <span className={`font-bold ${payKind[l.kind]?.tone}`}>
                          {payKind[l.kind]?.label} {payKind[l.kind]?.sign}
                          {money(l.amountInQepik)} AZN
                        </span>
                        {l.isVoid && <span className="ml-2 text-xs font-bold text-ink-2">(ləğv edilib)</span>}
                        {l.note && <span className="block text-ink-2">{l.note}</span>}
                        <span className="block text-xs text-ink-2">
                          {dateTime(l.createdAtUtc)} · {l.createdByName ?? "—"}
                        </span>
                      </span>
                      {!l.isVoid && <VoidPayLineButton id={l.id} />}
                    </li>
                  ))}
                </ul>
              )}

              <PayLineForm userId={id} month={key} remaining={sheet.data.remainingInQepik} />
            </>
          ) : (
            <div className="p-4">
              <LoadProblem what="Maaş" status={sheet.status} />
            </div>
          )}
        </Card>
      </div>

      <Card title="Pul tarixçəsi">
        {history.ok ? (
          <>
            <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-2 text-sm">
              <span className="font-semibold text-ink-2">Dövr:</span>
              {[12, 24, 36].map((n) => (
                <Link
                  key={n}
                  href={adminHref(`/kadr/isciler/${id}?ay=${key}&aylar=${n}`)}
                  className={`rounded-lg px-2.5 py-1 font-bold ${shown === n ? "bg-primary text-white" : "text-primary hover:bg-primary-soft"}`}
                >
                  {n} ay
                </Link>
              ))}
              <span className="text-xs text-ink-2">· {history.data.monthsShown} ay göstərilir</span>
            </div>

            <div className="grid grid-cols-2 gap-3 p-4 sm:grid-cols-3 xl:grid-cols-6">
              <Figure label="Əsas maaşlar" value={history.data.totalBaseInQepik} />
              <Figure label="Mükafatlar" value={history.data.totalBonusInQepik} tone="text-success" />
              <Figure label="Kəsintilər" value={history.data.totalDeductionsInQepik} tone="text-danger" />
              <Figure label="Avanslar" value={history.data.totalAdvancesInQepik} tone="text-warning" />
              <Figure label="Ödənilib" value={history.data.totalPaymentsInQepik} tone="text-primary" />
              <Figure
                label={history.data.totalRemainingInQepik < 0 ? "Artıq ödənilib" : "Qalıb (ödəniləcək)"}
                value={Math.abs(history.data.totalRemainingInQepik)}
                tone={history.data.totalRemainingInQepik < 0 ? "text-danger" : "text-primary"}
              />
            </div>

            {history.data.months.length > 0 ? (
              <div className="max-h-[28rem] overflow-auto border-t border-line">
                <table className="w-full min-w-[640px] text-sm">
                  <thead>
                    <tr className="sticky top-0 bg-bg text-left text-xs font-bold text-ink-2">
                      <th className="px-4 py-2">Ay</th>
                      <th className="px-3 py-2 text-right">Maaş</th>
                      <th className="px-3 py-2 text-right">Mükafat</th>
                      <th className="px-3 py-2 text-right">Kəsinti</th>
                      <th className="px-3 py-2 text-right">Avans</th>
                      <th className="px-3 py-2 text-right">Ödənilib</th>
                      <th className="px-4 py-2 text-right">Qalıb</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-line">
                    {history.data.months.map((m) => {
                      const [y, mm] = m.month.split("-").map(Number);
                      return (
                        <tr key={m.month} className={m.month.startsWith(key.slice(0, 7)) ? "bg-primary-soft/40" : ""}>
                          <td className="px-4 py-2 font-semibold">
                            <Link href={adminHref(`/kadr/isciler/${id}?ay=${m.month.slice(0, 7)}&aylar=${shown}`)} className="text-primary hover:underline">
                              {MONTHS[mm - 1]} {y}
                            </Link>
                            <span className="ml-2 text-xs font-normal text-ink-2">
                              {m.lineCount} sətir{m.voidedCount > 0 ? ` · ${m.voidedCount} ləğv` : ""}
                            </span>
                          </td>
                          <td className="px-3 py-2 text-right tabular-nums">{money(m.baseInQepik)}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-success">{money(m.bonusInQepik)}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-danger">{money(m.deductionsInQepik)}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-warning">{money(m.advancesInQepik)}</td>
                          <td className="px-3 py-2 text-right tabular-nums text-primary">{money(m.paymentsInQepik)}</td>
                          <td className={`px-4 py-2 text-right font-bold tabular-nums ${m.remainingInQepik < 0 ? "text-danger" : ""}`}>
                            {money(m.remainingInQepik)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="border-t border-line px-4 py-6 text-center text-sm text-ink-2">Bu dövrdə hələ pul hərəkəti yoxdur.</p>
            )}

            <div className="border-t border-line">
              <h3 className="px-4 pt-3 text-sm font-extrabold">Maaşın dəyişmə tarixçəsi</h3>
              {history.data.salaryChanges.length > 0 ? (
                <ul className="max-h-72 divide-y divide-line overflow-y-auto">
                  {history.data.salaryChanges.map((c) => (
                    <li key={`${c.effectiveFrom}-${c.changedAtUtc}`} className="flex flex-wrap items-center gap-x-3 px-4 py-2.5 text-sm">
                      <span className="font-bold">
                        {c.previousAmountInQepik == null ? "Başlanğıc maaş: " : `${money(c.previousAmountInQepik)} → `}
                        {money(c.amountInQepik)} AZN
                      </span>
                      {c.previousAmountInQepik != null && (
                        <span className={`text-xs font-bold ${c.amountInQepik >= c.previousAmountInQepik ? "text-success" : "text-danger"}`}>
                          {c.amountInQepik >= c.previousAmountInQepik ? "+" : "−"}
                          {money(Math.abs(c.amountInQepik - c.previousAmountInQepik))} AZN
                        </span>
                      )}
                      <span className="text-xs text-ink-2">
                        {MONTHS[Number(c.effectiveFrom.slice(5, 7)) - 1]} {c.effectiveFrom.slice(0, 4)} ayından · {dateTime(c.changedAtUtc)} · {c.changedByName ?? "—"}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="px-4 py-3 pb-4 text-sm text-ink-2">Maaş hələ dəyişdirilməyib.</p>
              )}
            </div>
          </>
        ) : (
          <div className="p-4">
            <LoadProblem what="Pul tarixçəsi" status={history.status} />
          </div>
        )}
      </Card>
    </div>
  );
}
