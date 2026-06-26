import type { ReactNode } from 'react';

type Props = {
  icon: string;
  title: string;
  defaultOpen?: boolean;
  children: ReactNode;
};

export function AccordionSection({ icon, title, defaultOpen = false, children }: Props) {
  return (
    <details className="accordion-section" open={defaultOpen || undefined}>
      <summary className="accordion-summary">
        <div className="accordion-summary-left">
          <span className="material-symbols-outlined accordion-icon" aria-hidden>{icon}</span>
          <span className="accordion-title">{title}</span>
        </div>
        <span className="material-symbols-outlined accordion-chevron" aria-hidden>expand_more</span>
      </summary>
      <div className="accordion-body">{children}</div>
    </details>
  );
}
