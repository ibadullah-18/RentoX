# RentoX web

The public website: Next.js (App Router) + TypeScript + Tailwind CSS, on top of
the .NET API in `../backend`. One layout that follows the mobile app on a
phone and opens up into a tap.az-style page on a computer.

## Run

```bash
cd web
npm install
npm run dev        # http://localhost:3000
```

The API must be running (default `http://localhost:5155`). Settings, if the
defaults do not fit, go in `web/.env.local`:

```
API_BASE_URL=http://localhost:5155
NEXT_PUBLIC_SITE_URL=http://localhost:3000
```

`/api/*` on the site is proxied to the API (see `next.config.ts`), so the
browser never needs CORS and listing photos load from the same address.

## Layout

- `src/lib` — API calls (`api.ts`), types, formatting, texts (`i18n.ts`).
- `src/components` — header, catalog menu, cards, gallery, bottom bar.
- `src/app` — `/` home, `/search`, `/listing/[id]`; `(soon)` holds the
  account sections that are not on the web yet.

The palette in `src/app/globals.css` is the same "Indigo" palette as the app.
