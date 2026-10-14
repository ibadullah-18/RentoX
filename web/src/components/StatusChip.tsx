import { getT } from "@/lib/i18n/server";
import { effectiveStatus, statusLabel, statusTone } from "@/lib/status";

export async function StatusChip({
  status,
  expiresAtUtc,
}: {
  status: number;
  expiresAtUtc?: string | null;
}) {
  const t = await getT();
  const effective = effectiveStatus(status, expiresAtUtc);
  return (
    <span
      className={`inline-flex items-center rounded-md px-2.5 py-1 text-xs font-bold ${
        statusTone[effective] ?? "bg-line text-ink-2"
      }`}
    >
      {statusLabel[effective] ? t(statusLabel[effective]) : "—"}
    </span>
  );
}
