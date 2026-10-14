import { requireSection } from "@/lib/admin";

/** Only an Admin who has the finance section gets in. */
export default async function SectionLayout({ children }: { children: React.ReactNode }) {
  await requireSection("finance");
  return children;
}
