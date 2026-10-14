import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ChevronLeft, ImageIcon } from "lucide-react";
import { ManageActions } from "@/components/ManageActions";
import { StatusChip } from "@/components/StatusChip";
import { fieldValue, formatPrice, ownerImage, formatNumber } from "@/lib/format";
import { getMyListing, getWallet } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { effectiveStatus } from "@/lib/status";
import { Status } from "@/lib/types";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Elan"), robots: { index: false } };
}

export default async function MyListingPage({ params }: PageProps<"/elanlarim/[id]">) {
  const t = await getT();
  const locale = await getLocale();
  const { id } = await params;
  if (!(await getSession())) redirect(`/giris?next=/elanlarim/${id}`);

  const [listing, wallet] = await Promise.all([getMyListing(id), getWallet()]);
  if (!listing.ok) {
    if (listing.status === 401) redirect(`/giris?next=/elanlarim/${id}`);
    notFound();
  }
  const l = listing.data;
  const status = effectiveStatus(l.status, l.expiresAtUtc);
  const images = [...l.images].sort((a, b) => a.displayOrder - b.displayOrder);
  const balance = wallet.ok ? wallet.data.balance : 0;
  const place = [l.cityName, l.districtName].filter(Boolean).join(", ");

  const hint: Record<number, string> = {
    [Status.pendingReview]: t("Moderator baxır. Nəticəni bildirişlə xəbər verəcəyik."),
    [Status.paymentRequired]: t("Pulsuz elan limiti dolub. Dərc üçün aktivləşdirmə haqqı lazımdır."),
    [Status.expired]: t("Elanın 30 günü bitib. Yeniləyənədək görünmür."),
    [Status.deactivated]: t("Elan dayandırılıb, axtarışda görünmür."),
    [Status.draft]: t("Elan hələ göndərilməyib."),
  };

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <Link href="/elanlarim" className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        {t("Elanlarım")}</Link>

      <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0 space-y-5">
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-2">
              <StatusChip status={l.status} expiresAtUtc={l.expiresAtUtc} />
              {status === Status.active && (
                <Link href={`/listing/${l.id}`} className="text-sm font-bold text-primary hover:underline">
                  {t("Saytda bax")}</Link>
              )}
            </div>
            <h1 className="text-2xl font-extrabold leading-tight">{l.title}</h1>
            <p className="text-xl font-extrabold text-primary">
              {formatPrice(l.price, l.currency, l.rentalPeriodUnit, t, locale)}
            </p>
            {place && <p className="text-ink-2">{place}</p>}
          </div>

          {status === Status.rejected && (
            <div role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm">
              <p className="font-bold text-danger">{t("Elan rədd edilib")}</p>
              {l.rejectionReason && <p className="mt-1 text-ink">{l.rejectionReason}</p>}
              <p className="mt-1 text-ink-2">{t("Düzəldib yenidən göndərə bilərsiniz.")}</p>
            </div>
          )}
          {hint[status] && status !== Status.rejected && (
            <p className="rounded-xl bg-primary-soft px-4 py-3 text-sm font-semibold text-primary">{hint[status]}</p>
          )}

          {images.length > 0 ? (
            <ul className="strip">
              {images.map((img) => (
                <li key={img.id} className="relative size-28 shrink-0 overflow-hidden rounded-lg bg-primary-soft">
                  <Image src={ownerImage(img.url)!} alt="" fill unoptimized sizes="112px" className="object-cover" />
                </li>
              ))}
            </ul>
          ) : (
            <div className="flex h-28 items-center justify-center rounded-lg bg-primary-soft text-primary">
              <ImageIcon className="size-8" aria-hidden />
            </div>
          )}

          {l.description && <p className="whitespace-pre-line leading-relaxed">{l.description}</p>}

          {l.fields.length > 0 && (
            <dl className="overflow-hidden rounded-xl border border-line bg-surface">
              {l.fields.flatMap((f, i) => {
                const value = fieldValue(f, t, locale);
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

        <aside className="space-y-3 md:sticky md:top-24 md:self-start">
          <div className="rounded-xl border border-line bg-surface p-4">
            <p className="text-sm text-ink-2">{t("Pul kisəsi")}</p>
            <p className="text-xl font-extrabold">{formatNumber(balance, locale)} {t("AZN")}</p>
          </div>
          <ManageActions id={l.id} status={status} balance={balance} />
        </aside>
      </div>
    </div>
  );
}
