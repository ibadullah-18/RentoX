import { requireSection } from "@/lib/admin";

/** Only an Admin who has this section of the panel gets in. */
export default async function SectionLayout({ children }: { children: React.ReactNode }) {
  await requireSection("users");
  return children;
}
