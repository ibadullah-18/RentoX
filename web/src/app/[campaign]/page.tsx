import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Markdown } from "@/components/Markdown";
import { getCampaignPage } from "@/lib/api";
import { getT } from "@/lib/i18n/server";

/** A campaign page can only have a plain name; anything else is not one of ours. */
const NAME = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export async function generateMetadata({ params }: { params: Promise<{ campaign: string }> }): Promise<Metadata> {
  const { campaign } = await params;
  const page = NAME.test(campaign) ? await getCampaignPage(campaign) : null;
  return { title: page ? `${page.title} | RentoX` : "RentoX" };
}

/**
 * rentox.az/name: the page of a campaign the staff made (a banner with a page). It has the picture, the long text
 * and an optional button. A name nobody made, or a campaign switched off or not started yet, is "not found".
 */
export default async function CampaignPageRoute({ params }: { params: Promise<{ campaign: string }> }) {
  const { campaign } = await params;
  if (!NAME.test(campaign)) notFound();
  const page = await getCampaignPage(campaign);
  if (!page) notFound();

  const t = await getT();
  const button = page.buttonUrl && page.buttonLabel ? { url: page.buttonUrl, label: page.buttonLabel } : null;
  const buttonClass =
    "inline-flex h-12 items-center justify-center rounded-xl bg-primary px-6 text-[15px] font-bold text-white hover:bg-primary-press";

  return (
    <article className="mx-auto max-w-3xl space-y-5 pb-6">
      <div className="relative aspect-[3/1] min-h-32 w-full overflow-hidden rounded-xl bg-primary-soft">
        <Image src={page.imageUrl} alt="" fill unoptimized priority sizes="(min-width: 1100px) 768px, 100vw" className="object-cover" />
      </div>

      <header>
        <h1 className="text-2xl font-extrabold leading-tight md:text-3xl">{page.title}</h1>
        {page.subtitle && <p className="mt-1 text-base text-ink-2">{page.subtitle}</p>}
      </header>

      {page.isOver && (
        <p role="status" className="rounded-xl bg-warning-soft px-4 py-3 text-sm font-semibold text-warning">
          {t("Bu kampaniya bitib.")}
        </p>
      )}

      {page.body && <Markdown text={page.body} />}

      {button && !page.isOver &&
        (button.url.startsWith("/") ? (
          <Link href={button.url} className={buttonClass}>
            {button.label}
          </Link>
        ) : (
          <a href={button.url} target="_blank" rel="noopener noreferrer" className={buttonClass}>
            {button.label}
          </a>
        ))}

      <Link href="/" className="inline-block text-sm font-bold text-primary hover:underline">
        {t("Ana səhifəyə qayıt")}
      </Link>
    </article>
  );
}
