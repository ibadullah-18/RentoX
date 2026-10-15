import { getAccessToken, getSections, isAdmin } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const PAGE_SIZE = 50;
const MAX_PAGES = 40; // 2000 rows: a list for a person to open, not a data dump

type Column = { head: string; value: (row: Record<string, unknown>) => unknown };

const kinds: Record<string, { path: string; section: string; file: string; columns: Column[] }> = {
  users: {
    path: "/api/admin/users",
    section: "users",
    file: "istifadeciler",
    columns: [
      { head: "ID", value: (r) => r.id },
      { head: "Ad", value: (r) => r.fullName },
      { head: "Telefon", value: (r) => r.phone },
      { head: "Status", value: (r) => r.status },
      { head: "Qeydiyyat", value: (r) => r.registeredAtUtc },
      { head: "Son aktivlik", value: (r) => r.lastSeenAtUtc },
      { head: "Elanlar", value: (r) => r.listingCount },
      { head: "Aktiv elanlar", value: (r) => r.activeListingCount },
      { head: "Balans", value: (r) => r.balance },
      { head: "Valyuta", value: (r) => r.currency },
    ],
  },
  stores: {
    path: "/api/admin/stores",
    section: "stores",
    file: "magazalar",
    columns: [
      { head: "ID", value: (r) => r.id },
      { head: "Ad", value: (r) => r.name },
      { head: "Sahibi", value: (r) => r.ownerName },
      { head: "Telefon", value: (r) => r.ownerPhone },
      { head: "Status", value: (r) => r.status },
      { head: "Rədd səbəbi", value: (r) => r.rejectionReason },
      { head: "Yaradılıb", value: (r) => r.createdAtUtc },
    ],
  },
  listings: {
    path: "/api/admin/listings",
    section: "listings",
    file: "elanlar",
    columns: [
      { head: "ID", value: (r) => r.id },
      { head: "Başlıq", value: (r) => r.title },
      { head: "Sahibi", value: (r) => r.ownerName },
      { head: "Telefon", value: (r) => r.ownerPhone },
      { head: "Qiymət", value: (r) => r.price },
      { head: "Valyuta", value: (r) => r.currency },
      { head: "Status", value: (r) => r.status },
      { head: "Baxış", value: (r) => r.viewCount },
      { head: "Yaradılıb", value: (r) => r.createdAtUtc },
      { head: "Bitir", value: (r) => r.expiresAtUtc },
    ],
  },
};

/** One cell of a CSV; a text that a spreadsheet would run as a formula is made plain text. */
function cell(value: unknown): string {
  let text = value === null || value === undefined ? "" : String(value);
  if (/^[=+\-@\t\r]/.test(text) && Number.isNaN(Number(text))) text = `'${text}`;
  return /[",\n\r;]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

/** The list an admin is looking at (same search and status), as a file for a spreadsheet. */
export async function GET(request: Request, ctx: RouteContext<"/bff/admin-export/[kind]">) {
  const { kind } = await ctx.params;
  const spec = kinds[kind];
  if (!spec || !(await isAdmin())) return new Response(null, { status: 404 });

  const sections = await getSections();
  if (sections !== null && !sections.includes(spec.section)) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return new Response(null, { status: 401 });

  const incoming = new URL(request.url).searchParams;
  const rows: string[] = [spec.columns.map((c) => cell(c.head)).join(";")];

  try {
    for (let page = 1; page <= MAX_PAGES; page++) {
      const url = new URL(spec.path, API_BASE);
      for (const key of ["q", "status"]) {
        const v = incoming.get(key);
        if (v && v !== "all") url.searchParams.set(key, v);
      }
      url.searchParams.set("page", String(page));
      url.searchParams.set("pageSize", String(PAGE_SIZE));

      const res = await fetch(url, { headers: { Authorization: `Bearer ${token}` }, cache: "no-store" });
      if (!res.ok) return new Response(null, { status: res.status === 401 || res.status === 403 ? res.status : 502 });
      const data = (await res.json()) as { items: Record<string, unknown>[]; totalPages: number };
      for (const item of data.items) rows.push(spec.columns.map((c) => cell(c.value(item))).join(";"));
      if (page >= data.totalPages) break;
    }
  } catch {
    return new Response(null, { status: 502 });
  }

  // A byte-order mark so Excel reads the Azerbaijani letters right; ";" because Excel there splits on it.
  const stamp = new Date().toISOString().slice(0, 10);
  return new Response("﻿" + rows.join("\r\n") + "\r\n", {
    headers: {
      "Content-Type": "text/csv; charset=utf-8",
      "Content-Disposition": `attachment; filename="${spec.file}-${stamp}.csv"`,
      "Cache-Control": "private, no-store",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
