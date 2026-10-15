import Link from "next/link";
import { BackLink } from "./BackLink";
import type { LucideIcon } from "lucide-react";

/** The title of an admin page, with an optional line under it and a way back. */
export function PageHead({
  title,
  hint,
  back,
  actions,
}: {
  title: string;
  hint?: string;
  back?: { href: string; label: string };
  actions?: React.ReactNode;
}) {
  return (
    <div className="space-y-2">
      {back && (
        <BackLink href={back.href} title={back.label} />
      )}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-extrabold md:text-2xl">{title}</h1>
          {hint && <p className="mt-1 max-w-2xl text-sm text-ink-2">{hint}</p>}
        </div>
        {actions && <div className="no-print flex gap-2">{actions}</div>}
      </div>
    </div>
  );
}

const tones = {
  warning: "bg-warning-soft text-warning",
  primary: "bg-primary-soft text-primary",
  success: "bg-success-soft text-success",
  danger: "bg-danger/10 text-danger",
  muted: "bg-line text-ink-2",
} as const;

export type Tone = keyof typeof tones;

/** A small coloured label (a status). */
export function Pill({ tone = "muted", children }: { tone?: Tone; children: React.ReactNode }) {
  return <span className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-bold ${tones[tone]}`}>{children}</span>;
}

export function Empty({ icon: Icon, text }: { icon: LucideIcon; text: string }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-xl border border-line bg-surface py-14 text-center">
      <Icon className="size-10 text-primary" aria-hidden />
      <p className="font-bold">{text}</p>
    </div>
  );
}

/** Filter chips that are links (the filter lives in the address). */
export function Chips({
  items,
  current,
}: {
  items: { label: string; href: string; value?: string }[];
  current?: string;
}) {
  return (
    <div className="no-print strip">
      {items.map((i) => (
        <Link
          key={i.label}
          href={i.href}
          className={`shrink-0 rounded-lg border px-3.5 py-1.5 text-sm font-semibold ${
            current === i.value ? "border-primary bg-primary text-white" : "border-line bg-surface hover:border-primary"
          }`}
        >
          {i.label}
        </Link>
      ))}
    </div>
  );
}

/** A box with a title around a block of content. */
export function Card({ title, children }: { title?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-line bg-surface">
      {title && <h2 className="border-b border-line px-4 py-3 text-[15px] font-extrabold">{title}</h2>}
      {children}
    </section>
  );
}

export const dateTime = (iso?: string | null) =>
  iso ? new Date(iso).toLocaleString("az", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "—";

export const shortId = (id?: string | null) => (id ? id.slice(0, 8) : "—");

/** Why a list did not load: an old API (it does not know the call yet) says so plainly. */
export function LoadProblem({ what, status }: { what: string; status: number }) {
  const oldApi = status === 404 || status === 405;
  const denied = status === 403;
  return (
    <div role="alert" className="space-y-1 rounded-xl bg-danger/10 px-4 py-3 text-sm text-danger">
      <p className="font-bold">{what} yüklənmədi.</p>
      <p>
        {denied
          ? "Bu bölməyə icazəniz yoxdur."
          : oldApi
          ? "API-nin köhnə versiyası işləyir: bu sorğunu bilmir. API-ni dayandırıb yenidən başladın (dotnet run)."
          : status === 0
            ? "API-yə qoşulmaq olmadı. API-nin işlədiyini yoxlayın."
            : `API cavabı: ${status}.`}
      </p>
    </div>
  );
}
