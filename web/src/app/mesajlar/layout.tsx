import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { MessagesShell } from "@/components/messages/MessagesShell";
import { getConversations } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Mesajlar"), robots: { index: false } };
}

export default async function MessagesLayout({ children }: LayoutProps<"/mesajlar">) {
  const session = await getSession();
  if (!session) redirect("/giris?next=/mesajlar");

  const result = await getConversations(1, 50);
  if (!result.ok && result.status === 401) redirect("/giris?next=/mesajlar");

  return (
    <MessagesShell initial={result.ok ? result.data.items : []} me={session.userId}>
      {children}
    </MessagesShell>
  );
}
