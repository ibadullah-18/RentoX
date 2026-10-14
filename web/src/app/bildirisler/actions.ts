"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";

const ID = /^[0-9a-f-]{36}$/i;

export async function markNotificationRead(id: string): Promise<void> {
  if (!ID.test(id)) return;
  await authed(`/api/notifications/${id}/read`, { method: "PATCH" });
  revalidatePath("/bildirisler");
}

export async function markAllNotificationsRead(): Promise<void> {
  await authed("/api/notifications/read-all", { method: "PATCH" });
  revalidatePath("/bildirisler");
}
