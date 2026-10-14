import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const ID = /^[0-9a-f-]{36}$/i;

/** The owner's own store logo/cover while the store is not public yet. */
export async function GET(_request: Request, ctx: RouteContext<"/bff/store-image/[id]/[kind]">) {
  const { id, kind } = await ctx.params;
  if (!ID.test(id) || (kind !== "logo" && kind !== "cover")) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return new Response(null, { status: 401 });

  try {
    const res = await fetch(`${API_BASE}/api/stores/${id}/${kind}`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    if (!res.ok || !res.body) return new Response(null, { status: res.status });
    return new Response(res.body, {
      headers: {
        "Content-Type": res.headers.get("Content-Type") ?? "application/octet-stream",
        "Cache-Control": "private, no-store",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return new Response(null, { status: 502 });
  }
}
