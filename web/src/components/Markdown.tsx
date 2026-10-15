import Link from "next/link";
import type { ReactNode } from "react";

/** Only places a visitor can safely be sent to: web addresses, e-mail, phone, or a page of this site. */
function safeHref(url: string): string | null {
  if (/^https?:\/\//i.test(url) || url.startsWith("mailto:") || url.startsWith("tel:")) return url;
  if (url.startsWith("/") && !url.startsWith("//")) return url;
  return null;
}

/** **bold** and [text](address) inside a line; everything else is plain text. */
function inline(text: string): ReactNode[] {
  const out: ReactNode[] = [];
  const pattern = /(\*\*[^*]+\*\*|\[[^\]]+\]\([^)\s]+\))/g;
  let last = 0;
  let key = 0;
  for (const match of text.matchAll(pattern)) {
    if (match.index > last) out.push(text.slice(last, match.index));
    const token = match[0];
    if (token.startsWith("**")) {
      out.push(<strong key={key++}>{token.slice(2, -2)}</strong>);
    } else {
      const label = token.slice(1, token.indexOf("]"));
      const href = safeHref(token.slice(token.indexOf("](") + 2, -1));
      if (!href) {
        out.push(label);
      } else if (href.startsWith("/")) {
        out.push(
          <Link key={key++} href={href} className="font-semibold text-primary hover:underline">
            {label}
          </Link>,
        );
      } else {
        out.push(
          <a key={key++} href={href} target="_blank" rel="noopener noreferrer" className="font-semibold text-primary hover:underline">
            {label}
          </a>,
        );
      }
    }
    last = match.index + token.length;
  }
  if (last < text.length) out.push(text.slice(last));
  return out;
}

type Block =
  | { kind: "h2" | "h3"; text: string }
  | { kind: "p"; lines: string[] }
  | { kind: "ul" | "ol"; items: string[] };

function parse(text: string): Block[] {
  const blocks: Block[] = [];
  for (const raw of text.replace(/\r\n/g, "\n").split("\n")) {
    const line = raw.trimEnd();
    const last = blocks[blocks.length - 1];
    if (line.trim() === "") {
      blocks.push({ kind: "p", lines: [] }); // a blank line ends the block before it
      continue;
    }
    const heading = /^(#{2,3})\s+(.*)$/.exec(line);
    if (heading) {
      blocks.push({ kind: heading[1].length === 2 ? "h2" : "h3", text: heading[2] });
      continue;
    }
    const bullet = /^[-*]\s+(.*)$/.exec(line);
    if (bullet) {
      if (last?.kind === "ul") last.items.push(bullet[1]);
      else blocks.push({ kind: "ul", items: [bullet[1]] });
      continue;
    }
    const numbered = /^\d+\.\s+(.*)$/.exec(line);
    if (numbered) {
      if (last?.kind === "ol") last.items.push(numbered[1]);
      else blocks.push({ kind: "ol", items: [numbered[1]] });
      continue;
    }
    if (last?.kind === "p") last.lines.push(line);
    else blocks.push({ kind: "p", lines: [line] });
  }
  return blocks.filter((b) => b.kind !== "p" || b.lines.length > 0);
}

/**
 * The page texts the staff write: ## headings, paragraphs, - lists, 1. lists, **bold** and [links](address).
 * Nothing is ever put in as raw HTML, so a page can never run a script.
 */
export function Markdown({ text }: { text: string }) {
  return (
    <div className="text-[15px] leading-relaxed md:text-base">
      {parse(text).map((b, i) => {
        switch (b.kind) {
          case "h2":
            return (
              <h2 key={i} className="mb-3 mt-8 text-xl font-extrabold md:text-2xl">
                {inline(b.text)}
              </h2>
            );
          case "h3":
            return (
              <h3 key={i} className="mb-2 mt-5 text-lg font-bold">
                {inline(b.text)}
              </h3>
            );
          case "ul":
            return (
              <ul key={i} className="mb-3 list-disc space-y-1.5 pl-5">
                {b.items.map((item, j) => (
                  <li key={j}>{inline(item)}</li>
                ))}
              </ul>
            );
          case "ol":
            return (
              <ol key={i} className="mb-3 list-decimal space-y-1.5 pl-5">
                {b.items.map((item, j) => (
                  <li key={j}>{inline(item)}</li>
                ))}
              </ol>
            );
          default:
            return (
              <p key={i} className="mb-3">
                {b.lines.map((line, j) => (
                  <span key={j}>
                    {j > 0 && <br />}
                    {inline(line)}
                  </span>
                ))}
              </p>
            );
        }
      })}
    </div>
  );
}
