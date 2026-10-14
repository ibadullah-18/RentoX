import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { SearchX } from "lucide-react";

export default async function NotFound() {
  const t = await getT();
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-3 py-20 text-center">
      <SearchX className="size-14 text-primary" aria-hidden />
      <h1 className="text-2xl font-extrabold">{t("Səhifə tapılmadı")}</h1>
      <p className="text-ink-2">{t("Elan silinmiş və ya müddəti bitmiş ola bilər.")}</p>
      <Link
        href="/"
        className="mt-2 flex h-11 items-center rounded-xl bg-primary px-8 font-bold text-white hover:bg-primary-press"
      >
        {t("Ana səhifə")}
      </Link>
    </div>
  );
}
