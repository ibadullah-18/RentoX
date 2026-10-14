"use client";

import { createContext, useContext, useMemo } from "react";
import { DEFAULT_LOCALE, translator, type Locale, type T } from "./locales";

const Context = createContext<Locale>(DEFAULT_LOCALE);

/** Gives client components the language the server chose for this request. */
export function I18nProvider({ locale, children }: { locale: Locale; children: React.ReactNode }) {
  return <Context.Provider value={locale}>{children}</Context.Provider>;
}

export const useLocale = () => useContext(Context);

/** The translate function for client components. */
export function useT(): T {
  const locale = useLocale();
  return useMemo(() => translator(locale), [locale]);
}
