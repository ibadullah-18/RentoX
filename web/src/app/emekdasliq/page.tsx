import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("cooperation");

export default function Page() {
  return <StaticPage slug="cooperation" />;
}
