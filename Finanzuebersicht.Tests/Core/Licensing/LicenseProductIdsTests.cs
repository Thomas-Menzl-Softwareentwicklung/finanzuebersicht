using Finanzuebersicht.Core.Licensing;

namespace Finanzuebersicht.Tests.Core.Licensing;

public class LicenseProductIdsTests
{
    [Fact]
    public void ProductIds_MatchExpectedBundlePrefix()
    {
        Assert.StartsWith("com.thomasmenzl.finanzuebersicht.", LicenseProductIds.Pro);
        Assert.StartsWith("com.thomasmenzl.finanzuebersicht.", LicenseProductIds.SyncYearly);
        Assert.Contains(LicenseProductIds.Pro, LicenseProductIds.All);
        Assert.Contains(LicenseProductIds.SyncYearly, LicenseProductIds.All);
    }
}
