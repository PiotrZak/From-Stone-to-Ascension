using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using TTS.Contracts;
using TTS.Core;
using TTS.Core.Agents;
using TTS.Core.Models;
using TTS.Core.Simulation;

namespace TTS.Game;

internal static class GameCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        var options = ParseArgs(args);

        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (options.Mode == GameMode.Client)
            return await OrleansClientCli.RunAsync(options.ClientArgs, options.MatchId, options.Config.ModeId);

        return options.Mode switch
        {
            GameMode.Instant => RunInstantDemo(options),
            GameMode.Status => RunStatus(options),
            GameMode.Tick => RunSingleTick(options),
            GameMode.Watch => RunWatch(options),
            GameMode.New => RunNewMatch(options),
            _ => RunInstantDemo(options)
        };
    }

    private static int RunInstantDemo(GameOptions options)
    {
        var host = MatchHost.CreateNew(
            options.Config,
            options.SavePath,
            withDemoGate: true);
        var tools = host.Services.CreateToolSurface(host.World);
        var player = host.World.Civilizations.First(c => c.IsPlayerControlled);

        Console.WriteLine("TTS: Technology Tier Simulation (instant demo)");
        Console.WriteLine("=============================================");
        PrintWorldSummary(host.World, tools);
        SimulationReporter.PrintTechCatalog(host.World.Technologies, tools);

        var loop = host.Loop;
        for (var i = 0; i < options.Config.MaxTicks; i++)
        {
            if (i == 1)
            {
                var pending = tools.GetPendingDecisions(player.Id);
                if (pending.Count > 0)
                {
                    var gate = pending[0];
                    var resolved = host.ResolveDecision(player.Id, gate.Id, "invest");
                    Console.WriteLine($"  Resolved gate '{gate.Title}' → {resolved.OptionId}");
                }
            }

            if (host.World.Match is { Status: MatchStatus.Ended })
                break;

            var now = host.World.SimulatedNow + host.World.Match!.Config.TickInterval;
            host.World.Match.LastTickAt = now;
            host.World.Match.NextTickAt = now + host.World.Match.Config.TickInterval;

            var result = loop.RunTurn(now);
            MatchConsoleReporter.PrintTurn(result, host.World, tools);

            if (host.World.Match.TickCount >= host.World.Match.Config.MaxTicks)
            {
                host.World.Match.Status = MatchStatus.Ended;
                host.World.Match.EndedAt = now;
                break;
            }
        }

        if (host.World.Turn > 2)
        {
            Console.WriteLine();
            Console.WriteLine(tools.GetAwaySummary(1, host.World.Turn - 1).Format(host.World));
        }

        host.Save();
        PrintAgentSurface(tools, player);
        return 0;
    }

    private static int RunStatus(GameOptions options)
    {
        if (!File.Exists(options.SavePath))
        {
            Console.WriteLine($"No saved match at {options.SavePath}. Use --new to create one.");
            return 1;
        }

        var host = MatchHost.Load(options.SavePath);
        MatchConsoleReporter.PrintStatus(host.GetStatus(DateTimeOffset.UtcNow));
        return 0;
    }

    private static void EnsureRunning(MatchHost host)
    {
        if (host.World.Match?.Status == MatchStatus.Lobby)
            host.StartMatch(DateTimeOffset.UtcNow);
    }

    private static int RunSingleTick(GameOptions options)
    {
        var host = MatchHost.LoadOrCreate(options.SavePath, options.Config, options.ForceNew, withDemoGate: options.WithDemoGate);
        var now = DateTimeOffset.UtcNow;

        if (options.ForceNew)
            host.Save();

        EnsureRunning(host);

        var tick = host.TryRunDueTick(now);
        switch (tick.Outcome)
        {
            case MatchTickOutcome.NotDue:
                Console.WriteLine(tick.Message);
                MatchConsoleReporter.PrintStatus(host.GetStatus(now));
                return 0;
            case MatchTickOutcome.MatchEnded:
                Console.WriteLine(tick.Message);
                return 0;
            case MatchTickOutcome.Completed:
                var tools = host.Services.CreateToolSurface(host.World);
                MatchConsoleReporter.PrintTurn(tick.Turn!.Value, host.World, tools);
                Console.WriteLine();
                MatchConsoleReporter.PrintStatus(host.GetStatus(now));
                return 0;
            default:
                return 1;
        }
    }

    private static int RunWatch(GameOptions options)
    {
        var host = MatchHost.LoadOrCreate(options.SavePath, options.Config, options.ForceNew, withDemoGate: options.WithDemoGate);
        if (options.ForceNew)
            host.Save();

        EnsureRunning(host);

        var tools = host.Services.CreateToolSurface(host.World);
        Console.WriteLine($"Watching match — compression 1h → {options.CompressionSeconds}s");
        Console.WriteLine($"Save file: {options.SavePath}");
        MatchConsoleReporter.PrintStatus(host.GetStatus(DateTimeOffset.UtcNow));

        while (host.World.Match is { Status: MatchStatus.Running })
        {
            var now = DateTimeOffset.UtcNow;
            var status = host.GetStatus(now);

            if (!status.IsTickDue)
            {
                var wait = status.NextTickAt - now;
                var compressed = TimeSpan.FromSeconds(
                    Math.Max(1, wait.TotalSeconds / Math.Max(1, options.CompressionFactor)));

                Console.WriteLine($"Waiting {compressed.TotalSeconds:F0}s until next tick ({status.NextTickAt:u})...");
                Thread.Sleep(compressed);
                continue;
            }

            var tick = host.TryRunDueTick(now);
            if (tick.Outcome == MatchTickOutcome.Completed)
                MatchConsoleReporter.PrintTurn(tick.Turn!.Value, host.World, tools);
            else
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Match watch ended.");
        MatchConsoleReporter.PrintStatus(host.GetStatus(DateTimeOffset.UtcNow));
        return 0;
    }

    private static int RunNewMatch(GameOptions options)
    {
        if (File.Exists(options.SavePath))
            File.Delete(options.SavePath);

        var host = MatchHost.CreateNew(options.Config, options.SavePath, withDemoGate: options.WithDemoGate);
        host.Save();
        Console.WriteLine($"Created new match: {host.World.Match!.Config.DisplayName}");
        Console.WriteLine($"Saved to: {options.SavePath}");
        MatchConsoleReporter.PrintStatus(host.GetStatus(DateTimeOffset.UtcNow));
        return 0;
    }

    private static void PrintWorldSummary(WorldState world, GameToolSurface tools)
    {
        foreach (var civilization in world.Civilizations)
        {
            var snapshot = tools.GetCivilizationState(civilization.Id);
            Console.WriteLine($"{snapshot.Name} ({snapshot.Id}) — TTS {(int)snapshot.CurrentTier}");
            SimulationReporter.PrintPolicyBranches(civilization, tools);
        }

        Console.WriteLine($"Technologies loaded: {world.Technologies.Count}");
        Console.WriteLine($"Knowledge links: {world.KnowledgeNetworks.Count}");

        foreach (var region in world.Regions.Where(r => r.CrimeProfile is not null))
        {
            var p = region.CrimeProfile!;
            Console.WriteLine(
                $"Crime data [{region.Name}]: {p.SourceState} {p.DataYear} — pressure {p.CrimePressureIndex:F1} (TTS 4+)");
        }
    }

    private static void PrintAgentSurface(GameToolSurface tools, Civilization player)
    {
        Console.WriteLine();
        Console.WriteLine("Agent tool surface (MAF integration point):");
        var snapshot = tools.GetCivilizationState(player.Id);
        var analysis = tools.GetPolicyResearchAnalysis(player.Id);
        Console.WriteLine($"  {snapshot.Name} @ TTS {(int)snapshot.CurrentTier} — agent tools ready from TTS 5+");
        if (analysis.Recommended is { } next)
        {
            Console.WriteLine(
                $"  Next policy pick: {next.Name} ({next.Category}→{next.Branch}, score {next.TotalScore:F1})");
        }
    }

    private static GameOptions ParseArgs(string[] args)
    {
        var options = new GameOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    options.ShowHelp = true;
                    break;
                case "--client":
                    options.Mode = GameMode.Client;
                    options.ClientArgs = args[(i + 1)..];
                    return options;
                case "--instant":
                    options.Mode = GameMode.Instant;
                    break;
                case "--status":
                    options.Mode = GameMode.Status;
                    break;
                case "--tick":
                    options.Mode = GameMode.Tick;
                    break;
                case "--watch":
                    options.Mode = GameMode.Watch;
                    break;
                case "--new":
                    options.Mode = GameMode.New;
                    options.ForceNew = true;
                    break;
                case "--demo-gate":
                    options.WithDemoGate = true;
                    break;
                case "--mode" when i + 1 < args.Length:
                    options.Config = MatchPresets.Resolve(args[++i]);
                    break;
                case "--match-id" when i + 1 < args.Length:
                    options.MatchId = args[++i];
                    break;
                case "--save" when i + 1 < args.Length:
                    options.SavePath = args[++i];
                    break;
                case "--compression" when i + 1 < args.Length && int.TryParse(args[++i], out var factor):
                    options.CompressionFactor = Math.Max(1, factor);
                    options.CompressionSeconds = 3600 / options.CompressionFactor;
                    break;
            }
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            TTS: Technology Tier Simulation

            Usage:
              dotnet run --project src/TTS.Game                         Instant 8-turn demo (default)
              dotnet run --project src/TTS.Game -- --new                Create saved match
              dotnet run --project src/TTS.Game -- --tick               Run one tick if due
              dotnet run --project src/TTS.Game -- --watch              Compressed scheduled ticks
              dotnet run --project src/TTS.Game -- --status             Show match status

            Orleans client (silo must be running):
              dotnet run --project src/TTS.Game -- --client init [--demo-gate]
              dotnet run --project src/TTS.Game -- --client status
              dotnet run --project src/TTS.Game -- --client tick
              dotnet run --project src/TTS.Game -- --client resolve civ-player gate-id invest

            Options:
              --match-id demo           Grain / match id (Orleans client)
              --mode sprint-8h|blitz-24h|standard-36h|extended-48h
              --save path/to/match-state.json
              --compression 60          1 hour → 60 seconds in --watch mode
              --demo-gate               Inject crime briefing gate on new match
              --instant                 Force instant demo mode
            """);
    }

    private enum GameMode
    {
        Instant,
        Status,
        Tick,
        Watch,
        New,
        Client
    }

    private sealed class GameOptions
    {
        public GameMode Mode { get; set; } = GameMode.Instant;
        public MatchConfig Config { get; set; } = MatchPresets.Sprint8h;
        public string SavePath { get; set; } = MatchPersistence.DefaultSavePath;
        public string MatchId { get; set; } = "demo";
        public string[] ClientArgs { get; set; } = [];
        public bool ShowHelp { get; set; }
        public bool ForceNew { get; set; }
        public bool WithDemoGate { get; set; }
        public int CompressionFactor { get; set; } = 60;
        public int CompressionSeconds { get; set; } = 60;
    }
}
