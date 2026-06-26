import { useMemo, useState } from 'react';
import type { HexMap, HexTile } from '../api';

const BIOME_COLORS: Record<string, string> = {
  Ocean: '#388bfd',
  Coast: '#38bdf8',
  Plains: '#3fb950',
  Forest: '#15803d',
  Hills: '#a3a3a3',
  Mountains: '#525252',
  Desert: '#f0b429',
  Tundra: '#cbd5e1',
  Wetlands: '#0d9488',
};

const LEGEND_BIOMES = ['Forest', 'Desert', 'Plains', 'Ocean'] as const;

const CIV_COLORS: Record<string, string> = {
  'civ-player': 'rgba(56, 139, 253, 0.45)',
  'civ-rival': 'rgba(248, 81, 73, 0.45)',
};

const HEX_SIZE = 22;

function axialToPixel(q: number, r: number, size: number) {
  const x = size * (3 / 2) * q;
  const y = size * (Math.sqrt(3) * (r + q / 2));
  return { x, y };
}

function hexCorners(cx: number, cy: number, size: number): string {
  const points: string[] = [];
  for (let i = 0; i < 6; i++) {
    const angle = (Math.PI / 180) * (60 * i);
    points.push(`${cx + size * Math.cos(angle)},${cy + size * Math.sin(angle)}`);
  }
  return points.join(' ');
}

function canClaim(tile: HexTile, map: HexMap, myCivId: string | null): boolean {
  if (!myCivId || tile.controllingCivilizationId || tile.biome === 'Ocean') return false;

  const neighbors = [
    [tile.q + 1, tile.r],
    [tile.q + 1, tile.r - 1],
    [tile.q, tile.r - 1],
    [tile.q - 1, tile.r],
    [tile.q - 1, tile.r + 1],
    [tile.q, tile.r + 1],
  ];

  return neighbors.some(([q, r]) =>
    map.tiles.some((t) => t.q === q && t.r === r && t.controllingCivilizationId === myCivId),
  );
}

function tileMeta(tile: HexTile, myCivId: string | null, claimable: boolean): string {
  const parts = [`${tile.biome} · yield ${Math.round(tile.resourceYield)}`];
  if (tile.controllingCivilizationId) {
    parts.push(tile.controllingCivilizationId === myCivId ? 'yours' : 'occupied');
  } else if (claimable) {
    parts.push('click to claim');
  } else {
    parts.push('neutral');
  }
  return parts.join(' · ');
}

interface HexMapViewProps {
  map: HexMap;
  myCivilizationId: string | null;
  disabled?: boolean;
  onClaim?: (q: number, r: number) => void | Promise<void>;
  onSelectionChange?: (tile: HexTile | null, meta: string | null) => void;
  showLegend?: boolean;
}

export function HexMapView({
  map,
  myCivilizationId,
  disabled,
  onClaim,
  onSelectionChange,
  showLegend = true,
}: HexMapViewProps) {
  const [selected, setSelected] = useState<HexTile | null>(null);

  const layout = useMemo(() => {
    const positioned = map.tiles.map((tile) => {
      const { x, y } = axialToPixel(tile.q, tile.r, HEX_SIZE);
      return { tile, x, y };
    });
    const xs = positioned.map((p) => p.x);
    const ys = positioned.map((p) => p.y);
    const minX = Math.min(...xs) - HEX_SIZE * 1.4;
    const maxX = Math.max(...xs) + HEX_SIZE * 1.4;
    const minY = Math.min(...ys) - HEX_SIZE * 1.4;
    const maxY = Math.max(...ys) + HEX_SIZE * 1.4;
    return { positioned, minX, minY, width: maxX - minX, height: maxY - minY };
  }, [map.tiles]);

  const claimable = selected && canClaim(selected, map, myCivilizationId);
  const meta = selected ? tileMeta(selected, myCivilizationId, !!claimable) : null;

  const selectTile = (tile: HexTile) => {
    setSelected(tile);
    const can = canClaim(tile, map, myCivilizationId);
    onSelectionChange?.(tile, tileMeta(tile, myCivilizationId, can));
  };

  const presentBiomes = useMemo(() => {
    const set = new Set(map.tiles.map((t) => t.biome));
    return LEGEND_BIOMES.filter((b) => set.has(b));
  }, [map.tiles]);

  return (
    <div className="hex-map-wrap">
      <svg
        className="hex-map"
        viewBox={`${layout.minX} ${layout.minY} ${layout.width} ${layout.height}`}
        preserveAspectRatio="xMidYMid meet"
        role="img"
        aria-label="Territory map"
      >
        {layout.positioned.map(({ tile, x, y }) => {
          const fill = BIOME_COLORS[tile.biome] ?? '#64748b';
          const owner = tile.controllingCivilizationId
            ? CIV_COLORS[tile.controllingCivilizationId] ?? 'rgba(148, 163, 184, 0.45)'
            : null;
          const isSelected = selected?.q === tile.q && selected?.r === tile.r;
          const clickable = !!onClaim && canClaim(tile, map, myCivilizationId);

          return (
            <g key={`${tile.q},${tile.r}`}>
              <polygon
                points={hexCorners(x, y, HEX_SIZE)}
                fill={fill}
                fillOpacity={owner ? 0.55 : 0.35}
                stroke={isSelected ? '#e0e2ec' : `${fill}80`}
                strokeWidth={isSelected ? 2.2 : 1.4}
                className={clickable ? 'hex-tile hex-tile-claimable' : 'hex-tile'}
                onClick={() => {
                  selectTile(tile);
                  if (clickable && onClaim && !disabled) void onClaim(tile.q, tile.r);
                }}
              />
              {owner && (
                <polygon
                  points={hexCorners(x, y, HEX_SIZE * 0.82)}
                  fill={owner}
                  stroke="none"
                  pointerEvents="none"
                />
              )}
              {tile.isCapital && (
                <circle cx={x} cy={y} r={5} fill="#fef08a" stroke="#713f12" strokeWidth={0.75} />
              )}
            </g>
          );
        })}
      </svg>
      {meta && <p className="hex-map-meta-hidden" aria-live="polite">{meta}</p>}
      {showLegend && presentBiomes.length > 0 && (
        <div className="map-biome-legend">
          {presentBiomes.map((biome) => (
            <div key={biome} className="map-biome-legend-item">
              <span className="map-biome-dot" style={{ background: BIOME_COLORS[biome] }} />
              <span>{biome}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function defaultTerritoryHint(): string {
  return 'Select a tile on the map';
}
