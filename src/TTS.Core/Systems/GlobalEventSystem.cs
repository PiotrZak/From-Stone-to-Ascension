namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Generates and resolves global events scaled by technology tier.
/// </summary>
public class GlobalEventSystem
{
    private readonly Random _random = new();

    public GlobalEvent? MaybeGenerateEvent(WorldState world)
    {
        var maxTier = world.Civilizations.DefaultIfEmpty().Max(c => c?.CurrentTier ?? TechTier.PreIndustrial);
        var chance = 0.1 + ((int)maxTier * 0.02);

        if (_random.NextDouble() > chance)
            return null;

        return maxTier switch
        {
            >= TechTier.Temporal => new GlobalEvent(
                $"evt-temporal-{world.Turn}",
                "Temporal Fracture",
                "Causality instability ripples across the timeline.",
                TechTier.Temporal,
                severity: 4,
                duration: 3),
            >= TechTier.EarlyAI => new GlobalEvent(
                $"evt-ai-{world.Turn}",
                "AI Alignment Crisis",
                "Autonomous systems disagree on governance priorities.",
                TechTier.EarlyAI,
                severity: 3,
                duration: 2),
            >= TechTier.Industrial => new GlobalEvent(
                $"evt-industrial-{world.Turn}",
                "Industrial Boom",
                "Production chains accelerate across connected regions.",
                TechTier.Industrial,
                severity: 1,
                duration: 2),
            _ => new GlobalEvent(
                $"evt-resource-{world.Turn}",
                "Resource Shortage",
                "Regional scarcity strains early settlements.",
                TechTier.PreIndustrial,
                severity: 2,
                duration: 2)
        };
    }

    public void TickEvents(WorldState world)
    {
        for (var i = world.ActiveEvents.Count - 1; i >= 0; i--)
        {
            world.ActiveEvents[i].RemainingTurns--;
            if (world.ActiveEvents[i].RemainingTurns <= 0)
                world.ActiveEvents.RemoveAt(i);
        }
    }

    public void EmitEvent(WorldState world, GlobalEvent globalEvent)
    {
        world.ActiveEvents.Add(globalEvent);
    }
}
