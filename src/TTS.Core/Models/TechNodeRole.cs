namespace TTS.Core.Models;

/// <summary>Role of a node within its TTS sub-tree (see tech-trees-by-tier.md).</summary>
public enum TechNodeRole
{
    Core,
    Branch,
    Forbidden,
    Fusion
}
