import "server-only";
import { cookies } from "next/headers";
import { getBranding } from "./api";
import { THEMES, THEME_COOKIE, type Theme } from "./theme";

/** The look chosen on this browser: day, night, or "auto" (follow the device). Before a choice, the owner's default. */
export async function getTheme(): Promise<Theme> {
  try {
    const chosen = (await cookies()).get(THEME_COOKIE)?.value;
    if (THEMES.includes(chosen as Theme)) return chosen as Theme;
  } catch {
    return "light";
  }
  return THEMES[(await getBranding()).defaultTheme] ?? "light";
}
