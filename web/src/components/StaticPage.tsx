import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Markdown } from "@/components/Markdown";
import { getPage } from "@/lib/api";
import { getLocale, getT } from "@/lib/i18n/server";

/** A fixed page (terms, privacy, about, …): the text the staff wrote, in the visitor's language. */
export async function StaticPage({ slug }: { slug: string }) {
  const page = await getPage(slug);
  if (!page) notFound();

  const t = await getT();
  const locale = await getLocale();
  const date = new Date(page.updatedAtUtc).toLocaleDateString(locale, { year: "numeric", month: "long", day: "numeric" });

  return (
    <article className="mx-auto max-w-3xl pb-6">
      <h1 className="text-2xl font-extrabold leading-tight md:text-3xl">{page.title}</h1>
      <p className="mb-6 mt-1 text-sm text-ink-2">{t("Son yenilənmə: {date}", { date })}</p>
      <Markdown text={page.body} />
    </article>
  );
}

export async function pageMetadata(slug: string): Promise<Metadata> {
  const page = await getPage(slug);
  return { title: page?.title ?? "RentoX" };
}
