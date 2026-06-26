import { useEffect, useState } from 'react';

function tickIntervalSec(modeId: string): number {
  switch (modeId) {
    case 'dev-blitz-3m':
    case 'dev-blitz':
    case 'dev':
      return 30;
    case 'blitz-24h':
    case 'blitz':
      return 4 * 3600;
    case 'standard-36h':
    case 'standard':
      return 3 * 3600;
    case 'extended-48h':
    case 'extended':
      return 4 * 3600;
    case 'classic-stone':
    case 'classic':
      return 3 * 3600;
    default:
      return 3600;
  }
}

function formatClock(totalSec: number): string {
  const h = Math.floor(totalSec / 3600);
  const m = Math.floor((totalSec % 3600) / 60);
  const s = totalSec % 60;
  if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  return `${m}:${String(s).padStart(2, '0')}`;
}

const RING_R = 21;
const RING_C = 2 * Math.PI * RING_R;

interface TickClockProps {
  modeId: string;
  nextTickAt: string;
  isTickDue: boolean;
  tickCount: number;
  maxTicks: number;
}

export function TickClock({ modeId, nextTickAt, isTickDue, tickCount, maxTicks }: TickClockProps) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  const intervalSec = tickIntervalSec(modeId);
  const secRemaining = Math.max(0, Math.floor((new Date(nextTickAt).getTime() - now) / 1000));
  const due = isTickDue || secRemaining === 0;
  const progress = due ? 1 : Math.min(1, Math.max(0, 1 - secRemaining / intervalSec));
  const seasonProgress = maxTicks > 0 ? tickCount / maxTicks : 0;
  const dashOffset = RING_C * (1 - progress);

  return (
    <div className={`tick-clock-hud${due ? ' tick-clock-hud-due' : ''}`} aria-live="polite">
      <div className="tick-clock-hud-meta">
        <p className="label-caps">Tick {tickCount}/{maxTicks}</p>
        <div className="tick-season-bar">
          <div className="tick-season-fill" style={{ width: `${Math.round(seasonProgress * 100)}%` }} />
        </div>
      </div>
      <div className="tick-clock-hud-ring-wrap">
        <svg className="tick-clock-hud-ring" viewBox="0 0 48 48" aria-hidden>
          <circle cx="24" cy="24" r={RING_R} fill="transparent" stroke="#30363d" strokeWidth="2.5" />
          <circle
            cx="24"
            cy="24"
            r={RING_R}
            fill="transparent"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeDasharray={RING_C}
            strokeDashoffset={dashOffset}
            className="tick-clock-hud-progress"
          />
        </svg>
        <span className="tick-clock-hud-value">{due ? 'NOW' : formatClock(secRemaining)}</span>
      </div>
    </div>
  );
}

export function formatGateCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'EXPIRED';
  if (sec < 3600) return `${formatClock(sec)} REMAINING`;
  const h = Math.floor(sec / 3600);
  const m = Math.floor((sec % 3600) / 60);
  return `${h}H ${m}M REMAINING`;
}
