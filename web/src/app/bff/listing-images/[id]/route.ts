import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const ID = /^[0-9a-f-]{36}$/i;

/**
 * A photo of the signed-in person's own listing. Photos of a listing that is
 * not public yet (draft, in review, paused) are only served with the owner's
 * token, which an <img> tag cannot send: this adds it. Never cached.
 */
export async function GET(
  _request: Request,
  ctx: RouteContext<"/bff/listing-images/[id]">,
) {
  const { id } = await ctx.params;
  if (!ID.test(id)) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return new Response(null, { status: 401 });

  try {
    const res = await fetch(`${API_BASE}/api/listing-images/${id}`, {
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
