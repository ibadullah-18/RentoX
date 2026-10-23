import type { Metadata } from "next";
import { DEFAULT_ICON, getBranding } from "@/lib/api";

export async function generateMetadata(): Promise<Metadata> {
  const branding = await getBranding();
  const icon = branding.iconUrl ?? DEFAULT_ICON;
  return {
    title: { default: `${branding.siteName} Admin`, template: `%s · ${branding.siteName} Admin` },
    robots: { index: false, follow: false },
    icons: { icon: [{ url: icon }], shortcut: [{ url: icon }], apple: [{ url: icon }] },
  };
}

export default function AdminRoot({ children }: LayoutProps<"/admin">) {
  return children;
}
