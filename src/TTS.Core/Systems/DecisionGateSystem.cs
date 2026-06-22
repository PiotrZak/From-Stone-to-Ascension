namespace TTS.Core.Systems;

using TTS.Core.Models;
using TTS.Core.Simulation;

public sealed class DecisionGateSystem
{
    private const double FactionCrisisThreshold = 40;
    private const double CrimeGatePressureThreshold = 65;

    public bool HasBlockingGate(Civilization civilization) =>
        civilization.ActiveGate is not null;

    public IReadOnlyList<DecisionGate> GetPendingGates(Civilization civilization) =>
        civilization.PendingDecisions.Where(g => !g.IsResolved).ToList();

    public void ExpireGates(WorldState world, SimulationServices services)
    {
        foreach (var civilization in world.Civilizations)
        {
            foreach (var gate in civilization.PendingDecisions.Where(g => !g.IsResolved && g.ExpiresAt <= world.SimulatedNow).ToList())
                Resolve(world, civilization, gate.Id, gate.DefaultOptionId, autoResolved: true, services);
        }
    }

    public void ScanAfterTurn(WorldState world, SimulationServices services, TurnSnapshot snapshot)
    {
        var decisionWindow = world.Match?.Config.DecisionWindow ?? TimeSpan.FromHours(12);
        var forbiddenGatesEnabled = world.Match?.Config.EnableForbiddenTechGates ?? true;

        foreach (var civilization in world.Civilizations)
        {
            if (HasBlockingGate(civilization))
                continue;

            if (forbiddenGatesEnabled && TryOpenForbiddenTechGate(world, civilization, decisionWindow))
                continue;

            if (TryOpenTierAdvancementGate(world, civilization, snapshot, decisionWindow))
                continue;

            if (TryOpenGlobalCrisisGate(world, civilization, decisionWindow))
                continue;

            if (TryOpenFactionCrisisGate(world, civilization, decisionWindow))
                continue;

            if (TryOpenCrimePressureGate(world, civilization, services, decisionWindow))
                continue;
        }
    }

    public GateResolutionResult Resolve(
        WorldState world,
        Civilization civilization,
        string gateId,
        string optionId,
        bool autoResolved,
        SimulationServices services)
    {
        var gate = civilization.PendingDecisions.FirstOrDefault(g => g.Id == gateId && !g.IsResolved);
        if (gate is null)
            return GateResolutionResult.Rejected("Gate not found or already resolved.");

        if (!gate.Options.Any(o => o.Id == optionId))
            return GateResolutionResult.Rejected($"Invalid option '{optionId}'.");

        ApplyOption(world, civilization, gate, optionId, services);

        gate.IsResolved = true;
        gate.ResolvedOptionId = optionId;
        gate.WasAutoResolved = autoResolved;

        services.RecordGateResolution(new GateResolutionRecord(
            civilization.Id,
            gate.Id,
            gate.Type,
            gate.Title,
            optionId,
            autoResolved));

        return GateResolutionResult.Succeeded(gate.Id, optionId, autoResolved);
    }

    private static void ApplyOption(
        WorldState world,
        Civilization civilization,
        DecisionGate gate,
        string optionId,
        SimulationServices services)
    {
        switch (gate.Type)
        {
            case GateType.ForbiddenTech:
                ApplyForbiddenTech(civilization, gate, optionId, world, services);
                break;
            case GateType.TierAdvancement:
                ApplyTierAdvancement(civilization, optionId, services);
                break;
            case GateType.GlobalCrisis:
                ApplyGlobalCrisis(civilization, optionId, services);
                break;
            case GateType.FactionCrisis:
                ApplyFactionCrisis(civilization, optionId);
                break;
            case GateType.CrimePressure:
                ApplyCrimePressure(civilization, optionId);
                break;
            case GateType.AiAlignment:
                ApplyAiAlignment(civilization, optionId, services);
                break;
        }
    }

    private static void ApplyForbiddenTech(
        Civilization civilization,
        DecisionGate gate,
        string optionId,
        WorldState world,
        SimulationServices services)
    {
        if (gate.ContextTechnologyId is null)
            return;

        var technology = world.Technologies.FirstOrDefault(t => t.Id == gate.ContextTechnologyId);
        if (technology is null)
            return;

        switch (optionId)
        {
            case "pursue":
                services.Research.Execute(civilization, technology, world);
                break;
            case "ban":
                civilization.BannedTechnologyIds.Add(technology.Id);
                break;
            case "delay":
                break;
        }
    }

    private static void ApplyTierAdvancement(Civilization civilization, string optionId, SimulationServices services)
    {
        switch (optionId)
        {
            case "embrace":
                services.Stability.ApplyInstability(civilization, 8);
                break;
            case "regulate":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 3, 0, 100);
                break;
            case "delay":
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability + 2, 0, 100);
                break;
        }
    }

    private static void ApplyGlobalCrisis(Civilization civilization, string optionId, SimulationServices services)
    {
        switch (optionId)
        {
            case "regulate":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 5, 0, 100);
                break;
            case "accelerate":
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability - 5, 0, 100);
                break;
            case "isolate":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 3, 0, 100);
                civilization.EconomicStability = Math.Clamp(civilization.EconomicStability + 3, 0, 100);
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability + 3, 0, 100);
                break;
        }
    }

    private static void ApplyFactionCrisis(Civilization civilization, string optionId)
    {
        switch (optionId)
        {
            case "appease":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 5, 0, 100);
                break;
            case "suppress":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 3, 0, 100);
                civilization.EconomicStability = Math.Clamp(civilization.EconomicStability - 3, 0, 100);
                break;
            case "reform":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 2, 0, 100);
                civilization.EconomicStability = Math.Clamp(civilization.EconomicStability + 2, 0, 100);
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability + 2, 0, 100);
                break;
        }
    }

    private static void ApplyCrimePressure(Civilization civilization, string optionId)
    {
        switch (optionId)
        {
            case "invest":
                civilization.EconomicStability = Math.Clamp(civilization.EconomicStability - 2, 0, 100);
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability + 3, 0, 100);
                break;
            case "ignore":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability - 3, 0, 100);
                break;
            case "crackdown":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 2, 0, 100);
                civilization.EconomicStability = Math.Clamp(civilization.EconomicStability - 2, 0, 100);
                break;
        }
    }

    private static void ApplyAiAlignment(Civilization civilization, string optionId, SimulationServices services)
    {
        switch (optionId)
        {
            case "align":
                civilization.TechnologicalStability = Math.Clamp(civilization.TechnologicalStability + 4, 0, 100);
                break;
            case "contain":
                civilization.PoliticalStability = Math.Clamp(civilization.PoliticalStability + 4, 0, 100);
                break;
            case "merge":
                services.Stability.ApplyInstability(civilization, 10);
                break;
        }
    }

    private bool TryOpenForbiddenTechGate(WorldState world, Civilization civilization, TimeSpan decisionWindow)
    {
        var forbidden = world.Technologies
            .Where(t => t.IsForbidden && !civilization.BannedTechnologyIds.Contains(t.Id))
            .FirstOrDefault(t =>
            {
                var key = $"forbidden:{t.Id}";
                if (civilization.OfferedGateKeys.Contains(key))
                    return false;

                return civilization.ResearchedTechnologyIds.IsSupersetOf(t.Prerequisites)
                    && !civilization.ResearchedTechnologyIds.Contains(t.Id)
                    && (int)t.Tier <= (int)civilization.CurrentTier + 1;
            });

        if (forbidden is null)
            return false;

        var key2 = $"forbidden:{forbidden.Id}";
        civilization.OfferedGateKeys.Add(key2);

        OpenGate(civilization, new DecisionGate(
            $"gate-{world.Turn}-{civilization.Id}-forbidden",
            civilization.Id,
            GateType.ForbiddenTech,
            $"Forbidden research: {forbidden.Name}",
            $"{forbidden.Name} is available before your society is ready. Risk level {forbidden.RiskLevel}.",
            [
                new DecisionOption("pursue", "Pursue", "Research now — high instability risk.", "Stability −8 · forbidden risk +"),
                new DecisionOption("ban", "Ban", "Forbid this line of research.", "Stability +2 · tech blocked"),
                new DecisionOption("delay", "Delay", "Defer the decision.", "Stability +1 · gate returns later")
            ],
            defaultOptionId: "ban",
            world.SimulatedNow,
            world.SimulatedNow + decisionWindow,
            contextTechnologyId: forbidden.Id));

        return true;
    }

    private bool TryOpenTierAdvancementGate(
        WorldState world,
        Civilization civilization,
        TurnSnapshot snapshot,
        TimeSpan decisionWindow)
    {
        if (!snapshot.CivilizationsAtStart.TryGetValue(civilization.Id, out var start))
            return false;

        if (civilization.CurrentTier <= start.Tier)
            return false;

        var key = $"tier:{(int)civilization.CurrentTier}";
        if (civilization.OfferedGateKeys.Contains(key))
            return false;

        civilization.OfferedGateKeys.Add(key);
        snapshot.TierChanges[civilization.Id] = new TierChangeRecord(start.Tier, civilization.CurrentTier);

        OpenGate(civilization, new DecisionGate(
            $"gate-{world.Turn}-{civilization.Id}-tier",
            civilization.Id,
            GateType.TierAdvancement,
            $"TTS {(int)civilization.CurrentTier} unlocked",
            $"Your civilization reached {civilization.CurrentTier}. How do you embrace the new era?",
            [
                new DecisionOption("embrace", "Embrace", "Push forward — accept disruption.", "Tier locked · stability −3 · tech +"),
                new DecisionOption("regulate", "Regulate", "Cautious adoption with oversight.", "Stability +2 · slower growth"),
                new DecisionOption("delay", "Delay", "Slow rollout to preserve stability.", "Stability +4 · tier pace −")
            ],
            defaultOptionId: "embrace",
            world.SimulatedNow,
            world.SimulatedNow + decisionWindow));

        return true;
    }

    private bool TryOpenGlobalCrisisGate(WorldState world, Civilization civilization, TimeSpan decisionWindow)
    {
        var crisis = world.ActiveEvents
            .Where(e => civilization.CurrentTier >= e.MinimumTier)
            .FirstOrDefault(e =>
            {
                var key = $"crisis:{e.Id}";
                return !civilization.OfferedGateKeys.Contains(key);
            });

        if (crisis is null)
            return false;

        civilization.OfferedGateKeys.Add($"crisis:{crisis.Id}");

        var gateType = crisis.MinimumTier >= TechTier.EarlyAI ? GateType.AiAlignment : GateType.GlobalCrisis;
        var options = gateType == GateType.AiAlignment
            ? new[]
            {
                new DecisionOption("align", "Align", "Integrate AI governance standards.", "Tech stability +3 · AI branch +"),
                new DecisionOption("contain", "Contain", "Restrict autonomous systems.", "Stability +2 · AI pace −"),
                new DecisionOption("merge", "Merge", "Deep integration — high risk.", "Tech +5 · stability −5")
            }
            : new[]
            {
                new DecisionOption("regulate", "Regulate", "Strengthen oversight.", "Stability +3 · growth −"),
                new DecisionOption("accelerate", "Accelerate", "Push through the crisis.", "Tech +2 · stability −4"),
                new DecisionOption("isolate", "Isolate", "Shield your civilization.", "Stability +1 · diffusion −")
            };

        var defaultOption = gateType == GateType.AiAlignment ? "contain" : "regulate";

        OpenGate(civilization, new DecisionGate(
            $"gate-{world.Turn}-{civilization.Id}-crisis",
            civilization.Id,
            gateType,
            crisis.Name,
            crisis.Description,
            options,
            defaultOption,
            world.SimulatedNow,
            world.SimulatedNow + decisionWindow,
            contextEventId: crisis.Id));

        return true;
    }

    private bool TryOpenFactionCrisisGate(WorldState world, Civilization civilization, TimeSpan decisionWindow)
    {
        if (civilization.AverageStability >= FactionCrisisThreshold)
            return false;

        const string key = "faction-crisis";
        if (civilization.OfferedGateKeys.Contains(key))
            return false;

        civilization.OfferedGateKeys.Add(key);

        OpenGate(civilization, new DecisionGate(
            $"gate-{world.Turn}-{civilization.Id}-faction",
            civilization.Id,
            GateType.FactionCrisis,
            "Faction crisis",
            $"Stability is critically low ({civilization.AverageStability:F0}). Factions demand a response.",
            [
                new DecisionOption("appease", "Appease", "Concessions to reduce unrest.", "Stability +4 · factions +2"),
                new DecisionOption("suppress", "Suppress", "Crack down — short-term control.", "Stability +1 · long-term −3"),
                new DecisionOption("reform", "Reform", "Structural reforms across pillars.", "Stability +2 · all pillars +1")
            ],
            defaultOptionId: "appease",
            world.SimulatedNow,
            world.SimulatedNow + decisionWindow));

        return true;
    }

    private bool TryOpenCrimePressureGate(
        WorldState world,
        Civilization civilization,
        SimulationServices services,
        TimeSpan decisionWindow)
    {
        if (civilization.CurrentTier < TechTier.InformationAge)
            return false;

        const string key = "crime-pressure";
        if (civilization.OfferedGateKeys.Contains(key))
            return false;

        var perspective = services.Crime.GetPerspective(civilization, world);
        if (!perspective.Available || perspective.AverageCrimePressure < CrimeGatePressureThreshold)
            return false;

        civilization.OfferedGateKeys.Add(key);

        OpenGate(civilization, new DecisionGate(
            $"gate-{world.Turn}-{civilization.Id}-crime",
            civilization.Id,
            GateType.CrimePressure,
            "Crime pressure spike",
            $"Regional crime pressure is {perspective.AverageCrimePressure:F0}. TTS 4 perspective demands a policy response.",
            [
                new DecisionOption("invest", "Invest", "Fund cybersecurity and social programs.", "Stability +4 · crime −8"),
                new DecisionOption("ignore", "Ignore", "Accept political erosion.", "Stability −3 · crime +"),
                new DecisionOption("crackdown", "Crackdown", "Law enforcement surge.", "Stability +1 · factions −2")
            ],
            defaultOptionId: "invest",
            world.SimulatedNow,
            world.SimulatedNow + decisionWindow));

        return true;
    }

    private static void OpenGate(Civilization civilization, DecisionGate gate) =>
        civilization.PendingDecisions.Add(gate);
}

public readonly record struct GateResolutionResult(bool Success, string Message, string? GateId = null, string? OptionId = null, bool AutoResolved = false)
{
    public static GateResolutionResult Succeeded(string gateId, string optionId, bool autoResolved) =>
        new(true, "Decision resolved.", gateId, optionId, autoResolved);

    public static GateResolutionResult Rejected(string reason) =>
        new(false, reason);
}
