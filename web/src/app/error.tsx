"use client";

import { useT } from "@/lib/i18n/client";
import Link from "next/link";
import { useEffect } from "react";
import { TriangleAlert } from "lucide-react";

/** Something broke while showing a page: say so plainly and offer a way on. */
export default function ErrorPage({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const t = useT();
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-3 py-20 text-center">
      <TriangleAlert className="size-14 text-warning" aria-hidden />
      <h1 className="text-2xl font-extrabold">{t("Xəta baş verdi")}</h1>
      <p className="text-ink-2">{t("Səhifə yüklənmədi. Bir az sonra yenidən cəhd edin.")}</p>
      <div className="mt-2 flex gap-2">
        <button
          type="button"
          onClick={reset}
          className="flex h-11 items-center rounded-xl bg-primary px-8 font-bold text-white hover:bg-primary-press"
        >
          {t("Yenidən cəhd et")}</button>
        <Link href="/" className="flex h-11 items-center rounded-xl border border-line px-6 font-bold hover:border-primary">
          {t("Ana səhifə")}</Link>
      </div>
    </div>
  );
}
