import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const ID = /^[0-9a-f-]{36}$/i;
const TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const MAX_BYTES = 10 * 1024 * 1024;
const MAX_FILES = 5;

/**
 * Sends photos (with an optional caption) in one message. The browser posts
 * them here; this adds the token and passes them on to the API.
 */
export async function POST(request: Request, ctx: RouteContext<"/bff/conversations/[id]/images">) {
  const { id } = await ctx.params;
  if (!ID.test(id)) return Response.json({ message: "not found" }, { status: 404 });

  const token = await getAccessToken();
  if (!token) return Response.json({ message: "unauthorized" }, { status: 401 });

  const incoming = await request.formData().catch(() => null);
  if (!incoming) return Response.json({ message: "bad request" }, { status: 400 });

  const files = incoming.getAll("files").filter((f): f is File => f instanceof File);
  if (
    files.length < 1 ||
    files.length > MAX_FILES ||
    files.some((f) => !TYPES.has(f.type) || f.size === 0 || f.size > MAX_BYTES)
  ) {
    return Response.json({ message: "bad files" }, { status: 400 });
  }

  const form = new FormData();
  const body = incoming.get("body");
  if (typeof body === "string" && body.trim()) form.set("body", body.trim().slice(0, 4000));
  for (const f of files) form.append("files", f, f.name);

  try {
    const res = await fetch(`${API_BASE}/api/conversations/${id}/messages/images`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: form,
      cache: "no-store",
    });
    const text = await res.text();
    return new Response(text, {
      status: res.status,
      headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
    });
  } catch {
    return Response.json({ message: "network" }, { status: 502 });
  }
}
