using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Licensing;
using NSubstitute;

namespace Finanzuebersicht.Tests.Core.Licensing;

public class LicenseEntitlementStoreTests
{
    [Fact]
    public async Task ApplyOwnedProductIdsAsync_WhenEmpty_DoesNotClearCache()
    {
        var settings = Substitute.For<ISettingsService>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        settings.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
        {
            var key = ci.ArgAt<string>(0);
            var fallback = ci.ArgAt<string>(1);
            return values.TryGetValue(key, out var v) ? v : fallback;
        });
        settings.When(s => s.Set(Arg.Any<string>(), Arg.Any<string>()))
            .Do(ci => values[ci.ArgAt<string>(0)] = ci.ArgAt<string>(1));

        var billing = Substitute.For<IStoreBillingService>();
        billing.IsAvailable.Returns(false);

        var sut = new LicenseEntitlementStore(settings, billing, allowEntitlementStubs: false);
        await sut.ApplyOwnedProductIdsAsync([LicenseProductIds.Pro]);
        Assert.Equal("true", values[LicenseEntitlementStore.CacheProKey]);

        await sut.ApplyOwnedProductIdsAsync([]);
        Assert.Equal("true", values[LicenseEntitlementStore.CacheProKey]);
    }
}
