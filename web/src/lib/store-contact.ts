import { tx, type T } from "./i18n/locales";

export type OpenDay = { day: number; openMinute: number; closeMinute: number };

/** Monday first (1 Monday … 7 Sunday), as the API writes the days. */
export const weekdayNames = [
  tx("Bazar ertəsi"),
  tx("Çərşənbə axşamı"),
  tx("Çərşənbə"),
  tx("Cümə axşamı"),
  tx("Cümə"),
  tx("Şənbə"),
  tx("Bazar"),
];

export const hhmm = (minutes: number) =>
  `${String(Math.floor(minutes / 60) % 24).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;

/** "09:30" → 570 (null when it is not a time). */
export function toMinutes(text: string): number | null {
  const m = /^(\d{1,2}):(\d{2})$/.exec(text.trim());
  if (!m) return null;
  const h = Number(m[1]);
  const min = Number(m[2]);
  return h > 24 || min > 59 ? null : h * 60 + min;
}

/** The day (1 Monday … 7 Sunday) and minute it is now in Baku. */
function bakuNow(): { day: number; minute: number } {
  const d = new Date(Date.now() + 4 * 3600_000);
  return { day: ((d.getUTCDay() + 6) % 7) + 1, minute: d.getUTCHours() * 60 + d.getUTCMinutes() };
}

/** Today's number (1 Monday … 7 Sunday) in Baku, to mark the day in the list. */
export const todayNumber = () => bakuNow().day;

/** Is the store open right now? null when no hours were given. */
export function openNow(hours: OpenDay[] | null | undefined): boolean | null {
  if (!hours || hours.length === 0) return null;
  const now = bakuNow();
  return hours.some((h) => h.day === now.day && now.minute >= h.openMinute && now.minute < h.closeMinute);
}

/** wa.me link with the first message the owner chose. */
export function whatsappLink(number: string, message?: string | null): string {
  const text = message?.trim();
  return `https://wa.me/${number.replace(/\D/g, "")}${text ? `?text=${encodeURIComponent(text)}` : ""}`;
}

export const defaultWhatsAppMessage = tx("Salam! Mağazanızla maraqlanıram. Ünvanınızı deyə bilərsiniz?");

/** Checks a link before it is sent (the same rule as the store form). */
export function checkUrl(value: string, t: T): string | null {
  const v = value.trim();
  if (!v) return null;
  if (v.length > 300) return t("Link çox uzundur.");
  try {
    const url = new URL(v);
    return url.protocol === "http:" || url.protocol === "https:" ? null : t("Link http:// və ya https:// ilə başlamalıdır.");
  } catch {
    return t("Düzgün link yazın (https://…).");
  }
}

/** Milliseconds from now until an ISO time (negative when it has passed). Kept out of components: it reads the clock. */
export function msUntil(iso: string): number {
  return Date.parse(iso) - Date.now();
}
