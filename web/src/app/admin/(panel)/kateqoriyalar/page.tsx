import Link from "next/link";
import { FolderTree, Plus } from "lucide-react";
import { Empty, LoadProblem, PageHead, Pill } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { CategoryIcon } from "@/components/CategoryIcon";
import { buildTree, flatten, trName, type AdminCategory } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";

/** The whole catalogue as a tree, deactivated categories included. */
export default async function CategoriesAdminPage() {
  const result = await authed<AdminCategory[]>("/api/admin/categories");
  if (!result.ok) return <LoadProblem what="Kateqoriyalar" status={result.status} />;
  const rows = flatten(buildTree(result.data));

  return (
    <div className="space-y-5">
      <PageHead
        title="Kateqoriyalar"
        hint="Kateqoriyalar, onların sahələri (filtrlər və elan formasındakı xanalar) və seçimləri. Dəyişikliklər saytda bir neçə dəqiqəyə görünür."
        actions={
          <Link
            href={adminHref("/kateqoriyalar/yeni")}
            className="flex h-9 items-center gap-1.5 rounded-lg bg-primary px-3.5 text-sm font-bold text-white hover:bg-primary-press"
          >
            <Plus className="size-4" aria-hidden />
            Yeni kateqoriya
          </Link>
        }
      />

      {rows.length === 0 ? (
        <Empty icon={FolderTree} text="Kateqoriya yoxdur" />
      ) : (
        <div className="overflow-hidden rounded-xl border border-line bg-surface">
          <ul className="divide-y divide-line">
            {rows.map((c) => {
              return (
              <li key={c.id}>
                <div className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft" style={{ paddingLeft: 16 + c.depth * 24 }}>
                  <CategoryIcon slug={c.slug} iconUrl={c.iconUrl} className="size-4 shrink-0 text-primary" />
                  <Link href={adminHref(`/kateqoriyalar/${c.id}`)} className="min-w-0 flex-1">
                    <span className={`font-semibold ${c.isActive ? "" : "text-ink-2 line-through"}`}>{trName(c.translations)}</span>
                    <span className="ml-2 font-mono text-xs text-ink-2">{c.slug}</span>
                  </Link>
                  {!c.isActive && <Pill tone="muted">Deaktiv</Pill>}
                  {c.children.length > 0 && <span className="text-xs text-ink-2">{c.children.length} alt</span>}
                  <Link
                    href={adminHref(`/kateqoriyalar/yeni?parent=${c.id}`)}
                    className="text-xs font-bold text-primary hover:underline"
                  >
                    + alt kateqoriya
                  </Link>
                </div>
              </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}
