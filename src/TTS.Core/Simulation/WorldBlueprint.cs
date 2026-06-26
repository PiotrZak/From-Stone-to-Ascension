namespace TTS.Core.Simulation;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Prototype layout for the standard two-civilization arena — cloned into new matches.</summary>
public static class WorldBlueprint
{
    public const string PlayerCivId = "civ-player";
    public const string RivalCivId = "civ-rival";
    public const string PlayerRegionId = "reg-a";
    public const string RivalRegionId = "reg-b";

    private static readonly CivilizationPolicy PlayerPolicyPrototype = CivilizationPolicy.Balanced();
    private static readonly CivilizationPolicy RivalPolicyPrototype = CivilizationPolicy.TechRush();

    /// <summary>Populates civilizations, regions, factions, tech catalog, and knowledge links.</summary>
    public static (Civilization Player, Civilization Rival) ApplyStandardArena(WorldState world)
    {
        var player = CloneCivilization(PlayerCivId, "Aurora Collective", isPlayerControlled: true, PlayerPolicyPrototype);
        var rival = CloneCivilization(RivalCivId, "Iron Dominion", isPlayerControlled: false, RivalPolicyPrototype);

        player.Factions.Add(new Faction("fac-gov", "Central Council", player.Id, FactionType.Government, FactionStance.Neutral));
        player.Factions.Add(new Faction("fac-corp", "Helix Industries", player.Id, FactionType.Corporation, FactionStance.Accelerationist));
        rival.Factions.Add(new Faction("fac-ai", "Silent Lattice", rival.Id, FactionType.AiCollective, FactionStance.Accelerationist));

        var regionA = CloneRegion(PlayerRegionId, "Meridian Bay", player.Id, "California", 2015);
        var regionB = CloneRegion(RivalRegionId, "Redstone Harbor", rival.Id, "Louisiana", 2015);

        player.ControlledRegionIds.Add(regionA.Id);
        rival.ControlledRegionIds.Add(regionB.Id);

        world.Civilizations.Add(player);
        world.Civilizations.Add(rival);
        world.Regions.Add(regionA);
        world.Regions.Add(regionB);

        world.Technologies.AddRange(LoadTechnologies());

        world.KnowledgeNetworks.Add(new KnowledgeNetwork(player.Id, rival.Id, DiffusionChannel.Trade));
        world.KnowledgeNetworks.Add(new KnowledgeNetwork(rival.Id, player.Id, DiffusionChannel.Espionage));

        return (player, rival);
    }

    private static Civilization CloneCivilization(
        string id,
        string name,
        bool isPlayerControlled,
        CivilizationPolicy policyPrototype)
    {
        var civ = new Civilization(id, name, isPlayerControlled)
        {
            Policy = ClonePolicy(policyPrototype)
        };
        return civ;
    }

    private static CivilizationPolicy ClonePolicy(CivilizationPolicy prototype) => new()
    {
        Research = prototype.Research,
        Risk = prototype.Risk,
        Diplomacy = prototype.Diplomacy,
        BranchWeights = new Dictionary<string, double>(prototype.BranchWeights, StringComparer.OrdinalIgnoreCase)
    };

    private static Region CloneRegion(string id, string name, string ownerId, string crimeState, int crimeYear)
    {
        var region = new Region(id, name) { ControllingCivilizationId = ownerId };
        AttachCityProfile(region, crimeState, crimeYear);
        return region;
    }

    private static IEnumerable<Technology> LoadTechnologies()
    {
        var catalog = TechTreeCatalog.Default;
        if (catalog.IsLoaded && catalog.Technologies.Count > 0)
            return catalog.Technologies;

        return SampleWorldFactory.CreateFallbackTechnologiesOnly();
    }

    internal static void AttachCityProfile(Region region, string stateName, int year)
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
