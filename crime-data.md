# Crime & City Data — TTS 4+ Perspective

**Data file:** `src/data/state_crime_income_merged.csv`  
**Unlocks at:** TTS 4 (Information Age) for full perspective; city stats visible from match start  
**Systems:** `CrimeDataRepository`, `CrimeSystem`, `EconomySystem`  
**Economy design:** [economy.md](economy.md)

---

## Purpose

At **TTS 4**, civs gain a **socioeconomic perspective**: crime, poverty, inequality, and labor market data influence stability. The same CSV powers **city economy** (GDP, unemployment) from the first tick — see [economy.md §4–5](economy.md#4-region-city-attributes).

---

## Demo cities (not abstract regions)

| City | Civ | CSV anchor | Character |
|------|-----|------------|-----------|
| **Meridian Bay** | Aurora Collective | California 2015 | High GDP coastal metro |
| **Redstone Harbor** | Iron Dominion | Louisiana 2015 | Industrial port, higher poverty |

The UI shows both the **city name** and **data source** so California/Louisiana stats are intentional, not confusing metadata.

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

### Ollama gate text

- Crime gates are **not** narrated by Ollama below TTS 4 (hardcoded text only).
- At TTS 4+, prompts use in-game city names only — never “California” or US state names in the fable.
- The dev **demo gate** at match start is a TTS 1 **faction dispute**, not a crime gate.

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
