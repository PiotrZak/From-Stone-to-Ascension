import type { HexMap, HexTile } from '../../api';

/** Data-driven biome palette (spec: Forest, Aquatic, Arid + extended sim biomes). */
export const BIOME_COLORS: Record<string, string> = {
  Ocean: '#0c4a6e',
  Coast: '#155e75',
  Plains: '#3d5a40',
  Forest: '#15803d',
  Hills: '#57534e',
  Mountains: '#44403c',
  Desert: '#a16207',
  Tundra: '#64748b',
  Wetlands: '#0f766e',
};

export const BIOME_LABELS: Record<string, string> = {
  Ocean: 'Aquatic',
  Desert: 'Arid',
  Forest: 'Forest',
  Plains: 'Plains',
};

export const LEGEND_BIOMES = ['Forest', 'Desert', 'Plains', 'Ocean'] as const;

export const CIV_FILL_COLORS: Record<string, string> = {
  'civ-player': 'rgba(76, 215, 246, 0.45)',
  'civ-rival': 'rgba(255, 107, 107, 0.45)',
};

export const DEFAULT_HEX_SIZE = 34;

/** Normalized elevation per biome (0 = lowland, 1 = peak). */
export const BIOME_ELEVATION: Record<string, number> = {
  Ocean: 0.04,
  Coast: 0.14,
  Wetlands: 0.18,
  Plains: 0.32,
  Desert: 0.36,
  Forest: 0.44,
  Tundra: 0.4,
  Hills: 0.68,
  Mountains: 1,
};

export function biomeElevation(biome: string): number {
  return BIOME_ELEVATION[biome] ?? 0.32;
}

export function tileExtrusionHeight(hexSize: number): number {
  return hexSize * 0.34;
}

export function axialToPixel(q: number, r: number, size: number) {
  const x = size * (3 / 2) * q;
  const y = size * (Math.sqrt(3) * (r + q / 2));
  return { x, y };
}

export function hexCornerPoints(cx: number, cy: number, size: number): { x: number; y: number }[] {
  const points: { x: number; y: number }[] = [];
  for (let i = 0; i < 6; i++) {
    const angle = (Math.PI / 180) * (60 * i);
    points.push({ x: cx + size * Math.cos(angle), y: cy + size * Math.sin(angle) });
  }
  return points;
}

export function tileKey(tile: HexTile | string): string {
  return typeof tile === 'string' ? tile : tile.id;
}

export function biomeLabel(biome: string): string {
  return BIOME_LABELS[biome] ?? biome;
}

export function canClaim(tile: HexTile, map: HexMap, myCivId: string | null): boolean {
  if (!myCivId || tile.controllingCivilizationId || tile.biome === 'Ocean') return false;

  return (tile.neighbourIds ?? []).some(
    (id) => map.tiles.find((t) => t.id === id)?.controllingCivilizationId === myCivId,
  );
}

export function tileMeta(tile: HexTile, myCivId: string | null, claimable: boolean): string {
  const parts: string[] = [];
  if (tile.worldRegionName) parts.push(tile.worldRegionName);
  parts.push(`${biomeLabel(tile.biome)} · yield ${Math.round(tile.resourceYield)}`);
  if (tile.controllingCivilizationId) {
    parts.push(tile.controllingCivilizationId === myCivId ? 'yours' : 'occupied');
  } else if (claimable) {
    parts.push('click to claim');
  } else if (tile.biome !== 'Ocean') {
    parts.push('neutral');
  }
  return parts.join(' · ');
}

export type MapLayout = {
  positioned: { tile: HexTile; x: number; y: number }[];
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
  width: number;
  height: number;
};

export function computeMapLayout(tiles: HexTile[], _hexSize: number): MapLayout {
  const positioned = tiles.map((tile) => ({
    tile,
    x: tile.centerX,
    y: tile.centerZ,
  }));
  const xs = positioned.map((p) => p.x);
  const ys = positioned.map((p) => p.y);
  const pad = 10;
  const minX = Math.min(...xs) - pad;
  const maxX = Math.max(...xs) + pad;
  const minY = Math.min(...ys) - pad;
  const maxY = Math.max(...ys) + pad;
  return {
    positioned,
    minX,
    minY,
    maxX,
    maxY,
    width: maxX - minX,
    height: maxY - minY,
  };
}

export function parseCssColor(css: string): { color: number; alpha: number } {
  if (css.startsWith('#')) {
    const hex = css.slice(1);
    const full =
      hex.length === 3
        ? hex
            .split('')
            .map((c) => c + c)
            .join('')
        : hex;
    return { color: Number.parseInt(full, 16), alpha: 1 };
  }

  const match = css.match(/rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*([\d.]+))?\s*\)/);
  if (match) {
    const r = Number(match[1]);
    const g = Number(match[2]);
    const b = Number(match[3]);
    const alpha = match[4] !== undefined ? Number(match[4]) : 1;
    return { color: (r << 16) | (g << 8) | b, alpha };
  }

  return { color: 0x64748b, alpha: 1 };
}
