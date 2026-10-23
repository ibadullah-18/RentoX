import Link from "next/link";
import { notFound } from "next/navigation";
import { Plus } from "lucide-react";
import { CategoryActiveButton, CategoryDeleteButton, CategoryForm } from "@/components/admin/CatalogForms";
import { Card, LoadProblem, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import {
  buildTree,
  descendantIds,
  fieldTypeLabel,
  flatten,
  trName,
  type AdminCategory,
  type AdminField,
} from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";

export default async function EditCategoryPage({ params }: PageProps<"/admin/kateqoriyalar/[id]">) {
  const { id } = await params;
  const [categories, fields] = await Promise.all([
    authed<AdminCategory[]>("/api/admin/categories"),
    authed<AdminField[]>(`/api/admin/categories/${encodeURIComponent(id)}/fields`),
  ]);
  if (!categories.ok) return <LoadProblem what="Kateqoriya" status={categories.status} />;

  const tree = buildTree(categories.data);
  const flat = flatten(tree);
  const category = flat.find((c) => c.id === id);
  if (!category) notFound();

  // A category cannot move under itself or anything below it.
  const blocked = descendantIds(tree, id);
  const parents = flat
    .filter((c) => !blocked.has(c.id))
    .map((c) => ({ value: c.id, label: `${"— ".repeat(c.depth)}${trName(c.translations)}` }));

  const chain: string[] = [];
  for (let cur: AdminCategory | undefined = category; cur; cur = flat.find((c) => c.id === cur?.parentId)) {
    chain.unshift(trName(cur.translations));
  }

  const list = fields.ok ? fields.data : [];

  return (
    <div className="space-y-6">
      <PageHead
        title={trName(category.translations)}
        hint={chain.join(" › ")}
        back={{ href: adminHref("/kateqoriyalar"), label: "Kateqoriyalar" }}
        actions={
          <>
            {!category.isActive && <Pill tone="muted">Deaktiv</Pill>}
            <CategoryActiveButton id={category.id} isActive={category.isActive} />
            <CategoryDeleteButton id={category.id} name={trName(category.translations)} />
          </>
        }
      />

      <CategoryForm key={JSON.stringify(category)} category={category} parentId={category.parentId} parents={parents} />

      <Card title={`Sahələr (${list.length})`}>
        <ul className="divide-y divide-line">
          {list.length === 0 && <li className="px-4 py-6 text-center text-sm text-ink-2">Bu kateqoriyanın öz sahəsi yoxdur.</li>}
          {list.map((f) => (
            <li key={f.id}>
              <Link
                href={adminHref(`/kateqoriyalar/${id}/sahe/${f.id}`)}
                className="flex flex-wrap items-center gap-2 px-4 py-2.5 hover:bg-primary-soft"
              >
                <span className={`min-w-0 flex-1 ${f.isActive ? "" : "text-ink-2 line-through"}`}>
                  <span className="font-semibold">{trName(f.translations)}</span>
                  <span className="ml-2 font-mono text-xs text-ink-2">{f.key}</span>
                </span>
                <span className="text-xs text-ink-2">{fieldTypeLabel[f.type]}</span>
                {f.isRequired && <Pill tone="warning">Məcburi</Pill>}
                {f.isFilterable && <Pill tone="primary">Filtr</Pill>}
                {!f.isActive && <Pill tone="muted">Deaktiv</Pill>}
              </Link>
            </li>
          ))}
        </ul>
        <div className="border-t border-line p-4">
          <Link
            href={adminHref(`/kateqoriyalar/${id}/sahe/yeni`)}
            className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary"
          >
            <Plus className="size-4" aria-hidden />
            Yeni sahə
          </Link>
        </div>
      </Card>
      <p className="text-xs text-ink-2">
        Üst kateqoriyadan keçən sahələr burada görünmür; onları üst kateqoriyanın səhifəsində idarə edin. Köhnə elanlarda yeni sahənin
        dəyəri avtomatik dolmur.
      </p>
    </div>
  );
}
