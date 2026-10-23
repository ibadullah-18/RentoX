import { canSeeBranding, getAccessToken, isAdmin } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const MAX_BYTES = 5 * 1024 * 1024;
const SLOTS = new Set(["header-logo", "header-logo-dark", "footer-logo", "footer-logo-dark", "icon", "share-image"]);

/** A picture of the brand (a logo or the icon): the owner's browser posts the file here, this adds the token. */
export async function PUT(request: Request, ctx: RouteContext<"/bff/admin-branding-image/[slot]">) {
  const { slot } = await ctx.params;
  if (!SLOTS.has(slot) || !(await isAdmin()) || !(await canSeeBranding())) {
    return new Response(null, { status: 404 });
  }

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
    const res = await fetch(`${API_BASE}/api/admin/branding/${slot}/image`, {
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
