import { BrowserRouter, Link, Route, Routes, useLocation } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { HomePage } from './pages/HomePage';
import { MatchPage } from './pages/MatchPage';
import { TradeGlobePage } from './pages/TradeGlobePage';

function AppLayout() {
  const location = useLocation();
  const isMatch = location.pathname.startsWith('/match/');
  const isTrade = location.pathname.startsWith('/trade');

  return (
    <div
      className={cn(
        'min-h-screen text-foreground',
        isTrade && 'h-dvh overflow-hidden',
        isMatch || isTrade ? 'match-app-bg' : 'home-app-bg',
      )}
    >
      {!isMatch && !isTrade && (
        <header className="sticky top-0 z-40 border-b border-border/25 bg-background/70 backdrop-blur-md">
          <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-4 sm:px-6">
            <Link to="/" className="group flex items-center gap-3 no-underline">
              <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-sm font-bold text-primary ring-1 ring-primary/20">
                TTS
              </span>
              <div className="hidden sm:block">
                <p className="text-sm font-semibold leading-none text-foreground transition-colors group-hover:text-primary">
                  From Stone to Ascension
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">Governor briefing dashboard</p>
              </div>
            </Link>
            <Badge variant="outline" className="hidden border-border font-normal text-muted-foreground sm:inline-flex">
              Async strategy
            </Badge>
            <Button variant="ghost" size="sm" asChild className="hidden sm:inline-flex">
              <Link to="/trade">Trade globe</Link>
            </Button>
          </div>
        </header>
      )}
      <main
        className={cn(
          isTrade
            ? 'h-dvh w-full max-w-none overflow-hidden p-0'
            : isMatch
              ? 'mx-auto max-w-[1600px] px-3 py-4 sm:px-5'
              : 'mx-auto max-w-6xl px-4 py-6 sm:px-6',
        )}
      >
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/trade" element={<TradeGlobePage />} />
          <Route path="/match/:matchId" element={<MatchPage />} />
        </Routes>
      </main>
    </div>
  );
}

export function App() {
  return (
    <BrowserRouter>
      <AppLayout />
    </BrowserRouter>
  );
}
