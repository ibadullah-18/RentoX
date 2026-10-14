import "server-only";
import { notFound, redirect } from "next/navigation";
import { getSession, isAdmin } from "./session";

/** Signed out: to the login page. Signed in without admin rights: as if the page did not exist. */
export async function requireAdmin(next: string): Promise<void> {
  if (!(await getSession())) redirect(`/giris?next=${encodeURIComponent(next)}`);
  if (!(await isAdmin())) notFound();
}

export * from "./admin-labels";
