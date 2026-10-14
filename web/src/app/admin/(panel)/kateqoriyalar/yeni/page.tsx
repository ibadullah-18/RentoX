import { CategoryForm } from "@/components/admin/CatalogForms";
import { PageHead } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { buildTree, flatten, trName, type AdminCategory } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";

export default async function NewCategoryPage({ searchParams }: PageProps<"/admin/kateqoriyalar/yeni">) {
  const raw = (await searchParams).parent;
  const parent = (Array.isArray(raw) ? raw[0] : raw) ?? null;

  const result = await authed<AdminCategory[]>("/api/admin/categories");
  const list = result.ok ? flatten(buildTree(result.data)) : [];
  const parents = list.map((c) => ({ value: c.id, label: `${"— ".repeat(c.depth)}${trName(c.translations)}` }));

  return (
    <div className="space-y-5">
      <PageHead title="Yeni kateqoriya" back={{ href: adminHref("/kateqoriyalar"), label: "Kateqoriyalar" }} />
      <CategoryForm parentId={parent && list.some((c) => c.id === parent) ? parent : null} parents={parents} />
    </div>
  );
}
