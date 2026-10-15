import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { BackButton } from "@/components/BackButton";
import { NotificationSettings } from "@/components/NotificationSettings";
import { ProfileForm } from "@/components/ProfileForm";
import { getLocale, getT } from "@/lib/i18n/server";
import { authed } from "@/lib/authed";
import { getAccount } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Profili redaktə et"), robots: { index: false } };
}

export default async function EditProfilePage() {
  const t = await getT();
  const session = await getSession();
  if (!session) redirect("/giris?next=/kabinet/profil");

  const account = await getAccount();
  if (!account.ok) {
    if (account.status === 401) redirect("/giris?next=/kabinet/profil");
    redirect("/kabinet");
  }

  const settings = await authed<{ announcementsEnabled: boolean }>("/api/account/notification-settings");

  return (
    <div className="mx-auto max-w-xl space-y-5">
      <div className="flex items-center gap-3">
        <BackButton fallback="/kabinet" />
        <h1 className="text-xl font-extrabold md:text-2xl">{t("Profili redaktə et")}</h1>
      </div>
      <ProfileForm
        userId={session.userId}
        phone={account.data.phoneNumber}
        initialName={account.data.fullName}
        initialBio={account.data.bio ?? ""}
        initialLanguage={await getLocale()}
      />
      {settings.ok && <NotificationSettings initial={settings.data.announcementsEnabled} />}
    </div>
  );
}
