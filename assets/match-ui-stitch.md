# Match UI — Stitch Design Brief

**Product:** From Stone to Ascension (TTS)  
**Screen:** Governor Match Dashboard (`MatchPage`)  
**Stack:** React + CSS · full-width · map-left layout  
**Purpose:** Handoff doc for [Google Stitch](https://stitch.withgoogle.com) or similar AI UI tools

---

## 1. Design intent

TTS is an **async governor dashboard**, not an RTS. Players visit for **2–5 minutes** to:

1. See **when the next simulation tick** fires (wall-clock)
2. Resolve **decision gates** if any are pending
3. Glance at **civ stability**, **policy**, and **territory map**
4. Leave

**Primary visual hierarchy**

| Priority | Element |
|----------|---------|
| 1 | **Next tick countdown** — largest numeric element in HUD |
| 2 | **Decision gates** — amber hero cards, full-width in main column |
| 3 | **Territory hex map** — sticky left panel, always visible when match running |
| 4 | **Civ vitals + policy** — compact command strip |
| 5 | **More** — cities, rivals, tech tree, log (collapsed by default) |

---

## 2. Layout (desktop ≥769px)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  HUD (sticky, full width)                                                    │
│  [←]  Sprint 8h                    ┌──────────────┐         [TTS 4 badge]   │
│       Aurora Collective · host     │  12:34       │                          │
│                                    │  next tick   │                          │
│                                    │  tick 3/8 ▓▓░│                          │
│                                    └──────────────┘                          │
├──────────────────┬──────────────────────────────────────────────────────────┤
│  TERRITORY       │  MAIN COLUMN                                              │
│  (38vw, sticky)  │                                                           │
│                  │  ┌─ DECISION GATE (if pending) ─────────────────────┐    │
│   [hex map SVG]  │  │ DECISION · 1:42 left                             │    │
│                  │  │ Data sovereignty dispute                          │    │
│   biome legend   │  │ [ Invest ] [ Delay ] [ Ban ]                      │    │
│   (optional)     │  └───────────────────────────────────────────────────┘    │
│                  │                                                           │
│                  │  ┌─ COMMAND STRIP ────────────────────────────────────┐  │
│                  │  │ Civ name · stability bars │ Policy [▼] [Save]      │  │
│                  │  └────────────────────────────────────────────────────┘  │
│                  │                                                           │
│                  │  ▼ More — 2 cities · 1 rival · 12 techs                    │
└──────────────────┴──────────────────────────────────────────────────────────┘
```

**Mobile (<769px):** HUD full width → map panel (max 45vh) → main column stacked.

---

## 3. Color tokens

| Token | Hex | Usage |
|-------|-----|--------|
| `bg-app` | `#0f1419` | Page background |
| `bg-surface` | `#161b22` | Cards |
| `bg-elevated` | `#1c2128` | HUD, map panel |
| `border` | `#30363d` | Card borders |
| `text-primary` | `#e8edf5` | Headings, countdown digits |
| `text-muted` | `#8b949e` | Labels, meta |
| `accent-blue` | `#388bfd` | Links, primary buttons, default gate option |
| `accent-green` | `#3fb950` | Stability good, ready badge |
| `accent-amber` | `#f0b429` | Gate hero, decision urgency |
| `accent-red` | `#f85149` | Tick due NOW, low stability |
| `tier-info` | `#6ec8ff` | TTS tier badges |

**Gate card:** gradient `#1a1608` → `#161b22`, border `#f0b42955`.

**Tick due state:** pulsing red ring `#f85149`, label "NOW".

---

## 4. Typography

| Role | Size | Weight |
|------|------|--------|
| Tick countdown | 1.75–2rem | 700, tabular nums |
| Gate title | 1.15rem | 600 |
| HUD title | 1rem | 600 |
| Section label | 0.72rem | 700, uppercase, letter-spacing 0.08em |
| Body | 0.88rem | 400 |
| Meta / muted | 0.78–0.82rem | 400 |

Font: **system-ui** stack (Inter-like on web).

---

## 5. Components

### 5.1 Match HUD (`match-hud`)

- Sticky top, `backdrop-filter: blur(8px)`, border-bottom
- Left: back link, mode name, civ + role (host/guest)
- Center-right: **TickClock** (see below)
- Far right: tier badge pill

### 5.2 Tick clock (`tick-clock`)

- **Circular ring** showing progress toward next tick (fills as deadline approaches)
- Center: `MM:SS` or `H:MM:SS` countdown; when due → **NOW** in red
- Below ring: season bar `tick N / max` with thin progress fill
- Updates every second (client-side)

### 5.3 Decision gate (`gate-hero`)

- Amber border + subtle gold gradient background
- Header row: `DECISION` label + gate countdown (monospace timer)
- Title + 2-line description max
- Options: stacked full-width buttons; default option has blue border
- Each button: label + muted impact hint on second line

### 5.4 Territory panel (`match-map-panel`)

- Title: "Territory" (uppercase small label)
- Hex SVG, flat-top, biome colors
- Capital = yellow dot; player overlay = blue; rival = red
- Click neutral adjacent hex to claim (cursor pointer on claimable)
- Selected tile meta line below map

### 5.5 Command strip (`match-command`)

- Horizontal card: civ vitals left, policy dropdown + Save right
- Stability: 3 thin pillar bars (political / economic / tech)
- Large stability score number (color-coded)

### 5.6 Lobby state

- HUD without tick clock; show "Waiting for players" pill
- Player list with ready badges
- Join code copy button prominent
- Host sees Start when min ready

### 5.7 Ended state

- HUD shows "Match ended"
- Results ranked list replaces gates
- Map read-only (no claim)

---

## 6. Screen states (for Stitch variants)

| State ID | Description |
|----------|-------------|
| `match-lobby` | 1–2 players, ready toggles, no tick clock |
| `match-running-clean` | Tick clock, map, civ strip, no gates |
| `match-gate-urgent` | Gate hero + tick clock < 5 min |
| `match-away` | Collapsible "While you were away" open above command strip |
| `match-ended` | Results list, dimmed map |

---

## 7. Sample copy

| Element | Example |
|---------|---------|
| HUD title | Dev Blitz (3m) |
| HUD sub | Solar Concord · host |
| Tick label | next tick |
| Tick due | NOW · tick due |
| Season | Tick **3** / 6 |
| Gate kicker | DECISION · **1:42** left |
| Gate title | Data sovereignty dispute |
| Policy hint | Next research: **Machine Learning** |
| Map meta | Forest · yield 52 · click to claim |
| More subtitle | 2 cities · 1 rival · 12 techs |

---

## 8. Stitch prompt (paste-ready)

```
Design a dark-mode async strategy governor dashboard for a web game.

Layout: full-width desktop. Sticky top HUD with a large circular countdown timer
showing time until next simulation tick (MM:SS), plus tick 3/8 season progress.
Left sidebar (38% width): hex territory map with colored biomes. Right column:
amber-highlighted decision card with 3 action buttons, then a compact civ status
strip with stability bars and policy dropdown.

Colors: background #0f1419, cards #161b22, text #e8edf5, accent blue #388bfd,
decision amber #f0b429, tick-due red #f85149. System font. Minimal, not gamey-
fantasy — think Linear meets Civilization briefing screen. Mobile: stack map
above content.
```

---

## 9. Implementation reference

| Asset | Path |
|-------|------|
| Match page | `src/TTS.Web/src/pages/MatchPage.tsx` |
| Tick clock | `src/TTS.Web/src/components/TickClock.tsx` |
| Hex map | `src/TTS.Web/src/components/HexMapView.tsx` |
| Styles | `src/TTS.Web/src/index.css` (`.match-hud`, `.tick-clock`, `.gate-hero`) |
| Design doc | `ui-design.md` |

---

## 10. Out of scope for this screen

- Real-time WebSocket tick stream
- 3D map / unit movement
- In-match chat
- Account settings / auth screens
