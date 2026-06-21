import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  api,
  loadSession,
  POLICY_PRESETS,
  saveSession,
  type CivDashboard,
  type MatchSummary,
  type PlayerSession,
} from '../api';

function formatCountdown(targetIso: string): string {
  const sec = Math.max(0, Math.floor((new Date(targetIso).getTime() - Date.now()) / 1000));
  if (sec === 0) return 'now';
  if (sec < 3600) return `${Math.floor(sec / 60)}m ${sec % 60}s`;
  return `${Math.floor(sec / 3600)}h ${Math.floor((sec % 3600) / 60)}m`;
}

function civLine(c: { name: string; tier: number; averageStability: number; techCount: number }) {
  return `${c.name} · TTS ${c.tier} · stability ${Math.round(c.averageStability)} · ${c.techCount} techs`;
}

export function MatchPage() {
  const { matchId = '' } = useParams();
  const [summary, setSummary] = useState<MatchSummary | null>(null);
  const [dashboard, setDashboard] = useState<CivDashboard | null>(null);
  const [session, setSession] = useState<PlayerSession | null>(() => loadSession(matchId));
  const [playerName, setPlayerName] = useState('Governor');
  const [policyPreset, setPolicyPreset] = useState('balanced');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const civId = session?.civilizationId ?? 'civ-player';

  const refresh = useCallback(async () => {
    if (!matchId) return;
    try {
      const next = await api.getMatch(matchId);
      setSummary(next);
      if (session || loadSession(matchId)) {
        const dash = await api.getCivDashboard(matchId, civId);
        setDashboard(dash);
        setPolicyPreset(dash.presetId === 'custom' ? 'balanced' : dash.presetId);
      }
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load match');
    } finally {
      setLoading(false);
    }
  }, [matchId, civId, session]);

  useEffect(() => {
    void refresh();
    const ms = summary?.modeId === 'dev-blitz-3m' ? 3000 : 15000;
    const timer = setInterval(() => void refresh(), ms);
    return () => clearInterval(timer);
  }, [refresh, summary?.modeId]);

  const handleJoin = async () => {
    setBusy(true);
    try {
      const joined = await api.joinMatch(matchId, playerName);
      const next = {
        playerId: joined.playerId,
        playerName: joined.playerName,
        civilizationId: joined.civilizationId,
        civilizationName: joined.civilizationName,
      };
      saveSession(matchId, next);
      setSession(next);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to join');
    } finally {
      setBusy(false);
    }
  };

  const handleResolve = async (gateId: string, optionId: string) => {
    if (!session) return;
    setBusy(true);
    try {
      await api.resolveDecision(matchId, session.civilizationId, gateId, optionId);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to resolve decision');
    } finally {
      setBusy(false);
    }
  };

  const handlePolicySave = async () => {
    if (!session) return;
    setBusy(true);
    try {
      const dash = await api.updatePolicy(matchId, session.civilizationId, policyPreset);
      setDashboard(dash);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update policy');
    } finally {
      setBusy(false);
    }
  };

  const handleReady = async (ready: boolean) => {
    if (!session) return;
    setBusy(true);
    try {
      await api.setReady(matchId, session.playerId, ready);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update ready status');
    } finally {
      setBusy(false);
    }
  };

  const handleStart = async () => {
    if (!session) return;
    setBusy(true);
    try {
      await api.startMatch(matchId, session.playerId);
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to start match');
    } finally {
      setBusy(false);
    }
  };

  if (loading && !summary) return <p className="muted">Loading…</p>;
  if (!summary) {
    return (
      <>
        <p className="error">{error ?? 'Match not found'}</p>
        <Link to="/">Back</Link>
      </>
    );
  }

  const ended = summary.status === 'Ended';
  const inLobby = summary.status === 'Lobby';
  const isHost = session?.playerId === summary.hostPlayerId;
  const canStart = inLobby && isHost && summary.readyCount >= summary.minPlayers;
  const myCiv = summary.civilizations.find((c) => c.id === civId);
  const rivals = summary.civilizations.filter((c) => c.id !== civId);
  const myGates = summary.pendingGates.filter((g) => g.civilizationId === civId);

  return (
    <div className="card">
      <div className="row spread">
        <Link to="/">← Back</Link>
        <span className="muted">
          {summary.modeDisplayName} · {inLobby ? 'lobby' : `tick ${summary.tickCount}/${summary.maxTicks}`}
          {!inLobby && !ended ? ` · next ${summary.isTickDue ? 'now' : formatCountdown(summary.nextTickAt)}` : ''}
          {ended ? ' · finished' : ''}
        </span>
      </div>

      <p className="muted">Code {summary.joinCode}</p>

      {!session ? (
        <div className="row join-row">
          <input value={playerName} onChange={(e) => setPlayerName(e.target.value)} placeholder="Your name" />
          <button className="btn btn-primary" disabled={busy} onClick={() => void handleJoin()}>
            Join
          </button>
        </div>
      ) : (
        <p className="muted">Playing as {session.playerName} ({session.civilizationName})</p>
      )}

      {ended && summary.results.length > 0 && (
        <div className="results-block">
          <p className="section-label">Final results</p>
          <ul className="simple-list">
            {summary.results.map((r) => (
              <li key={r.civilizationId}>
                <strong>{r.rank}. {r.civilizationName}</strong>
                <span className="muted">
                  {' '}— TTS {r.tier}, stability {Math.round(r.stability)}, {r.techCount} techs ({r.outcome})
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {inLobby && (
        <div className="panel-block">
          <p className="section-label">Lobby</p>
          <p className="muted">
            {summary.readyCount}/{summary.minPlayers} ready · {summary.players.length}/{summary.maxPlayers} players
          </p>
          <ul className="simple-list compact">
            {summary.players.map((p) => (
              <li key={p.playerId} className="muted">
                {p.playerName} ({p.civilizationName})
                {p.isHost ? ' · host' : ''}
                {p.isReady ? ' · ready' : ''}
              </li>
            ))}
          </ul>
          {session && (
            <div className="row">
              {(() => {
                const me = summary.players.find((p) => p.playerId === session.playerId);
                const ready = me?.isReady ?? false;
                return (
                  <button className="btn" disabled={busy} onClick={() => void handleReady(!ready)}>
                    {ready ? 'Not ready' : 'Ready up'}
                  </button>
                );
              })()}
              {canStart && (
                <button className="btn btn-primary" disabled={busy} onClick={() => void handleStart()}>
                  Start match
                </button>
              )}
            </div>
          )}
          {!session && <p className="muted">Join to ready up.</p>}
        </div>
      )}

      {myCiv && !inLobby && <p>{civLine(myCiv)}</p>}
      {!inLobby && rivals.map((r) => (
        <p key={r.id} className="muted">{civLine(r)}</p>
      ))}

      {myGates.map((gate) => (
        <div key={gate.gateId} className="gate-block">
          <p><strong>{gate.title}</strong></p>
          <p className="fable">{gate.description}</p>
          <div className="row">
            {gate.options.map((opt) => (
              <button
                key={opt.id}
                className="btn btn-primary"
                disabled={busy || !session}
                onClick={() => void handleResolve(gate.gateId, opt.id)}
              >
                {opt.id.toUpperCase()}: {opt.label}
              </button>
            ))}
          </div>
        </div>
      ))}

      {session && dashboard && !ended && (
        <>
          <div className="panel-block">
            <p className="section-label">Policy</p>
            <div className="row join-row">
              <select value={policyPreset} onChange={(e) => setPolicyPreset(e.target.value)}>
                {POLICY_PRESETS.map((p) => (
                  <option key={p.id} value={p.id}>{p.label}</option>
                ))}
              </select>
              <button className="btn btn-primary" disabled={busy} onClick={() => void handlePolicySave()}>
                Save
              </button>
            </div>
            <p className="muted">
              {dashboard.researchStance} · risk {dashboard.riskTolerance.toLowerCase()}
            </p>
            {dashboard.recommendedTech && (
              <p className="muted">
                Recommended: {dashboard.recommendedTech.name} (TTS {dashboard.recommendedTech.tier}, {dashboard.recommendedTech.branch})
              </p>
            )}
          </div>

          <div className="panel-block">
            <p className="section-label">Tech tree</p>
            {dashboard.researchedTech.length > 0 && (
              <>
                <p className="muted">Researched</p>
                <ul className="simple-list compact">
                  {dashboard.researchedTech.map((t) => (
                    <li key={t.id} className="muted">TTS {t.tier} · {t.name}</li>
                  ))}
                </ul>
              </>
            )}
            {dashboard.availableTech.length > 0 && (
              <>
                <p className="muted">Available next</p>
                <ul className="simple-list compact">
                  {dashboard.availableTech.map((t) => (
                    <li key={t.id} className="muted">TTS {t.tier} · {t.name} ({t.branch})</li>
                  ))}
                </ul>
              </>
            )}
          </div>

          {dashboard.crime && (
            <div className="panel-block">
              <p className="section-label">Crime perspective (TTS 4+)</p>
              <p className="muted">
                Pressure {dashboard.crime.averageCrimePressure.toFixed(0)}
                {dashboard.crime.cybersecurityMitigationActive ? ' · cybersecurity active' : ''}
              </p>
              <ul className="simple-list compact">
                {dashboard.crime.regions.map((r) => (
                  <li key={r.regionName} className="muted">
                    {r.regionName} ({r.sourceState}) — pressure {r.crimePressure.toFixed(0)}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}

      {summary.tickLogs.length > 0 && (
        <div className="log-block">
          <p className="section-label">Match log</p>
          {summary.tickLogs.map((entry) => (
            <div key={entry.tick} className="log-tick">
              <p className="log-tick-title">Tick {entry.tick}</p>
              <ul className="simple-list compact">
                {entry.lines.map((line) => (
                  <li key={line} className="muted">{line}</li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}

      {error && <p className="error">{error}</p>}
    </div>
  );
}
