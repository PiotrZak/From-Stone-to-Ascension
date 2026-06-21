namespace TTS.Core;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Creates a minimal playable world for demos and tests.</summary>
public static class SampleWorldFactory
{
    public static WorldState Create(bool withDemoGate = false) =>
        Create(MatchPresets.Sprint8h, withDemoGate);

    public static WorldState Create(MatchConfig config, bool withDemoGate = false)
    {
        var world = new WorldState();

        var player = new Civilization("civ-player", "Aurora Collective", isPlayerControlled: true)
        {
            Policy = CivilizationPolicy.Balanced()
        };
        var rival = new Civilization("civ-rival", "Iron Dominion", isPlayerControlled: false)
        {
            Policy = CivilizationPolicy.TechRush()
        };

        player.Factions.Add(new Faction("fac-gov", "Central Council", player.Id, FactionType.Government, FactionStance.Neutral));
        player.Factions.Add(new Faction("fac-corp", "Helix Industries", player.Id, FactionType.Corporation, FactionStance.Accelerationist));
        rival.Factions.Add(new Faction("fac-ai", "Silent Lattice", rival.Id, FactionType.AiCollective, FactionStance.Accelerationist));

        var regionA = new Region("reg-a", "Meridian Bay") { ControllingCivilizationId = player.Id };
        var regionB = new Region("reg-b", "Redstone Harbor") { ControllingCivilizationId = rival.Id };

        AttachCityProfile(regionA, "California", 2015);
        AttachCityProfile(regionB, "Louisiana", 2015);

        player.ControlledRegionIds.Add(regionA.Id);
        rival.ControlledRegionIds.Add(regionB.Id);

        world.Civilizations.Add(player);
        world.Civilizations.Add(rival);
        world.Regions.Add(regionA);
        world.Regions.Add(regionB);

        world.Technologies.AddRange(LoadTechnologies());

        var match = new MatchState($"match-{Guid.NewGuid():N}"[..12], config, DateTimeOffset.UtcNow);
        world.Match = match;

        if (withDemoGate)
            AttachDemoGate(world, player);

        world.KnowledgeNetworks.Add(new KnowledgeNetwork(player.Id, rival.Id, DiffusionChannel.Trade));
        world.KnowledgeNetworks.Add(new KnowledgeNetwork(rival.Id, player.Id, DiffusionChannel.Espionage));

        return world;
    }

    internal static IEnumerable<Technology> CreateFallbackTechnologiesOnly() => CreateFallbackTechnologies();

    private static IEnumerable<Technology> LoadTechnologies()
    {
        var catalog = TechTreeCatalog.Default;
        if (catalog.IsLoaded && catalog.Technologies.Count > 0)
            return catalog.Technologies;

        return CreateFallbackTechnologies();
    }

    /// <summary>Minimal spine when catalog.json is missing (tests in isolation).</summary>
    private static IEnumerable<Technology> CreateFallbackTechnologies()
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

    private static void AttachDemoGate(WorldState world, Civilization player)
    {
        if (world.Match is null)
            return;

        var window = world.Match.Config.DecisionWindow;
        player.PendingDecisions.Add(new DecisionGate(
            "gate-demo-start",
            player.Id,
            GateType.FactionCrisis,
            "Granary dispute in Meridian Bay",
            "Merchants and farmers quarrel over grain storage and road tolls as the young settlement grows.",
            [
                new DecisionOption("appease", "Appease", "Offer concessions to ease tensions."),
                new DecisionOption("suppress", "Suppress", "Send militia to restore order."),
                new DecisionOption("reform", "Reform", "Revise tolls and storage rules.")
            ],
            defaultOptionId: "appease",
            world.SimulatedNow,
            world.SimulatedNow + window));
    }

    private static void AttachCityProfile(Region region, string stateName, int year)
    {
        var repo = CrimeDataRepository.Default;
        if (!repo.IsLoaded)
            return;

        var record = repo.GetRecord(stateName, year);
        var profile = repo.ToProfile(stateName, year);
        if (profile is null)
            return;

        region.CrimeProfile = profile;
        if (record is not null)
            region.Population = record.Population;

        region.Infrastructure = Math.Clamp(profile.GdpPerCapita / 900.0, 20, 90);
        region.Resources = Math.Clamp(100 - profile.PovertyRate * 2.5, 25, 90);
    }
}
