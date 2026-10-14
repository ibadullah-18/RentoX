import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { NewTicketForm } from "@/components/SupportForms";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Yeni müraciət"), robots: { index: false } };
}

export default async function NewTicketPage({ searchParams }: PageProps<"/destek/yeni">) {
  const t = await getT();
  if (!(await getSession())) redirect("/giris?next=/destek/yeni");

  const raw = (await searchParams).category;
  const category = Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10);

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link href="/destek" className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        {t("Dəstək")}</Link>
      <h1 className="text-2xl font-extrabold">{t("Yeni müraciət")}</h1>
      <NewTicketForm initialCategory={category >= 1 && category <= 7 ? category : 1} />
    </div>
  );
}
