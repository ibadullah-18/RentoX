export const THEMES = ["light", "dark", "auto"] as const;
export type Theme = (typeof THEMES)[number];
export const THEME_COOKIE = "rx_theme";

export function normalizeTheme(value?: string | null): Theme {
  return THEMES.includes(value as Theme) ? (value as Theme) : "auto";
}
