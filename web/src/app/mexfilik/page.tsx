import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("privacy");

export default function Page() {
  return <StaticPage slug="privacy" />;
}
