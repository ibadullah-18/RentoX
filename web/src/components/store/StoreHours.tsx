import { hhmm, openNow, todayNumber, weekdayNames, type OpenDay } from "@/lib/store-contact";
import { StoreHoursView } from "./StoreHoursView";

/** The week of the store as one line that opens up, today marked, and whether it is open right now. Nothing is drawn without hours. */
export function StoreHours({ hours }: { hours?: OpenDay[] | null }) {
  if (!hours || hours.length === 0) return null;

  const today = todayNumber();
  const rows = weekdayNames.map((name, i) => {
    const day = hours.find((h) => h.day === i + 1);
    return { name, text: day ? `${hhmm(day.openMinute)} – ${hhmm(day.closeMinute)}` : null, today: i + 1 === today };
  });

  return <StoreHoursView open={openNow(hours)} todayText={rows.find((r) => r.today)?.text ?? null} rows={rows} />;
}
