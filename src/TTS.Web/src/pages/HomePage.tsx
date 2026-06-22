import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, loadSession, saveSession, type MatchListItem } from '../api';
import { llmStatusShort, llmStatusTone } from '../llmStatus';

function statusLabel(status: string): string {
  switch (status) {
    case 'Lobby': return 'Lobby';
    case 'Active': return 'In progress';
    case 'Running': return 'In progress';
    case 'Ended': return 'Finished';
    default: return status;
  }
}

function statusClass(status: string): string {
  const key = status.toLowerCase();
  if (key === 'running') return 'active';
  return key;
}

function matchSortKey(m: MatchListItem): number {
  const status = m.status.toLowerCase();
  if (status === 'active' || status === 'running') {
    return m.pendingGateCount > 0 ? 0 : 1;
  }
  if (status === 'lobby') return 2;
  return 3;
}

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function MatchCard({
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
  const ended = match.status.toLowerCase() === 'ended';
  const inLobby = match.status.toLowerCase() === 'lobby';
  const inProgress = !ended && !inLobby;
  const tickPct = match.maxTicks > 0 ? Math.round((match.tickCount / match.maxTicks) * 100) : 0;
  const needsAction = match.pendingGateCount > 0;

  return (
    <article className={`match-card${needsAction ? ' match-card-action' : ''}`}>
      <div className="match-card-top">
        <div className="match-card-title-block">
          <h3 className="match-card-title">{match.modeDisplayName}</h3>
          {session && (
            <p className="match-card-you muted">
              You · {session.civilizationName}
            </p>
          )}
        </div>
        <span className={`badge badge-status badge-${statusClass(match.status)}`}>
          {statusLabel(match.status)}
        </span>
        {inProgress && match.llmStatus && (
          <span className={`badge badge-llm badge-llm-${llmStatusTone(match.llmStatus)}`} title={match.llmStatus.statusMessage}>
            {llmStatusShort(match.llmStatus)}
          </span>
        )}
      </div>

      <div className="match-card-code-row">
        <button
          type="button"
          className="match-card-code"
          onClick={(e) => {
            e.stopPropagation();
            onCopy();
          }}
          title="Copy join code"
        >
          <span className="join-code-label">Code</span>
          <span className="join-code-value">{match.joinCode}</span>
        </button>
        <span className="muted match-card-copy-hint">{copied ? 'Copied' : 'Tap to copy'}</span>
      </div>

      <div className="match-card-stats">
        <div className="match-card-stat">
          <span className="match-card-stat-label">Players</span>
          <strong>{match.playerCount}/{match.maxPlayers}</strong>
        </div>
        {inProgress && (
          <div className="match-card-stat match-card-stat-grow">
            <span className="match-card-stat-label">Progress</span>
            <strong>Tick {match.tickCount}/{match.maxTicks}</strong>
          </div>
        )}
        {inLobby && (
          <div className="match-card-stat">
            <span className="match-card-stat-label">Stage</span>
            <strong>Waiting to start</strong>
          </div>
        )}
        {ended && (
          <div className="match-card-stat">
            <span className="match-card-stat-label">Result</span>
            <strong>{match.tickCount}/{match.maxTicks} ticks</strong>
          </div>
        )}
      </div>

      {inProgress && (
        <div className="match-card-progress">
          <div className="stability-track tick-progress-track">
            <div className="stability-fill stability-good" style={{ width: `${tickPct}%` }} />
          </div>
        </div>
      )}

      {inProgress && match.llmStatus && (
        <p className="match-card-llm muted">{match.llmStatus.statusMessage}</p>
      )}

      {needsAction && (
        <p className="match-card-alert">
          Decision required
          {match.nextGateExpiresAt ? ` · ${formatCountdown(match.nextGateExpiresAt)} left` : ''}
        </p>
      )}

      <button type="button" className="btn btn-primary match-card-open" onClick={onOpen}>
        {needsAction ? 'Resolve decision' : session ? 'Open match' : 'View match'}
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
    } catch {
      /* ignore */
    }
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
      <section className="card home-setup">
        <h2 className="home-section-title">Play</h2>
        <p className="muted home-lead">Create a match, share the code, or join an existing game.</p>

        <div className="field">
          <label htmlFor="playerName">Your name</label>
          <input
            id="playerName"
            value={playerName}
            onChange={(e) => setPlayerName(e.target.value)}
            placeholder="Governor"
          />
        </div>

        <div className="home-setup-grid">
          <div className="home-setup-block">
            <p className="home-setup-label">Create new match</p>
            <div className="row">
              <select value={modeId} onChange={(e) => setModeId(e.target.value)} aria-label="Mode">
                <option value="sprint-8h">Sprint 8h</option>
                <option value="blitz-24h">Blitz 24h</option>
                <option value="standard-36h">Standard 36h</option>
                <option value="extended-48h">Extended 48h</option>
                <option value="classic-stone">From Stone (Classic)</option>
                <option value="dev-blitz-3m">Dev 3 min</option>
              </select>
              <button className="btn btn-primary" disabled={busy} onClick={() => void handleCreate()}>
                Create
              </button>
            </div>
          </div>

          <div className="home-setup-block">
            <p className="home-setup-label">Join with code</p>
            <div className="row join-row">
              <input
                value={joinCode}
                onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                placeholder="ABCD12"
                aria-label="Join code"
              />
              <button className="btn" disabled={busy || !joinCode.trim()} onClick={() => void handleJoin()}>
                Join
              </button>
            </div>
          </div>
        </div>

        {error && <p className="error">{error}</p>}
      </section>

      <section className="home-matches">
        <div className="home-matches-head">
          <div>
            <h2 className="home-section-title">Matches</h2>
            <p className="muted home-lead">
              {loading ? 'Loading…' : matches.length === 0
                ? 'No matches yet — create one above.'
                : `${matches.length} match${matches.length === 1 ? '' : 'es'}${actionCount > 0 ? ` · ${actionCount} need${actionCount === 1 ? 's' : ''} your decision` : ''}`}
            </p>
          </div>
          <button type="button" className="btn" disabled={loading} onClick={() => void refresh()}>
            Refresh
          </button>
        </div>

        {!loading && sortedMatches.length > 0 && (
          <div className="match-list">
            {sortedMatches.map((m) => (
              <MatchCard
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
