import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { EditListingForm } from "@/components/new-listing/EditListingForm";
import { categoryChain, getCategories, getCities } from "@/lib/api";
import { getMyListing } from "@/lib/personal";
import { getPrices } from "@/lib/tariffs";
import { getSession } from "@/lib/session";
import type { FieldDefinition } from "@/lib/types";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Elanı redaktə et"), robots: { index: false } };
}

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

async function getFieldDefs(categoryId: string): Promise<FieldDefinition[]> {
  try {
    const language = await getLocale();
    const res = await fetch(`${API_BASE}/api/categories/${categoryId}/fields?language=${language}`, {
      next: { revalidate: 60 },
    });
    if (!res.ok) return [];
    const list = (await res.json()) as FieldDefinition[];
    return [...list].sort((a, b) => a.displayOrder - b.displayOrder);
  } catch {
    return [];
  }
}

export default async function EditListingPage({ params }: PageProps<"/elanlarim/[id]/redakte">) {
  const t = await getT();
  const { id } = await params;
  if (!(await getSession())) redirect(`/giris?next=/elanlarim/${id}/redakte`);

  const result = await getMyListing(id);
  if (!result.ok) {
    if (result.status === 401) redirect(`/giris?next=/elanlarim/${id}/redakte`);
    notFound();
  }
  const listing = result.data;

  // Only a draft or a rejected listing can be edited; others go back to the page.
  if (listing.status !== 1 && listing.status !== 4) redirect(`/elanlarim/${id}`);

  const [categories, cities, fieldDefs] = await Promise.all([
    getCategories(),
    getCities(),
    getFieldDefs(listing.categoryId),
  ]);
  const category = categoryChain(categories, listing.categoryId).at(-1);
  const periods = category?.rentalPeriods?.length ? category.rentalPeriods : [1, 2, 3, 4, 5, 6];

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        href={`/elanlarim/${id}`}
        className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary"
      >
        <ChevronLeft className="size-4" aria-hidden />
        {t("Elana qayıt")}</Link>
      <h1 className="text-2xl font-extrabold">{t("Elanı redaktə et")}</h1>
      <EditListingForm listing={listing} fieldDefs={fieldDefs} cities={cities} periods={periods} maxPhotos={(await getPrices()).maxPhotos} />
    </div>
  );
}
