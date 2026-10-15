import { StaticPage, pageMetadata } from "@/components/StaticPage";

export const generateMetadata = () => pageMetadata("terms");

export default function Page() {
  return <StaticPage slug="terms" />;
}
