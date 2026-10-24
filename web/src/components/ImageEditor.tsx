"use client";

import { useT } from "@/lib/i18n/client";
import { useCallback, useEffect, useRef, useState } from "react";
import { FlipHorizontal2, Grid3x3, Loader2, Maximize2, Minimize2, Minus, Move, Plus, RotateCcw, RotateCw, X } from "lucide-react";
import { clampOffset, coverScale, fitFrame, leavesGap, outputSize, turned, zoomLimits, type Size } from "@/lib/image-crop";

export type EditorOptions = {
  /** Width / height of the finished picture (1 for a profile picture, 3 for a banner). */
  aspect: number;
  /** The frame is a circle (the finished picture is still square-cornered: it is shown round by the page). */
  round?: boolean;
  /** The width of the finished picture in pixels (never more than the photo can give). */
  outWidth: number;
  title: string;
  hint?: string;
  /** Narrower shapes the picture may also be shown in (a phone): drawn as dashed lines inside the frame. */
  guides?: { label: string; aspect: number }[];
  /** Small live previews of how the picture will look where it is used. */
  previews?: { label: string; aspect: number; width: number; round?: boolean }[];
  /** More tools for the admin: grid, mirror, colour settings, a choice of the background. */
  pro?: boolean;
};

type Background = "transparent" | "white" | "black" | "color" | "blur";
type Look = { brightness: number; contrast: number; saturation: number };
const PLAIN: Look = { brightness: 100, contrast: 100, saturation: 100 };

const MAX_BYTES = 4_800_000;
/** The blurred copy is a little bigger than needed, so its soft edges stay outside the frame. */
const BLUR_OVERSCAN = 1.12;

const cssFilter = (l: Look) => `brightness(${l.brightness}%) contrast(${l.contrast}%) saturate(${l.saturation}%)`;
const isPlain = (l: Look) => l.brightness === 100 && l.contrast === 100 && l.saturation === 100;

const CHECKER =
  "conic-gradient(#d9d9e3 25%, #f4f4f8 0 50%, #d9d9e3 0 75%, #f4f4f8 0) 0 0 / 16px 16px";

/** Opens the editor for one photo: `edit(file, options)` answers with the finished file, or null if it was cancelled. */
export function useImageEditor() {
  const [state, setState] = useState<{ file: File; options: EditorOptions; resolve: (f: File | null) => void } | null>(null);

  const edit = useCallback(
    (file: File, options: EditorOptions) =>
      new Promise<File | null>((resolve) => {
        setState({ file, options, resolve });
      }),
    [],
  );

  const close = (result: File | null) => {
    state?.resolve(result);
    setState(null);
  };

  const dialog = state ? <ImageEditor key={state.file.name + state.file.size + state.file.lastModified} file={state.file} options={state.options} onDone={close} /> : null;
  return { edit, dialog };
}

/** The picture that is already there (a profile photo, a store logo, a banner), as a file the editor can open again. */
export async function fetchPicture(url: string, name: string): Promise<File | null> {
  try {
    const res = await fetch(url, { cache: "no-store", credentials: "same-origin" });
    if (!res.ok) return null;
    const blob = await res.blob();
    if (!blob.type.startsWith("image/")) return null;
    return new File([blob], name, { type: blob.type });
  } catch {
    return null;
  }
}

/** What fills the part of the frame the picture does not cover: drawn the same way in the editor, the previews and the file. */
function Backdrop({
  bg,
  color,
  url,
  natural,
  box,
  rot,
  flip,
  coverK,
  look,
}: {
  bg: Background;
  color: string;
  url: string;
  natural: Size;
  box: Size;
  rot: number;
  flip: boolean;
  /** The scale at which the photo just covers the box (blurred copy). */
  coverK: number;
  look: Look;
}) {
  const style: React.CSSProperties = {
    position: "absolute",
    left: "50%",
    top: "50%",
    width: box.w,
    height: box.h,
    transform: "translate(-50%, -50%)",
    overflow: "hidden",
  };
  if (bg === "transparent") style.background = CHECKER;
  else if (bg === "white") style.background = "#ffffff";
  else if (bg === "black") style.background = "#000000";
  else if (bg === "color") style.background = color;

  return (
    <div style={style} aria-hidden>
      {bg === "blur" && (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={url}
          alt=""
          draggable={false}
          style={{
            position: "absolute",
            left: "50%",
            top: "50%",
            width: natural.w,
            height: natural.h,
            maxWidth: "none",
            transform: `translate(-50%, -50%) rotate(${rot * 90}deg) scale(${(flip ? -1 : 1) * coverK * BLUR_OVERSCAN}, ${coverK * BLUR_OVERSCAN})`,
            filter: `blur(${Math.max(6, box.w / 28)}px) ${cssFilter(look)}`,
          }}
        />
      )}
    </div>
  );
}

function ImageEditor({ file, options, onDone }: { file: File; options: EditorOptions; onDone: (f: File | null) => void }) {
  const t = useT();
  const { aspect, round = false, outWidth, guides = [], previews = [], pro = false } = options;
  const dialogRef = useRef<HTMLDialogElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);

  const [photo, setPhoto] = useState<{ el: HTMLImageElement; url: string } | null>(null);
  const [failed, setFailed] = useState(false);
  const [stage, setStage] = useState<Size>({ w: 480, h: 340 });
  const [rot, setRot] = useState(0);
  const [flip, setFlip] = useState(false);
  const [zoom, setZoom] = useState(1);
  const [off, setOff] = useState({ x: 0, y: 0 });
  const [grid, setGrid] = useState(false);
  const [busy, setBusy] = useState(false);
  const [bg, setBg] = useState<Background>("transparent");
  const [color, setColor] = useState("#ffffff");
  const [look, setLook] = useState<Look>(PLAIN);

  // The photo.
  useEffect(() => {
    const url = URL.createObjectURL(file);
    const el = new Image();
    // A load that was given up on (the window closed, or React ran this twice) must not report anything.
    let current = true;
    el.onload = () => current && setPhoto({ el, url });
    el.onerror = () => current && setFailed(true);
    el.src = url;
    return () => {
      current = false;
      URL.revokeObjectURL(url);
    };
  }, [file]);

  // The window.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
  }, []);

  // The size of the stage (the frame is made to fit in it).
  useEffect(() => {
    const el = stageRef.current;
    if (!el) return;
    const measure = () => setStage({ w: el.clientWidth, h: el.clientHeight });
    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const natural: Size = photo ? { w: photo.el.naturalWidth, h: photo.el.naturalHeight } : { w: 1, h: 1 };
  const pic = turned(natural, rot);
  const frame = fitFrame(stage, aspect);
  const cover = coverScale(pic, frame);
  const limits = zoomLimits(pic, frame, outWidth);
  const z = Math.min(limits.max, Math.max(limits.min, zoom));
  const scale = cover * z;
  const pos = clampOffset(off, pic, frame, scale);
  const gap = leavesGap(pic, frame, scale);

  // The newest numbers for the handlers that run between two renders.
  const geo = useRef({ pic, frame, scale, cover, limits, pos, z });
  useEffect(() => {
    geo.current = { pic, frame, scale, cover, limits, pos, z };
  });

  const setZoomTo = useCallback((next: number) => {
    const g = geo.current;
    const value = Math.min(g.limits.max, Math.max(g.limits.min, next));
    setZoom(value);
    setOff((o) => clampOffset(o, g.pic, g.frame, g.cover * value));
  }, []);

  // Dragging with one finger or the mouse, pinching with two.
  const pointers = useRef(new Map<number, { x: number; y: number }>());
  const pinch = useRef<{ d: number; z: number } | null>(null);

  function onPointerDown(e: React.PointerEvent<HTMLDivElement>) {
    e.currentTarget.setPointerCapture(e.pointerId);
    pointers.current.set(e.pointerId, { x: e.clientX, y: e.clientY });
    if (pointers.current.size === 2) {
      const [a, b] = [...pointers.current.values()];
      pinch.current = { d: Math.hypot(a.x - b.x, a.y - b.y), z: geo.current.z };
    }
  }

  function onPointerMove(e: React.PointerEvent<HTMLDivElement>) {
    const last = pointers.current.get(e.pointerId);
    if (!last) return;
    const now = { x: e.clientX, y: e.clientY };
    pointers.current.set(e.pointerId, now);

    if (pointers.current.size >= 2 && pinch.current) {
      const [a, b] = [...pointers.current.values()];
      const d = Math.hypot(a.x - b.x, a.y - b.y);
      if (pinch.current.d > 0) setZoomTo(pinch.current.z * (d / pinch.current.d));
      return;
    }
    // Added to the newest offset (not the one of the last render): fast moves must not lose each other.
    const dx = now.x - last.x;
    const dy = now.y - last.y;
    setOff((o) => {
      const g = geo.current;
      return clampOffset({ x: o.x + dx, y: o.y + dy }, g.pic, g.frame, g.scale);
    });
  }

  function onPointerUp(e: React.PointerEvent<HTMLDivElement>) {
    pointers.current.delete(e.pointerId);
    if (pointers.current.size < 2) pinch.current = null;
  }

  // The mouse wheel zooms (needs a listener that may stop the page from scrolling).
  useEffect(() => {
    const el = stageRef.current;
    if (!el) return;
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      setZoomTo(geo.current.z * Math.exp(-e.deltaY * 0.0015));
    };
    el.addEventListener("wheel", onWheel, { passive: false });
    return () => el.removeEventListener("wheel", onWheel);
  }, [setZoomTo]);

  function onKeyDown(e: React.KeyboardEvent<HTMLDivElement>) {
    const g = geo.current;
    const step = 12;
    const move = (x: number, y: number) => {
      e.preventDefault();
      setOff((o) => clampOffset({ x: o.x + x, y: o.y + y }, g.pic, g.frame, g.scale));
    };
    if (e.key === "ArrowLeft") move(-step, 0);
    else if (e.key === "ArrowRight") move(step, 0);
    else if (e.key === "ArrowUp") move(0, -step);
    else if (e.key === "ArrowDown") move(0, step);
    else if (e.key === "+" || e.key === "=") setZoomTo(g.z * 1.1);
    else if (e.key === "-") setZoomTo(g.z / 1.1);
  }

  function turn(by: number) {
    setRot((r) => (r + by + 4) % 4);
    setOff({ x: 0, y: 0 });
  }

  function reset() {
    setRot(0);
    setFlip(false);
    setZoom(1);
    setOff({ x: 0, y: 0 });
    setBg("transparent");
    setLook(PLAIN);
  }

  async function apply() {
    if (!photo || busy) return;
    setBusy(true);
    try {
      const out = outputSize(frame, scale, outWidth, aspect);
      const k = out.w / frame.w;
      const canFilter = "filter" in document.createElement("canvas").getContext("2d")!;
      const adjust = canFilter && !isPlain(look) ? cssFilter(look) : "none";

      // `underlay` is what the picture is drawn on when the file cannot keep transparency.
      const draw = (canvas: HTMLCanvasElement, underlay: string | null) => {
        canvas.width = out.w;
        canvas.height = out.h;
        const ctx = canvas.getContext("2d");
        if (!ctx) throw new Error("no canvas");
        ctx.imageSmoothingQuality = "high";
        const back = bg === "white" ? "#ffffff" : bg === "black" ? "#000000" : bg === "color" ? color : underlay;
        if (back && bg !== "blur") {
          ctx.fillStyle = back;
          ctx.fillRect(0, 0, out.w, out.h);
        }
        if (gap && bg === "blur") {
          ctx.save();
          ctx.fillStyle = underlay ?? "#1d1d2b";
          if (underlay || !canFilter) ctx.fillRect(0, 0, out.w, out.h);
          if (canFilter) ctx.filter = `blur(${Math.max(6, out.w / 28)}px) ${adjust === "none" ? "" : adjust}`;
          ctx.translate(out.w / 2, out.h / 2);
          ctx.rotate((rot * Math.PI) / 2);
          const c = cover * k * BLUR_OVERSCAN;
          ctx.scale(c * (flip ? -1 : 1), c);
          ctx.drawImage(photo.el, -natural.w / 2, -natural.h / 2);
          ctx.restore();
        }
        ctx.save();
        if (adjust !== "none") ctx.filter = adjust;
        ctx.translate(out.w / 2 + pos.x * k, out.h / 2 + pos.y * k);
        ctx.rotate((rot * Math.PI) / 2);
        ctx.scale(scale * k * (flip ? -1 : 1), scale * k);
        ctx.drawImage(photo.el, -natural.w / 2, -natural.h / 2);
        ctx.restore();
      };
      const toBlob = (canvas: HTMLCanvasElement, type: string, q: number) =>
        new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, type, q));

      // Transparency is only needed when there is an empty part and "transparent" was chosen.
      const needsAlpha = gap && bg === "transparent";
      const canvas = document.createElement("canvas");
      draw(canvas, null);
      let type = "image/webp";
      let quality = 0.92;
      let blob = await toBlob(canvas, type, quality);
      if (!blob || blob.type !== "image/webp") {
        // This browser cannot make WebP: PNG keeps the transparency, otherwise JPEG (on white) is smaller.
        type = needsAlpha ? "image/png" : "image/jpeg";
        draw(canvas, type === "image/jpeg" ? "#ffffff" : null);
        blob = await toBlob(canvas, type, quality);
        if (type === "image/png" && blob && blob.size > MAX_BYTES) {
          type = "image/jpeg";
          draw(canvas, "#ffffff");
          blob = await toBlob(canvas, type, quality);
        }
      }
      while (blob && blob.size > MAX_BYTES && quality > 0.5 && type !== "image/png") {
        quality -= 0.1;
        blob = await toBlob(canvas, type, quality);
      }
      if (!blob) throw new Error("no picture");
      const base = file.name.replace(/\.[^.]+$/, "") || "photo";
      const ext = type === "image/webp" ? "webp" : type === "image/png" ? "png" : "jpg";
      onDone(new File([blob], `${base}.${ext}`, { type }));
    } catch {
      setFailed(true);
      setBusy(false);
    }
  }

  const transform = (k: number) =>
    `translate(-50%, -50%) translate(${pos.x * k}px, ${pos.y * k}px) rotate(${rot * 90}deg) scale(${(flip ? -1 : 1) * scale * k}, ${scale * k})`;
  const photoFilter = isPlain(look) ? undefined : cssFilter(look);

  const button =
    "flex size-9 items-center justify-center rounded-lg border border-line bg-surface text-ink-2 hover:border-primary hover:text-primary disabled:opacity-50";
  const wide =
    "flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary disabled:opacity-50";
  const percent = Math.round(z * 100);

  const previewBoxes = previews.map((p) => {
    const boxH = p.width / p.aspect;
    // The finished picture is shown "covering" the box: the side that is too long is cut off.
    const w = Math.max(p.width, boxH * aspect);
    return { ...p, boxH, k: w / frame.w, w, h: w / aspect };
  });

  const backgrounds: { id: Background; label: string }[] = [
    { id: "transparent", label: t("Şəffaf") },
    { id: "white", label: t("Ağ") },
    { id: "black", label: t("Qara") },
    { id: "color", label: t("Rəng") },
    { id: "blur", label: t("Bulanıq") },
  ];
  // The background only matters when there is an empty part; a plain user sees the choice only then.
  const showBackground = pro || gap;

  return (
    <dialog
      ref={dialogRef}
      onClose={() => onDone(null)}
      className="m-auto max-h-[96vh] w-[min(96vw,720px)] overflow-y-auto rounded-2xl border border-line bg-surface p-0 text-ink backdrop:bg-black/70"
    >
      <div className="flex items-start gap-3 border-b border-line px-5 py-4">
        <div className="min-w-0 flex-1">
          <h2 className="text-lg font-extrabold">{options.title}</h2>
          {options.hint && <p className="mt-0.5 text-sm text-ink-2">{options.hint}</p>}
        </div>
        <button type="button" aria-label={t("Ləğv et")} onClick={() => dialogRef.current?.close()} className={button}>
          <X className="size-5" aria-hidden />
        </button>
      </div>

      <div
        ref={stageRef}
        tabIndex={0}
        role="application"
        aria-label={options.title}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerCancel={onPointerUp}
        onKeyDown={onKeyDown}
        className="relative h-[min(46vh,380px)] cursor-grab touch-none select-none overflow-hidden bg-[#0d0c1c] outline-none active:cursor-grabbing"
      >
        {photo && gap && (
          <div className={`absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 ${round ? "overflow-hidden rounded-full" : ""}`} style={{ width: frame.w, height: frame.h }}>
            <Backdrop bg={bg} color={color} url={photo.url} natural={natural} box={frame} rot={rot} flip={flip} coverK={cover} look={look} />
          </div>
        )}
        {photo && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={photo.url}
            alt=""
            draggable={false}
            style={{
              position: "absolute",
              left: "50%",
              top: "50%",
              width: natural.w,
              height: natural.h,
              maxWidth: "none",
              transform: transform(1),
              willChange: "transform",
              filter: photoFilter,
            }}
          />
        )}
        {!photo && !failed && (
          <div className="absolute inset-0 flex items-center justify-center text-white/70">
            <Loader2 className="size-7 animate-spin" aria-hidden />
          </div>
        )}
        {failed && (
          <p role="alert" className="absolute inset-0 flex items-center justify-center px-6 text-center font-semibold text-white">
            {t("Şəkil açılmadı. Başqa şəkil seçin.")}
          </p>
        )}

        {/* The frame: what is inside it is what stays. */}
        <div
          className={`pointer-events-none absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 border-2 border-white/90 ${round ? "rounded-full" : "rounded-sm"}`}
          style={{ width: frame.w, height: frame.h, boxShadow: "0 0 0 9999px rgba(8,7,20,0.62)" }}
        >
          {grid && (
            <div
              className="absolute inset-0"
              style={{
                backgroundImage:
                  "linear-gradient(to right, transparent calc(33.33% - .5px), rgba(255,255,255,.45) calc(33.33% - .5px) calc(33.33% + .5px), transparent calc(33.33% + .5px) calc(66.66% - .5px), rgba(255,255,255,.45) calc(66.66% - .5px) calc(66.66% + .5px), transparent calc(66.66% + .5px)), linear-gradient(to bottom, transparent calc(33.33% - .5px), rgba(255,255,255,.45) calc(33.33% - .5px) calc(33.33% + .5px), transparent calc(33.33% + .5px) calc(66.66% - .5px), rgba(255,255,255,.45) calc(66.66% - .5px) calc(66.66% + .5px), transparent calc(66.66% + .5px))",
              }}
            />
          )}
          {guides.map((g) => {
            const gw = Math.min(frame.w, frame.h * g.aspect);
            return (
              <div
                key={g.label}
                className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 border border-dashed border-amber-300/90"
                style={{ width: gw, height: gw / g.aspect }}
              >
                <span className="absolute left-1 top-0.5 rounded bg-black/55 px-1 text-[10px] font-bold text-amber-200">{g.label}</span>
              </div>
            );
          })}
        </div>

        <span className="pointer-events-none absolute bottom-2 left-1/2 flex -translate-x-1/2 items-center gap-1.5 rounded-full bg-black/55 px-3 py-1 text-xs font-semibold text-white/90">
          <Move className="size-3.5" aria-hidden />
          {t("Sürüşdürərək yerləşdirin, iki barmaqla və ya təkərlə böyüdün")}
        </span>
      </div>

      <div className="space-y-3 px-5 py-4">
        <div className="flex items-center gap-2">
          <button type="button" aria-label={t("Kiçilt")} onClick={() => setZoomTo(z / 1.15)} disabled={!photo} className={button}>
            <Minus className="size-4" aria-hidden />
          </button>
          <input
            type="range"
            aria-label={t("Yaxınlaşdırma")}
            min={limits.min}
            max={limits.max}
            step={0.01}
            value={z}
            disabled={!photo}
            onChange={(e) => setZoomTo(Number(e.target.value))}
            className="h-2 min-w-0 flex-1 accent-[var(--color-primary)]"
          />
          <button type="button" aria-label={t("Böyüt")} onClick={() => setZoomTo(z * 1.15)} disabled={!photo} className={button}>
            <Plus className="size-4" aria-hidden />
          </button>
          <span className="w-12 text-right text-sm font-bold tabular-nums text-ink-2">{percent}%</span>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button type="button" aria-label={t("Sola fırlat")} title={t("Sola fırlat")} onClick={() => turn(-1)} disabled={!photo} className={button}>
            <RotateCcw className="size-4" aria-hidden />
          </button>
          <button type="button" aria-label={t("Sağa fırlat")} title={t("Sağa fırlat")} onClick={() => turn(1)} disabled={!photo} className={button}>
            <RotateCw className="size-4" aria-hidden />
          </button>
          {pro && (
            <>
              <button
                type="button"
                aria-label={t("Güzgü")}
                title={t("Güzgü")}
                aria-pressed={flip}
                onClick={() => setFlip((f) => !f)}
                disabled={!photo}
                className={`${button} ${flip ? "!border-primary !text-primary" : ""}`}
              >
                <FlipHorizontal2 className="size-4" aria-hidden />
              </button>
              <button
                type="button"
                aria-label={t("Şəbəkə")}
                title={t("Şəbəkə")}
                aria-pressed={grid}
                onClick={() => setGrid((g) => !g)}
                className={`${button} ${grid ? "!border-primary !text-primary" : ""}`}
              >
                <Grid3x3 className="size-4" aria-hidden />
              </button>
            </>
          )}
          <button type="button" onClick={() => setZoomTo(limits.min)} disabled={!photo} className={wide} title={t("Bütün şəkil çərçivəyə sığsın")}>
            <Minimize2 className="size-4" aria-hidden />
            {t("Sığdır")}
          </button>
          <button type="button" onClick={() => setZoomTo(1)} disabled={!photo} className={wide} title={t("Şəkil çərçivəni tam doldursun")}>
            <Maximize2 className="size-4" aria-hidden />
            {t("Doldur")}
          </button>
          <button type="button" onClick={() => setOff({ x: 0, y: 0 })} disabled={!photo} className={wide}>
            {t("Mərkəzə")}
          </button>
          <button type="button" onClick={reset} disabled={!photo} className={wide}>
            {t("Sıfırla")}
          </button>
          {pro && photo && (
            <span className="ml-auto text-xs text-ink-2">
              {natural.w}×{natural.h} px
            </span>
          )}
        </div>

        {showBackground && (
          <div className="space-y-1.5 border-t border-line pt-3">
            <p className="text-xs font-bold text-ink-2">{t("Boş yerin fonu")}</p>
            <div className="flex flex-wrap items-center gap-1.5">
              {backgrounds.map((b) => (
                <button
                  key={b.id}
                  type="button"
                  aria-pressed={bg === b.id}
                  onClick={() => setBg(b.id)}
                  className={`h-8 rounded-md border px-3 text-xs font-bold ${bg === b.id ? "border-primary bg-primary-soft text-primary" : "border-line text-ink-2 hover:border-primary"}`}
                >
                  {b.label}
                </button>
              ))}
              {bg === "color" && (
                <input
                  type="color"
                  aria-label={t("Rəng")}
                  value={color}
                  onChange={(e) => setColor(e.target.value)}
                  className="h-8 w-10 cursor-pointer rounded-md border border-line bg-surface p-0.5"
                />
              )}
            </div>
            {!gap && <p className="text-xs text-ink-2">{t("Fon yalnız şəkil çərçivəni tam doldurmayanda görünür (kiçildəndə).")}</p>}
          </div>
        )}

        {pro && (
          <div className="grid gap-x-4 gap-y-2 border-t border-line pt-3 sm:grid-cols-3">
            {(
              [
                ["brightness", t("Parlaqlıq")],
                ["contrast", t("Kontrast")],
                ["saturation", t("Doyğunluq")],
              ] as const
            ).map(([key, label]) => (
              <label key={key} className="block text-xs font-bold text-ink-2">
                <span className="flex justify-between">
                  {label}
                  <span className="tabular-nums">{look[key]}%</span>
                </span>
                <input
                  type="range"
                  min={50}
                  max={150}
                  step={1}
                  value={look[key]}
                  disabled={!photo}
                  onChange={(e) => setLook((l) => ({ ...l, [key]: Number(e.target.value) }))}
                  className="mt-1 h-2 w-full accent-[var(--color-primary)]"
                />
              </label>
            ))}
          </div>
        )}

        {previewBoxes.length > 0 && (
          <div className="flex flex-wrap items-end gap-4 border-t border-line pt-3">
            {previewBoxes.map((p) => (
              <div key={p.label} className="text-xs font-semibold text-ink-2">
                <div
                  className={`relative overflow-hidden bg-[#0d0c1c] ${p.round ? "rounded-full" : "rounded-md"}`}
                  style={{ width: p.width, height: p.boxH }}
                >
                  {photo && (
                    <div className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2" style={{ width: p.w, height: p.h }}>
                      {gap && (
                        <Backdrop bg={bg} color={color} url={photo.url} natural={natural} box={{ w: p.w, h: p.h }} rot={rot} flip={flip} coverK={cover * p.k} look={look} />
                      )}
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={photo.url}
                        alt=""
                        draggable={false}
                        style={{
                          position: "absolute",
                          left: "50%",
                          top: "50%",
                          width: natural.w,
                          height: natural.h,
                          maxWidth: "none",
                          transform: transform(p.k),
                          filter: photoFilter,
                        }}
                      />
                    </div>
                  )}
                </div>
                <span className="mt-1 block">{p.label}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="flex gap-2 border-t border-line px-5 py-4">
        <button
          type="button"
          onClick={() => dialogRef.current?.close()}
          disabled={busy}
          className="h-11 flex-1 rounded-xl border border-line font-bold hover:border-primary disabled:opacity-60"
        >
          {t("Ləğv et")}
        </button>
        <button
          type="button"
          onClick={() => void apply()}
          disabled={!photo || busy}
          className="flex h-11 flex-1 items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-60"
        >
          {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
          {t("Hazırdır")}
        </button>
      </div>
    </dialog>
  );
}
