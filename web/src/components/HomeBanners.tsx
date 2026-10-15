import Image from "next/image";
import Link from "next/link";
import type { Banner } from "@/lib/types";

/** One banner: the picture with the text over a dark fade; a link when the banner has one. */
function Slide({ banner, first }: { banner: Banner; first: boolean }) {
  const body = (
    <>
      <Image
        src={banner.imageUrl}
        alt=""
        fill
        unoptimized
        priority={first}
        sizes="(min-width: 1100px) 1100px, 100vw"
        className="object-cover"
      />
      <span className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" aria-hidden />
      <span className="absolute inset-x-0 bottom-0 p-4 text-white md:p-6">
        <span className="block text-lg font-extrabold leading-tight drop-shadow md:text-3xl">{banner.title}</span>
        {banner.subtitle && <span className="mt-1 block text-sm font-medium opacity-95 drop-shadow md:text-base">{banner.subtitle}</span>}
      </span>
    </>
  );
  const box = "relative block aspect-[3/1] min-h-32 w-full shrink-0 snap-center overflow-hidden rounded-xl bg-primary-soft";
  const url = banner.linkUrl ?? "";

  if (!url) return <div className={box}>{body}</div>;
  if (url.startsWith("/")) {
    return (
      <Link href={url} className={box}>
        {body}
      </Link>
    );
  }
  return (
    <a href={url} target="_blank" rel="noopener noreferrer" className={box}>
      {body}
    </a>
  );
}

/** The banners of the home page; with several, they sit side by side and swipe. */
export function HomeBanners({ banners }: { banners: Banner[] }) {
  if (banners.length === 0) return null;
  return (
    <section aria-label="Banner" className="flex snap-x snap-mandatory gap-3 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
      {banners.map((b, i) => (
        <Slide key={b.id} banner={b} first={i === 0} />
      ))}
    </section>
  );
}
