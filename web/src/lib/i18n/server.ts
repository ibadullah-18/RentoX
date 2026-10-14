import "server-only";
import { cookies } from "next/headers";
import { cache } from "react";
import { DEFAULT_LOCALE, LOCALE_COOKIE, normalizeLocale, translator, type Locale, type T } from "./locales";

/** The language chosen on this browser (Azerbaijani until it is changed). */
export const getLocale = cache(async (): Promise<Locale> => {
  try {
    return normalizeLocale((await cookies()).get(LOCALE_COOKIE)?.value);
  } catch {
    // Outside a request (sitemap, build): the default language.
    return DEFAULT_LOCALE;
  }
});

/** The translate function for the current request. */
export async function getT(): Promise<T> {
  return translator(await getLocale());
}
