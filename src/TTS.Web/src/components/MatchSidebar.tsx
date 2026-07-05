import {
  Building2,
  Cpu,
  Globe2,
  Landmark,
  Map,
  Radio,
  type LucideIcon,
} from 'lucide-react';
import { cn } from '@/lib/utils';

export type MatchSection =
  | 'territory'
  | 'economy'
  | 'civics'
  | 'rivals'
  | 'tech'
  | 'intel';

const NAV: { id: MatchSection; label: string; icon: LucideIcon }[] = [
  { id: 'territory', label: 'Territory', icon: Map },
  { id: 'economy', label: 'Economy', icon: Landmark },
  { id: 'civics', label: 'Civics', icon: Building2 },
  { id: 'rivals', label: 'Military', icon: Globe2 },
  { id: 'tech', label: 'Tech', icon: Cpu },
  { id: 'intel', label: 'Intel', icon: Radio },
];

type Props = {
  active: MatchSection;
  civilizationName?: string;
  onNavigate: (section: MatchSection) => void;
  hasUrgentGate?: boolean;
  onExecuteCommands?: () => void;
};

export function MatchSidebar({ active, civilizationName, onNavigate, hasUrgentGate, onExecuteCommands }: Props) {
  return (
    <nav className="gc-sidebar" aria-label="Strategic hub">
      <div className="gc-sidebar-profile">
        <div className="gc-sidebar-avatar" aria-hidden>
          <Globe2 className="h-4 w-4" />
        </div>
        <div className="gc-sidebar-profile-text">
          <p className="gc-sidebar-kicker">Strategic hub</p>
          <p className="gc-sidebar-civ">{civilizationName ?? 'Spectating'}</p>
        </div>
      </div>

      <ul className="gc-sidebar-nav">
        {NAV.map(({ id, label, icon: Icon }) => (
          <li key={id}>
            <button
              type="button"
              className={cn('gc-sidebar-link', active === id && 'gc-sidebar-link-active')}
              onClick={() => onNavigate(id)}
            >
              <Icon className="h-3.5 w-3.5 shrink-0" aria-hidden />
              <span>{label}</span>
              {id === 'economy' && hasUrgentGate && (
                <span className="gc-sidebar-alert" aria-label="Urgent decision" />
              )}
            </button>
          </li>
        ))}
      </ul>

      {onExecuteCommands && (
        <button type="button" className="gc-sidebar-execute" onClick={onExecuteCommands}>
          Execute commands
        </button>
      )}
    </nav>
  );
}
