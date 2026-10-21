import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ExternalLink, Store, Tag } from "lucide-react";
import { ReportDecision } from "@/components/admin/ReportDecision";
import { OtherReports, Outcome, PersonCard, ReasonCard } from "@/components/admin/ReportParts";
import { Card, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import type { AdminUserDetails } from "@/lib/admin-catalog";
import { listingStatusLabel, reportStatus, storeStatusLabel } from "@/lib/admin-labels";
import type { ReportDetails, ReportSummary } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import { ownerImage } from "@/lib/format";
import { LISTING_CAN_TAKE_DOWN, STORE_CAN_SUSPEND, ago, reportStatusTone } from "@/lib/report-view";
import { getSections } from "@/lib/session";
import type { Paged } from "@/lib/types";

/** A report about a listing or a store: what was said, what it is about, and the decision with its consequences. */
export default async function ReportPage({ params }: PageProps<"/admin/sikayetler/[id]">) {
  const { id } = await params;
  const result = await authed<ReportDetails>(`/api/admin/content-reports/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const r = result.data;

  const sections = await getSections();
  const has = (section: string) => sections === null || sections.includes(section);
  const isListing = r.targetType === 1;
  const noun = isListing ? "elan" : "mağaza";

  const [others, owner] = await Promise.all([
    authed<Paged<ReportSummary>>("/api/admin/content-reports", { query: { targetId: r.targetId, pageSize: 20 } }),
    has("users") && r.targetOwnerId ? authed<AdminUserDetails>(`/api/admin/users/${r.targetOwnerId}`) : Promise.resolve(null),
  ]);

  const other: ReportSummary[] = others.ok ? others.data.items.filter((o) => o.id !== r.id) : [];
  const otherOpen = other.filter((o) => o.status === 1 || o.status === 2);
  const stateLabel = r.targetStatus == null ? null : (isListing ? listingStatusLabel : storeStatusLabel)[r.targetStatus];

  // What an upheld report may lead to: only what this admin may do and what the thing's state still allows.
  const canTakeDown = isListing && has("listings") && r.targetStatus != null && LISTING_CAN_TAKE_DOWN.has(r.targetStatus);
  const canSuspend = !isListing && has("stores") && r.targetStatus != null && STORE_CAN_SUSPEND.has(r.targetStatus);
  const canBlock = has("users") && !!r.targetOwnerId;

  return (
    <div className="space-y-5">
      <PageHead
        title={r.targetTitle ?? "Şikayət"}
        hint={`${isListing ? "Elan" : "Mağaza"} haqqında şikayət · ${ago(r.createdAtUtc)}`}
        back={{ href: adminHref("/sikayetler"), label: "Şikayətlər" }}
        actions={<Pill tone={reportStatusTone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>}
      />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="space-y-5">
          <ReasonCard
            kind="content"
            reason={r.reason}
            details={r.details}
            reporterName={r.reporterName}
            reporterHref={has("users") ? adminHref(`/istifadeciler/${r.reporterId}`) : null}
            createdAtUtc={r.createdAtUtc}
            status={r.status}
            reviewedAtUtc={r.reviewedAtUtc}
            openOnSame={r.openReportsOnTarget}
          />

          <Card title={`Şikayət edilən ${noun}`}>
            <div className="flex flex-wrap items-center gap-4 p-4">
              <span className="relative flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-primary-soft text-primary">
                {isListing && r.coverImageId ? (
                  <Image src={ownerImage(`/api/listing-images/${r.coverImageId}`)!} alt="" fill unoptimized sizes="96px" className="object-cover" />
                ) : isListing ? (
                  <Tag className="size-8" aria-hidden />
                ) : (
                  <Store className="size-8" aria-hidden />
                )}
              </span>
              <div className="min-w-0 flex-1 space-y-1.5">
                <p className="text-lg font-extrabold leading-tight">{r.targetTitle ?? "—"}</p>
                <div className="flex flex-wrap items-center gap-2 text-sm text-ink-2">
                  {stateLabel && <Pill tone={r.targetStatus === 3 ? "success" : "muted"}>{stateLabel}</Pill>}
                  {r.ownerName && <span>Sahibi: <span className="font-semibold text-ink">{r.ownerName}</span></span>}
                </div>
                <div className="flex flex-wrap gap-x-4 gap-y-1 pt-1 text-sm font-bold">
                  {has(isListing ? "listings" : "stores") && (
                    <Link href={adminHref(`/${isListing ? "elanlar" : "magazalar"}/${r.targetId}`)} className="text-primary hover:underline">
                      Moderasiya səhifəsində aç
                    </Link>
                  )}
                  {isListing && (
                    <Link href={`/listing/${r.targetId}`} target="_blank" className="inline-flex items-center gap-1 text-primary hover:underline">
                      Saytda bax <ExternalLink className="size-3.5" aria-hidden />
                    </Link>
                  )}
                </div>
              </div>
            </div>
          </Card>

          <OtherReports items={other} noun={noun} />
        </div>

        <div className="space-y-5 xl:sticky xl:top-4 xl:self-start">
          <Card title="Qərar">
            {r.status === 3 || r.status === 4 ? (
              <Outcome status={r.status} note={r.resolutionNote} />
            ) : (
              <ReportDecision
                key={r.id}
                kind="content"
                id={r.id}
                status={r.status}
                noun={noun}
                listing={canTakeDown ? { id: r.targetId } : null}
                store={canSuspend ? { id: r.targetId } : null}
                person={canBlock && r.ownerName ? { id: r.targetOwnerId!, name: r.ownerName } : null}
                others={otherOpen.map((o) => ({ id: o.id }))}
                listUrl={adminHref("/sikayetler")}
              />
            )}
          </Card>

          {r.ownerName && (
            <PersonCard
              title="Sahibi haqqında"
              name={r.ownerName}
              userHref={has("users") && r.targetOwnerId ? adminHref(`/istifadeciler/${r.targetOwnerId}`) : null}
              details={owner && owner.ok ? owner.data : null}
            />
          )}
        </div>
      </div>
    </div>
  );
}
