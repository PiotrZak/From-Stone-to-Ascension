import { Link } from 'react-router-dom';
import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Anchor,
  ArrowLeft,
  BarChart3,
  Moon,
  SlidersHorizontal,
  Sun,
  X,
} from 'lucide-react';
import { api, type HexTile, type TradeGlobe, type TradeHub } from '../api';
import { TradeDetailPanel } from '../components/trade/TradeDetailPanel';
import { TradeGlobeView } from '../components/trade/TradeGlobeView';
import {
  readTradeGlobeTheme,
  TRADE_GLOBE_THEME_KEY,
  type TradeGlobeTheme,
} from '../components/trade/tradeGlobeTheme';
import { cn } from '@/lib/utils';

type TradeWidget = 'filters' | 'data' | 'ports' | 'selection';

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
  const [theme, setTheme] = useState<TradeGlobeTheme>(readTradeGlobeTheme);
  const [openWidget, setOpenWidget] = useState<TradeWidget | null>(null);

  const hasFilters = Boolean(commodity || importCountry || transportMode);
  const hasSelection = Boolean(selectedHub || selectedTile);

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
    setOpenWidget((current) => (current === 'selection' ? null : current));
  }, [commodity, importCountry, transportMode]);

  useEffect(() => {
    document.documentElement.classList.toggle('trade-theme-light', theme === 'light');
    try {
      localStorage.setItem(TRADE_GLOBE_THEME_KEY, theme);
    } catch {
      /* ignore */
    }
    return () => document.documentElement.classList.remove('trade-theme-light');
  }, [theme]);

  const toggleTheme = () => setTheme((current) => (current === 'dark' ? 'light' : 'dark'));

  const toggleWidget = (widget: TradeWidget) => {
    setOpenWidget((current) => (current === widget ? null : widget));
  };

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
    setOpenWidget((current) => (current === 'selection' ? null : current));
  };

  const handleHubSelect = (hub: TradeHub | null) => {
    setSelectedHub(hub);
    if (hub) {
      setSelectedTile(null);
      setSelectedCountryId(hub.countryId);
      setOpenWidget('selection');
    } else if (!selectedTile) {
      setSelectedCountryId(undefined);
      setOpenWidget((current) => (current === 'selection' ? null : current));
    }
  };

  const handleTileSelect = (tile: HexTile | null, tradeCountry?: string) => {
    setSelectedTile(tile);
    setSelectedCountryId(tradeCountry);
    if (tile) {
      setSelectedHub(null);
      setOpenWidget('selection');
    } else if (!selectedHub) {
      setSelectedCountryId(undefined);
      setOpenWidget((current) => (current === 'selection' ? null : current));
    }
  };

  const selectHubFromList = (hub: TradeHub) => {
    handleHubSelect(hub);
  };

  return (
    <div className={cn('trade-page', theme === 'light' && 'trade-page--light')}>
      <section className="trade-globe-stage">
        {data && !loading ? (
          <TradeGlobeView
            data={data}
            theme={theme}
            selectedTileId={selectedTile?.id ?? null}
            selectedHubId={selectedHub?.id ?? null}
            onHubSelect={handleHubSelect}
            onTileSelect={handleTileSelect}
          />
        ) : (
          <p className="trade-loading">Loading trade globe…</p>
        )}
      </section>

      <nav className="trade-widget-dock" aria-label="Trade globe controls">
        <Link to="/" className="trade-dock-btn" aria-label="Home" title="Home">
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'filters' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('filters')}
          aria-label="Filters"
          title="Filters"
          aria-pressed={openWidget === 'filters'}
        >
          <SlidersHorizontal className="h-4 w-4" />
          {hasFilters && <span className="trade-dock-badge" />}
        </button>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'data' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('data')}
          aria-label="Trade data"
          title="Trade data"
          aria-pressed={openWidget === 'data'}
        >
          <BarChart3 className="h-4 w-4" />
        </button>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'ports' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('ports')}
          aria-label="Ports"
          title="Ports"
          aria-pressed={openWidget === 'ports'}
        >
          <Anchor className="h-4 w-4" />
        </button>
        <button
          type="button"
          className={cn(
            'trade-dock-btn',
            openWidget === 'selection' && 'trade-dock-btn--active',
            hasSelection && 'trade-dock-btn--has-selection',
          )}
          onClick={() => toggleWidget('selection')}
          aria-label="Selection details"
          title="Selection details"
          aria-pressed={openWidget === 'selection'}
          disabled={!hasSelection}
        >
          <span className="trade-dock-selection-dot" />
        </button>
        <button
          type="button"
          className="trade-dock-btn"
          onClick={toggleTheme}
          aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
          title={theme === 'dark' ? 'Light theme' : 'Dark theme'}
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>
      </nav>

      {openWidget && (
        <aside className="trade-widget-panel" aria-label="Trade widget panel">
          <header className="trade-widget-panel-head">
            <h2 className="trade-widget-panel-title">
              {openWidget === 'filters' && 'Filters'}
              {openWidget === 'data' && 'Trade data'}
              {openWidget === 'ports' && 'Ports'}
              {openWidget === 'selection' && 'Selection'}
            </h2>
            <button
              type="button"
              className="trade-widget-panel-close"
              onClick={() => setOpenWidget(null)}
              aria-label="Close panel"
            >
              <X className="h-4 w-4" />
            </button>
          </header>

          <div className="trade-widget-panel-body">
            {openWidget === 'filters' && (
              <>
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
              </>
            )}

            {openWidget === 'data' && data && (
              <div className="trade-stats">
                <p className="trade-stats-total">
                  {hasFilters
                    ? `Filtered: ${data.filteredShipmentCount.toLocaleString()} shipments · $${Math.round(totalValue).toLocaleString()}`
                    : `Total value: $${Math.round(totalValue).toLocaleString()}`}
                </p>
                <p className="trade-stats-meta">
                  {(data.flows ?? []).length} visible routes · {(data.hubs ?? []).length} hubs ·{' '}
                  {(data.map?.tiles ?? []).length} tiles
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

            {openWidget === 'ports' && data && (
              <ul className="trade-port-list">
                {(data.hubs ?? []).map((hub) => {
                  const stats = data.hubStats?.[hub.id];
                  const country = data.countries.find((c) => c.countryId === hub.countryId);
                  const selected = selectedHub?.id === hub.id;
                  const active =
                    !hasFilters || (data.activeHubIds ?? []).includes(hub.id);
                  return (
                    <li key={hub.id}>
                      <button
                        type="button"
                        className={cn(
                          'trade-port-item',
                          selected && 'trade-port-item--selected',
                          !active && 'trade-port-item--inactive',
                        )}
                        onClick={() => selectHubFromList(hub)}
                      >
                        <span
                          className="trade-country-dot"
                          style={{ background: COUNTRY_COLORS[hub.countryId] ?? '#94a3b8' }}
                        />
                        <span className="trade-port-item-main">
                          <span className="trade-port-item-name">{hub.name}</span>
                          <span className="trade-port-item-sub">
                            {hub.isPort ? 'Port' : 'Capital'} · {country?.displayName ?? hub.countryId}
                          </span>
                        </span>
                        <span className="trade-port-item-value">
                          {stats
                            ? `$${Math.round(stats.outboundValueUsd + stats.inboundValueUsd).toLocaleString()}`
                            : '—'}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}

            {openWidget === 'selection' && data && (
              <>
                {hasSelection ? (
                  <TradeDetailPanel
                    data={data}
                    selectedHub={selectedHub}
                    selectedTile={selectedTile}
                    selectedCountryId={selectedCountryId}
                    hasFilters={hasFilters}
                    activeCountryIds={activeCountryIds}
                    onClear={clearSelection}
                  />
                ) : (
                  <p className="trade-widget-empty">Click a port or territory on the globe.</p>
                )}
              </>
            )}

            {error && <p className="trade-error">{error}</p>}
          </div>
        </aside>
      )}
    </div>
  );
}
