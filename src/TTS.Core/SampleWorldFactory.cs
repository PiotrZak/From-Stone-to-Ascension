namespace TTS.Core;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Creates a minimal playable world for demos and tests.</summary>
public static class SampleWorldFactory
{
    public static WorldState Create()
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

        var regionA = new Region("reg-a", "Green Basin") { ControllingCivilizationId = player.Id };
        var regionB = new Region("reg-b", "Iron Coast") { ControllingCivilizationId = rival.Id };

        AttachCrimeProfiles(regionA, regionB);

        player.ControlledRegionIds.Add(regionA.Id);
        rival.ControlledRegionIds.Add(regionB.Id);

        world.Civilizations.Add(player);
        world.Civilizations.Add(rival);
        world.Regions.Add(regionA);
        world.Regions.Add(regionB);

        world.Technologies.AddRange(CreateStarterTechnologies());

        world.KnowledgeNetworks.Add(new KnowledgeNetwork(player.Id, rival.Id, DiffusionChannel.Trade));
        world.KnowledgeNetworks.Add(new KnowledgeNetwork(rival.Id, player.Id, DiffusionChannel.Espionage));

        return world;
    }

    private static IEnumerable<Technology> CreateStarterTechnologies()
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

    private static void AttachCrimeProfiles(Region regionA, Region regionB)
    {
        var repo = CrimeDataRepository.Default;
        if (!repo.IsLoaded)
            return;

        // Demo mapping: player region ← California, rival ← Louisiana (contrasting crime/income)
        regionA.CrimeProfile = repo.ToProfile("California", 2015);
        regionB.CrimeProfile = repo.ToProfile("Louisiana", 2015);
    }
}
