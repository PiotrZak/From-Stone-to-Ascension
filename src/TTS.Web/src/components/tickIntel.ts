import type { TickLogEntry } from '../api';

export type IntelTag = 'alert' | 'gate' | 'research' | 'tier' | 'event' | 'info';

export type IntelLine = {
  tick: number;
  text: string;
  tag: IntelTag;
};

const TAG_LABELS: Record<IntelTag, string> = {
  alert: 'ALERT',
  gate: 'GATE',
  research: 'R&D',
  tier: 'TIER',
  event: 'EVENT',
  info: 'INFO',
};

export function intelTagLabel(tag: IntelTag): string {
  return TAG_LABELS[tag];
}

export function classifyIntelLine(text: string): IntelTag {
  const lower = text.toLowerCase();
  if (/missed|crisis|urgent|war|attack|collapse/.test(lower)) return 'alert';
  if (/decided '|decided "/i.test(text) || /\(auto\)/.test(text)) return 'gate';
  if (/researched| · llm| · classical/i.test(text)) return 'research';
  if (/reached tts|tts \d/i.test(lower)) return 'tier';
  if (/global event:/i.test(text)) return 'event';
  return 'info';
}

export function buildTickIntelLines(tickLogs: TickLogEntry[], newestFirst = true): IntelLine[] {
  const ordered = newestFirst ? [...tickLogs].reverse() : [...tickLogs];
  const lines: IntelLine[] = [];

  for (const entry of ordered) {
    for (const text of entry.lines) {
      lines.push({ tick: entry.tick, text, tag: classifyIntelLine(text) });
    }
  }

  return lines;
}

export function isLineAboutCiv(text: string, civilizationName: string | undefined): boolean {
  if (!civilizationName) return false;
  return text.toLowerCase().includes(civilizationName.toLowerCase());
}

export function tickProgressPercent(tickCount: number, maxTicks: number): number {
  if (maxTicks <= 0) return 0;
  return Math.round(Math.min(100, (tickCount / maxTicks) * 100));
}

export function formatEta(iso: string): string {
  const sec = Math.max(0, Math.floor((new Date(iso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 60) return `in ${sec}s`;
  if (sec < 3600) return `in ${Math.floor(sec / 60)}m`;
  return `in ${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}
