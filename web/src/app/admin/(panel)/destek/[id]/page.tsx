import Link from "next/link";
import { notFound } from "next/navigation";
import { Phone } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { TicketChat } from "@/components/admin/TicketChat";
import { TicketControls } from "@/components/admin/TicketControls";
import { Card, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import type { AdminTicket } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import { ago } from "@/lib/report-view";
import { getSections } from "@/lib/session";
import { ticketCategory, ticketStatus } from "@/lib/support";

const pushLabel: Record<number, string> = {
  1: "telefona göndərilməyi gözləyir",
  2: "telefona göndərilir",
  3: "telefona çatdırıldı",
  4: "telefona göndərilmədi (təkrar cəhd olunacaq)",
  5: "telefona göndərilə bilmədi",
};

const statusTone: Record<number, Tone> = { 1: "primary", 2: "warning", 3: "success", 4: "muted" };

/** One support request as a conversation: their words, our answers, and the person and the state on the side. */
export default async function AdminTicketPage({ params }: PageProps<"/admin/destek/[id]">) {
  const { id } = await params;
  const result = await authed<AdminTicket>(`/api/admin/support/tickets/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const t = result.data;
  const messages = [...(t.messages ?? [])].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
  const last = messages[messages.length - 1];

  const sections = await getSections();
  const canSeeUser = sections === null || sections.includes("users");

  return (
    <div className="space-y-5">
      <PageHead
        title={t.subject}
        hint={`${ticketCategory[t.category] ?? "Digər"} · açılıb ${ago(t.createdAtUtc)}${last ? ` · son mesaj ${ago(last.sentAtUtc)}` : ""}`}
        back={{ href: adminHref("/destek"), label: "Müraciətlər" }}
        actions={<Pill tone={statusTone[t.status]}>{ticketStatus[t.status] ?? "—"}</Pill>}
      />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_320px]">
        <TicketChat
          key={t.id}
          id={t.id}
          status={t.status}
          userName={t.userName}
          messages={messages.map((m) => ({
            id: m.id,
            isAdmin: m.isAdmin,
            body: m.body,
            sentAtUtc: m.sentAtUtc,
            senderName: m.senderName,
            images: m.images,
          }))}
        />

        <div className="space-y-5 xl:sticky xl:top-4 xl:self-start">
          <Card title="İstifadəçi">
            <div className="flex items-center gap-3 p-4">
              <Avatar name={t.userName} size="lg" />
              <div className="min-w-0">
                <p className="truncate font-extrabold">
                  {canSeeUser ? (
                    <Link href={adminHref(`/istifadeciler/${t.userId}`)} className="hover:text-primary hover:underline">
                      {t.userName ?? "İstifadəçi"}
                    </Link>
                  ) : (
                    (t.userName ?? "İstifadəçi")
                  )}
                </p>
                {t.userPhone && (
                  <a href={`tel:${t.userPhone}`} className="flex items-center gap-1 text-sm text-ink-2 hover:text-primary">
                    <Phone className="size-3.5" aria-hidden />
                    {t.userPhone}
                  </a>
                )}
                {(t.otherTickets ?? 0) > 0 && <p className="text-xs text-ink-2">Başqa {t.otherTickets} müraciəti var</p>}
              </div>
            </div>
          </Card>

          <Card title="Bildirişlər">
            {(t.notifications ?? []).length === 0 ? (
              <p className="p-4 text-sm text-ink-2">Bu müraciət üçün hələ bildiriş göndərilməyib. Cavab yazanda və ya status dəyişəndə istifadəçiyə bildiriş gedir.</p>
            ) : (
              <ul className="divide-y divide-line">
                {(t.notifications ?? []).map((n) => (
                  <li key={`${n.type}-${n.createdAtUtc}`}>
                    <details className="group">
                      <summary className="flex cursor-pointer list-none items-start gap-2 px-4 py-2.5 text-sm hover:bg-primary-soft">
                        <span className="min-w-0 flex-1 space-y-0.5">
                          <span className="block font-semibold">
                            {n.type === 7 ? "Cavab bildirişi" : "Status bildirişi"} · {dateTime(n.createdAtUtc)}
                          </span>
                          <span className={`block ${n.readAtUtc ? "text-success" : "text-ink-2"}`}>
                            {n.readAtUtc ? `Oxunub · ${dateTime(n.readAtUtc)}` : "Göndərilib, hələ oxunmayıb"}
                          </span>
                        </span>
                        <span className="mt-0.5 text-xs font-bold text-primary group-open:hidden">Ətraflı</span>
                        <span className="mt-0.5 hidden text-xs font-bold text-primary group-open:inline">Bağla</span>
                      </summary>
                      <dl className="space-y-2 border-t border-line bg-bg px-4 py-3 text-[13px]">
                        <div>
                          <dt className="font-bold text-ink-2">Bildirişin mətni</dt>
                          <dd>
                            <b>{n.title}</b>
                            {n.body ? <span className="block text-ink-2">{n.body}</span> : null}
                          </dd>
                        </div>
                        <div>
                          <dt className="font-bold text-ink-2">Tətbiqdə (sayt və mobil)</dt>
                          <dd>
                            Göndərildi: {dateTime(n.createdAtUtc)}. {n.readAtUtc ? `İstifadəçi oxudu: ${dateTime(n.readAtUtc)}.` : "İstifadəçi hələ oxumayıb."}
                          </dd>
                        </div>
                        <div>
                          <dt className="font-bold text-ink-2">Telefona push</dt>
                          <dd>
                            {n.pushStatus != null ? (
                              <>
                                {pushLabel[n.pushStatus] ?? "—"}
                                {(n.pushAttempts ?? 0) > 0 ? ` · cəhd sayı: ${n.pushAttempts}` : ""}
                                {n.pushSentAtUtc ? ` · çatdırılma: ${dateTime(n.pushSentAtUtc)}` : ""}
                                {n.pushError ? <span className="block text-danger">Xəta: {n.pushError}</span> : null}
                              </>
                            ) : (t.userDevices ?? 0) > 0 ? (
                              "Növbəyə qoyulmayıb: bildiriş yalnız tətbiqdə görünür."
                            ) : (
                              <>
                                Göndərilməyib, çünki bu istifadəçi mobil tətbiqə hələ daxil olmayıb və ya push icazəsi verməyib (telefon qeydiyyatsızdır).
                                Bildiriş sayt və tətbiq açılanda görünür.
                              </>
                            )}
                          </dd>
                        </div>
                      </dl>
                    </details>
                  </li>
                ))}
              </ul>
            )}
          </Card>

          <Card title="Müraciət">
            <TicketControls key={`${t.status}-${t.priority}`} id={t.id} status={t.status} priority={t.priority} />
            <dl className="divide-y divide-line border-t border-line text-sm">
              <div className="flex justify-between gap-4 px-4 py-2.5">
                <dt className="text-ink-2">Açılıb</dt>
                <dd className="text-right font-semibold">{dateTime(t.createdAtUtc)}</dd>
              </div>
              <div className="flex justify-between gap-4 px-4 py-2.5">
                <dt className="text-ink-2">Mesaj sayı</dt>
                <dd className="text-right font-semibold">{messages.length}</dd>
              </div>
              {t.resolvedAtUtc && (
                <div className="flex justify-between gap-4 px-4 py-2.5">
                  <dt className="text-ink-2">Həll olunub</dt>
                  <dd className="text-right font-semibold">{dateTime(t.resolvedAtUtc)}</dd>
                </div>
              )}
              {t.closedAtUtc && (
                <div className="flex justify-between gap-4 px-4 py-2.5">
                  <dt className="text-ink-2">Bağlanıb</dt>
                  <dd className="text-right font-semibold">{dateTime(t.closedAtUtc)}</dd>
                </div>
              )}
            </dl>
          </Card>
        </div>
      </div>
    </div>
  );
}
