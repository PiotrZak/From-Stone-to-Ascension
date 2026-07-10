import type { HexTile, TradeGlobe, TradeHub, TradeHubStats } from '../../api';

const COUNTRY_COLORS: Record<string, string> = {
  china: '#ef4444',
  india: '#ec4899',
  'middle-east': '#a78bfa',
  nigeria: '#22c55e',
  kenya: '#3b82f6',
  tanzania: '#a855f7',
  ethiopia: '#f59e0b',
  egypt: '#eab308',
  ghana: '#14b8a6',
  'south-africa': '#f97316',
};

type Props = {
  data: TradeGlobe;
  selectedHub: TradeHub | null;
  selectedTile: HexTile | null;
  selectedCountryId?: string;
  hasFilters: boolean;
  activeCountryIds: Set<string>;
  onClear: () => void;
};

function fmtUsd(value: number): string {
  return `$${Math.round(value).toLocaleString()}`;
}

function hubFlows(data: TradeGlobe, hubId: string) {
  return data.flows.filter((f) => f.fromHubId === hubId || f.toHubId === hubId);
}

export function TradeDetailPanel({
  data,
  selectedHub,
  selectedTile,
  selectedCountryId,
  hasFilters,
  activeCountryIds,
  onClear,
}: Props) {
  if (!selectedHub && !selectedTile) return null;

  const countryStats = selectedCountryId
    ? data.countries.find((c) => c.countryId === selectedCountryId)
    : undefined;
  const hubStats: TradeHubStats | undefined = selectedHub
    ? data.hubStats?.[selectedHub.id]
    : undefined;
  const connectedFlows = selectedHub ? hubFlows(data, selectedHub.id) : [];
  const flowValue = connectedFlows.reduce((sum, f) => sum + f.totalValueUsd, 0);
  const flowShipments = connectedFlows.reduce((sum, f) => sum + f.shipmentCount, 0);
  const countryActive = !selectedCountryId || !hasFilters || activeCountryIds.has(selectedCountryId);

  return (
    <div className="trade-detail-panel">
      <div className="trade-detail-head">
        <p className="trade-detail-kicker">
          {selectedHub ? 'Port hub' : 'Territory tile'}
        </p>
        <button type="button" className="trade-detail-clear" onClick={onClear}>
          Clear
        </button>
      </div>

      {selectedHub && (
        <>
          <h2 className="trade-detail-title">{selectedHub.name}</h2>
          <p className="trade-detail-sub">
            {selectedHub.isPort ? 'Port' : 'Capital'} ·{' '}
            {countryStats?.displayName ?? selectedHub.countryId} ·{' '}
            {selectedHub.latDeg.toFixed(2)}°, {selectedHub.lonDeg.toFixed(2)}°
          </p>

          {hubStats && (
            <div className="trade-detail-grid">
              <div className="trade-detail-stat">
                <span className="trade-detail-stat-label">Outbound</span>
                <span className="trade-detail-stat-value">{fmtUsd(hubStats.outboundValueUsd)}</span>
                <span className="trade-detail-stat-sub">{hubStats.outboundShipments.toLocaleString()} shipments</span>
              </div>
              <div className="trade-detail-stat">
                <span className="trade-detail-stat-label">Inbound</span>
                <span className="trade-detail-stat-value">{fmtUsd(hubStats.inboundValueUsd)}</span>
                <span className="trade-detail-stat-sub">{hubStats.inboundShipments.toLocaleString()} shipments</span>
              </div>
              <div className="trade-detail-stat">
                <span className="trade-detail-stat-label">Visible flows</span>
                <span className="trade-detail-stat-value">{fmtUsd(flowValue)}</span>
                <span className="trade-detail-stat-sub">{flowShipments.toLocaleString()} shipments · {connectedFlows.length} routes</span>
              </div>
            </div>
          )}

          {hubStats && hubStats.topCommodities.length > 0 && (
            <div className="trade-detail-block">
              <p className="trade-detail-block-label">Top commodities</p>
              <p className="trade-detail-block-value">{hubStats.topCommodities.join(' · ')}</p>
            </div>
          )}

          {hubStats && hubStats.topRoutes.length > 0 && (
            <div className="trade-detail-block">
              <p className="trade-detail-block-label">Top routes</p>
              <ul className="trade-route-list">
                {hubStats.topRoutes.map((route) => (
                  <li key={`${route.direction}-${route.counterpartHubId}`}>
                    <span className="trade-route-dir">{route.direction === 'Outbound' ? '→' : '←'}</span>
                    <span>{route.counterpartName}</span>
                    <span>{fmtUsd(route.totalValueUsd)}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}

      {selectedTile && !selectedHub && (
        <>
          <h2 className="trade-detail-title">
            {countryStats?.displayName ?? selectedCountryId ?? 'Territory'}
          </h2>
          <p className="trade-detail-sub">
            Tile {selectedTile.id} · {selectedTile.biome}
            {selectedTile.isPentagon ? ' · pentagon' : ' · hex'}
          </p>

          <div className="trade-detail-grid">
            <div className="trade-detail-stat">
              <span className="trade-detail-stat-label">Trade value</span>
              <span className="trade-detail-stat-value">
                {countryStats && countryStats.totalValueUsd > 0
                  ? fmtUsd(countryStats.totalValueUsd)
                  : '—'}
              </span>
              <span className="trade-detail-stat-sub">
                {countryStats ? `${countryStats.shipmentCount.toLocaleString()} shipments` : 'No trade data'}
              </span>
            </div>
            <div className="trade-detail-stat">
              <span className="trade-detail-stat-label">Top commodity</span>
              <span className="trade-detail-stat-value trade-detail-stat-value--text">
                {countryStats?.topCommodity ?? '—'}
              </span>
              <span className="trade-detail-stat-sub">
                {countryActive ? 'In current view' : 'Filtered out'}
              </span>
            </div>
            <div className="trade-detail-stat">
              <span className="trade-detail-stat-label">Region</span>
              <span className="trade-detail-stat-value trade-detail-stat-value--text">
                {selectedTile.worldRegionName ?? selectedTile.worldRegionId ?? '—'}
              </span>
              <span className="trade-detail-stat-sub">Macro continent</span>
            </div>
          </div>

          {selectedCountryId && (
            <div className="trade-detail-block">
              <p className="trade-detail-block-label">Country</p>
              <p className="trade-detail-block-value">
                <span
                  className="trade-country-dot"
                  style={{ background: COUNTRY_COLORS[selectedCountryId] ?? '#94a3b8' }}
                />{' '}
                {countryStats?.displayName ?? selectedCountryId}
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}
