import { BrowserRouter, Link, Route, Routes, useLocation } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { HomePage } from './pages/HomePage';
import { LiveSandboxPage } from './pages/LiveSandboxPage';
import { MatchPage } from './pages/MatchPage';
import { SatelliteGlobePage } from './pages/SatelliteGlobePage';
import { TradeGlobePage } from './pages/TradeGlobePage';

function AppLayout() {
  const location = useLocation();
  const isMatch = location.pathname.startsWith('/match/');
  const isTrade = location.pathname.startsWith('/trade');
  const isSatellites = location.pathname.startsWith('/satellites');
  const isLive = location.pathname.startsWith('/live');
  const isFullscreenGlobe = isTrade || isSatellites || isLive;

  return (
    <div
      className={cn(
        'min-h-screen text-foreground',
        isFullscreenGlobe && 'h-dvh overflow-hidden',
        isMatch || isFullscreenGlobe ? 'match-app-bg' : 'home-app-bg',
      )}
    >
      {!isMatch && !isFullscreenGlobe && (
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
            <div className="hidden items-center gap-1 sm:flex">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/live">Live sandbox</Link>
              </Button>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/trade">Trade globe</Link>
              </Button>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/satellites">Satellites</Link>
              </Button>
            </div>
          </div>
        </header>
      )}
      <main
        className={cn(
          isFullscreenGlobe
            ? 'h-dvh w-full max-w-none overflow-hidden p-0'
            : isMatch
              ? 'mx-auto max-w-[1600px] px-3 py-4 sm:px-5'
              : 'mx-auto max-w-6xl px-4 py-6 sm:px-6',
        )}
      >
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/live" element={<LiveSandboxPage />} />
          <Route path="/trade" element={<TradeGlobePage />} />
          <Route path="/satellites" element={<SatelliteGlobePage />} />
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
