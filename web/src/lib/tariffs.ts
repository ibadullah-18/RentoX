import "server-only";

/** The prices of the paid services, as the site shows them. */
export type Prices = {
  freeLimit: number;
  activation: number;
  bump: number;
  vip: number;
  vipDays: number;
};

/** What the site started with: used until the API answers (or if it cannot). */
export const startingPrices: Prices = { freeLimit: 5, activation: 1, bump: 5, vip: 10, vipDays: 7 };

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

export async function getPrices(): Promise<Prices> {
  try {
    const res = await fetch(`${API_BASE}/api/tariffs`, { next: { revalidate: 30, tags: ["tariffs"] } });
    if (!res.ok) return startingPrices;
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
    };
  } catch {
    return startingPrices;
  }
}
