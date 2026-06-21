using Orleans.Configuration;
using TTS.Api.Models;
using TTS.Api.Services;
using TTS.Contracts;
using TTS.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "tts-dev";
        options.ServiceId = "tts-server";
    });
});

builder.Services.AddSingleton<MatchRegistry>();
builder.Services.AddSingleton<OrleansMatchService>();
builder.Services.AddHostedService<MatchTickBackgroundService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();
app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = ex switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        await context.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Unknown error" });
    });
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/matches", async (MatchRegistry registry, OrleansMatchService orleans) =>
{
    var items = new List<MatchListItemDto>();
    foreach (var entry in registry.List())
    {
        GrainMatchStatus? status = null;
        try { status = await orleans.GetGrain(entry.MatchId).GetStatusAsync(); }
        catch { /* grain not initialized yet */ }

        items.Add(new MatchListItemDto
        {
            MatchId = entry.MatchId,
            JoinCode = entry.JoinCode,
            ModeId = entry.ModeId,
            ModeDisplayName = entry.ModeDisplayName,
            Status = status?.Status ?? "Lobby",
            TickCount = status?.TickCount ?? 0,
            MaxTicks = status?.MaxTicks ?? MatchPresets.Resolve(entry.ModeId).MaxTicks,
            PlayerCount = entry.Players.Count,
            MaxPlayers = MatchPresets.Resolve(entry.ModeId).MaxPlayers,
            PendingGateCount = status?.PendingGates.Count ?? 0
        });
    }

    return Results.Ok(items);
});

app.MapPost("/api/matches", async (CreateMatchRequestDto request, MatchRegistry registry, OrleansMatchService orleans) =>
{
    var config = MatchPresets.Resolve(request.ModeId);
    var entry = registry.Create(config.ModeId, config.DisplayName);
    await orleans.InitializeMatchAsync(entry.MatchId, config.ModeId, request.WithDemoGate);

    return Results.Ok(new CreateMatchResponseDto
    {
        MatchId = entry.MatchId,
        JoinCode = entry.JoinCode,
        ModeDisplayName = entry.ModeDisplayName
    });
});

app.MapPost("/api/matches/join/{joinCode}", async (
    string joinCode,
    JoinMatchRequestDto request,
    MatchRegistry registry,
    OrleansMatchService orleans) =>
{
    var entry = registry.GetByJoinCode(joinCode)
        ?? throw new KeyNotFoundException($"No match with code '{joinCode}'.");

    var status = await orleans.GetGrain(entry.MatchId).GetStatusAsync();
    if (status.Status != "Lobby")
        throw new InvalidOperationException("Match has already started.");

    var player = registry.Join(entry.MatchId, request.PlayerName);
    return Results.Ok(new JoinMatchResponseDto
    {
        MatchId = entry.MatchId,
        PlayerId = player.PlayerId,
        PlayerName = player.PlayerName,
        CivilizationId = player.CivilizationId,
        CivilizationName = player.CivilizationName
    });
});

app.MapPost("/api/matches/{matchId}/join", async (
    string matchId,
    JoinMatchRequestDto request,
    MatchRegistry registry,
    OrleansMatchService orleans) =>
{
    _ = registry.GetById(matchId)
        ?? throw new KeyNotFoundException($"Match '{matchId}' not found.");

    var status = await orleans.GetGrain(matchId).GetStatusAsync();
    if (status.Status != "Lobby")
        throw new InvalidOperationException("Match has already started.");

    var player = registry.Join(matchId, request.PlayerName);
    return Results.Ok(new JoinMatchResponseDto
    {
        MatchId = matchId,
        PlayerId = player.PlayerId,
        PlayerName = player.PlayerName,
        CivilizationId = player.CivilizationId,
        CivilizationName = player.CivilizationName
    });
});

app.MapGet("/api/matches/{matchId}", async (string matchId, MatchRegistry registry, OrleansMatchService orleans) =>
{
    var entry = registry.GetById(matchId)
        ?? throw new KeyNotFoundException($"Match '{matchId}' not found.");

    var grain = orleans.GetGrain(matchId);
    await grain.AdvanceTickIfDueAsync();

    var status = await grain.GetStatusAsync();
    var gates = await grain.GetPendingGatesAsync();
    var civilizations = await grain.GetCivilizationsAsync();
    var regions = await grain.GetRegionsAsync();
    var tickLogs = await grain.GetMatchLogAsync();
    var results = await grain.GetMatchResultsAsync();

    string? awaySummary = null;
    if (status.TickCount > 0)
        awaySummary = await grain.GetAwaySummaryAsync(1, Math.Max(1, status.TickCount));

    string? resultsSummary = status.Status == "Ended"
        ? string.Join('\n', results.Select(r =>
            $"{r.Rank}. {r.CivilizationName} — TTS {r.Tier}, stability {r.Stability:F0}, {r.TechCount} techs ({r.Outcome})"))
        : null;

    var config = MatchPresets.Resolve(entry.ModeId);
    var readyCount = entry.Players.Count(p => p.IsReady);

    return Results.Ok(new MatchSummaryDto
    {
        MatchId = entry.MatchId,
        JoinCode = entry.JoinCode,
        ModeId = entry.ModeId,
        ModeDisplayName = status.ModeDisplayName,
        Status = status.Status,
        TickCount = status.TickCount,
        MaxTicks = status.MaxTicks,
        MinPlayers = config.MinPlayers,
        MaxPlayers = config.MaxPlayers,
        ReadyCount = readyCount,
        HostPlayerId = entry.HostPlayerId,
        NextTickAt = status.NextTickAt,
        SimulatedNow = status.SimulatedNow,
        IsTickDue = status.IsTickDue,
        VictoryTier = (int)config.VictoryTier,
        VictoryStabilityMin = config.VictoryStabilityMin,
        Players = entry.Players.Select(p => new PlayerSlotDto
        {
            PlayerId = p.PlayerId,
            PlayerName = p.PlayerName,
            CivilizationId = p.CivilizationId,
            CivilizationName = p.CivilizationName,
            IsReady = p.IsReady,
            IsHost = p.PlayerId == entry.HostPlayerId
        }).ToList(),
        Civilizations = civilizations.Select(c => new CivilizationDto
        {
            Id = c.Id,
            Name = c.Name,
            Tier = c.Tier,
            AverageStability = c.AverageStability,
            PoliticalStability = c.PoliticalStability,
            EconomicStability = c.EconomicStability,
            TechnologicalStability = c.TechnologicalStability,
            PolicyLabel = c.PolicyLabel,
            TechCount = c.TechCount
        }).ToList(),
        Regions = regions.Select(r => new RegionDto
        {
            Id = r.Id,
            Name = r.Name,
            ControllingCivilizationId = r.ControllingCivilizationId,
            ControllingCivilizationName = r.ControllingCivilizationName,
            Population = r.Population,
            Infrastructure = r.Infrastructure,
            Resources = r.Resources,
            SourceState = r.SourceState,
            DataYear = r.DataYear,
            GdpPerCapita = r.GdpPerCapita,
            UnemploymentRate = r.UnemploymentRate,
            PovertyRate = r.PovertyRate,
            EconomicHealth = r.EconomicHealth,
            CrimePressure = r.CrimePressure
        }).ToList(),
        PendingGates = gates.Select(g => new DecisionGateDto
        {
            GateId = g.GateId,
            CivilizationId = g.CivilizationId,
            CivilizationName = g.CivilizationName,
            Title = g.Title,
            Description = g.Description,
            Type = g.Type,
            DefaultOptionId = g.DefaultOptionId,
            ExpiresAt = g.ExpiresAt,
            Options = g.Options.Select(o => new DecisionOptionDto
            {
                Id = o.Id,
                Label = o.Label,
                Description = o.Description
            }).ToList()
        }).ToList(),
        AwaySummary = awaySummary,
        ResultsSummary = resultsSummary,
        Results = results.Select(r => new MatchResultEntryDto
        {
            Rank = r.Rank,
            CivilizationId = r.CivilizationId,
            CivilizationName = r.CivilizationName,
            Tier = r.Tier,
            Stability = r.Stability,
            TechCount = r.TechCount,
            Outcome = r.Outcome
        }).ToList(),
        TickLogs = tickLogs.Select(t => new TickLogEntryDto
        {
            Tick = t.Tick,
            Lines = t.Lines
        }).ToList()
    });
});

app.MapPost("/api/matches/{matchId}/decisions", async (
    string matchId,
    ResolveDecisionRequestDto request,
    OrleansMatchService orleans) =>
{
    var result = await orleans.GetGrain(matchId).ResolveDecisionAsync(
        request.CivilizationId, request.GateId, request.OptionId);

    return Results.Ok(new ResolveDecisionResponseDto
    {
        Success = result.Success,
        Message = result.Message,
        OptionId = result.OptionId
    });
});

app.MapGet("/api/matches/{matchId}/civs/{civilizationId}", async (
    string matchId,
    string civilizationId,
    OrleansMatchService orleans) =>
{
    var dashboard = await orleans.GetGrain(matchId).GetCivDashboardAsync(civilizationId);
    return Results.Ok(CivDashboardMapping.ToDto(dashboard));
});

app.MapGet("/api/matches/{matchId}/civs/{civilizationId}/advisor", async (
    string matchId,
    string civilizationId,
    OrleansMatchService orleans) =>
{
    var briefing = await orleans.GetGrain(matchId).GetAdvisorBriefingAsync(civilizationId);
    return Results.Ok(new AdvisorBriefingDto
    {
        Available = briefing.Available,
        Briefing = briefing.Briefing,
        Source = briefing.Source
    });
});

app.MapPut("/api/matches/{matchId}/civs/{civilizationId}/policy", async (
    string matchId,
    string civilizationId,
    UpdatePolicyRequestDto request,
    OrleansMatchService orleans) =>
{
    await orleans.GetGrain(matchId).UpdatePolicyAsync(civilizationId, request.PresetId);
    var dashboard = await orleans.GetGrain(matchId).GetCivDashboardAsync(civilizationId);
    return Results.Ok(CivDashboardMapping.ToDto(dashboard));
});

app.MapPost("/api/matches/{matchId}/ready", (string matchId, ReadyRequestDto request, MatchRegistry registry) =>
{
    var player = registry.SetReady(matchId, request.PlayerId, request.Ready);
    return Results.Ok(new { playerId = player.PlayerId, ready = player.IsReady });
});

app.MapPost("/api/matches/{matchId}/start", async (
    string matchId,
    StartMatchRequestDto request,
    MatchRegistry registry,
    OrleansMatchService orleans) =>
{
    registry.ValidateStart(matchId, request.PlayerId);
    await orleans.GetGrain(matchId).StartMatchAsync();
    return Results.Ok(new { started = true });
});

app.MapPost("/api/matches/{matchId}/tick", async (string matchId, OrleansMatchService orleans) =>
{
    var result = await orleans.GetGrain(matchId).AdvanceTickIfDueAsync();
    return Results.Ok(result);
});

app.Run();
