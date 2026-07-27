using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Licensing;

namespace Finanzuebersicht.Tests.Core.Licensing;

public class LicenseServiceTests
{
    private static (LicenseService Sut, LicenseEntitlementStore Store) CreateStoreSut()
    {
        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var settings = Substitute.For<ISettingsService>();
        settings.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
            bag.TryGetValue(ci.ArgAt<string>(0), out var value) ? value : ci.ArgAt<string>(1));
        settings.When(s => s.Set(Arg.Any<string>(), Arg.Any<string>()))
            .Do(ci => bag[ci.ArgAt<string>(0)] = ci.ArgAt<string>(1));

        var store = new LicenseEntitlementStore(
            settings,
            new UnavailableStoreBillingService(),
            allowEntitlementStubs: true);
        var sut = new LicenseService(
            new FixedDistributionChannelProvider(DistributionChannel.Store),
            store);
        return (sut, store);
    }

    [Fact]
    public async Task Direct_IsAlwaysPro_AndNeverCloudSync()
    {
        var settings = Substitute.For<ISettingsService>();
        var sut = new LicenseService(
            new FixedDistributionChannelProvider(DistributionChannel.Direct),
            new LicenseEntitlementStore(
                settings,
                new UnavailableStoreBillingService(),
                allowEntitlementStubs: false));

        await sut.RefreshAsync();

        Assert.True(sut.HasPro);
        Assert.False(sut.CanUseCloudSync);
        Assert.True(sut.HasFeature(AppFeature.CsvImport));
        Assert.True(sut.HasFeature(AppFeature.Cashflow));
        Assert.False(sut.HasFeature(AppFeature.CloudSync));
        Assert.True(sut.CheckCreateLimit(LimitedResource.Accounts, 99).Allowed);
    }

    [Fact]
    public async Task StoreFree_BlocksFourthAccount_AndProFeatures()
    {
        var (sut, _) = CreateStoreSut();
        await sut.RefreshAsync();

        Assert.False(sut.HasPro);
        Assert.False(sut.HasFeature(AppFeature.CsvImport));
        Assert.False(sut.HasFeature(AppFeature.Cashflow));

        var ok = sut.CheckCreateLimit(LimitedResource.Accounts, FreeTierLimits.MaxAccounts - 1);
        Assert.True(ok.Allowed);

        var blocked = sut.CheckCreateLimit(LimitedResource.Accounts, FreeTierLimits.MaxAccounts);
        Assert.False(blocked.Allowed);
        Assert.Equal(FreeTierLimits.MaxAccounts, blocked.Limit);

        Assert.Throws<FeatureGateException>(() =>
            sut.EnsureCanCreate(LimitedResource.Accounts, FreeTierLimits.MaxAccounts));
    }

    [Fact]
    public async Task Store_SyncWithoutPro_UnlocksCloudSyncCapability()
    {
        var (sut, store) = CreateStoreSut();
        await store.SetStubEntitlementsAsync(hasPro: false, hasSyncSubscription: true);
        await sut.RefreshAsync();

        Assert.False(sut.HasPro);
        Assert.True(sut.HasSyncSubscription);
        Assert.True(sut.CanUseCloudSync);
        Assert.True(sut.HasFeature(AppFeature.CloudSync));
        Assert.False(sut.HasFeature(AppFeature.CsvImport));
    }

    [Fact]
    public async Task StorePro_RemovesLimits()
    {
        var (sut, store) = CreateStoreSut();
        await store.SetStubEntitlementsAsync(hasPro: true, hasSyncSubscription: false);
        await sut.RefreshAsync();

        Assert.True(sut.HasPro);
        Assert.True(sut.CheckCreateLimit(LimitedResource.Accounts, 50).Allowed);
        Assert.True(sut.HasFeature(AppFeature.Cashflow));
        Assert.False(sut.CanUseCloudSync);
    }

    [Fact]
    public async Task ApplyOwnedProductIds_MapsProProduct()
    {
        var (sut, store) = CreateStoreSut();
        await store.ApplyOwnedProductIdsAsync([LicenseProductIds.Pro]);
        await sut.RefreshAsync();

        Assert.True(sut.HasPro);
        Assert.False(sut.HasSyncSubscription);
    }

    [Fact]
    public async Task ReleaseStore_IgnoresStubEntitlements()
    {
        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var settings = Substitute.For<ISettingsService>();
        settings.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(ci =>
            bag.TryGetValue(ci.ArgAt<string>(0), out var value) ? value : ci.ArgAt<string>(1));
        settings.When(s => s.Set(Arg.Any<string>(), Arg.Any<string>()))
            .Do(ci => bag[ci.ArgAt<string>(0)] = ci.ArgAt<string>(1));

        // Leftover Debug stub state must not unlock Pro in Release.
        bag[LicenseEntitlementStore.PreferStubKey] = "true";
        bag[LicenseEntitlementStore.ProKey] = "true";
        bag[LicenseEntitlementStore.SyncKey] = "true";

        var store = new LicenseEntitlementStore(
            settings,
            new UnavailableStoreBillingService(),
            allowEntitlementStubs: false);
        var sut = new LicenseService(
            new FixedDistributionChannelProvider(DistributionChannel.Store),
            store);

        await store.SetStubEntitlementsAsync(hasPro: true, hasSyncSubscription: true);
        await sut.RefreshAsync();

        Assert.False(sut.HasPro);
        Assert.False(sut.HasSyncSubscription);
        Assert.Equal("false", bag[LicenseEntitlementStore.PreferStubKey]);
    }
}
