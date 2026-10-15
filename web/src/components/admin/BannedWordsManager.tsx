"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, Plus, X } from "lucide-react";
import { addBannedWord, removeBannedWord } from "@/app/admin/(panel)/banned-actions";
import { adminHref } from "@/lib/admin-path";

export type BannedWordRow = { id: string; word: string; createdAtUtc: string };

/** The list of words, a box to add one, and a cross on each to remove it. */
export function BannedWordsManager({ words }: { words: BannedWordRow[] }) {
  const router = useRouter();
  const [word, setWord] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function run(key: string, call: () => ReturnType<typeof addBannedWord>) {
    setBusy(key);
    setError(null);
    const res = await call();
    setBusy(null);
    if (res.ok) {
      if (key === "add") setWord("");
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <div className="space-y-4">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (word.trim()) void run("add", () => addBannedWord(word));
        }}
        className="space-y-2 rounded-xl border border-line bg-surface p-4"
      >
        <label className="block">
          <span className="mb-1 block text-sm font-bold">Yeni söz və ya ifadə</span>
          <div className="flex gap-2">
            <input
              value={word}
              onChange={(e) => setWord(e.target.value)}
              maxLength={50}
              placeholder="məsələn: dolandır*"
              disabled={busy !== null}
              className="h-10 min-w-0 flex-1 rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60"
            />
            <button
              type="submit"
              disabled={busy !== null || !word.trim()}
              className="flex h-10 shrink-0 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
            >
              {busy === "add" ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Plus className="size-4" aria-hidden />}
              Əlavə et
            </button>
          </div>
        </label>
        <p className="text-xs text-ink-2">
          Yalnız tam söz tapılır (məsələn «sik» «musiqi» içində tapılmır). Sözün sonuna <b>*</b> qoysanız, bütün şəkilçiləri tutur:
          «dolandır*» → dolandırıcı, dolandırıcılıq. Böyük-kiçik hərf fərqi yoxdur. Elan tapılsa, silinmir: moderatora «qadağan söz» işarəsi ilə göstərilir.
        </p>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </form>

      <section className="rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 text-[15px] font-extrabold">Siyahı ({words.length})</h2>
        {words.length === 0 ? (
          <p className="px-4 py-6 text-center text-sm text-ink-2">Hələ söz əlavə edilməyib.</p>
        ) : (
          <ul className="flex flex-wrap gap-2 p-4">
            {words.map((w) => (
              <li key={w.id} className="flex items-center gap-1 rounded-full bg-warning-soft py-1 pl-3 pr-1 text-sm font-semibold">
                {w.word}
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => void run(w.id, () => removeBannedWord(w.id))}
                  aria-label={`Sil: ${w.word}`}
                  className="flex size-6 items-center justify-center rounded-full text-ink-2 hover:bg-danger/15 hover:text-danger disabled:opacity-50"
                >
                  {busy === w.id ? <Loader2 className="size-3.5 animate-spin" aria-hidden /> : <X className="size-3.5" aria-hidden />}
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
