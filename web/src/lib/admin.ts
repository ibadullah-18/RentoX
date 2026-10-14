import "server-only";
import { cache } from "react";
import { adminHref } from "./admin-path";
import { notFound, redirect } from "next/navigation";
import type { Dashboard } from "./admin-types";
import { authed } from "./authed";
import { getSections, getSession, isAdmin } from "./session";

/** Signed out: to the login page. Signed in without admin rights: as if the page did not exist. */
export async function requireAdmin(next: string): Promise<void> {
  if (!(await getSession())) redirect(`${adminHref("/giris")}?next=${encodeURIComponent(next)}`);
  if (!(await isAdmin())) notFound();
}

/** The dashboard numbers; one call per request even when the layout and the page both ask. */
export const getDashboard = cache(() => authed<Dashboard>("/api/admin/analytics/dashboard"));

/** A section of the panel: someone limited to other sections sees it as if it did not exist. */
export async function requireSection(section: string): Promise<void> {
  const allowed = await getSections();
  if (allowed !== null && !allowed.includes(section)) notFound();
}

export * from "./admin-labels";
