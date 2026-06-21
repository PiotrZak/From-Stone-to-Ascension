using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using TTS.Contracts;

namespace TTS.Game;

internal static class OrleansClientCli
{
    public static async Task<int> RunAsync(string[] clientArgs, string matchId, string modeId)
    {
        if (clientArgs.Length == 0)
        {
            PrintHelp(matchId);
            return 0;
        }

        using var host = Host.CreateDefaultBuilder()
            .UseOrleansClient(client =>
            {
                client.UseLocalhostClustering();
                client.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "tts-dev";
                    options.ServiceId = "tts-server";
                });
            })
            .Build();

        await host.StartAsync();
        var cluster = host.Services.GetRequiredService<IClusterClient>();
        var grain = cluster.GetGrain<IWorldGrain>(matchId);

        try
        {
            return clientArgs[0] switch
            {
                "init" => await RunInit(grain, modeId, clientArgs),
                "status" => await RunStatus(grain),
                "tick" => await RunTick(grain),
                "resolve" => await RunResolve(grain, clientArgs),
                "summary" => await RunSummary(grain, clientArgs),
                "help" or "-h" => PrintHelp(matchId),
                _ => Unknown(clientArgs[0])
            };
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task<int> RunInit(IWorldGrain grain, string modeId, string[] args)
    {
        var withDemoGate = args.Contains("--demo-gate");
        await grain.InitializeMatchAsync(modeId, withDemoGate);
        Console.WriteLine($"Initialized match on silo (mode={modeId})");
        return await RunStatus(grain);
    }

    private static async Task<int> RunStatus(IWorldGrain grain)
    {
        var status = await grain.GetStatusAsync();
        Console.WriteLine($"Match: {status.ModeDisplayName} ({status.MatchId})");
        Console.WriteLine($"Status: {status.Status} — tick {status.TickCount}/{status.MaxTicks}");
        Console.WriteLine($"Simulated: {status.SimulatedNow:u}");
        Console.WriteLine($"Next tick: {status.NextTickAt:u} {(status.IsTickDue ? "(DUE NOW)" : "")}");

        if (status.PendingGates.Count == 0)
            Console.WriteLine("Pending gates: none");
        else
        {
            Console.WriteLine($"Pending gates: {status.PendingGates.Count}");
            foreach (var gate in status.PendingGates)
                Console.WriteLine($"  [{gate.Type}] {gate.CivilizationName}: {gate.Title} — expires {gate.ExpiresAt:u}");
        }

        return 0;
    }

    private static async Task<int> RunTick(IWorldGrain grain)
    {
        var result = await grain.AdvanceTickIfDueAsync();
        switch (result.Outcome)
        {
            case GrainTickOutcomeKind.NotDue:
            case GrainTickOutcomeKind.MatchEnded:
                Console.WriteLine(result.Message);
                break;
            case GrainTickOutcomeKind.Completed:
                Console.WriteLine($"--- Tick {result.Turn} ---");
                foreach (var civ in result.Civilizations)
                    Console.WriteLine($"  {civ.Name}: TTS {civ.Tier}, stability {civ.Stability:F1}, techs {civ.TechCount}");
                foreach (var gate in result.ActiveGates)
                    Console.WriteLine($"  GATE [{gate.Type}]: {gate.Title} (default: {gate.DefaultOptionId})");
                break;
        }

        Console.WriteLine();
        await RunStatus(grain);
        return 0;
    }

    private static async Task<int> RunResolve(IWorldGrain grain, string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: --client resolve <civId> <gateId> <optionId>");
            return 1;
        }

        var result = await grain.ResolveDecisionAsync(args[1], args[2], args[3]);
        Console.WriteLine(result.Success
            ? $"Resolved: {result.OptionId}"
            : $"Failed: {result.Message}");
        return result.Success ? 0 : 1;
    }

    private static async Task<int> RunSummary(IWorldGrain grain, string[] args)
    {
        var from = args.Length > 1 && int.TryParse(args[1], out var f) ? f : 1;
        var to = args.Length > 2 && int.TryParse(args[2], out var t) ? t : from;
        Console.WriteLine(await grain.GetAwaySummaryAsync(from, to));
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.WriteLine($"Unknown client command: {command}");
        return 1;
    }

    private static int PrintHelp(string matchId)
    {
        Console.WriteLine($"""
            Orleans client — talks to TTS.Server silo (keep silo running in another terminal)

            Usage:
              dotnet run --project src/TTS.Game -- --client init [--demo-gate]
              dotnet run --project src/TTS.Game -- --client status
              dotnet run --project src/TTS.Game -- --client tick
              dotnet run --project src/TTS.Game -- --client resolve civ-player gate-demo-start invest
              dotnet run --project src/TTS.Game -- --client summary [fromTurn] [toTurn]

            Options (before --client args):
              --match-id demo          Grain key (default: demo)
              --mode sprint-8h         Match preset for init

            Match id: {matchId}
            """);
        return 0;
    }
}
