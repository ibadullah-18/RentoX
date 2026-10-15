import Link from "next/link";
import { notFound } from "next/navigation";
import { Avatar } from "@/components/admin/Avatar";
import { PayLineForm, ProfileForm, VoidPayLineButton } from "@/components/admin/HrForms";
import { Card, LoadProblem, PageHead, dateTime } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { MONTHS, money, payKind, shiftMonth, thisMonth, type PayrollSheet, type StaffProfile } from "@/lib/admin-hr";
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
  const { year, month, key } = thisMonth((await searchParams).ay);

  const [profile, sheet] = await Promise.all([
    authed<StaffProfile>(`/api/admin/hr/staff/${encodeURIComponent(id)}`),
    authed<PayrollSheet>(`/api/admin/hr/payroll/${encodeURIComponent(id)}`, { query: { month: key } }),
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

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
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
                <ul className="divide-y divide-line border-t border-line">
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
    </div>
  );
}
