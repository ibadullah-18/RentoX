import Link from "next/link";
import { Card, Pill, dateTime } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { reportReason, chatReason, reportStatus } from "@/lib/admin-labels";
import type { AdminUserDetails } from "@/lib/admin-catalog";
import { ago, chatReasonTone, contentReasonTone, reportStatusTone, urgency, urgencyClass } from "@/lib/report-view";

/** Why it was reported and what the reporter wrote. */
export function ReasonCard({
  kind,
  reason,
  details,
  reporterName,
  reporterHref,
  createdAtUtc,
  status,
  reviewedAtUtc,
  openOnSame = 0,
}: {
  kind: "content" | "chat";
  reason: number;
  details?: string | null;
  reporterName?: string | null;
  reporterHref?: string | null;
  createdAtUtc: string;
  status: number;
  reviewedAtUtc?: string | null;
  openOnSame?: number;
}) {
  const open = status === 1 || status === 2;
  const label = (kind === "chat" ? chatReason : reportReason)[reason] ?? "—";
  const tone = (kind === "chat" ? chatReasonTone : contentReasonTone)[reason] ?? "muted";

  return (
    <Card title="Şikayət">
      <div className="space-y-3 p-4">
        <div className="flex flex-wrap items-center gap-2">
          <Pill tone={tone}>{label}</Pill>
          {open && openOnSame > 1 && <Pill tone="danger">{openOnSame} açıq şikayət: təkrar</Pill>}
        </div>

        {details ? (
          <blockquote className="whitespace-pre-line rounded-lg border-l-4 border-primary bg-bg px-4 py-3 text-[15px]">{details}</blockquote>
        ) : (
          <p className="text-sm text-ink-2">Şikayət edən əlavə mətn yazmayıb.</p>
        )}

        <dl className="divide-y divide-line rounded-lg border border-line text-sm">
          <div className="flex justify-between gap-4 px-3 py-2">
            <dt className="text-ink-2">Şikayət edən</dt>
            <dd className="text-right font-semibold">
              {reporterHref ? (
                <Link href={reporterHref} className="text-primary hover:underline">
                  {reporterName ?? "İstifadəçi"}
                </Link>
              ) : (
                (reporterName ?? "İstifadəçi")
              )}
            </dd>
          </div>
          <div className="flex justify-between gap-4 px-3 py-2">
            <dt className="text-ink-2">Göndərilib</dt>
            <dd className="text-right font-semibold">
              {dateTime(createdAtUtc)} <span className={open ? urgencyClass[urgency(createdAtUtc)] : "text-ink-2"}>({ago(createdAtUtc)})</span>
            </dd>
          </div>
          {reviewedAtUtc && (
            <div className="flex justify-between gap-4 px-3 py-2">
              <dt className="text-ink-2">Qərar tarixi</dt>
              <dd className="text-right font-semibold">{dateTime(reviewedAtUtc)}</dd>
            </div>
          )}
        </dl>
      </div>
    </Card>
  );
}

/** The final answer on a closed report. */
export function Outcome({ status, note }: { status: number; note?: string | null }) {
  const upheld = status === 3;
  return (
    <div className={`m-4 space-y-1 rounded-xl border-2 p-4 ${upheld ? "border-danger bg-danger/5" : "border-success bg-success-soft"}`}>
      <p className="font-extrabold">{upheld ? "Şikayət əsaslı sayıldı" : "Şikayət əsassız sayıldı"}</p>
      {note && <p className="whitespace-pre-line text-sm">{note}</p>}
      <p className="text-xs text-ink-2">Bağlanmış şikayəti dəyişmək olmaz.</p>
    </div>
  );
}

/** The record of the person the report is about: how many times they were reported and how those ended. */
export function PersonCard({
  title,
  name,
  userHref,
  details,
}: {
  title: string;
  name?: string | null;
  userHref?: string | null;
  details?: AdminUserDetails | null;
}) {
  const reports = details?.reports;
  return (
    <Card title={title}>
      <div className="space-y-3 p-4 text-sm">
        <p className="font-bold">
          {userHref ? (
            <Link href={userHref} className="text-primary hover:underline">
              {name ?? "İstifadəçi"}
            </Link>
          ) : (
            (name ?? "İstifadəçi")
          )}
        </p>
        {reports ? (
          <>
            <div className="grid grid-cols-3 gap-px overflow-hidden rounded-lg bg-line text-center">
              {[
                { label: "Şikayət", n: reports.total, cls: "" },
                { label: "Təsdiqlənib", n: reports.upheld, cls: reports.upheld > 0 ? "text-danger" : "" },
                { label: "Rədd edilib", n: reports.dismissed, cls: "" },
              ].map((x) => (
                <div key={x.label} className="bg-surface px-2 py-2">
                  <p className={`text-lg font-extrabold ${x.cls}`}>{x.n}</p>
                  <p className="text-[11px] text-ink-2">{x.label}</p>
                </div>
              ))}
            </div>
            {reports.upheld >= 2 && (
              <p className="rounded-lg bg-danger/10 px-3 py-2 text-xs font-semibold text-danger">
                Bu şəxsə qarşı {reports.upheld} şikayət əvvəl də təsdiqlənib: ağır tədbir düşünülə bilər.
              </p>
            )}
          </>
        ) : (
          <p className="text-xs text-ink-2">Şikayət tarixçəsini görmək üçün «İstifadəçilər» bölməsinə icazə lazımdır.</p>
        )}
      </div>
    </Card>
  );
}

export type OtherReport = { id: string; reason: number; status: number; createdAtUtc: string; reporterName?: string | null };

/** The other reports on the same thing. */
export function OtherReports({ items, noun }: { items: OtherReport[]; noun: string }) {
  if (items.length === 0) return null;
  return (
    <Card title={`Eyni ${noun} üzrə digər şikayətlər (${items.length})`}>
      <ul className="divide-y divide-line">
        {items.map((r) => (
          <li key={r.id}>
            <Link href={adminHref(`/sikayetler/${r.id}`)} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold">{reportReason[r.reason] ?? "—"}</span>
                <span className="block truncate text-xs text-ink-2">
                  {r.reporterName ?? "İstifadəçi"} · {ago(r.createdAtUtc)}
                </span>
              </span>
              <Pill tone={reportStatusTone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>
            </Link>
          </li>
        ))}
      </ul>
    </Card>
  );
}
