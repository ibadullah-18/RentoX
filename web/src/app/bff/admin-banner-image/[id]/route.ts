import { getAccessToken, getSections, isAdmin } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const MAX_BYTES = 5 * 1024 * 1024;
const ID = /^[0-9a-f-]{36}$/i;

/** The picture of a home banner: the admin's browser posts the file here, this adds the token. */
export async function PUT(request: Request, ctx: RouteContext<"/bff/admin-banner-image/[id]">) {
  const { id } = await ctx.params;
  if (!ID.test(id) || !(await isAdmin())) return new Response(null, { status: 404 });
  const sections = await getSections();
  if (sections !== null && !sections.includes("catalog")) return new Response(null, { status: 404 });

  const token = await getAccessToken();
  if (!token) return Response.json({ message: "unauthorized" }, { status: 401 });

  const incoming = await request.formData().catch(() => null);
  const file = incoming?.get("file");
  if (!(file instanceof File) || !TYPES.has(file.type) || file.size === 0 || file.size > MAX_BYTES) {
    return Response.json({ message: "bad file" }, { status: 400 });
  }

  const form = new FormData();
  form.set("file", file, file.name);

  try {
    const res = await fetch(`${API_BASE}/api/admin/banners/${id}/image`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${token}` },
      body: form,
      cache: "no-store",
    });
    return new Response(null, { status: res.ok ? 204 : res.status });
  } catch {
    return Response.json({ message: "network" }, { status: 502 });
  }
}
