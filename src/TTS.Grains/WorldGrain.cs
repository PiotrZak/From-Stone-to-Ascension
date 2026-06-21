using Orleans;
using TTS.Contracts;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;
using TTS.Core.Systems;
using TTS.Llm;
using TTS.Llm.Agents;

namespace TTS.Grains;

public sealed class WorldGrain : Grain, IWorldGrain
{
    private static readonly GateFableGenerator FableGenerator = new();
    private static readonly ILlmTurnAgent? SharedTurnAgent = AgentProviderFactory.CreateTurnAgent();
    private static readonly AdvisorAgent AdvisorAgent = new();
    private MatchHost? _host;
    private IGrainTimer? _tickTimer;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var savePath = ResolveSavePath(this.GetPrimaryKeyString());
        if (File.Exists(savePath))
            _host = MatchHost.Load(savePath, SharedTurnAgent);

        if (_host?.World.Match?.Status == MatchStatus.Running)
            RegisterTickTimer();

        await base.OnActivateAsync(cancellationToken);
    }

    public Task InitializeMatchAsync(string modeId, bool withDemoGate = false)
    {
        var savePath = ResolveSavePath(this.GetPrimaryKeyString());
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

        _host = MatchHost.CreateNew(MatchPresets.Resolve(modeId), savePath, withDemoGate, llmTurnAgent: SharedTurnAgent);
        _host.Save();
        return Task.CompletedTask;
    }

    public async Task<GrainMatchStatus> GetStatusAsync()
    {
        await EnrichGateFablesAsync();
        return GrainMapping.ToGrain(RequireHost().GetStatus(DateTimeOffset.UtcNow));
    }

    public async Task<GrainTickResult> AdvanceTickIfDueAsync()
    {
        var host = RequireHost();
        var result = host.TryRunDueTick(DateTimeOffset.UtcNow);
        await EnrichGateFablesAsync();

        if (host.World.Match?.Status != MatchStatus.Running)
            StopTickTimer();

        return GrainMapping.ToGrain(result);
    }

    public Task<GrainDecisionResult> ResolveDecisionAsync(string civilizationId, string gateId, string optionId)
    {
        var result = RequireHost().ResolveDecision(civilizationId, gateId, optionId);
        return Task.FromResult(GrainMapping.ToGrain(result));
    }

    public Task<string> GetAwaySummaryAsync(int fromTurn, int toTurn)
    {
        var host = RequireHost();
        var summary = host.Services.AwaySummary.Build(host.World, host.Services.TurnHistory, fromTurn, toTurn);
        return Task.FromResult(summary.Format(host.World));
    }

    public async Task<IReadOnlyList<GrainDecisionGateDetail>> GetPendingGatesAsync(string? civilizationId = null)
    {
        await EnrichGateFablesAsync();
        var host = RequireHost();
        var gates = host.World.Civilizations
            .SelectMany(c => c.PendingDecisions.Where(g => !g.IsResolved).Select(g => (c, g)))
            .Where(x => civilizationId is null || x.c.Id == civilizationId)
            .Select(x => new GrainDecisionGateDetail(
                x.g.Id,
                x.c.Id,
                x.c.Name,
                x.g.Title,
                x.g.DisplayText,
                x.g.Type.ToString(),
                x.g.DefaultOptionId,
                x.g.ExpiresAt,
                x.g.Options.Select(o => new GrainDecisionOptionDetail(o.Id, o.Label, o.Description)).ToList()))
            .ToList();

        return gates;
    }

    public Task<IReadOnlyList<GrainCivDetail>> GetCivilizationsAsync()
    {
        var host = RequireHost();
        var civs = host.World.Civilizations
            .Select(c => new GrainCivDetail(
                c.Id,
                c.Name,
                (int)c.CurrentTier,
                c.AverageStability,
                c.PoliticalStability,
                c.EconomicStability,
                c.TechnologicalStability,
                c.Policy.Research.ToString(),
                c.ResearchedTechnologyIds.Count))
            .ToList();

        return Task.FromResult<IReadOnlyList<GrainCivDetail>>(civs);
    }

    public Task<IReadOnlyList<GrainMatchResultEntry>> GetMatchResultsAsync()
    {
        var host = RequireHost();
        var results = host.GetResults()
            .Select(r => new GrainMatchResultEntry(
                r.Rank,
                r.CivilizationId,
                r.CivilizationName,
                r.Tier,
                r.Stability,
                r.TechCount,
                r.Outcome))
            .ToList();

        return Task.FromResult<IReadOnlyList<GrainMatchResultEntry>>(results);
    }

    public Task<IReadOnlyList<GrainTickLogEntry>> GetMatchLogAsync()
    {
        var host = RequireHost();
        var log = host.GetTickLog()
            .Select(e => new GrainTickLogEntry(e.Tick, e.Lines.ToList()))
            .ToList();

        return Task.FromResult<IReadOnlyList<GrainTickLogEntry>>(log);
    }

    public Task<GrainCivDashboard> GetCivDashboardAsync(string civilizationId)
    {
        var host = RequireHost();
        var tools = host.CreateToolSurface();
        var analysis = tools.GetPolicyResearchAnalysis(civilizationId);
        var available = tools.GetAvailableTechnologies(civilizationId);
        var civ = host.World.Civilizations.First(c => c.Id == civilizationId);
        var crime = tools.GetCrimePerspective(civilizationId);

        var researched = host.World.Technologies
            .Where(t => civ.ResearchedTechnologyIds.Contains(t.Id))
            .OrderBy(t => t.Tier)
            .Select(ToTechEntry)
            .ToList();

        var availableList = available
            .OrderBy(t => t.Tier)
            .Take(12)
            .Select(ToTechEntry)
            .ToList();

        GrainRecommendedTech? recommended = analysis.Recommended is { } rec
            ? new GrainRecommendedTech(rec.TechnologyId, rec.Name, (int)rec.Tier, rec.Branch, rec.TotalScore)
            : null;

        GrainCrimePerspective? crimeDto = crime.Available
            ? new GrainCrimePerspective(
                crime.AverageCrimePressure,
                crime.AverageViolentCrimeRate,
                crime.AveragePovertyRate,
                crime.CybersecurityMitigationActive,
                crime.Regions?.Select(r => new GrainRegionCrime(r.RegionName, r.SourceState, r.CrimePressureIndex)).ToList() ?? [])
            : null;

        var techTree = TechTreeViewBuilder.Build(civ, host.World, host.Services.TechTree)
            .Select(n => new GrainTechTreeNode(
                n.Id,
                n.Name,
                n.Tier,
                n.Branch,
                n.Role,
                n.Prerequisites.ToList(),
                n.RiskLevel,
                n.IsForbidden,
                n.Status))
            .ToList();

        return Task.FromResult(new GrainCivDashboard(
            civilizationId,
            DetectPresetId(civ.Policy),
            analysis.ResearchStance.ToString(),
            analysis.RiskTolerance.ToString(),
            analysis.BranchWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            recommended,
            researched,
            availableList,
            crimeDto,
            techTree,
            ResearchThroughput.SlotsFor(civ)));
    }

    public Task UpdatePolicyAsync(string civilizationId, string presetId)
    {
        RequireHost().UpdatePolicy(civilizationId, presetId);
        return Task.CompletedTask;
    }

    public async Task<GrainAdvisorBriefing> GetAdvisorBriefingAsync(string civilizationId)
    {
        var host = RequireHost();
        var civ = host.World.Civilizations.FirstOrDefault(c => c.Id == civilizationId);
        if (civ is null || (int)civ.CurrentTier < (int)TechTier.EarlyAI)
        {
            return new GrainAdvisorBriefing(
                false,
                "Strategic advisor unlocks at TTS 5 (Early AI Age).",
                "system");
        }

        var settings = AgentProviderSettings.FromEnvironment();
        if (!settings.AgentsEnabled)
        {
            var tools = host.CreateToolSurface();
            var analysis = tools.GetPolicyResearchAnalysis(civilizationId);
            var rec = analysis.Recommended?.Name ?? "none";
            return new GrainAdvisorBriefing(
                true,
                $"Classical advisor: policy {analysis.ResearchStance}, risk {analysis.RiskTolerance}. Recommended research: {rec}.",
                "classical");
        }

        var briefing = await AdvisorAgent.GetBriefingAsync(civilizationId, host.CreateToolSurface());
        return briefing is not null
            ? new GrainAdvisorBriefing(true, briefing, "llm-tools")
            : new GrainAdvisorBriefing(
                true,
                "Advisor unavailable — use policy panel and tech tree. Set TTS_LLM_PROVIDER=none to skip LLM.",
                "fallback");
    }

    public Task StartMatchAsync()
    {
        RequireHost().StartMatch(DateTimeOffset.UtcNow);
        RegisterTickTimer();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GrainRegionDetail>> GetRegionsAsync()
    {
        var host = RequireHost();
        var civNames = host.World.Civilizations.ToDictionary(c => c.Id, c => c.Name);
        var regions = host.World.Regions
            .Select(r =>
            {
                var profile = r.CrimeProfile;
                civNames.TryGetValue(r.ControllingCivilizationId ?? "", out var civName);
                return new GrainRegionDetail(
                    r.Id,
                    r.Name,
                    r.ControllingCivilizationId,
                    civName,
                    r.Population,
                    r.Infrastructure,
                    r.Resources,
                    profile?.SourceState,
                    profile?.DataYear,
                    profile?.GdpPerCapita ?? 0,
                    profile?.UnemploymentRate ?? 0,
                    profile?.PovertyRate ?? 0,
                    profile?.EconomicHealthIndex ?? Math.Clamp(r.Infrastructure + r.Resources * 0.5, 0, 100),
                    profile?.CrimePressureIndex ?? 0);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<GrainRegionDetail>>(regions);
    }

    private void RegisterTickTimer()
    {
        var match = _host?.World.Match;
        if (match is not { Status: MatchStatus.Running })
            return;

        _tickTimer?.Dispose();
        var period = TickPollPeriod(match.Config);
        _tickTimer = this.RegisterGrainTimer(
            async _ => await AdvanceTickIfDueAsync(),
            new GrainTimerCreationOptions
            {
                DueTime = period,
                Period = period,
                Interleave = true
            });
    }

    private void StopTickTimer()
    {
        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    private static TimeSpan TickPollPeriod(MatchConfig config)
    {
        if (config.TickInterval <= TimeSpan.FromMinutes(1))
            return TimeSpan.FromSeconds(5);

        return TimeSpan.FromMinutes(1);
    }

    private static GrainTechEntry ToTechEntry(TTS.Core.Models.Technology t)
    {
        var branch = TechBranchMapping.Describe(t);
        return new GrainTechEntry(t.Id, t.Name, (int)t.Tier, branch.Branch);
    }

    private static string DetectPresetId(CivilizationPolicy policy)
    {
        foreach (var (id, _) in PolicyPresets.All)
        {
            var preset = PolicyPresets.Resolve(id);
            if (preset.Research == policy.Research && preset.Risk == policy.Risk)
                return id;
        }

        return "custom";
    }

    private async Task EnrichGateFablesAsync()
    {
        var host = _host;
        if (host is null)
            return;

        foreach (var civ in host.World.Civilizations)
        {
            var gate = civ.PendingDecisions.FirstOrDefault(g => !g.IsResolved && string.IsNullOrWhiteSpace(g.Fable));
            if (gate is null)
                continue;

            var fable = await FableGenerator.GenerateAsync(
                civ.Name,
                gate.Type,
                gate.Title,
                gate.Description,
                (int)civ.CurrentTier);

            if (string.IsNullOrWhiteSpace(fable))
                return;

            gate.Fable = fable;
            host.Save();
            return;
        }
    }

    private MatchHost RequireHost() =>
        _host ?? throw new InvalidOperationException("World grain is not initialized. Call InitializeMatchAsync first.");

    private static string ResolveSavePath(string matchId) =>
        Path.Combine(AppContext.BaseDirectory, "matches", $"{matchId}.json");
}
