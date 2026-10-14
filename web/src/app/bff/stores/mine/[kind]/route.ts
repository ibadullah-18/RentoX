import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const LIMITS = { logo: 5 * 1024 * 1024, cover: 10 * 1024 * 1024 } as const;

function kindOf(value: string): "logo" | "cover" | null {
  return value === "logo" || value === "cover" ? value : null;
}

/** A store logo or cover: the browser posts the file here, this adds the token. */
export async function POST(request: Request, ctx: RouteContext<"/bff/stores/mine/[kind]">) {
  const kind = kindOf((await ctx.params).kind);
  if (!kind) return Response.json({ message: "not found" }, { status: 404 });

  const token = await getAccessToken();
  if (!token) return Response.json({ message: "unauthorized" }, { status: 401 });

  const incoming = await request.formData().catch(() => null);
  const file = incoming?.get("file");
  if (!(file instanceof File) || !TYPES.has(file.type) || file.size === 0 || file.size > LIMITS[kind]) {
    return Response.json({ message: "bad file" }, { status: 400 });
  }

  const form = new FormData();
  form.set("file", file, file.name);

  try {
    const res = await fetch(`${API_BASE}/api/stores/mine/${kind}`, {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: form,
      cache: "no-store",
    });
    return new Response(null, { status: res.ok ? 204 : res.status });
  } catch {
    return Response.json({ message: "network" }, { status: 502 });
  }
}
