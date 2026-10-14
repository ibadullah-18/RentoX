import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { BottomNav } from "@/components/BottomNav";
import { Footer } from "@/components/Footer";
import { FavoritesProvider } from "@/components/FavoritesProvider";
import { Header } from "@/components/Header";
import { getFavoriteIds, getUnreadMessages, getUnreadNotifications } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { siteUrl } from "@/lib/format";
import { I18nProvider } from "@/lib/i18n/client";
import { intlTag } from "@/lib/i18n/locales";
import { getLocale, getT } from "@/lib/i18n/server";
import { getTheme } from "@/lib/theme-server";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin", "latin-ext", "cyrillic"],
  display: "swap",
});

export async function generateMetadata(): Promise<Metadata> {
  const t = await getT();
  const locale = await getLocale();
  return {
    metadataBase: new URL(siteUrl()),
    title: { default: `RentoX — ${t("Kirayə elanları — hər şey bir yerdə")}`, template: "%s | RentoX" },
    description: t("Avtomobil, mənzil, texnika, avadanlıq və daha çox — Azərbaycanda kirayə elanları."),
    openGraph: { siteName: "RentoX", locale: intlTag[locale].replace("-", "_"), type: "website" },
  };
}

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f6f5ff" },
    { media: "(prefers-color-scheme: dark)", color: "#0f0d24" },
  ],
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  const locale = await getLocale();
  const theme = await getTheme();
  const session = await getSession();
  const [favoriteIds, unreadMessages, unreadNotifications] = session
    ? await Promise.all([getFavoriteIds(), getUnreadMessages(), getUnreadNotifications()])
    : [[], 0, 0];

  return (
    <html lang={locale} data-theme={theme === "auto" ? undefined : theme} className={`${inter.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col">
        <I18nProvider locale={locale}>
        <FavoritesProvider initialIds={favoriteIds} signedIn={session !== null}>
          <Header unreadMessages={unreadMessages} unreadNotifications={unreadNotifications} />
          <main className="mx-auto w-full max-w-[1240px] flex-1 px-4 py-5 md:px-6 md:py-8">
            {children}
          </main>
          <Footer />
          <BottomNav unreadMessages={unreadMessages} />
        </FavoritesProvider>
        </I18nProvider>
      </body>
    </html>
  );
}
