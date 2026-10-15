import { AnnouncementForm } from "@/components/admin/AnnouncementForm";
import { Card, PageHead, dateTime } from "@/components/admin/ui";
import { authed } from "@/lib/authed";
import type { Paged } from "@/lib/types";

type Sent = { title: string; body: string; actionUrl?: string | null; sentAtUtc: string; recipients: number; read: number };

/** A message to everyone, store owners or sellers; below it, what was sent before and who read it. */
export default async function AnnouncementsPage() {
  const history = await authed<Paged<Sent>>("/api/admin/announcements", { query: { pageSize: 20 } });

  return (
    <div className="space-y-6">
      <PageHead
        title="Bildiriş göndər"
        hint="Bütün aktiv istifadəçilərə, mağaza sahiblərinə və ya satıcılara bildiriş göndərilir: saytdakı zəngdə və mobil tətbiqdə görünür. Konkret bir nəfərə yazmaq üçün onun səhifəsini açın."
      />
      <AnnouncementForm />

      <Card title="Göndərilənlər">
        <ul className="divide-y divide-line">
          {(!history.ok || history.data.items.length === 0) && (
            <li className="px-4 py-6 text-center text-sm text-ink-2">Hələ bildiriş göndərilməyib.</li>
          )}
          {history.ok &&
            history.data.items.map((h) => (
              <li key={`${h.sentAtUtc}-${h.title}`} className="px-4 py-3">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <span className="font-bold">{h.title}</span>
                  <span className="text-xs text-ink-2">{dateTime(h.sentAtUtc)}</span>
                </div>
                <p className="mt-1 whitespace-pre-line text-sm text-ink-2">{h.body}</p>
                <p className="mt-1 text-xs text-ink-2">
                  {h.recipients} nəfərə göndərilib · {h.read} nəfər oxuyub
                  {h.actionUrl ? ` · keçid: ${h.actionUrl}` : ""}
                </p>
              </li>
            ))}
        </ul>
      </Card>
    </div>
  );
}
