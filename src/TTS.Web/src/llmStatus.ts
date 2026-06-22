import type { LlmLayerStatus } from './api';

export function llmStatusLabel(status: LlmLayerStatus | null | undefined): string {
  if (!status) return 'LLM unknown';
  if (!status.providerEnabled) return 'LLM off';
  if (!status.turnAgentReady) return 'LLM not ready';
  if (!status.anyRivalEligible) return `Classical rivals (TTS ${status.rivalTierGate}+ for LLM)`;
  if (status.lastRivalRunner === 'agent') return 'Rivals: LLM active';
  if (status.lastRivalRunner === 'classical-ai') return 'Rivals: classical fallback';
  return 'Rivals: LLM ready';
}

export function llmStatusTone(status: LlmLayerStatus | null | undefined): 'on' | 'waiting' | 'off' {
  if (!status?.providerEnabled || !status.turnAgentReady) return 'off';
  if (!status.anyRivalEligible) return 'waiting';
  if (status.lastRivalRunner === 'agent' || status.anyRivalEligible) return 'on';
  return 'waiting';
}

export function llmStatusShort(status: LlmLayerStatus | null | undefined): string {
  if (!status?.providerEnabled) return 'Off';
  if (!status.turnAgentReady) return 'Setup';
  if (!status.anyRivalEligible) return `TTS ${status.rivalTierGate}+`;
  return status.lastRivalRunner === 'agent' ? 'LLM' : 'Ready';
}
