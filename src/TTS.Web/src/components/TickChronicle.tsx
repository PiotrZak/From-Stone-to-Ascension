import type { TickLogEntry } from '../api';
import { classifyIntelLine, intelTagLabel, isLineAboutCiv, type IntelLine } from './tickIntel';

type Props = {
  tickLogs: TickLogEntry[];
  civilizationName?: string;
  newestFirst?: boolean;
  maxHeight?: string;
  emptyMessage?: string;
};

function IntelRow({ line, highlight }: { line: IntelLine; highlight: boolean }) {
  return (
    <li className={`gc-intel-item gc-intel-${line.tag}${highlight ? ' gc-intel-you' : ''}`}>
      <span className="gc-intel-tag">[{intelTagLabel(line.tag)}]</span>
      <span>{line.text}</span>
    </li>
  );
}

export function TickChronicle({
  tickLogs,
  civilizationName,
  newestFirst = true,
  maxHeight = 'min(42vh, 420px)',
  emptyMessage = 'No tick reports yet — simulation will log research, gates, and world events each cycle.',
}: Props) {
  if (tickLogs.length === 0) {
    return <p className="gc-intel-empty">{emptyMessage}</p>;
  }

  const orderedTicks = newestFirst ? [...tickLogs].reverse() : [...tickLogs];

  return (
    <div className="gc-tick-chronicle" style={{ maxHeight }}>
      {orderedTicks.map((entry) => (
        <section key={entry.tick} className="gc-tick-block">
          <header className="gc-tick-block-head">
            <span className="gc-tick-block-label">Tick {entry.tick}</span>
            <span className="gc-tick-block-count">
              {entry.lines.length} report{entry.lines.length === 1 ? '' : 's'}
            </span>
          </header>
          <ul className="gc-intel-list gc-intel-list-grouped">
            {entry.lines.map((text) => (
              <IntelRow
                key={`${entry.tick}-${text}`}
                line={{ tick: entry.tick, text, tag: classifyIntelLine(text) }}
                highlight={isLineAboutCiv(text, civilizationName)}
              />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}
