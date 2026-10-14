"use client";

import { useRouter } from "next/navigation";
import { useRef, useState } from "react";
import { Camera, Loader2, Trash2 } from "lucide-react";
import { deleteAccount, updateProfile } from "@/app/kabinet/profil/actions";
import { useT } from "@/lib/i18n/client";
import { LOCALES, type Locale } from "@/lib/i18n/locales";
import { ConfirmDialog } from "./ConfirmDialog";
import { UserAvatar } from "./messages/UserAvatar";
import { Select } from "./ui/Select";

const TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_BYTES = 5 * 1024 * 1024;
const input =
  "w-full rounded-lg border border-line bg-surface px-3.5 py-2.5 text-[16px] outline-none focus:border-primary disabled:opacity-60";

/** Photo, name, about-me and language of the account; and deleting it. */
export function ProfileForm({
  userId,
  phone,
  initialName,
  initialBio,
  initialLanguage,
}: {
  userId: string;
  phone: string;
  initialName: string;
  initialBio: string;
  initialLanguage: Locale;
}) {
  const t = useT();
  const router = useRouter();
  const file = useRef<HTMLInputElement>(null);
  const [name, setName] = useState(initialName);
  const [bio, setBio] = useState(initialBio);
  const [language, setLanguage] = useState<Locale>(initialLanguage);
  const [photoVersion, setPhotoVersion] = useState(0);
  const [busy, setBusy] = useState<"save" | "photo" | "delete" | null>(null);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
  const [askDelete, setAskDelete] = useState(false);

  async function save() {
    if (busy) return;
    setBusy("save");
    setMessage(null);
    const res = await updateProfile({ fullName: name, bio, language });
    setBusy(null);
    if (res.ok) {
      setMessage({ ok: true, text: t("Yadda saxlanıldı.") });
      router.refresh();
    } else if (res.unauthorized) router.push("/giris?next=/kabinet/profil");
    else setMessage({ ok: false, text: res.message });
  }

  async function upload(picked: File | undefined) {
    if (!picked || busy) return;
    if (!TYPES.includes(picked.type)) return setMessage({ ok: false, text: t("Yalnız JPEG, PNG və WebP şəkilləri qəbul olunur.") });
    if (picked.size === 0 || picked.size > MAX_BYTES) {
      return setMessage({ ok: false, text: t("Şəkil ən çoxu {n} MB ola bilər.", { n: 5 }) });
    }
    setBusy("photo");
    setMessage(null);
    const form = new FormData();
    form.set("file", picked, picked.name);
    try {
      const res = await fetch("/bff/profile-image", { method: "POST", body: form });
      if (res.status === 401) return router.push("/giris?next=/kabinet/profil");
      if (!res.ok) return setMessage({ ok: false, text: t("Şəkil yüklənmədi. Yenidən cəhd edin.") });
      setPhotoVersion((v) => v + 1);
      router.refresh();
    } catch {
      setMessage({ ok: false, text: t("Serverə qoşulmaq olmadı.") });
    } finally {
      setBusy(null);
    }
  }

  async function removePhoto() {
    if (busy) return;
    setBusy("photo");
    try {
      await fetch("/bff/profile-image", { method: "DELETE" });
      setPhotoVersion((v) => v + 1);
      router.refresh();
    } finally {
      setBusy(null);
    }
  }

  async function remove() {
    setBusy("delete");
    const res = await deleteAccount();
    // On success the server has already sent the person to the home page.
    if (res && !res.ok) {
      setBusy(null);
      setAskDelete(false);
      if (res.unauthorized) router.push("/giris");
      else setMessage({ ok: false, text: res.message });
    }
  }

  return (
    <div className="space-y-5">
      <section className="flex items-center gap-4 rounded-xl border border-line bg-surface p-4">
        <div className="relative">
          <UserAvatar key={photoVersion} userId={userId} size={72} version={photoVersion} />
          {busy === "photo" && (
            <span className="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 text-white">
              <Loader2 className="size-6 animate-spin" aria-hidden />
            </span>
          )}
        </div>
        <div className="min-w-0 space-y-1.5">
          <p className="truncate text-sm text-ink-2">{phone}</p>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => file.current?.click()}
              disabled={busy !== null}
              className="flex h-9 items-center gap-1.5 rounded-lg bg-primary-soft px-3 text-[13px] font-bold text-primary hover:opacity-80 disabled:opacity-60"
            >
              <Camera className="size-4" aria-hidden />
              {t("Şəkil əlavə et")}
            </button>
            <button
              type="button"
              onClick={() => void removePhoto()}
              disabled={busy !== null}
              className="flex h-9 items-center rounded-lg border border-line px-3 text-[13px] font-semibold text-ink-2 hover:border-danger hover:text-danger disabled:opacity-60"
            >
              {t("Şəkli sil")}
            </button>
          </div>
          <input
            ref={file}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            hidden
            onChange={(e) => {
              void upload(e.target.files?.[0]);
              e.target.value = "";
            }}
          />
        </div>
      </section>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          void save();
        }}
        className="space-y-4 rounded-xl border border-line bg-surface p-4"
      >
        <label className="block">
          <span className="mb-1.5 block text-sm font-bold">{t("Ad və soyad")}</span>
          <input value={name} onChange={(e) => setName(e.target.value)} maxLength={100} disabled={busy !== null} className={input} />
        </label>
        <label className="block">
          <span className="mb-1.5 block text-sm font-bold">{t("Haqqımda")}</span>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            maxLength={500}
            rows={4}
            disabled={busy !== null}
            placeholder={t("Özünüz haqqında qısa məlumat")}
            className={`${input} resize-none`}
          />
          <span className="mt-1 block text-right text-xs text-ink-2">{bio.length} / 500</span>
        </label>
        <div className="block">
          <span className="mb-1.5 block text-sm font-bold">{t("Dil")}</span>
          <Select
            value={language}
            onChange={(v) => setLanguage(v as Locale)}
            ariaLabel={t("Dil")}
            disabled={busy !== null}
            options={LOCALES.map((code) => ({ value: code, label: code.toUpperCase() }))}
          />
        </div>

        {message && (
          <p role="status" className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
            {message.text}
          </p>
        )}
        <button
          type="submit"
          disabled={busy !== null}
          className="flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-60"
        >
          {busy === "save" && <Loader2 className="size-5 animate-spin" aria-hidden />}
          {t("Yadda saxla")}
        </button>
      </form>

      <section className="rounded-xl border border-line bg-surface p-4">
        <h2 className="font-bold">{t("Hesabı sil")}</h2>
        <p className="mt-1 text-sm text-ink-2">{t("Hesabınız, elanlarınız və mesajlarınız silinəcək. Bunu geri qaytarmaq olmaz.")}</p>
        <button
          type="button"
          onClick={() => setAskDelete(true)}
          disabled={busy !== null}
          className="mt-3 flex h-10 items-center gap-2 rounded-lg border border-line px-4 text-[14px] font-bold text-danger hover:border-danger disabled:opacity-60"
        >
          <Trash2 className="size-4" aria-hidden />
          {t("Hesabı sil")}
        </button>
      </section>

      <ConfirmDialog
        open={askDelete}
        title={t("Hesab silinsin?")}
        message={t("Hesabınız, elanlarınız və mesajlarınız silinəcək. Bunu geri qaytarmaq olmaz.")}
        confirmLabel={t("Hesabı sil")}
        destructive
        busy={busy === "delete"}
        onClose={() => setAskDelete(false)}
        onConfirm={() => void remove()}
      />
    </div>
  );
}
