import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);
const MAX_BYTES = 5 * 1024 * 1024;

/** The profile photo: the browser posts the file here, this adds the token. */
export async function POST(request: Request) {
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
    const res = await fetch(`${API_BASE}/api/account/me/profile-image`, {
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

export async function DELETE() {
  const token = await getAccessToken();
  if (!token) return Response.json({ message: "unauthorized" }, { status: 401 });

  try {
    const res = await fetch(`${API_BASE}/api/account/me/profile-image`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` },
      cache: "no-store",
    });
    return new Response(null, { status: res.ok ? 204 : res.status });
  } catch {
    return Response.json({ message: "network" }, { status: 502 });
  }
}
