import type { Tone } from "@/components/admin/ui";

/** "3 gün əvvəl", "5 saat əvvəl": how long ago something happened. */
export function ago(iso: string, now: number = Date.now()): string {
  const minutes = Math.max(0, Math.floor((now - Date.parse(iso)) / 60000));
  if (minutes < 1) return "indicə";
  if (minutes < 60) return `${minutes} dəq əvvəl`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} saat əvvəl`;
  return `${Math.floor(hours / 24)} gün əvvəl`;
}

/** How urgent an open report is by how long it has waited. */
export type Urgency = "fresh" | "waiting" | "late";

export function urgency(iso: string, now: number = Date.now()): Urgency {
  const hours = (now - Date.parse(iso)) / 3_600_000;
  if (hours < 24) return "fresh";
  return hours < 72 ? "waiting" : "late";
}

export const urgencyClass: Record<Urgency, string> = {
  fresh: "text-ink-2",
  waiting: "font-semibold text-warning",
  late: "font-bold text-danger",
};

/** Fraud and prohibited content are the serious ones: they are red; spam and the like are amber. */
export const contentReasonTone: Record<number, Tone> = {
  1: "warning",
  2: "danger",
  3: "danger",
  4: "warning",
  5: "muted",
  6: "muted",
};

export const chatReasonTone: Record<number, Tone> = {
  1: "warning",
  2: "danger",
  3: "danger",
  4: "danger",
  5: "muted",
};

export const reportStatusTone: Record<number, Tone> = { 1: "warning", 2: "primary", 3: "success", 4: "muted" };

/** A listing can be taken down while it is live, paused, waiting for review or unpaid. */
export const LISTING_CAN_TAKE_DOWN = new Set([2, 3, 6, 8]);

/** A store can be suspended while it is active. */
export const STORE_CAN_SUSPEND = new Set([3]);
