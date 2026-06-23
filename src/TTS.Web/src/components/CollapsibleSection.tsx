import { useState, type ReactNode } from 'react';

type Props = {
  title: string;
  subtitle?: string;
  defaultOpen?: boolean;
  open?: boolean;
  onToggle?: (open: boolean) => void;
  className?: string;
  variant?: 'default' | 'subtle';
  children: ReactNode;
  badge?: ReactNode;
};

export function CollapsibleSection({
  title,
  subtitle,
  defaultOpen = false,
  open: controlledOpen,
  onToggle,
  className = '',
  variant = 'default',
  children,
  badge,
}: Props) {
  const [internalOpen, setInternalOpen] = useState(defaultOpen);
  const open = controlledOpen ?? internalOpen;

  const toggle = () => {
    const next = !open;
    if (onToggle) onToggle(next);
    else setInternalOpen(next);
  };

  return (
    <section className={`collapsible collapsible-${variant} ${className}${open ? ' collapsible-open' : ''}`}>
      <button type="button" className="collapsible-trigger" onClick={toggle} aria-expanded={open}>
        <span className="collapsible-trigger-text">
          <span className="collapsible-title">{title}</span>
          {subtitle && !open && <span className="muted collapsible-subtitle">{subtitle}</span>}
        </span>
        <span className="collapsible-trigger-end">
          {badge}
          <span className="muted collapsible-chevron" aria-hidden>{open ? '▾' : '▸'}</span>
        </span>
      </button>
      {open && <div className="collapsible-body">{children}</div>}
    </section>
  );
}
