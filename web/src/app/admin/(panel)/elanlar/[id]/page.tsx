import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ExternalLink, Flag, Phone } from "lucide-react";
import { approveListing, rejectListing, takeDownListing } from "@/app/admin/(panel)/actions";
import { ModerationActions, TakeDownPanel } from "@/components/admin/AdminControls";
import { GiftPanel } from "@/components/admin/GiftPanel";
import { Avatar } from "@/components/admin/Avatar";
import { Card, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { authed } from "@/lib/authed";
import { getFlags } from "@/lib/banned-words";
import type { ModerationListing, ReportSummary } from "@/lib/admin-types";
import { reportReason, reportStatus } from "@/lib/admin-labels";
import { fieldValue, formatPrice, ownerImage } from "@/lib/format";
import { getRoles, getSections } from "@/lib/session";
import { statusLabel } from "@/lib/status";
import type { RentalPeriodId } from "@/lib/types";

const statusTone: Record<number, Tone> = { 2: "warning", 3: "success", 4: "danger", 6: "muted", 8: "warning" };

/** A listing as a moderator sees it: the photos and the text, who owns it, the reports on it and what can be done. */
export default async function ReviewListingPage({ params }: PageProps<"/admin/elanlar/[id]">) {
  const { id } = await params;
  const result = await authed<ModerationListing>(`/api/admin/listings/${encodeURIComponent(id)}`, {
    query: { language: "az" },
  });
  if (!result.ok) notFound();
  const l = result.data;
  const images = [...l.images].sort((a, b) => a.displayOrder - b.displayOrder);
  const pending = l.status === 2;
  const flagged = (await getFlags([l.id]))[l.id] ?? [];

  const sections = await getSections();
  // Only the owner gives free VIP or bumps; the length that is filled in is the one of the paid VIP.
  const isOwner = (await getRoles()).includes("SuperAdmin");
  const tariff = isOwner && l.status === 3 ? await authed<{ vipDurationDays: number }>("/api/admin/tariffs") : null;
  const vipDays = tariff?.ok ? tariff.data.vipDurationDays : 7;
  const canSeeUsers = sections === null || sections.includes("users");
  // The reports on this listing (only for someone who may read reports; others simply do not see the card).
  const reports =
    sections === null || sections.includes("reports")
      ? await authed<{ items: ReportSummary[]; totalCount?: number }>("/api/admin/content-reports", {
          query: { targetId: l.id, targetType: 1, pageSize: 10 },
        })
      : null;
  const reportItems = reports?.ok ? reports.data.items : null;

  const facts = l.fields.flatMap((f) => {
    const value = fieldValue({ ...f });
    return value ? [{ label: f.label ?? f.key ?? "", value }] : [];
  });

  return (
    <div className="space-y-5">
      <PageHead
        title={l.title}
        hint={`${formatPrice(l.price, l.currency, l.rentalPeriodUnit as RentalPeriodId)} · əlavə olunub ${dateTime(l.createdAtUtc)}`}
        back={{ href: adminHref("/elanlar"), label: "Elanlar" }}
        actions={
          <>
            <Pill tone={statusTone[l.status] ?? "muted"}>{statusLabel[l.status] ?? String(l.status)}</Pill>
            {l.status === 3 && (
              <Link
                href={`/listing/${l.id}`}
                target="_blank"
                className="inline-flex h-7 items-center gap-1 rounded-md border border-line px-2.5 text-xs font-bold text-ink-2 hover:border-primary hover:text-primary"
              >
                Saytda bax
                <ExternalLink className="size-3.5" aria-hidden />
              </Link>
            )}
          </>
        }
      />

      {flagged.length > 0 && (
        <p role="alert" className="rounded-xl bg-warning-soft px-4 py-3 text-sm font-semibold">
          Diqqət: başlıqda və ya təsvirdə qadağan söz tapıldı — <b>{flagged.join(", ")}</b>. Elanı diqqətlə yoxlayın.
        </p>
      )}

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_340px]">
        <div className="min-w-0 space-y-5">
          {images.length > 0 && (
            <Card title={`Şəkillər (${images.length})`}>
              <ul className="grid grid-cols-2 gap-2 p-3 sm:grid-cols-3">
                {images.map((img, i) => (
                  <li
                    key={img.id}
                    className={`relative overflow-hidden rounded-lg bg-primary-soft ${i === 0 ? "col-span-2 row-span-2 aspect-[4/3] sm:col-span-2" : "aspect-[4/3]"}`}
                  >
                    <Image src={ownerImage(img.url)!} alt="" fill unoptimized sizes="400px" className="object-cover" />
                    {img.isCover && (
                      <span className="absolute left-2 top-2 rounded-md bg-black/60 px-2 py-0.5 text-[11px] font-bold text-white">Əsas şəkil</span>
                    )}
                  </li>
                ))}
              </ul>
            </Card>
          )}

          <Card title="Təsvir">
            <p className="whitespace-pre-line p-4 leading-relaxed">{l.description}</p>
          </Card>

          {facts.length > 0 && (
            <Card title="Məlumat">
              <dl className="divide-y divide-line">
                {facts.map((f, i) => (
                  <div key={i} className="flex justify-between gap-4 px-4 py-2.5 text-sm">
                    <dt className="text-ink-2">{f.label}</dt>
                    <dd className="text-right font-semibold">{f.value}</dd>
                  </div>
                ))}
              </dl>
            </Card>
          )}
        </div>

        <aside className="space-y-5 xl:sticky xl:top-4 xl:self-start">
          <Card title="Elan sahibi">
            <div className="flex items-center gap-3 p-4">
              <Avatar name={l.ownerFullName} size="lg" />
              <div className="min-w-0">
                <p className="truncate font-extrabold">
                  {canSeeUsers ? (
                    <Link href={adminHref(`/istifadeciler/${l.ownerId}`)} className="hover:text-primary hover:underline">
                      {l.ownerFullName}
                    </Link>
                  ) : (
                    l.ownerFullName
                  )}
                </p>
                <a href={`tel:${l.phoneNumber}`} className="flex items-center gap-1 text-sm text-ink-2 hover:text-primary">
                  <Phone className="size-3.5" aria-hidden />
                  {l.phoneNumber}
                </a>
              </div>
            </div>
          </Card>

          {pending ? (
            <ModerationActions
              approve={approveListing.bind(null, l.id)}
              reject={rejectListing.bind(null, l.id)}
              backHref={adminHref("/elanlar")}
              noun="Elan"
            />
          ) : [3, 6, 8].includes(l.status) ? (
            <TakeDownPanel take={takeDownListing.bind(null, l.id)} statusText={statusLabel[l.status] ?? String(l.status)} />
          ) : (
            <p className="rounded-xl bg-line px-4 py-3 text-sm font-semibold text-ink-2">
              Bu elan yoxlamada deyil ({statusLabel[l.status] ?? l.status}), üzərində əməliyyat yoxdur.
            </p>
          )}

          {l.status === 3 && isOwner && <GiftPanel target="listing" id={l.id} defaultDays={vipDays} />}

          {reportItems && (
            <Card title={`Şikayətlər (${reportItems.length})`}>
              {reportItems.length === 0 ? (
                <p className="p-4 text-sm text-ink-2">Bu elan haqqında şikayət yoxdur.</p>
              ) : (
                <ul className="divide-y divide-line">
                  {reportItems.map((r) => (
                    <li key={r.id}>
                      <Link
                        href={adminHref(`/sikayetler/${r.id}`)}
                        className="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-primary-soft"
                      >
                        <Flag className="size-4 shrink-0 text-danger" aria-hidden />
                        <span className="min-w-0 flex-1">
                          <span className="block truncate font-semibold">{reportReason[r.reason] ?? "—"}</span>
                          <span className="block truncate text-xs text-ink-2">
                            {r.reporterName ?? "İstifadəçi"} · {dateTime(r.createdAtUtc)}
                          </span>
                        </span>
                        <Pill tone={r.status === 1 ? "warning" : r.status === 3 ? "success" : "muted"}>{reportStatus[r.status] ?? "—"}</Pill>
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          )}
        </aside>
      </div>
    </div>
  );
}
