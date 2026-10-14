import Image from "next/image";
import Link from "next/link";
import { CheckCircle2, ImageIcon } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { authed } from "@/lib/authed";
import type { PendingListing } from "@/lib/admin-types";
import { formatPrice, ownerImage } from "@/lib/format";
import type { Paged, RentalPeriodId } from "@/lib/types";

export default async function PendingListingsPage({ searchParams }: PageProps<"/admin/elanlar">) {
  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const result = await authed<Paged<PendingListing>>("/api/admin/listings/pending", {
    query: { page, pageSize: 20 },
  });
  const data = result.ok ? result.data : null;

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-extrabold">Yoxlama gözləyən elanlar</h1>
      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-16 text-center">
          <CheckCircle2 className="size-12 text-success" aria-hidden />
          <p className="font-bold">Gözləyən elan yoxdur</p>
        </div>
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((l) => (
              <li key={l.id}>
                <Link
                  href={`/admin/elanlar/${l.id}`}
                  className="flex gap-4 rounded-xl border border-line bg-surface p-3 hover:border-primary"
                >
                  <span className="relative size-20 shrink-0 overflow-hidden rounded-lg bg-primary-soft">
                    {l.coverImageUrl ? (
                      <Image src={ownerImage(l.coverImageUrl)!} alt="" fill unoptimized sizes="80px" className="object-cover" />
                    ) : (
                      <span className="flex size-full items-center justify-center text-primary">
                        <ImageIcon className="size-6" aria-hidden />
                      </span>
                    )}
                  </span>
                  <span className="min-w-0">
                    <span className="block truncate font-bold">{l.title}</span>
                    <span className="font-extrabold text-primary">
                      {formatPrice(l.price, l.currency, l.rentalPeriodUnit as RentalPeriodId)}
                    </span>
                    <span className="block text-sm text-ink-2">
                      {l.imageCount} şəkil · {new Date(l.createdAtUtc).toLocaleString("az")}
                    </span>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => (p === 1 ? "/admin/elanlar" : `/admin/elanlar?page=${p}`)}
          />
        </>
      )}
    </div>
  );
}
