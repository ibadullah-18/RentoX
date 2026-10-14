import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const ID = /^[0-9a-f-]{36}$/i;

/** A photo from a conversation: private, so it is served only with the token. */
export async function GET(
  _request: Request,
  ctx: RouteContext<"/bff/chat-image/[cid]/[imageId]">,
) {
  const { cid, imageId } = await ctx.params;
  if (!ID.test(cid) || !ID.test(imageId)) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return new Response(null, { status: 401 });

  try {
    const res = await fetch(`${API_BASE}/api/conversations/${cid}/images/${imageId}`, {
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    if (!res.ok || !res.body) return new Response(null, { status: res.status });

    return new Response(res.body, {
      headers: {
        "Content-Type": res.headers.get("Content-Type") ?? "application/octet-stream",
        // Private photos: kept by this person's browser for a while, never by a shared cache.
        "Cache-Control": "private, max-age=3600",
        "X-Content-Type-Options": "nosniff",
      },
    });
  } catch {
    return new Response(null, { status: 502 });
  }
}
