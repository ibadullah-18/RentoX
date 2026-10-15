import { getT } from "@/lib/i18n/server";
import Link from "next/link";
import type { ComponentType } from "react";
import { Headset, Mail, Phone } from "lucide-react";
import { Logo } from "./Logo";
import { FacebookIcon, InstagramIcon, TikTokIcon, WhatsAppIcon } from "./SocialIcons";
import { getPageList, getSiteInfo } from "@/lib/api";

/** The page behind each fixed page of the site. */
const PAGE_PATH: Record<string, string> = {
  about: "/haqqimizda",
  terms: "/sertler",
  privacy: "/mexfilik",
  cooperation: "/emekdasliq",
  rules: "/qaydalar",
  faq: "/suallar",
};

/** The numbers and addresses a link can be made from. */
const digits = (value: string) => value.replace(/[^\d+]/g, "");

export async function Footer() {
  const t = await getT();
  const [pages, info] = await Promise.all([getPageList(), getSiteInfo()]);

  // A channel that is not filled in is simply not here.
  type Contact = { key: string; icon: ComponentType<{ className?: string }>; label: string; href: string; title: string };
  const contacts: Contact[] = [];
  if (info.phone) contacts.push({ key: "phone", icon: Phone, label: info.phone, href: `tel:${digits(info.phone)}`, title: t("Telefon") });
  if (info.hotline) contacts.push({ key: "hotline", icon: Headset, label: info.hotline, href: `tel:${digits(info.hotline)}`, title: t("Qaynar xətt") });
  if (info.email) contacts.push({ key: "email", icon: Mail, label: info.email, href: `mailto:${info.email}`, title: t("E-poçt") });

  type Social = { key: string; icon: ComponentType<{ className?: string }>; label: string; href: string };
  const social: Social[] = [];
  if (info.whatsApp) social.push({ key: "whatsapp", icon: WhatsAppIcon, label: "WhatsApp", href: `https://wa.me/${info.whatsApp.replace(/\D/g, "")}` });
  if (info.instagram) social.push({ key: "instagram", icon: InstagramIcon, label: "Instagram", href: info.instagram });
  if (info.tikTok) social.push({ key: "tiktok", icon: TikTokIcon, label: "TikTok", href: info.tikTok });
  if (info.facebook) social.push({ key: "facebook", icon: FacebookIcon, label: "Facebook", href: info.facebook });

  const links = pages.filter((p) => PAGE_PATH[p.slug]);
  const hasContact = contacts.length > 0 || social.length > 0;

  return (
    <footer className="mt-16 border-t border-line">
      <div className="mx-auto grid max-w-[1240px] gap-8 px-4 py-8 text-sm text-ink-2 md:grid-cols-[1.3fr_1fr_1fr] md:px-6">
        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <Logo />
          </div>
          <p>{t("Kirayə elanları — hər şey bir yerdə")}</p>
          {(info.operatorName || info.voen) && (
            <p className="text-xs">
              {info.operatorName}
              {info.operatorName && info.voen ? " · " : ""}
              {info.voen && `VÖEN ${info.voen}`}
            </p>
          )}
        </div>

        {links.length > 0 && (
          <nav aria-label={t("Alt keçidlər")}>
            <h2 className="mb-3 text-sm font-extrabold text-ink">{t("Faydalı məlumat")}</h2>
            <ul className="space-y-2">
              {links.map((p) => (
                <li key={p.slug}>
                  <Link href={PAGE_PATH[p.slug]} className="font-semibold hover:text-primary">
                    {p.title}
                  </Link>
                </li>
              ))}
              <li>
                <Link href="/magazalar" className="font-semibold hover:text-primary">
                  {t("Mağazalar")}
                </Link>
              </li>
            </ul>
          </nav>
        )}

        {hasContact && (
          <div>
            <h2 className="mb-3 text-sm font-extrabold text-ink">{t("Əlaqə")}</h2>
            {contacts.length > 0 && (
              <ul className="space-y-2">
                {contacts.map(({ key, icon: Icon, label, href, title }) => (
                  <li key={key}>
                    <a href={href} className="flex items-center gap-2 font-semibold hover:text-primary" title={title}>
                      <Icon className="size-4 shrink-0" aria-hidden />
                      <span className="sr-only">{title}: </span>
                      {label}
                    </a>
                  </li>
                ))}
              </ul>
            )}
            {social.length > 0 && (
              <ul className={`flex flex-wrap gap-2 ${contacts.length > 0 ? "mt-4" : ""}`}>
                {social.map(({ key, icon: Icon, label, href }) => (
                  <li key={key}>
                    <a
                      href={href}
                      target="_blank"
                      rel="noopener noreferrer"
                      aria-label={label}
                      title={label}
                      className="flex size-10 items-center justify-center rounded-lg border border-line text-ink-2 hover:border-primary hover:text-primary"
                    >
                      <Icon className="size-5" />
                    </a>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </div>

      <div className="border-t border-line">
        <div className="mx-auto max-w-[1240px] px-4 py-6 text-center md:px-6">
          <p className="text-xs text-ink-2">
            © {new Date().getFullYear()} RentoX. {t("Bütün hüquqlar qorunur.")}
          </p>
          <a
            href="https://www.linkedin.com/in/ibadulla-huseynzade/"
            target="_blank"
            rel="noopener noreferrer"
            aria-label="Site by Ibadulla Huseynzade, LinkedIn"
            className="credit-link mt-2 inline-block text-[9px] font-extrabold tracking-[0.26em]"
          >
            SITE BY IBADULLA HUSEYNZADE
          </a>
        </div>
      </div>
    </footer>
  );
}
