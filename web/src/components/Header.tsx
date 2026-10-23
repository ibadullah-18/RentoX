import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { Suspense } from "react";
import { Heart, MessageCircle, Plus } from "lucide-react";
import { getSession } from "@/lib/session";
import { getTheme } from "@/lib/theme-server";
import { LanguageSwitcher } from "./LanguageSwitcher";
import { Logo } from "./Logo";
import { NotificationBell } from "./NotificationBell";
import { SearchBox } from "./SearchBox";
import { ThemeSwitcher } from "./ThemeSwitcher";
import { UserMenu } from "./UserMenu";

const iconLink =
  "relative hidden size-10 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft hover:text-primary md:flex";

/**
 * Computer: logo, search, then favourites, messages, bell, look,
 * language, new listing, login — one slim row. Phone: logo with look,
 * language and bell on top, the search bar under it; the rest lives in the
 * bottom bar.
 */
export async function Header({
  unreadMessages = 0,
  unreadNotifications = 0,
}: {
  unreadMessages?: number;
  unreadNotifications?: number;
}) {
  const t = await getT();
  const [session, theme] = await Promise.all([getSession(), getTheme()]);

  return (
    <header className="sticky top-0 z-50 border-b border-line bg-bg">
      <div className="mx-auto flex max-w-[1240px] flex-wrap items-center gap-x-3 gap-y-2.5 px-4 py-2.5 md:flex-nowrap md:px-6">
        <Logo variant="header" className="order-1" />

        <Suspense fallback={<div className="order-4 h-11 w-full md:order-2 md:h-10 md:w-auto md:flex-1" />}>
          <SearchBox className="order-4 w-full md:order-2 md:w-auto md:flex-1" />
        </Suspense>

        <nav className="order-2 ml-auto flex items-center gap-0.5 md:order-3 md:ml-0 md:gap-1">
          <Link href="/secilmisler" aria-label={t("Seçilmişlər")} className={iconLink}>
            <Heart className="size-5" />
          </Link>
          <Link href="/mesajlar" aria-label={t("Mesajlar")} className={iconLink}>
            <MessageCircle className="size-5" />
            {unreadMessages > 0 && (
              <span className="absolute right-0 top-0 flex h-[17px] min-w-[17px] items-center justify-center rounded-full bg-accent px-1 text-[10px] font-bold text-white">
                {unreadMessages > 99 ? "99+" : unreadMessages}
              </span>
            )}
          </Link>
          <NotificationBell unread={unreadNotifications} signedIn={session !== null} />
          <ThemeSwitcher initial={theme} />
          <LanguageSwitcher />
          <Link
            href="/yeni-elan"
            className="ml-1.5 hidden h-10 items-center gap-1.5 rounded-lg bg-success px-4 text-[14px] font-bold text-white hover:opacity-90 md:flex"
          >
            <Plus className="size-[18px]" aria-hidden />
            {t("Yeni elan")}
          </Link>
          <UserMenu session={session} />
        </nav>
      </div>
    </header>
  );
}
