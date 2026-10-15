import type { MetadataRoute } from "next";
import { getCategories, getListings, getStores } from "@/lib/api";
import { siteUrl } from "@/lib/format";
import type { Category } from "@/lib/types";

// Rebuilt about once an hour; listings come and go all day.
export const revalidate = 3600;

function flatten(tree: Category[]): Category[] {
  return tree.flatMap((c) => [c, ...flatten(c.children)]);
}

/** Pages a search engine should know: home, categories, stores, live listings. */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = siteUrl();
  const now = new Date();

  const [categories, firstStores] = await Promise.all([getCategories(), getStores(undefined, 1, 50)]);

  const entries: MetadataRoute.Sitemap = [
    { url: base, lastModified: now, changeFrequency: "hourly", priority: 1 },
    { url: `${base}/magazalar`, lastModified: now, changeFrequency: "daily", priority: 0.6 },
    ...["haqqimizda", "sertler", "mexfilik", "emekdasliq", "qaydalar", "suallar"].map((page) => ({
      url: `${base}/${page}`,
      lastModified: now,
      changeFrequency: "monthly" as const,
      priority: 0.3,
    })),
    ...flatten(categories).map((c) => ({
      url: `${base}/search?category=${c.id}`,
      lastModified: now,
      changeFrequency: "daily" as const,
      priority: 0.7,
    })),
    ...firstStores.items.map((s) => ({
      url: `${base}/store/${s.slug}`,
      lastModified: now,
      changeFrequency: "daily" as const,
      priority: 0.5,
    })),
  ];

  // Listings: newest first, a bounded number of pages.
  for (let page = 1; page <= 20; page++) {
    const batch = await getListings({ page, pageSize: 50 });
    entries.push(
      ...batch.items.map((l) => ({
        url: `${base}/listing/${l.id}`,
        lastModified: now,
        changeFrequency: "weekly" as const,
        priority: 0.8,
      })),
    );
    if (page >= batch.totalPages) break;
  }

  return entries;
}
