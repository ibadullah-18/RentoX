import { fmtDate, fmtTime } from "@/lib/dates";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ExternalLink } from "lucide-react";
import { ReportDecision } from "@/components/admin/ReportDecision";
import { Outcome, PersonCard, ReasonCard } from "@/components/admin/ReportParts";
import { Card, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import type { AdminUserDetails } from "@/lib/admin-catalog";
import { reportStatus } from "@/lib/admin-labels";
import type { ChatReportDetails } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import { ago, reportStatusTone } from "@/lib/report-view";
import { getSections } from "@/lib/session";

const timeOnly = (iso: string) => fmtTime(iso);
const dayOnly = (iso: string) => fmtDate(iso, "az");

/** A report about a conversation: the messages with who wrote them, the evidence marked, and the decision. */
export default async function ChatReportPage({ params }: PageProps<"/admin/yazisma-sikayetleri/[id]">) {
  const { id } = await params;
  const result = await authed<ChatReportDetails>(`/api/admin/conversation-reports/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const d = result.data;
  const r = d.report;

  const sections = await getSections();
  const has = (section: string) => sections === null || sections.includes(section);

  // The person reported is the other one in the conversation.
  const reportedId = r.reporterId === d.buyerId ? d.sellerId : d.buyerId;
  const reportedName = reportedId === d.sellerId ? d.sellerName : d.buyerName;
  const reporterName = d.reporterName ?? (r.reporterId === d.buyerId ? d.buyerName : d.sellerName);
  const reportedHistory = has("users") ? await authed<AdminUserDetails>(`/api/admin/users/${reportedId}`) : null;

  const role = (userId: string) => (userId === d.sellerId ? "Satıcı" : "Alıcı");
  const nameOf = (userId: string) => (userId === d.sellerId ? d.sellerName : d.buyerName) ?? role(userId);

  // A date line is shown above the first message of each day.
  const startsDay = d.messages.map((m, i) => i === 0 || dayOnly(m.sentAtUtc) !== dayOnly(d.messages[i - 1].sentAtUtc));

  return (
    <div className="space-y-5">
      <PageHead
        title={d.listingTitle}
        hint={`Yazışma haqqında şikayət · ${ago(r.createdAtUtc)}`}
        back={{ href: adminHref("/sikayetler?tur=yazisma"), label: "Şikayətlər" }}
        actions={<Pill tone={reportStatusTone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>}
      />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_380px]">
        <Card title="Yazışma">
          <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-2.5 text-xs">
            <span className="rounded-md bg-primary-soft px-2 py-1 font-bold text-primary">Şikayət edən: {reporterName ?? "—"}</span>
            <span className="rounded-md bg-danger/10 px-2 py-1 font-bold text-danger">Şikayət edilən: {reportedName ?? "—"}</span>
            {r.evidenceMessageId && (
              <a href="#evidence" className="ml-auto font-bold text-primary hover:underline">
                Sübut mesajına get
              </a>
            )}
            <Link href={`/listing/${d.listingId}`} target="_blank" className="inline-flex items-center gap-1 font-bold text-primary hover:underline">
              Elana bax <ExternalLink className="size-3" aria-hidden />
            </Link>
          </div>

          <div className="max-h-[680px] space-y-2 overflow-y-auto bg-bg p-4">
            {d.messages.length === 0 && <p className="py-8 text-center text-sm text-ink-2">Mesaj yoxdur</p>}
            {d.messages.length >= 100 && <p className="pb-2 text-center text-xs text-ink-2">Yalnız son 100 mesaj göstərilir.</p>}
            {d.messages.map((m, i) => {
              const mine = m.senderId === r.reporterId; // the reporter's messages on the right
              const evidence = m.id === r.evidenceMessageId;
              return (
                <div key={m.id}>
                  {startsDay[i] && (
                    <p className="my-3 text-center text-[11px] font-bold uppercase tracking-wide text-ink-2">{dayOnly(m.sentAtUtc)}</p>
                  )}
                  <div id={evidence ? "evidence" : undefined} className={`flex scroll-mt-24 ${mine ? "justify-end" : "justify-start"}`}>
                    <div
                      className={`max-w-[82%] rounded-2xl px-3.5 py-2 text-[14px] ${
                        mine ? "rounded-br-md bg-primary text-white" : "rounded-bl-md border border-line bg-surface"
                      } ${evidence ? "ring-2 ring-warning ring-offset-2 ring-offset-bg" : ""}`}
                    >
                      <p className={`mb-0.5 text-[11px] font-bold ${mine ? "text-white/80" : "text-ink-2"}`}>
                        {nameOf(m.senderId)} ({role(m.senderId).toLowerCase()}) · {timeOnly(m.sentAtUtc)}
                        {evidence && " · ŞİKAYƏT EDİLƏN MESAJ"}
                      </p>
                      {m.body && <p className="whitespace-pre-wrap break-words">{m.body}</p>}
                      {m.images.length > 0 && (
                        <div className="mt-1.5 flex flex-wrap gap-1.5">
                          {m.images.map((img) => (
                            <a key={img.id} href={`/bff/admin-report-image/${r.id}/${img.id}`} target="_blank" rel="noreferrer">
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img src={`/bff/admin-report-image/${r.id}/${img.id}`} alt="" className="size-24 rounded-lg object-cover" />
                            </a>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </Card>

        <div className="space-y-5 xl:sticky xl:top-4 xl:self-start">
          <ReasonCard
            kind="chat"
            reason={r.reason}
            details={r.details}
            reporterName={reporterName}
            reporterHref={has("users") ? adminHref(`/istifadeciler/${r.reporterId}`) : null}
            createdAtUtc={r.createdAtUtc}
            status={r.status}
            reviewedAtUtc={r.reviewedAtUtc}
          />

          <Card title="Qərar">
            {r.status === 3 || r.status === 4 ? (
              <Outcome status={r.status} note={r.resolutionNote} />
            ) : (
              <ReportDecision
                key={r.id}
                kind="chat"
                id={r.id}
                status={r.status}
                noun="yazışma"
                person={has("users") && reportedName ? { id: reportedId, name: reportedName } : null}
                others={[]}
                listUrl={adminHref("/sikayetler?tur=yazisma")}
              />
            )}
          </Card>

          <PersonCard
            title="Şikayət edilən şəxs"
            name={reportedName}
            userHref={has("users") ? adminHref(`/istifadeciler/${reportedId}`) : null}
            details={reportedHistory && reportedHistory.ok ? reportedHistory.data : null}
          />
        </div>
      </div>
    </div>
  );
}
