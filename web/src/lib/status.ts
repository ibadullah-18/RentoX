import { tx } from "./i18n/locales";
import { Status } from "./types";

/** A listing whose 30 days are over counts as expired even before the server flips it. */
export function effectiveStatus(status: number, expiresAtUtc?: string | null): number {
  if (
    (status === Status.active || status === Status.deactivated) &&
    expiresAtUtc &&
    new Date(expiresAtUtc).getTime() <= Date.now()
  ) {
    return Status.expired;
  }
  return status;
}

export const statusLabel: Record<number, string> = {
  [Status.draft]: tx("Qaralama"),
  [Status.pendingReview]: tx("Yoxlamada"),
  [Status.active]: tx("Aktiv"),
  [Status.rejected]: tx("Rədd edilib"),
  [Status.expired]: tx("Müddəti bitib"),
  [Status.deactivated]: tx("Dayandırılıb"),
  [Status.paymentRequired]: tx("Ödəniş gözləyir"),
  [Status.deleted]: tx("Silinib"),
};

/** Chip colours: green live, amber waiting, red problem, grey the rest. */
export const statusTone: Record<number, string> = {
  [Status.active]: "bg-success-soft text-success",
  [Status.pendingReview]: "bg-warning-soft text-warning",
  [Status.paymentRequired]: "bg-warning-soft text-warning",
  [Status.rejected]: "bg-danger/10 text-danger",
  [Status.draft]: "bg-primary-soft text-primary",
  [Status.expired]: "bg-line text-ink-2",
  [Status.deactivated]: "bg-line text-ink-2",
};

export const money = new Intl.NumberFormat("az", { maximumFractionDigits: 2 });

export const PRICES = { vip: 10, bump: 5, activation: 1, vipDays: 7 } as const;
