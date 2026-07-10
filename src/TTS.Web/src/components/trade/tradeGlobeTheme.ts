export type TradeGlobeTheme = 'dark' | 'light';

export const TRADE_GLOBE_THEME_KEY = 'trade-globe-theme';

export type TradeGlobeThemePalette = {
  sceneBg: number;
  fog: number;
  ocean: number;
  landFallback: number;
  inactiveLand: number;
  selection: number;
  ambient: number;
  ambientIntensity: number;
  sun: number;
  sunIntensity: number;
  hubEmissive: number;
  tileEmissiveActive: number;
  light: boolean;
};

export const TRADE_GLOBE_PALETTES: Record<TradeGlobeTheme, TradeGlobeThemePalette> = {
  dark: {
    sceneBg: 0x0a1e32,
    fog: 0x0a1e32,
    ocean: 0x0c4a6e,
    landFallback: 0x334155,
    inactiveLand: 0x1e293b,
    selection: 0x4cd7f6,
    ambient: 0xc8d8f0,
    ambientIntensity: 0.78,
    sun: 0xfff8ee,
    sunIntensity: 1.45,
    hubEmissive: 0x222222,
    tileEmissiveActive: 0x0a1520,
    light: false,
  },
  light: {
    sceneBg: 0xc5d8eb,
    fog: 0xc5d8eb,
    ocean: 0x0369a1,
    landFallback: 0x475569,
    inactiveLand: 0xa8b8c8,
    selection: 0x0e7490,
    ambient: 0xf8fafc,
    ambientIntensity: 0.55,
    sun: 0xfff7ed,
    sunIntensity: 1.5,
    hubEmissive: 0x334155,
    tileEmissiveActive: 0x7dd3fc,
    light: true,
  },
};

export function readTradeGlobeTheme(): TradeGlobeTheme {
  try {
    const stored = localStorage.getItem(TRADE_GLOBE_THEME_KEY);
    if (stored === 'light' || stored === 'dark') return stored;
  } catch {
    /* ignore */
  }
  return 'dark';
}
