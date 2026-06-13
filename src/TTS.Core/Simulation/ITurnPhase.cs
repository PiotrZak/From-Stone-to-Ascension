namespace TTS.Core.Simulation;

using TTS.Core.Models;

public interface ITurnPhase
{
    string Name { get; }
    void Execute(WorldState world, SimulationServices services);
}
