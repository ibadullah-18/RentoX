"use client";

import { fmtNumeric } from "@/lib/dates";

import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { ImagePlus, Loader2, Pencil, Plus, Trash2 } from "lucide-react";
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
};

const field =
  "h-9 w-full rounded-lg border border-line bg-surface px-2.5 text-sm outline-none focus:border-primary disabled:opacity-60";
const TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_BYTES = 5 * 1024 * 1024;

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
  linkUrl: string;
  displayOrder: string;
  isActive: boolean;
  starts: string;
  ends: string;
};

const draftOf = (b?: BannerRow, order = 1): Draft => ({
  titleAz: b?.titleAz ?? "",
  titleRu: b?.titleRu ?? "",
  titleEn: b?.titleEn ?? "",
  subtitleAz: b?.subtitleAz ?? "",
  subtitleRu: b?.subtitleRu ?? "",
  subtitleEn: b?.subtitleEn ?? "",
  linkUrl: b?.linkUrl ?? "",
  displayOrder: String(b?.displayOrder ?? order),
  isActive: b?.isActive ?? true,
  starts: toLocal(b?.startsAtUtc),
  ends: toLocal(b?.endsAtUtc),
});

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

function BannerForm({
  initial,
  askImage,
  label,
  submit,
  onDone,
  onCancel,
}: {
  initial: Draft;
  /** A new banner: pick the picture together with the text. */
  askImage: boolean;
  label: string;
  submit: (input: BannerFormInput) => Promise<BannerSaved>;
  onDone: (createdId?: string, file?: File | null) => Promise<string | null>;
  onCancel: () => void;
}) {
  const router = useRouter();
  const [v, setV] = useState(initial);
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const set = (patch: Partial<Draft>) => setV((x) => ({ ...x, ...patch }));

  async function save() {
    setBusy(true);
    setError(null);
    const res = await submit({
      titleAz: v.titleAz,
      titleRu: v.titleRu,
      titleEn: v.titleEn,
      subtitleAz: v.subtitleAz,
      subtitleRu: v.subtitleRu,
      subtitleEn: v.subtitleEn,
      linkUrl: v.linkUrl,
      displayOrder: Number(v.displayOrder),
      isActive: v.isActive,
      startsAtUtc: toIso(v.starts),
      endsAtUtc: toIso(v.ends),
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

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-3 rounded-lg bg-bg p-3"
    >
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
      <div className="grid gap-2 sm:grid-cols-[1fr_120px]">
        <input aria-label="Keçid" placeholder="Keçid: /destek və ya https://… (ixtiyari)" value={v.linkUrl} onChange={(e) => set({ linkUrl: e.target.value })} maxLength={500} disabled={busy} className={field} />
        <input aria-label="Sıra" placeholder="Sıra" inputMode="numeric" value={v.displayOrder} onChange={(e) => set({ displayOrder: e.target.value })} disabled={busy} className={field} />
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <label className="block text-xs font-semibold text-ink-2">
          Başlayır (ixtiyari)
          <input type="datetime-local" value={v.starts} onChange={(e) => set({ starts: e.target.value })} disabled={busy} className={`${field} mt-1`} />
        </label>
        <label className="block text-xs font-semibold text-ink-2">
          Bitir (ixtiyari)
          <input type="datetime-local" value={v.ends} onChange={(e) => set({ ends: e.target.value })} disabled={busy} className={`${field} mt-1`} />
        </label>
      </div>
      {askImage && (
        <label className="block text-xs font-semibold text-ink-2">
          Şəkil (JPG, PNG, WebP, ən çoxu 5 MB; geniş şəkil yaxşıdır, məsələn 1200×400)
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            disabled={busy}
            className="mt-1 block w-full text-sm"
          />
        </label>
      )}
      <label className="flex items-center gap-2 text-sm font-semibold">
        <input type="checkbox" checked={v.isActive} onChange={(e) => set({ isActive: e.target.checked })} disabled={busy} className="size-4 accent-[var(--color-primary)]" />
        Göstərilir
      </label>
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

/** The banners of the home page: the list, a form to add one, and for each a picture, edit and delete. */
export function BannerManager({ banners }: { banners: BannerRow[] }) {
  const router = useRouter();
  const [open, setOpen] = useState<string | null>(null); // "new" or a banner id
  const [deleting, setDeleting] = useState<BannerRow | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<{ tone: "ok" | "bad"; text: string } | null>(null);
  // A new picture has the same address: this makes the browser ask for it again.
  const [version, setVersion] = useState(0);
  const picker = useRef<HTMLInputElement>(null);
  const pickFor = useRef<string | null>(null);

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
      <input
        ref={picker}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0];
          const id = pickFor.current;
          e.target.value = "";
          if (file && id) void changeImage(id, file);
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
          label="Banneri əlavə et"
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
          Yeni banner
        </button>
      )}

      {banners.length === 0 && <p className="rounded-xl border border-line bg-surface px-4 py-6 text-center text-sm text-ink-2">Hələ banner yoxdur.</p>}

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
                  {b.linkUrl ? ` · ${b.linkUrl}` : ""}
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
                  askImage={false}
                  label="Yadda saxla"
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
        title="Banner silinsin?"
        message={`«${deleting?.titleAz ?? ""}» bannerı və şəkli həmişəlik silinəcək. Müvəqqəti gizlətmək üçün «Dəyiş» ilə söndürün.`}
        confirmLabel="Sil"
        destructive
        busy={busy !== null}
        onClose={() => setDeleting(null)}
        onConfirm={() => deleting && void remove(deleting)}
      />
    </div>
  );
}
