using TTS.Core.Models;
using TTS.Llm;

namespace TTS.Tests;

public class GateFableTests
{
    [Fact]
    public void ShouldEnrich_CrimeGate_RequiresTts4()
    {
        Assert.False(GateFableGenerator.ShouldEnrich(GateType.CrimePressure, (int)TechTier.PreIndustrial));
        Assert.False(GateFableGenerator.ShouldEnrich(GateType.CrimePressure, (int)TechTier.EarlyElectronics));
        Assert.True(GateFableGenerator.ShouldEnrich(GateType.CrimePressure, (int)TechTier.InformationAge));
    }

    [Fact]
    public void ShouldEnrich_FactionGate_AllowedAtTts1()
    {
        Assert.True(GateFableGenerator.ShouldEnrich(GateType.FactionCrisis, (int)TechTier.PreIndustrial));
    }
}
