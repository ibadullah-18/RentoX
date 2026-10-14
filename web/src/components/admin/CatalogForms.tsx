"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Eye, EyeOff, Loader2, Pencil, Plus, Trash2 } from "lucide-react";
import {
  createCategory,
  createField,
  saveOption,
  setCategoryActive,
  setFieldActive,
  setOptionActive,
  updateCategory,
  updateField,
  type FieldInput,
  type OptionInput,
  type Saved,
} from "@/app/admin/(panel)/catalog-actions";
import { adminHref } from "@/lib/admin-path";
import {
  LANGS,
  fieldTypeLabel,
  isSelectType,
  periodLabel,
  slugify,
  trText,
  type AdminCategory,
  type AdminField,
  type AdminFieldOption,
} from "@/lib/admin-catalog";
import { Select } from "@/components/ui/Select";
import { CATEGORY_ICONS } from "@/lib/category-icon";
import { CategoryIcon, isIconLink } from "@/components/CategoryIcon";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";
const label = "mb-1 block text-sm font-bold";
const primary =
  "flex h-10 items-center justify-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60";
const ghost =
  "flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary disabled:opacity-60";

type Names = { az: string; ru: string; en: string };
const emptyNames: Names = { az: "", ru: "", en: "" };
const namesOf = (list: { language: number; text: string }[]): Names => ({
  az: trText(list, 1),
  ru: trText(list, 2),
  en: trText(list, 3),
});

/** Runs a save, shows the problem if there is one, and tells the caller when it worked. */
function useSave() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  async function run(action: () => Promise<Saved>, onOk?: (id?: string) => void) {
    if (busy) return;
    setBusy(true);
    setError(null);
    setDone(false);
    const res = await action();
    setBusy(false);
    if (res.ok) {
      setDone(true);
      router.refresh();
      onOk?.(res.id);
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return { busy, error, done, run, setError };
}

function Feedback({ error, done }: { error: string | null; done: boolean }) {
  if (error) {
    return (
      <p role="alert" className="text-sm font-semibold text-danger">
        {error}
      </p>
    );
  }
  return done ? (
    <p role="status" className="text-sm font-semibold text-success">
      Saxlanıldı.
    </p>
  ) : null;
}

function NamesInputs({
  names,
  onChange,
  disabled,
  title,
}: {
  names: Names;
  onChange: (n: Names) => void;
  disabled: boolean;
  title: string;
}) {
  return (
    <div className="grid gap-3 sm:grid-cols-3">
      {LANGS.map((l) => (
        <label key={l.code} className="block">
          <span className={label}>
            {title} · {l.code.toUpperCase()}
          </span>
          <input
            value={names[l.code]}
            onChange={(e) => onChange({ ...names, [l.code]: e.target.value })}
            disabled={disabled}
            maxLength={150}
            className={input}
          />
        </label>
      ))}
    </div>
  );
}

function Check({
  checked,
  onChange,
  children,
  hint,
  disabled,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  children: React.ReactNode;
  hint?: string;
  disabled?: boolean;
}) {
  return (
    <label className="flex cursor-pointer items-start gap-2.5 text-sm">
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-0.5 size-4 accent-[var(--primary)]"
      />
      <span>
        <span className="font-semibold">{children}</span>
        {hint && <span className="block text-xs text-ink-2">{hint}</span>}
      </span>
    </label>
  );
}

// ---- categories ---------------------------------------------------------------

export function CategoryForm({
  category,
  parentId,
  parents,
}: {
  category?: AdminCategory;
  /** For a new category: the one it is created under (the top level when null). */
  parentId: string | null;
  /** The categories this one may sit under, as { value, label }. */
  parents: { value: string; label: string }[];
}) {
  const router = useRouter();
  const { busy, error, done, run } = useSave();
  const [names, setNames] = useState<Names>(category ? namesOf(category.translations) : emptyNames);
  const [slug, setSlug] = useState(category?.slug ?? "");
  const [slugTouched, setSlugTouched] = useState(Boolean(category));
  const [order, setOrder] = useState(String(category?.displayOrder ?? 0));
  const [icon, setIcon] = useState(category?.iconUrl ?? "");
  const [parent, setParent] = useState<string>(category ? (category.parentId ?? "") : (parentId ?? ""));
  const [periods, setPeriods] = useState<number[]>(category?.rentalPeriods ?? []);

  const data = () => ({
    parentId: parent || null,
    slug,
    iconUrl: icon,
    displayOrder: Number.parseInt(order, 10) || 0,
    names,
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (category) void run(() => updateCategory(category.id, data(), periods));
        else void run(() => createCategory(data()), (id) => router.push(adminHref(id ? `/kateqoriyalar/${id}` : "/kateqoriyalar")));
      }}
      className="space-y-4 rounded-xl border border-line bg-surface p-4 md:p-5"
    >
      <NamesInputs
        names={names}
        disabled={busy}
        title="Ad"
        onChange={(n) => {
          setNames(n);
          if (!slugTouched) setSlug(slugify(n.en || n.az));
        }}
      />

      <div className="grid gap-3 sm:grid-cols-3">
        <label className="block">
          <span className={label}>Slug (linkdə görünən söz)</span>
          <input
            value={slug}
            onChange={(e) => {
              setSlug(e.target.value);
              setSlugTouched(true);
            }}
            disabled={busy}
            maxLength={100}
            className={input}
          />
        </label>
        <label className="block">
          <span className={label}>Sıra nömrəsi</span>
          <input value={order} onChange={(e) => setOrder(e.target.value)} inputMode="numeric" disabled={busy} className={input} />
        </label>
        <div className="block">
          <span className={label}>Üst kateqoriya</span>
          <Select
            value={parent}
            onChange={setParent}
            disabled={busy}
            ariaLabel="Üst kateqoriya"
            options={[{ value: "", label: "— Əsas kateqoriya —" }, ...parents]}
          />
        </div>
      </div>

      <fieldset className="space-y-2">
        <legend className={label}>İkon</legend>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => setIcon("")}
            disabled={busy}
            aria-pressed={icon === ""}
            className={`flex h-10 items-center rounded-lg border px-3 text-sm font-semibold ${
              icon === "" ? "border-primary bg-primary-soft text-primary" : "border-line hover:border-primary"
            }`}
          >
            Avtomatik
          </button>
          {CATEGORY_ICONS.map(({ key, label: text, Icon }) => (
            <button
              key={key}
              type="button"
              onClick={() => setIcon(key)}
              disabled={busy}
              aria-pressed={icon === key}
              aria-label={text}
              title={text}
              className={`flex size-10 items-center justify-center rounded-lg border ${
                icon === key ? "border-primary bg-primary-soft text-primary" : "border-line text-ink-2 hover:border-primary hover:text-primary"
              }`}
            >
              <Icon className="size-5" aria-hidden />
            </button>
          ))}
        </div>
        <label className="block">
          <span className="mb-1 block text-xs font-semibold text-ink-2">Və ya öz şəklinizin linki (https://… .png, .svg, .webp)</span>
          <div className="flex items-center gap-2">
            <input
              value={isIconLink(icon) ? icon : ""}
              onChange={(e) => setIcon(e.target.value)}
              placeholder="https://"
              disabled={busy}
              maxLength={500}
              className={input}
            />
            {isIconLink(icon) && (
              <span className="flex size-10 shrink-0 items-center justify-center rounded-lg border border-line bg-primary-soft">
                <CategoryIcon key={icon} slug={slug || "x"} iconUrl={icon} className="size-6" />
              </span>
            )}
          </div>
        </label>
        <p className="text-xs text-ink-2">
          Sayt və mobil tətbiq bu ikonu göstərir. “Avtomatik” seçilsə, ikon slug-dan təxmin edilir (məsələn, adında “car” olan maşın ikonu alır).
        </p>
      </fieldset>

      {category && (
        <fieldset className="space-y-2">
          <legend className={label}>İcazə verilən kirayə müddətləri</legend>
          <div className="flex flex-wrap gap-x-5 gap-y-2">
            {Object.entries(periodLabel).map(([v, text]) => {
              const n = Number(v);
              return (
                <Check
                  key={v}
                  checked={periods.includes(n)}
                  disabled={busy}
                  onChange={(on) => setPeriods((p) => (on ? [...p, n] : p.filter((x) => x !== n)))}
                >
                  {text}
                </Check>
              );
            })}
          </div>
          <p className="text-xs text-ink-2">Heç biri seçilməsə, üst kateqoriyanın seçimi (və ya hamısı) keçərlidir.</p>
        </fieldset>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy} className={primary}>
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {category ? "Saxla" : "Kateqoriya yarat"}
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

export function CategoryActiveButton({ id, isActive }: { id: string; isActive: boolean }) {
  const { busy, error, run } = useSave();
  return (
    <div className="flex items-center gap-3">
      <button type="button" disabled={busy} onClick={() => void run(() => setCategoryActive(id, !isActive))} className={ghost}>
        {isActive ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
        {isActive ? "Deaktiv et" : "Aktiv et"}
      </button>
      {error && <span className="text-sm font-semibold text-danger">{error}</span>}
    </div>
  );
}

// ---- fields ---------------------------------------------------------------------

type OptionRow = { value: string; order: string; labels: Names };

function OptionsBuilder({ rows, onChange, disabled }: { rows: OptionRow[]; onChange: (r: OptionRow[]) => void; disabled: boolean }) {
  const set = (i: number, patch: Partial<OptionRow>) => onChange(rows.map((r, n) => (n === i ? { ...r, ...patch } : r)));
  return (
    <div className="space-y-3">
      {rows.map((r, i) => (
        <div key={i} className="rounded-lg border border-line p-3">
          <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr_110px_auto]">
            {LANGS.map((l) => (
              <input
                key={l.code}
                value={r.labels[l.code]}
                onChange={(e) => {
                  const labels = { ...r.labels, [l.code]: e.target.value };
                  set(i, { labels, value: r.value || slugify(labels.en, "_") });
                }}
                placeholder={`Ad · ${l.code.toUpperCase()}`}
                disabled={disabled}
                className={input}
              />
            ))}
            <input
              value={r.value}
              onChange={(e) => set(i, { value: e.target.value })}
              placeholder="açar"
              disabled={disabled}
              className={input}
              aria-label="Açar"
            />
            <button
              type="button"
              onClick={() => onChange(rows.filter((_, n) => n !== i))}
              disabled={disabled}
              aria-label="Sil"
              className="flex size-10 items-center justify-center rounded-lg text-ink-2 hover:bg-danger/10 hover:text-danger"
            >
              <Trash2 className="size-4" />
            </button>
          </div>
        </div>
      ))}
      <button
        type="button"
        disabled={disabled}
        onClick={() => onChange([...rows, { value: "", order: String((rows.length + 1) * 10), labels: emptyNames }])}
        className={ghost}
      >
        <Plus className="size-4" aria-hidden />
        Seçim əlavə et
      </button>
    </div>
  );
}

export function FieldForm({ categoryId, field }: { categoryId: string; field?: AdminField }) {
  const router = useRouter();
  const { busy, error, done, run } = useSave();
  const [labels, setLabels] = useState<Names>(field ? namesOf(field.translations) : emptyNames);
  const [key, setKey] = useState(field?.key ?? "");
  const [keyTouched, setKeyTouched] = useState(Boolean(field));
  const [type, setType] = useState(String(field?.type ?? 5));
  const [order, setOrder] = useState(String(field?.displayOrder ?? 10));
  const [required, setRequired] = useState(field?.isRequired ?? false);
  const [filterable, setFilterable] = useState(field?.isFilterable ?? true);
  const [searchable, setSearchable] = useState(field?.isSearchable ?? false);
  const [custom, setCustom] = useState(field?.allowCustomValue ?? false);
  const [descendants, setDescendants] = useState(field?.appliesToDescendants ?? true);
  const [rows, setRows] = useState<OptionRow[]>([]);

  const select = isSelectType(Number(type));
  const data = (): FieldInput => ({
    key,
    type: Number(type),
    isRequired: required,
    isFilterable: filterable,
    isSearchable: searchable,
    allowCustomValue: select && custom,
    appliesToDescendants: descendants,
    displayOrder: Number.parseInt(order, 10) || 0,
    labels,
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (field) void run(() => updateField(categoryId, field.id, data()));
        else {
          const options: OptionInput[] = select
            ? rows.map((r, i) => ({ value: r.value, displayOrder: Number.parseInt(r.order, 10) || (i + 1) * 10, labels: r.labels }))
            : [];
          void run(
            () => createField(categoryId, data(), options),
            (id) => router.push(adminHref(id ? `/kateqoriyalar/${categoryId}/sahe/${id}` : `/kateqoriyalar/${categoryId}`)),
          );
        }
      }}
      className="space-y-4 rounded-xl border border-line bg-surface p-4 md:p-5"
    >
      <NamesInputs
        names={labels}
        disabled={busy}
        title="Sahənin adı"
        onChange={(n) => {
          setLabels(n);
          if (!keyTouched) setKey(slugify(n.en || n.az, "_"));
        }}
      />

      <div className="grid gap-3 sm:grid-cols-3">
        <label className="block">
          <span className={label}>Açar (texniki ad)</span>
          <input
            value={key}
            onChange={(e) => {
              setKey(e.target.value);
              setKeyTouched(true);
            }}
            disabled={busy}
            maxLength={100}
            className={input}
          />
        </label>
        <div className="block">
          <span className={label}>Tip</span>
          <Select
            value={type}
            onChange={setType}
            disabled={busy || Boolean(field)}
            ariaLabel="Tip"
            options={Object.entries(fieldTypeLabel).map(([value, text]) => ({ value, label: text }))}
          />
        </div>
        <label className="block">
          <span className={label}>Sıra nömrəsi</span>
          <input value={order} onChange={(e) => setOrder(e.target.value)} inputMode="numeric" disabled={busy} className={input} />
        </label>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <Check checked={required} onChange={setRequired} disabled={busy} hint="Elan yaradanda doldurmaq məcburidir">
          Məcburi
        </Check>
        <Check checked={filterable} onChange={setFilterable} disabled={busy} hint="Axtarış filtrində göstərilir">
          Filtrdə göstərilsin
        </Check>
        <Check checked={searchable} onChange={setSearchable} disabled={busy} hint="Yazılı axtarış bu sahənin dəyərində də axtarır">
          Mətn axtarışında işləsin
        </Check>
        <Check checked={descendants} onChange={setDescendants} disabled={busy} hint="Alt kateqoriyalarda da görünür">
          Alt kateqoriyalara keçsin
        </Check>
        {select && (
          <Check checked={custom} onChange={setCustom} disabled={busy} hint="Siyahıda olmayanı özü yaza bilər (hamıya görünən seçim yaranmır)">
            İstifadəçi öz dəyərini yaza bilsin
          </Check>
        )}
      </div>

      {!field && select && (
        <div>
          <span className={label}>Seçimlər</span>
          <OptionsBuilder rows={rows} onChange={setRows} disabled={busy} />
        </div>
      )}
      {field && select && <p className="text-xs text-ink-2">Seçimləri aşağıdakı siyahıdan idarə edin.</p>}
      {field && <p className="text-xs text-ink-2">Sahənin tipi sonradan dəyişdirilmir.</p>}

      <div className="flex flex-wrap items-center gap-3">
        <button type="submit" disabled={busy} className={primary}>
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {field ? "Saxla" : "Sahə yarat"}
        </button>
        <Feedback error={error} done={done} />
      </div>
    </form>
  );
}

export function FieldActiveButton({ categoryId, field }: { categoryId: string; field: AdminField }) {
  const { busy, error, run } = useSave();
  return (
    <div className="flex items-center gap-3">
      <button
        type="button"
        disabled={busy}
        onClick={() => void run(() => setFieldActive(categoryId, field.id, !field.isActive))}
        className={ghost}
      >
        {field.isActive ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
        {field.isActive ? "Deaktiv et" : "Aktiv et"}
      </button>
      {error && <span className="text-sm font-semibold text-danger">{error}</span>}
    </div>
  );
}

// ---- options of an existing field ----------------------------------------------------

function OptionLine({ categoryId, fieldId, option }: { categoryId: string; fieldId: string; option: AdminFieldOption }) {
  const { busy, error, run } = useSave();
  const [editing, setEditing] = useState(false);
  const [names, setNames] = useState<Names>(namesOf(option.translations));
  const [value, setValue] = useState(option.value);
  const [order, setOrder] = useState(String(option.displayOrder));

  if (!editing) {
    return (
      <li className="flex flex-wrap items-center gap-3 px-4 py-2.5">
        <span className={`min-w-0 flex-1 ${option.isActive ? "" : "text-ink-2 line-through"}`}>
          <span className="font-semibold">{names.az || option.value}</span>
          <span className="ml-2 text-xs text-ink-2">
            {names.ru} · {names.en} · <span className="font-mono">{option.value}</span>
          </span>
        </span>
        <button type="button" onClick={() => setEditing(true)} className={ghost}>
          <Pencil className="size-4" aria-hidden />
          Dəyiş
        </button>
        <button
          type="button"
          disabled={busy}
          onClick={() => void run(() => setOptionActive(categoryId, fieldId, option.id, !option.isActive))}
          className={ghost}
        >
          {option.isActive ? "Deaktiv et" : "Aktiv et"}
        </button>
        {error && <span className="w-full text-sm font-semibold text-danger">{error}</span>}
      </li>
    );
  }

  return (
    <li className="space-y-2 px-4 py-3">
      <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr_1fr_90px]">
        {LANGS.map((l) => (
          <input
            key={l.code}
            value={names[l.code]}
            onChange={(e) => setNames({ ...names, [l.code]: e.target.value })}
            placeholder={`Ad · ${l.code.toUpperCase()}`}
            disabled={busy}
            className={input}
          />
        ))}
        <input value={value} onChange={(e) => setValue(e.target.value)} placeholder="açar" disabled={busy} className={input} aria-label="Açar" />
        <input value={order} onChange={(e) => setOrder(e.target.value)} inputMode="numeric" disabled={busy} className={input} aria-label="Sıra" />
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          disabled={busy}
          onClick={() =>
            void run(
              () => saveOption(categoryId, fieldId, option.id, { value, displayOrder: Number.parseInt(order, 10) || 0, labels: names }),
              () => setEditing(false),
            )
          }
          className={primary}
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Saxla
        </button>
        <button type="button" onClick={() => setEditing(false)} className={ghost}>
          Ləğv et
        </button>
        {error && <span className="text-sm font-semibold text-danger">{error}</span>}
      </div>
    </li>
  );
}

export function OptionsEditor({ categoryId, field }: { categoryId: string; field: AdminField }) {
  const router = useRouter();
  const { busy, error, run } = useSave();
  const [adding, setAdding] = useState(false);
  const [names, setNames] = useState<Names>(emptyNames);
  const [value, setValue] = useState("");
  const [order, setOrder] = useState(String((field.options.length + 1) * 10));

  return (
    <div className="overflow-hidden rounded-xl border border-line bg-surface">
      <h2 className="border-b border-line px-4 py-3 text-[15px] font-extrabold">Seçimlər ({field.options.length})</h2>
      <ul className="divide-y divide-line">
        {field.options.length === 0 && <li className="px-4 py-6 text-center text-sm text-ink-2">Hələ seçim yoxdur.</li>}
        {field.options.map((o) => (
          <OptionLine key={`${o.id}-${o.isActive}-${o.translations.length}`} categoryId={categoryId} fieldId={field.id} option={o} />
        ))}
      </ul>
      <div className="border-t border-line p-4">
        {!adding ? (
          <button type="button" onClick={() => setAdding(true)} className={ghost}>
            <Plus className="size-4" aria-hidden />
            Seçim əlavə et
          </button>
        ) : (
          <div className="space-y-2">
            <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr_1fr_90px]">
              {LANGS.map((l) => (
                <input
                  key={l.code}
                  value={names[l.code]}
                  onChange={(e) => {
                    const next = { ...names, [l.code]: e.target.value };
                    setNames(next);
                    if (!value || value === slugify(names.en, "_")) setValue(slugify(next.en, "_"));
                  }}
                  placeholder={`Ad · ${l.code.toUpperCase()}`}
                  disabled={busy}
                  className={input}
                />
              ))}
              <input value={value} onChange={(e) => setValue(e.target.value)} placeholder="açar" disabled={busy} className={input} aria-label="Açar" />
              <input value={order} onChange={(e) => setOrder(e.target.value)} inputMode="numeric" disabled={busy} className={input} aria-label="Sıra" />
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                disabled={busy}
                onClick={() =>
                  void run(
                    () => saveOption(categoryId, field.id, null, { value, displayOrder: Number.parseInt(order, 10) || 0, labels: names }),
                    () => {
                      setAdding(false);
                      setNames(emptyNames);
                      setValue("");
                      router.refresh();
                    },
                  )
                }
                className={primary}
              >
                {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
                Əlavə et
              </button>
              <button type="button" onClick={() => setAdding(false)} className={ghost}>
                Ləğv et
              </button>
              {error && <span className="text-sm font-semibold text-danger">{error}</span>}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
