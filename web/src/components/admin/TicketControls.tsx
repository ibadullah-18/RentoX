"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { setTicketPriority, setTicketStatus } from "@/app/admin/(panel)/actions";
import { adminHref } from "@/lib/admin-path";
import { priorityLabel } from "@/lib/admin-labels";
import { ticketStatus } from "@/lib/support";

const select = "h-10 w-full rounded-lg border border-line bg-surface px-3 text-sm font-semibold outline-none focus:border-primary disabled:opacity-60";

/** The status and the priority of the request; a change is saved at once. */
export function TicketControls({ id, status, priority }: { id: string; status: number; priority: number }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function change(call: () => ReturnType<typeof setTicketStatus>) {
    setBusy(true);
    setError(null);
    const res = await call();
    setBusy(false);
    if (res.ok) router.refresh();
    else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <div className="space-y-3 p-4">
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Status</span>
        <select value={status} disabled={busy} onChange={(e) => void change(() => setTicketStatus(id, Number(e.target.value)))} className={select}>
          {Object.entries(ticketStatus).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>
      <label className="block">
        <span className="mb-1 block text-sm font-bold">Prioritet</span>
        <select value={priority} disabled={busy} onChange={(e) => void change(() => setTicketPriority(id, Number(e.target.value)))} className={select}>
          {Object.entries(priorityLabel).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  );
}
