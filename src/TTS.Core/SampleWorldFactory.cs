namespace TTS.Core;

using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;

/// <summary>Creates a minimal playable world for demos and tests.</summary>
public static class SampleWorldFactory
{
    public static WorldState Create(bool withDemoGate = false) =>
        Create(MatchPresets.Sprint8h, withDemoGate);

    public static WorldState Create(MatchConfig config, bool withDemoGate = false, string? matchId = null, bool useStandardArena = false, int? worldSeed = null)
    {
        var world = new WorldState();
        matchId ??= $"match-{Guid.NewGuid():N}"[..12];
        var generation = useStandardArena
            ? WorldGenerationOptions.Standard(matchId)
            : WorldGenerationOptions.FromMatch(config, matchId, worldSeed);

        var generator = WorldGenerators.Resolve(generation);
        var (player, _) = generator.Generate(world, generation);

        world.Match = new MatchState(matchId, config, DateTimeOffset.UtcNow, generation.Seed);
        HexMapBootstrap.Attach(world);

        if (withDemoGate)
            AttachDemoGate(world, player, config);

        if (config.StartingTier >= TechTier.InformationAge)
        {
            var services = new SimulationServices();
            InformationAgeTechSpine.ApplyBootstrap(world, services, config.StartingTier);
        }

        return world;
    }

    internal static IEnumerable<Technology> CreateFallbackTechnologiesOnly() => CreateFallbackTechnologies();

    /// <summary>Minimal spine when catalog.json is missing (tests in isolation).</summary>
    internal static IEnumerable<Technology> CreateFallbackTechnologies()
    {
        yield return new Technology("tech-agriculture", "Agriculture Systems", TechTier.PreIndustrial, TechCategory.Agriculture);
        yield return new Technology("tech-governance", "Basic Governance", TechTier.PreIndustrial, TechCategory.Agriculture, ["tech-agriculture"]);
        yield return new Technology("tech-metallurgy", "Metallurgy", TechTier.Industrial, TechCategory.Manufacturing, ["tech-governance"]);
        yield return new Technology("tech-steam", "Steam Power", TechTier.Industrial, TechCategory.Energy, ["tech-metallurgy"]);
        yield return new Technology("tech-electrical", "Electrical Grids", TechTier.EarlyElectronics, TechCategory.Energy, ["tech-steam"]);
        yield return new Technology("tech-computing", "Digital Computing", TechTier.InformationAge, TechCategory.Computing, ["tech-electrical"]);
        yield return new Technology(
            "tech-cybersecurity",
            "Cybersecurity Systems",
            TechTier.InformationAge,
            TechCategory.Computing,
            ["tech-computing"],
            riskLevel: 5,
            fusionTags: ["cyber", "crime"]);
        yield return new Technology("tech-ml", "Machine Learning", TechTier.InformationAge, TechCategory.Computing, ["tech-computing"]);
        yield return new Technology(
            "tech-agi",
            "Artificial General Intelligence",
            TechTier.EarlyAI,
            TechCategory.ArtificialIntelligence,
            ["tech-ml"],
            riskLevel: 25,
            isForbidden: false,
            fusionTags: ["ai"]);
        yield return new Technology(
            "tech-recursive-ai",
            "Self-aware Recursive AI",
            TechTier.EarlyAI,
            TechCategory.ArtificialIntelligence,
            ["tech-ml"],
            riskLevel: 60,
            isForbidden: true,
            fusionTags: ["ai", "forbidden"]);
    }

    private static void AttachDemoGate(WorldState world, Civilization player, MatchConfig config)
    {
        if (world.Match is null)
            return;

        var window = world.Match.Config.DecisionWindow;

        if (config.StartingTier >= TechTier.InformationAge)
        {
            var capital = world.Regions.FirstOrDefault(r => r.ControllingCivilizationId == player.Id);
            player.PendingDecisions.Add(new DecisionGate(
                "gate-demo-start",
                player.Id,
                GateType.CrimePressure,
                "Data sovereignty dispute",
                $"Platform regulators and civic groups clash over cross-border data flows in {CapitalName(world, player)}.",
                GateOptionTemplates.DemoCrimePressure,
                defaultOptionId: "invest",
                world.SimulatedNow,
                world.SimulatedNow + window,
                contextRegionId: capital?.Id));
            return;
        }

        var stoneCapital = world.Regions.FirstOrDefault(r => r.ControllingCivilizationId == player.Id);
        var leadingFaction = player.Factions.OrderByDescending(f => f.Influence).FirstOrDefault();
        player.PendingDecisions.Add(new DecisionGate(
            "gate-demo-start",
            player.Id,
            GateType.FactionCrisis,
            $"Granary dispute in {CapitalName(world, player)}",
            $"Merchants and farmers quarrel over grain storage and road tolls as {CapitalName(world, player)} grows.",
            GateOptionTemplates.DemoFactionStone,
            defaultOptionId: "appease",
            world.SimulatedNow,
            world.SimulatedNow + window,
            contextRegionId: stoneCapital?.Id,
            contextFactionId: leadingFaction?.Id));
    }

    private static string CapitalName(WorldState world, Civilization civ) =>
        world.Regions.FirstOrDefault(r => r.ControllingCivilizationId == civ.Id)?.Name ?? "the capital";
}
