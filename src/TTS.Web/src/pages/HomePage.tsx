import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, saveSession } from '../api';

export function HomePage() {
  const navigate = useNavigate();
  const [matches, setMatches] = useState<Awaited<ReturnType<typeof api.listMatches>>>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [playerName, setPlayerName] = useState('Governor');
  const [joinCode, setJoinCode] = useState('');
  const [modeId, setModeId] = useState('dev-blitz-3m');
  const [busy, setBusy] = useState(false);

  const refresh = async () => {
    try {
      setMatches(await api.listMatches());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load matches');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

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
      const created = await api.createMatch(modeId, true);
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
    <div className="card">
      <div className="field">
        <label htmlFor="playerName">Name</label>
        <input
          id="playerName"
          value={playerName}
          onChange={(e) => setPlayerName(e.target.value)}
          placeholder="Governor"
        />
      </div>

      <div className="row">
        <select value={modeId} onChange={(e) => setModeId(e.target.value)} aria-label="Mode">
          <option value="dev-blitz-3m">Dev 3 min</option>
          <option value="sprint-8h">Sprint 8h</option>
          <option value="blitz-24h">Blitz 24h</option>
          <option value="standard-36h">Standard 36h</option>
          <option value="extended-48h">Extended 48h</option>
        </select>
        <button className="btn btn-primary" disabled={busy} onClick={() => void handleCreate()}>
          Create
        </button>
      </div>

      <div className="row join-row">
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

      {error && <p className="error">{error}</p>}

      <hr className="divider" />

      {loading ? (
        <p className="muted">Loading…</p>
      ) : matches.length === 0 ? (
        <p className="muted">No matches yet.</p>
      ) : (
        <ul className="simple-list">
          {matches.map((m) => (
            <li key={m.matchId}>
              <button type="button" className="list-link" onClick={() => navigate(`/match/${m.matchId}`)}>
                <span>{m.modeDisplayName}</span>
                <span className="muted">
                  {m.joinCode} · {m.status.toLowerCase()} · {m.tickCount}/{m.maxTicks}
                  {m.pendingGateCount > 0 ? ' · decision pending' : ''}
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
