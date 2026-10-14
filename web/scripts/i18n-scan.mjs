// Lists string literals that look like interface text but are not wrapped in
// t(...) / tx(...) / translate(...).   node scripts/i18n-scan.mjs [--keys]
// With --keys it prints every text that is wrapped (the dictionary keys).
import fs from "node:fs";
import path from "node:path";
import ts from "typescript";

const keysMode = process.argv.includes("--keys");
const skipDirs = ["src/app/admin", "src/lib/i18n", "src/lib/admin"];
const files = [];
(function walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name).replaceAll("\\", "/");
    if (e.isDirectory()) walk(p);
    else if (/\.(ts|tsx)$/.test(e.name) && !p.endsWith(".d.ts") && !skipDirs.some((d) => p.startsWith(d))) files.push(p);
  }
})("src");

const AZ = /[əƏöÖüÜğĞıİşŞçÇ]/;
const WRAPPERS = new Set(["t", "tx", "tr", "mark", "translate"]);
const found = new Set();
const out = [];

for (const file of files) {
  const text = fs.readFileSync(file, "utf8");
  const src = ts.createSourceFile(file, text, ts.ScriptTarget.Latest, true, file.endsWith("x") ? ts.ScriptKind.TSX : ts.ScriptKind.TS);
  const visit = (n) => {
    const isStr = ts.isStringLiteral(n) || ts.isNoSubstitutionTemplateLiteral(n);
    if (isStr || ts.isTemplateHead(n) || ts.isTemplateMiddle(n) || ts.isTemplateTail(n)) {
      let p = n.parent;
      if (!isStr) p = p.parent;
      const wrapped = ts.isCallExpression(p) && (WRAPPERS.has(p.expression.getText()) || p.expression.getText() === "(await getT())") && p.arguments[0] === n;
      if (wrapped) {
        if (keysMode) found.add(n.text);
      } else if (!keysMode) {
        const v = n.text;
        const inImport = ts.isImportDeclaration(p) || ts.isExportDeclaration(p) || ts.isLiteralTypeNode(p);
        const attr = ts.isJsxAttribute(p) ? p.name.getText() : "";
        const skipAttr = ["className", "href", "type", "name", "id", "key", "role", "autoComplete", "inputMode", "htmlFor", "method", "rel", "target", "sizes", "src", "pattern", "d", "viewBox", "fill", "stroke"].includes(attr);
        const looksText = /\p{L}{3,}/u.test(v) && (AZ.test(v) || /\s/.test(v)) && !/^[\w./:#?=&%-]+$/.test(v) && !v.includes("=>") && !/^(use client|use server)$/.test(v);
        if (!inImport && !skipAttr && looksText && !/^\s*(rounded|flex|grid|text-|bg-|border|size-|h-|w-|px|py|mx|mt|mb|gap)/.test(v)) {
          const { line } = src.getLineAndCharacterOfPosition(n.getStart());
          out.push(`${file}:${line + 1}: ${v.replace(/\s+/g, " ").slice(0, 90)}`);
        }
      }
    }
    ts.forEachChild(n, visit);
  };
  visit(src);
}
if (keysMode) console.log(JSON.stringify([...found].sort(), null, 1));
else console.log(out.join("\n") + `\n${out.length} candidates`);
