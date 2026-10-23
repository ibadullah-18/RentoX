"use client";

import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { ImagePlus, Loader2, RotateCcw, Save } from "lucide-react";
import { brandImageChanged, resetBrandImage, saveBranding } from "@/app/admin/(panel)/brand-actions";
import { adminHref } from "@/lib/admin-path";
import type { Branding } from "@/lib/types";
import { BrandMark } from "@/components/BrandMark";

const DEFAULT_LOGO = "/brand/logo.png";
const DEFAULT_LOGO_DARK = "/brand/logo-dark.png";
const DEFAULT_ICON = "/brand/icon.png";
const TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_BYTES = 5 * 1024 * 1024;

type Slot = {
  slug: string;
  title: string;
  hint: string;
  dark: boolean;
  square?: boolean;
  /** The address of the owner's own picture, or empty. */
  custom: string | null | undefined;
  /** What is shown while there is no own picture. */
  fallback: string;
  fallbackNote?: string;
};

const field =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-sm outline-none focus:border-primary disabled:opacity-60";

/**
 * The look of the brand. The name, footer text and sizes are saved with the button; every picture is uploaded on its
 * own and works at once. Whatever is not set keeps the picture that comes with the site, and "Standarta qaytar"
 * brings that back.
 */
export function BrandingForm({ branding }: { branding: Branding }) {
  const router = useRouter();
  const picker = useRef<HTMLInputElement>(null);
  const pickFor = useRef<string | null>(null);

  const [name, setName] = useState(branding.siteName);
  const [footer, setFooter] = useState(branding.footerText ?? "");
  const [text, setText] = useState(branding.useTextLogo);
  const [headerHeight, setHeaderHeight] = useState(branding.headerLogoHeight);
  const [footerHeight, setFooterHeight] = useState(branding.footerLogoHeight);
  const [motion, setMotion] = useState(branding.motionLevel);
  const [theme, setTheme] = useState(branding.defaultTheme);
  const [about, setAbout] = useState(branding.description ?? "");
  const [noticeOn, setNoticeOn] = useState(branding.announcementEnabled);
  const [noticeText, setNoticeText] = useState(branding.announcementText ?? "");
  const [noticeLink, setNoticeLink] = useState(branding.announcementLink ?? "");
  const [noticeTone, setNoticeTone] = useState(branding.announcementTone);
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const headerLight = branding.headerLogoUrl ?? DEFAULT_LOGO;
  const headerDark = branding.headerLogoDarkUrl ?? branding.headerLogoUrl ?? DEFAULT_LOGO_DARK;

  const slots: Slot[] = [
    {
      slug: "header-logo",
      title: "Başlıq loqosu (açıq fon)",
      hint: "Saytın yuxarısında və mobil tətbiqdə. Şəffaf fonlu PNG, eni ən azı 600 piksel.",
      dark: false,
      custom: branding.headerLogoUrl,
      fallback: DEFAULT_LOGO,
    },
    {
      slug: "header-logo-dark",
      title: "Başlıq loqosu (tünd fon)",
      hint: "Gecə rejimi üçün açıq rəngli versiya. Yüklənməsə, açıq fon üçün olan loqo göstərilir.",
      dark: true,
      custom: branding.headerLogoDarkUrl,
      fallback: branding.headerLogoUrl ?? DEFAULT_LOGO_DARK,
      fallbackNote: branding.headerLogoUrl ? "Açıq fon loqosu istifadə olunur" : undefined,
    },
    {
      slug: "footer-logo",
      title: "Alt hissə loqosu (açıq fon)",
      hint: "Saytın aşağısında. Yüklənməsə, başlıq loqosu istifadə olunur.",
      dark: false,
      custom: branding.footerLogoUrl,
      fallback: headerLight,
      fallbackNote: "Başlıq loqosu istifadə olunur",
    },
    {
      slug: "footer-logo-dark",
      title: "Alt hissə loqosu (tünd fon)",
      hint: "Gecə rejimində aşağıda. Yüklənməsə, başlıq loqosunun tünd versiyası istifadə olunur.",
      dark: true,
      custom: branding.footerLogoDarkUrl,
      fallback: headerDark,
      fallbackNote: "Başlıq loqosu istifadə olunur",
    },
    {
      slug: "icon",
      title: "İkon",
      hint: "Brauzer sekməsində (favicon) və tətbiqin içində. Kvadrat PNG, 512×512.",
      dark: false,
      square: true,
      custom: branding.iconUrl,
      fallback: DEFAULT_ICON,
    },
    {
      slug: "share-image",
      title: "Paylaşım şəkli",
      hint: "Keçid WhatsApp, Telegram, Facebook-da paylaşılanda görünən şəkil. Üfüqi, 1200×630 ən yaxşısıdır.",
      dark: false,
      custom: branding.shareImageUrl,
      fallback: DEFAULT_ICON,
      fallbackNote: "Şəkil göstərilmir",
    },
  ];

  async function save() {
    setBusy("save");
    setMessage(null);
    const res = await saveBranding({
      siteName: name,
      footerText: footer,
      useTextLogo: text,
      headerLogoHeight: headerHeight,
      footerLogoHeight: footerHeight,
      motionLevel: motion,
      defaultTheme: theme,
      description: about,
      announcementEnabled: noticeOn,
      announcementText: noticeText,
      announcementLink: noticeLink,
      announcementTone: noticeTone,
    });
    setBusy(null);
    if (res.ok) {
      setMessage({ ok: true, text: "Yadda saxlanıldı. Saytda və tətbiqdə bir dəqiqə ərzində görünür." });
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ ok: false, text: res.message });
  }

  async function upload(slug: string, file: File) {
    if (!TYPES.includes(file.type)) return setMessage({ ok: false, text: "Şəkil JPG, PNG və ya WebP olmalıdır." });
    if (file.size > MAX_BYTES) return setMessage({ ok: false, text: "Şəkil 5 MB-dan böyük ola bilməz." });
    setBusy(slug);
    setMessage(null);
    const form = new FormData();
    form.set("file", file, file.name);
    try {
      const res = await fetch(`/bff/admin-branding-image/${slug}`, { method: "PUT", body: form });
      if (res.ok) {
        await brandImageChanged();
        setMessage({ ok: true, text: "Şəkil dəyişdirildi." });
        router.refresh();
      } else {
        setMessage({
          ok: false,
          text: res.status === 400 ? "Şəkil qəbul edilmədi: faylı yoxlayın (JPG, PNG, WebP, ən çoxu 5 MB)." : "Şəkil yüklənmədi. Yenidən cəhd edin.",
        });
      }
    } catch {
      setMessage({ ok: false, text: "Serverə qoşulmaq olmadı." });
    } finally {
      setBusy(null);
    }
  }

  async function reset(slug: string) {
    setBusy(slug);
    setMessage(null);
    const res = await resetBrandImage(slug);
    setBusy(null);
    if (res.ok) {
      setMessage({ ok: true, text: "Standart şəkil qaytarıldı." });
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ ok: false, text: res.message });
  }

  return (
    <div className="space-y-5">
      <input
        ref={picker}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0];
          const slug = pickFor.current;
          e.target.value = "";
          if (file && slug) void upload(slug, file);
        }}
      />

      {message && (
        <p role="status" className={`rounded-xl px-4 py-3 text-sm font-semibold ${message.ok ? "bg-success/10 text-success" : "bg-danger/10 text-danger"}`}>
          {message.text}
        </p>
      )}

      <section className="rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 font-extrabold">Ad və yazı</h2>
        <div className="grid gap-4 p-4 md:grid-cols-2">
          <label className="block text-sm font-semibold">
            Saytın adı
            <input value={name} onChange={(e) => setName(e.target.value)} maxLength={40} disabled={busy !== null} className={`${field} mt-1`} />
            <span className="mt-1 block text-xs font-normal text-ink-2">Brauzer başlığında, paylaşılan keçiddə və alt hissədə yazılır.</span>
          </label>
          <label className="block text-sm font-semibold">
            Alt hissədə qısa yazı (ixtiyari)
            <input
              value={footer}
              onChange={(e) => setFooter(e.target.value)}
              maxLength={200}
              placeholder="Boş: «Kirayə elanları — hər şey bir yerdə»"
              disabled={busy !== null}
              className={`${field} mt-1`}
            />
            <span className="mt-1 block text-xs font-normal text-ink-2">Bütün dillərdə eyni yazılır. Boş qoysan, hər dilin öz standart yazısı göstərilir.</span>
          </label>

          <label className="flex items-center gap-2 text-sm font-semibold md:col-span-2">
            <input type="checkbox" checked={text} onChange={(e) => setText(e.target.checked)} disabled={busy !== null} className="size-4 accent-[var(--color-primary)]" />
            Loqonun yerinə sadə yazı göstər (köhnə görünüş)
          </label>

          <div className="grid gap-4 sm:grid-cols-2 md:col-span-2">
            <label className="block text-sm font-semibold">
              Başlıqdakı loqonun hündürlüyü: <b>{headerHeight}</b> px
              <input type="range" min={20} max={96} value={headerHeight} onChange={(e) => setHeaderHeight(Number(e.target.value))} disabled={busy !== null} className="mt-2 w-full accent-[var(--color-primary)]" />
              <span className="block text-xs font-normal text-ink-2">Telefonda ən çoxu 34 px göstərilir ki, başlıq nazik qalsın.</span>
            </label>
            <label className="block text-sm font-semibold">
              Alt hissədəki loqonun hündürlüyü: <b>{footerHeight}</b> px
              <input type="range" min={20} max={96} value={footerHeight} onChange={(e) => setFooterHeight(Number(e.target.value))} disabled={busy !== null} className="mt-2 w-full accent-[var(--color-primary)]" />
            </label>
          </div>
        </div>

        <div className="grid gap-3 border-t border-line p-4 md:grid-cols-2">
          <div>
            <p className="mb-1.5 text-xs font-bold text-ink-2">Başlıq (açıq görünüş)</p>
            <div className="flex items-center rounded-lg border border-line bg-[#f6f5ff] px-4 py-2.5">
              <BrandMark light={headerLight} dark={headerLight} name={name} height={headerHeight} text={text} />
            </div>
          </div>
          <div>
            <p className="mb-1.5 text-xs font-bold text-ink-2">Başlıq (gecə görünüşü)</p>
            <div className="flex items-center rounded-lg border border-line bg-[#0f0d24] px-4 py-2.5">
              <BrandMark light={headerDark} dark={headerDark} name={name} height={headerHeight} text={text} />
            </div>
          </div>
        </div>

      </section>

      <section className="rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 font-extrabold">Sayt ayarları</h2>
        <div className="grid gap-4 p-4 md:grid-cols-2">
          <label className="block text-sm font-semibold">
            Yeni gələn ziyarətçi üçün görünüş
            <select value={theme} onChange={(e) => setTheme(Number(e.target.value))} disabled={busy !== null} className={`${field} mt-1`}>
              <option value={0}>Gündüz</option>
              <option value={1}>Gecə</option>
              <option value={2}>Avtomatik (cihaz kimi)</option>
            </select>
            <span className="mt-1 block text-xs font-normal text-ink-2">Ziyarətçi özü seçənə qədər bu görünüş açılır.</span>
          </label>
          <label className="block text-sm font-semibold">
            Hərəkət səviyyəsi
            <select value={motion} onChange={(e) => setMotion(Number(e.target.value))} disabled={busy !== null} className={`${field} mt-1`}>
              <option value={0}>Tam (animasiya və 3D təsirlər)</option>
              <option value={1}>Yumşaq (yalnız solma, dərinliksiz)</option>
              <option value={2}>Söndürülü (animasiyasız)</option>
            </select>
            <span className="mt-1 block text-xs font-normal text-ink-2">Zəif telefonlar və hərəkətdən narahat olanlar üçün cihaz ayarı həmişə nəzərə alınır.</span>
          </label>
          <label className="block text-sm font-semibold md:col-span-2">
            Saytın qısa təsviri (axtarış sistemləri və paylaşılan keçid üçün)
            <textarea
              value={about}
              onChange={(e) => setAbout(e.target.value)}
              maxLength={300}
              rows={2}
              placeholder="Boş: hər dilin öz standart təsviri"
              disabled={busy !== null}
              className="mt-1 w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm outline-none focus:border-primary disabled:opacity-60"
            />
            <span className="mt-1 block text-right text-xs font-normal text-ink-2">{about.length}/300</span>
          </label>
        </div>
      </section>

      <section className="rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 font-extrabold">Elan xəbərdarlığı (saytın ən yuxarısında)</h2>
        <div className="grid gap-4 p-4 md:grid-cols-2">
          <label className="flex items-center gap-2 text-sm font-semibold md:col-span-2">
            <input type="checkbox" checked={noticeOn} onChange={(e) => setNoticeOn(e.target.checked)} disabled={busy !== null} className="size-4 accent-[var(--color-primary)]" />
            Xəbərdarlığı göstər
          </label>
          <label className="block text-sm font-semibold md:col-span-2">
            Mətn
            <input value={noticeText} onChange={(e) => setNoticeText(e.target.value)} maxLength={200} placeholder="Məsələn: Bu gecə 02:00-da qısa texniki iş olacaq" disabled={busy !== null} className={`${field} mt-1`} />
          </label>
          <label className="block text-sm font-semibold">
            Keçid (ixtiyari)
            <input value={noticeLink} onChange={(e) => setNoticeLink(e.target.value)} maxLength={300} placeholder="/destek və ya https://..." disabled={busy !== null} className={`${field} mt-1`} />
          </label>
          <label className="block text-sm font-semibold">
            Rəng
            <select value={noticeTone} onChange={(e) => setNoticeTone(Number(e.target.value))} disabled={busy !== null} className={`${field} mt-1`}>
              <option value={0}>Məlumat (göy)</option>
              <option value={1}>Xəbərdarlıq (sarı)</option>
              <option value={2}>Yaxşı xəbər (yaşıl)</option>
            </select>
          </label>
          <p className="text-xs text-ink-2 md:col-span-2">Ziyarətçi bağlaya bilər; mətn dəyişsə, yenidən görünür. Yadda saxla düyməsi səhifənin aşağısındadır.</p>
        </div>
      </section>

      <section className="rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 font-extrabold">Loqolar, ikon və paylaşım şəkli</h2>
        <ul className="divide-y divide-line">
          {slots.map((s) => {
            const shown = s.custom ?? s.fallback;
            return (
              <li key={s.slug} className="grid gap-3 p-4 md:grid-cols-[minmax(0,260px)_1fr_auto] md:items-center">
                <div
                  className={`flex h-24 items-center justify-center overflow-hidden rounded-lg border border-line px-3 ${s.dark ? "bg-[#0f0d24]" : "bg-[#f6f5ff]"}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={shown} alt="" className={s.square ? "size-16 rounded-xl" : "max-h-14 w-auto max-w-full"} />
                </div>
                <div className="min-w-0">
                  <p className="flex flex-wrap items-center gap-2 font-bold">
                    {s.title}
                    <span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${s.custom ? "bg-success/10 text-success" : "bg-line text-ink-2"}`}>
                      {s.custom ? "Xüsusi" : (s.fallbackNote ?? "Standart")}
                    </span>
                  </p>
                  <p className="mt-0.5 text-xs text-ink-2">{s.hint}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    disabled={busy !== null}
                    onClick={() => {
                      pickFor.current = s.slug;
                      picker.current?.click();
                    }}
                    className="flex h-9 items-center gap-1.5 rounded-lg border border-line bg-surface px-3 text-sm font-bold hover:border-primary disabled:opacity-50"
                  >
                    {busy === s.slug ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <ImagePlus className="size-4" aria-hidden />}
                    Yüklə
                  </button>
                  {s.custom && (
                    <button
                      type="button"
                      disabled={busy !== null}
                      onClick={() => void reset(s.slug)}
                      className="flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-bold text-danger hover:bg-danger/10 disabled:opacity-50"
                    >
                      <RotateCcw className="size-4" aria-hidden />
                      Standarta qaytar
                    </button>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      </section>
      <div className="sticky bottom-3 z-10 flex flex-wrap items-center gap-3 rounded-xl border border-line bg-surface/95 p-3 shadow-lg backdrop-blur">
        <button
          type="button"
          onClick={() => void save()}
          disabled={busy !== null}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60"
        >
          {busy === "save" ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Save className="size-4" aria-hidden />}
          Yadda saxla
        </button>
        <span className="text-xs text-ink-2">Ad, mətnlər və ayarlar buradan saxlanır. Şəkillər isə hər birinin öz düyməsi ilə dərhal dəyişir.</span>
      </div>
    </div>
  );
}
