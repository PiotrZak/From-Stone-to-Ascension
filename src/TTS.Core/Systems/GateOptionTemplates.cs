namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>Immutable prototype option sets for decision gates — cloned by reference into gates.</summary>
public static class GateOptionTemplates
{
    public static readonly DecisionOption[] ForbiddenTech =
    [
        new("pursue", "Pursue", "Research now — high instability risk.", "Stability −8 · forbidden risk +"),
        new("ban", "Ban", "Forbid this line of research.", "Stability +2 · tech blocked"),
        new("delay", "Delay", "Defer the decision.", "Stability +1 · gate returns later")
    ];

    public static readonly DecisionOption[] TierAdvancement =
    [
        new("embrace", "Embrace", "Push forward — accept disruption.", "Tier locked · stability −3 · tech +"),
        new("regulate", "Regulate", "Cautious adoption with oversight.", "Stability +2 · slower growth"),
        new("delay", "Delay", "Slow rollout to preserve stability.", "Stability +4 · tier pace −")
    ];

    public static readonly DecisionOption[] AiAlignment =
    [
        new("align", "Align", "Integrate AI governance standards.", "Tech stability +3 · AI branch +"),
        new("contain", "Contain", "Restrict autonomous systems.", "Stability +2 · AI pace −"),
        new("merge", "Merge", "Deep integration — high risk.", "Tech +5 · stability −5")
    ];

    public static readonly DecisionOption[] GlobalCrisis =
    [
        new("regulate", "Regulate", "Strengthen oversight.", "Stability +3 · growth −"),
        new("accelerate", "Accelerate", "Push through the crisis.", "Tech +2 · stability −4"),
        new("isolate", "Isolate", "Shield your civilization.", "Stability +1 · diffusion −")
    ];

    public static readonly DecisionOption[] FactionCrisis =
    [
        new("appease", "Appease", "Concessions to reduce unrest.", "Stability +4 · factions +2"),
        new("suppress", "Suppress", "Crack down — short-term control.", "Stability +1 · long-term −3"),
        new("reform", "Reform", "Structural reforms across pillars.", "Stability +2 · all pillars +1")
    ];

    public static readonly DecisionOption[] CrimePressure =
    [
        new("invest", "Invest", "Fund cybersecurity and social programs.", "Stability +4 · crime −8"),
        new("ignore", "Ignore", "Accept political erosion.", "Stability −3 · crime +"),
        new("crackdown", "Crackdown", "Law enforcement surge.", "Stability +1 · factions −2")
    ];

    public static readonly DecisionOption[] DemoCrimePressure =
    [
        new("invest", "Invest", "Fund cybersecurity and digital literacy programs.", "Stability +4 · crime −8"),
        new("ignore", "Ignore", "Defer regulation — accept erosion.", "Stability −3 · short-term calm"),
        new("crackdown", "Crackdown", "Restrict networks and enforce compliance.", "Stability +1 · factions −2")
    ];

    public static readonly DecisionOption[] DemoFactionStone =
    [
        new("appease", "Appease", "Offer concessions to ease tensions.", "Stability +3 · factions +2"),
        new("suppress", "Suppress", "Send militia to restore order.", "Stability +1 · risk +2"),
        new("reform", "Reform", "Revise tolls and storage rules.", "Stability +2 · long-term balance")
    ];
}
