import { getT } from "@/lib/i18n/server";
import { unitLabel } from "@/lib/format";
import type { Parsed } from "@/lib/search-params";
import { toQuery } from "@/lib/search-params";
import { FieldType, type City, type FieldDefinition } from "@/lib/types";
import { FilterShell } from "./FilterShell";
import { Select } from "./ui/Select";

const field =
  "h-11 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary";

function Group({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <fieldset className="space-y-2">
      <legend className="mb-2 text-sm font-bold">{title}</legend>
      {children}
    </fieldset>
  );
}

/**
 * Every filter as a plain GET form (works without JavaScript): price, place,
 * period, seller, sorting, and the category's own fields. Phones fold it away
 * behind a "Filtrlər" button.
 */
export async function FilterForm({
  parsed,
  cities,
  fields,
  periods,
}: {
  parsed: Parsed;
  cities: City[];
  /** The fields of the chosen category that may be filtered. */
  fields: FieldDefinition[];
  /** The rental periods that make sense here (the category's). */
  periods: number[];
}) {
  const t = await getT();
  const selected = (key: string) => new Set(parsed.fieldParams.filter(([k]) => k === key).map(([, v]) => v));
  const value = (key: string) => parsed.fieldParams.find(([k]) => k === key)?.[1] ?? "";

  const active =
    parsed.minPrice || parsed.maxPrice || parsed.city || parsed.period || parsed.seller || parsed.fieldParams.length > 0;

  const reset = toQuery({ ...parsed, minPrice: undefined, maxPrice: undefined, city: undefined, period: undefined, seller: undefined, sort: undefined, fieldParams: [] });

  return (
    <FilterShell label={t("Filtrlər")} active={Boolean(active)}>
      <form action="/search" className="space-y-5 rounded-xl border border-line bg-surface p-4 max-lg:rounded-none max-lg:border-0 max-lg:bg-transparent max-lg:p-0">
        {parsed.q && <input type="hidden" name="q" value={parsed.q} />}
        {parsed.category && <input type="hidden" name="category" value={parsed.category} />}

        <Group title={`${t("Qiymət")} (${t("AZN")})`}>
          <div className="flex gap-2">
            <input name="minPrice" inputMode="decimal" placeholder={t("Min.")} defaultValue={parsed.minPrice} aria-label={`${t("Qiymət")}: ${t("Min.")}`} className={field} />
            <input name="maxPrice" inputMode="decimal" placeholder={t("Maks.")} defaultValue={parsed.maxPrice} aria-label={`${t("Qiymət")}: ${t("Maks.")}`} className={field} />
          </div>
        </Group>

        <div className="block">
          <span className="mb-2 block text-sm font-bold">{t("Şəhər")}</span>
          <Select
            name="city"
            defaultValue={parsed.city ?? ""}
            ariaLabel={t("Şəhər")}
            options={[{ value: "", label: t("Hamısı") }, ...cities.map((c) => ({ value: c.id, label: c.name }))]}
          />
        </div>

        {periods.length > 1 && (
          <div className="block">
            <span className="mb-2 block text-sm font-bold">{t("Müddət")}</span>
            <Select
              name="period"
              defaultValue={parsed.period ?? ""}
              ariaLabel={t("Müddət")}
              options={[
                { value: "", label: t("İstənilən") },
                ...periods.map((p) => ({ value: String(p), label: p === 6 ? t(unitLabel[6]) : `/ ${t(unitLabel[p])}` })),
              ]}
            />
          </div>
        )}

        <div className="block">
          <span className="mb-2 block text-sm font-bold">{t("Elan sahibi")}</span>
          <Select
            name="seller"
            defaultValue={parsed.seller ?? "all"}
            ariaLabel={t("Elan sahibi")}
            options={[
              { value: "all", label: t("Hamısı") },
              { value: "store", label: t("Mağaza") },
              { value: "individual", label: t("Fərdi") },
            ]}
          />
        </div>

        {fields.map((f) => {
          const label = f.label ?? f.key ?? "";
          if (f.type === FieldType.singleSelect || f.type === FieldType.multiSelect) {
            const picked = selected(`o_${f.id}`);
            return (
              <Group key={f.id} title={label}>
                <div className="max-h-48 space-y-1.5 overflow-y-auto pr-1">
                  {(f.options ?? []).map((o) => (
                    <label key={o.id} className="flex cursor-pointer items-center gap-2.5 text-[15px]">
                      <input
                        type="checkbox"
                        name={`o_${f.id}`}
                        value={o.id}
                        defaultChecked={picked.has(o.id)}
                        className="size-4 accent-[var(--primary)]"
                      />
                      {o.label}
                    </label>
                  ))}
                </div>
              </Group>
            );
          }
          if (f.type === FieldType.wholeNumber || f.type === FieldType.fractionalNumber) {
            return (
              <Group key={f.id} title={label}>
                <div className="flex gap-2">
                  <input name={`n_${f.id}_min`} inputMode="decimal" placeholder={t("Min.")} defaultValue={value(`n_${f.id}_min`)} aria-label={`${label}: ${t("Min.")}`} className={field} />
                  <input name={`n_${f.id}_max`} inputMode="decimal" placeholder={t("Maks.")} defaultValue={value(`n_${f.id}_max`)} aria-label={`${label}: ${t("Maks.")}`} className={field} />
                </div>
              </Group>
            );
          }
          if (f.type === FieldType.boolean) {
            return (
              <div key={f.id} className="block">
                <span className="mb-2 block text-sm font-bold">{label}</span>
                <Select
                  name={`b_${f.id}`}
                  defaultValue={value(`b_${f.id}`)}
                  ariaLabel={label}
                  options={[
                    { value: "", label: t("Fərq etməz") },
                    { value: "1", label: t("Bəli") },
                    { value: "0", label: t("Xeyr") },
                  ]}
                />
              </div>
            );
          }
          return null;
        })}

        <div className="block">
          <span className="mb-2 block text-sm font-bold">{t("Sıralama")}</span>
          <Select
            name="sort"
            defaultValue={parsed.sort ?? "date"}
            ariaLabel={t("Sıralama")}
            options={[
              { value: "date", label: t("Yenilər əvvəl") },
              { value: "price_asc", label: t("Əvvəlcə ucuz") },
              { value: "price_desc", label: t("Əvvəlcə bahalı") },
            ]}
          />
        </div>

        <div className="flex gap-2 max-lg:sticky max-lg:bottom-0 max-lg:-mx-4 max-lg:border-t max-lg:border-line max-lg:bg-bg max-lg:px-4 max-lg:py-3">
          <button type="submit" className="h-11 flex-1 rounded-lg bg-primary font-bold text-white hover:bg-primary-press">
            {t("Tətbiq et")}
          </button>
          <a
            href={reset.size > 0 ? `/search?${reset}` : "/search"}
            className="flex h-11 items-center rounded-lg border border-line px-4 font-semibold text-ink-2"
          >
            {t("Sıfırla")}
          </a>
        </div>
      </form>
    </FilterShell>
  );
}
