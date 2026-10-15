import { redirect } from "next/navigation";
import { adminHref } from "@/lib/admin-path";

/** The chat reports are a tab of the reports page now. */
export default async function ChatReportsPage({ searchParams }: PageProps<"/admin/yazisma-sikayetleri">) {
  const sp = await searchParams;
  const status = Array.isArray(sp.status) ? sp.status[0] : sp.status;
  redirect(adminHref(`/sikayetler?tur=yazisma${status && status !== "1" ? `&status=${status}` : ""}`));
}
