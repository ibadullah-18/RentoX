import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { Logo } from "./Logo";

export async function Footer() {
  const t = await getT();
  return (
    <footer className="mt-16 border-t border-line">
      <div className="mx-auto flex max-w-[1240px] flex-col gap-2 px-4 py-8 text-sm text-ink-2 md:flex-row md:items-center md:justify-between md:px-6">
        <div className="flex items-center gap-3">
          <Logo />
          <span>{t("Kirayə elanları — hər şey bir yerdə")}</span>
        </div>
        <nav aria-label={t("Alt keçidlər")} className="flex flex-wrap gap-x-5 gap-y-1">
          <Link href="/magazalar" className="font-semibold hover:text-primary">
            {t("Mağazalar")}
          </Link>
          <span>
            © {new Date().getFullYear()} RentoX. {t("Bütün hüquqlar qorunur.")}
          </span>
        </nav>
      </div>
    </footer>
  );
}
