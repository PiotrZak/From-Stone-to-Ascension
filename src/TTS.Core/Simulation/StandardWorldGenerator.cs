namespace TTS.Core.Simulation;

using TTS.Core.Models;

/// <summary>Fixed demo arena — Aurora vs Iron with CSV-anchored cities.</summary>
public sealed class StandardWorldGenerator : IWorldGenerator
{
    public (Civilization Player, Civilization Rival) Generate(WorldState world, WorldGenerationOptions options) =>
        WorldBlueprint.ApplyStandardArena(world);
}
