using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Simulation;
using TTS.Llm.Tools;

namespace TTS.Tests;

public class GameToolRegistryTests
{
    [Fact]
    public void Registry_ExposesReadAndWriteTools()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);

        var registry = new GameToolRegistry(tools, player.Id, readOnly: false);
        var available = registry.AvailableTools().ToHashSet();

        Assert.Contains(GameTool.GetCivilizationState, available);
        Assert.Contains(GameTool.ProposeResearch, available);

        var readOnly = new GameToolRegistry(tools, player.Id, readOnly: true);
        Assert.DoesNotContain(GameTool.ProposeResearch, readOnly.AvailableTools());
    }

    [Fact]
    public void Registry_GetState_ReturnsJson()
    {
        var services = new SimulationServices();
        var world = SampleWorldFactory.Create();
        var tools = services.CreateToolSurface(world);
        var player = world.Civilizations.First(c => c.IsPlayerControlled);
        var registry = new GameToolRegistry(tools, player.Id);

        using var args = System.Text.Json.JsonDocument.Parse("{}");
        var result = registry.Execute(GameTool.GetCivilizationState, args.RootElement);

        Assert.True(result.Success, result.Error);
        Assert.Contains(player.Name, result.Json);
    }

    [Fact]
    public void GameTool_ToApiName_MatchesSnakeCase()
    {
        Assert.Equal("get_civilization_state", GameTool.GetCivilizationState.ToApiName());
        Assert.Equal("propose_research", GameTool.ProposeResearch.ToApiName());
    }

    [Fact]
    public void GameTool_TryParse_RoundTrips()
    {
        Assert.True(GameToolExtensions.TryParse("propose_research", out var tool));
        Assert.Equal(GameTool.ProposeResearch, tool);
    }
}
