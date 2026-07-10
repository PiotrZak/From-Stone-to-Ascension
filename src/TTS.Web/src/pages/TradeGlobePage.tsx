import { Link } from 'react-router-dom';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ArrowLeft } from 'lucide-react';
import { api, type HexTile, type TradeGlobe, type TradeHub } from '../api';
import { TradeDetailPanel } from '../components/trade/TradeDetailPanel';
import { TradeGlobeView } from '../components/trade/TradeGlobeView';
import { Button } from '@/components/ui/button';

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

export function TradeGlobePage() {
  const [data, setData] = useState<TradeGlobe | null>(null);
  const [commodity, setCommodity] = useState('');
  const [importCountry, setImportCountry] = useState('');
  const [transportMode, setTransportMode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [selectedHub, setSelectedHub] = useState<TradeHub | null>(null);
  const [selectedTile, setSelectedTile] = useState<HexTile | null>(null);
  const [selectedCountryId, setSelectedCountryId] = useState<string | undefined>();
  const [loading, setLoading] = useState(true);

  const hasFilters = Boolean(commodity || importCountry || transportMode);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setData(
        await api.getTradeGlobe({
          commodity: commodity || undefined,
          importCountry: importCountry || undefined,
          transportMode: transportMode || undefined,
        }),
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load trade globe');
    } finally {
      setLoading(false);
    }
  }, [commodity, importCountry, transportMode]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setSelectedHub(null);
    setSelectedTile(null);
    setSelectedCountryId(undefined);
  }, [commodity, importCountry, transportMode]);

  const activeCountryIds = useMemo(
    () => new Set(data?.activeCountryIds ?? []),
    [data?.activeCountryIds],
  );

  const totalValue = useMemo(
    () =>
      (data?.countries ?? [])
        .filter((c) => !hasFilters || activeCountryIds.has(c.countryId))
        .reduce((sum, c) => sum + c.totalValueUsd, 0),
    [data?.countries, hasFilters, activeCountryIds],
  );

  const clearFilters = () => {
    setCommodity('');
    setImportCountry('');
    setTransportMode('');
  };

  const clearSelection = () => {
    setSelectedHub(null);
    setSelectedTile(null);
    setSelectedCountryId(undefined);
  };

  const handleHubSelect = (hub: TradeHub | null) => {
    setSelectedHub(hub);
    if (hub) {
      setSelectedTile(null);
      setSelectedCountryId(hub.countryId);
    } else if (!selectedTile) {
      setSelectedCountryId(undefined);
    }
  };

  const handleTileSelect = (tile: HexTile | null, tradeCountry?: string) => {
    setSelectedTile(tile);
    setSelectedCountryId(tradeCountry);
    if (tile) setSelectedHub(null);
  };

  return (
    <div className="trade-page">
      <header className="trade-page-head">
        <div>
          <p className="trade-page-kicker">China–Africa trade dataset</p>
          <h1 className="trade-page-title">Trade globe</h1>
          <p className="trade-page-sub">
            Goldberg polyhedron · choropleth · hub markers · flow arcs
          </p>
        </div>
        <Button variant="ghost" size="icon" asChild>
          <Link to="/" aria-label="Home">
            <ArrowLeft className="h-4 w-4" />
          </Link>
        </Button>
      </header>

      <div className="trade-page-layout">
        <aside className="trade-filters">
          <label className="trade-filter-label">
            Commodity
            <select
              className="trade-filter-select"
              value={commodity}
              onChange={(e) => setCommodity(e.target.value)}
            >
              <option value="">All commodities</option>
              {(data?.commodities ?? []).map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </label>

          <label className="trade-filter-label">
            Import country
            <select
              className="trade-filter-select"
              value={importCountry}
              onChange={(e) => setImportCountry(e.target.value)}
            >
              <option value="">All countries</option>
              {(data?.importCountries ?? []).map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </label>

          <label className="trade-filter-label">
            Transport
            <select
              className="trade-filter-select"
              value={transportMode}
              onChange={(e) => setTransportMode(e.target.value)}
            >
              <option value="">All modes</option>
              {(data?.transportModes ?? []).map((m) => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>
          </label>

          {hasFilters && (
            <button type="button" className="trade-filter-clear" onClick={clearFilters}>
              Clear filters
            </button>
          )}

          {error && <p className="trade-error">{error}</p>}

          {data && (
            <div className="trade-stats">
              <p className="trade-stats-total">
                {hasFilters
                  ? `Filtered: ${data.filteredShipmentCount.toLocaleString()} shipments · $${Math.round(totalValue).toLocaleString()}`
                  : `Total value: $${Math.round(totalValue).toLocaleString()}`}
              </p>
              <p className="trade-stats-meta">
                {(data.flows ?? []).length} visible routes · {(data.hubs ?? []).length} hubs · {(data.map?.tiles ?? []).length} tiles
              </p>
              <ul className="trade-country-list">
                {(data.countries ?? []).map((c) => {
                  const active = !hasFilters || activeCountryIds.has(c.countryId);
                  return (
                    <li key={c.countryId} className={active ? '' : 'trade-country-inactive'}>
                      <span
                        className="trade-country-dot"
                        style={{ background: COUNTRY_COLORS[c.countryId] ?? '#94a3b8' }}
                      />
                      <span>{c.displayName}</span>
                      <span>
                        {c.totalValueUsd > 0
                          ? `$${Math.round(c.totalValueUsd).toLocaleString()}`
                          : active
                            ? '—'
                            : 'Filtered out'}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>
          )}

          {data && (
            <TradeDetailPanel
              data={data}
              selectedHub={selectedHub}
              selectedTile={selectedTile}
              selectedCountryId={selectedCountryId}
              hasFilters={hasFilters}
              activeCountryIds={activeCountryIds}
              onClear={clearSelection}
            />
          )}
        </aside>

        <section className="trade-globe-panel">
          {data && !loading ? (
            <TradeGlobeView
              data={data}
              selectedTileId={selectedTile?.id ?? null}
              selectedHubId={selectedHub?.id ?? null}
              onHubSelect={handleHubSelect}
              onTileSelect={handleTileSelect}
            />
          ) : (
            <p className="trade-loading">Loading trade globe…</p>
          )}
        </section>
      </div>
    </div>
  );
}
