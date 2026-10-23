import Link from "next/link";
import { brandLogos, getBranding } from "@/lib/api";
import { BrandMark } from "./BrandMark";

/** The logo of the site (a link to the home page), as the owner set it in the admin panel. */
export async function Logo({ variant = "header", className = "" }: { variant?: "header" | "footer"; className?: string }) {
  const branding = await getBranding();
  const logos = brandLogos(branding, variant);
  return (
    <Link href="/" aria-label={branding.siteName} className={`inline-flex items-center ${className}`}>
      <BrandMark
        light={logos.light}
        dark={logos.dark}
        name={branding.siteName}
        height={logos.height}
        text={branding.useTextLogo}
      />
    </Link>
  );
}
