import {
  Building2,
  ChevronDown,
  GitBranch,
  History,
  ScrollText,
  Shield,
  Users,
  type LucideIcon,
} from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';

const ICONS: Record<string, LucideIcon> = {
  history: History,
  location_city: Building2,
  groups: Users,
  shield: Shield,
  account_tree: GitBranch,
  article: ScrollText,
};

type Props = {
  icon: string;
  title: string;
  defaultOpen?: boolean;
  children: ReactNode;
};

export function AccordionSection({ icon, title, defaultOpen = false, children }: Props) {
  const Icon = ICONS[icon] ?? ScrollText;

  return (
    <details
      className="accordion-section group rounded-xl border border-border bg-card shadow-sm"
      open={defaultOpen || undefined}
    >
      <summary className="accordion-summary flex cursor-pointer list-none items-center justify-between gap-3 px-4 py-3 marker:content-none">
        <div className="accordion-summary-left flex items-center gap-2.5">
          <Icon className="accordion-icon h-4 w-4 text-muted-foreground" aria-hidden />
          <span className="accordion-title text-sm font-medium">{title}</span>
        </div>
        <ChevronDown
          className={cn(
            'accordion-chevron h-4 w-4 shrink-0 text-muted-foreground transition-transform',
            'group-open:rotate-180',
          )}
          aria-hidden
        />
      </summary>
      <div className="accordion-body border-t border-border/60 px-4 py-4">{children}</div>
    </details>
  );
}
