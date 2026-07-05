import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './briefing-tokens.css';
import './index.css';
import './match-ui.css';
import './governor-command.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
