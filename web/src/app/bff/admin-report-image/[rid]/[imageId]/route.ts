import { getAccessToken, isAdmin } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const ID = /^[0-9a-f-]{36}$/i;

/** A photo from a reported conversation, for the admin who reviews the report. */
export async function GET(
  _request: Request,
  ctx: RouteContext<"/bff/admin-report-image/[rid]/[imageId]">,
) {
  const { rid, imageId } = await ctx.params;
  if (!ID.test(rid) || !ID.test(imageId)) return new Response(null, { status: 404 });
  if (!(await isAdmin())) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return new Response(null, { status: 401 });

  try {
    const res = await fetch(`${API_BASE}/api/admin/conversation-reports/${rid}/images/${imageId}`, {
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
