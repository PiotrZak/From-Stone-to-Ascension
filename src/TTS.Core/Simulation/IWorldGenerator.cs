namespace TTS.Core.Simulation;

using TTS.Core.Models;

public interface IWorldGenerator
{
    (Civilization Player, Civilization Rival) Generate(WorldState world, WorldGenerationOptions options);
}

public static class WorldGenerators
{
    private static readonly IWorldGenerator Standard = new StandardWorldGenerator();
    private static readonly IWorldGenerator Procedural = new ProceduralWorldGenerator();

    public static IWorldGenerator Resolve(WorldGenerationOptions options) =>
        options.UseStandardArena ? Standard : Procedural;
}
