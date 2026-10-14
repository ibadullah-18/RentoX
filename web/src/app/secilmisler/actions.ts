"use server";

import { authed } from "@/lib/authed";

export type FavoriteResult = { ok: true } | { ok: false; unauthorized: boolean };

/** Adds the listing to the signed-in person's favourites, or takes it out. */
export async function setFavorite(listingId: string, on: boolean): Promise<FavoriteResult> {
  if (!/^[0-9a-f-]{36}$/i.test(listingId)) return { ok: false, unauthorized: false };

  const res = await authed(`/api/favorites/${listingId}`, {
    method: on ? "POST" : "DELETE",
  });
  return res.ok ? { ok: true } : { ok: false, unauthorized: res.status === 401 };
}
