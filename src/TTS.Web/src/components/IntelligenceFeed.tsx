import type { CivDashboard, Civilization, MatchSummary } from '../api';
import { TickChronicle } from './TickChronicle';

type Props = {
  summary: MatchSummary;
  myCiv: Civilization | undefined;
  civilizationName?: string;
  dashboard: CivDashboard | null;
  cityCount: number;
};

export function IntelligenceFeed({
  summary,
  myCiv,
  civilizationName,
  dashboard,
  cityCount,
}: Props) {
  return (
    <section className="gc-intel-feed gc-intel-feed-full" aria-label="Intelligence feed">
      <header className="gc-panel-head">
        <h2 className="gc-panel-title">Intelligence feed</h2>
      </header>

      {myCiv && (
        <div className="gc-intel-snapshot">
          <span>TTS {myCiv.tier}</span>
          <span>{myCiv.techCount} tech</span>
          <span>{cityCount} hubs</span>
          <span>{Math.round(myCiv.averageStability)}% stability</span>
          {dashboard?.recommendedTech && <span>Next: {dashboard.recommendedTech.name}</span>}
        </div>
      )}

      <h3 className="gc-tick-chronicle-title">Tick chronicle</h3>
      <TickChronicle
        tickLogs={summary.tickLogs}
        civilizationName={civilizationName ?? myCiv?.name}
      />
    </section>
  );
}
