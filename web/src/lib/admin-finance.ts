import "server-only";
import { authed } from "./authed";

export type FinancePeriod = {
  name: "today" | "week" | "month" | "all";
  from: string;
  to: string;
  serviceIncome: number;
  refunds: number;
  income: number;
  businessExpenses: number;
  staffCost: number | null;
  netIncome: number | null;
};

export type FinanceSummary = { currency: string; generatedAtUtc: string; periods: FinancePeriod[] };

export type Expense = {
  id: string;
  /** 1 budget, 2 tax, 3 other, 4 personal. */
  category: number;
  title: string;
  amount: number;
  day: string;
  note?: string | null;
  createdByName?: string | null;
  createdAtUtc: string;
  isVoid: boolean;
  voidReason?: string | null;
  voidedByName?: string | null;
};

export type FinanceProfile = {
  fullName: string;
  voen?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
};

type Line = { day: string; title: string; amount: number };

export type MonthlyReport = {
  month: string;
  currency: string;
  generatedAtUtc: string;
  incomeLines: { label: string; amount: number }[];
  serviceIncome: number;
  refunds: number;
  income: number;
  staff: { name: string; salary: number; bonus: number; deductions: number; cost: number }[];
  staffCost: number;
  budgets: Line[];
  budgetsTotal: number;
  taxes: Line[];
  taxesTotal: number;
  otherExpenses: Line[];
  otherTotal: number;
  netIncome: number;
  personal: Line[];
  personalTotal: number;
  afterPersonal: number;
  profile: FinanceProfile;
};

/** "2026-10" from the address, or nothing (the API then takes the current month). */
export function monthParam(value: string | string[] | undefined): string | undefined {
  const text = Array.isArray(value) ? value[0] : value;
  return text && /^\d{4}-(0[1-9]|1[0-2])$/.test(text) ? text : undefined;
}

export const getFinanceSummary = () => authed<FinanceSummary>("/api/admin/finance/summary");
export const getMonthlyReport = (month?: string) => authed<MonthlyReport>("/api/admin/finance/report", { query: { month } });
export const getExpenses = (month?: string) => authed<Expense[]>("/api/admin/finance/expenses", { query: { month } });
export const getFinanceProfile = () => authed<FinanceProfile>("/api/admin/finance/profile");
