/**
 * Where the admin panel lives on the web. It is not /admin on purpose: the
 * address is part of the protection (it cannot be guessed), on top of the
 * sign-in and the role check. Change it per site with NEXT_PUBLIC_ADMIN_PATH
 * (one word or a few, letters, digits and dashes; set a long random one in
 * production).
 *
 * The pages themselves sit in the folder app/admin; the proxy answers the
 * secret address with them and shows "not found" at /admin.
 */
export const ADMIN_SEGMENT = (process.env.NEXT_PUBLIC_ADMIN_PATH ?? "RentoXAdminPanel").replace(/^\/+|\/+$/g, "");

/** The public address of an admin page: adminHref("/elanlar") → "/RentoXAdminPanel/elanlar". */
export function adminHref(sub = ""): string {
  return `/${ADMIN_SEGMENT}${sub === "/" ? "" : sub}`;
}
