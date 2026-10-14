import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { NewListingForm } from "@/components/new-listing/NewListingForm";
import { getCategories } from "@/lib/api";
import { getCities } from "@/lib/api";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Yeni elan"), robots: { index: false } };
}

export default async function NewListingPage() {
  const t = await getT();
  if (!(await getSession())) redirect("/giris?next=/yeni-elan");

  const [categories, cities] = await Promise.all([getCategories(), getCities()]);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-extrabold">{t("Yeni elan")}</h1>
        <p className="mt-1 text-ink-2">{t("Doldurun, şəkillər əlavə edin və göndərin.")}</p>
      </div>
      <NewListingForm categories={categories} cities={cities} />
    </div>
  );
}
