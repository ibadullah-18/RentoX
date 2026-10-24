// Two demo campaign (home page banner) pictures, 1500x500 (3:1), made from SVG with the "sharp" library of the web project.
// The left third is kept calm on purpose: the banner's title and subtitle are written over it by the site.
// Run:  cd web && node ../tools/demo-listings/campaigns/build.mjs
import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const sharp = createRequire(join(here, "../../../web/package.json"))("sharp");

// A price tag like the one in the logo (rounded corners, a hole, a small string).
const tag = (x, y, s, fill, rot, hole = "#ffffff") => `
  <g transform="translate(${x} ${y}) rotate(${rot}) scale(${s})">
    <path d="M-60 -34 H18 L62 0 L18 34 H-60 Q-76 34 -76 18 V-18 Q-76 -34 -60 -34Z" fill="${fill}"/>
    <circle cx="-48" cy="0" r="9" fill="${hole}"/>
    <path d="M-48 0 C-20 -40 20 -40 40 -14" fill="none" stroke="${hole}" stroke-width="3" opacity=".0"/>
  </g>`;

const car = (x, y, s, body, glass) => `
  <g transform="translate(${x} ${y}) scale(${s})">
    <ellipse cx="190" cy="118" rx="210" ry="14" fill="#0b1d5c" opacity=".28"/>
    <path d="M10 96 L10 70 Q10 56 28 52 L96 40 L134 8 Q142 0 156 0 H252 Q268 0 280 10 L330 44 L372 52 Q390 56 390 74 V96 Q390 106 378 106 H12 Q10 106 10 96Z" fill="${body}"/>
    <path d="M110 40 L142 12 H196 V44Z" fill="${glass}"/>
    <path d="M206 44 V12 H250 Q258 12 264 18 L304 44Z" fill="${glass}"/>
    <rect x="26" y="70" width="40" height="8" rx="4" fill="#ffd36b"/>
    <rect x="338" y="68" width="40" height="8" rx="4" fill="#ff6b6b"/>
    <circle cx="100" cy="106" r="30" fill="#14151c"/><circle cx="100" cy="106" r="15" fill="#d3d8e2"/>
    <circle cx="298" cy="106" r="30" fill="#14151c"/><circle cx="298" cy="106" r="15" fill="#d3d8e2"/>
  </g>`;

const house = (x, y, s, wall, roof, door) => `
  <g transform="translate(${x} ${y}) scale(${s})">
    <ellipse cx="130" cy="6" rx="150" ry="10" fill="#0b1d5c" opacity=".22"/>
    <rect x="20" y="-130" width="220" height="130" fill="${wall}"/>
    <polygon points="0,-130 130,-220 260,-130" fill="${roof}"/>
    <rect x="110" y="-82" width="44" height="82" rx="4" fill="${door}"/>
    <rect x="42" y="-108" width="48" height="44" rx="4" fill="#bfe0f5"/><rect x="42" y="-88" width="48" height="3" fill="${wall}"/>
    <rect x="170" y="-108" width="48" height="44" rx="4" fill="#bfe0f5"/><rect x="170" y="-88" width="48" height="3" fill="${wall}"/>
    <rect x="186" y="-196" width="22" height="46" fill="${roof}"/>
  </g>`;

const bike = (x, y, s, color) => `
  <g transform="translate(${x} ${y}) scale(${s})" fill="none" stroke="${color}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round">
    <circle cx="40" cy="70" r="38"/><circle cx="190" cy="70" r="38"/>
    <path d="M40 70 L90 8 H146 L190 70 M90 8 L120 70 L40 70 M120 70 L146 8 M84 -6 H108 M146 8 L156 -14 H176"/>
    <circle cx="120" cy="70" r="6" fill="${color}"/>
  </g>`;

const key = (x, y, s, color) => `
  <g transform="translate(${x} ${y}) scale(${s})" fill="none" stroke="${color}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round">
    <circle cx="0" cy="0" r="46"/><circle cx="0" cy="0" r="14" fill="${color}" stroke="none"/>
    <path d="M46 0 H250 M190 0 V44 M236 0 V32"/>
  </g>`;

const bubbles = (items) =>
  items.map(([x, y, r, o]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="#fff" opacity="${o}"/>`).join("");

// 1. Blue: a car, a house and a bike on a quiet street; the left part is empty for the text.
const bannerBlue = `
<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="500" viewBox="0 0 1500 500">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#0d3fd1"/><stop offset=".55" stop-color="#1d5bfe"/><stop offset="1" stop-color="#4f8bff"/>
    </linearGradient>
    <linearGradient id="road" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1b3a9c"/><stop offset="1" stop-color="#112868"/></linearGradient>
  </defs>
  <rect width="1500" height="500" fill="url(#bg)"/>
  ${bubbles([[1180, 90, 150, 0.08], [1320, 60, 84, 0.1], [860, 70, 60, 0.07], [1440, 330, 120, 0.07], [700, 400, 90, 0.05]])}
  <circle cx="1300" cy="110" r="44" fill="#fff6d0" opacity=".95"/>
  <polygon points="760,372 900,250 1020,330 1130,236 1300,372" fill="#2f6bff" opacity=".55"/>
  <polygon points="1000,372 1180,270 1340,372" fill="#2257e6" opacity=".6"/>
  <rect y="372" width="1500" height="128" fill="url(#road)"/>
  <rect y="372" width="1500" height="6" fill="#ffffff" opacity=".18"/>
  <g stroke="#fff" stroke-opacity=".35" stroke-width="5" stroke-dasharray="46 40"><line x1="0" y1="446" x2="1500" y2="446"/></g>
  ${house(880, 372, 1.15, "#f4efe6", "#e8743b", "#7a4a2c")}
  ${car(520, 300, 1.05, "#ff4d8d", "#cfeaff")}
  ${bike(1215, 300, 1.0, "#ffffff")}
  ${tag(1380, 190, 1.3, "#ffd36b", -14, "#1d5bfe")}
  ${tag(760, 120, 0.9, "#ffffff", 12, "#1d5bfe")}
</svg>`;

// 2. Purple to pink: keys and price tags floating over soft circles; calm on the left.
const bannerPink = `
<svg xmlns="http://www.w3.org/2000/svg" width="1500" height="500" viewBox="0 0 1500 500">
  <defs>
    <linearGradient id="bg2" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#3a2bd6"/><stop offset=".5" stop-color="#7a45e8"/><stop offset="1" stop-color="#ff4d8d"/>
    </linearGradient>
    <radialGradient id="spot" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#fff" stop-opacity=".35"/><stop offset="1" stop-color="#fff" stop-opacity="0"/></radialGradient>
  </defs>
  <rect width="1500" height="500" fill="url(#bg2)"/>
  <circle cx="1090" cy="250" r="300" fill="url(#spot)"/>
  ${bubbles([[1380, 70, 110, 0.1], [940, 430, 80, 0.09], [1460, 400, 130, 0.08], [760, 80, 50, 0.1], [1200, 450, 46, 0.12]])}
  <g opacity=".95">
    ${key(880, 250, 1.1, "#ffd36b")}
  </g>
  ${tag(1180, 120, 1.45, "#ffffff", -10, "#7a45e8")}
  ${tag(1330, 300, 1.15, "#ffd36b", 14, "#7a45e8")}
  ${tag(1235, 440, 0.8, "#ffffff", -4, "#7a45e8")}
  ${bike(985, 345, 0.6, "#ffffff")}
  <g fill="#fff" opacity=".9"><circle cx="1270" cy="60" r="4"/><circle cx="1410" cy="200" r="3"/><circle cx="800" cy="400" r="3"/><circle cx="1000" cy="60" r="3"/></g>
</svg>`;

await sharp(Buffer.from(bannerBlue)).png({ compressionLevel: 9 }).toFile(join(here, "campaign-1-blue.png"));
await sharp(Buffer.from(bannerPink)).png({ compressionLevel: 9 }).toFile(join(here, "campaign-2-purple.png"));
console.log("done");
