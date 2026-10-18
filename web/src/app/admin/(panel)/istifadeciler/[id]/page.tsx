import Link from "next/link";
import { notFound } from "next/navigation";
import { Card, LoadProblem, PageHead, Pill, dateTime, type Tone } from "@/components/admin/ui";
import { AnnouncementForm } from "@/components/admin/AnnouncementForm";
import { BlockUserPanel, UnblockPanel } from "@/components/admin/AdminControls";
import { WalletAdjustForm } from "@/components/admin/WalletAdjustForm";
import { PrintButton } from "@/components/admin/PrintButton";
import { blockUser, unblockUser } from "@/app/admin/(panel)/actions";
import { adminHref } from "@/lib/admin-path";
import { chatReason, listingStatusLabel, reportReason, reportStatus, walletDirection, walletType } from "@/lib/admin-labels";
import { userStatusLabel, type AdminListingItem, type AdminUserDetails } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import { getRoles, getSections } from "@/lib/session";
import { FreeServicesPanel } from "@/components/admin/FreeServicesPanel";
import type { Paged } from "@/lib/types";

const tone: Record<number, Tone> = { 1: "success", 2: "warning", 3: "danger", 4: "muted" };
const money = new Intl.NumberFormat("az", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const languages: Record<number, string> = { 1: "Azərbaycanca", 2: "Русский", 3: "English" };

/** One person: contact, listings, balance and the last money moves. */
export default async function UserAdminPage({ params }: PageProps<"/admin/istifadeciler/[id]">) {
  const { id } = await params;
  const [details, listings] = await Promise.all([
    authed<AdminUserDetails>(`/api/admin/users/${encodeURIComponent(id)}`),
    authed<Paged<AdminListingItem>>("/api/admin/listings", { query: { ownerId: id, pageSize: 10 } }),
  ]);
  if (!details.ok) {
    if (details.status === 404 && details.message !== "") notFound();
    return <LoadProblem what="İstifadəçi" status={details.status} />;
  }
  const d = details.data;
  const sections = await getSections();
  const isOwner = (await getRoles()).includes("SuperAdmin");
  const canAdjust = sections === null || sections.includes("finance");
  const u = d.user;

  return (
    <div className="space-y-5">
      <PageHead
        title={u.fullName}
        hint={u.phone ?? undefined}
        back={{ href: adminHref("/istifadeciler"), label: "İstifadəçilər" }}
        actions={
          <>
            <Pill tone={tone[u.status]}>{userStatusLabel[u.status] ?? u.status}</Pill>
            <PrintButton />
          </>
        }
      />

      {u.status === 1 && <BlockUserPanel block={blockUser.bind(null, u.id)} />}
      {(u.status === 2 || u.status === 3) && <UnblockPanel unblock={unblockUser.bind(null, u.id)} reason={d.statusReason} />}

      {isOwner && u.status === 1 && <FreeServicesPanel userId={u.id} services={d.freeServices ?? 0} />}

      <div className="grid gap-3 sm:grid-cols-3">
        <Stat label="Balans" value={`${money.format(u.balance)} ${u.currency}`} />
        <Stat label="Elanlar" value={`${u.activeListingCount} aktiv / ${u.listingCount}`} />
        <Stat label="Son aktivlik" value={dateTime(u.lastSeenAtUtc)} />
      </div>

      {u.status === 1 && (
        <Card title="Mesaj göndər">
          <div className="p-4">
            <AnnouncementForm userId={u.id} />
          </div>
        </Card>
      )}

      {d.reports && (
        <Card title="Bu şəxs haqqında şikayətlər">
          <div className="grid grid-cols-2 gap-px bg-line sm:grid-cols-4">
            {[
              { label: "Cəmi", n: d.reports.total, tone: "" },
              { label: "Gözləyir / baxılır", n: d.reports.open, tone: d.reports.open > 0 ? "text-warning" : "" },
              { label: "Təsdiqlənib", n: d.reports.upheld, tone: d.reports.upheld > 0 ? "text-danger" : "" },
              { label: "Rədd edilib", n: d.reports.dismissed, tone: "" },
            ].map((x) => (
              <div key={x.label} className="bg-surface px-4 py-3">
                <p className="text-xs text-ink-2">{x.label}</p>
                <p className={`text-xl font-extrabold ${x.tone}`}>{x.n}</p>
              </div>
            ))}
          </div>
          {d.reports.recent.length === 0 ? (
            <p className="px-4 py-5 text-center text-sm text-ink-2">Şikayət yoxdur.</p>
          ) : (
            <ul className="divide-y divide-line border-t border-line">
              {d.reports.recent.map((r) => (
                <li key={`${r.kind}-${r.id}`}>
                  <Link
                    href={adminHref(r.kind === "conversation" ? `/yazisma-sikayetleri/${r.id}` : `/sikayetler/${r.id}`)}
                    className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft"
                  >
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-semibold">
                        {r.kind === "listing" ? "Elan" : r.kind === "store" ? "Mağaza" : "Yazışma"} ·{" "}
                        {(r.kind === "conversation" ? chatReason : reportReason)[r.reason] ?? r.reason}
                      </span>
                      <span className="block truncate text-xs text-ink-2">
                        {dateTime(r.createdAtUtc)}
                        {r.note ? ` · ${r.note}` : ""}
                      </span>
                    </span>
                    <Pill tone={r.status === 3 ? "danger" : r.status === 4 ? "muted" : "warning"}>{reportStatus[r.status] ?? r.status}</Pill>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}

      <Card title="Məlumat">
        <dl className="divide-y divide-line text-sm">
          <Row label="Qeydiyyat" value={dateTime(u.registeredAtUtc)} />
          <Row label="Dil" value={languages[d.preferredLanguage] ?? "—"} />
          <Row label="Haqqında" value={d.bio || "—"} />
        </dl>
      </Card>

      <Card title="Son elanlar">
        <ul className="divide-y divide-line">
          {listings.ok && listings.data.items.length === 0 && (
            <li className="px-4 py-6 text-center text-sm text-ink-2">Elan yoxdur.</li>
          )}
          {listings.ok &&
            listings.data.items.map((l) => (
              <li key={l.id}>
                <Link href={adminHref(`/elanlar/${l.id}`)} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
                  <span className="min-w-0 flex-1 truncate font-semibold">{l.title}</span>
                  <span className="text-xs text-ink-2">{dateTime(l.createdAtUtc)}</span>
                  <Pill>{listingStatusLabel[l.status] ?? l.status}</Pill>
                </Link>
              </li>
            ))}
        </ul>
        {listings.ok && listings.data.totalCount > 10 && (
          <div className="border-t border-line px-4 py-3">
            <Link href={adminHref(`/elanlar?status=all&q=${encodeURIComponent(u.phone ?? u.fullName)}`)} className="text-sm font-bold text-primary hover:underline">
              Hamısına bax ({listings.data.totalCount})
            </Link>
          </div>
        )}
      </Card>

      {canAdjust && d.user.status !== 4 && (
        <Card title="Pul kisəsini əllə dəyiş">
          <WalletAdjustForm userId={d.user.id} balance={d.user.balance} currency={d.user.currency} />
        </Card>
      )}

      <Card title="Pul kisəsinin son əməliyyatları">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[560px] text-left text-sm">
            <thead className="border-b border-line text-ink-2">
              <tr>
                <th className="px-4 py-3 font-semibold">Vaxt</th>
                <th className="px-4 py-3 font-semibold">Növ</th>
                <th className="px-4 py-3 text-right font-semibold">Məbləğ</th>
                <th className="px-4 py-3 text-right font-semibold">Qalıq</th>
              </tr>
            </thead>
            <tbody>
              {d.recentTransactions.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-ink-2">
                    Əməliyyat yoxdur.
                  </td>
                </tr>
              )}
              {d.recentTransactions.map((t) => (
                <tr key={t.id} className="border-b border-line last:border-b-0">
                  <td className="whitespace-nowrap px-4 py-2.5">{dateTime(t.occurredAtUtc)}</td>
                  <td className="px-4 py-2.5">{walletType[t.type] ?? t.type}</td>
                  <td className={`px-4 py-2.5 text-right font-semibold ${t.direction === 1 ? "text-success" : "text-danger"}`}>
                    {t.direction === 1 ? "+" : "−"}
                    {money.format(t.amount)}
                    <span className="sr-only"> {walletDirection[t.direction]}</span>
                  </td>
                  <td className="px-4 py-2.5 text-right">{money.format(t.balanceAfter)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-line bg-surface p-4">
      <p className="text-sm text-ink-2">{label}</p>
      <p className="mt-1 text-lg font-extrabold">{value}</p>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 px-4 py-2.5">
      <dt className="text-ink-2">{label}</dt>
      <dd className="text-right font-semibold">{value}</dd>
    </div>
  );
}
