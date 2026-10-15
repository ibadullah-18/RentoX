import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("rules");

export default function Page() {
  return <StaticPage slug="rules" />;
}
