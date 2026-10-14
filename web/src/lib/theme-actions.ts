"use server";

import { cookies } from "next/headers";
import { normalizeTheme, THEME_COOKIE } from "./theme";

/** Remembers the chosen look (day / night / auto) for a year. */
export async function setTheme(value: string): Promise<void> {
  (await cookies()).set(THEME_COOKIE, normalizeTheme(value), {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
  });
}
