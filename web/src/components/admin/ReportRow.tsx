import Image from "next/image";
import Link from "next/link";
import { ArrowRight, MessagesSquare, Store, Tag } from "lucide-react";
import { Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { chatReason, reportReason, reportStatus } from "@/lib/admin-labels";
import { ownerImage } from "@/lib/format";
import { ago, chatReasonTone, contentReasonTone, reportStatusTone, urgency, urgencyClass } from "@/lib/report-view";

export type ReportRowData = {
  kind: "listing" | "store" | "chat";
  id: string;
  title: string;
  reason: number;
  status: number;
  createdAtUtc: string;
  reporterName?: string | null;
  /** The person on the other side: the owner of the reported thing, or the other person in the chat. */
  otherName?: string | null;
  /** How many open reports wait on the same thing. */
  openOnSame?: number;
  coverImageId?: string | null;
};

const noun = { listing: "Elan", store: "Mağaza", chat: "Yazışma" } as const;

/** One report in the queue: what it is about, why, who sent it, and how long it has waited. */
export function ReportRow({ r }: { r: ReportRowData }) {
  const open = r.status === 1 || r.status === 2;
  const level = urgency(r.createdAtUtc);
  const reason = (r.kind === "chat" ? chatReason : reportReason)[r.reason] ?? "—";
  const tone = (r.kind === "chat" ? chatReasonTone : contentReasonTone)[r.reason] ?? "muted";
  const href = r.kind === "chat" ? adminHref(`/yazisma-sikayetleri/${r.id}`) : adminHref(`/sikayetler/${r.id}`);

  return (
    <li>
      <Link href={href} className="group flex gap-3 rounded-xl border border-line bg-surface p-3 hover:border-primary">
        <span className="relative flex size-14 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-primary-soft text-primary">
          {r.kind === "listing" && r.coverImageId ? (
            <Image src={ownerImage(`/api/listing-images/${r.coverImageId}`)!} alt="" fill unoptimized sizes="56px" className="object-cover" />
          ) : r.kind === "chat" ? (
            <MessagesSquare className="size-6" aria-hidden />
          ) : r.kind === "store" ? (
            <Store className="size-6" aria-hidden />
          ) : (
            <Tag className="size-6" aria-hidden />
          )}
        </span>

        <span className="min-w-0 flex-1 space-y-1">
          <span className="flex items-start justify-between gap-2">
            <span className="min-w-0">
              <span className="block text-[11px] font-bold uppercase tracking-wide text-ink-2">{noun[r.kind]}</span>
              <span className="block truncate font-bold">{r.title}</span>
            </span>
            <Pill tone={reportStatusTone[r.status]}>{reportStatus[r.status] ?? "—"}</Pill>
          </span>

          <span className="flex flex-wrap items-center gap-1.5">
            <Pill tone={tone}>{reason}</Pill>
            {open && (r.openOnSame ?? 0) > 1 && <Pill tone="danger">{r.openOnSame} açıq şikayət: təkrar</Pill>}
          </span>

          <span className="block truncate text-xs text-ink-2">
            {r.reporterName ? `${r.reporterName}` : "İstifadəçi"} şikayət etdi
            {r.otherName && (
              <>
                <ArrowRight className="mx-1 inline size-3" aria-hidden />
                {r.otherName}
              </>
            )}
            {" · "}
            <span className={open ? urgencyClass[level] : ""}>
              {ago(r.createdAtUtc)}
              {open && level === "late" && " — uzun gözləyir"}
            </span>
          </span>
        </span>
      </Link>
    </li>
  );
}
