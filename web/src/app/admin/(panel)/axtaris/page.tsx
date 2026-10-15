import Link from "next/link";
import { Search } from "lucide-react";
import { Card, PageHead, Pill, type Tone } from "@/components/admin/ui";
import { adminHref } from "@/lib/admin-path";
import { listingStatusLabel, storeStatusLabel } from "@/lib/admin-labels";
import { userStatusLabel, type AdminStoreItem, type AdminUserItem, type AdminListingItem } from "@/lib/admin-catalog";
import { authed } from "@/lib/authed";
import { getSections } from "@/lib/session";
import type { Paged } from "@/lib/types";

const SHOWN = 5;
const userTone: Record<number, Tone> = { 1: "success", 2: "warning", 3: "danger", 4: "muted" };

/** One search for people, listings and stores together (only what the admin may open). */
export default async function SearchPage({ searchParams }: PageProps<"/admin/axtaris">) {
  const sp = await searchParams;
  const raw = sp.q;
  const q = ((Array.isArray(raw) ? raw[0] : raw) ?? "").trim().slice(0, 100);

  const sections = await getSections();
  const has = (section: string) => sections === null || sections.includes(section);

  const [users, listings, stores] = q
    ? await Promise.all([
        has("users") ? authed<Paged<AdminUserItem>>("/api/admin/users", { query: { q, pageSize: SHOWN } }) : null,
        has("listings") ? authed<Paged<AdminListingItem>>("/api/admin/listings", { query: { q, pageSize: SHOWN } }) : null,
        has("stores") ? authed<Paged<AdminStoreItem>>("/api/admin/stores", { query: { q, pageSize: SHOWN } }) : null,
      ])
    : [null, null, null];

  const total = (r: { ok: true; data: Paged<unknown> } | { ok: false } | null) => (r && r.ok ? r.data.totalCount : 0);
  const nothing = q && total(users) + total(listings) + total(stores) === 0;

  return (
    <div className="space-y-6">
      <PageHead title="Axtarış" hint="Ad, telefon, elanın başlığı və ya mağazanın adı ilə axtarın." />

      <form action={adminHref("/axtaris")} className="flex gap-2">
        <div className="flex h-11 min-w-0 flex-1 items-center gap-2 rounded-lg border border-line bg-surface px-3 focus-within:border-primary">
          <Search className="size-4 shrink-0 text-muted" aria-hidden />
          <input
            name="q"
            defaultValue={q}
            autoFocus
            placeholder="Axtarın…"
            className="min-w-0 flex-1 bg-transparent text-[15px] outline-none"
          />
        </div>
        <button type="submit" className="h-11 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press">
          Axtar
        </button>
      </form>

      {!q && <p className="text-sm text-ink-2">Yuxarıdakı xanaya yazın. Menyudakı axtarışı da işlədə bilərsiniz.</p>}
      {nothing && <p className="rounded-xl border border-line bg-surface px-4 py-6 text-center text-ink-2">Heç nə tapılmadı.</p>}

      {users?.ok && users.data.items.length > 0 && (
        <Card title={`İstifadəçilər (${users.data.totalCount})`}>
          <ul className="divide-y divide-line">
            {users.data.items.map((u) => (
              <li key={u.id}>
                <Link href={adminHref(`/istifadeciler/${u.id}`)} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{u.fullName}</span>
                    <span className="block truncate text-xs text-ink-2">{u.phone ?? "—"}</span>
                  </span>
                  <Pill tone={userTone[u.status]}>{userStatusLabel[u.status] ?? u.status}</Pill>
                </Link>
              </li>
            ))}
          </ul>
          {users.data.totalCount > SHOWN && (
            <More href={adminHref(`/istifadeciler?q=${encodeURIComponent(q)}`)} count={users.data.totalCount} />
          )}
        </Card>
      )}

      {listings?.ok && listings.data.items.length > 0 && (
        <Card title={`Elanlar (${listings.data.totalCount})`}>
          <ul className="divide-y divide-line">
            {listings.data.items.map((l) => (
              <li key={l.id}>
                <Link href={adminHref(`/elanlar/${l.id}`)} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{l.title}</span>
                    <span className="block truncate text-xs text-ink-2">
                      {l.ownerName}
                      {l.ownerPhone ? ` · ${l.ownerPhone}` : ""}
                    </span>
                  </span>
                  <Pill>{listingStatusLabel[l.status] ?? l.status}</Pill>
                </Link>
              </li>
            ))}
          </ul>
          {listings.data.totalCount > SHOWN && (
            <More href={adminHref(`/elanlar?status=all&q=${encodeURIComponent(q)}`)} count={listings.data.totalCount} />
          )}
        </Card>
      )}

      {stores?.ok && stores.data.items.length > 0 && (
        <Card title={`Mağazalar (${stores.data.totalCount})`}>
          <ul className="divide-y divide-line">
            {stores.data.items.map((s) => (
              <li key={s.id}>
                <Link href={adminHref(`/magazalar/${s.id}`)} className="flex items-center gap-3 px-4 py-2.5 hover:bg-primary-soft">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{s.name}</span>
                    <span className="block truncate text-xs text-ink-2">
                      {s.ownerName}
                      {s.ownerPhone ? ` · ${s.ownerPhone}` : ""}
                    </span>
                  </span>
                  <Pill>{storeStatusLabel[s.status] ?? s.status}</Pill>
                </Link>
              </li>
            ))}
          </ul>
          {stores.data.totalCount > SHOWN && (
            <More href={adminHref(`/magazalar?status=all&q=${encodeURIComponent(q)}`)} count={stores.data.totalCount} />
          )}
        </Card>
      )}
    </div>
  );
}

function More({ href, count }: { href: string; count: number }) {
  return (
    <div className="border-t border-line px-4 py-3">
      <Link href={href} className="text-sm font-bold text-primary hover:underline">
        Hamısına bax ({count})
      </Link>
    </div>
  );
}
