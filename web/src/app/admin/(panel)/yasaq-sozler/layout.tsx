import { requireSection } from "@/lib/admin";

/** Only an Admin who has the listings section gets in. */
export default async function SectionLayout({ children }: { children: React.ReactNode }) {
  await requireSection("listings");
  return children;
}
