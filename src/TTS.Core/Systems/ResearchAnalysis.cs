namespace TTS.Core.Systems;

using TTS.Core.Models;

public readonly record struct ResearchCandidateEvaluation(
    string TechnologyId,
    string Name,
    TechCategory Category,
    string Branch,
    IReadOnlyList<string> FusionTags,
    TechTier Tier,
    int RiskLevel,
    bool IsForbidden,
    bool AllowedByRisk,
    double BranchWeightScore,
    double StanceBonus,
    double TotalScore,
    string? RejectionReason = null);

public readonly record struct PolicyResearchAnalysis(
    ResearchStance ResearchStance,
    RiskTolerance RiskTolerance,
    IReadOnlyDictionary<string, double> BranchWeights,
    IReadOnlyList<ResearchCandidateEvaluation> RankedCandidates,
    ResearchCandidateEvaluation? Recommended);

public readonly record struct TechnologyDetailSnapshot(
    string Id,
    string Name,
    TechTier Tier,
    TechCategory Category,
    string Branch,
    IReadOnlyList<string> FusionTags,
    IReadOnlyList<string> Prerequisites,
    int RiskLevel,
    bool IsForbidden);

public readonly record struct TurnResearchDecision(
    string CivilizationId,
    string CivilizationName,
    string Runner,
    bool Researched,
    string? TechnologyId,
    ResearchCandidateEvaluation? Evaluation,
    string Message);
