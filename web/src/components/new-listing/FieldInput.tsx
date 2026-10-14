"use client";

import { Select } from "@/components/ui/Select";
import { useT } from "@/lib/i18n/client";
import { tx } from "@/lib/i18n/locales";
import { FieldType, type FieldDefinition } from "@/lib/types";
import { type Answer, type FieldError } from "./fields";

const input =
  "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary";
const chip = (on: boolean) =>
  `rounded-lg border px-4 py-2 text-sm font-semibold transition-colors ${
    on
      ? "border-primary bg-primary text-white"
      : "border-line bg-surface hover:border-primary"
  }`;

const errorText: Record<FieldError, string> = {
  required: tx("Bu xana məcburidir."),
  number: tx("Rəqəm yazın."),
  whole: tx("Tam ədəd yazın."),
};

/** One category field, drawn to suit its type. */
export function FieldInput({
  def,
  answer,
  error,
  disabled,
  onChange,
}: {
  def: FieldDefinition;
  answer: Answer;
  error: FieldError | null;
  disabled: boolean;
  onChange: (next: Answer) => void;
}) {
  const t = useT();
  const label = def.label ?? def.key ?? "";
  const options = def.options ?? [];
  const set = (patch: Partial<Answer>) => onChange({ ...answer, ...patch });

  let control: React.ReactNode;

  switch (def.type) {
    case FieldType.boolean:
      control = (
        <div className="flex gap-2">
          {[
            [true, t("Bəli")],
            [false, "Xeyr"],
          ].map(([value, text]) => (
            <button
              key={String(value)}
              type="button"
              disabled={disabled}
              onClick={() => set({ flag: answer.flag === value ? null : (value as boolean) })}
              className={chip(answer.flag === value)}
            >
              {text as string}
            </button>
          ))}
        </div>
      );
      break;

    case FieldType.date:
      control = (
        <input
          type="date"
          disabled={disabled}
          value={answer.date}
          onChange={(e) => set({ date: e.target.value })}
          className={input}
        />
      );
      break;

    case FieldType.singleSelect:
    case FieldType.multiSelect: {
      const multi = def.type === FieldType.multiSelect;
      const toggle = (id: string) => {
        if (multi) {
          set({
            optionIds: answer.optionIds.includes(id)
              ? answer.optionIds.filter((x) => x !== id)
              : [...answer.optionIds, id],
          });
        } else {
          set({ optionIds: answer.optionIds[0] === id ? [] : [id], custom: "" });
        }
      };
      control = (
        <div className="space-y-3">
          {options.length > 8 && !multi ? (
            <Select
              disabled={disabled}
              value={answer.optionIds[0] ?? ""}
              onChange={(v) => set({ optionIds: v ? [v] : [], custom: "" })}
              placeholder={t("Seçin")}
              ariaLabel={def.label ?? ""}
              className="h-11"
              options={options.map((o) => ({ value: o.id, label: o.label }))}
            />
          ) : (
            <div className="flex flex-wrap gap-2">
              {options.map((o) => (
                <button
                  key={o.id}
                  type="button"
                  disabled={disabled}
                  aria-pressed={answer.optionIds.includes(o.id)}
                  onClick={() => toggle(o.id)}
                  className={chip(answer.optionIds.includes(o.id))}
                >
                  {o.label}
                </button>
              ))}
            </div>
          )}
          {def.allowCustomValue && (
            <input
              disabled={disabled}
              value={answer.custom}
              onChange={(e) =>
                set({ custom: e.target.value, optionIds: multi ? answer.optionIds : [] })
              }
              placeholder={t("Siyahıda yoxdursa, özünüz yazın")}
              maxLength={500}
              className={input}
            />
          )}
        </div>
      );
      break;
    }

    default: {
      const numeric = def.type === FieldType.wholeNumber || def.type === FieldType.fractionalNumber;
      control = (
        <input
          disabled={disabled}
          value={answer.text}
          onChange={(e) => set({ text: e.target.value })}
          inputMode={numeric ? "decimal" : undefined}
          maxLength={numeric ? 20 : 2000}
          className={input}
        />
      );
    }
  }

  return (
    <div>
      <span className="mb-2 block text-sm font-bold">
        {label}
        {def.isRequired && <span className="text-danger"> *</span>}
      </span>
      {control}
      {error && (
        <p role="alert" className="mt-1.5 text-sm font-semibold text-danger">
          {t(errorText[error])}
        </p>
      )}
    </div>
  );
}
