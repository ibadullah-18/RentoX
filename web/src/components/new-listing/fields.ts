import { FieldType, type FieldDefinition } from "@/lib/types";
import type { FieldRequest } from "@/app/yeni-elan/actions";

/** What the person entered for one field. Numbers stay text while typing. */
export type Answer = {
  text: string;
  flag: boolean | null;
  date: string;
  optionIds: string[];
  custom: string;
};

export const emptyAnswer: Answer = {
  text: "",
  flag: null,
  date: "",
  optionIds: [],
  custom: "",
};

export type FieldError = "required" | "number" | "whole";

/** "12", "12.5" and "12,5" are numbers. */
export function parseNumber(raw: string): number | null {
  const cleaned = raw.trim().replace(",", ".");
  if (!cleaned) return null;
  const n = Number(cleaned);
  return Number.isFinite(n) ? n : null;
}

export function isEmpty(def: FieldDefinition, a: Answer): boolean {
  switch (def.type) {
    case FieldType.boolean:
      return a.flag === null;
    case FieldType.date:
      return !a.date;
    case FieldType.singleSelect:
    case FieldType.multiSelect:
      return a.optionIds.length === 0 && !a.custom.trim();
    default:
      return !a.text.trim();
  }
}

export function validate(def: FieldDefinition, a: Answer): FieldError | null {
  if (isEmpty(def, a)) return def.isRequired ? "required" : null;
  if (def.type === FieldType.wholeNumber) {
    const n = parseNumber(a.text);
    if (n === null) return "number";
    if (!Number.isInteger(n)) return "whole";
  }
  if (def.type === FieldType.fractionalNumber && parseNumber(a.text) === null) {
    return "number";
  }
  return null;
}

/**
 * The body the API expects, or null when the field was left empty. The API
 * insists on `optionIds` for every field, even those without a list.
 */
export function toRequest(def: FieldDefinition, a: Answer): FieldRequest | null {
  if (isEmpty(def, a)) return null;
  const body: FieldRequest = { fieldId: def.id, optionIds: [] };

  switch (def.type) {
    case FieldType.text:
      body.textValue = a.text.trim();
      break;
    case FieldType.wholeNumber:
    case FieldType.fractionalNumber:
      body.numericValue = parseNumber(a.text) ?? undefined;
      break;
    case FieldType.boolean:
      body.flagValue = a.flag ?? undefined;
      break;
    case FieldType.date:
      body.calendarValue = a.date;
      break;
    case FieldType.singleSelect:
      // One option or a custom value, never both.
      if (a.optionIds.length > 0) body.optionIds = [a.optionIds[0]];
      else body.customValue = a.custom.trim();
      break;
    case FieldType.multiSelect:
      body.optionIds = a.optionIds;
      if (a.custom.trim()) body.customValue = a.custom.trim();
      break;
  }
  return body;
}
