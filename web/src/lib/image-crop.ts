// The arithmetic behind the picture editor (a photo moved and zoomed inside a fixed frame, like the profile picture of a
// messenger). Kept free of the page so it can be tested.

export type Size = { w: number; h: number };

/** The biggest frame of the given shape (width / height) that fits in the stage, leaving a margin around it. */
export function fitFrame(stage: Size, aspect: number, margin = 16): Size {
  const maxW = Math.max(1, stage.w - margin * 2);
  const maxH = Math.max(1, stage.h - margin * 2);
  const w = Math.min(maxW, maxH * aspect);
  return { w, h: w / aspect };
}

/** The size of the picture after it was turned a number of quarter-turns. */
export function turned(size: Size, quarterTurns: number): Size {
  return quarterTurns % 2 === 0 ? size : { w: size.h, h: size.w };
}

/** How much the picture must be enlarged to just cover the frame (the smallest allowed size). */
export function coverScale(picture: Size, frame: Size): number {
  return Math.max(frame.w / picture.w, frame.h / picture.h);
}

/** Keeps the picture covering the frame: the offset (screen pixels from the centre) may not show an empty edge. */
export function clampOffset(offset: { x: number; y: number }, picture: Size, frame: Size, scale: number) {
  const maxX = Math.max(0, (picture.w * scale - frame.w) / 2);
  const maxY = Math.max(0, (picture.h * scale - frame.h) / 2);
  return {
    x: Math.min(maxX, Math.max(-maxX, offset.x)),
    y: Math.min(maxY, Math.max(-maxY, offset.y)),
  };
}

/** Zoom limits: 1 is "just covers the frame"; below 1 the whole picture fits inside it (with room round it); the top is
 * where the picture would be blown up too far to be sharp. */
export function zoomLimits(picture: Size, frame: Size, outWidth: number, allowFit = true) {
  const cover = coverScale(picture, frame);
  const contain = Math.min(frame.w / picture.w, frame.h / picture.h);
  // Past the point where one source pixel becomes more than ~2 output pixels the result only gets blurry.
  const sharp = (outWidth / frame.w) / 2;
  const max = Math.max(1.5, Math.min(8, sharp / cover));
  return { min: allowFit ? Math.min(1, contain / cover) : 1, max };
}

/** Does the picture, at this scale, leave part of the frame empty? */
export function leavesGap(picture: Size, frame: Size, scale: number): boolean {
  return picture.w * scale < frame.w - 0.5 || picture.h * scale < frame.h - 0.5;
}

/** The size of the finished picture: the wanted width, but never more than the photo can give (no blown-up pixels). */
export function outputSize(frame: Size, scale: number, outWidth: number, aspect: number): Size {
  const visible = frame.w / scale; // photo pixels across the frame
  const w = Math.max(64, Math.min(outWidth, Math.round(visible)));
  return { w, h: Math.max(1, Math.round(w / aspect)) };
}
