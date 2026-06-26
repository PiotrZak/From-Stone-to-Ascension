namespace TTS.Core.Simulation;

using TTS.Core;
using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Seeded civs, regions, factions, and knowledge links from data pools.</summary>
public sealed class ProceduralWorldGenerator : IWorldGenerator
{
    private static readonly CivilizationPolicy[] PolicyPool =
    [
        CivilizationPolicy.Balanced(),
        CivilizationPolicy.TechRush(),
        CivilizationPolicy.StabilityFirst(),
        CivilizationPolicy.Expansionist(),
        CivilizationPolicy.Diplomatic()
    ];

    private static readonly (FactionType Type, FactionStance Stance)[] FactionArchetypes =
    [
        (FactionType.Government, FactionStance.Neutral),
        (FactionType.Corporation, FactionStance.Accelerationist),
        (FactionType.AiCollective, FactionStance.Accelerationist),
        (FactionType.ReligiousGroup, FactionStance.Preservationist),
        (FactionType.UndergroundResistance, FactionStance.Preservationist)
    ];

    private static readonly DiffusionChannel[] DiffusionChannels =
    [
        DiffusionChannel.Trade,
        DiffusionChannel.Espionage,
        DiffusionChannel.OpenScience
    ];

    public (Civilization Player, Civilization Rival) Generate(WorldState world, WorldGenerationOptions options)
    {
        var rng = new Random(options.Seed);
        var anchors = PickStateAnchors(options, rng);

        var player = CreateCivilization(
            WorldBlueprint.PlayerCivId,
            WorldNameGenerator.CivilizationName(options.Seed, 0),
            isPlayerControlled: true,
            PickPolicy(rng, 0));
        var rival = CreateCivilization(
            WorldBlueprint.RivalCivId,
            WorldNameGenerator.CivilizationName(options.Seed, 1),
            isPlayerControlled: false,
            PickPolicy(rng, 1));

        AddFactions(player, options.Seed, 0, rng);
        AddFactions(rival, options.Seed, 1, rng);

        var playerRegion = CreateRegion(
            WorldBlueprint.PlayerRegionId,
            WorldNameGenerator.RegionName(options.Seed, 0, anchors[0].State),
            player.Id,
            anchors[0]);
        var rivalRegion = CreateRegion(
            WorldBlueprint.RivalRegionId,
            WorldNameGenerator.RegionName(options.Seed, 1, anchors[1].State),
            rival.Id,
            anchors[1]);

        player.ControlledRegionIds.Add(playerRegion.Id);
        rival.ControlledRegionIds.Add(rivalRegion.Id);

        world.Civilizations.Add(player);
        world.Civilizations.Add(rival);
        world.Regions.Add(playerRegion);
        world.Regions.Add(rivalRegion);
        world.Technologies.AddRange(LoadTechnologies());
        AddKnowledgeLinks(world, options.Seed, rng);

        return (player, rival);
    }

    private static Civilization CreateCivilization(
        string id,
        string name,
        bool isPlayerControlled,
        CivilizationPolicy policy)
    {
        var civ = new Civilization(id, name, isPlayerControlled) { Policy = ClonePolicy(policy) };
        return civ;
    }

    private static CivilizationPolicy ClonePolicy(CivilizationPolicy prototype) => new()
    {
        Research = prototype.Research,
        Risk = prototype.Risk,
        Diplomacy = prototype.Diplomacy,
        BranchWeights = new Dictionary<string, double>(prototype.BranchWeights, StringComparer.OrdinalIgnoreCase)
    };

    private static CivilizationPolicy PickPolicy(Random rng, int index)
    {
        var policy = PolicyPool[(index + rng.Next(PolicyPool.Length)) % PolicyPool.Length];
        return ClonePolicy(policy);
    }

    private static void AddFactions(Civilization civ, int seed, int civIndex, Random rng)
    {
        var count = 2 + rng.Next(2);
        var usedTypes = new HashSet<FactionType>();
        for (var i = 0; i < count; i++)
        {
            var archetype = FactionArchetypes[(civIndex + i + rng.Next(FactionArchetypes.Length)) % FactionArchetypes.Length];
            if (!usedTypes.Add(archetype.Type))
                continue;

            civ.Factions.Add(new Faction(
                $"fac-{civ.Id}-{i}",
                WorldNameGenerator.FactionName(seed, civIndex, i),
                civ.Id,
                archetype.Type,
                archetype.Stance)
            {
                Influence = 20 + rng.Next(40)
            });
        }

        if (civ.Factions.Count == 0)
        {
            civ.Factions.Add(new Faction(
                $"fac-{civ.Id}-0",
                WorldNameGenerator.FactionName(seed, civIndex, 0),
                civ.Id,
                FactionType.Government,
                FactionStance.Neutral));
        }
    }

    private static Region CreateRegion(string id, string name, string ownerId, StateAnchor anchor)
    {
        var region = new Region(id, name) { ControllingCivilizationId = ownerId };
        WorldBlueprint.AttachCityProfile(region, anchor.State, anchor.Year);
        return region;
    }

    private static void AddKnowledgeLinks(WorldState world, int seed, Random rng)
    {
        var civs = world.Civilizations.ToList();
        for (var i = 0; i < civs.Count; i++)
        {
            for (var j = 0; j < civs.Count; j++)
            {
                if (i == j)
                    continue;

                var channel = DiffusionChannels[(i + j + rng.Next(DiffusionChannels.Length)) % DiffusionChannels.Length];
                var strength = 0.35 + rng.NextDouble() * 0.55;
                world.KnowledgeNetworks.Add(new KnowledgeNetwork(civs[i].Id, civs[j].Id, channel)
                {
                    Strength = strength
                });
            }
        }
    }

    private static List<StateAnchor> PickStateAnchors(WorldGenerationOptions options, Random rng)
    {
        var repo = CrimeDataRepository.Default;
        if (options.UseCrimeDataAnchors && repo.IsLoaded)
        {
            var pool = repo.Records
                .GroupBy(r => r.State, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(r => r.Year).First())
                .OrderBy(_ => rng.Next())
                .Take(8)
                .Select(r => new StateAnchor(r.State, r.Year))
                .ToList();

            if (pool.Count >= 2)
                return [pool[0], pool[1]];
        }

        return
        [
            new StateAnchor("California", 2015),
            new StateAnchor("Louisiana", 2015)
        ];
    }

    private static IEnumerable<Technology> LoadTechnologies()
    {
        var catalog = TechTreeCatalog.Default;
        if (catalog.IsLoaded && catalog.Technologies.Count > 0)
            return catalog.Technologies;

        return SampleWorldFactory.CreateFallbackTechnologiesOnly();
    }

    private readonly record struct StateAnchor(string State, int Year);
}
