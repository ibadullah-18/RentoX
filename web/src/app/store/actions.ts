"use server";

import { revalidatePath } from "next/cache";
import { authed } from "@/lib/authed";

export type Followed = { ok: true } | { ok: false; unauthorized: boolean };

const ID = /^[0-9a-f-]{36}$/i;

/** Follows a store, or stops following it. */
export async function setFollow(storeId: string, slug: string, on: boolean): Promise<Followed> {
  if (!ID.test(storeId)) return { ok: false, unauthorized: false };
  const res = await authed(`/api/stores/${storeId}/follow`, { method: on ? "POST" : "DELETE" });
  revalidatePath(`/store/${slug}`);
  return res.ok ? { ok: true } : { ok: false, unauthorized: res.status === 401 };
}
