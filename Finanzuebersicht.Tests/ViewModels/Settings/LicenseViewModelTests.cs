using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class LicenseViewModelTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void ShowCloudSyncControls_RequiresBothEntitlementAndImplementation(
        bool canUseCloudSync,
        bool isImplemented,
        bool expected)
    {
        var license = CreateLicenseService(canUseCloudSync, isImplemented);
        var sut = CreateSut(license);

        Assert.Equal(expected, sut.ShowCloudSyncControls);
    }

    [Fact]
    public async Task EnableCloudSync_WhenSuccessful_KeepsSwitchOnAndShowsNoAlert()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadata = new SyncMetadata();
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);
        var enableUseCase = CreateEnableUseCaseForSuccess(metadataStore, license);

        var dialogService = CreateDialogService();
        var sut = CreateSut(license, enableUseCase, metadataStore, dialogService);

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        Assert.True(sut.CloudSyncEnabled);
        await dialogService.DidNotReceive().ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task EnableCloudSync_WhenBlockedBothHaveData_RevertsSwitchAndShowsAlert()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = false });
        var enableUseCase = CreateEnableUseCaseForBothHaveData(license, metadataStore);

        var dialogService = CreateDialogService();
        var localization = CreateLocalizationService();
        var sut = CreateSut(license, enableUseCase, metadataStore, dialogService, localization);

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        Assert.False(sut.CloudSyncEnabled);
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Err_Titel,
            ResourceKeys.Sync_BlockedBothHaveData,
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task CloudSyncToggled_WhenBusy_RestoresSwitchAndDoesNotEnable()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadata = new SyncMetadata { SyncEnabled = false };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);

        var transport = Substitute.For<ICloudSyncTransport>();
        var enableUseCase = CreateEnableUseCaseWithTransport(transport, metadataStore, license);
        var sut = CreateSut(license, enableUseCase, metadataStore);

        await sut.InitializeAsync();

        sut.IsBusy = true;
        sut.CloudSyncEnabled = true;

        Assert.False(sut.CloudSyncEnabled);
        await transport.DidNotReceive().GetAccountStatusAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableCloudSync_SetsMetadataFalseAndStopsOrchestrator()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadata = new SyncMetadata { SyncEnabled = true };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);

        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var sut = CreateSut(license, metadataStore: metadataStore, orchestrator: orchestrator);

        await sut.CloudSyncToggledCommand.ExecuteAsync(false);

        Assert.False(metadata.SyncEnabled);
        await metadataStore.Received(1).SaveAsync(metadata);
        await orchestrator.Received(1).StopAsync(Arg.Any<CancellationToken>());
    }

    private static LicenseViewModel CreateSut(
        ILicenseService license,
        EnableCloudSyncUseCase? enableUseCase = null,
        ISyncMetadataStore? metadataStore = null,
        IDialogService? dialogService = null,
        ILocalizationService? localization = null,
        ICloudSyncOrchestrator? orchestrator = null)
    {
        return new LicenseViewModel(
            license,
            Substitute.For<ILicenseEntitlementStore>(),
            Substitute.For<IStoreBillingService>(),
            localization ?? CreateLocalizationService(),
            dialogService ?? CreateDialogService(),
            Substitute.For<IFeedbackService>(),
            enableUseCase ?? CreateEnableUseCaseForSuccess(
                Substitute.For<ISyncMetadataStore>(),
                license),
            metadataStore ?? Substitute.For<ISyncMetadataStore>(),
            orchestrator ?? Substitute.For<ICloudSyncOrchestrator>());
    }

    private static ILicenseService CreateLicenseService(bool canUseCloudSync, bool isImplemented)
    {
        var license = Substitute.For<ILicenseService>();
        license.Channel.Returns(DistributionChannel.Store);
        license.CanUseCloudSync.Returns(canUseCloudSync);
        license.IsCloudSyncImplemented.Returns(isImplemented);
        license.HasPro.Returns(false);
        license.HasSyncSubscription.Returns(canUseCloudSync);
        return license;
    }

    private static IDialogService CreateDialogService()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        return dialogService;
    }

    private static ILocalizationService CreateLocalizationService()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));
        return localizationService;
    }

    private static EnableCloudSyncUseCase CreateEnableUseCaseWithTransport(
        ICloudSyncTransport transport,
        ISyncMetadataStore metadataStore,
        ILicenseService license)
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns([new Account { Id = "sys-1", Name = "Cash", SystemKey = "cash" }]);

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync()
            .Returns([new Category { Id = "cat-sys", Name = "Food", SystemKey = "food" }]);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns([]);

        var sparZielRepository = Substitute.For<ISparZielRepository>();
        sparZielRepository.GetSparZieleAsync().Returns([]);

        return new EnableCloudSyncUseCase(
            transport,
            metadataStore,
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            license);
    }

    private static EnableCloudSyncUseCase CreateEnableUseCaseForSuccess(
        ISyncMetadataStore metadataStore,
        ILicenseService license)
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.Available);
        transport.IsZoneEmptyAsync(Arg.Any<CancellationToken>()).Returns(false);

        return CreateEnableUseCaseWithTransport(transport, metadataStore, license);
    }

    private static EnableCloudSyncUseCase CreateEnableUseCaseForBothHaveData(
        ILicenseService license,
        ISyncMetadataStore metadataStore)
    {
        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.Available);
        transport.IsZoneEmptyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns([new Account { Id = "acc-1", Name = "Giro" }]);

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns([]);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns([]);

        var sparZielRepository = Substitute.For<ISparZielRepository>();
        sparZielRepository.GetSparZieleAsync().Returns([]);

        return new EnableCloudSyncUseCase(
            transport,
            metadataStore,
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            license);
    }
}
