import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { BottomNav } from "@/components/BottomNav";
import { FooterGate, MainFrame } from "@/components/ChromeGate";
import { Footer } from "@/components/Footer";
import { FavoritesProvider } from "@/components/FavoritesProvider";
import { MotionController } from "@/components/MotionController";
import { AnnouncementBar } from "@/components/AnnouncementBar";
import { Header } from "@/components/Header";
import { NotificationToasts } from "@/components/NotificationToasts";
import { getFavoriteIds, getUnreadMessages, getUnreadNotifications } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { headers } from "next/headers";
import { siteUrl } from "@/lib/format";
import { I18nProvider } from "@/lib/i18n/client";
import { intlTag } from "@/lib/i18n/locales";
import { getLocale, getT } from "@/lib/i18n/server";
import { getTheme } from "@/lib/theme-server";
import { DEFAULT_ICON, getBranding } from "@/lib/api";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin", "latin-ext", "cyrillic"],
  display: "swap",
});

export async function generateMetadata(): Promise<Metadata> {
  const t = await getT();
  const locale = await getLocale();
  const branding = await getBranding();
  const icon = branding.iconUrl ?? DEFAULT_ICON;
  return {
    metadataBase: new URL(siteUrl()),
    title: { default: `${branding.siteName} — ${t("Kirayə elanları — hər şey bir yerdə")}`, template: `%s | ${branding.siteName}` },
    description: branding.description?.trim() || t("Avtomobil, mənzil, texnika, avadanlıq və daha çox — Azərbaycanda kirayə elanları."),
    openGraph: {
      siteName: branding.siteName,
      locale: intlTag[locale].replace("-", "_"),
      type: "website",
      ...(branding.shareImageUrl ? { images: [{ url: branding.shareImageUrl }] } : {}),
    },
    icons: { icon: [{ url: icon }], shortcut: [{ url: icon }], apple: [{ url: icon }] },
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
  const branding = await getBranding();
  // The admin panel has its own frame: no site header, footer or bottom bar.
  const admin = (await headers()).get("x-rx-area") === "admin";
  const session = await getSession();
  const [favoriteIds, unreadMessages, unreadNotifications] = session && !admin
    ? await Promise.all([getFavoriteIds(), getUnreadMessages(), getUnreadNotifications()])
    : [[], 0, 0];

  return (
    <html lang={locale} data-theme={theme === "auto" ? undefined : theme} data-motion={["full", "soft", "off"][branding.motionLevel] ?? "full"} className={`${inter.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col">
        <I18nProvider locale={locale}>
        {admin ? (
          <>
            {children}
            {session && <NotificationToasts />}
          </>
        ) : (
          <FavoritesProvider initialIds={favoriteIds} signedIn={session !== null}>
            <MotionController />
            <AnnouncementBar branding={branding} />
            <Header unreadMessages={unreadMessages} unreadNotifications={unreadNotifications} />
            <MainFrame>{children}</MainFrame>
            <FooterGate>
              <Footer />
            </FooterGate>
            <BottomNav unreadMessages={unreadMessages} />
            {session && <NotificationToasts />}
          </FavoritesProvider>
        )}
        </I18nProvider>
      </body>
    </html>
  );
}
