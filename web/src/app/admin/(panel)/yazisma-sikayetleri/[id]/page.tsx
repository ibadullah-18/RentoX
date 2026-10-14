import Link from "next/link";
import { notFound } from "next/navigation";
import { Card, PageHead, Pill, dateTime, shortId, type Tone } from "@/components/admin/ui";
import { ReportReview } from "@/components/admin/TicketAdmin";
import { adminHref } from "@/lib/admin-path";
import { chatReason, reportStatus } from "@/lib/admin-labels";
import type { ChatReportDetails } from "@/lib/admin-types";
import { authed } from "@/lib/authed";

const tone: Record<number, Tone> = { 1: "warning", 2: "primary", 3: "success", 4: "muted" };

export default async function ChatReportPage({ params }: PageProps<"/admin/yazisma-sikayetleri/[id]">) {
  const { id } = await params;
  const result = await authed<ChatReportDetails>(`/api/admin/conversation-reports/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const d = result.data;
  const r = d.report;

  const role = (userId: string) => (userId === d.sellerId ? "Satıcı" : userId === d.buyerId ? "Alıcı" : "İstifadəçi");

  return (
    <div className="space-y-5">
      <PageHead
        title={d.listingTitle}
        back={{ href: adminHref("/yazisma-sikayetleri"), label: "Yazışma şikayətləri" }}
        hint={`${chatReason[r.reason] ?? "—"} · ${dateTime(r.createdAtUtc)}`}
        actions={<Pill tone={tone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>}
      />

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
        <Card title="Yazışma (son 100 mesaj)">
          <div className="max-h-[640px] space-y-2 overflow-y-auto bg-bg p-4">
            {d.messages.length === 0 && <p className="py-6 text-center text-sm text-ink-2">Mesaj yoxdur</p>}
            {d.messages.map((m) => {
              const seller = m.senderId === d.sellerId;
              const evidence = m.id === r.evidenceMessageId;
              return (
                <div key={m.id} className={`flex ${seller ? "justify-end" : "justify-start"}`}>
                  <div
                    className={`max-w-[80%] rounded-xl px-3 py-2 text-[14px] ${
                      seller ? "bg-primary text-white" : "border border-line bg-surface"
                    } ${evidence ? "ring-2 ring-warning" : ""}`}
                  >
                    <p className={`mb-0.5 text-[11px] font-bold ${seller ? "text-white/80" : "text-ink-2"}`}>
                      {role(m.senderId)} · {dateTime(m.sentAtUtc)}
                      {evidence && " · sübut"}
                    </p>
                    {m.body && <p className="whitespace-pre-wrap break-words">{m.body}</p>}
                    {m.images.length > 0 && (
                      <div className="mt-1.5 flex flex-wrap gap-1.5">
                        {m.images.map((img) => (
                          <a key={img.id} href={`/bff/admin-report-image/${r.id}/${img.id}`} target="_blank" rel="noreferrer">
                            {/* eslint-disable-next-line @next/next/no-img-element */}
                            <img
                              src={`/bff/admin-report-image/${r.id}/${img.id}`}
                              alt=""
                              className="size-24 rounded-lg object-cover"
                            />
                          </a>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </Card>

        <div className="space-y-5">
          <Card title="Şikayət">
            <dl className="divide-y divide-line text-sm">
              <Row label="Şikayət edən" value={`${role(r.reporterId)} · ${shortId(r.reporterId)}`} />
              <Row label="Alıcı" value={shortId(d.buyerId)} />
              <Row label="Satıcı" value={shortId(d.sellerId)} />
              <Row label="Baxılıb" value={r.reviewedAtUtc ? dateTime(r.reviewedAtUtc) : "—"} />
            </dl>
            {r.details && <p className="whitespace-pre-line border-t border-line p-4 text-sm">{r.details}</p>}
            {r.resolutionNote && (
              <p className="border-t border-line p-4 text-sm">
                <span className="font-bold">Qərar: </span>
                {r.resolutionNote}
              </p>
            )}
            <div className="border-t border-line p-4">
              <Link href={`/listing/${d.listingId}`} className="text-sm font-bold text-primary hover:underline">
                Elana bax
              </Link>
            </div>
          </Card>

          <Card title="Qərar">
            <div className="p-4">
              <p className="mb-3 text-xs text-ink-2">
                Qərar yalnız şikayəti bağlayır; istifadəçini avtomatik bloklamır və mesajları silmir.
              </p>
              <ReportReview id={r.id} status={r.status} kind="chat" />
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 px-4 py-2.5">
      <dt className="text-ink-2">{label}</dt>
      <dd className="text-right font-semibold">{value}</dd>
    </div>
  );
}
