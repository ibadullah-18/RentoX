import { BackLink } from "@/components/admin/BackLink";
import { adminHref } from "@/lib/admin-path";
import { notFound } from "next/navigation";
import { Store } from "lucide-react";
import { approveStore, reinstateStore, rejectStore, suspendStore } from "@/app/admin/(panel)/actions";
import { ModerationActions, ReinstateButton, TakeDownPanel } from "@/components/admin/AdminControls";
import { authed } from "@/lib/authed";
import type { MyStore } from "@/lib/types";

const days = ["B.e.", "Ç.a.", "Çər.", "C.a.", "Cümə", "Şən.", "Baz."];
const hhmm = (m: number) => `${String(Math.floor(m / 60)).padStart(2, "0")}:${String(m % 60).padStart(2, "0")}`;

export default async function ReviewStorePage({ params }: PageProps<"/admin/magazalar/[id]">) {
  const { id } = await params;
  const result = await authed<MyStore & { ownerId?: string }>(`/api/admin/stores/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const s = result.data;
  const pending = s.status === 2;

  const rows: [string, string | null | undefined][] = [
    ["Telefon", s.phoneNumber],
    ["E-poçt", s.email],
    ["Ünvan", s.address],
    ["Sayt", s.websiteUrl],
    ["Instagram", s.instagramUrl],
    ["TikTok", s.tiktokUrl],
    ["Facebook", s.facebookUrl],
    ["YouTube", s.youtubeUrl],
    ["WhatsApp", s.whatsAppNumber ? `+${s.whatsAppNumber}` : null],
    ["WhatsApp ilk mesaj", s.whatsAppMessage],
    [
      "İş saatları",
      s.workingHours && s.workingHours.length > 0
        ? s.workingHours.map((h) => `${days[h.day - 1]} ${hhmm(h.openMinute)}–${hhmm(h.closeMinute)}`).join(" · ")
        : null,
    ],
  ];

  return (
    <div className="space-y-6">
      <BackLink href={adminHref("/magazalar")} title="Mağazalar" />

      <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0 space-y-5">
          {s.coverImageUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={`/bff/store-image/${s.id}/cover`} alt="" className="aspect-[4/1] w-full rounded-xl object-cover" />
          )}
          <div className="flex items-center gap-4">
            <span className="flex size-20 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-primary-soft text-primary">
              {s.logoImageUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={`/bff/store-image/${s.id}/logo`} alt="" className="size-full object-cover" />
              ) : (
                <Store className="size-8" aria-hidden />
              )}
            </span>
            <h1 className="text-2xl font-extrabold">{s.name}</h1>
          </div>
          <p className="whitespace-pre-line leading-relaxed">{s.description}</p>
          <dl className="overflow-hidden rounded-xl border border-line bg-surface">
            {rows.map(([k, v]) =>
              v ? (
                <div key={k} className="flex justify-between gap-4 border-b border-line px-4 py-3 last:border-b-0">
                  <dt className="text-ink-2">{k}</dt>
                  <dd className="break-all text-right font-semibold">{v}</dd>
                </div>
              ) : null,
            )}
          </dl>
        </div>

        <aside className="md:sticky md:top-24 md:self-start">
          {pending ? (
            <ModerationActions
              approve={approveStore.bind(null, s.id)}
              reject={rejectStore.bind(null, s.id)}
              backHref={adminHref("/magazalar")}
              noun="Mağaza"
            />
          ) : s.status === 3 ? (
            <TakeDownPanel take={suspendStore.bind(null, s.id)} statusText="aktiv" noun="mağaza" />
          ) : s.status === 5 ? (
            <ReinstateButton reinstate={reinstateStore.bind(null, s.id)} reason={s.rejectionReason} />
          ) : (
            <p className="rounded-xl bg-line px-4 py-3 text-sm font-semibold text-ink-2">
              Bu mağaza yoxlamada deyil.
            </p>
          )}
        </aside>
      </div>
    </div>
  );
}
