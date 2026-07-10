import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AlertCircle, Check, Copy, Loader2, Swords } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { api, loadSession, saveSession, type MatchListItem } from '../api';
import { cn } from '@/lib/utils';

const MODES = [
  { id: 'sprint-8h', label: 'Sprint 8h' },
  { id: 'blitz-24h', label: 'Blitz 24h' },
  { id: 'standard-36h', label: 'Standard 36h' },
  { id: 'extended-48h', label: 'Extended 48h' },
  { id: 'classic-stone', label: 'Classic' },
  { id: 'dev-blitz-3m', label: 'Dev 3m' },
] as const;

const homeCard = 'border-border bg-card shadow-none';
const homeField = 'border-input bg-[var(--surface-container-low)] shadow-none focus-visible:ring-1 focus-visible:ring-ring';

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function matchSortKey(m: MatchListItem): number {
  if (m.pendingGateCount > 0) return 0;
  const key = m.status.toLowerCase();
  if (key === 'running' || key === 'active') return 1;
  if (key === 'lobby') return 2;
  return 3;
}

function matchLine(match: MatchListItem, sessionName?: string): string {
  const parts: string[] = [];
  if (sessionName) parts.push(sessionName);
  if (match.pendingGateCount > 0) {
    parts.push(`decision · ${match.nextGateExpiresAt ? formatCountdown(match.nextGateExpiresAt) : 'due'}`);
  } else if (match.status.toLowerCase() === 'lobby') {
    parts.push(`${match.playerCount}/${match.maxPlayers} players`);
  } else if (match.status.toLowerCase() === 'ended') {
    parts.push(`finished · tick ${match.tickCount}`);
  } else {
    parts.push(`tick ${match.tickCount}/${match.maxTicks}`);
  }
  return parts.join(' · ');
}

function MatchRow({
  match,
  onOpen,
  onCopy,
  copied,
}: {
  match: MatchListItem;
  onOpen: () => void;
  onCopy: () => void;
  copied: boolean;
}) {
  const session = loadSession(match.matchId);
  const needsAction = match.pendingGateCount > 0;

  return (
    <Card
      className={cn(
        homeCard,
        'cursor-pointer transition-colors hover:bg-muted/20',
        needsAction && 'border-amber-500/20 bg-amber-500/[0.04] hover:bg-amber-500/[0.07]',
      )}
      onClick={onOpen}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onOpen();
        }
      }}
      role="button"
      tabIndex={0}
    >
      <CardContent className="flex items-center gap-3 p-4">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted/40">
          <Swords className="h-4 w-4 text-muted-foreground/80" />
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-medium leading-tight text-foreground/90">{match.modeDisplayName}</p>
            {needsAction && <Badge variant="warning">Decision due</Badge>}
          </div>
          <p className="mt-1 truncate text-sm text-muted-foreground">{matchLine(match, session?.civilizationName)}</p>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="shrink-0 font-mono text-xs text-muted-foreground hover:text-foreground"
          onClick={(e) => {
            e.stopPropagation();
            onCopy();
          }}
          title="Copy join code"
        >
          {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
          {match.joinCode}
        </Button>
      </CardContent>
    </Card>
  );
}

export function HomePage() {
  const navigate = useNavigate();
  const [matches, setMatches] = useState<MatchListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [playerName, setPlayerName] = useState('Governor');
  const [joinCode, setJoinCode] = useState('');
  const [modeId, setModeId] = useState('sprint-8h');
  const [busy, setBusy] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      setMatches(await api.listMatches());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load matches');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
    const timer = setInterval(() => void refresh(), 12000);
    return () => clearInterval(timer);
  }, [refresh]);

  const sortedMatches = useMemo(
    () => [...matches].sort((a, b) => matchSortKey(a) - matchSortKey(b) || b.tickCount - a.tickCount),
    [matches],
  );

  const actionCount = matches.filter((m) => m.pendingGateCount > 0).length;

  const copyCode = async (matchId: string, code: string) => {
    try {
      await navigator.clipboard.writeText(code);
      setCopiedId(matchId);
      setTimeout(() => setCopiedId(null), 2000);
    } catch { /* ignore */ }
  };

  const join = async (matchId: string) => {
    const result = await api.joinMatch(matchId, playerName);
    saveSession(matchId, {
      playerId: result.playerId,
      playerName: result.playerName,
      civilizationId: result.civilizationId,
      civilizationName: result.civilizationName,
    });
    navigate(`/match/${matchId}`);
  };

  const handleCreate = async () => {
    setBusy(true);
    setError(null);
    try {
      const created = await api.createMatch(modeId, false);
      await join(created.matchId);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create match');
    } finally {
      setBusy(false);
    }
  };

  const handleJoin = async () => {
    if (!joinCode.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const result = await api.joinByCode(joinCode.trim(), playerName);
      saveSession(result.matchId, {
        playerId: result.playerId,
        playerName: result.playerName,
        civilizationId: result.civilizationId,
        civilizationName: result.civilizationName,
      });
      navigate(`/match/${result.matchId}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to join match');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="home-page mx-auto max-w-2xl space-y-10">
      <div className="space-y-2">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground/90">Briefing room</h1>
        <p className="max-w-xl text-sm text-muted-foreground">
          2–5 minute command sessions. Create a match, join with a code, or resume before the next tick.
        </p>
        <Button variant="outline" size="sm" asChild className="mt-2">
          <Link to="/trade">China–Africa trade globe</Link>
        </Button>
      </div>

      <Card className={homeCard}>
        <CardHeader className="pb-4">
          <CardTitle className="text-base font-medium text-foreground/90">Play</CardTitle>
          <CardDescription>Your governor name is shown to other players in the lobby.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="playerName" className="text-muted-foreground">Governor name</Label>
              <Input
                id="playerName"
                className={homeField}
                value={playerName}
                onChange={(e) => setPlayerName(e.target.value)}
                placeholder="Governor"
              />
            </div>
            <div className="space-y-2">
              <Label className="text-muted-foreground">Match mode</Label>
              <Select value={modeId} onValueChange={setModeId}>
                <SelectTrigger className={homeField}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="border-border/40 bg-popover/95 backdrop-blur-sm">
                  {MODES.map((m) => (
                    <SelectItem key={m.id} value={m.id}>
                      {m.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button disabled={busy} onClick={() => void handleCreate()}>
              {busy ? <Loader2 className="animate-spin" /> : null}
              New match
            </Button>
          </div>

          <div className="rounded-lg border border-border/20 bg-muted/10 p-4">
            <p className="mb-3 text-xs font-medium uppercase tracking-wide text-muted-foreground">Join with code</p>
            <div className="flex flex-col gap-2 sm:flex-row">
              <Input
                value={joinCode}
                onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                placeholder="Join code"
                aria-label="Join code"
                className={cn(homeField, 'font-mono uppercase sm:max-w-xs')}
              />
              <Button variant="secondary" disabled={busy || !joinCode.trim()} onClick={() => void handleJoin()}>
                Join match
              </Button>
            </div>
          </div>

          {error && (
            <Alert variant="destructive" className="border-destructive/30 bg-destructive/10">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>

      <section className="space-y-3">
        <h2 className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
          {loading ? 'Loading matches…' : actionCount > 0
            ? `${actionCount} match${actionCount === 1 ? '' : 'es'} need a decision`
            : sortedMatches.length === 0
              ? 'No matches yet'
              : `${sortedMatches.length} active match${sortedMatches.length === 1 ? '' : 'es'}`}
        </h2>

        {!loading && sortedMatches.length > 0 && (
          <div className="grid gap-2">
            {sortedMatches.map((m) => (
              <MatchRow
                key={m.matchId}
                match={m}
                copied={copiedId === m.matchId}
                onCopy={() => void copyCode(m.matchId, m.joinCode)}
                onOpen={() => navigate(`/match/${m.matchId}`)}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
