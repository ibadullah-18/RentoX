import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ImageIcon, ListChecks, Plus } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { StatusChip } from "@/components/StatusChip";
import { formatPrice, formatPublished, ownerImage } from "@/lib/format";
import { getMyListings } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Elanlarım"), robots: { index: false } };
}

export default async function MyListingsPage({ searchParams }: PageProps<"/elanlarim">) {
  const t = await getT();
  const locale = await getLocale();
  if (!(await getSession())) redirect("/giris?next=/elanlarim");

  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const result = await getMyListings(page);
  if (!result.ok && result.status === 401) redirect("/giris?next=/elanlarim");
  const data = result.ok ? result.data : null;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-extrabold">{t("Elanlarım")}</h1>
        <Link
          href="/yeni-elan"
          className="flex h-11 items-center gap-2 rounded-xl bg-success px-5 font-bold text-white hover:opacity-90"
        >
          <Plus className="size-5" aria-hidden />
          {t("Yeni elan")}</Link>
      </div>

      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-line bg-surface py-16 text-center">
          <ListChecks className="size-12 text-primary" aria-hidden />
          <h2 className="text-lg font-extrabold">{t("Hələ elanınız yoxdur")}</h2>
          <p className="max-w-sm text-ink-2">{t("İlk elanınızı verin, yoxlamadan sonra hamı görəcək.")}</p>
        </div>
      ) : (
        <>
          <ul className="space-y-3">
            {data.items.map((l) => (
              <li key={l.id}>
                <Link
                  href={`/elanlarim/${l.id}`}
                  className="flex gap-4 rounded-xl border border-line bg-surface p-3 hover:border-primary"
                >
                  <div className="relative size-24 shrink-0 overflow-hidden rounded-lg bg-primary-soft">
                    {l.coverImageUrl ? (
                      <Image src={ownerImage(l.coverImageUrl)!} alt="" fill unoptimized sizes="96px" className="object-cover" />
                    ) : (
                      <div className="flex size-full items-center justify-center text-primary">
                        <ImageIcon className="size-7" aria-hidden />
                      </div>
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-2">
                      <h2 className="truncate font-bold">{l.title}</h2>
                      <StatusChip status={l.status} expiresAtUtc={l.expiresAtUtc} />
                    </div>
                    <p className="mt-1 font-extrabold text-primary">
                      {formatPrice(l.price, l.currency, l.rentalPeriodUnit, t, locale)}
                    </p>
                    <p className="mt-1 text-sm text-ink-2">
                      {l.imageCount} {t("şəkil")}{l.expiresAtUtc && ` · ${t("{date} tarixinədək", { date: formatPublished(l.expiresAtUtc, t, locale) ?? "" })}`}
                    </p>
                  </div>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => (p === 1 ? "/elanlarim" : `/elanlarim?page=${p}`)}
          />
        </>
      )}
    </div>
  );
}
