import { Link } from 'react-router-dom';
import { useCallback, useEffect, useState } from 'react';
import {
  Antenna,
  ArrowLeft,
  Moon,
  Orbit,
  Satellite,
  SlidersHorizontal,
  Sun,
  X,
} from 'lucide-react';
import {
  api,
  type SatelliteBody,
  type SatelliteGlobe,
  type SatelliteGroundStation,
} from '../api';
import { SatelliteGlobeView } from '../components/satellites/SatelliteGlobeView';
import {
  readTradeGlobeTheme,
  TRADE_GLOBE_THEME_KEY,
  type TradeGlobeTheme,
} from '../components/trade/tradeGlobeTheme';
import { cn } from '@/lib/utils';

type Widget = 'filters' | 'constellations' | 'stations' | 'selection';

export function SatelliteGlobePage() {
  const [data, setData] = useState<SatelliteGlobe | null>(null);
  const [purpose, setPurpose] = useState('');
  const [orbitClass, setOrbitClass] = useState('');
  const [country, setCountry] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [theme, setTheme] = useState<TradeGlobeTheme>(readTradeGlobeTheme);
  const [openWidget, setOpenWidget] = useState<Widget | null>('constellations');
  const [selectedSat, setSelectedSat] = useState<SatelliteBody | null>(null);
  const [selectedStation, setSelectedStation] = useState<SatelliteGroundStation | null>(null);

  const hasFilters = Boolean(purpose || orbitClass || country);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setData(
        await api.getSatelliteGlobe({
          purpose: purpose || undefined,
          orbitClass: orbitClass || undefined,
          country: country || undefined,
        }),
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load satellite globe');
    } finally {
      setLoading(false);
    }
  }, [purpose, orbitClass, country]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setSelectedSat(null);
    setSelectedStation(null);
  }, [purpose, orbitClass, country]);

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
  const toggleWidget = (widget: Widget) =>
    setOpenWidget((current) => (current === widget ? null : widget));

  const clearFilters = () => {
    setPurpose('');
    setOrbitClass('');
    setCountry('');
  };

  const hasSelection = Boolean(selectedSat || selectedStation);
  const selectedConstellation = selectedSat
    ? data?.availableConstellations.find((c) => c.id === selectedSat.constellationId)
    : selectedStation
      ? data?.availableConstellations.find((c) => c.id === selectedStation.constellationId)
      : undefined;

  return (
    <div className={cn('trade-page satellite-page', theme === 'light' && 'trade-page--light')}>
      <section className="trade-globe-stage">
        {data && !loading ? (
          <SatelliteGlobeView
            data={data}
            theme={theme}
            selectedSatelliteId={selectedSat?.id ?? null}
            onSatelliteSelect={(sat) => {
              setSelectedSat(sat);
              if (sat) {
                setSelectedStation(null);
                setOpenWidget('selection');
              }
            }}
            onStationSelect={(station) => {
              setSelectedStation(station);
              if (station) {
                setSelectedSat(null);
                setOpenWidget('selection');
              }
            }}
          />
        ) : (
          <p className="trade-loading">
            {loading ? 'Loading satellite globe…' : error ?? 'Failed to load satellite globe'}
          </p>
        )}
      </section>

      <nav className="trade-widget-dock" aria-label="Satellite globe controls">
        <Link to="/" className="trade-dock-btn" aria-label="Home" title="Home">
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'filters' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('filters')}
          aria-label="Filters"
          title="Filters"
        >
          <SlidersHorizontal className="h-4 w-4" />
          {hasFilters && <span className="trade-dock-badge" />}
        </button>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'constellations' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('constellations')}
          aria-label="Purpose groups"
          title="Purpose groups"
        >
          <Orbit className="h-4 w-4" />
        </button>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'stations' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('stations')}
          aria-label="Launch sites"
          title="Launch sites"
        >
          <Antenna className="h-4 w-4" />
        </button>
        <button
          type="button"
          className={cn('trade-dock-btn', openWidget === 'selection' && 'trade-dock-btn--active')}
          onClick={() => toggleWidget('selection')}
          aria-label="Selection"
          title="Selection"
          disabled={!hasSelection}
        >
          <Satellite className="h-4 w-4" />
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
        <aside className="trade-widget-panel" aria-label="Satellite widget panel">
          <header className="trade-widget-panel-head">
            <h2 className="trade-widget-panel-title">
              {openWidget === 'filters' && 'Filters'}
              {openWidget === 'constellations' && 'Purpose groups'}
              {openWidget === 'stations' && 'Launch sites'}
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
                  Purpose
                  <select
                    className="trade-filter-select"
                    value={purpose}
                    onChange={(e) => setPurpose(e.target.value)}
                  >
                    <option value="">All purposes</option>
                    {(data?.availableConstellations ?? []).map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="trade-filter-label">
                  Orbit class
                  <select
                    className="trade-filter-select"
                    value={orbitClass}
                    onChange={(e) => setOrbitClass(e.target.value)}
                  >
                    <option value="">All orbits</option>
                    {(data?.orbitClasses ?? []).map((o) => (
                      <option key={o} value={o}>
                        {o}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="trade-filter-label">
                  Operator country
                  <select
                    className="trade-filter-select"
                    value={country}
                    onChange={(e) => setCountry(e.target.value)}
                  >
                    <option value="">All countries</option>
                    {(data?.countries ?? []).map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </label>

                {hasFilters && (
                  <button type="button" className="trade-filter-clear" onClick={clearFilters}>
                    Clear filters
                  </button>
                )}
                <p className="trade-widget-empty">
                  Data from satelites.csv. Up to 100 satellites are drawn at once for performance;
                  filters narrow the catalog before sampling.
                </p>
              </>
            )}

            {openWidget === 'constellations' && data && (
              <ul className="trade-port-list">
                {data.availableConstellations.map((c) => {
                  const active = !purpose || purpose === c.id;
                  return (
                    <li key={c.id}>
                      <button
                        type="button"
                        className={cn(
                          'trade-port-item',
                          purpose === c.id && 'trade-port-item--selected',
                          !active && 'trade-port-item--inactive',
                        )}
                        onClick={() => setPurpose(purpose === c.id ? '' : c.id)}
                      >
                        <span className="trade-country-dot" style={{ background: c.color }} />
                        <span className="trade-port-item-main">
                          <span className="trade-port-item-name">{c.name}</span>
                          <span className="trade-port-item-sub">
                            {c.satelliteCount.toLocaleString()} in catalog
                          </span>
                        </span>
                      </button>
                      <p className="satellite-constellation-summary">{c.summary}</p>
                    </li>
                  );
                })}
              </ul>
            )}

            {openWidget === 'stations' && data && (
              <ul className="trade-port-list">
                {data.groundStations.map((station) => {
                  const color =
                    data.availableConstellations.find((c) => c.id === station.constellationId)
                      ?.color ?? '#94a3b8';
                  return (
                    <li key={station.id}>
                      <button
                        type="button"
                        className={cn(
                          'trade-port-item',
                          selectedStation?.id === station.id && 'trade-port-item--selected',
                        )}
                        onClick={() => {
                          setSelectedStation(station);
                          setSelectedSat(null);
                          setOpenWidget('selection');
                        }}
                      >
                        <span className="trade-country-dot" style={{ background: color }} />
                        <span className="trade-port-item-main">
                          <span className="trade-port-item-name">{station.name}</span>
                          <span className="trade-port-item-sub">
                            {station.region} · {station.launchCount.toLocaleString()} launches in view
                          </span>
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}

            {openWidget === 'selection' && (
              <>
                {selectedSat && (
                  <div className="trade-detail-panel">
                    <p className="trade-detail-kicker">Satellite</p>
                    <h2 className="trade-detail-title">{selectedSat.name}</h2>
                    <p className="trade-detail-sub">
                      {selectedSat.operator || 'Unknown operator'} · {selectedSat.country || '—'}
                    </p>
                    <div className="trade-detail-grid">
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Purpose</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedSat.purpose || '—'}
                        </span>
                      </div>
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Orbit</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedSat.orbitClass || '—'}
                          {selectedSat.orbitType ? ` · ${selectedSat.orbitType}` : ''}
                        </span>
                      </div>
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Altitude</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {Math.round(selectedSat.perigeeKm).toLocaleString()}–
                          {Math.round(selectedSat.apogeeKm).toLocaleString()} km
                        </span>
                      </div>
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Inclination / period</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedSat.inclinationDeg.toFixed(1)}° ·{' '}
                          {selectedSat.periodMinutes > 0
                            ? `${selectedSat.periodMinutes.toFixed(1)} min`
                            : '—'}
                        </span>
                      </div>
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Launch</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedSat.launchDate || '—'}
                          {selectedSat.launchSite ? ` · ${selectedSat.launchSite}` : ''}
                        </span>
                      </div>
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Users / NORAD</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedSat.users || '—'}
                          {selectedSat.noradNumber ? ` · ${selectedSat.noradNumber}` : ''}
                        </span>
                      </div>
                    </div>
                    {selectedConstellation && (
                      <p className="satellite-constellation-summary">{selectedConstellation.summary}</p>
                    )}
                  </div>
                )}
                {selectedStation && (
                  <div className="trade-detail-panel">
                    <p className="trade-detail-kicker">Launch site</p>
                    <h2 className="trade-detail-title">{selectedStation.name}</h2>
                    <p className="trade-detail-sub">
                      {selectedStation.region} · {selectedStation.launchCount.toLocaleString()} launches
                      in current filter
                    </p>
                    <div className="trade-detail-grid">
                      <div className="trade-detail-stat">
                        <span className="trade-detail-stat-label">Coordinates</span>
                        <span className="trade-detail-stat-value trade-detail-stat-value--text">
                          {selectedStation.latDeg.toFixed(2)}°, {selectedStation.lonDeg.toFixed(2)}°
                        </span>
                      </div>
                    </div>
                  </div>
                )}
                {!hasSelection && (
                  <p className="trade-widget-empty">Click a satellite or launch site on the globe.</p>
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
