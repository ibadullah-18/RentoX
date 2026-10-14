import type { Metadata } from "next";
import Link from "next/link";
import { BackButton } from "@/components/BackButton";
import { getCategories } from "@/lib/api";
import { categoryIcon, tints } from "@/lib/category-icon";
import { getT } from "@/lib/i18n/server";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Bütün kateqoriyalar") };
}

/** Every top category as a square. A tap leads into the category, step by step. */
export default async function CategoriesPage() {
  const t = await getT();
  const categories = await getCategories();

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-3">
        <BackButton fallback="/" />
        <h1 className="text-xl font-extrabold md:text-2xl">{t("Bütün kateqoriyalar")}</h1>
      </div>
      <ul className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-8">
        {categories.map((c, i) => {
          const Icon = categoryIcon(c.slug);
          return (
            <li key={c.id}>
              <Link
                href={`/search?category=${c.id}`}
                className={`flex aspect-square flex-col items-center justify-center gap-2 rounded-xl p-2 text-center transition-transform hover:-translate-y-0.5 ${tints[i % tints.length]}`}
              >
                <Icon className="size-8" aria-hidden />
                <span className="line-clamp-2 text-[13px] font-bold leading-tight">{c.name}</span>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
