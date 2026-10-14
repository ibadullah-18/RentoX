import { notFound } from "next/navigation";
import { FieldActiveButton, FieldForm, OptionsEditor } from "@/components/admin/CatalogForms";
import { LoadProblem, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { isSelectType, trName, type AdminCategory, type AdminField } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";

export default async function EditFieldPage({ params }: PageProps<"/admin/kateqoriyalar/[id]/sahe/[fieldId]">) {
  const { id, fieldId } = await params;
  const [categories, fields] = await Promise.all([
    authed<AdminCategory[]>("/api/admin/categories"),
    authed<AdminField[]>(`/api/admin/categories/${encodeURIComponent(id)}/fields`),
  ]);
  if (!categories.ok) return <LoadProblem what="Kateqoriya" status={categories.status} />;
  if (!fields.ok) return <LoadProblem what="Sahələr" status={fields.status} />;
  const category = categories.data.find((c) => c.id === id);
  const field = fields.data.find((f) => f.id === fieldId);
  if (!category || !field) notFound();

  return (
    <div className="space-y-6">
      <PageHead
        title={trName(field.translations)}
        hint={`${trName(category.translations)} · ${field.key}`}
        back={{ href: adminHref(`/kateqoriyalar/${id}`), label: trName(category.translations) }}
        actions={
          <>
            {!field.isActive && <Pill tone="muted">Deaktiv</Pill>}
            <FieldActiveButton categoryId={id} field={field} />
          </>
        }
      />
      <FieldForm key={JSON.stringify(field)} categoryId={id} field={field} />
      {isSelectType(field.type) && <OptionsEditor categoryId={id} field={field} />}
    </div>
  );
}
