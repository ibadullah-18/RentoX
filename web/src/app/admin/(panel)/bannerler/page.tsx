import { BannerManager, type BannerRow } from "@/components/admin/BannerManager";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { getCategories } from "@/lib/api";
import { authed } from "@/lib/authed";
import type { AdminStoreItem } from "@/lib/admin-catalog";
import type { Category, Paged } from "@/lib/types";

function flatten(tree: Category[], prefix = ""): { id: string; name: string }[] {
  return tree.flatMap((c) => [{ id: c.id, name: prefix + c.name }, ...flatten(c.children, `${prefix}${c.name} › `)]);
}

/** The banners at the top of the home page (site and app). */
export default async function BannersAdminPage() {
  const [result, stores, categories] = await Promise.all([
    authed<BannerRow[]>("/api/admin/banners"),
    // Only live stores can be advertised (status 3 = active).
    authed<Paged<AdminStoreItem>>("/api/admin/stores", { query: { status: 3, pageSize: 100 } }),
    getCategories(),
  ]);
  if (!result.ok) return <LoadProblem what="Kampaniyalar" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Kampaniyalar (ana səhifə bannerləri)"
        hint="Saytın və mobil tətbiqin ana səhifəsinin yuxarısında göstərilir. Kampaniya yalnız şəkli olanda, «Göstərilir» seçiləndə və tarix aralığında olanda görünür. Basanda nə olacağını özün seçirsən: heç nə (kliklənmir), öz kampaniya səhifəsi (rentox.az/ad), mağaza, elan, kateqoriya, saytın başqa səhifəsi və ya xarici sayt. Kampaniya səhifəsi və mağaza/elan keçidləri mobil tətbiqdə də açılır; saytın digər səhifələrinə keçid yalnız vebdə işləyir."
      />
      <BannerManager
        banners={result.data}
        stores={stores.ok ? stores.data.items.map((s) => ({ slug: s.slug, name: s.name })) : []}
        categories={flatten(categories)}
      />
    </div>
  );
}
