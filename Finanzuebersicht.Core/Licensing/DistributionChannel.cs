namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// How the binary is distributed. Direct = GitHub/self-built (Windows, sideload Mac).
/// Store = App Store / Mac App Store.
/// </summary>
public enum DistributionChannel
{
    /// <summary>Self-built / GitHub release: full local Pro, no Cloud Sync.</summary>
    Direct = 0,

    /// <summary>Apple App Store / Mac App Store: Free/Pro/Sync apply.</summary>
    Store = 1
}
