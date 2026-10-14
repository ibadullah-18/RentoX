import { dateFmt } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ChevronLeft } from "lucide-react";
import { ReplyForm } from "@/components/SupportForms";
import { getTicket } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { ticketCategory, ticketStatus, ticketTone } from "@/lib/support";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Müraciət"), robots: { index: false } };
}

export default async function TicketPage({ params }: PageProps<"/destek/[id]">) {
  const t = await getT();
  const locale = await getLocale();
  const { id } = await params;
  if (!(await getSession())) redirect(`/giris?next=/destek/${id}`);

  const result = await getTicket(id);
  if (!result.ok) {
    if (result.status === 401) redirect(`/giris?next=/destek/${id}`);
    notFound();
  }
  const ticket = result.data;
  const messages = [...(ticket.messages ?? [])].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
  const closed = ticket.status === 4;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link href="/destek" className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary">
        <ChevronLeft className="size-4" aria-hidden />
        {t("Dəstək")}</Link>

      <div className="space-y-2">
        <h1 className="text-2xl font-extrabold leading-tight">{ticket.subject}</h1>
        <div className="flex items-center gap-2 text-sm text-ink-2">
          <span className={`rounded-md px-2.5 py-1 text-xs font-bold ${ticketTone[ticket.status] ?? ""}`}>
            {ticketStatus[ticket.status] ? t(ticketStatus[ticket.status]) : "—"}
          </span>
          {ticketCategory[ticket.category] ? t(ticketCategory[ticket.category]) : t("Digər")}
        </div>
      </div>

      <ul className="space-y-3">
        {messages.map((m) => (
          <li key={m.id} className={`flex flex-col ${m.isAdmin ? "items-start" : "items-end"}`}>
            <div
              className={`max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-4 py-2.5 text-[15px] ${
                m.isAdmin
                  ? "rounded-bl-md border border-line bg-surface"
                  : "rounded-br-md bg-primary text-white"
              }`}
            >
              {m.body}
            </div>
            <span className="mt-1 px-1 text-xs text-ink-2">
              {m.isAdmin ? t("RentoX dəstək · ") : ""}
              {dateFmt(locale, { day: "numeric", month: "long", hour: "2-digit", minute: "2-digit" }).format(new Date(m.sentAtUtc))}
            </span>
          </li>
        ))}
      </ul>

      {closed ? (
        <p className="rounded-xl bg-line px-4 py-3 text-center text-sm font-semibold text-ink-2">
          {t("Bu müraciət bağlanıb. Yeni sualınız varsa, yeni müraciət açın.")}</p>
      ) : (
        <ReplyForm ticketId={ticket.id} />
      )}
    </div>
  );
}
