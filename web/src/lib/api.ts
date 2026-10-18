import { getLocale } from "./i18n/server";
import type {
  Category,
  FieldDefinition,
  PublicStore,
  StoreCard,
  Banner,
  CampaignPage,
  PageContent,
  PageSummary,
  SiteInfo,
  City,
  ListingDetails,
  ListingSummary,
  Paged,
} from "./types";

// Used by server components only. The browser talks to /api/* on this origin
// (see next.config.ts), so nothing here ever reaches the client bundle.
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

/** The API's `language` value: the language the visitor chose (az, en or ru). */
const lang = () => getLocale();

async function get<T>(
  path: string,
  query: Record<string, string | number | undefined> = {},
  revalidate = 60,
  tags: string[] = [],
): Promise<T | null> {
  const url = new URL(path, API_BASE);
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== "") {
      url.searchParams.set(key, String(value));
    }
  }

  try {
    const res = await fetch(url, { next: { revalidate, tags } });
    if (!res.ok) return null;
    return (await res.json()) as T;
  } catch {
    // The API being down must not take the whole page down.
    return null;
  }
}

export async function getCategories(): Promise<Category[]> {
  return (
    (await get<Category[]>("/api/categories", { language: await lang() }, 300, ["catalog"])) ??
    []
  );
}

/** The fixed pages (terms, privacy, …) with their titles in the visitor's language. */
export async function getPageList(): Promise<PageSummary[]> {
  return (await get<PageSummary[]>("/api/pages", { language: await lang() }, 60, ["pages"])) ?? [];
}

export async function getPage(slug: string): Promise<PageContent | null> {
  return get<PageContent>(`/api/pages/${encodeURIComponent(slug)}`, { language: await lang() }, 60, ["pages"]);
}

/** Who runs the site and how to reach them (nothing if the API cannot say). */
export async function getSiteInfo(): Promise<SiteInfo> {
  return (await get<SiteInfo>("/api/site-info", {}, 60, ["pages"])) ?? {};
}

/** The banners on show on the home page (none if the API cannot say). */
export async function getBanners(): Promise<Banner[]> {
  return (await get<Banner[]>("/api/banners", { language: await lang(), platform: "web" }, 60, ["banners"])) ?? [];
}

/** The page of a campaign (rentox.az/name); nothing when there is none, or it is switched off. */
export async function getCampaignPage(slug: string): Promise<CampaignPage | null> {
  return get<CampaignPage>(`/api/banners/pages/${encodeURIComponent(slug)}`, { language: await lang() }, 60, ["banners"]);
}

export async function getCities(): Promise<City[]> {
  return (
    (await get<City[]>("/api/locations/cities", { language: await lang() }, 3600, ["locations"])) ?? []
  );
}

export type ListingQuery = {
  search?: string;
  categoryId?: string;
  minPrice?: string;
  maxPrice?: string;
  sort?: string;
  seller?: string;
  cityId?: string;
  rentalPeriod?: string;
  /** JSON for the `filters` parameter: conditions on the category's own fields. */
  fieldFilters?: string;
  page?: number;
  pageSize?: number;
  /** "wide": the feed for a computer screen (rows of five: 2 of VIP, 3 moved up, 3 ordinary). */
  feed?: "wide";
};

export async function getListings(
  query: ListingQuery = {},
): Promise<Paged<ListingSummary>> {
  const result = await get<Paged<ListingSummary>>("/api/listings", {
    language: await lang(),
    page: query.page ?? 1,
    pageSize: query.pageSize ?? 24,
    search: query.search,
    categoryId: query.categoryId,
    minPrice: query.minPrice,
    maxPrice: query.maxPrice,
    sort: query.sort && query.sort !== "date" ? query.sort : undefined,
    sellerType: query.seller && query.seller !== "all" ? query.seller : undefined,
    cityId: query.cityId,
    rentalPeriod: query.rentalPeriod,
    filters: query.fieldFilters,
    feed: query.feed,
  });
  return result ?? { items: [], page: 1, totalPages: 0, totalCount: 0 };
}

/** The fields of a category that search may filter by (own and inherited). */
export async function getFilterFields(categoryId: string): Promise<FieldDefinition[]> {
  const list = await get<(FieldDefinition & { isFilterable?: boolean })[]>(
    `/api/categories/${encodeURIComponent(categoryId)}/fields`,
    { language: await lang() },
    300,
    ["catalog"],
  );
  return (list ?? []).filter((f) => f.isFilterable).sort((a, b) => a.displayOrder - b.displayOrder);
}

export async function getListing(id: string): Promise<ListingDetails | null> {
  return get<ListingDetails>(`/api/listings/${encodeURIComponent(id)}`, {
    language: await lang(),
  });
}

export async function getSimilar(id: string): Promise<ListingSummary[]> {
  return (
    (await get<ListingSummary[]>(
      `/api/listings/${encodeURIComponent(id)}/similar`,
      { language: await lang() },
    )) ?? []
  );
}

/** Ids from the top category down to [id] (empty when it is not in the tree). */
export function categoryChain(tree: Category[], id: string): Category[] {
  for (const c of tree) {
    if (c.id === id) return [c];
    const below = categoryChain(c.children, id);
    if (below.length > 0) return [c, ...below];
  }
  return [];
}

export function findCategory(tree: Category[], id: string): Category | null {
  const chain = categoryChain(tree, id);
  return chain.at(-1) ?? null;
}

export function getStore(slug: string): Promise<PublicStore | null> {
  return get<PublicStore>(`/api/stores/${encodeURIComponent(slug)}`);
}

export async function getStoreListings(
  slug: string,
  page = 1,
  pageSize = 20,
  filter: { search?: string; minPrice?: string; maxPrice?: string } = {},
): Promise<Paged<ListingSummary>> {
  const result = await get<Paged<ListingSummary>>(`/api/stores/${encodeURIComponent(slug)}/listings`, {
    language: await lang(),
    page,
    pageSize,
    search: filter.search,
    minPrice: filter.minPrice,
    maxPrice: filter.maxPrice,
  });
  return result ?? { items: [], page: 1, totalPages: 0, totalCount: 0 };
}

/** The stores that paid for a place on the home page (running VIP). */
export async function getVipStores(take = 6): Promise<StoreCard[]> {
  return (await get<StoreCard[]>("/api/stores/vip", { take })) ?? [];
}

export async function getStores(search?: string, page = 1, pageSize = 24): Promise<Paged<StoreCard>> {
  const result = await get<Paged<StoreCard>>("/api/stores", { search, page, pageSize });
  return result ?? { items: [], page: 1, totalPages: 0, totalCount: 0 };
}
