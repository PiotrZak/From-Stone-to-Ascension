namespace TTS.Api.Services;

using TTS.Contracts;

public sealed class OrleansMatchService(IClusterClient client)
{
    public IWorldGrain GetGrain(string matchId) => client.GetGrain<IWorldGrain>(matchId);

    public async Task InitializeMatchAsync(string matchId, string modeId, bool withDemoGate)
    {
        var grain = GetGrain(matchId);
        await grain.InitializeMatchAsync(modeId, withDemoGate);
    }
}
