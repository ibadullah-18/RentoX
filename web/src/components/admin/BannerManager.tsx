"use client";

import { fmtNumeric } from "@/lib/dates";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { ExternalLink, ImagePlus, Loader2, Pencil, Plus, SlidersHorizontal, Trash2 } from "lucide-react";
import {
  bannerImageChanged,
  createBanner,
  deleteBanner,
  updateBanner,
  type BannerFormInput,
  type BannerSaved,
} from "@/app/admin/(panel)/banner-actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { fetchPicture, useImageEditor, type EditorOptions } from "@/components/ImageEditor";
import { Pill } from "@/components/admin/ui";

export type BannerRow = {
  id: string;
  titleAz: string;
  titleRu: string;
  titleEn: string;
  subtitleAz?: string | null;
  subtitleRu?: string | null;
  subtitleEn?: string | null;
  linkUrl?: string | null;
  displayOrder: number;
  isActive: boolean;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  hasImage: boolean;
  isLive: boolean;
  pageSlug?: string | null;
  pageBodyAz?: string | null;
  pageBodyRu?: string | null;
  pageBodyEn?: string | null;
  pageButtonLabelAz?: string | null;
  pageButtonLabelRu?: string | null;
  pageButtonLabelEn?: string | null;
  pageButtonUrl?: string | null;
  openInNewTab?: boolean;
  platform?: number;
  showText?: boolean;
  textAlign?: number;
  darkOverlay?: boolean;
  textTone?: number;
};

/** What the owner can pick to send a visitor to when the banner is pressed. */
export type StoreChoice = { slug: string; name: string };
export type CategoryChoice = { id: string; name: string };

type LinkKind = "none" | "page" | "store" | "listing" | "category" | "site" | "external";

const KINDS: { id: LinkKind; label: string; hint: string }[] = [
  { id: "none", label: "Heç yerə (kliklənmir)", hint: "Banner yalnız şəkil kimi görünür, üstünə basanda heç nə olmur." },
  { id: "page", label: "Kampaniya səhifəsi", hint: "Öz ünvanı olan səhifə yaradılır: rentox.az/ad. Mətn, şəkil və düymə orada olur." },
  { id: "store", label: "Mağaza", hint: "Mağazanın səhifəsi açılır (mağaza reklamı üçün)." },
  { id: "listing", label: "Konkret elan", hint: "Bir elanın səhifəsi açılır." },
  { id: "category", label: "Kateqoriya", hint: "Həmin kateqoriyanın elanları açılır." },
  { id: "site", label: "Saytın başqa səhifəsi", hint: "Məsələn /destek. Səhifə mövcud olmalıdır, yoxsa «tapılmadı» çıxar." },
  { id: "external", label: "Xarici sayt (https://…)", hint: "Başqa saytın ünvanı yeni pəncərədə açılır." },
];

const SITE_PLACES = ["/destek", "/magazalar", "/search", "/yeni-elan", "/secilmisler", "/pul-kisesi", "/qaydalar", "/haqqimizda"];

const field =
  "h-9 w-full rounded-lg border border-line bg-surface px-2.5 text-sm outline-none focus:border-primary disabled:opacity-60";
const area =
  "w-full rounded-lg border border-line bg-surface px-2.5 py-2 text-sm outline-none focus:border-primary disabled:opacity-60";
const TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_BYTES = 5 * 1024 * 1024;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const SLUG = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

/** An ISO time as the value of a datetime-local box (in the admin's own time zone). */
function toLocal(iso?: string | null) {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

const toIso = (local: string) => (local ? new Date(local).toISOString() : "");

type Draft = {
  titleAz: string;
  titleRu: string;
  titleEn: string;
  subtitleAz: string;
  subtitleRu: string;
  subtitleEn: string;
  kind: LinkKind;
  slug: string;
  bodyAz: string;
  bodyRu: string;
  bodyEn: string;
  buttonAz: string;
  buttonRu: string;
  buttonEn: string;
  buttonUrl: string;
  store: string;
  listingId: string;
  category: string;
  site: string;
  external: string;
  openInNewTab: boolean;
  platform: string;
  showText: boolean;
  textAlign: number;
  darkOverlay: boolean;
  textTone: number;
  displayOrder: string;
  isActive: boolean;
  starts: string;
  ends: string;
};

/** Works out which kind of target a saved banner has, from what was saved. */
function kindOf(b?: BannerRow): { kind: LinkKind; store: string; listingId: string; category: string; site: string; external: string } {
  const none = { kind: "none" as LinkKind, store: "", listingId: "", category: "", site: "", external: "" };
  if (!b) return none;
  if (b.pageSlug) return { ...none, kind: "page" };
  const url = b.linkUrl ?? "";
  if (!url) return none;
  if (url.startsWith("/store/")) return { ...none, kind: "store", store: url.slice("/store/".length) };
  if (url.startsWith("/listing/")) return { ...none, kind: "listing", listingId: url.slice("/listing/".length) };
  if (url.startsWith("/search?category=")) return { ...none, kind: "category", category: url.slice("/search?category=".length) };
  if (url.startsWith("/")) return { ...none, kind: "site", site: url };
  return { ...none, kind: "external", external: url };
}

const draftOf = (b?: BannerRow, order = 1): Draft => ({
  titleAz: b?.titleAz ?? "",
  titleRu: b?.titleRu ?? "",
  titleEn: b?.titleEn ?? "",
  subtitleAz: b?.subtitleAz ?? "",
  subtitleRu: b?.subtitleRu ?? "",
  subtitleEn: b?.subtitleEn ?? "",
  ...kindOf(b),
  slug: b?.pageSlug ?? "",
  bodyAz: b?.pageBodyAz ?? "",
  bodyRu: b?.pageBodyRu ?? "",
  bodyEn: b?.pageBodyEn ?? "",
  buttonAz: b?.pageButtonLabelAz ?? "",
  buttonRu: b?.pageButtonLabelRu ?? "",
  buttonEn: b?.pageButtonLabelEn ?? "",
  buttonUrl: b?.pageButtonUrl ?? "",
  openInNewTab: b?.openInNewTab ?? false,
  platform: String(b?.platform ?? 3),
  showText: b?.showText ?? true,
  textAlign: b?.textAlign ?? 0,
  darkOverlay: b?.darkOverlay ?? true,
  textTone: b?.textTone ?? 0,
  displayOrder: String(b?.displayOrder ?? order),
  isActive: b?.isActive ?? true,
  starts: toLocal(b?.startsAtUtc),
  ends: toLocal(b?.endsAtUtc),
});

/** The address a banner leads to, as the site will store it; empty when it leads nowhere. */
function linkOf(v: Draft): string {
  switch (v.kind) {
    case "page":
      return v.slug.trim() ? `/${v.slug.trim().replace(/^\/+/, "").toLowerCase()}` : "";
    case "store":
      return v.store ? `/store/${v.store}` : "";
    case "listing":
      return v.listingId.trim() ? `/listing/${v.listingId.trim()}` : "";
    case "category":
      return v.category ? `/search?category=${v.category}` : "";
    case "site":
      return v.site.trim();
    case "external":
      return v.external.trim();
    default:
      return "";
  }
}

/** Why the choice cannot be saved, or nothing. */
function problemOf(v: Draft): string | null {
  if (v.kind === "page") {
    const slug = v.slug.trim().replace(/^\/+/, "").toLowerCase();
    if (!slug) return "Səhifənin adını yazın (məsələn: birbank).";
    if (!SLUG.test(slug) || slug.length < 2) return "Səhifənin adında yalnız a-z hərfləri, rəqəmlər və tək-tək defis ola bilər (ən azı 2 simvol).";
    if (v.buttonUrl.trim() && !(v.buttonAz.trim() && v.buttonRu.trim() && v.buttonEn.trim())) return "Düymənin yazısını üç dildə də yazın.";
  }
  if (v.kind === "store" && !v.store) return "Mağazanı seçin.";
  if (v.kind === "listing" && !GUID.test(v.listingId.trim())) return "Elanın nömrəsini (ID) düzgün yazın.";
  if (v.kind === "category" && !v.category) return "Kateqoriyanı seçin.";
  if (v.kind === "site" && !(v.site.trim().startsWith("/") && !v.site.trim().startsWith("//"))) return "Saytın səhifəsi / ilə başlamalıdır, məsələn: /destek";
  if (v.kind === "external" && !/^https?:\/\/\S+$/i.test(v.external.trim())) return "Xarici ünvan https:// ilə başlamalıdır.";
  return null;
}

/** Uploads a picture for a banner through the site's own route (it adds the sign-in). */
async function uploadImage(id: string, file: File): Promise<string | null> {
  if (!TYPES.includes(file.type)) return "Şəkil JPG, PNG və ya WebP olmalıdır.";
  if (file.size > MAX_BYTES) return "Şəkil 5 MB-dan böyük ola bilməz.";
  const form = new FormData();
  form.set("file", file, file.name);
  try {
    const res = await fetch(`/bff/admin-banner-image/${id}`, { method: "PUT", body: form });
    if (res.ok) return null;
    return res.status === 400 ? "Şəkil qəbul edilmədi: faylı yoxlayın (JPG, PNG, WebP, ən çoxu 5 MB)." : "Şəkil yüklənmədi. Yenidən cəhd edin.";
  } catch {
    return "Serverə qoşulmaq olmadı.";
  }
}

/** The frame for a banner picture: wide (3:1), with the phone's narrower cut and live previews. */
function bannerEditor(): EditorOptions {
  return {
    aspect: 3,
    outWidth: 1500,
    pro: true,
    title: "Kampaniya şəklini yerləşdirin",
    hint: "Çərçivənin içi banner olacaq. Telefonda kənarlar azca kəsilir: vacib hissəni sarı xəttin içində saxlayın. Yazılar şəklin üstünə düşür, ona görə yazı yeri sakit olsun.",
    guides: [{ label: "Telefon", aspect: 2.5 }],
    previews: [
      { label: "Kompüterdə", aspect: 3, width: 240 },
      { label: "Telefonda", aspect: 2.5, width: 180 },
    ],
  };
}

function Group({ title, hint, children }: { title: string; hint?: string; children: React.ReactNode }) {
  return (
    <fieldset className="space-y-2 rounded-lg border border-line bg-surface p-3">
      <legend className="px-1 text-sm font-extrabold">{title}</legend>
      {hint && <p className="-mt-1 text-xs text-ink-2">{hint}</p>}
      {children}
    </fieldset>
  );
}

/** The banner as the visitor will see it, with the look chosen so far. */
function Preview({ v, src }: { v: Draft; src: string | null }) {
  const dark = v.textTone === 1;
  const align = v.textAlign === 1 ? "text-center" : v.textAlign === 2 ? "text-right" : "text-left";
  return (
    <div className="relative aspect-[3/1] w-full max-w-xl overflow-hidden rounded-xl bg-primary-soft">
      {src ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={src} alt="" className="absolute inset-0 size-full object-cover" />
      ) : (
        <span className="absolute inset-0 flex items-center justify-center text-xs font-semibold text-primary">Şəkil seçilməyib</span>
      )}
      {v.darkOverlay && (
        <span className={`absolute inset-0 bg-gradient-to-t ${dark ? "from-white/80 via-white/25" : "from-black/70 via-black/20"} to-transparent`} aria-hidden />
      )}
      {v.showText && (
        <span className={`absolute inset-x-0 bottom-0 p-3 ${align} ${dark ? "text-ink" : "text-white"}`}>
          <span className="block text-base font-extrabold leading-tight">{v.titleAz || "Başlıq"}</span>
          {v.subtitleAz && <span className="mt-0.5 block text-xs font-medium">{v.subtitleAz}</span>}
        </span>
      )}
    </div>
  );
}

function BannerForm({
  initial,
  currentImage,
  askImage,
  label,
  stores,
  categories,
  submit,
  onDone,
  onCancel,
}: {
  initial: Draft;
  /** The picture the banner has now, to show in the preview. */
  currentImage?: string | null;
  /** A new banner: pick the picture together with the text. */
  askImage: boolean;
  label: string;
  stores: StoreChoice[];
  categories: CategoryChoice[];
  submit: (input: BannerFormInput) => Promise<BannerSaved>;
  onDone: (createdId?: string, file?: File | null) => Promise<string | null>;
  onCancel: () => void;
}) {
  const router = useRouter();
  const [v, setV] = useState(initial);
  const [file, setFile] = useState<File | null>(null);
  // The photo as it was chosen, so it can be placed again without choosing it again.
  const [original, setOriginal] = useState<File | null>(null);
  const editor = useImageEditor();

  /** The chosen picture is placed in the banner frame first: what the visitor will see, on the computer and the phone. */
  async function chooseImage(chosen: File | undefined) {
    if (!chosen) return;
    setOriginal(chosen);
    const edited = await editor.edit(chosen, bannerEditor());
    if (edited) setFile(edited);
  }
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const set = (patch: Partial<Draft>) => setV((x) => ({ ...x, ...patch }));

  const picked = useMemo(() => (file ? URL.createObjectURL(file) : null), [file]);
  useEffect(() => () => { if (picked) URL.revokeObjectURL(picked); }, [picked]);

  const kind = KINDS.find((k) => k.id === v.kind) ?? KINDS[0];
  const address = linkOf(v);

  async function save() {
    const problem = problemOf(v);
    if (problem) {
      setError(problem);
      return;
    }
    setBusy(true);
    setError(null);
    const page = v.kind === "page";
    const res = await submit({
      titleAz: v.titleAz,
      titleRu: v.titleRu,
      titleEn: v.titleEn,
      subtitleAz: v.subtitleAz,
      subtitleRu: v.subtitleRu,
      subtitleEn: v.subtitleEn,
      linkUrl: page ? "" : address,
      displayOrder: Number(v.displayOrder),
      isActive: v.isActive,
      startsAtUtc: toIso(v.starts),
      endsAtUtc: toIso(v.ends),
      pageSlug: page ? v.slug.trim().replace(/^\/+/, "").toLowerCase() : "",
      pageBodyAz: page ? v.bodyAz : "",
      pageBodyRu: page ? v.bodyRu : "",
      pageBodyEn: page ? v.bodyEn : "",
      pageButtonLabelAz: page ? v.buttonAz : "",
      pageButtonLabelRu: page ? v.buttonRu : "",
      pageButtonLabelEn: page ? v.buttonEn : "",
      pageButtonUrl: page ? v.buttonUrl : "",
      openInNewTab: v.kind === "none" ? false : v.openInNewTab,
      platform: Number(v.platform),
      showText: v.showText,
      textAlign: v.textAlign,
      darkOverlay: v.darkOverlay,
      textTone: v.textTone,
    });
    if (!res.ok) {
      setBusy(false);
      if (res.unauthorized) router.push(adminHref("/giris"));
      else setError(res.message);
      return;
    }
    const imageProblem = await onDone(res.id, file);
    setBusy(false);
    if (imageProblem) setError(`Banner yadda saxlandı, amma: ${imageProblem}`);
    router.refresh();
  }

  const check = "size-4 accent-[var(--color-primary)]";
  const seg = (on: boolean) =>
    `h-8 flex-1 rounded-md border px-2 text-xs font-bold ${on ? "border-primary bg-primary-soft text-primary" : "border-line bg-surface text-ink-2 hover:border-primary"}`;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-3 rounded-lg bg-bg p-3"
    >
      {editor.dialog}
      <Group title="1. Yazılar" hint="Başlıq üç dildə məcburidir. Alt yazı ixtiyaridir.">
        <div className="grid gap-2 sm:grid-cols-3">
          <input aria-label="Başlıq (AZ)" placeholder="Başlıq (AZ)" value={v.titleAz} onChange={(e) => set({ titleAz: e.target.value })} maxLength={100} disabled={busy} className={field} />
          <input aria-label="Başlıq (RU)" placeholder="Заголовок (RU)" value={v.titleRu} onChange={(e) => set({ titleRu: e.target.value })} maxLength={100} disabled={busy} className={field} />
          <input aria-label="Başlıq (EN)" placeholder="Title (EN)" value={v.titleEn} onChange={(e) => set({ titleEn: e.target.value })} maxLength={100} disabled={busy} className={field} />
        </div>
        <div className="grid gap-2 sm:grid-cols-3">
          <input aria-label="Alt yazı (AZ)" placeholder="Alt yazı (AZ, ixtiyari)" value={v.subtitleAz} onChange={(e) => set({ subtitleAz: e.target.value })} maxLength={200} disabled={busy} className={field} />
          <input aria-label="Alt yazı (RU)" placeholder="Подзаголовок (RU)" value={v.subtitleRu} onChange={(e) => set({ subtitleRu: e.target.value })} maxLength={200} disabled={busy} className={field} />
          <input aria-label="Alt yazı (EN)" placeholder="Subtitle (EN)" value={v.subtitleEn} onChange={(e) => set({ subtitleEn: e.target.value })} maxLength={200} disabled={busy} className={field} />
        </div>
      </Group>

      <Group title="2. Banner basılanda nə olsun?" hint={kind.hint}>
        <select aria-label="Basanda nə olsun" value={v.kind} onChange={(e) => set({ kind: e.target.value as LinkKind })} disabled={busy} className={field}>
          {KINDS.map((k) => (
            <option key={k.id} value={k.id}>
              {k.label}
            </option>
          ))}
        </select>

        {v.kind === "page" && (
          <div className="space-y-3">
            <label className="block text-xs font-semibold text-ink-2">
              Səhifənin adı (ünvan)
              <span className="mt-1 flex items-center gap-1">
                <span className="rounded-lg border border-line bg-bg px-2.5 py-2 text-sm text-ink-2">rentox.az/</span>
                <input value={v.slug} onChange={(e) => set({ slug: e.target.value })} placeholder="birbank" maxLength={60} disabled={busy} className={field} />
              </span>
            </label>
            <div className="grid gap-2 sm:grid-cols-3">
              <label className="block text-xs font-semibold text-ink-2">
                Səhifənin mətni (AZ)
                <textarea value={v.bodyAz} onChange={(e) => set({ bodyAz: e.target.value })} rows={5} maxLength={6000} disabled={busy} className={`${area} mt-1`} />
              </label>
              <label className="block text-xs font-semibold text-ink-2">
                Текст страницы (RU)
                <textarea value={v.bodyRu} onChange={(e) => set({ bodyRu: e.target.value })} rows={5} maxLength={6000} disabled={busy} className={`${area} mt-1`} />
              </label>
              <label className="block text-xs font-semibold text-ink-2">
                Page text (EN)
                <textarea value={v.bodyEn} onChange={(e) => set({ bodyEn: e.target.value })} rows={5} maxLength={6000} disabled={busy} className={`${area} mt-1`} />
              </label>
            </div>
            <p className="text-xs text-ink-2">Mətndə **qalın** yazı, [yazı](ünvan) keçid və boş sətirlə abzas işləyir.</p>
            <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr_1.4fr]">
              <input aria-label="Düymə (AZ)" placeholder="Düymə yazısı (AZ)" value={v.buttonAz} onChange={(e) => set({ buttonAz: e.target.value })} maxLength={40} disabled={busy} className={field} />
              <input aria-label="Düymə (RU)" placeholder="Текст кнопки (RU)" value={v.buttonRu} onChange={(e) => set({ buttonRu: e.target.value })} maxLength={40} disabled={busy} className={field} />
              <input aria-label="Düymə (EN)" placeholder="Button text (EN)" value={v.buttonEn} onChange={(e) => set({ buttonEn: e.target.value })} maxLength={40} disabled={busy} className={field} />
              <input aria-label="Düymənin ünvanı" placeholder="Düymə hara aparsın: /search və ya https://… (ixtiyari)" value={v.buttonUrl} onChange={(e) => set({ buttonUrl: e.target.value })} maxLength={500} disabled={busy} className={field} />
            </div>
          </div>
        )}

        {v.kind === "store" && (
          <select aria-label="Mağaza" value={v.store} onChange={(e) => set({ store: e.target.value })} disabled={busy} className={field}>
            <option value="">Mağazanı seçin…</option>
            {v.store && !stores.some((s) => s.slug === v.store) && <option value={v.store}>{v.store}</option>}
            {stores.map((s) => (
              <option key={s.slug} value={s.slug}>
                {s.name}
              </option>
            ))}
          </select>
        )}

        {v.kind === "listing" && (
          <input aria-label="Elanın nömrəsi" placeholder="Elanın nömrəsi (ID), məsələn 3f2b…" value={v.listingId} onChange={(e) => set({ listingId: e.target.value })} maxLength={40} disabled={busy} className={field} />
        )}

        {v.kind === "category" && (
          <select aria-label="Kateqoriya" value={v.category} onChange={(e) => set({ category: e.target.value })} disabled={busy} className={field}>
            <option value="">Kateqoriyanı seçin…</option>
            {v.category && !categories.some((c) => c.id === v.category) && <option value={v.category}>{v.category}</option>}
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        )}

        {v.kind === "site" && (
          <>
            <input aria-label="Saytın səhifəsi" list="banner-places" placeholder="/destek" value={v.site} onChange={(e) => set({ site: e.target.value })} maxLength={500} disabled={busy} className={field} />
            <datalist id="banner-places">
              {SITE_PLACES.map((p) => (
                <option key={p} value={p} />
              ))}
            </datalist>
          </>
        )}

        {v.kind === "external" && (
          <input aria-label="Xarici ünvan" placeholder="https://…" value={v.external} onChange={(e) => set({ external: e.target.value })} maxLength={500} disabled={busy} className={field} />
        )}

        {v.kind !== "none" && (
          <>
            <label className="flex items-center gap-2 text-sm font-semibold">
              <input type="checkbox" checked={v.openInNewTab} onChange={(e) => set({ openInNewTab: e.target.checked })} disabled={busy} className={check} />
              Yeni pəncərədə açılsın
            </label>
            <p className="text-xs text-ink-2">
              Açılacaq ünvan:{" "}
              <b className="break-all text-ink">{v.kind === "page" ? `rentox.az/${v.slug.trim().replace(/^\/+/, "").toLowerCase() || "…"}` : address || "…"}</b>
            </p>
          </>
        )}
      </Group>

      <Group title="3. Görünüş">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-1">
            <span className="block text-xs font-semibold text-ink-2">Harada göstərilsin</span>
            <div className="flex gap-1">
              {[
                ["3", "Sayt və tətbiq"],
                ["1", "Yalnız sayt"],
                ["2", "Yalnız tətbiq"],
              ].map(([id, text]) => (
                <button key={id} type="button" onClick={() => set({ platform: id })} disabled={busy} className={seg(v.platform === id)}>
                  {text}
                </button>
              ))}
            </div>
          </div>
          <div className="space-y-1">
            <span className="block text-xs font-semibold text-ink-2">Yazının yeri</span>
            <div className="flex gap-1">
              {["Solda", "Ortada", "Sağda"].map((text, i) => (
                <button key={text} type="button" onClick={() => set({ textAlign: i })} disabled={busy || !v.showText} className={seg(v.textAlign === i)}>
                  {text}
                </button>
              ))}
            </div>
          </div>
          <div className="space-y-1">
            <span className="block text-xs font-semibold text-ink-2">Yazının rəngi</span>
            <div className="flex gap-1">
              {["Açıq (ağ)", "Tünd"].map((text, i) => (
                <button key={text} type="button" onClick={() => set({ textTone: i })} disabled={busy || !v.showText} className={seg(v.textTone === i)}>
                  {text}
                </button>
              ))}
            </div>
          </div>
          <div className="flex flex-col justify-end gap-2">
            <label className="flex items-center gap-2 text-sm font-semibold">
              <input type="checkbox" checked={v.showText} onChange={(e) => set({ showText: e.target.checked })} disabled={busy} className={check} />
              Başlıq və alt yazı şəkil üzərində yazılsın (söndürsən şəkil təmiz görünür)
            </label>
            <label className="flex items-center gap-2 text-sm font-semibold">
              <input type="checkbox" checked={v.darkOverlay} onChange={(e) => set({ darkOverlay: e.target.checked })} disabled={busy} className={check} />
              Şəklin altında solğun fon olsun (yazı oxunsun deyə; söndürsən şəkil olduğu kimi qalır)
            </label>
          </div>
        </div>
        <Preview v={v} src={picked ?? currentImage ?? null} />
      </Group>

      <Group title="4. Vaxt və sıra">
        <div className="grid gap-2 sm:grid-cols-[1fr_1fr_120px]">
          <label className="block text-xs font-semibold text-ink-2">
            Başlayır (ixtiyari)
            <input type="datetime-local" value={v.starts} onChange={(e) => set({ starts: e.target.value })} disabled={busy} className={`${field} mt-1`} />
          </label>
          <label className="block text-xs font-semibold text-ink-2">
            Bitir (ixtiyari)
            <input type="datetime-local" value={v.ends} onChange={(e) => set({ ends: e.target.value })} disabled={busy} className={`${field} mt-1`} />
          </label>
          <label className="block text-xs font-semibold text-ink-2">
            Sıra
            <input aria-label="Sıra" placeholder="Sıra" inputMode="numeric" value={v.displayOrder} onChange={(e) => set({ displayOrder: e.target.value })} disabled={busy} className={`${field} mt-1`} />
          </label>
        </div>
        {askImage && (
          <label className="block text-xs font-semibold text-ink-2">
            Şəkil (JPG, PNG, WebP, ən çoxu 5 MB; geniş şəkil yaxşıdır, məsələn 1200×400)
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(e) => {
                void chooseImage(e.target.files?.[0]);
                e.target.value = "";
              }}
              disabled={busy}
              className="mt-1 block w-full text-sm"
            />
            {file && original && (
              <button
                type="button"
                onClick={() => void chooseImage(original)}
                disabled={busy}
                className="mt-2 h-8 rounded-md border border-line bg-surface px-3 text-xs font-bold text-primary hover:border-primary"
              >
                Şəkli yenidən yerləşdir
              </button>
            )}
          </label>
        )}
        <label className="flex items-center gap-2 text-sm font-semibold">
          <input type="checkbox" checked={v.isActive} onChange={(e) => set({ isActive: e.target.checked })} disabled={busy} className={check} />
          Göstərilir
        </label>
      </Group>

      <div className="flex flex-wrap items-center gap-2">
        <button type="submit" disabled={busy} className="flex h-9 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60">
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {label}
        </button>
        <button type="button" onClick={onCancel} disabled={busy} className="h-9 rounded-lg px-3 text-sm font-semibold text-ink-2 hover:bg-primary-soft">
          Ləğv et
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </div>
    </form>
  );
}

const PLATFORM_TEXT: Record<number, string> = { 1: "yalnız sayt", 2: "yalnız tətbiq" };

/** The banners of the home page: the list, a form to add one, and for each a picture, edit and delete. */
export function BannerManager({ banners, stores, categories }: { banners: BannerRow[]; stores: StoreChoice[]; categories: CategoryChoice[] }) {
  const router = useRouter();
  const [open, setOpen] = useState<string | null>(null); // "new" or a banner id
  const [deleting, setDeleting] = useState<BannerRow | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<{ tone: "ok" | "bad"; text: string } | null>(null);
  // A new picture has the same address: this makes the browser ask for it again.
  const [version, setVersion] = useState(0);
  const picker = useRef<HTMLInputElement>(null);
  const pickFor = useRef<string | null>(null);
  const editor = useImageEditor();

  /** The picture the banner already has, placed again (moved, zoomed, background, colours) without choosing a new one. */
  async function tuneImage(id: string) {
    setBusy(id);
    setMessage(null);
    const current = await fetchPicture(`/api/banners/${id}/image?v=${version}`, "banner");
    setBusy(null);
    if (!current) return setMessage({ tone: "bad", text: "Şəkil açılmadı. Başqa şəkil seçin." });
    await placeAndChange(id, current);
  }

  async function placeAndChange(id: string, chosen: File) {
    const edited = await editor.edit(chosen, bannerEditor());
    if (edited) await changeImage(id, edited);
  }

  async function changeImage(id: string, file: File) {
    setBusy(id);
    setMessage(null);
    const problem = await uploadImage(id, file);
    setBusy(null);
    if (problem) setMessage({ tone: "bad", text: problem });
    else {
      await bannerImageChanged();
      setVersion((x) => x + 1);
      router.refresh();
    }
  }

  async function remove(b: BannerRow) {
    setBusy(b.id);
    const res = await deleteBanner(b.id);
    setBusy(null);
    setDeleting(null);
    if (res.ok) router.refresh();
    else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ tone: "bad", text: res.message });
  }

  const nextOrder = banners.length ? Math.max(...banners.map((b) => b.displayOrder)) + 1 : 1;

  return (
    <div className="space-y-4">
      {editor.dialog}
      <input
        ref={picker}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0];
          const id = pickFor.current;
          e.target.value = "";
          if (file && id) void placeAndChange(id, file);
        }}
      />

      {message && (
        <p role="status" className={`rounded-xl px-4 py-3 text-sm font-semibold ${message.tone === "ok" ? "bg-success/10 text-success" : "bg-danger/10 text-danger"}`}>
          {message.text}
        </p>
      )}

      {open === "new" ? (
        <BannerForm
          initial={draftOf(undefined, nextOrder)}
          askImage
          label="Kampaniyanı əlavə et"
          stores={stores}
          categories={categories}
          submit={createBanner}
          onCancel={() => setOpen(null)}
          onDone={async (id, file) => {
            setOpen(null);
            if (id && file) {
              const problem = await uploadImage(id, file);
              if (!problem) {
                await bannerImageChanged();
                setVersion((x) => x + 1);
              }
              return problem;
            }
            return null;
          }}
        />
      ) : (
        <button type="button" onClick={() => setOpen("new")} className="flex h-10 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press">
          <Plus className="size-4" aria-hidden />
          Yeni kampaniya
        </button>
      )}

      {banners.length === 0 && <p className="rounded-xl border border-line bg-surface px-4 py-6 text-center text-sm text-ink-2">Hələ kampaniya yoxdur.</p>}

      <ul className="space-y-3">
        {banners.map((b) => (
          <li key={b.id} className="rounded-xl border border-line bg-surface p-3">
            <div className="flex flex-wrap items-center gap-3">
              <span className="relative flex h-16 w-28 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-primary-soft text-primary">
                {b.hasImage ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={`/api/banners/${b.id}/image?v=${version}`} alt="" className="size-full object-cover" />
                ) : (
                  <ImagePlus className="size-5" aria-hidden />
                )}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate font-bold">{b.titleAz}</span>
                <span className="block truncate text-xs text-ink-2">
                  {b.titleRu} · {b.titleEn} · sıra {b.displayOrder}
                  {b.platform && PLATFORM_TEXT[b.platform] ? ` · ${PLATFORM_TEXT[b.platform]}` : ""}
                </span>
                <span className="block truncate text-xs text-ink-2">
                  {b.linkUrl ? (
                    <>
                      <ExternalLink className="mr-1 inline size-3" aria-hidden />
                      {b.pageSlug ? `kampaniya səhifəsi: rentox.az/${b.pageSlug}` : `keçid: ${b.linkUrl}`}
                    </>
                  ) : (
                    "kliklənmir"
                  )}
                </span>
                {(b.startsAtUtc || b.endsAtUtc) && (
                  <span className="block text-xs text-ink-2">
                    {b.startsAtUtc ? fmtNumeric(b.startsAtUtc) : "…"} — {b.endsAtUtc ? fmtNumeric(b.endsAtUtc) : "…"}
                  </span>
                )}
              </span>
              {b.isLive ? (
                <Pill tone="success">Saytda</Pill>
              ) : !b.hasImage ? (
                <Pill tone="warning">Şəkil yoxdur</Pill>
              ) : !b.isActive ? (
                <Pill tone="muted">Söndürülüb</Pill>
              ) : (
                <Pill tone="muted">Vaxtı gəlməyib / bitib</Pill>
              )}
              <span className="flex gap-1">
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => {
                    pickFor.current = b.id;
                    picker.current?.click();
                  }}
                  className="flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft disabled:opacity-50"
                >
                  {busy === b.id ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <ImagePlus className="size-4" aria-hidden />}
                  Şəkil
                </button>
                {b.hasImage && (
                  <button
                    type="button"
                    disabled={busy !== null}
                    onClick={() => void tuneImage(b.id)}
                    className="flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft disabled:opacity-50"
                  >
                    <SlidersHorizontal className="size-4" aria-hidden />
                    Düzəlt
                  </button>
                )}
                <button type="button" onClick={() => setOpen(open === b.id ? null : b.id)} className="flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft">
                  <Pencil className="size-4" aria-hidden />
                  Dəyiş
                </button>
                <button type="button" disabled={busy !== null} onClick={() => setDeleting(b)} className="flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-danger hover:bg-danger/10 disabled:opacity-50">
                  <Trash2 className="size-4" aria-hidden />
                  Sil
                </button>
              </span>
            </div>
            {open === b.id && (
              <div className="mt-3">
                <BannerForm
                  initial={draftOf(b)}
                  currentImage={b.hasImage ? `/api/banners/${b.id}/image?v=${version}` : null}
                  askImage={false}
                  label="Yadda saxla"
                  stores={stores}
                  categories={categories}
                  submit={(i) => updateBanner(b.id, i)}
                  onCancel={() => setOpen(null)}
                  onDone={async () => {
                    setOpen(null);
                    return null;
                  }}
                />
              </div>
            )}
          </li>
        ))}
      </ul>

      <ConfirmDialog
        open={deleting !== null}
        title="Kampaniya silinsin?"
        message={`«${deleting?.titleAz ?? ""}» kampaniyası, şəkli və səhifəsi həmişəlik silinəcək. Müvəqqəti gizlətmək üçün «Dəyiş» ilə söndürün.`}
        confirmLabel="Sil"
        destructive
        busy={busy !== null}
        onClose={() => setDeleting(null)}
        onConfirm={() => deleting && void remove(deleting)}
      />
    </div>
  );
}
