namespace Finanzuebersicht.Core.Licensing;

/// <summary>Provides the compile-/build-time distribution channel.</summary>
public interface IDistributionChannelProvider
{
    DistributionChannel Channel { get; }
}
