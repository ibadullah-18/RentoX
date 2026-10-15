/** The person's initials in a coloured circle (the colour comes from the name, so it stays the same everywhere). */
const colours = [
  "bg-primary-soft text-primary",
  "bg-success-soft text-success",
  "bg-warning-soft text-warning",
  "bg-accent-soft text-accent",
];

export function Avatar({ name, size = "md" }: { name?: string | null; size?: "sm" | "md" | "lg" }) {
  const text = (name ?? "").trim();
  const initials = text
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase("az"))
    .join("") || "?";
  const colour = colours[[...text].reduce((sum, ch) => sum + ch.charCodeAt(0), 0) % colours.length];
  const box = size === "lg" ? "size-14 text-lg" : size === "sm" ? "size-8 text-xs" : "size-11 text-sm";
  return (
    <span aria-hidden className={`flex shrink-0 items-center justify-center rounded-full font-extrabold ${box} ${colour}`}>
      {initials}
    </span>
  );
}
