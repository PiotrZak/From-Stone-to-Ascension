import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, loadSession, saveSession, type MatchListItem } from '../api';

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
    <article
      className={`match-row${needsAction ? ' match-row-action' : ''}`}
      role="button"
      tabIndex={0}
      onClick={onOpen}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onOpen(); } }}
    >
      <div className="match-row-main">
        <span className="match-row-title">{match.modeDisplayName}</span>
        <span className="muted match-row-meta">{matchLine(match, session?.civilizationName)}</span>
      </div>
      <button
        type="button"
        className="match-row-code"
        onClick={(e) => { e.stopPropagation(); onCopy(); }}
        title="Copy join code"
      >
        {copied ? '✓' : match.joinCode}
      </button>
    </article>
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
    <div className="home-page">
      <section className="card home-bar">
        <input
          id="playerName"
          className="home-bar-name"
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
          placeholder="Your name"
          aria-label="Your name"
        />
        <div className="home-bar-create row">
          <select value={modeId} onChange={(e) => setModeId(e.target.value)} aria-label="Mode">
            <option value="sprint-8h">Sprint 8h</option>
            <option value="blitz-24h">Blitz 24h</option>
            <option value="standard-36h">Standard 36h</option>
            <option value="extended-48h">Extended 48h</option>
            <option value="classic-stone">Classic</option>
            <option value="dev-blitz-3m">Dev 3m</option>
          </select>
          <button className="btn btn-primary" disabled={busy} onClick={() => void handleCreate()}>
            New
          </button>
        </div>
        <div className="home-bar-join row join-row">
          <input
            value={joinCode}
            onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
            placeholder="Join code"
            aria-label="Join code"
          />
          <button className="btn" disabled={busy || !joinCode.trim()} onClick={() => void handleJoin()}>
            Join
          </button>
        </div>
        {error && <p className="error home-bar-error">{error}</p>}
      </section>

      <section className="home-matches">
        <p className="home-matches-label muted">
          {loading ? 'Loading…' : actionCount > 0
            ? `${actionCount} need${actionCount === 1 ? 's' : ''} a decision`
            : sortedMatches.length === 0
              ? 'No matches yet'
              : `${sortedMatches.length} match${sortedMatches.length === 1 ? '' : 'es'}`}
        </p>

        {!loading && sortedMatches.length > 0 && (
          <div className="match-list">
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
