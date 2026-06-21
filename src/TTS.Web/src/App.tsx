import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { HomePage } from './pages/HomePage';
import { MatchPage } from './pages/MatchPage';

export function App() {
  return (
    <BrowserRouter>
      <div className="app-shell">
        <header className="app-header">
          <h1>TTS</h1>
        </header>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/match/:matchId" element={<MatchPage />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}
