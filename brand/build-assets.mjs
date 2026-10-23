// Makes every picture of the brand from the two original files in brand/source:
//   icon-1024.png   the square icon (the price tag with the R)
//   logo-full.webp  the whole logo: the icon, "rentox" and "KİRAYƏ ELANLARI" on white
//
// Run from the web folder (it uses the image library installed there):
//   cd web && node ../brand/build-assets.mjs
//
// It writes: web/public/brand/* (the logo for light and dark pages, the icons), web/src/app/favicon.ico is NOT written
// (the site serves /favicon.ico itself, so the owner can change it in the admin panel), mobile/assets/brand/*.
import { createRequire } from "node:module";
import { mkdir, writeFile, copyFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import path from "node:path";

const require = createRequire(path.resolve(process.cwd(), "package.json"));
const sharp = require("sharp");

const here = path.dirname(fileURLToPath(import.meta.url));
const source = (name) => path.join(here, "source", name);
const web = path.join(here, "..", "web", "public", "brand");
const mobile = path.join(here, "..", "mobile", "assets", "brand");

await mkdir(web, { recursive: true });
await mkdir(mobile, { recursive: true });

// ------------------------------------------------------------------------------------------------ the full logo
// The original has a white background. Every pixel is one of three inks (navy, brand blue, grey) mixed with white,
// so: find which ink explains the pixel and how much of it there is; that amount is the transparency. This keeps the
// exact brand colours, which a plain "remove white" would shift.
const { data, info } = await sharp(source("logo-full.webp")).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const { width: W, height: H } = info;

const seeds = { navy: [12, 30, 76], blue: [30, 92, 254], grey: [76, 92, 120] };
function refine(seed) {
  let sum = [0, 0, 0];
  let n = 0;
  for (let i = 0; i < data.length; i += 3) {
    const d = Math.abs(data[i] - seed[0]) + Math.abs(data[i + 1] - seed[1]) + Math.abs(data[i + 2] - seed[2]);
    if (d < 14) {
      sum[0] += data[i];
      sum[1] += data[i + 1];
      sum[2] += data[i + 2];
      n++;
    }
  }
  return n > 50 ? sum.map((v) => Math.round(v / n)) : seed;
}
const inks = Object.fromEntries(Object.entries(seeds).map(([k, v]) => [k, refine(v)]));
console.log("inks", inks);

const lightInk = { navy: inks.navy, blue: inks.blue, grey: inks.grey };
const darkInk = { navy: [241, 240, 255], blue: inks.blue, grey: [169, 166, 207] };

function render(map) {
  const out = Buffer.alloc(W * H * 4);
  const names = Object.keys(inks);
  for (let p = 0, o = 0; p < data.length; p += 3, o += 4) {
    const r = 255 - data[p];
    const g = 255 - data[p + 1];
    const b = 255 - data[p + 2];
    if (r + g + b < 6) continue; // white: stays transparent
    let best = null;
    for (const name of names) {
      const k = inks[name];
      const kr = 255 - k[0];
      const kg = 255 - k[1];
      const kb = 255 - k[2];
      const a = Math.min(1, Math.max(0, (r * kr + g * kg + b * kb) / (kr * kr + kg * kg + kb * kb)));
      const er = r - a * kr;
      const eg = g - a * kg;
      const eb = b - a * kb;
      const err = er * er + eg * eg + eb * eb;
      if (!best || err < best.err) best = { name, a, err };
    }
    const c = map[best.name];
    out[o] = c[0];
    out[o + 1] = c[1];
    out[o + 2] = c[2];
    out[o + 3] = Math.round(best.a * 255);
  }
  return out;
}

async function logo(map, file, mobileFile) {
  const full = sharp(render(map), { raw: { width: W, height: H, channels: 4 } });
  const trimmed = await full.png().toBuffer().then((buf) => sharp(buf).trim({ background: { r: 0, g: 0, b: 0, alpha: 0 }, threshold: 1 }).toBuffer());
  const resized = await sharp(trimmed).resize({ width: 960 }).png({ compressionLevel: 9 }).toBuffer();
  await writeFile(path.join(web, file), resized);
  await writeFile(path.join(mobile, mobileFile), resized);
  const meta = await sharp(resized).metadata();
  console.log(file, meta.width, "x", meta.height);
}

await logo(lightInk, "logo.png", "logo.png");
await logo(darkInk, "logo-dark.png", "logo_dark.png");

// ------------------------------------------------------------------------------------------------ the icon
const icon = sharp(source("icon-1024.png")).removeAlpha();
const png = (size) => icon.clone().resize(size, size).png({ compressionLevel: 9 }).toBuffer();

await writeFile(path.join(web, "icon.png"), await png(512));
await writeFile(path.join(web, "icon-192.png"), await png(192));
await writeFile(path.join(web, "apple-touch-icon.png"), await png(180));
await copyFile(source("icon-1024.png"), path.join(mobile, "app_icon.png"));

// favicon.ico: an ICO file can hold PNG pictures as they are.
const sizes = [16, 32, 48];
const pics = await Promise.all(sizes.map(png));
const head = Buffer.alloc(6 + 16 * sizes.length);
head.writeUInt16LE(0, 0);
head.writeUInt16LE(1, 2);
head.writeUInt16LE(sizes.length, 4);
let offset = head.length;
sizes.forEach((s, i) => {
  const e = 6 + 16 * i;
  head[e] = s;
  head[e + 1] = s;
  head.writeUInt16LE(1, e + 4);
  head.writeUInt16LE(32, e + 6);
  head.writeUInt32LE(pics[i].length, e + 8);
  head.writeUInt32LE(offset, e + 12);
  offset += pics[i].length;
});
await writeFile(path.join(web, "favicon.ico"), Buffer.concat([head, ...pics]));
console.log("icons written");
