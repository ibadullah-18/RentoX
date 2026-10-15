import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("faq");

export default function Page() {
  return <StaticPage slug="faq" />;
}
