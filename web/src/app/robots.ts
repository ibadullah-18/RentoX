import type { MetadataRoute } from "next";
import { siteUrl } from "@/lib/format";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
        // Personal and technical areas: nothing for a search engine there.
        disallow: [
          "/admin",
          "/kabinet",
          "/mesajlar",
          "/elanlarim",
          "/pul-kisesi",
          "/secilmisler",
          "/destek",
          "/bildirisler",
          "/magaza",
          "/yeni-elan",
          "/giris",
          "/bff/",
          "/api/",
          "/hubs/",
        ],
      },
    ],
    sitemap: `${siteUrl()}/sitemap.xml`,
  };
}
