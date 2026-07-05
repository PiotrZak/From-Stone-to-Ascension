import { Minus, Plus, RotateCcw } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import type { HexMap, HexTile } from '../api';
import { Button } from '@/components/ui/button';
import type { CameraState, ThreeHexMapHandle } from './hex-map/createThreeHexMap';
import {
  BIOME_COLORS,
  biomeLabel,
  canClaim,
  DEFAULT_HEX_SIZE,
  LEGEND_BIOMES,
  tileMeta,
} from './hex-map/hexMapModel';

interface HexMapViewProps {
  map: HexMap;
  myCivilizationId: string | null;
  disabled?: boolean;
  hexSize?: number;
  onClaim?: (q: number, r: number) => void | Promise<void>;
  onSelectionChange?: (tile: HexTile | null, meta: string | null) => void;
  showLegend?: boolean;
}

export function HexMapView({
  map,
  myCivilizationId,
  disabled,
  hexSize = DEFAULT_HEX_SIZE,
  onClaim,
  onSelectionChange,
  showLegend = true,
}: HexMapViewProps) {
  const hostRef = useRef<HTMLDivElement>(null);
  const engineRef = useRef<ThreeHexMapHandle | null>(null);
  const mapRef = useRef(map);
  const civRef = useRef(myCivilizationId);
  const onClaimRef = useRef(onClaim);
  const onSelectionChangeRef = useRef(onSelectionChange);
  const cameraMemoryRef = useRef<CameraState | null>(null);
  const [selected, setSelected] = useState<HexTile | null>(null);
  const [hovered, setHovered] = useState<HexTile | null>(null);
  const [mapError, setMapError] = useState<string | null>(null);
  const selectedRef = useRef<HexTile | null>(null);
  selectedRef.current = selected;

  mapRef.current = map;
  civRef.current = myCivilizationId;
  onClaimRef.current = onClaim;
  onSelectionChangeRef.current = onSelectionChange;

  const mapKey = useMemo(
    () => `${map.seed}:${map.width}x${map.height}`,
    [map.seed, map.width, map.height],
  );

  const mapRevision = useMemo(
    () =>
      map.tiles
        .map((t) => `${t.q},${t.r}:${t.controllingCivilizationId ?? ''}:${t.isCapital}`)
        .join('|'),
    [map.tiles],
  );

  const activeTile = hovered ?? selected;
  const claimable = activeTile && canClaim(activeTile, map, myCivilizationId);
  const meta = activeTile ? tileMeta(activeTile, myCivilizationId, !!claimable) : null;

  const presentBiomes = useMemo(() => {
    const set = new Set(map.tiles.map((t) => t.biome));
    return LEGEND_BIOMES.filter((b) => set.has(b));
  }, [map.tiles]);

  // Create globe once per map geometry; restore camera angle if we had one.
  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;

    let cancelled = false;
    if (engineRef.current) {
      cameraMemoryRef.current = engineRef.current.getCameraState();
      engineRef.current.destroy();
      engineRef.current = null;
    }
    setMapError(null);

    void import('./hex-map/createThreeHexMap')
      .then(({ createThreeHexMap }) =>
        createThreeHexMap(
          host,
          mapRef.current,
          hexSize,
          civRef.current,
          !!disabled,
          !!onClaim,
          {
            onTileSelect(tile) {
              setSelected(tile);
              const can = canClaim(tile, mapRef.current, civRef.current);
              onSelectionChangeRef.current?.(tile, tileMeta(tile, civRef.current, can));
            },
            onTileClaim(tile) {
              if (!onClaimRef.current || disabled) return;
              void onClaimRef.current(tile.q, tile.r);
            },
            onHoverChange(tile) {
              setHovered(tile);
              if (tile) {
                const can = canClaim(tile, mapRef.current, civRef.current);
                onSelectionChangeRef.current?.(tile, tileMeta(tile, civRef.current, can));
              } else if (selectedRef.current) {
                const t = selectedRef.current;
                const can = canClaim(t, mapRef.current, civRef.current);
                onSelectionChangeRef.current?.(t, tileMeta(t, civRef.current, can));
              }
            },
          },
          cameraMemoryRef.current,
        ),
      )
      .then((handle) => {
        if (cancelled) {
          handle.destroy();
          return;
        }
        engineRef.current = handle;
        if (selectedRef.current) handle.setSelected(selectedRef.current);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setMapError(err instanceof Error ? err.message : 'Failed to load 3D map');
      });

    return () => {
      cancelled = true;
      if (engineRef.current) {
        cameraMemoryRef.current = engineRef.current.getCameraState();
        engineRef.current.destroy();
        engineRef.current = null;
      }
    };
  }, [mapKey, hexSize]);

  // Live tile updates (ownership, capitals) without resetting the camera.
  useEffect(() => {
    engineRef.current?.syncMap(map, myCivilizationId, !!disabled, !!onClaim);
  }, [mapRevision, myCivilizationId, disabled, onClaim]);

  useEffect(() => {
    engineRef.current?.setSelected(selected);
  }, [selected]);

  return (
    <div className="hex-map-wrap">
      <div className="hex-map-viewport" ref={hostRef}>
        {mapError && (
          <p className="hex-map-error" role="alert">
            Map failed to load: {mapError}. Try refreshing the page.
          </p>
        )}
      </div>

      <div className="hex-map-controls" aria-label="Map zoom controls">
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="hex-map-control-btn"
          aria-label="Zoom in"
          onClick={() => engineRef.current?.zoomBy(1.2)}
        >
          <Plus className="h-4 w-4" />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="hex-map-control-btn"
          aria-label="Zoom out"
          onClick={() => engineRef.current?.zoomBy(0.84)}
        >
          <Minus className="h-4 w-4" />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="hex-map-control-btn"
          aria-label="Reset map view"
          onClick={() => engineRef.current?.fitToView()}
        >
          <RotateCcw className="h-4 w-4" />
        </Button>
      </div>

      {meta && <p className="hex-map-meta-hidden" aria-live="polite">{meta}</p>}

      {showLegend && presentBiomes.length > 0 && (
        <div className="map-biome-legend map-continent-legend">
          <span className="map-legend-heading">Biomes</span>
          {presentBiomes.map((biome) => (
            <div key={biome} className="map-biome-legend-item">
              <span className="map-biome-dot" style={{ background: BIOME_COLORS[biome] }} />
              <span>{biomeLabel(biome)}</span>
            </div>
          ))}
        </div>
      )}

      <p className="hex-map-hint">Drag to spin the world · Scroll to zoom · Right-drag to pan</p>
    </div>
  );
}

export function defaultTerritoryHint(): string {
  return 'Select a tile · biomes & yields';
}
