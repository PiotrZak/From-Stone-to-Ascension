namespace TTS.Api.Services;

using TTS.Api.Models;
using TTS.Contracts;

public static class CivDashboardMapping
{
    public static CivDashboardDto ToDto(GrainCivDashboard d) => new()
    {
        CivilizationId = d.CivilizationId,
        PresetId = d.PresetId,
        ResearchStance = d.ResearchStance,
        RiskTolerance = d.RiskTolerance,
        BranchWeights = d.BranchWeights,
        RecommendedTech = d.RecommendedTech is null ? null : new RecommendedTechDto
        {
            Id = d.RecommendedTech.Id,
            Name = d.RecommendedTech.Name,
            Tier = d.RecommendedTech.Tier,
            Branch = d.RecommendedTech.Branch,
            Score = d.RecommendedTech.Score
        },
        ResearchedTech = d.ResearchedTech.Select(t => new TechEntryDto
        {
            Id = t.Id,
            Name = t.Name,
            Tier = t.Tier,
            Branch = t.Branch
        }).ToList(),
        AvailableTech = d.AvailableTech.Select(t => new TechEntryDto
        {
            Id = t.Id,
            Name = t.Name,
            Tier = t.Tier,
            Branch = t.Branch
        }).ToList(),
        Crime = d.Crime is null ? null : new CrimePerspectiveDto
        {
            AverageCrimePressure = d.Crime.AverageCrimePressure,
            AverageViolentCrimeRate = d.Crime.AverageViolentCrimeRate,
            AveragePovertyRate = d.Crime.AveragePovertyRate,
            CybersecurityMitigationActive = d.Crime.CybersecurityMitigationActive,
            Regions = d.Crime.Regions.Select(r => new RegionCrimeDto
            {
                RegionName = r.RegionName,
                SourceState = r.SourceState,
                CrimePressure = r.CrimePressure
            }).ToList()
        },
        TechTree = d.TechTree.Select(n => new TechTreeNodeDto
        {
            Id = n.Id,
            Name = n.Name,
            Tier = n.Tier,
            Branch = n.Branch,
            Role = n.Role,
            Prerequisites = n.Prerequisites,
            RiskLevel = n.RiskLevel,
            IsForbidden = n.IsForbidden,
            Status = n.Status
        }).ToList(),
        ResearchSlotsPerTurn = d.ResearchSlotsPerTurn
    };
}
