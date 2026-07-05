import type { ReactNode } from 'react';
import type { Civilization, MatchSummary } from '../api';
import { TickClock } from './TickClock';
import { formatEta, tickProgressPercent } from './tickIntel';

type Props = {
  summary: MatchSummary;
  myCiv: Civilization;
  modeId: string;
  population: number;
  resourceYield: number;
  cityCount: number;
  rivalCount: number;
  pendingGateCount: number;
  formatPopulation: (n: number) => string;
};

function StatusChip({
  label,
  value,
  hint,
}: {
  label: string;
  value?: string;
  hint?: string;
}) {
  return (
    <div className="gc-cs-chip">
      <div className="gc-cs-chip-head">
        <span className="gc-cs-label">{label}</span>
        {value !== undefined && <span className="gc-cs-value">{value}</span>}
      </div>
      {hint && <span className="gc-cs-hint">{hint}</span>}
    </div>
  );
}

function ProgressCell({
  label,
  value,
  pct,
  sub,
}: {
  label: string;
  value: string;
  pct: number;
  sub?: ReactNode;
}) {
  return (
    <div className="gc-cs-progress-cell">
      <div className="gc-status-bar-head">
        <span className="gc-status-label">{label}</span>
        <span className="gc-status-value">{value}</span>
      </div>
      <div className="gc-status-bar">
        <div className="gc-status-bar-fill" style={{ width: `${pct}%` }} />
      </div>
      {sub && <p className="gc-status-sub">{sub}</p>}
    </div>
  );
}

export function CommandStatusBar({
  summary,
  myCiv,
  modeId,
  population,
  resourceYield,
  cityCount,
  rivalCount,
  pendingGateCount,
  formatPopulation,
}: Props) {
  const tickPct = tickProgressPercent(summary.tickCount, summary.maxTicks);
  const stabilityPct = Math.round(Math.max(0, Math.min(100, myCiv.averageStability)));
  const victoryGap = Math.max(0, summary.victoryTier - myCiv.tier);

  return (
    <section className="gc-command-status gc-command-status-line" aria-label="Command status">
      <StatusChip label="TTS" value={String(myCiv.tier)} />

      <StatusChip label="Pop" value={formatPopulation(population)} />

      <div className="gc-cs-chip gc-cs-stability-cell">
        <ProgressCell
          label="Stability"
          value={`${stabilityPct}%`}
          pct={stabilityPct}
          sub={myCiv.policyLabel ? `Policy: ${myCiv.policyLabel}` : undefined}
        />
      </div>

      <StatusChip label="Yield" value={String(Math.round(resourceYield))} />

      <StatusChip
        label="Hubs"
        value={String(cityCount)}
        hint={`${rivalCount} rival${rivalCount === 1 ? '' : 's'}`}
      />

      <StatusChip label="Tech" value={String(myCiv.techCount)} />

      <div className="gc-cs-tick-block">
        <TickClock
          ringOnly
          modeId={modeId}
          nextTickAt={summary.nextTickAt}
          isTickDue={summary.isTickDue}
          tickCount={summary.tickCount}
          maxTicks={summary.maxTicks}
        />
        <ProgressCell
          label="Match progress"
          value={`${tickPct}%`}
          pct={tickPct}
          sub={
            <>
              Tick {summary.tickCount}/{summary.maxTicks}
              {summary.isTickDue ? ' · Due now' : ` · Next ${formatEta(summary.nextTickAt)}`}
              {pendingGateCount > 0 && (
                <>
                  {' · '}
                  <span className="gc-cs-alert">
                    {pendingGateCount} gate{pendingGateCount === 1 ? '' : 's'}
                  </span>
                </>
              )}
            </>
          }
        />
      </div>

      <StatusChip
        label="Victory"
        value={`TTS ${summary.victoryTier}+`}
        hint={
          victoryGap > 0
            ? `${victoryGap} tier${victoryGap === 1 ? '' : 's'} left`
            : `${Math.round(summary.victoryStabilityMin)}+ stab`
        }
      />

      <StatusChip
        label="Pillars"
        value={`${Math.round(myCiv.politicalStability)}/${Math.round(myCiv.economicStability)}/${Math.round(myCiv.technologicalStability)}`}
        hint="Soc/Econ/Tech"
      />
    </section>
  );
}
