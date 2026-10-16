"use client";

import { Globe, Phone } from "lucide-react";
import { useT } from "@/lib/i18n/client";
import { whatsappLink } from "@/lib/store-contact";
import { FacebookIcon, InstagramIcon, TikTokIcon, WhatsAppIcon, YouTubeIcon } from "../SocialIcons";
import { reportStoreEvent } from "./StoreTrack";

type Links = {
  slug: string;
  phone?: string | null;
  whatsAppNumber?: string | null;
  whatsAppMessage?: string | null;
  instagramUrl?: string | null;
  tiktokUrl?: string | null;
  facebookUrl?: string | null;
  youtubeUrl?: string | null;
  websiteUrl?: string | null;
};

const round =
  "flex size-11 items-center justify-center rounded-full border border-line bg-surface transition hover:-translate-y-0.5 hover:shadow-md";

/**
 * The call button and the round logos of the networks the store has. A network without a link is not shown at all.
 * WhatsApp opens with the first message the owner chose.
 */
export function StoreContact({ slug, phone, whatsAppNumber, whatsAppMessage, instagramUrl, tiktokUrl, facebookUrl, youtubeUrl, websiteUrl }: Links) {
  const t = useT();
  const external = { target: "_blank", rel: "noopener noreferrer nofollow" } as const;

  return (
    <div className="flex flex-wrap items-center gap-2.5">
      {phone && (
        <a
          href={`tel:${phone.replace(/\s/g, "")}`}
          onClick={() => reportStoreEvent(slug, "call")}
          className="flex h-11 items-center gap-2 rounded-xl border border-line bg-surface px-4 font-bold hover:border-primary hover:text-primary"
        >
          <Phone className="size-5" aria-hidden />
          {t("Zəng et")}
        </a>
      )}

      {whatsAppNumber && (
        <a
          href={whatsappLink(whatsAppNumber, whatsAppMessage)}
          onClick={() => reportStoreEvent(slug, "whatsapp")}
          aria-label="WhatsApp"
          title="WhatsApp"
          {...external}
          className={`${round} text-[#25D366] hover:border-[#25D366]`}
        >
          <WhatsAppIcon className="size-6" />
        </a>
      )}
      {instagramUrl && (
        <a href={instagramUrl} aria-label="Instagram" title="Instagram" {...external} className={`${round} text-[#E4405F] hover:border-[#E4405F]`}>
          <InstagramIcon className="size-6" />
        </a>
      )}
      {tiktokUrl && (
        <a href={tiktokUrl} aria-label="TikTok" title="TikTok" {...external} className={`${round} hover:border-ink`}>
          <TikTokIcon className="size-6" />
        </a>
      )}
      {youtubeUrl && (
        <a href={youtubeUrl} aria-label="YouTube" title="YouTube" {...external} className={`${round} text-[#FF0000] hover:border-[#FF0000]`}>
          <YouTubeIcon className="size-6" />
        </a>
      )}
      {facebookUrl && (
        <a href={facebookUrl} aria-label="Facebook" title="Facebook" {...external} className={`${round} text-[#1877F2] hover:border-[#1877F2]`}>
          <FacebookIcon className="size-6" />
        </a>
      )}
      {websiteUrl && (
        <a href={websiteUrl} aria-label={t("Sayt")} title={t("Sayt")} {...external} className={`${round} text-primary hover:border-primary`}>
          <Globe className="size-6" aria-hidden />
        </a>
      )}
    </div>
  );
}
