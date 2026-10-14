import { FieldType, type FieldDefinition } from "./types";

type Raw = Record<string, string | string[] | undefined>;

const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
const many = (v: string | string[] | undefined) => (Array.isArray(v) ? v : v ? [v] : []);

/** What the filter form sent, in one tidy object. */
export type Parsed = {
  q?: string;
  category?: string;
  minPrice?: string;
  maxPrice?: string;
  sort?: string;
  seller?: string;
  city?: string;
  period?: string;
  page: number;
  /** Raw parameters of the category's own fields (o_*, n_*, b_*), kept to rebuild links. */
  fieldParams: [string, string][];
};

export function parseSearch(sp: Raw): Parsed {
  const fieldParams: [string, string][] = [];
  for (const [key, value] of Object.entries(sp)) {
    if (/^(o|n|b)_/.test(key)) for (const v of many(value)) if (v !== "") fieldParams.push([key, v]);
  }
  return {
    q: one(sp.q)?.trim() || undefined,
    category: one(sp.category),
    minPrice: one(sp.minPrice),
    maxPrice: one(sp.maxPrice),
    sort: one(sp.sort),
    seller: one(sp.seller),
    city: one(sp.city),
    period: one(sp.period),
    page: Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1),
    fieldParams,
  };
}

/** Whether the address has empty or default values that a tidy one would not (the form sends them all). */
export function isUntidy(sp: Raw): boolean {
  return (
    one(sp.seller) === "all" ||
    one(sp.sort) === "date" ||
    Object.values(sp).some((v) => many(v).some((x) => x === ""))
  );
}

/** The query string for a search, without the page. */
export function toQuery(p: Parsed, extra: Record<string, string | undefined> = {}): URLSearchParams {
  const out = new URLSearchParams();
  const set = (k: string, v?: string) => v && out.set(k, v);
  set("q", p.q);
  set("category", p.category);
  set("minPrice", p.minPrice);
  set("maxPrice", p.maxPrice);
  set("sort", p.sort === "date" ? undefined : p.sort);
  set("seller", p.seller === "all" ? undefined : p.seller);
  set("city", p.city);
  set("period", p.period);
  for (const [k, v] of p.fieldParams) out.append(k, v);
  for (const [k, v] of Object.entries(extra)) {
    if (v === undefined) out.delete(k);
    else out.set(k, v);
  }
  return out;
}

const number = (raw: string | undefined): number | null => {
  if (!raw) return null;
  const n = Number(raw.replace(",", "."));
  return Number.isFinite(n) ? n : null;
};

/**
 * The `filters` value for the API: one condition per category field that has
 * something chosen (options, a number range or yes/no), in a stable order.
 */
export function fieldFiltersJson(p: Parsed, fields: FieldDefinition[]): string | undefined {
  const byKey = new Map<string, string[]>();
  for (const [k, v] of p.fieldParams) byKey.set(k, [...(byKey.get(k) ?? []), v]);

  const conditions: Record<string, unknown>[] = [];
  for (const f of fields) {
    if (f.type === FieldType.singleSelect || f.type === FieldType.multiSelect) {
      const ids = (byKey.get(`o_${f.id}`) ?? []).filter((id) => f.options?.some((o) => o.id === id));
      if (ids.length > 0) conditions.push({ fieldId: f.id, optionIds: [...ids].sort() });
    } else if (f.type === FieldType.wholeNumber || f.type === FieldType.fractionalNumber) {
      const min = number(byKey.get(`n_${f.id}_min`)?.[0]);
      const max = number(byKey.get(`n_${f.id}_max`)?.[0]);
      if (min !== null || max !== null) {
        conditions.push({ fieldId: f.id, ...(min !== null ? { min } : {}), ...(max !== null ? { max } : {}) });
      }
    } else if (f.type === FieldType.boolean) {
      const flag = byKey.get(`b_${f.id}`)?.[0];
      if (flag === "1" || flag === "0") conditions.push({ fieldId: f.id, flag: flag === "1" });
    }
  }
  if (conditions.length === 0) return undefined;
  return JSON.stringify(conditions.sort((a, b) => String(a.fieldId).localeCompare(String(b.fieldId))));
}
