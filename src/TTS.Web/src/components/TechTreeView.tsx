import { useMemo, useState } from 'react';
import type { TechTreeNode } from '../api';
import { tierLabel } from '../tierLabels';

const STATUS_LABELS: Record<string, string> = {
  researched: 'Researched',
  available: 'Available',
  locked: 'Locked',
  blocked: 'Blocked',
};

function tierSummary(nodes: TechTreeNode[]) {
  const researched = nodes.filter((n) => n.status === 'researched').length;
  return `${researched}/${nodes.length}`;
}

export function TechTreeView({
  nodes,
  currentTier,
  recommendedId,
  startingTier = 1,
}: {
  nodes: TechTreeNode[];
  currentTier: number;
  recommendedId?: string | null;
  startingTier?: number;
}) {
  const [expanded, setExpanded] = useState<Record<number, boolean>>(() => {
    const initial: Record<number, boolean> = {};
    for (let tier = 1; tier <= 8; tier++) {
      if (tier < startingTier)
        initial[tier] = false;
      else
        initial[tier] = tier <= currentTier + 1;
    }
    return initial;
  });
  const [hoverId, setHoverId] = useState<string | null>(null);

  const byTier = useMemo(() => {
    const map = new Map<number, TechTreeNode[]>();
    for (const node of nodes) {
      const list = map.get(node.tier) ?? [];
      list.push(node);
      map.set(node.tier, list);
    }
    return map;
  }, [nodes]);

  const nameById = useMemo(() => new Map(nodes.map((n) => [n.id, n.name])), [nodes]);

  const highlightIds = useMemo(() => {
    if (!hoverId) return new Set<string>();
    const node = nodes.find((n) => n.id === hoverId);
    if (!node) return new Set<string>();
    return new Set([hoverId, ...node.prerequisites]);
  }, [hoverId, nodes]);

  if (nodes.length === 0) {
    return <p className="muted">Tech catalog not loaded.</p>;
  }

  return (
    <div className="tech-tree">
      <div className="tech-tree-legend">
        <span className="tech-legend-item"><i className="tech-dot researched" /> Researched</span>
        <span className="tech-legend-item"><i className="tech-dot available" /> Available</span>
        <span className="tech-legend-item"><i className="tech-dot locked" /> Locked</span>
        <span className="tech-legend-item"><i className="tech-dot blocked" /> Blocked</span>
      </div>

      {startingTier > 1 && (
        <p className="muted tech-tree-historical-note">
          TTS 1–{startingTier - 1} marked as historical foundation (completed at match start).
        </p>
      )}

      {Array.from({ length: 8 }, (_, i) => i + 1).map((tier) => {
        const tierNodes = byTier.get(tier);
        if (!tierNodes?.length) return null;

        const isCurrent = tier === currentTier;
        const isHistorical = tier < startingTier;
        const isOpen = expanded[tier] ?? false;

        return (
          <section
            key={tier}
            className={`tech-tier${isCurrent ? ' tech-tier-current' : ''}${isOpen ? ' tech-tier-open' : ''}${isHistorical ? ' tech-tier-historical' : ''}`}
          >
            <button
              type="button"
              className="tech-tier-header"
              onClick={() => setExpanded((prev) => ({ ...prev, [tier]: !isOpen }))}
            >
              <span className="tech-tier-title">
                TTS {tier} · {tierLabel(tier)}
                {isCurrent && <span className="badge badge-tier">You are here</span>}
                {isHistorical && <span className="badge badge-historical">Historical</span>}
              </span>
              <span className="muted">{tierSummary(tierNodes)} · {isOpen ? '▾' : '▸'}</span>
            </button>

            {isOpen && (
              <div className="tech-tier-grid">
                {tierNodes.map((node) => {
                  const isRecommended = node.id === recommendedId;
                  const isHighlighted = highlightIds.has(node.id);
                  const prereqNames = node.prerequisites
                    .map((id) => nameById.get(id))
                    .filter(Boolean) as string[];

                  return (
                    <article
                      key={node.id}
                      className={`tech-node tech-node-${node.status}${isRecommended ? ' tech-node-recommended' : ''}${isHighlighted ? ' tech-node-highlight' : ''}${node.isForbidden ? ' tech-node-forbidden' : ''}`}
                      onMouseEnter={() => setHoverId(node.id)}
                      onMouseLeave={() => setHoverId(null)}
                    >
                      <div className="tech-node-head">
                        <span className="tech-node-role">{node.role}</span>
                        {node.isForbidden && <span className="tech-node-risk">forbidden</span>}
                      </div>
                      <strong className="tech-node-name">{node.name}</strong>
                      <span className="tech-node-branch">{node.branch}</span>
                      <span className="tech-node-status">{STATUS_LABELS[node.status] ?? node.status}</span>
                      {prereqNames.length > 0 && (
                        <p className="tech-node-prereqs muted">
                          Needs {prereqNames.join(', ')}
                        </p>
                      )}
                    </article>
                  );
                })}
              </div>
            )}
          </section>
        );
      })}
    </div>
  );
}
