namespace TTS.Core.Agents;

/// <summary>LLM-callable tools exposed via <see cref="IGameToolSurface"/>.</summary>
public enum GameTool
{
    GetCivilizationState,
    GetFactionTensions,
    GetAvailableTechnologies,
    GetPolicyResearchAnalysis,
    GetGlobalEvents,
    GetPendingDecisions,
    ProposeResearch,
    SetResearchPriority,
    ProposeDiplomaticAction,
}

public static class GameToolExtensions
{
    public static string ToApiName(this GameTool tool) => tool switch
    {
        GameTool.GetCivilizationState => "get_civilization_state",
        GameTool.GetFactionTensions => "get_faction_tensions",
        GameTool.GetAvailableTechnologies => "get_available_technologies",
        GameTool.GetPolicyResearchAnalysis => "get_policy_research_analysis",
        GameTool.GetGlobalEvents => "get_global_events",
        GameTool.GetPendingDecisions => "get_pending_decisions",
        GameTool.ProposeResearch => "propose_research",
        GameTool.SetResearchPriority => "set_research_priority",
        GameTool.ProposeDiplomaticAction => "propose_diplomatic_action",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
    };

    public static bool IsReadOnly(this GameTool tool) => tool switch
    {
        GameTool.GetCivilizationState => true,
        GameTool.GetFactionTensions => true,
        GameTool.GetAvailableTechnologies => true,
        GameTool.GetPolicyResearchAnalysis => true,
        GameTool.GetGlobalEvents => true,
        GameTool.GetPendingDecisions => true,
        GameTool.ProposeResearch => false,
        GameTool.SetResearchPriority => false,
        GameTool.ProposeDiplomaticAction => false,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)
    };

    public static bool TryParse(string? apiName, out GameTool tool)
    {
        foreach (var value in Enum.GetValues<GameTool>())
        {
            if (string.Equals(value.ToApiName(), apiName, StringComparison.Ordinal))
            {
                tool = value;
                return true;
            }
        }

        tool = default;
        return false;
    }

    public static IEnumerable<GameTool> AllReadOnly =>
        Enum.GetValues<GameTool>().Where(t => t.IsReadOnly());

    public static IEnumerable<GameTool> AllWrite =>
        Enum.GetValues<GameTool>().Where(t => !t.IsReadOnly());
}
