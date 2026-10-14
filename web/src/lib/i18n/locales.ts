import { en } from "./en";
import { ru } from "./ru";

// Texts are written in Azerbaijani right in the code: t("Elanı göndər").
// English and Russian are looked up by that very text; a text that has no
// translation yet is shown in Azerbaijani, so nothing is ever blank.
export const LOCALES = ["az", "en", "ru"] as const;
export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "az";
export const LOCALE_COOKIE = "rx_lang";

export const localeNames: Record<Locale, string> = { az: "Azərbaycanca", en: "English", ru: "Русский" };

/** The tag Intl (dates, numbers) understands. */
export const intlTag: Record<Locale, string> = { az: "az", en: "en-GB", ru: "ru" };

export function normalizeLocale(value?: string | null): Locale {
  return LOCALES.includes(value as Locale) ? (value as Locale) : DEFAULT_LOCALE;
}

const dictionaries: Record<Exclude<Locale, "az">, Record<string, string>> = { en, ru };

export type Vars = Record<string, string | number>;

export function translate(locale: Locale, text: string, vars?: Vars): string {
  const found = locale === "az" ? text : (dictionaries[locale][text] ?? text);
  return vars ? found.replace(/\{(\w+)\}/g, (whole, name: string) => String(vars[name] ?? whole)) : found;
}

export type T = (text: string, vars?: Vars) => string;

export function translator(locale: Locale): T {
  return (text, vars) => translate(locale, text, vars);
}

/**
 * Marks a text for translation without translating it (labels kept in a
 * table at module level). Translate it where it is shown: t(label[key]).
 */
export function tx<S extends string>(text: S): S {
  return text;
}
