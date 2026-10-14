import { getNotifications } from "@/lib/personal";

/** The latest notifications for the bell's small window. */
export async function GET() {
  const res = await getNotifications(1, 20);
  if (!res.ok) {
    return Response.json({ message: "failed" }, { status: res.status === 401 ? 401 : 502 });
  }
  return Response.json(res.data.items, { headers: { "Cache-Control": "private, no-store" } });
}
