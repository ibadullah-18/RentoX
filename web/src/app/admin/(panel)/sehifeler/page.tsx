import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { LoadProblem, PageHead, dateTime } from "@/components/admin/ui";
import type { PageRow } from "@/components/admin/PageEditor";
import { adminHref } from "@/lib/admin-path";
import { authed } from "@/lib/authed";

type Row = PageRow & { updatedAtUtc: string };

/** Where each page is on the site. */
const PATH: Record<string, string> = {
  about: "/haqqimizda",
  terms: "/sertler",
  privacy: "/mexfilik",
  cooperation: "/emekdasliq",
  rules: "/qaydalar",
  faq: "/suallar",
};

/** The fixed pages of the site and app: terms, privacy, about us, cooperation, rules, questions. */
export default async function PagesAdminPage() {
  const result = await authed<Row[]>("/api/admin/pages");
  if (!result.ok) return <LoadProblem what="Səhifələr" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Səhifələr"
        hint="İstifadə şərtləri, məxfilik siyasəti, haqqımızda və digər sabit səhifələr. Saytda və mobil tətbiqdə eyni mətn göstərilir; dəyişiklik dərhal qüvvəyə minir."
      />
      <ul className="overflow-hidden rounded-xl border border-line bg-surface">
        {result.data.map((p) => (
          <li key={p.slug} className="border-b border-line last:border-b-0">
            <Link href={adminHref(`/sehifeler/${p.slug}`)} className="flex items-center gap-3 px-4 py-3 hover:bg-primary-soft">
              <span className="min-w-0 flex-1">
                <span className="block font-bold">{p.titleAz}</span>
                <span className="block text-xs text-ink-2">
                  {PATH[p.slug]} · {p.titleRu} · {p.titleEn} · yenilənib {dateTime(p.updatedAtUtc)}
                </span>
              </span>
              <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
