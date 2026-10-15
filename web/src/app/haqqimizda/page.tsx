import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("about");

export default function Page() {
  return <StaticPage slug="about" />;
}
