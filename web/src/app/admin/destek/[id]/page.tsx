import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { TicketAdmin } from "@/components/admin/TicketAdmin";
import { authed } from "@/lib/authed";
import type { AdminTicket } from "@/lib/admin-types";
import { ticketCategory } from "@/lib/support";

export default async function AdminTicketPage({ params }: PageProps<"/admin/destek/[id]">) {
  const { id } = await params;
  const result = await authed<AdminTicket>(`/api/admin/support/tickets/${encodeURIComponent(id)}`);
  if (!result.ok) notFound();
  const t = result.data;
  const messages = [...(t.messages ?? [])].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link href="/admin/destek" className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        Müraciətlər
      </Link>

      <div>
        <h1 className="text-2xl font-extrabold leading-tight">{t.subject}</h1>
        <p className="mt-1 text-sm text-ink-2">
          {ticketCategory[t.category] ?? "Digər"} · istifadəçi {t.userId.slice(0, 8)} ·{" "}
          {new Date(t.createdAtUtc).toLocaleString("az")}
        </p>
      </div>

      <ul className="space-y-3">
        {messages.map((m) => (
          <li key={m.id} className={`flex flex-col ${m.isAdmin ? "items-end" : "items-start"}`}>
            <div
              className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-4 py-2.5 text-[15px] ${
                m.isAdmin
                  ? "rounded-br-md bg-primary text-white"
                  : "rounded-bl-md border border-line bg-surface"
              }`}
            >
              {m.body}
            </div>
            <span className="mt-1 px-1 text-xs text-ink-2">
              {m.isAdmin ? "Dəstək" : "İstifadəçi"} · {new Date(m.sentAtUtc).toLocaleString("az")}
            </span>
          </li>
        ))}
      </ul>

      <TicketAdmin id={t.id} status={t.status} priority={t.priority} />
    </div>
  );
}
