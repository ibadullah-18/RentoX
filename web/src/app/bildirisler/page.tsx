import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { NoNotifications, NotificationList } from "@/components/NotificationList";
import { Pagination } from "@/components/Pagination";
import { getNotifications } from "@/lib/personal";
import { getSession } from "@/lib/session";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Bildirişlər"), robots: { index: false } };
}

export default async function NotificationsPage({ searchParams }: PageProps<"/bildirisler">) {
  const t = await getT();
  if (!(await getSession())) redirect("/giris?next=/bildirisler");

  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const result = await getNotifications(page);
  if (!result.ok && result.status === 401) redirect("/giris?next=/bildirisler");
  const data = result.ok ? result.data : null;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-extrabold">{t("Bildirişlər")}</h1>
      {!data || data.items.length === 0 ? (
        <NoNotifications />
      ) : (
        <>
          {/* A new page of results starts the list over. */}
          <NotificationList key={page} initial={data.items} />
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => (p === 1 ? "/bildirisler" : `/bildirisler?page=${p}`)}
          />
        </>
      )}
    </div>
  );
}
