import { notFound } from "next/navigation";
import { PageEditor, type PageRow } from "@/components/admin/PageEditor";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { authed } from "@/lib/authed";

export default async function EditPagePage({ params }: PageProps<"/admin/sehifeler/[slug]">) {
  const { slug } = await params;
  const result = await authed<PageRow[]>("/api/admin/pages");
  if (!result.ok) return <LoadProblem what="Səhifə" status={result.status} />;

  const page = result.data.find((p) => p.slug === slug);
  if (!page) notFound();

  return (
    <div className="space-y-5">
      <PageHead title={page.titleAz} back={{ href: adminHref("/sehifeler"), label: "Səhifələr" }} />
      <PageEditor key={`${page.slug}-${page.bodyAz.length}-${page.bodyRu.length}-${page.bodyEn.length}`} page={page} />
    </div>
  );
}
