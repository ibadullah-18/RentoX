import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { ReportReview } from "@/components/admin/TicketAdmin";
import { authed } from "@/lib/authed";
import type { ReportDetails } from "@/lib/admin-types";
import { reportReason, reportStatus } from "@/lib/admin";

export default async function ReportPage({ params }: PageProps<"/admin/sikayetler/[id]">) {
  const { id } = await params;
  const result = await authed<ReportDetails>(`/api/admin/content-reports/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const r = result.data;

  const target =
    r.targetType === 1 ? `/listing/${r.targetId}` : null; // a store is opened from its moderation page

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link href="/admin/sikayetler" className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        Şikayətlər
      </Link>

      <div>
        <h1 className="text-2xl font-extrabold leading-tight">{r.targetTitle ?? "Şikayət"}</h1>
        <p className="mt-1 text-sm text-ink-2">
          {r.targetType === 1 ? "Elan" : "Mağaza"} · {reportReason[r.reason] ?? "—"} ·{" "}
          {reportStatus[r.status] ?? "—"} · {new Date(r.createdAtUtc).toLocaleString("az")}
        </p>
      </div>

      {r.details && <p className="whitespace-pre-line rounded-xl border border-line bg-surface p-4">{r.details}</p>}

      <dl className="overflow-hidden rounded-xl border border-line bg-surface text-sm">
        <div className="flex justify-between gap-4 border-b border-line px-4 py-3">
          <dt className="text-ink-2">Bu hədəfə açıq şikayətlər</dt>
          <dd className="font-semibold">{r.openReportsOnTarget}</dd>
        </div>
        {r.resolutionNote && (
          <div className="flex justify-between gap-4 px-4 py-3">
            <dt className="text-ink-2">Qeyd</dt>
            <dd className="text-right font-semibold">{r.resolutionNote}</dd>
          </div>
        )}
      </dl>

      {target && (
        <Link href={target} className="inline-block font-bold text-primary hover:underline">
          Elana bax
        </Link>
      )}

      <ReportReview id={r.id} status={r.status} />
    </div>
  );
}
