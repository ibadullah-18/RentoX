import "server-only";
import { authed } from "./authed";

/** The banned words found in each of these listings (listings without any are left out). Empty if the API cannot say. */
export async function getFlags(ids: string[]): Promise<Record<string, string[]>> {
  if (ids.length === 0) return {};
  const res = await authed<Record<string, string[]>>("/api/admin/banned-words/flags", {
    query: { ids: ids.slice(0, 50).join(",") },
  });
  return res.ok ? res.data : {};
}
