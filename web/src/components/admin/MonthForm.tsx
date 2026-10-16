/** Picks the month of a finance page. A plain form: the address carries the choice. */
export function MonthForm({ value }: { value: string }) {
  return (
    <form method="get" className="no-print flex items-center gap-2">
      <input
        type="month"
        name="ay"
        defaultValue={value}
        className="h-9 rounded-lg border border-line bg-surface px-3 text-sm outline-none focus:border-primary"
      />
      <button type="submit" className="h-9 rounded-lg border border-line bg-surface px-3 text-sm font-semibold hover:border-primary">
        Göstər
      </button>
    </form>
  );
}
