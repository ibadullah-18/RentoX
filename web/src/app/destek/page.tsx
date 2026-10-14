import { dateFmt } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { ChevronRight, LifeBuoy, Plus } from "lucide-react";
import { Pagination } from "@/components/Pagination";
import { getTickets } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { ticketCategory, ticketStatus, ticketTone } from "@/lib/support";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Dəstək"), robots: { index: false } };
}

export default async function SupportPage({ searchParams }: PageProps<"/destek">) {
  const t = await getT();
  const locale = await getLocale();
  if (!(await getSession())) redirect("/giris?next=/destek");

  const raw = (await searchParams).page;
  const page = Math.max(1, Number.parseInt(Array.isArray(raw) ? raw[0] : (raw ?? "1"), 10) || 1);
  const result = await getTickets(page);
  if (!result.ok && result.status === 401) redirect("/giris?next=/destek");
  const data = result.ok ? result.data : null;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-extrabold">{t("Dəstək")}</h1>
        <Link
          href="/destek/yeni"
          className="flex h-11 items-center gap-2 rounded-xl bg-primary px-5 font-bold text-white hover:bg-primary-press"
        >
          <Plus className="size-5" aria-hidden />
          {t("Yeni müraciət")}</Link>
      </div>

      {!data || data.items.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-16 text-center">
          <LifeBuoy className="size-12 text-primary" aria-hidden />
          <h2 className="text-lg font-extrabold">{t("Hələ müraciətiniz yoxdur")}</h2>
          <p className="max-w-sm text-ink-2">{t("Sualınız və ya probleminiz varsa, bizə yazın.")}</p>
        </div>
      ) : (
        <>
          <ul className="space-y-2">
            {data.items.map((ticket) => (
              <li key={ticket.id}>
                <Link
                  href={`/destek/${ticket.id}`}
                  className="flex items-center gap-3 rounded-xl border border-line bg-surface p-4 hover:border-primary"
                >
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-bold">{ticket.subject}</span>
                    <span className="text-sm text-ink-2">
                      {ticketCategory[ticket.category] ? t(ticketCategory[ticket.category]) : t("Digər")} · {dateFmt(locale, { day: "numeric", month: "long", year: "numeric" }).format(new Date(ticket.updatedAtUtc ?? ticket.createdAtUtc))}
                    </span>
                  </span>
                  <span className={`rounded-md px-2.5 py-1 text-xs font-bold ${ticketTone[ticket.status] ?? ""}`}>
                    {ticketStatus[ticket.status] ? t(ticketStatus[ticket.status]) : "—"}
                  </span>
                  <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            hrefFor={(p) => (p === 1 ? "/destek" : `/destek?page=${p}`)}
          />
        </>
      )}
    </div>
  );
}
