import { useEffect, useRef, useState } from 'react';
import type { SatelliteBody, SatelliteGlobe, SatelliteGroundStation } from '../../api';
import type { SatelliteGlobeHandle } from './createSatelliteGlobe';
import type { TradeGlobeTheme } from '../trade/tradeGlobeTheme';
import { DEFAULT_HEX_SIZE } from '../hex-map/hexMapModel';

type Props = {
  data: SatelliteGlobe;
  theme?: TradeGlobeTheme;
  selectedSatelliteId: string | null;
  onSatelliteSelect?: (sat: SatelliteBody | null) => void;
  onStationSelect?: (station: SatelliteGroundStation | null) => void;
};

export function SatelliteGlobeView({
  data,
  theme = 'dark',
  selectedSatelliteId,
  onSatelliteSelect,
  onStationSelect,
}: Props) {
  const hostRef = useRef<HTMLDivElement>(null);
  const engineRef = useRef<SatelliteGlobeHandle | null>(null);
  const onSatRef = useRef(onSatelliteSelect);
  const onStationRef = useRef(onStationSelect);
  const [hover, setHover] = useState<string | null>(null);

  useEffect(() => {
    onSatRef.current = onSatelliteSelect;
    onStationRef.current = onStationSelect;
  }, [onSatelliteSelect, onStationSelect]);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;

    let cancelled = false;
    engineRef.current?.destroy();
    engineRef.current = null;

    void import('./createSatelliteGlobe')
      .then(({ createSatelliteGlobe }) =>
        createSatelliteGlobe(
          host,
          data,
          DEFAULT_HEX_SIZE,
          {
            onHover: setHover,
            onSatelliteSelect: (sat) => onSatRef.current?.(sat),
            onStationSelect: (station) => onStationRef.current?.(station),
          },
          theme,
        ),
      )
      .then((handle) => {
        if (cancelled) handle.destroy();
        else {
          engineRef.current = handle;
          handle.setSelection(selectedSatelliteId);
        }
      });

    return () => {
      cancelled = true;
      engineRef.current?.destroy();
      engineRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- init scene once
  }, []);

  useEffect(() => {
    engineRef.current?.applyData(data);
  }, [data]);

  useEffect(() => {
    engineRef.current?.setTheme(theme);
  }, [theme]);

  useEffect(() => {
    engineRef.current?.setSelection(selectedSatelliteId);
  }, [selectedSatelliteId]);

  return (
    <div className="trade-globe-wrap">
      <div className="trade-globe-viewport" ref={hostRef} />
      {data.appliedPurposeGroupId || data.appliedOrbitClass || data.appliedCountry ? (
        <p className="trade-globe-filter-badge">
          {data.visibleSatelliteCount.toLocaleString()} shown · {data.totalSatelliteCount.toLocaleString()}{' '}
          matching
          {data.appliedPurposeGroupId ? ` · ${data.constellations[0]?.name ?? data.appliedPurposeGroupId}` : ''}
          {data.appliedOrbitClass ? ` · ${data.appliedOrbitClass}` : ''}
          {data.appliedCountry ? ` · ${data.appliedCountry}` : ''}
        </p>
      ) : (
        <p className="trade-globe-filter-badge">
          {data.visibleSatelliteCount.toLocaleString()} shown · {data.totalSatelliteCount.toLocaleString()} in
          catalog
        </p>
      )}
      {hover && <p className="trade-globe-meta">{hover}</p>}
      <p className="trade-globe-hint">Click satellite or ground station · Drag to spin · Scroll to zoom</p>
    </div>
  );
}
