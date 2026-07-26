namespace Finanzuebersicht.Core.Licensing;

public sealed class FixedDistributionChannelProvider(DistributionChannel channel) : IDistributionChannelProvider
{
    public DistributionChannel Channel { get; } = channel;
}
