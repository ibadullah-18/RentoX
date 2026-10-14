/** `+994` and the 9 digits after it — the form the backend stores. */
export function normalizePhone(input: string): string {
  let digits = input.replace(/\D/g, "");
  if (digits.startsWith("994")) digits = digits.slice(3);
  else if (digits.startsWith("0")) digits = digits.slice(1);
  return `+994${digits}`;
}

export function isValidPhone(input: string): boolean {
  return normalizePhone(input).length === "+994".length + 9;
}

/** "50 123 45 67" while typing (only the 9 national digits). */
export function formatNational(input: string): string {
  let digits = input.replace(/\D/g, "");
  if (digits.startsWith("994")) digits = digits.slice(3);
  else if (digits.startsWith("0")) digits = digits.slice(1);
  digits = digits.slice(0, 9);
  const parts = [digits.slice(0, 2), digits.slice(2, 5), digits.slice(5, 7), digits.slice(7, 9)];
  return parts.filter(Boolean).join(" ");
}
