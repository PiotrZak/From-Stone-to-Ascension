import type { AwaySummaryStructured } from '../api';

export function AwaySummaryView({ summary }: { summary: AwaySummaryStructured }) {
  return (
    <div className="away-summary-structured">
      <p className="away-headline">{summary.headline}</p>
      {summary.bullets.length > 0 && (
        <ul className="away-bullets">
          {summary.bullets.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
      )}
      {summary.missedGates.length > 0 && (
        <div className="away-missed">
          <p className="away-missed-label">Missed gates (defaults applied)</p>
          <ul className="away-bullets away-missed-list">
            {summary.missedGates.map((line) => (
              <li key={line}>{line}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
