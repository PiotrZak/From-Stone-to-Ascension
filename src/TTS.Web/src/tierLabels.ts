export const TIER_LABELS: Record<number, string> = {
  1: 'Pre-Industrial',
  2: 'Industrial Age',
  3: 'Early Electronics',
  4: 'Information Age',
  5: 'Early AI',
  6: 'Bio / Nano',
  7: 'Temporal',
  8: 'Post-Singularity',
};

export function tierLabel(tier: number): string {
  return TIER_LABELS[tier] ?? `TTS ${tier}`;
}

export function tierClass(tier: number): string {
  return `tier-band-${Math.min(8, Math.max(1, tier))}`;
}
