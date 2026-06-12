namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Spreads technology knowledge between civilizations via trade, espionage, and networks.
/// </summary>
public class KnowledgeDiffusionSystem
{
    public void Diffuse(WorldState world)
    {
        foreach (var link in world.KnowledgeNetworks)
        {
            var source = world.Civilizations.FirstOrDefault(c => c.Id == link.SourceCivilizationId);
            var target = world.Civilizations.FirstOrDefault(c => c.Id == link.TargetCivilizationId);
            if (source is null || target is null)
                continue;

            var transferable = source.ResearchedTechnologyIds
                .Where(id => !target.ResearchedTechnologyIds.Contains(id))
                .ToList();

            foreach (var techId in transferable)
            {
                if (_random.NextDouble() < link.Strength * 0.01)
                    link.KnownTechnologyIds.Add(techId);
            }
        }
    }

    public KnowledgeNetwork CreateLink(string sourceId, string targetId, DiffusionChannel channel)
    {
        return new KnowledgeNetwork(sourceId, targetId, channel);
    }

    private readonly Random _random = new();
}
