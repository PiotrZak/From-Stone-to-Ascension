namespace TTS.Core.Models;

/// <summary>
/// Technology Tier levels. Each tier represents a distinct gameplay era
/// that changes not just tools but the rules of reality, society, and strategy.
/// </summary>
public enum TechTier
{
    /// <summary>TTS 1 – Pre-Industrial: survival, agriculture, early expansion.</summary>
    PreIndustrial = 1,

    /// <summary>TTS 2 – Industrial Age: factories, logistics, pollution.</summary>
    Industrial = 2,

    /// <summary>TTS 3 – Early Electronics: electricity grids, radio, aviation.</summary>
    EarlyElectronics = 3,

    /// <summary>TTS 4 – Information Age: internet, satellites, global markets.</summary>
    InformationAge = 4,

    /// <summary>TTS 5 – Early AI Age: autonomous systems, quantum acceleration.</summary>
    EarlyAI = 5,

    /// <summary>TTS 6 – Bio/Nano Age: genetic engineering, nanotech, human enhancement.</summary>
    BioNano = 6,

    /// <summary>TTS 7 – Temporal Age: experimental time manipulation, timeline branching.</summary>
    Temporal = 7,

    /// <summary>TTS 8+ – Post-Singularity: superintelligent systems, reality-level control.</summary>
    PostSingularity = 8
}
