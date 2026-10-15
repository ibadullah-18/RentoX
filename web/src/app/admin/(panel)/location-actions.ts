"use server";

import { revalidatePath, updateTag } from "next/cache";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type LocationSaved = { ok: true } | { ok: false; unauthorized: boolean; message: string };

export type LocationInput = {
  nameAz: string;
  nameRu: string;
  nameEn: string;
  displayOrder: number;
  /** On an update only. */
  isActive?: boolean;
};

const ID = /^[0-9a-f-]{36}$/i;

const known: [RegExp, string][] = [
  [/city with this name already exists/i, "Bu adda şəhər artıq var."],
  [/district with this name already exists/i, "Bu şəhərdə bu adda rayon artıq var."],
  [/city was not found/i, "Şəhər tapılmadı."],
  [/district was not found/i, "Rayon tapılmadı."],
  [/name is required/i, "Üç dildə də ad yazın."],
  [/cannot exceed 100/i, "Ad 100 simvoldan uzun ola bilməz."],
  [/display order/i, "Sıra 0 ilə 10000 arasında olmalıdır."],
];

function check(input: LocationInput): string | null {
  if (!input.nameAz.trim() || !input.nameRu.trim() || !input.nameEn.trim()) return "Üç dildə də ad yazın.";
  if ([input.nameAz, input.nameRu, input.nameEn].some((n) => n.trim().length > 100)) return "Ad 100 simvoldan uzun ola bilməz.";
  if (!Number.isInteger(input.displayOrder) || input.displayOrder < 0 || input.displayOrder > 10000) {
    return "Sıra 0 ilə 10000 arasında tam ədəd olmalıdır.";
  }
  return null;
}

async function send(path: string, method: "POST" | "PUT", input: LocationInput): Promise<LocationSaved> {
  if (!(await isAdmin())) return { ok: false, unauthorized: true, message: "" };
  const sections = await getSections();
  if (sections !== null && !sections.includes("catalog")) return { ok: false, unauthorized: true, message: "" };

  const problem = check(input);
  if (problem) return { ok: false, unauthorized: false, message: problem };

  const res = await authed(path, {
    method,
    body: {
      nameAz: input.nameAz.trim(),
      nameRu: input.nameRu.trim(),
      nameEn: input.nameEn.trim(),
      displayOrder: input.displayOrder,
      isActive: input.isActive,
    },
  });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }

  // The site keeps the list of cities for a while: drop that copy so the change shows now.
  updateTag("locations");
  revalidatePath("/admin/yerler");
  return { ok: true };
}

export async function createCity(input: LocationInput): Promise<LocationSaved> {
  return send("/api/admin/locations/cities", "POST", input);
}

export async function updateCity(id: string, input: LocationInput): Promise<LocationSaved> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  return send(`/api/admin/locations/cities/${id}`, "PUT", input);
}

export async function createDistrict(cityId: string, input: LocationInput): Promise<LocationSaved> {
  if (!ID.test(cityId)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  return send(`/api/admin/locations/cities/${cityId}/districts`, "POST", input);
}

export async function updateDistrict(id: string, input: LocationInput): Promise<LocationSaved> {
  if (!ID.test(id)) return { ok: false, unauthorized: false, message: "Yanlış sorğu." };
  return send(`/api/admin/locations/districts/${id}`, "PUT", input);
}
