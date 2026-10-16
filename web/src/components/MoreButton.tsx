import Link from "next/link";
import { getT } from "@/lib/i18n/server";

/** "Show more" under a list that grows page by page: a link to the same address with the next page. */
export async function MoreButton({ href }: { href: string | null }) {
  if (!href) return null;
  const t = await getT();
  return (
    <div className="mt-8 flex justify-center">
      <Link
        href={href}
        scroll={false}
        className="flex h-11 items-center rounded-xl border border-line bg-surface px-8 font-bold hover:border-primary"
      >
        {t("Daha çox göstər")}
      </Link>
    </div>
  );
}
