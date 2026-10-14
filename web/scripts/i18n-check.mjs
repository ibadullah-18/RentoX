// Compares the texts used in the code with the English and Russian
// dictionaries.   node scripts/i18n-check.mjs
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import ts from "typescript";

const keys = JSON.parse(execFileSync("node", ["scripts/i18n-scan.mjs", "--keys"], { encoding: "utf8" }));
const IGNORE = { has: (k) => k === "RentoX" || /^https?:\/\//.test(k) };

function load(file) {
  const src = ts.createSourceFile(file, fs.readFileSync(file, "utf8"), ts.ScriptTarget.Latest, true);
  const out = new Map();
  const visit = (n) => {
    if (ts.isPropertyAssignment(n) && ts.isStringLiteralLike(n.name) && ts.isStringLiteralLike(n.initializer)) {
      out.set(n.name.text, n.initializer.text);
    }
    ts.forEachChild(n, visit);
  };
  visit(src);
  return out;
}

let bad = 0;
for (const lang of ["en", "ru"]) {
  const dict = load(`src/lib/i18n/${lang}.ts`);
  const missing = keys.filter((k) => !dict.has(k) && !IGNORE.has(k));
  const extra = [...dict.keys()].filter((k) => !keys.includes(k));
  const holes = [...dict].filter(([k, v]) => {
    const a = (k.match(/\{\w+\}/g) ?? []).sort().join();
    const b = (v.match(/\{\w+\}/g) ?? []).sort().join();
    return a !== b;
  });
  console.log(`${lang}: ${dict.size} texts, ${missing.length} missing, ${extra.length} unused, ${holes.length} placeholder mismatches`);
  missing.forEach((k) => console.log(`  missing: ${k}`));
  extra.forEach((k) => console.log(`  unused: ${k}`));
  holes.forEach(([k]) => console.log(`  placeholders: ${k}`));
  bad += missing.length + holes.length;
}
process.exit(bad ? 1 : 0);
