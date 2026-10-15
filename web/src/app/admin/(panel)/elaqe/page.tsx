import { SiteInfoForm, type SiteInfoRow } from "@/components/admin/SiteInfoForm";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { authed } from "@/lib/authed";

/** Who runs the site and how to reach them. */
export default async function ContactAdminPage() {
  const result = await authed<SiteInfoRow>("/api/admin/site-info");
  if (!result.ok) return <LoadProblem what="Əlaqə məlumatları" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Əlaqə və şirkət məlumatları"
        hint="Saytın altlığında (footer) və mobil tətbiqin «Məlumat» bölməsində göstərilir. Operator məlumatı İstifadə şərtləri və Məxfilik siyasətinə də avtomatik yazılır."
      />
      <SiteInfoForm key={JSON.stringify(result.data)} current={result.data} />
    </div>
  );
}
