"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Eye, Loader2, Pencil } from "lucide-react";
import { savePage, type PageFormInput } from "@/app/admin/(panel)/content-actions";
import { Markdown } from "@/components/Markdown";
import { adminHref } from "@/lib/admin-path";

export type PageRow = {
  slug: string;
  titleAz: string;
  titleRu: string;
  titleEn: string;
  bodyAz: string;
  bodyRu: string;
  bodyEn: string;
};

const langs = [
  { id: "Az", label: "Azərbaycanca", note: "Əsas dil. Mübahisədə bu mətn əsas götürülür." },
  { id: "Ru", label: "Русский", note: "" },
  { id: "En", label: "English", note: "" },
] as const;

type LangId = (typeof langs)[number]["id"];

const field =
  "w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

/** One page in three languages: a title and a text for each, with a preview of how it will look. */
export function PageEditor({ page }: { page: PageRow }) {
  const router = useRouter();
  const [lang, setLang] = useState<LangId>("Az");
  const [preview, setPreview] = useState(false);
  const [v, setV] = useState<PageFormInput>({
    titleAz: page.titleAz,
    titleRu: page.titleRu,
    titleEn: page.titleEn,
    bodyAz: page.bodyAz,
    bodyRu: page.bodyRu,
    bodyEn: page.bodyEn,
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const titleKey = `title${lang}` as keyof PageFormInput;
  const bodyKey = `body${lang}` as keyof PageFormInput;
  const changed =
    v.titleAz !== page.titleAz || v.titleRu !== page.titleRu || v.titleEn !== page.titleEn ||
    v.bodyAz !== page.bodyAz || v.bodyRu !== page.bodyRu || v.bodyEn !== page.bodyEn;

  async function save() {
    setBusy(true);
    setError(null);
    setSaved(false);
    const res = await savePage(page.slug, v);
    setBusy(false);
    if (res.ok) {
      setSaved(true);
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-4"
    >
      <div role="tablist" aria-label="Dil" className="flex flex-wrap gap-2">
        {langs.map((l) => (
          <button
            key={l.id}
            type="button"
            role="tab"
            aria-selected={lang === l.id}
            onClick={() => setLang(l.id)}
            className={`h-9 rounded-lg px-4 text-sm font-bold ${lang === l.id ? "bg-primary text-white" : "border border-line hover:border-primary hover:text-primary"}`}
          >
            {l.label}
          </button>
        ))}
      </div>
      {langs.find((l) => l.id === lang)?.note && <p className="text-xs text-ink-2">{langs.find((l) => l.id === lang)?.note}</p>}

      <label className="block">
        <span className="mb-1 block text-sm font-bold">Başlıq</span>
        <input
          value={v[titleKey]}
          onChange={(e) => {
            setV({ ...v, [titleKey]: e.target.value });
            setSaved(false);
          }}
          maxLength={150}
          disabled={busy}
          className={`${field} h-10`}
        />
      </label>

      <div>
        <div className="mb-1 flex items-center justify-between">
          <span className="text-sm font-bold">Mətn</span>
          <button type="button" onClick={() => setPreview((x) => !x)} className="flex h-8 items-center gap-1.5 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft">
            {preview ? <Pencil className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
            {preview ? "Redaktə" : "Önbaxış"}
          </button>
        </div>
        {preview ? (
          <div className="min-h-64 rounded-lg border border-line bg-surface p-4">
            <h1 className="mb-4 text-2xl font-extrabold">{v[titleKey]}</h1>
            <Markdown text={v[bodyKey]} />
          </div>
        ) : (
          <textarea
            value={v[bodyKey]}
            onChange={(e) => {
              setV({ ...v, [bodyKey]: e.target.value });
              setSaved(false);
            }}
            rows={24}
            maxLength={60000}
            spellCheck
            disabled={busy}
            className={`${field} py-3 font-mono text-[13px] leading-relaxed`}
          />
        )}
        <p className="mt-1 text-right text-xs text-ink-2">{v[bodyKey].length} / 60000</p>
      </div>

      <details className="rounded-lg bg-bg p-3 text-sm">
        <summary className="cursor-pointer font-bold">Yazı qaydaları (kömək)</summary>
        <ul className="mt-2 list-disc space-y-1 pl-5 text-ink-2">
          <li><code>## Başlıq</code>: bölmə başlığı (<code>###</code> kiçik başlıq).</li>
          <li>Boş sətir yeni abzas başladır. Eyni abzasdakı yeni sətir sətir keçidi kimi qalır.</li>
          <li><code>- bənd</code>: nöqtəli siyahı, <code>1. bənd</code>: nömrəli siyahı.</li>
          <li><code>**qalın yazı**</code> və <code>[mətn](https://sayt.az)</code> link.</li>
          <li>
            Şirkət məlumatı üçün işarələr (Əlaqə və şirkət səhifəsində doldurulur):{" "}
            <code>{"{{operatorName}}"}</code> <code>{"{{voen}}"}</code> <code>{"{{address}}"}</code> <code>{"{{email}}"}</code>{" "}
            <code>{"{{phone}}"}</code> <code>{"{{hotline}}"}</code>. Məlumat boşdursa, «Telefon: {"{{phone}}"}» kimi ayrıca sətir göstərilmir.
          </li>
          <li>Mətni dəyişəndə üç dili də uyğun saxlayın. Hüquqi mətnləri dəyişməzdən əvvəl hüquqşünasla məsləhətləşin.</li>
        </ul>
      </details>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !changed}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Yadda saxla
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
        {saved && <p role="status" className="text-sm font-semibold text-success">Saxlanıldı, saytda dərhal görünür.</p>}
      </div>
    </form>
  );
}
