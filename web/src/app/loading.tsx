/** Shown while a page's data is on its way: grey cards in the shape of the real ones. */
export default function Loading() {
  return (
    <div className="min-h-[75vh] space-y-8" aria-busy="true" aria-live="polite">
      <div className="h-8 w-48 animate-pulse rounded-lg bg-line" />
      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:gap-4 lg:grid-cols-4 xl:grid-cols-5">
        {Array.from({ length: 10 }, (_, i) => (
          <li key={i} className="overflow-hidden rounded-lg">
            <div className="aspect-[4/3] animate-pulse bg-line" />
            <div className="space-y-2 border border-t-0 border-line p-2.5">
              <div className="h-4 w-3/4 animate-pulse rounded bg-line" />
              <div className="h-4 w-1/2 animate-pulse rounded bg-line" />
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
