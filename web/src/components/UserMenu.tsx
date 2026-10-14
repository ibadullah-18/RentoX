import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import { User } from "lucide-react";
import type { Session } from "@/lib/session";

/** "Giriş" when signed out; the person's icon linking to the cabinet when in. */
export async function UserMenu({ session }: { session: Session | null }) {
  const t = await getT();
  if (!session) {
    return (
      <Link
        href="/giris"
        className="ml-1 hidden h-10 items-center rounded-lg bg-primary-soft px-5 text-[14px] font-bold text-primary hover:opacity-80 md:flex"
      >
        {t("Giriş")}
      </Link>
    );
  }

  return (
    <Link
      href="/kabinet"
      aria-label={t("Kabinet")}
      className="ml-1 hidden size-10 items-center justify-center rounded-lg bg-primary-soft text-primary hover:opacity-80 md:flex"
    >
      <User className="size-5" aria-hidden />
    </Link>
  );
}
