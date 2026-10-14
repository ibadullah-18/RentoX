"use server";

import { cookies } from "next/headers";
import { LOCALE_COOKIE, normalizeLocale } from "./locales";

/** Remembers the language chosen on this browser for a year. */
export async function setLocale(value: string): Promise<void> {
  (await cookies()).set(LOCALE_COOKIE, normalizeLocale(value), {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
  });
}
