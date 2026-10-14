import "server-only";
import { cookies } from "next/headers";
import { normalizeTheme, THEME_COOKIE, type Theme } from "./theme";

/** The look chosen on this browser: day, night, or "auto" (follow the device). */
export async function getTheme(): Promise<Theme> {
  try {
    return normalizeTheme((await cookies()).get(THEME_COOKIE)?.value);
  } catch {
    return "auto";
  }
}
