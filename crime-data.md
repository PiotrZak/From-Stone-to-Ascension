# Crime Data — TTS 4 Perspective

**Data file:** `src/data/state_crime_income_merged.csv`  
**Unlocks at:** TTS 4 (Information Age)  
**Systems:** `CrimeDataRepository`, `CrimeSystem`

---

## Purpose

At **TTS 4**, the simulation gains a **Crime perspective**: regional stability is influenced by real-world-inspired socioeconomic indicators — violent crime, poverty, inequality (Gini), unemployment, and corruption.

This models the Information Age theme from `tech-tree.md`:

- Cybersecurity systems  
- Information manipulation  
- Digital-era social pressure  

---

## Data source

`state_crime_income_merged.csv` contains US state-level records (2005–2015) with:

| Column group | Examples |
|--------------|----------|
| **Crime** | Violent crime, property crime, rates by offense type |
| **Economy** | GDP per capita, Gini coefficient, poverty rate, unemployment |
| **Governance** | Corruption convictions per million |

The game loads this file at runtime and maps **demo regions** to sample states:

| Game region | Civ | CSV state | Role |
|-------------|-----|-----------|------|
| Green Basin | Aurora Collective | California 2015 | High GDP, moderate crime pressure |
| Iron Coast | Iron Dominion | Louisiana 2015 | Higher poverty/inequality pressure |

---

## How it works in code

```
state_crime_income_merged.csv
        ↓
CrimeDataRepository (parse + cache)
        ↓
RegionalCrimeProfile on Region.CrimeProfile
        ↓
CrimeSystem.ApplyTurnPressure()  [TTS 4+ only]
        ↓
Political + economic stability penalty each turn
```

### Crime pressure index

Composite score 0–100 from:

- Violent crime rate (per 100k)
- Property crime rate  
- Poverty rate  
- Gini coefficient  
- Unemployment  
- Corruption convictions per million  

### Mitigation

Research **`tech-cybersecurity`** (TTS 4) reduces crime pressure impact by **40%**.

---

## Game output

From `TTS.Game` at TTS 4+:

```
Crime data [Green Basin]: California 2015 — pressure 42.3 (TTS 4+)
...
Aurora Collective: TTS 4, stability 65.2, ...
  Crime perspective: pressure 42.3, violent 396/100k, poverty 15.3%
```

---

## Ollama scenario

```bash
dotnet run --project src/TTS.Agents -- crime
```

Uses regional CSV-backed stats in the prompt for TTS 4 policy advice.

---

## Files

| File | Role |
|------|------|
| `src/data/state_crime_income_merged.csv` | Source data |
| `TTS.Core/Models/RegionalCrimeProfile.cs` | Region metrics model |
| `TTS.Core/Systems/CrimeDataRepository.cs` | CSV loader |
| `TTS.Core/Systems/CrimeSystem.cs` | TTS 4+ pressure + perspective API |
| `TTS.Core/SampleWorldFactory.cs` | Maps states → demo regions |

---

## Related

- [README.md](README.md) — TTS 4 Information Age  
- [tech-tree.md](tech-tree.md) — Cybersecurity, information systems  
- [ollama-scenarios.md](ollama-scenarios.md) — `crime` scenario
