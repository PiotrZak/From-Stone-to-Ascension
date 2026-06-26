import type { AdvisorBriefing, DecisionGate, LlmLayerStatus } from '../api';
import { formatGateCountdown } from './TickClock';

export function advisorSourceLabel(source: string): string {
  switch (source) {
    case 'llm-tools':
      return 'AI advisor';
    case 'classical':
      return 'Policy engine';
    case 'rate-limit':
      return 'Classical · rate limited';
    case 'fallback':
      return 'Classical · LLM offline';
    case 'locked':
      return 'Locked';
    default:
      return 'Advisor';
  }
}

export function advisorSourceTone(source: string): 'ai' | 'classical' | 'muted' {
  if (source === 'llm-tools') return 'ai';
  if (source === 'classical' || source === 'rate-limit' || source === 'fallback') return 'classical';
  return 'muted';
}

export function advisorCallsLabel(status: LlmLayerStatus | null | undefined): string | null {
  if (!status?.providerEnabled) return 'LLM off · classical analysis only';
  if (status.maxAdvisorCallsPerTick <= 0) return null;
  const left = Math.max(0, status.maxAdvisorCallsPerTick - status.advisorCallsUsedThisTick);
  return `${left}/${status.maxAdvisorCallsPerTick} AI refreshes left this tick`;
}

type Props = {
  tier: number;
  advisor: AdvisorBriefing | null;
  loading: boolean;
  canRefresh: boolean;
  canApply: boolean;
  llmStatus: LlmLayerStatus | null | undefined;
  activeGate?: DecisionGate | null;
  onRefresh: () => void;
  onApplyRecommendation?: (gateId: string, optionId: string) => void;
};

export function StrategicAdvisorPanel({
  tier,
  advisor,
  loading,
  canRefresh,
  canApply,
  llmStatus,
  activeGate,
  onRefresh,
  onApplyRecommendation,
}: Props) {
  const locked = tier < 4;
  const callsLabel = advisorCallsLabel(llmStatus);
  const source = advisor?.source ?? 'system';
  const tone = advisorSourceTone(source);
  const gateFocus = advisor?.gateFocus ?? null;
  const highlights = advisor?.highlights ?? [];

  return (
    <section
      className={`advisor-panel${locked ? ' advisor-panel-locked' : ''}${loading ? ' advisor-panel-loading' : ''}${gateFocus ? ' advisor-panel-gate' : ''}`}
    >
      <div className="advisor-panel-head">
        <div className="advisor-panel-head-left">
          <div className="advisor-panel-icon" aria-hidden>
            <span className="material-symbols-outlined">{gateFocus ? 'gavel' : 'psychology'}</span>
          </div>
          <div>
            <p className="label-caps advisor-panel-kicker">
              {gateFocus ? 'Gate counsel' : 'Strategic advisor'}
            </p>
            {!locked && advisor && (
              <span className={`advisor-source-badge advisor-source-${tone}`}>
                {advisorSourceLabel(source)}
              </span>
            )}
          </div>
        </div>
        <div className="advisor-panel-actions">
          {callsLabel && tier >= 5 && (
            <span className="advisor-calls-label">{callsLabel}</span>
          )}
          <button
            type="button"
            className="advisor-refresh-btn"
            disabled={loading || !canRefresh || locked}
            onClick={onRefresh}
          >
            <span className="material-symbols-outlined" aria-hidden>refresh</span>
            {loading ? 'Analyzing…' : 'Refresh'}
          </button>
        </div>
      </div>

      {locked ? (
        <div className="advisor-panel-locked-body">
          <p className="advisor-headline">Advisor unlocks at TTS 4</p>
          <p className="advisor-briefing">
            Reach the Information Age to receive gate counsel, policy analysis, and research guidance.
          </p>
        </div>
      ) : loading && !advisor ? (
        <div className="advisor-panel-skeleton" aria-busy="true">
          <div className="advisor-skeleton-line advisor-skeleton-headline" />
          <div className="advisor-skeleton-line" />
          <div className="advisor-skeleton-line advisor-skeleton-short" />
        </div>
      ) : advisor ? (
        <>
          {gateFocus && (
            <div className="advisor-gate-block">
              <div className="advisor-gate-head">
                <div>
                  <p className="label-caps advisor-gate-kicker">
                    {activeGate ? formatGateCountdown(activeGate.expiresAt) : 'Decision pending'}
                  </p>
                  <h2 className="advisor-gate-title">{gateFocus.title}</h2>
                </div>
                <span className="advisor-gate-type">{gateFocus.gateType.replace(/([A-Z])/g, ' $1').trim()}</span>
              </div>

              <p className="advisor-gate-rationale">{gateFocus.rationale}</p>

              <div className="advisor-gate-options">
                {gateFocus.options.map((option) => (
                  <div
                    key={option.optionId}
                    className={`advisor-gate-option advisor-gate-option-${option.stance}`}
                  >
                    <div className="advisor-gate-option-top">
                      <span className="advisor-gate-option-label">{option.label}</span>
                      {option.stance === 'recommended' && (
                        <span className="advisor-gate-option-badge">Recommended</span>
                      )}
                      {option.stance === 'caution' && (
                        <span className="advisor-gate-option-badge advisor-gate-option-badge-warn">Risky</span>
                      )}
                    </div>
                    <p className="advisor-gate-option-note">{option.note}</p>
                  </div>
                ))}
              </div>

              {canApply && onApplyRecommendation && (
                <button
                  type="button"
                  className="advisor-apply-btn"
                  onClick={() => onApplyRecommendation(gateFocus.gateId, gateFocus.recommendedOptionId)}
                >
                  Apply recommendation: {gateFocus.recommendedOptionLabel}
                </button>
              )}
            </div>
          )}

          {!gateFocus && (
            <>
              <h2 className="advisor-headline">{advisor.headline || 'Strategic assessment'}</h2>
              {highlights.length > 0 && (
                <ul className="advisor-highlights">
                  {highlights.map((line) => (
                    <li key={line}>{line}</li>
                  ))}
                </ul>
              )}
            </>
          )}

          <p className={`advisor-briefing${gateFocus ? ' advisor-briefing-secondary' : ''}`}>
            {advisor.briefing}
          </p>

          {advisor.recommendedTechName && (
            <div className="advisor-rec-chip">
              <span className="material-symbols-outlined" aria-hidden>science</span>
              <span>
                Next research: <strong>{advisor.recommendedTechName}</strong>
              </span>
            </div>
          )}
        </>
      ) : (
        <p className="advisor-briefing muted">Tap Refresh for strategic guidance on pending gates and policy.</p>
      )}
    </section>
  );
}
