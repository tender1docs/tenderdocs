import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function fmtDate(iso: string, opts?: Intl.DateTimeFormatOptions) {
  return new Date(iso).toLocaleDateString('en-GB', opts ?? { day: 'numeric', month: 'short', year: 'numeric' });
}

export function fmtDayMonth(iso: string) {
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
}

export function pluralize(n: number, word: string) {
  return `${n} ${word}${n === 1 ? '' : 's'}`;
}

/**
 * Indian financial years (Apr 1 – Mar 31) for a dropdown: the current FY plus the
 * previous 10 and next 10, newest first. Labels look like "FY 2026-27".
 */
export function financialYearOptions(): string[] {
  const now = new Date();
  // getMonth() is 0-indexed; April === 3. Before April the FY started the prior year.
  const start = now.getMonth() >= 3 ? now.getFullYear() : now.getFullYear() - 1;
  const out: string[] = [];
  for (let y = start + 10; y >= start - 10; y--) {
    out.push(`FY ${y}-${String((y + 1) % 100).padStart(2, '0')}`);
  }
  return out;
}

/** The Indian financial year that a given date falls into, e.g. "FY 2026-27". */
export function financialYearForDate(dateStr?: string | null): string | null {
  if (!dateStr) return null;
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return null;
  const y = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
  return `FY ${y}-${String((y + 1) % 100).padStart(2, '0')}`;
}
