import { Brain, FlaskConical, Gavel, RefreshCw } from 'lucide-react';
import type { AdvisorBriefing, DecisionGate, LlmLayerStatus } from '../api';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
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
      className={`advisor-panel rounded-2xl border border-border/25 bg-card/50 shadow-lg shadow-black/10${locked ? ' advisor-panel-locked' : ''}${loading ? ' advisor-panel-loading' : ''}${gateFocus ? ' advisor-panel-gate' : ''}`}
    >
      <div className="advisor-panel-head border-b border-border/60 px-4 py-3">
        <div className="advisor-panel-head-left">
          <div className="advisor-panel-icon flex h-9 w-9 items-center justify-center rounded-lg bg-primary/15 text-primary" aria-hidden>
            {gateFocus ? <Gavel className="h-4 w-4" /> : <Brain className="h-4 w-4" />}
          </div>
          <div>
            <p className="label-caps advisor-panel-kicker text-xs text-muted-foreground">
              {gateFocus ? 'Gate counsel' : 'Strategic advisor'}
            </p>
            {!locked && advisor && (
              <Badge variant={tone === 'ai' ? 'default' : 'secondary'} className={`advisor-source-badge advisor-source-${tone} mt-1`}>
                {advisorSourceLabel(source)}
              </Badge>
            )}
          </div>
        </div>
        <div className="advisor-panel-actions">
          {callsLabel && tier >= 5 && (
            <span className="advisor-calls-label text-xs text-muted-foreground">{callsLabel}</span>
          )}
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="advisor-refresh-btn"
            disabled={loading || !canRefresh || locked}
            onClick={onRefresh}
          >
            <RefreshCw className={`h-3.5 w-3.5${loading ? ' animate-spin' : ''}`} aria-hidden />
            {loading ? 'Analyzing…' : 'Refresh'}
          </Button>
        </div>
      </div>

      <div className="px-4 py-4">
      {locked ? (
        <div className="advisor-panel-locked-body">
          <p className="advisor-headline font-medium">Advisor unlocks at TTS 4</p>
          <p className="advisor-briefing mt-2 text-sm text-muted-foreground">
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
                  <p className="label-caps advisor-gate-kicker text-xs text-muted-foreground">
                    {activeGate ? formatGateCountdown(activeGate.expiresAt) : 'Decision pending'}
                  </p>
                  <h2 className="advisor-gate-title text-base font-semibold">{gateFocus.title}</h2>
                </div>
                <Badge variant="outline" className="advisor-gate-type font-normal">
                  {gateFocus.gateType.replace(/([A-Z])/g, ' $1').trim()}
                </Badge>
              </div>

              <p className="advisor-gate-rationale mt-3 text-sm text-muted-foreground">{gateFocus.rationale}</p>

              <div className="advisor-gate-options mt-4 space-y-2">
                {gateFocus.options.map((option) => (
                  <div
                    key={option.optionId}
                    className={`advisor-gate-option advisor-gate-option-${option.stance} rounded-lg border border-border/60 p-3`}
                  >
                    <div className="advisor-gate-option-top flex items-center justify-between gap-2">
                      <span className="advisor-gate-option-label font-medium">{option.label}</span>
                      {option.stance === 'recommended' && (
                        <Badge variant="success" className="advisor-gate-option-badge">Recommended</Badge>
                      )}
                      {option.stance === 'caution' && (
                        <Badge variant="warning" className="advisor-gate-option-badge">Risky</Badge>
                      )}
                    </div>
                    <p className="advisor-gate-option-note mt-1 text-sm text-muted-foreground">{option.note}</p>
                  </div>
                ))}
              </div>

              {canApply && onApplyRecommendation && (
                <Button
                  type="button"
                  className="advisor-apply-btn mt-4 w-full sm:w-auto"
                  onClick={() => onApplyRecommendation(gateFocus.gateId, gateFocus.recommendedOptionId)}
                >
                  Apply recommendation: {gateFocus.recommendedOptionLabel}
                </Button>
              )}
            </div>
          )}

          {!gateFocus && (
            <>
              <h2 className="advisor-headline font-medium">{advisor.headline || 'Strategic assessment'}</h2>
              {highlights.length > 0 && (
                <ul className="advisor-highlights mt-3 space-y-1 text-sm text-muted-foreground">
                  {highlights.map((line) => (
                    <li key={line}>{line}</li>
                  ))}
                </ul>
              )}
            </>
          )}

          <p className={`advisor-briefing mt-3 text-sm text-muted-foreground${gateFocus ? ' advisor-briefing-secondary' : ''}`}>
            {advisor.briefing}
          </p>

          {advisor.recommendedTechName && (
            <div className="advisor-rec-chip mt-3 inline-flex items-center gap-2 rounded-md border border-border bg-muted/40 px-3 py-2 text-sm">
              <FlaskConical className="h-4 w-4 text-primary" aria-hidden />
              <span>
                Next research: <strong>{advisor.recommendedTechName}</strong>
              </span>
            </div>
          )}
        </>
      ) : (
        <p className="advisor-briefing text-sm text-muted-foreground">Tap Refresh for strategic guidance on pending gates and policy.</p>
      )}
      </div>
    </section>
  );
}
