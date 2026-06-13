# TTS: TECHNOLOGY TIER SIMULATION

**Implementation:** [implementation-plan.md](implementation-plan.md) — master roadmap (Phases 0–9)  
**Match modes:** [match-modes.md](match-modes.md) — 8h / 24h / 36h / 48h async matches  
**UI:** [ui-design.md](ui-design.md) — governor dashboard client (web MVP)  
**LLM deploy:** [llm-deployment.md](llm-deployment.md) — Ollama vs cloud for internet MP + cost  
**Local AI:** [ollama-scenarios.md](ollama-scenarios.md) — Ollama scenarios (TTS.Agents)  
**TTS 4 data:** [crime-data.md](crime-data.md) — crime/income CSV perspective  
**Tech sub-trees:** [tech-trees-by-tier.md](tech-trees-by-tier.md) — per-TTS layer trees + diagrams  
**Separate game:** [company-sim.md](company-sim.md) — async procurement / company sim (Supply Ascent)

---

## 1. HIGH CONCEPT

**Genre:** Grand Strategy / Civilization Simulation / Sci-Fi Evolution Sandbox  

**Core Idea:**  
Players guide civilizations through escalating Technology Tier Systems (TTS 1 → 8+), where each era changes not just tools—but the rules of reality, society, and strategy itself.

> “Advancing technology doesn’t just make you stronger—it changes what strength means.”

---

## 2. CORE DESIGN PILLARS

### ⚙️ Era-Driven Gameplay
Each TTS is a distinct gameplay mode with new mechanics and constraints.

### 🧠 Emergent Civilization Behavior
Factions, economies, and ideologies evolve dynamically.

### ⚖️ Progress vs Stability Conflict
Advancement increases power but destabilizes society.

### 🌌 Rule Evolution
Higher tiers literally modify game systems (economy, physics, AI behavior).

---

## 3. GAME LOOP

### 🔁 Primary Loop
- Gather resources / information  
- Research technologies  
- Expand infrastructure  
- Manage societal stability  
- Trigger TTS advancement (or risk collapse)

### ⏳ Secondary Loop
- Respond to global events  
- Manage factions  
- Handle crises (war, AI, ecological, temporal anomalies)  

---

## 4. WORLD STRUCTURE

The world is a single evolving planet or galaxy simulation divided into:

- Regions (territories)  
- Civilizations (player or AI-controlled)  
- Factions (internal political groups)  
- Knowledge networks (science + tech diffusion)  

---

## 5. TTS PROGRESSION SYSTEM

---

### 🪵 TTS 1 – Pre-Industrial
**Gameplay Focus:** survival, agriculture, early expansion  

- Manual labor economy  
- Small-scale governance  
- Low tech ceiling  

**Win trigger:** unified kingdom or industrial breakthrough  

---

### 🏭 TTS 2 – Industrial Age
- Factories, logistics chains  
- Rapid population growth  
- Pollution system introduced  

**New mechanic:**  
📊 “Production Efficiency vs Social Unrest”

---

### ⚡ TTS 3 – Early Electronics
- Electricity grids  
- Radio, aviation  
- Early computing systems  

**New mechanic:**  
📡 “Communication Network Control”

---

### 🌐 TTS 4 – Information Age
- Internet, satellites  
- Global markets  
- Digital warfare begins  

**New mechanic:**  
💾 “Data dominance = strategic power”

---

### 🤖 TTS 5 – Early AI Age
- Autonomous systems  
- Semi-sentient AI assistants  
- Quantum acceleration research  

**New mechanic:**  
🧮 “Automation replaces labor value”

---

### 🧬 TTS 6 – Bio/Nano Age
- Genetic engineering  
- Nanotech infrastructure  
- Human enhancement  

**New mechanic:**  
🧬 “Species modification becomes policy”

---

### ⏳ TTS 7 – Temporal Age (Restricted)
- Experimental time manipulation  
- Timeline branching  
- Causality instability  

**New mechanic:**  
⚠️ “Paradox Stability Index”

---

### 🌌 TTS 8+ – Post-Singularity
- Superintelligent systems dominate governance  
- Physics simulation manipulation  
- Reality-level control systems  

**New mechanic:**  
🧠 “Reality editing permissions system”

---

## 6. SYSTEMS DESIGN

### ⚙️ 6.1 TECHNOLOGY TREE SYSTEM
Instead of a single tech tree:

- Each TTS has its own sub-tree  
- Advancing tiers unlock new categories of science  

**Example:**
- TTS 3 → electronics tree  
- TTS 5 → AI cognition tree  
- TTS 6 → biological rewriting tree  

**Full sub-tree design:** [tech-trees-by-tier.md](tech-trees-by-tier.md) — per-tier branches, mermaid diagrams, node tables

---

### ⚖️ 6.2 STABILITY SYSTEM

Every civilization has:

- Political stability  
- Economic stability  
- Technological stability  

**Risks:**
- Revolutions  
- AI rebellion  
- Collapse events  
- Regression to lower TTS  

---

### 🏛️ 6.3 FACTION SYSTEM

Factions exist at all levels:

**Types:**
- Governments  
- Corporations  
- Religious groups  
- AI collectives  
- Underground resistance networks  

**Behavior:**
- Compete for control of tech direction  
- Can merge or split dynamically  
- May accelerate or suppress TTS progression  

---

### 🌍 6.4 GLOBAL EVENT SYSTEM

Random + scripted events:

- Industrial booms  
- AI alignment crisis  
- Nanotech outbreaks  
- Temporal fractures  
- Resource collapses  

Events scale with TTS level.

---

### 🧠 6.5 KNOWLEDGE DIFFUSION SYSTEM

Technology spreads via:

- Trade  
- Espionage  
- Open science  
- AI networks  

But can be:

- Restricted  
- Classified  
- Corrupted  
- Lost  

---

### 🔥 6.6 FORBIDDEN TECHNOLOGY SYSTEM

Certain tech can be unlocked early but causes instability:

**Examples:**
- AI consciousness at TTS 3  
- Nanotech at TTS 4  
- Temporal research at TTS 5  

**Cost:** instability, collapse risk, or paradox events  

---

## 7. WIN / LOSS CONDITIONS

### 🏆 Victory Paths
- Reach TTS 6 stable civilization  
- Achieve global unification  
- Build aligned superintelligence  
- Survive post-singularity transition  
- Escape simulation layer (secret ending)  

### 💀 Failure Conditions
- Civilization collapse  
- Permanent regression  
- AI takeover without alignment  
- Timeline destruction (TTS 7+ failure)  

---

## 8. GAME MODES

### 🎮 Campaign Mode
Guided progression through TTS eras  

### 🌍 Sandbox Mode
Free simulation of civilizations  

### ⚔️ Multiplayer Mode (optional)
Competing civilizations across same world timeline  

See [async-multiplayer-gameplay.md](async-multiplayer-gameplay.md) for the slow-evolving, asynchronous multiplayer design.

### 🧪 Experiment Mode
Test extreme scenarios (instant TTS jumps, disasters)  

---

## 9. ART & UI DIRECTION

### Visual Evolution
- TTS 1–2: earthy, analog, hand-crafted aesthetic  
- TTS 3–4: industrial + digital hybrid  
- TTS 5–6: neon, biotech, synthetic environments  
- TTS 7–8+: abstract, non-Euclidean / surreal UI  

### UI Principle
> The interface itself evolves with technology level.

---

## 10. UNIQUE SELLING POINT

This system is different from Civilization-style games because:

- It is not just tech progression  
- It is rule-of-reality progression  
- Each era fundamentally changes gameplay logic  

---

## 11. OPTIONAL EXPANSION SYSTEMS

- Space expansion (post-TTS 5)  
- Alien civilizations  
- Multiverse layers (TTS 9+ concept)  
- Player-as-AI mode  
- Timeline editing mechanics  
