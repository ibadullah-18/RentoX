import { tx, type T } from "./i18n/locales";

export type StoreFormValues = {
  name: string;
  description: string;
  phone: string;
  email: string;
  address: string;
  instagram: string;
  tiktok: string;
  facebook: string;
  website: string;
};

export const emptyStoreForm: StoreFormValues = {
  name: "",
  description: "",
  phone: "",
  email: "",
  address: "",
  instagram: "",
  tiktok: "",
  facebook: "",
  website: "",
};

export const StoreRules = {
  nameMin: 2,
  nameMax: 100,
  descriptionMin: 10,
  descriptionMax: 2000,
  phoneMin: 7,
  phoneMax: 30,
  emailMax: 254,
  addressMax: 500,
  urlMax: 300,
  logoMaxBytes: 5 * 1024 * 1024,
  coverMaxBytes: 10 * 1024 * 1024,
} as const;

export type StoreErrors = Partial<Record<keyof StoreFormValues, string>>;

type Messages = { empty: string; short: string; long: string };

function required(value: string, min: number, max: number, m: Messages, t: T): string | null {
  const v = value.trim();
  if (!v) return t(m.empty);
  if (v.length < min) return t(m.short, { min });
  if (v.length > max) return t(m.long, { max });
  return null;
}

function optionalUrl(value: string, t: T): string | null {
  const v = value.trim();
  if (!v) return null;
  if (v.length > StoreRules.urlMax) return t("Link çox uzundur.");
  try {
    const url = new URL(v);
    return url.protocol === "http:" || url.protocol === "https:"
      ? null
      : t("Link http:// və ya https:// ilə başlamalıdır.");
  } catch {
    return t("Düzgün link yazın (https://…).");
  }
}

/** The same rules as the backend, so mistakes are caught before sending. */
export function validateStoreForm(f: StoreFormValues, t: T): StoreErrors {
  const out: StoreErrors = {};
  const set = (key: keyof StoreFormValues, message: string | null) => {
    if (message) out[key] = message;
  };

  set(
    "name",
    required(f.name, StoreRules.nameMin, StoreRules.nameMax, {
      empty: tx("Mağaza adını yazın."),
      short: tx("Mağaza adı ən azı {min} simvol olmalıdır."),
      long: tx("Mağaza adı ən çoxu {max} simvol ola bilər."),
    }, t),
  );
  set(
    "description",
    required(f.description, StoreRules.descriptionMin, StoreRules.descriptionMax, {
      empty: tx("Təsviri yazın."),
      short: tx("Təsvir ən azı {min} simvol olmalıdır."),
      long: tx("Təsvir ən çoxu {max} simvol ola bilər."),
    }, t),
  );
  set(
    "phone",
    required(f.phone, StoreRules.phoneMin, StoreRules.phoneMax, {
      empty: tx("Telefonu yazın."),
      short: tx("Telefon ən azı {min} simvol olmalıdır."),
      long: tx("Telefon ən çoxu {max} simvol ola bilər."),
    }, t),
  );

  const email = f.email.trim();
  if (email) {
    if (email.length > StoreRules.emailMax) out.email = t("E-poçt çox uzundur.");
    else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) out.email = t("Düzgün e-poçt yazın.");
  }
  if (f.address.trim().length > StoreRules.addressMax) out.address = t("Ünvan çox uzundur.");

  set("instagram", optionalUrl(f.instagram, t));
  set("tiktok", optionalUrl(f.tiktok, t));
  set("facebook", optionalUrl(f.facebook, t));
  set("website", optionalUrl(f.website, t));
  return out;
}

/** The API body: empty optional fields are sent as null. */
export function storeBody(f: StoreFormValues) {
  const opt = (v: string) => (v.trim() ? v.trim() : null);
  return {
    name: f.name.trim(),
    description: f.description.trim(),
    phoneNumber: f.phone.trim(),
    email: opt(f.email),
    address: opt(f.address),
    instagramUrl: opt(f.instagram),
    tiktokUrl: opt(f.tiktok),
    facebookUrl: opt(f.facebook),
    websiteUrl: opt(f.website),
  };
}
