namespace Finanzuebersicht.Core.Licensing;

/// <summary>Thrown when a Store Free-tier gate blocks an action.</summary>
public sealed class FeatureGateException : InvalidOperationException
{
    public FeatureGateException(AppFeature feature, string message)
        : base(message)
    {
        Feature = feature;
        LimitedResource = null;
    }

    public FeatureGateException(LimitedResource resource, int currentCount, int limit, string message)
        : base(message)
    {
        Feature = null;
        LimitedResource = resource;
        CurrentCount = currentCount;
        Limit = limit;
    }

    public AppFeature? Feature { get; }
    public LimitedResource? LimitedResource { get; }
    public int? CurrentCount { get; }
    public int? Limit { get; }
}
