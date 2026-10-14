import { notFound } from "next/navigation";
import { FieldForm } from "@/components/admin/CatalogForms";
import { PageHead } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { trName, type AdminCategory } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";

export default async function NewFieldPage({ params }: PageProps<"/admin/kateqoriyalar/[id]/sahe/yeni">) {
  const { id } = await params;
  const categories = await authed<AdminCategory[]>("/api/admin/categories");
  const category = categories.ok ? categories.data.find((c) => c.id === id) : undefined;
  if (!category) notFound();

  return (
    <div className="space-y-5">
      <PageHead
        title="Yeni sahə"
        hint={trName(category.translations)}
        back={{ href: adminHref(`/kateqoriyalar/${id}`), label: trName(category.translations) }}
      />
      <FieldForm categoryId={id} />
    </div>
  );
}
