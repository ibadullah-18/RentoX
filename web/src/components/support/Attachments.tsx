"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { ImagePlus, X } from "lucide-react";

export const MAX_PHOTOS = 5;
const MAX_BYTES = 10 * 1024 * 1024;
const TYPES = ["image/jpeg", "image/png", "image/webp"];

export type Attach = {
  files: File[];
  previews: string[];
  /** A short reason when the last choice was not accepted. */
  problem: string | null;
  add: (picked: FileList | File[] | null) => void;
  remove: (index: number) => void;
  clear: () => void;
};

/**
 * The photos chosen for the next message: up to 5, JPG, PNG or WebP, at most 10 MB each. Each choice is checked here
 * too, so a wrong file is explained at once instead of failing after the upload.
 */
export function useAttachments(messages: { type: string; size: string; count: string }): Attach {
  const [files, setFiles] = useState<File[]>([]);
  const [problem, setProblem] = useState<string | null>(null);
  const previews = useMemo(() => files.map((f) => URL.createObjectURL(f)), [files]);
  useEffect(() => () => previews.forEach((url) => URL.revokeObjectURL(url)), [previews]);

  return {
    files,
    previews,
    problem,
    add(picked) {
      if (!picked) return;
      const incoming = Array.from(picked);
      let note: string | null = null;
      const accepted: File[] = [];
      for (const file of incoming) {
        if (!TYPES.includes(file.type)) note = messages.type;
        else if (file.size === 0 || file.size > MAX_BYTES) note = messages.size;
        else accepted.push(file);
      }
      const room = MAX_PHOTOS - files.length;
      if (accepted.length > room) note = messages.count;
      setProblem(note);
      if (accepted.length > 0) setFiles((now) => [...now, ...accepted].slice(0, MAX_PHOTOS));
    },
    remove(index) {
      setProblem(null);
      setFiles((now) => now.filter((_, i) => i !== index));
    },
    clear() {
      setProblem(null);
      setFiles([]);
    },
  };
}

/** The little picture button next to the text box. */
export function AttachButton({ attach, disabled, label }: { attach: Attach; disabled?: boolean; label: string }) {
  const input = useRef<HTMLInputElement>(null);
  return (
    <>
      <input
        ref={input}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        multiple
        hidden
        onChange={(e) => {
          attach.add(e.target.files);
          e.target.value = "";
        }}
      />
      <button
        type="button"
        onClick={() => input.current?.click()}
        disabled={disabled || attach.files.length >= MAX_PHOTOS}
        aria-label={label}
        title={label}
        className="flex size-12 shrink-0 items-center justify-center rounded-xl border border-line bg-surface text-ink-2 hover:border-primary hover:text-primary disabled:opacity-50"
      >
        <ImagePlus className="size-5" aria-hidden />
      </button>
    </>
  );
}

/** The chosen photos before they are sent, each with a way to take it out. */
export function AttachPreview({ attach, disabled, removeLabel }: { attach: Attach; disabled?: boolean; removeLabel: string }) {
  if (attach.files.length === 0) return null;
  return (
    <ul className="flex flex-wrap gap-2">
      {attach.previews.map((src, i) => (
        <li key={src} className="relative size-16 overflow-hidden rounded-lg border border-line bg-bg">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={src} alt="" className="size-full object-cover" />
          <button
            type="button"
            onClick={() => attach.remove(i)}
            disabled={disabled}
            aria-label={removeLabel}
            className="absolute right-0.5 top-0.5 flex size-5 items-center justify-center rounded-full bg-black/60 text-white hover:bg-black/80"
          >
            <X className="size-3" aria-hidden />
          </button>
        </li>
      ))}
    </ul>
  );
}

/** The photos of one message, inside its bubble: a tap opens the full picture in a new tab. */
export function MessagePhotos({ sources }: { sources: string[] }) {
  if (sources.length === 0) return null;
  const single = sources.length === 1;
  return (
    <span className={`mb-1.5 grid gap-1 ${single ? "grid-cols-1" : "grid-cols-2"}`}>
      {sources.map((src) => (
        <a key={src} href={src} target="_blank" rel="noopener noreferrer" className="block overflow-hidden rounded-lg bg-black/5">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={src}
            alt=""
            loading="lazy"
            className={`w-full object-cover ${single ? "max-h-72 min-h-24" : "aspect-square"}`}
          />
        </a>
      ))}
    </span>
  );
}

/** Posts the photos (and the words) of one message to one of the site's own routes; true when it went through. */
export async function postPhotos(url: string, files: File[], body: string): Promise<{ ok: boolean; status: number }> {
  const form = new FormData();
  if (body.trim()) form.set("body", body.trim());
  for (const file of files) form.append("files", file, file.name);
  try {
    const res = await fetch(url, { method: "POST", body: form });
    return { ok: res.ok, status: res.status };
  } catch {
    return { ok: false, status: 0 };
  }
}
