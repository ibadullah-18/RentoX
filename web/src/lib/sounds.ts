"use client";

// Two sounds made with the browser's own synthesiser (no files to load): a longer chime for something new that
// needs attention (a report, a support request) and a short blip for a message. The browser keeps sound locked
// until the person has clicked or typed on the page once, so the first touch unlocks it.

let context: AudioContext | null = null;

function audio(): AudioContext | null {
  if (typeof window === "undefined") return null;
  if (!context) {
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    try {
      context = new Ctor();
    } catch {
      return null;
    }
  }
  return context;
}

/** Call once; the first click or key press on the page unlocks sound. */
export function unlockSound(): () => void {
  const unlock = () => {
    const ctx = audio();
    if (ctx && ctx.state === "suspended") void ctx.resume().catch(() => {});
  };
  const events = ["pointerdown", "keydown", "touchstart"] as const;
  events.forEach((e) => window.addEventListener(e, unlock, { passive: true }));
  return () => events.forEach((e) => window.removeEventListener(e, unlock));
}

type Note = { at: number; hz: number; length: number; gain: number };

function play(notes: Note[]) {
  const ctx = audio();
  if (!ctx || ctx.state !== "running") return;
  const start = ctx.currentTime + 0.02;
  for (const n of notes) {
    const osc = ctx.createOscillator();
    const amp = ctx.createGain();
    osc.type = "sine";
    osc.frequency.value = n.hz;
    const from = start + n.at;
    amp.gain.setValueAtTime(0.0001, from);
    amp.gain.exponentialRampToValueAtTime(n.gain, from + 0.02);
    amp.gain.exponentialRampToValueAtTime(0.0001, from + n.length);
    osc.connect(amp).connect(ctx.destination);
    osc.start(from);
    osc.stop(from + n.length + 0.05);
  }
}

/** About 3.5 seconds: a rising chime, played twice, fading out. */
export function playNotificationSound() {
  const phrase = [659.25, 783.99, 987.77, 1318.51]; // E5 G5 B5 E6
  const notes: Note[] = [];
  [0, 1.7].forEach((offset, round) => {
    phrase.forEach((hz, i) => {
      notes.push({ at: offset + i * 0.22, hz, length: 0.9, gain: round === 0 ? 0.22 : 0.14 });
    });
    notes.push({ at: offset + 0.88, hz: phrase[0] * 2, length: 0.8, gain: round === 0 ? 0.1 : 0.06 });
  });
  play(notes);
}

/** A short double blip for a new message. */
export function playMessageSound() {
  play([
    { at: 0, hz: 880, length: 0.16, gain: 0.2 },
    { at: 0.13, hz: 1174.66, length: 0.24, gain: 0.2 },
  ]);
}
