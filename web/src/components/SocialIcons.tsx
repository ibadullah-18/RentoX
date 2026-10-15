/** The logos of the networks we link to (the icon set of the site has none). They take the colour of the text. */
type Props = { className?: string };

export function InstagramIcon({ className }: Props) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
      <rect x="3" y="3" width="18" height="18" rx="5" />
      <circle cx="12" cy="12" r="4" />
      <circle cx="17.3" cy="6.7" r="1" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function FacebookIcon({ className }: Props) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor" aria-hidden>
      <path d="M14 8.5V7c0-.7.3-1 1-1h2V3h-3c-2.8 0-4 1.7-4 4v1.5H8V12h2v9h4v-9h2.6l.4-3.5H14z" />
    </svg>
  );
}

export function TikTokIcon({ className }: Props) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor" aria-hidden>
      <path d="M16.6 3c.3 2.2 1.6 3.7 3.4 3.9v3.2c-1.3 0-2.5-.4-3.5-1.1v6.1A5.6 5.6 0 1 1 10.9 9.5v3.3a2.4 2.4 0 1 0 1.6 2.3V3h4.1z" />
    </svg>
  );
}

export function WhatsAppIcon({ className }: Props) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d="M3 21l1.6-4.7A8.5 8.5 0 1 1 8 19.5L3 21z" />
      <path d="M9 9.2c.2 2.7 2.6 5.2 5.6 5.7l1.2-1.2-1.9-1-.8.7c-.9-.4-1.6-1.2-2-2l.7-.8-1-1.9L9 9.2z" fill="currentColor" stroke="none" />
    </svg>
  );
}
