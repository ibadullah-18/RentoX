/**
 * The logo as a picture, one for light pages and one for dark ones (the page look decides which is seen: see
 * ".rx-logo-*" in globals.css). With [text], the name is written instead (the old look).
 */
export function BrandMark({
  light,
  dark,
  name,
  height,
  text = false,
  className = "",
}: {
  light: string;
  dark: string;
  name: string;
  height: number;
  text?: boolean;
  className?: string;
}) {
  if (text) {
    const plain = name.toLowerCase() === "rentox";
    return (
      <span className={`text-[26px] font-extrabold tracking-tight text-ink ${className}`}>
        {plain ? (
          <>
            rento<span className="text-primary">x</span>
          </>
        ) : (
          name
        )}
      </span>
    );
  }
  // The height is the owner's setting; on a phone the logo never goes above 36 px so the header stays slim.
  const style = { ["--logo-h" as string]: `${height}px` } as React.CSSProperties;
  return (
    <span className={`rx-logo ${className}`} style={style}>
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={light} alt={name} className="rx-logo-light" />
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={dark} alt={name} className="rx-logo-dark" aria-hidden />
    </span>
  );
}
