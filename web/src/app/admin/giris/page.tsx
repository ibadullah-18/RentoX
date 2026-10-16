import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { ShieldCheck } from "lucide-react";
import { AdminLoginForm } from "@/components/admin/AdminLoginForm";
import { adminHref, ADMIN_SEGMENT } from "@/lib/admin-path";
import { isAdmin } from "@/lib/session";

export const metadata: Metadata = { title: "Giriş", robots: { index: false, follow: false } };

/** Only a path inside the panel: a link must never send people elsewhere. */
function safeNext(value: string | string[] | undefined): string {
  const next = Array.isArray(value) ? value[0] : value;
  return next && next.startsWith(`/${ADMIN_SEGMENT}`) && !next.startsWith("//") && !next.includes("\\") ? next : adminHref();
}

export default async function AdminLoginPage({ searchParams }: PageProps<"/admin/giris">) {
  const next = safeNext((await searchParams).next);
  if (await isAdmin()) redirect(next);

  return (
    <div className="flex min-h-dvh items-center justify-center bg-bg px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <p className="text-3xl font-extrabold tracking-tight">
            rento<span className="text-primary">x</span>
          </p>
          <p className="mt-1 flex items-center justify-center gap-1.5 text-sm font-bold text-ink-2">
            <ShieldCheck className="size-4 text-primary" aria-hidden />
            İdarəetmə paneli
          </p>
        </div>
        <div className="rounded-xl border border-line bg-surface p-6 shadow-sm">
          <h1 className="mb-4 text-lg font-extrabold">Giriş</h1>
          <AdminLoginForm next={next} />
        </div>
        <p className="mt-4 text-center text-xs text-ink-2">Yalnız səlahiyyətli işçilər üçün.</p>
      </div>
    </div>
  );
}
