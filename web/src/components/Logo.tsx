import Link from "next/link";

export function Logo({ className = "" }: { className?: string }) {
  return (
    <Link
      href="/"
      aria-label="RentoX"
      className={`text-[26px] font-extrabold tracking-tight text-ink ${className}`}
    >
      rento<span className="text-primary">x</span>
    </Link>
  );
}
