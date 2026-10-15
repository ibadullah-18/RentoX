import "server-only";

/** The prices of the paid services, as the site shows them. */
export type Prices = {
  freeLimit: number;
  activation: number;
  bump: number;
  vip: number;
  vipDays: number;
  /** How many days a listing stays on show after it is published or renewed. */
  lifetimeDays: number;
  /** How many photos one listing may have (an admin can change it). */
  maxPhotos: number;
};

/** What the site started with: used until the API answers (or if it cannot). */
export const startingPrices: Prices = { freeLimit: 5, activation: 1, bump: 5, vip: 10, vipDays: 7, lifetimeDays: 30, maxPhotos: 15 };

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

/** The settings an admin can change (days a listing lives, photos per listing); the starting values if the API cannot say. */
async function getSettings(): Promise<{ lifetimeDays: number; maxPhotos: number }> {
  const starting = { lifetimeDays: startingPrices.lifetimeDays, maxPhotos: startingPrices.maxPhotos };
  try {
    const res = await fetch(`${API_BASE}/api/settings`, { next: { revalidate: 30, tags: ["settings"] } });
    if (!res.ok) return starting;
    const s = (await res.json()) as { listingLifetimeDays?: number; maxListingPhotos?: number };
    return { lifetimeDays: s.listingLifetimeDays ?? starting.lifetimeDays, maxPhotos: s.maxListingPhotos ?? starting.maxPhotos };
  } catch {
    return starting;
  }
}

export async function getPrices(): Promise<Prices> {
  const { lifetimeDays, maxPhotos } = await getSettings();
  try {
    const res = await fetch(`${API_BASE}/api/tariffs`, { next: { revalidate: 30, tags: ["tariffs"] } });
    if (!res.ok) return { ...startingPrices, lifetimeDays, maxPhotos };
    const t = (await res.json()) as {
      freeActiveListingLimit: number;
      activationFee: number;
      bumpPrice: number;
      vipPrice: number;
      vipDurationDays: number;
    };
    return {
      freeLimit: t.freeActiveListingLimit,
      activation: t.activationFee,
      bump: t.bumpPrice,
      vip: t.vipPrice,
      vipDays: t.vipDurationDays,
      lifetimeDays,
      maxPhotos,
    };
  } catch {
    return startingPrices;
  }
}
