import type { Locale } from "./i18n/locales";

/**
 * Dates and times, written by hand in Baku time (UTC+4, no summer time). The browser's own date formatting is not
 * used: its Azerbaijani month names are incomplete ("M10") and differ between server and browser (hydration errors).
 */
const MONTHS: Record<Locale, string[]> = {
  az: ["yanvar", "fevral", "mart", "aprel", "may", "iyun", "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr"],
  en: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
  ru: ["января", "февраля", "марта", "апреля", "мая", "июня", "июля", "августа", "сентября", "октября", "ноября", "декабря"],
};

const SHORT: Record<Locale, string[]> = {
  az: ["yan", "fev", "mar", "apr", "may", "iyn", "iyl", "avq", "sen", "okt", "noy", "dek"],
  en: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
  ru: ["янв", "фев", "мар", "апр", "мая", "июн", "июл", "авг", "сен", "окт", "ноя", "дек"],
};

const pad = (n: number) => String(n).padStart(2, "0");

export type BakuParts = { year: number; month: number; day: number; hour: number; minute: number };

/** The year, month (1-12), day, hour and minute of a moment as the clock in Baku shows them. */
export function bakuParts(input: string | number | Date): BakuParts {
  const shifted = new Date(new Date(input).getTime() + 4 * 3_600_000);
  return {
    year: shifted.getUTCFullYear(),
    month: shifted.getUTCMonth() + 1,
    day: shifted.getUTCDate(),
    hour: shifted.getUTCHours(),
    minute: shifted.getUTCMinutes(),
  };
}

const valid = (input: string | number | Date) => !Number.isNaN(new Date(input).getTime());

/** "12 oktyabr 2026" (or "12 okt" when short, without the year when [year] is false). */
export function fmtDate(input: string | number | Date, locale: Locale = "az", options: { short?: boolean; year?: boolean } = {}): string {
  if (!valid(input)) return "—";
  const p = bakuParts(input);
  const month = (options.short ? SHORT : MONTHS)[locale][p.month - 1];
  return `${p.day} ${month}${options.year === false ? "" : ` ${p.year}`}`;
}

/** "14:05". */
export function fmtTime(input: string | number | Date): string {
  if (!valid(input)) return "—";
  const p = bakuParts(input);
  return `${pad(p.hour)}:${pad(p.minute)}`;
}

/** "12 oktyabr, 14:05" (the year is added when [year] is true). */
export function fmtDateTime(input: string | number | Date, locale: Locale = "az", options: { short?: boolean; year?: boolean } = {}): string {
  if (!valid(input)) return "—";
  return `${fmtDate(input, locale, { ...options, year: options.year ?? false })}, ${fmtTime(input)}`;
}

/** "12.10.2026 14:05": the compact form for tables. */
export function fmtNumeric(input?: string | number | Date | null, withTime = true): string {
  if (input === null || input === undefined || !valid(input)) return "—";
  const p = bakuParts(input);
  const date = `${pad(p.day)}.${pad(p.month)}.${p.year}`;
  return withTime ? `${date} ${pad(p.hour)}:${pad(p.minute)}` : date;
}

/** "oktyabr 2026" for a month given as any day of it (or "2026-10"). */
export function fmtMonth(input: string | number | Date, locale: Locale = "az"): string {
  const text = typeof input === "string" && /^\d{4}-\d{2}$/.test(input) ? `${input}-15T12:00:00Z` : input;
  if (!valid(text)) return "—";
  const p = bakuParts(text);
  return `${MONTHS[locale][p.month - 1]} ${p.year}`;
}

/** Today in Baku as "2026-10-14". Kept in a function (not in a component) because it reads the clock. */
export function bakuTodayIso(): string {
  const p = bakuParts(Date.now());
  return `${p.year}-${pad(p.month)}-${pad(p.day)}`;
}
