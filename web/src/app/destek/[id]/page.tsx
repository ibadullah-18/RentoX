import { dateFmt } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { SupportRealtimeProvider } from "@/components/support/SupportRealtime";
import { TicketThread } from "@/components/support/TicketThread";
import { getTicket } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { ticketCategory, ticketStatus, ticketTone } from "@/lib/support";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Müraciət"), robots: { index: false } };
}

export default async function TicketPage({
  params,
}: PageProps<"/destek/[id]">) {
  const t = await getT();
  const locale = await getLocale();
  const { id } = await params;
  if (!(await getSession())) redirect(`/giris?next=/destek/${id}`);

  const result = await getTicket(id);
  if (!result.ok) {
    if (result.status === 401) redirect(`/giris?next=/destek/${id}`);
    if (result.status === 404 || result.status === 403) redirect("/destek");
    notFound();
  }
  const ticket = result.data;
  const messages = [...(ticket.messages ?? [])].sort((a, b) =>
    a.sentAtUtc.localeCompare(b.sentAtUtc),
  );

  // Baku time on the server and in the browser alike, so the two always print the same text.
  const day = dateFmt(locale, {
    day: "numeric",
    month: "long",
    timeZone: "Asia/Baku",
  });
  const time = dateFmt(locale, {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Asia/Baku",
  });

  return (
    <SupportRealtimeProvider>
      <TicketThread
        ticketId={ticket.id}
        subject={ticket.subject}
        category={
          ticketCategory[ticket.category]
            ? t(ticketCategory[ticket.category])
            : t("Digər")
        }
        status={ticket.status}
        statusLabel={
          ticketStatus[ticket.status] ? t(ticketStatus[ticket.status]) : "—"
        }
        statusTone={ticketTone[ticket.status] ?? ""}
        messages={messages.map((m) => ({
          id: m.id,
          isAdmin: m.isAdmin,
          body: m.body,
          sentAtUtc: m.sentAtUtc,
          images: m.images,
          day: day.format(new Date(m.sentAtUtc)),
          time: time.format(new Date(m.sentAtUtc)),
        }))}
      />
    </SupportRealtimeProvider>
  );
}
