import { useEffect, useRef, useState } from 'react';
import type { HexTile, TradeGlobe, TradeHub } from '../../api';
import type { TradeGlobeHandle } from './createTradeGlobe';
import type { TradeGlobeTheme } from './tradeGlobeTheme';
import { DEFAULT_HEX_SIZE } from '../hex-map/hexMapModel';

type Props = {
  data: TradeGlobe;
  theme?: TradeGlobeTheme;
  hexSize?: number;
  selectedTileId: string | null;
  selectedHubId: string | null;
  onHubSelect?: (hub: TradeHub | null) => void;
  onTileSelect?: (tile: HexTile | null, tradeCountry?: string) => void;
};

export function TradeGlobeView({
  data,
  theme = 'dark',
  hexSize = DEFAULT_HEX_SIZE,
  selectedTileId,
  selectedHubId,
  onHubSelect,
  onTileSelect,
}: Props) {
  const hostRef = useRef<HTMLDivElement>(null);
  const engineRef = useRef<TradeGlobeHandle | null>(null);
  const onHubSelectRef = useRef(onHubSelect);
  const onTileSelectRef = useRef(onTileSelect);
  const [hover, setHover] = useState<string | null>(null);

  useEffect(() => {
    onHubSelectRef.current = onHubSelect;
    onTileSelectRef.current = onTileSelect;
  }, [onHubSelect, onTileSelect]);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;

    let cancelled = false;
    engineRef.current?.destroy();
    engineRef.current = null;

    void import('./createTradeGlobe')
      .then(({ createTradeGlobe }) =>
        createTradeGlobe(host, data, hexSize, {
          onHover: setHover,
          onHubSelect: (hub) => onHubSelectRef.current?.(hub),
          onTileSelect: (tile, tradeCountry) => onTileSelectRef.current?.(tile, tradeCountry),
        }, theme),
      )
      .then((handle) => {
        if (cancelled) handle.destroy();
        else {
          engineRef.current = handle;
          handle.setSelection(selectedTileId, selectedHubId);
        }
      });

    return () => {
      cancelled = true;
      engineRef.current?.destroy();
      engineRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- init scene once
  }, [hexSize]);

  useEffect(() => {
    engineRef.current?.applyData(data);
  }, [data]);

  useEffect(() => {
    engineRef.current?.setTheme(theme);
  }, [theme]);

  useEffect(() => {
    engineRef.current?.setSelection(selectedTileId, selectedHubId);
  }, [selectedTileId, selectedHubId]);

  const filterHint = [
    data.appliedCommodity,
    data.appliedImportCountry,
    data.appliedTransportMode,
  ].filter(Boolean);

  return (
    <div className="trade-globe-wrap">
      <div className="trade-globe-viewport" ref={hostRef} />
      {filterHint.length > 0 && (
        <p className="trade-globe-filter-badge">
          {data.filteredShipmentCount.toLocaleString()} shipments · {filterHint.join(' · ')}
        </p>
      )}
      {hover && <p className="trade-globe-meta">{hover}</p>}
      <p className="trade-globe-hint">Click hex or port · Drag to spin · Scroll to zoom</p>
    </div>
  );
}
