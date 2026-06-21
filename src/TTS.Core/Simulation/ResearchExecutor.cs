namespace TTS.Core.Simulation;

using TTS.Core.Models;
using TTS.Core.Systems;

/// <summary>Single validated path for applying technology research.</summary>
public sealed class ResearchExecutor
{
    private readonly TechTreeSystem _techTree;
    private readonly ForbiddenTechSystem _forbiddenTech;

    public ResearchExecutor(TechTreeSystem techTree, ForbiddenTechSystem forbiddenTech)
    {
        _techTree = techTree;
        _forbiddenTech = forbiddenTech;
    }

    public ResearchResult Execute(Civilization civilization, Technology technology, WorldState world) =>
        _techTree.Research(civilization, technology, _forbiddenTech, world);

    public ResearchResult ExecuteById(Civilization civilization, WorldState world, string technologyId)
    {
        var technology = world.Technologies.FirstOrDefault(t => t.Id == technologyId);
        if (technology is null)
            return ResearchResult.Rejected($"Technology '{technologyId}' not found.");

        return Execute(civilization, technology, world);
    }
}
