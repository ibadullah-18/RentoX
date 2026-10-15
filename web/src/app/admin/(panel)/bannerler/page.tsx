import { BannerManager, type BannerRow } from "@/components/admin/BannerManager";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { authed } from "@/lib/authed";

/** The banners at the top of the home page (site and app). */
export default async function BannersAdminPage() {
  const result = await authed<BannerRow[]>("/api/admin/banners");
  if (!result.ok) return <LoadProblem what="Bannerlər" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Ana səhifə bannerləri"
        hint="Saytın və mobil tətbiqin ana səhifəsinin yuxarısında göstərilir. Banner yalnız şəkli olanda, «Göstərilir» seçiləndə və tarix aralığında olanda görünür. Mobil tətbiqdə keçid olaraq yalnız /listing/…, /store/… və tam https:// ünvanları açılır; saytın digər səhifələrinə keçid yalnız vebdə işləyir. Kateqoriyaların sırası və görünməsi «Kateqoriyalar» bölməsindən idarə olunur."
      />
      <BannerManager banners={result.data} />
    </div>
  );
}
