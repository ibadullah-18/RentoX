import { adminHref } from "@/lib/admin-path";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { approveListing, rejectListing, takeDownListing } from "@/app/admin/(panel)/actions";
import { ModerationActions, TakeDownPanel } from "@/components/admin/AdminControls";
import { authed } from "@/lib/authed";
import type { ModerationListing } from "@/lib/admin-types";
import { fieldValue, formatPrice, ownerImage } from "@/lib/format";
import { statusLabel } from "@/lib/status";
import type { RentalPeriodId } from "@/lib/types";

export default async function ReviewListingPage({ params }: PageProps<"/admin/elanlar/[id]">) {
  const { id } = await params;
  const result = await authed<ModerationListing>(`/api/admin/listings/${encodeURIComponent(id)}`, {
    query: { language: "az" },
  });
  if (!result.ok) notFound();
  const l = result.data;
  const images = [...l.images].sort((a, b) => a.displayOrder - b.displayOrder);
  const pending = l.status === 2;

  return (
    <div className="space-y-6">
      <Link href={adminHref("/elanlar")} className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        Elanlar
      </Link>

      <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0 space-y-5">
          <div>
            <h1 className="text-2xl font-extrabold leading-tight">{l.title}</h1>
            <p className="mt-1 text-xl font-extrabold text-primary">
              {formatPrice(l.price, l.currency, l.rentalPeriodUnit as RentalPeriodId)}
            </p>
            <p className="mt-1 text-sm text-ink-2">
              {l.ownerFullName} · {l.phoneNumber} · {new Date(l.createdAtUtc).toLocaleString("az")} ·{" "}
              {statusLabel[l.status] ?? l.status}
            </p>
          </div>

          {images.length > 0 && (
            <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3">
              {images.map((img) => (
                <li key={img.id} className="relative aspect-[4/3] overflow-hidden rounded-lg bg-primary-soft">
                  <Image src={ownerImage(img.url)!} alt="" fill unoptimized sizes="300px" className="object-cover" />
                </li>
              ))}
            </ul>
          )}

          <p className="whitespace-pre-line leading-relaxed">{l.description}</p>

          {l.fields.length > 0 && (
            <dl className="overflow-hidden rounded-xl border border-line bg-surface">
              {l.fields.flatMap((f, i) => {
                const value = fieldValue({ ...f });
                return value
                  ? [
                      <div key={i} className="flex justify-between gap-4 border-b border-line px-4 py-3 last:border-b-0">
                        <dt className="text-ink-2">{f.label ?? f.key}</dt>
                        <dd className="text-right font-semibold">{value}</dd>
                      </div>,
                    ]
                  : [];
              })}
            </dl>
          )}
        </div>

        <aside className="md:sticky md:top-24 md:self-start">
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
              Bu elan artıq yoxlamada deyil ({statusLabel[l.status] ?? l.status}).
            </p>
          )}
        </aside>
      </div>
    </div>
  );
}
