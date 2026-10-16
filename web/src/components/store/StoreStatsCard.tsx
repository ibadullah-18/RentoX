import { Eye, Heart, MessageCircle, Phone, Users } from "lucide-react";
import { WhatsAppIcon } from "@/components/SocialIcons";
import { formatNumber } from "@/lib/format";
import { getLocale, getT } from "@/lib/i18n/server";
import type { StoreStats } from "@/lib/types";

/** What the owner wants to know: who follows, how often the store and its listings are looked at, how often it is contacted. */
export async function StoreStatsCard({ stats }: { stats: StoreStats }) {
  const t = await getT();
  const locale = await getLocale();
  const n = (v: number) => formatNumber(v, locale);
  const peak = Math.max(1, ...stats.last30Days.map((d) => d.views));

  const figures = [
    { label: t("İzləyici"), value: n(stats.followers), Icon: Users },
    { label: t("Mağaza baxışı (7 gün)"), value: n(stats.profileViewsLast7Days), Icon: Eye },
    { label: t("Mağaza baxışı (30 gün)"), value: n(stats.profileViewsLast30Days), Icon: Eye },
    { label: t("Elanların baxışı"), value: n(stats.listingViews), Icon: Eye },
    { label: t("Elanlar seçilmişlərdə"), value: n(stats.listingFavorites), Icon: Heart },
    { label: t("Zəng düyməsi"), value: n(stats.calls), Icon: Phone },
    { label: "WhatsApp", value: n(stats.whatsApp), Icon: WhatsAppIcon },
    { label: t("Mağazaya yazılan"), value: n(stats.messages), Icon: MessageCircle },
  ];

  return (
    <section className="space-y-4 rounded-2xl border border-line bg-surface p-5">
      <div>
        <h2 className="text-lg font-extrabold">{t("Statistika")}</h2>
        <p className="text-sm text-ink-2">
          {t("Mağaza səhifəsinin neçə dəfə açıldığı və əlaqə düymələrinin istifadəsi. Sizin öz baxışlarınız sayılmır.")}
        </p>
      </div>

      <ul className="grid grid-cols-2 gap-3 md:grid-cols-4">
        {figures.map(({ label, value, Icon }) => (
          <li key={label} className="rounded-xl border border-line bg-bg px-3.5 py-3">
            <Icon className="mb-1 size-4 text-primary" aria-hidden />
            <p className="text-xl font-extrabold">{value}</p>
            <p className="text-xs font-semibold text-ink-2">{label}</p>
          </li>
        ))}
      </ul>

      <div>
        <p className="mb-2 text-sm font-bold">
          {t("Son 30 gün: mağaza baxışı")} <span className="font-normal text-ink-2">({t("əlaqə: {n}", { n: n(stats.contactsLast30Days) })})</span>
        </p>
        <ol className="flex h-24 items-end gap-[3px]" aria-hidden>
          {stats.last30Days.map((d) => (
            <li
              key={d.day}
              title={`${d.day.split("-").reverse().join(".")} · ${d.views}`}
              className="flex-1 rounded-t bg-primary/80"
              style={{ height: `${Math.max(d.views > 0 ? 6 : 2, Math.round((d.views / peak) * 100))}%`, opacity: d.views > 0 ? 1 : 0.25 }}
            />
          ))}
        </ol>
      </div>
    </section>
  );
}
