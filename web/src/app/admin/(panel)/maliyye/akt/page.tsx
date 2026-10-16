import { LoadProblem, PageHead } from "@/components/admin/ui";
import { MonthForm } from "@/components/admin/MonthForm";
import { PrintButton } from "@/components/admin/PrintButton";
import { adminHref } from "@/lib/admin-path";
import { getMonthlyReport, monthParam, type MonthlyReport } from "@/lib/admin-finance";
import { bakuTodayIso, fmtDate, fmtMonth, fmtNumeric } from "@/lib/dates";

const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const m = (n: number) => `${money.format(n)} AZN`;

const cell = "border border-black/30 px-3 py-1.5";

/**
 * The monthly report as a document to print or save as PDF: what the site earned from its services, less pay,
 * budgets, taxes and other costs, what is left for the owner, and the owner's signature block.
 */
export default async function MonthlyActPage({ searchParams }: PageProps<"/admin/maliyye/akt">) {
  const month = monthParam((await searchParams).ay) ?? bakuTodayIso().slice(0, 7);
  const result = await getMonthlyReport(month);
  if (!result.ok) return <LoadProblem what="Aylıq akt" status={result.status} />;
  const r = result.data;

  return (
    <div className="space-y-4">
      <PageHead
        title="Aylıq maliyyə aktı"
        hint="Çap et düyməsini basıb “PDF kimi saxla” seçə də bilərsiniz."
        back={{ href: adminHref("/maliyye"), label: "Maliyyə" }}
        actions={
          <>
            <MonthForm value={month} />
            <PrintButton />
          </>
        }
      />

      <article className="mx-auto max-w-3xl space-y-6 rounded-xl border border-line bg-white p-6 text-[14px] text-black md:p-10 print:border-0 print:p-0">
        <div className="space-y-1 text-center">
          <h2 className="text-xl font-extrabold uppercase">Aylıq maliyyə aktı</h2>
          <p className="font-semibold">{fmtMonth(r.month)}</p>
          <p className="text-xs text-black/60">Tərtib tarixi: {fmtNumeric(r.generatedAtUtc)}</p>
        </div>

        <Identity r={r} />

        <Section title="1. Gəlir (xidmətlərdən)">
          <table className="w-full border-collapse">
            <tbody>
              {r.incomeLines.map((l) => (
                <tr key={l.label}>
                  <td className={cell}>{l.label}</td>
                  <td className={`${cell} text-right`}>{m(l.amount)}</td>
                </tr>
              ))}
              <tr>
                <td className={`${cell} font-semibold`}>Xidmətlərdən ümumi daxilolma</td>
                <td className={`${cell} text-right font-semibold`}>{m(r.serviceIncome)}</td>
              </tr>
              <tr>
                <td className={cell}>Geri qaytarılanlar (çıxılır)</td>
                <td className={`${cell} text-right`}>− {m(r.refunds)}</td>
              </tr>
              <tr className="bg-black/5">
                <td className={`${cell} font-bold`}>Aylıq ümumi gəlir</td>
                <td className={`${cell} text-right font-bold`}>{m(r.income)}</td>
              </tr>
            </tbody>
          </table>
          <p className="mt-1 text-xs text-black/60">
            İstifadəçilərin pul kisəsinə artırdığı məbləğ gəlir sayılmır: yalnız xidmətə (elan, irəli çəkmə, VIP) xərclənən pul gəlirdir.
          </p>
        </Section>

        <Section title="2. İşçilərin əmək haqqı">
          <table className="w-full border-collapse">
            <thead>
              <tr className="bg-black/5 text-left">
                <th className={cell}>İşçi</th>
                <th className={`${cell} text-right`}>Maaş</th>
                <th className={`${cell} text-right`}>Mükafat</th>
                <th className={`${cell} text-right`}>Kəsinti</th>
                <th className={`${cell} text-right`}>Cəmi</th>
              </tr>
            </thead>
            <tbody>
              {r.staff.length === 0 && (
                <tr>
                  <td colSpan={5} className={`${cell} text-center text-black/60`}>
                    Bu ay işçi xərci yoxdur.
                  </td>
                </tr>
              )}
              {r.staff.map((s) => (
                <tr key={s.name}>
                  <td className={cell}>{s.name}</td>
                  <td className={`${cell} text-right`}>{money.format(s.salary)}</td>
                  <td className={`${cell} text-right`}>{money.format(s.bonus)}</td>
                  <td className={`${cell} text-right`}>{money.format(s.deductions)}</td>
                  <td className={`${cell} text-right`}>{money.format(s.cost)}</td>
                </tr>
              ))}
              <tr className="bg-black/5">
                <td colSpan={4} className={`${cell} font-bold`}>İşçilərə ayrılan cəmi</td>
                <td className={`${cell} text-right font-bold`}>{m(r.staffCost)}</td>
              </tr>
            </tbody>
          </table>
        </Section>

        <Lines title="3. Ayrılan büdcələr" lines={r.budgets} total={r.budgetsTotal} totalLabel="Büdcələr cəmi" />
        <Lines title="4. Vergiyə ödənilənlər" lines={r.taxes} total={r.taxesTotal} totalLabel="Vergilər cəmi" />
        <Lines title="5. Digər xərclər" lines={r.otherExpenses} total={r.otherTotal} totalLabel="Digər xərclər cəmi" />

        <Section title="6. Nəticə">
          <table className="w-full border-collapse">
            <tbody>
              <Row label="Aylıq ümumi gəlir" value={m(r.income)} />
              <Row label="İşçilərə ayrılan" value={`− ${m(r.staffCost)}`} />
              <Row label="Büdcələr" value={`− ${m(r.budgetsTotal)}`} />
              <Row label="Vergilər" value={`− ${m(r.taxesTotal)}`} />
              <Row label="Digər xərclər" value={`− ${m(r.otherTotal)}`} />
              <tr className="bg-black/10">
                <td className={`${cell} text-base font-extrabold`}>Mənə qalan xalis gəlir</td>
                <td className={`${cell} text-right text-base font-extrabold`}>{m(r.netIncome)}</td>
              </tr>
            </tbody>
          </table>
        </Section>

        {r.personal.length > 0 && (
          <Lines
            title="Şəxsi xərclər (şirkət xərci deyil)"
            lines={r.personal}
            total={r.personalTotal}
            totalLabel="Şəxsi xərclər cəmi"
            after={{ label: "Şəxsi xərclərdən sonra qalan", value: m(r.afterPersonal) }}
          />
        )}

        <footer className="grid gap-6 pt-6 sm:grid-cols-2">
          <div>
            <p className="font-semibold">{r.profile.fullName}</p>
            {r.profile.voen && <p>VÖEN: {r.profile.voen}</p>}
          </div>
          <div>
            <p className="mb-8 font-semibold">İmza:</p>
            <div className="border-b border-black" />
            <p className="mt-1 text-xs text-black/60">{r.profile.fullName}</p>
          </div>
        </footer>
        <p className="text-xs text-black/50">Tarix: {fmtDate(r.generatedAtUtc, "az")}</p>
      </article>
    </div>
  );
}

function Identity({ r }: { r: MonthlyReport }) {
  const p = r.profile;
  const rows: [string, string | null | undefined][] = [
    ["Soyad, ad, ata adı", p.fullName],
    ["VÖEN", p.voen],
    ["Telefon", p.phone],
    ["E-poçt", p.email],
    ["Ünvan", p.address],
  ];
  return (
    <dl className="grid gap-x-6 gap-y-1 sm:grid-cols-2">
      {rows
        .filter(([, v]) => v)
        .map(([k, v]) => (
          <div key={k} className="flex gap-2">
            <dt className="text-black/60">{k}:</dt>
            <dd className="font-semibold">{v}</dd>
          </div>
        ))}
    </dl>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-2 break-inside-avoid">
      <h3 className="font-extrabold">{title}</h3>
      {children}
    </section>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <tr>
      <td className={cell}>{label}</td>
      <td className={`${cell} text-right`}>{value}</td>
    </tr>
  );
}

function Lines({
  title,
  lines,
  total,
  totalLabel,
  after,
}: {
  title: string;
  lines: { day: string; title: string; amount: number }[];
  total: number;
  totalLabel: string;
  after?: { label: string; value: string };
}) {
  return (
    <Section title={title}>
      <table className="w-full border-collapse">
        <tbody>
          {lines.length === 0 && (
            <tr>
              <td colSpan={3} className={`${cell} text-center text-black/60`}>
                Bu ay yoxdur.
              </td>
            </tr>
          )}
          {lines.map((l, i) => (
            <tr key={`${l.day}-${i}`}>
              <td className={`${cell} w-28 whitespace-nowrap`}>{fmtNumeric(l.day, false)}</td>
              <td className={cell}>{l.title}</td>
              <td className={`${cell} text-right`}>{money.format(l.amount)}</td>
            </tr>
          ))}
          <tr className="bg-black/5">
            <td colSpan={2} className={`${cell} font-bold`}>{totalLabel}</td>
            <td className={`${cell} text-right font-bold`}>{m(total)}</td>
          </tr>
          {after && (
            <tr>
              <td colSpan={2} className={`${cell} font-bold`}>{after.label}</td>
              <td className={`${cell} text-right font-bold`}>{after.value}</td>
            </tr>
          )}
        </tbody>
      </table>
    </Section>
  );
}
