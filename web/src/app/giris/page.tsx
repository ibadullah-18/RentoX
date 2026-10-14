import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { LoginForm } from "@/components/LoginForm";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Daxil ol"), robots: { index: false } };
}

/** Only a path on this site: a link must never send people elsewhere. */
function safeNext(value: string | string[] | undefined): string {
  const next = Array.isArray(value) ? value[0] : value;
  return next && next.startsWith("/") && !next.startsWith("//") ? next : "/kabinet";
}

export default async function LoginPage({ searchParams }: PageProps<"/giris">) {
  const t = await getT();
  const next = safeNext((await searchParams).next);
  if (await getSession()) redirect(next);

  return (
    <div className="mx-auto max-w-md py-6 md:py-12">
      <div className="rounded-2xl border border-line bg-surface p-6 md:p-8">
        <h1 className="text-2xl font-extrabold">{t("Daxil ol")}</h1>
        <p className="mb-6 mt-1 text-ink-2">{t("Elan vermək, mesaj yazmaq və seçilmişlər üçün.")}</p>
        <LoginForm next={next} />
      </div>
    </div>
  );
}
