# Player Experience — Gameplay UX

**Project:** TTS — Technology Tier Simulation  
**Status:** Design guidance — reflects shipped UI + TTS 4 default start  
**Related:** [architecture-overview.md](architecture-overview.md) · [ui-design.md](ui-design.md) · [match-modes.md](match-modes.md) · [v2/tts4-start.md](v2/tts4-start.md)

---

## Executive summary

TTS is designed around an async governor loop: **log in → digest → decide → adjust policy → leave**. Most visits should be **2–5 minutes**.

See [architecture-overview.md](architecture-overview.md) Part B for the **gameplay architecture diagram** and era model.

---

## Design pillars (player-facing)

| Pillar | What the player should feel |
|--------|----------------------------|
| **Slow evolution** | World advances on a schedule |
| **Meaningful decisions** | Crises and tier shifts matter |
| **Progress vs stability** | Getting stronger makes society fragile |
| **Shared timeline** | Rivals, diffusion, and events connect stories |
| **Era identity** | Each TTS tier changes the dashboard |

---

## Check-in priority (dashboard)

1. **Pending gate** — decision required, impact hints, countdown  
2. **Away summary** — headline + bullets + missed gates  
3. **Civ vitals** — tier band, stability bars  
4. **Next tick** — countdown + recommended research  
5. **Policy / advisor / rivals / tech tree** — secondary panels  

Implemented in `TTS.Web` (`MatchPage.tsx`). See [ui-design.md](ui-design.md) §5.3.

---

## TTS 4 default start

Modern match modes begin at **Information Age** with a curated tech spine (crime, cybersecurity, digital computing visible from tick 0).

| Mode | Start | Notes |
|------|-------|-------|
| Sprint / Blitz / Standard / Extended / Dev | TTS 4 | Default experience |
| **classic-stone** | TTS 1 | Legacy full ascent |

---

## Agency without micromanagement

| Control | Effect |
|---------|--------|
| **Policy presets** | Stance between visits |
| **Decision gates** | High-impact A/B/C choices |
| **Advisor (TTS 4+)** | Classical analysis; LLM at TTS 5+ |
| **Auto-research** | Policy picks tech each tick — no per-tick clicking |

---

## Success criteria

- First dashboard load shows **crime + modern UI** (TTS 4 start)  
- Each visit answers: **What changed? What must I decide? What for while I'm gone?**  
- Rival LLM turns observable within **first few ticks** of a sprint match  
- Legacy **TTS 1** path available via **From Stone (Classic)**  

---

## Document map

| Topic | Doc |
|-------|-----|
| Technical stack diagram | [architecture-overview.md](architecture-overview.md) Part A |
| Gameplay flow diagram | [architecture-overview.md](architecture-overview.md) Part B |
| Screen wireframes | [ui-design.md](ui-design.md) |
| TTS 4 bootstrap rationale | [v2/tts4-start.md](v2/tts4-start.md) |
