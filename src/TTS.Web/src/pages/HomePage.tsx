import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { CollapsibleSection } from '../components/CollapsibleSection';
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

function matchGroup(status: string, needsAction: boolean): 'action' | 'active' | 'lobby' | 'ended' {
  if (needsAction) return 'action';
  const key = status.toLowerCase();
  if (key === 'lobby') return 'lobby';
  if (key === 'ended') return 'ended';
  return 'active';
}

const GROUP_LABELS: Record<string, string> = {
  action: 'Needs your decision',
  active: 'In progress',
  lobby: 'Lobby',
  ended: 'Finished',
};

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function matchSummaryLine(match: MatchListItem): string {
  const ended = match.status.toLowerCase() === 'ended';
  const inLobby = match.status.toLowerCase() === 'lobby';
  if (inLobby) return `${match.playerCount}/${match.maxPlayers} players · waiting to start`;
  if (ended) return `Finished · ${match.tickCount}/${match.maxTicks} ticks`;
  const parts = [`Tick ${match.tickCount}/${match.maxTicks}`, `${match.playerCount} players`];
  if (match.pendingGateCount > 0) parts.unshift(`${match.pendingGateCount} gate${match.pendingGateCount === 1 ? '' : 's'}`);
  return parts.join(' · ');
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
  const needsAction = match.pendingGateCount > 0;
  const inProgress = !['ended', 'lobby'].includes(match.status.toLowerCase());
  const tickPct = match.maxTicks > 0 ? Math.round((match.tickCount / match.maxTicks) * 100) : 0;

  return (
    <article className={`match-card${needsAction ? ' match-card-action' : ''}`}>
      <div className="match-card-primary">
        <div className="match-card-headline">
          <h3 className="match-card-title">{match.modeDisplayName}</h3>
          <p className="muted match-card-summary">{matchSummaryLine(match)}</p>
          {session && (
            <p className="match-card-you muted">You · {session.civilizationName}</p>
          )}
        </div>
        <div className="match-card-badges">
          <span className={`badge badge-status badge-${statusClass(match.status)}`}>
            {statusLabel(match.status)}
          </span>
          {inProgress && match.llmStatus && (
            <span
              className={`badge badge-llm badge-llm-${llmStatusTone(match.llmStatus)}`}
              title={match.llmStatus.statusMessage}
            >
              {llmStatusShort(match.llmStatus)}
            </span>
          )}
        </div>
      </div>

      {needsAction && (
        <p className="match-card-alert">
          Decision required
          {match.nextGateExpiresAt ? ` · ${formatCountdown(match.nextGateExpiresAt)} left` : ''}
        </p>
      )}

      {inProgress && (
        <div className="match-card-progress">
          <div className="stability-track tick-progress-track">
            <div className="stability-fill stability-good" style={{ width: `${tickPct}%` }} />
          </div>
        </div>
      )}

      <div className="match-card-actions">
        <button type="button" className="btn btn-primary" onClick={onOpen}>
          {needsAction ? 'Resolve decision' : session ? 'Open match' : 'View match'}
        </button>
      </div>

      <CollapsibleSection
        title="Match details"
        subtitle={`Code ${match.joinCode}`}
        className="match-card-details"
      >
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
        {inProgress && match.llmStatus && (
          <p className="match-card-llm muted">{match.llmStatus.statusMessage}</p>
        )}
      </CollapsibleSection>
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

  const groupedMatches = useMemo(() => {
    const groups: Record<string, MatchListItem[]> = { action: [], active: [], lobby: [], ended: [] };
    for (const m of matches) {
      const key = matchGroup(m.status, m.pendingGateCount > 0);
      groups[key].push(m);
    }
    const sortByTick = (a: MatchListItem, b: MatchListItem) => b.tickCount - a.tickCount;
    groups.action.sort(sortByTick);
    groups.active.sort(sortByTick);
    groups.lobby.sort((a, b) => b.playerCount - a.playerCount);
    groups.ended.sort(sortByTick);
    return groups;
  }, [matches]);

  const actionCount = matches.filter((m) => m.pendingGateCount > 0).length;
  const playDefaultOpen = matches.length === 0;

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

  const groupOrder = ['action', 'active', 'lobby', 'ended'] as const;

  return (
    <div className="home-page">
      <CollapsibleSection
        title="Create or join"
        subtitle="New match · join code"
        defaultOpen={playDefaultOpen}
        className="card home-setup"
      >
        <p className="muted home-lead">Share a code with friends or join an existing game.</p>

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
      </CollapsibleSection>

      <section className="home-matches">
        <div className="home-matches-head">
          <div>
            <h2 className="home-section-title">Your matches</h2>
            <p className="muted home-lead home-matches-sub">
              {loading ? 'Loading…' : matches.length === 0
                ? 'No matches yet.'
                : actionCount > 0
                  ? `${actionCount} need${actionCount === 1 ? 's' : ''} your decision`
                  : `${matches.length} match${matches.length === 1 ? '' : 'es'}`}
            </p>
          </div>
          <button type="button" className="btn btn-ghost" disabled={loading} onClick={() => void refresh()}>
            Refresh
          </button>
        </div>

        {!loading && matches.length > 0 && (
          <div className="match-groups">
            {groupOrder.map((key) => {
              const items = groupedMatches[key];
              if (items.length === 0) return null;
              return (
                <div key={key} className="match-group">
                  <h3 className="match-group-label">{GROUP_LABELS[key]}</h3>
                  <div className="match-list">
                    {items.map((m) => (
                      <MatchCard
                        key={m.matchId}
                        match={m}
                        copied={copiedId === m.matchId}
                        onCopy={() => void copyCode(m.matchId, m.joinCode)}
                        onOpen={() => navigate(`/match/${m.matchId}`)}
                      />
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
