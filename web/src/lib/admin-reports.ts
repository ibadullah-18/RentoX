import "server-only";
import { authed } from "./authed";
import type { Paged } from "./types";

/** How many reports are in a state (one tiny request: only the total is read). */
async function count(path: string, status: number): Promise<number> {
  const res = await authed<Paged<unknown>>(path, { query: { status, page: 1, pageSize: 1 } });
  return res.ok ? res.data.totalCount : 0;
}

export type ReportCounts = {
  content: { waiting: number; reviewing: number };
  chat: { waiting: number; reviewing: number };
};

/** The numbers on the report tabs and chips. */
export async function getReportCounts(): Promise<ReportCounts> {
  const [cw, cr, tw, tr] = await Promise.all([
    count("/api/admin/content-reports", 1),
    count("/api/admin/content-reports", 2),
    count("/api/admin/conversation-reports", 1),
    count("/api/admin/conversation-reports", 2),
  ]);
  return { content: { waiting: cw, reviewing: cr }, chat: { waiting: tw, reviewing: tr } };
}
