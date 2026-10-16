import { adminHref } from "@/lib/admin-path";
import { getLocale, getT } from "@/lib/i18n/server";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { UserAvatar } from "@/components/messages/UserAvatar";
import { ThemeSwitcher } from "@/components/ThemeSwitcher";
import { getTheme } from "@/lib/theme-server";
import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ChevronRight, Heart, LifeBuoy, ShieldCheck, ListChecks, LogOut, MessageCircle, Plus, Store, Wallet, Pencil } from "lucide-react";
import { logout } from "@/app/giris/actions";
import { getAccount, getMyStore, getWallet } from "@/lib/personal";
import { formatNumber } from "@/lib/format";
import { getSession, isAdmin } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Kabinet"), robots: { index: false } };
}

export default async function CabinetPage() {
  const t = await getT();
  const locale = await getLocale();
  const session = await getSession();
  const theme = await getTheme();
  if (!session) redirect("/giris?next=/kabinet");

  const [account, wallet, store, admin] = await Promise.all([getAccount(), getWallet(), getMyStore(), isAdmin()]);
  if (!account.ok && account.status === 401) redirect("/giris?next=/kabinet");

  const name = account.ok ? account.data.fullName : "";
  const phone = account.ok ? account.data.phoneNumber : session.phone;
  const bio = account.ok ? (account.data.bio ?? "") : "";

  const row =
    "flex items-center gap-3 border-b border-line px-4 py-4 last:border-b-0 hover:bg-primary-soft";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-extrabold">{t("Kabinet")}</h1>

      {store.ok ? (
        // With a store, the store takes the place of the personal card (the personal profile stays one tap away).
        <div className="space-y-2">
          <Link href="/magaza" className="flex items-center gap-4 rounded-xl border border-line bg-surface p-4 hover:border-primary">
            <span className="relative flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-primary-soft text-primary">
              {store.data.logoImageUrl ? (
                <Image src={store.data.logoImageUrl} alt="" fill unoptimized sizes="64px" className="object-cover" />
              ) : (
                <Store className="size-7" aria-hidden />
              )}
            </span>
            <div className="min-w-0 flex-1">
              <p className="truncate text-lg font-extrabold">{store.data.name}</p>
              <p className="text-sm text-ink-2">
                {{ 1: t("Qaralama"), 2: t("Yoxlamada"), 3: t("Aktiv"), 4: t("Rədd edilib"), 5: t("Dayandırılıb") }[store.data.status] ?? ""}
                {store.data.phoneNumber ? ` · ${store.data.phoneNumber}` : ""}
              </p>
            </div>
            <span className="flex items-center gap-1 text-sm font-bold text-primary">
              <Pencil className="size-4" aria-hidden />
              {t("Redaktə et")}
            </span>
          </Link>
          <Link href="/kabinet/profil" className="flex items-center gap-2 px-1 text-sm font-semibold text-ink-2 hover:text-primary">
            <UserAvatar userId={session.userId} size={24} />
            <span className="truncate">{name || phone}</span>
            <ChevronRight className="size-4" aria-hidden />
          </Link>
        </div>
      ) : (
      <Link
        href="/kabinet/profil"
        className="flex items-center gap-4 rounded-xl border border-line bg-surface p-4 hover:border-primary"
      >
        <UserAvatar userId={session.userId} size={56} />
        <div className="min-w-0 flex-1">
          <p className="truncate text-lg font-extrabold">{name || t("Adınızı əlavə edin")}</p>
          <p className="text-sm text-ink-2">{phone}</p>
          {bio && <p className="mt-0.5 line-clamp-1 text-sm text-ink-2">{bio}</p>}
        </div>
        <span className="flex items-center gap-1 text-sm font-bold text-primary">
          <Pencil className="size-4" aria-hidden />
          {t("Redaktə et")}
        </span>
      </Link>
      )}

      <nav aria-label={t("Kabinet")} className="overflow-hidden rounded-xl border border-line bg-surface">
        <Link href="/yeni-elan" className={row}>
          <Plus className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Yeni elan")}</span>
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
        <Link href="/elanlarim" className={row}>
          <ListChecks className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Elanlarım")}</span>
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
        <Link href="/pul-kisesi" className={row}>
          <Wallet className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Pul kisəsi")}</span>
          {wallet.ok && (
            <span className="font-extrabold text-primary">{formatNumber(wallet.data.balance, locale)} {t("AZN")}</span>
          )}
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
        {!store.ok && (
          <Link href="/magaza" className={row}>
            <Store className="size-5 text-primary" aria-hidden />
            <span className="flex-1 font-semibold">{t("Mağaza yarat")}</span>
            <ChevronRight className="size-5 text-ink-2" aria-hidden />
          </Link>
        )}
        <Link href="/secilmisler" className={row}>
          <Heart className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Seçilmişlər")}</span>
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
        {admin && (
          <Link href={adminHref()} className={row}>
            <ShieldCheck className="size-5 text-primary" aria-hidden />
            <span className="flex-1 font-semibold">{t("Admin paneli")}</span>
            <ChevronRight className="size-5 text-ink-2" aria-hidden />
          </Link>
        )}
        <Link href="/destek" className={row}>
          <LifeBuoy className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Dəstək")}</span>
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
        <Link href="/mesajlar" className={row}>
          <MessageCircle className="size-5 text-primary" aria-hidden />
          <span className="flex-1 font-semibold">{t("Mesajlar")}</span>
          <ChevronRight className="size-5 text-ink-2" aria-hidden />
        </Link>
      </nav>

      <section className="overflow-hidden rounded-xl border border-line bg-surface">
        <div className="flex items-center gap-3 border-b border-line px-4 py-2.5">
          <span className="flex-1 font-semibold">{t("Görünüş")}</span>
          <ThemeSwitcher initial={theme} />
        </div>
        <div className="flex items-center gap-3 px-4 py-2.5">
          <span className="flex-1 font-semibold">{t("Dil")}</span>
          <LanguageSwitcher />
        </div>
      </section>

      <form action={logout}>
        <button
          type="submit"
          className="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-line bg-surface font-bold text-danger hover:border-danger"
        >
          <LogOut className="size-5" aria-hidden />
          {t("Çıxış")}</button>
      </form>
    </div>
  );
}
