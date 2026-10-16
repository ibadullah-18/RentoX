import { fmtDate, fmtDateTime, fmtTime } from "./dates";
import { intlTag, translator, tx, type Locale, type T } from "./i18n/locales";
import type { ListingField, RentalPeriodId } from "./types";

const az = translator("az");

const formats = new Map<string, Intl.NumberFormat | Intl.DateTimeFormat>();
function numberFormat(locale: Locale) {
  const key = `n-${locale}`;
  if (!formats.has(key)) formats.set(key, new Intl.NumberFormat(intlTag[locale], { maximumFractionDigits: 2 }));
  return formats.get(key) as Intl.NumberFormat;
}
/** The label of a rental period; translate it where it is shown. */
export const unitLabel: Record<number, string> = {
  1: tx("saat"),
  2: tx("gün"),
  3: tx("həftə"),
  4: tx("ay"),
  5: tx("tədbir"),
  6: tx("razılaşma ilə"),
};

/** "1 500 AZN / ay", or "800 AZN" for negotiable prices. */
export function formatPrice(
  price: number,
  currency: string,
  unit: RentalPeriodId,
  t: T = az,
  locale: Locale = "az",
): string {
  const amount = `${numberFormat(locale).format(price)} ${currency}`;
  return unit === 6 ? amount : `${amount} / ${t(unitLabel[unit])}`;
}

/** "Bu gün", "Dünən" or "12 oktyabr 2026". */
export function formatPublished(iso?: string | null, t: T = az, locale: Locale = "az"): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;

  const startOfDay = (d: Date) =>
    new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  const days = Math.round(
    (startOfDay(new Date()) - startOfDay(date)) / 86_400_000,
  );
  if (days === 0) return t("Bu gün");
  if (days === 1) return t("Dünən");
  return fmtDate(date, locale);
}

/** What to show for one field of a listing, or null when it is empty. */
export function fieldValue(field: ListingField, t: T = az, locale: Locale = "az"): string | null {
  const picked = (field.selections ?? [])
    .map((s) => s.label ?? s.value ?? "")
    .filter(Boolean);
  if (picked.length > 0) return picked.join(", ");
  if (field.textValue) return field.textValue;
  if (field.customValue) return field.customValue;
  if (field.numericValue != null) return numberFormat(locale).format(field.numericValue);
  if (field.flagValue != null) return field.flagValue ? t("Bəli") : t("Xeyr");
  if (field.calendarValue) {
    const d = new Date(field.calendarValue);
    return Number.isNaN(d.getTime()) ? null : fmtDate(d, locale);
  }
  return null;
}

/**
 * The address to show one of the owner's own photos from. Photos of a listing
 * that is not public yet need the owner's token, which only the site's own
 * route can add.
 */
export function ownerImage(url?: string | null): string | null {
  if (!url) return null;
  return url.replace("/api/listing-images/", "/bff/listing-images/");
}

/** The site's own address, for links and share previews. */
export function siteUrl(): string {
  return (process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000").replace(
    /\/$/,
    "",
  );
}

/**
 * A date formatter for the language in Baku time, e.g. dateFmt(locale, { hour: "2-digit" }). It has the shape of an
 * Intl formatter (`.format(date)`) but is written by hand (see lib/dates.ts), so the server and the browser agree.
 */
export function dateFmt(
  locale: Locale,
  options: { day?: string; month?: string; year?: string; hour?: string; minute?: string; timeZone?: string },
): { format: (date: Date | string | number) => string } {
  return {
    format(date) {
      const short = options.month === "short";
      if (options.hour && !options.day) return fmtTime(date);
      if (options.hour) return fmtDateTime(date, locale, { short, year: options.year !== undefined });
      return fmtDate(date, locale, { short, year: options.year !== undefined });
    },
  };
}

/** A number written the way the language writes it (1 500,5 / 1,500.5). */
export function formatNumber(value: number, locale: Locale = "az"): string {
  return numberFormat(locale).format(value);
}
