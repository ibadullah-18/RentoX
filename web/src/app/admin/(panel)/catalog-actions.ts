"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { isAdmin } from "@/lib/session";

export type Saved = { ok: true; id?: string } | { ok: false; unauthorized: boolean; message: string };

const ID = /^[0-9a-f-]{36}$/i;
const bad: Saved = { ok: false, unauthorized: false, message: "Yanlış sorğu." };

/** The API's explanations (English) as an admin should read them. */
const known: [RegExp, string][] = [
  [/slug already exists/i, "Bu slug artıq mövcuddur."],
  [/key already exists/i, "Bu açar bu kateqoriyada artıq mövcuddur."],
  [/translations are required/i, "Azərbaycan, rus və ingilis dilində adlar məcburidir."],
  [/cycle/i, "Kateqoriya öz alt kateqoriyasının içinə köçürülə bilməz."],
  [/parent category was not found/i, "Üst kateqoriya tapılmadı."],
  [/must remain a selection field/i, "Seçimləri olan sahə seçim tipində qalmalıdır."],
  [/custom values are only valid/i, "Öz dəyər yalnız seçim sahələrində mümkündür."],
  [/has sub-categories/i, "Bu kateqoriyanın alt kateqoriyaları var: əvvəlcə onları silin və ya köçürün."],
  [/category has listings/i, "Bu kateqoriyada elan var, silmək olmaz: əvəzinə deaktiv edin."],
  [/still used elsewhere/i, "Kateqoriya başqa yerdə istifadə olunur, silmək olmaz: əvəzinə deaktiv edin."],
  [/not found/i, "Qeyd tapılmadı."],
  [/duplicate|already/i, "Bu qeyd artıq mövcuddur."],
];

function fail(status: number, message: string): Saved {
  if (status === 401 || status === 403) return { ok: false, unauthorized: true, message: "" };
  const text = known.find(([re]) => re.test(message))?.[1];
  if (text) return { ok: false, unauthorized: false, message: text };
  if (status === 0) return { ok: false, unauthorized: false, message: "Serverə qoşulmaq olmadı." };
  return { ok: false, unauthorized: false, message: "Əməliyyat alınmadı. Yazılanları yoxlayın." };
}

async function send(
  path: string,
  method: "POST" | "PUT" | "PATCH" | "DELETE",
  body: unknown,
  paths: string[],
): Promise<Saved> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const res = await authed<{ id?: string }>(path, { method, body });
  for (const p of paths) revalidatePath(p);
  // The public site keeps the catalogue for a few minutes: drop that copy now so the change shows at once.
  if (res.ok) updateTag("catalog");
  return res.ok ? { ok: true, id: res.data?.id } : fail(res.status, res.message);
}

export type CategoryInput = {
  parentId: string | null;
  slug: string;
  iconUrl: string;
  displayOrder: number;
  names: { az: string; ru: string; en: string };
};

function categoryBody(i: CategoryInput) {
  return {
    parentId: i.parentId,
    slug: i.slug.trim(),
    iconUrl: i.iconUrl.trim() || null,
    displayOrder: i.displayOrder,
    translations: [
      { language: 1, name: i.names.az.trim() },
      { language: 2, name: i.names.ru.trim() },
      { language: 3, name: i.names.en.trim() },
    ],
  };
}

function checkCategory(i: CategoryInput): Saved | null {
  if (!i.names.az.trim() || !i.names.ru.trim() || !i.names.en.trim()) {
    return { ok: false, unauthorized: false, message: "Üç dildə ad yazın." };
  }
  if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(i.slug.trim())) {
    return { ok: false, unauthorized: false, message: "Slug yalnız kiçik latın hərfləri, rəqəmlər və tire ola bilər." };
  }
  const icon = i.iconUrl.trim();
  if (icon && !/^[a-z]+$/.test(icon) && !/^https?:\/\/\S+$/i.test(icon)) {
    return { ok: false, unauthorized: false, message: "İkon linki https:// ilə başlamalıdır." };
  }
  return null;
}

export async function createCategory(input: CategoryInput): Promise<Saved> {
  const problem = checkCategory(input);
  if (problem) return problem;
  return send("/api/admin/categories", "POST", categoryBody(input), ["/admin/kateqoriyalar"]);
}

export async function updateCategory(id: string, input: CategoryInput, periods: number[]): Promise<Saved> {
  if (!ID.test(id)) return bad;
  const problem = checkCategory(input);
  if (problem) return problem;
  const saved = await send(`/api/admin/categories/${id}`, "PUT", categoryBody(input), [
    "/admin/kateqoriyalar",
    `/admin/kateqoriyalar/${id}`,
  ]);
  if (!saved.ok) return saved;
  return send(
    `/api/admin/categories/${id}/rental-periods`,
    "PUT",
    { periods: periods.filter((p) => p >= 1 && p <= 6) },
    ["/admin/kateqoriyalar", `/admin/kateqoriyalar/${id}`],
  );
}

export async function setCategoryActive(id: string, isActive: boolean): Promise<Saved> {
  if (!ID.test(id)) return bad;
  return send(`/api/admin/categories/${id}/status`, "PATCH", { isActive }, ["/admin/kateqoriyalar", `/admin/kateqoriyalar/${id}`]);
}

/** Deletes a category for good: only one with no listing and no sub-category. */
export async function deleteCategory(id: string): Promise<Saved> {
  if (!ID.test(id)) return bad;
  return send(`/api/admin/categories/${id}`, "DELETE", undefined, ["/admin/kateqoriyalar"]);
}

export type FieldInput = {
  key: string;
  type: number;
  isRequired: boolean;
  isFilterable: boolean;
  isSearchable: boolean;
  allowCustomValue: boolean;
  appliesToDescendants: boolean;
  displayOrder: number;
  labels: { az: string; ru: string; en: string };
};

export type OptionInput = {
  value: string;
  displayOrder: number;
  labels: { az: string; ru: string; en: string };
};

const labels = (l: { az: string; ru: string; en: string }) => [
  { language: 1, label: l.az.trim() },
  { language: 2, label: l.ru.trim() },
  { language: 3, label: l.en.trim() },
];

function checkLabels(l: { az: string; ru: string; en: string }): Saved | null {
  return l.az.trim() && l.ru.trim() && l.en.trim()
    ? null
    : { ok: false, unauthorized: false, message: "Üç dildə ad yazın." };
}

function fieldBody(i: FieldInput) {
  return {
    key: i.key.trim(),
    type: i.type,
    isRequired: i.isRequired,
    isFilterable: i.isFilterable,
    isSearchable: i.isSearchable,
    allowCustomValue: i.allowCustomValue,
    appliesToDescendants: i.appliesToDescendants,
    displayOrder: i.displayOrder,
    translations: labels(i.labels),
  };
}

function checkField(i: FieldInput): Saved | null {
  if (!/^[a-z][a-z0-9_]*$/.test(i.key.trim())) {
    return { ok: false, unauthorized: false, message: "Açar kiçik latın hərfi ilə başlamalı, yalnız hərf, rəqəm və _ ola bilər." };
  }
  return checkLabels(i.labels);
}

export async function createField(categoryId: string, input: FieldInput, options: OptionInput[]): Promise<Saved> {
  if (!ID.test(categoryId)) return bad;
  const problem = checkField(input);
  if (problem) return problem;
  for (const o of options) {
    const p = checkLabels(o.labels);
    if (p) return p;
    if (!o.value.trim()) return { ok: false, unauthorized: false, message: "Hər seçimin açarı (dəyəri) olmalıdır." };
  }
  return send(
    `/api/admin/categories/${categoryId}/fields`,
    "POST",
    {
      ...fieldBody(input),
      options: options.map((o) => ({ value: o.value.trim(), displayOrder: o.displayOrder, translations: labels(o.labels) })),
    },
    [`/admin/kateqoriyalar/${categoryId}`],
  );
}

export async function updateField(categoryId: string, fieldId: string, input: FieldInput): Promise<Saved> {
  if (!ID.test(categoryId) || !ID.test(fieldId)) return bad;
  const problem = checkField(input);
  if (problem) return problem;
  return send(`/api/admin/categories/${categoryId}/fields/${fieldId}`, "PUT", fieldBody(input), [
    `/admin/kateqoriyalar/${categoryId}`,
    `/admin/kateqoriyalar/${categoryId}/sahe/${fieldId}`,
  ]);
}

export async function setFieldActive(categoryId: string, fieldId: string, isActive: boolean): Promise<Saved> {
  if (!ID.test(categoryId) || !ID.test(fieldId)) return bad;
  return send(`/api/admin/categories/${categoryId}/fields/${fieldId}/status`, "PATCH", { isActive }, [
    `/admin/kateqoriyalar/${categoryId}`,
    `/admin/kateqoriyalar/${categoryId}/sahe/${fieldId}`,
  ]);
}

export async function saveOption(
  categoryId: string,
  fieldId: string,
  optionId: string | null,
  input: OptionInput,
): Promise<Saved> {
  if (!ID.test(categoryId) || !ID.test(fieldId) || (optionId && !ID.test(optionId))) return bad;
  const p = checkLabels(input.labels);
  if (p) return p;
  if (!input.value.trim()) return { ok: false, unauthorized: false, message: "Seçimin açarı (dəyəri) boş ola bilməz." };
  const base = `/api/admin/categories/${categoryId}/fields/${fieldId}/options`;
  return send(
    optionId ? `${base}/${optionId}` : base,
    optionId ? "PUT" : "POST",
    { value: input.value.trim(), displayOrder: input.displayOrder, translations: labels(input.labels) },
    [`/admin/kateqoriyalar/${categoryId}/sahe/${fieldId}`],
  );
}

export async function setOptionActive(
  categoryId: string,
  fieldId: string,
  optionId: string,
  isActive: boolean,
): Promise<Saved> {
  if (!ID.test(categoryId) || !ID.test(fieldId) || !ID.test(optionId)) return bad;
  return send(`/api/admin/categories/${categoryId}/fields/${fieldId}/options/${optionId}/status`, "PATCH", { isActive }, [
    `/admin/kateqoriyalar/${categoryId}/sahe/${fieldId}`,
  ]);
}
