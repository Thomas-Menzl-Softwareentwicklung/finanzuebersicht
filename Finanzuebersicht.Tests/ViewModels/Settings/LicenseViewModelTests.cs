using Finanzuebersicht.Application.UseCases.Backup;
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
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, true, true)]
    public async Task ShowCloudSyncControls_VisibleWhenImplementedAndEntitledOrAlreadyEnabled(
        bool canUseCloudSync,
        bool isImplemented,
        bool syncEnabled,
        bool expected)
    {
        var license = CreateLicenseService(canUseCloudSync, isImplemented);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = syncEnabled });
        var sut = CreateSut(license, metadataStore: metadataStore);

        await sut.InitializeAsync();

        Assert.Equal(expected, sut.ShowCloudSyncControls);
    }

    [Fact]
    public async Task CanToggleCloudSync_IsFalseWhenEntitlementExpired()
    {
        var license = CreateLicenseService(canUseCloudSync: false, isImplemented: true);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = true });
        var sut = CreateSut(license, metadataStore: metadataStore);

        await sut.InitializeAsync();

        Assert.True(sut.ShowCloudSyncControls);
        Assert.False(sut.CanToggleCloudSync);
        Assert.Equal(ResourceKeys.Sync_PausedRenew, sut.CloudSyncStatusLine);
    }

    [Fact]
    public async Task ShowSyncPurchaseLaterHint_HiddenWhenEngineIsImplemented()
    {
        var license = CreateLicenseService(canUseCloudSync: false, isImplemented: true);
        var billing = Substitute.For<IStoreBillingService>();
        billing.IsAvailable.Returns(true);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata());
        var sut = CreateSut(license, metadataStore: metadataStore, billing: billing);

        await sut.InitializeAsync();

        Assert.False(sut.ShowSyncPurchaseLaterHint);
        Assert.True(sut.CanBuySync);
        Assert.Equal(ResourceKeys.Lic_SyncAvailable, sut.SyncLabel);
    }

    [Fact]
    public void ShowLegalLinks_TrueForStore_FalseForDirect()
    {
        var store = CreateSut(CreateLicenseService(canUseCloudSync: false, isImplemented: true));
        Assert.True(store.ShowLegalLinks);

        var directLicense = CreateLicenseService(canUseCloudSync: false, isImplemented: false);
        directLicense.Channel.Returns(DistributionChannel.Direct);
        var direct = CreateSut(directLicense);
        Assert.False(direct.ShowLegalLinks);
    }

    [Fact]
    public async Task OpenTermsOfUse_OpensAppleStandardEula()
    {
        var browser = Substitute.For<IExternalBrowser>();
        var sut = CreateSut(CreateLicenseService(canUseCloudSync: false, isImplemented: true), browser: browser);

        await sut.OpenTermsOfUseCommand.ExecuteAsync(null);

        await browser.Received(1).OpenAsync(new Uri(StoreLegalUrls.AppleStandardEula));
    }

    [Theory]
    [InlineData("de", StoreLegalUrls.PrivacyPolicyDe)]
    [InlineData("en", StoreLegalUrls.PrivacyPolicyEn)]
    [InlineData("en-US", StoreLegalUrls.PrivacyPolicyEn)]
    public async Task OpenPrivacyPolicy_UsesLocalizedSite(string languageCode, string expectedUrl)
    {
        var browser = Substitute.For<IExternalBrowser>();
        var localization = CreateLocalizationService();
        localization.CurrentLanguageCode.Returns(languageCode);
        var sut = CreateSut(
            CreateLicenseService(canUseCloudSync: false, isImplemented: true),
            localization: localization,
            browser: browser);

        await sut.OpenPrivacyPolicyCommand.ExecuteAsync(null);

        await browser.Received(1).OpenAsync(new Uri(expectedUrl));
    }

    [Fact]
    public async Task BuySync_PurchasesYearlySyncProduct()
    {
        var license = CreateLicenseService(canUseCloudSync: false, isImplemented: true);
        var billing = Substitute.For<IStoreBillingService>();
        billing.IsAvailable.Returns(true);
        billing.InitializeAsync(Arg.Any<CancellationToken>()).Returns(true);
        billing.PurchaseAsync(LicenseProductIds.SyncYearly, Arg.Any<CancellationToken>())
            .Returns(StorePurchaseResult.Success(LicenseProductIds.SyncYearly));
        billing.GetOwnedProductIdsAsync(Arg.Any<CancellationToken>())
            .Returns([LicenseProductIds.SyncYearly]);
        var entitlements = Substitute.For<ILicenseEntitlementStore>();
        var feedback = Substitute.For<IFeedbackService>();
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata());
        var sut = CreateSut(
            license,
            metadataStore: metadataStore,
            billing: billing,
            entitlementStore: entitlements,
            feedback: feedback);

        await sut.BuySyncCommand.ExecuteAsync(null);

        await billing.Received(1).PurchaseAsync(LicenseProductIds.SyncYearly, Arg.Any<CancellationToken>());
        await entitlements.Received(1).ApplyOwnedProductIdsAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Contains(LicenseProductIds.SyncYearly)));
        await feedback.Received(1).ShowSnackbarAsync(ResourceKeys.Lic_SyncPurchaseSuccess);
    }

    [Fact]
    public async Task EnableCloudSync_WhenUseCaseThrows_RevertsSwitchShowsAlertAndPersistsLastError()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadata = new SyncMetadata { SyncEnabled = false };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);

        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.Available);
        transport.IsZoneEmptyAsync(Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new InvalidOperationException("CloudKit unavailable"));

        var enableUseCase = CreateEnableUseCaseWithTransport(transport, metadataStore, license);
        var dialogService = CreateDialogService();
        var localization = CreateLocalizationService();
        var sut = CreateSut(license, enableUseCase, metadataStore, dialogService, localization);

        var ex = await Record.ExceptionAsync(() => sut.CloudSyncToggledCommand.ExecuteAsync(true));

        Assert.Null(ex);
        Assert.False(sut.CloudSyncEnabled);
        Assert.Equal("CloudKit unavailable", metadata.LastError);
        await metadataStore.Received().SaveAsync(Arg.Is<SyncMetadata>(m => m.LastError == "CloudKit unavailable"));
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Err_Titel,
            ResourceKeys.Sync_Error,
            ResourceKeys.Btn_OK);
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
        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var sut = CreateSut(license, enableUseCase, metadataStore, dialogService, orchestrator: orchestrator);

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        Assert.True(sut.CloudSyncEnabled);
        await dialogService.DidNotReceive().ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await orchestrator.Received(1).StartIfEnabledAsync(Arg.Any<CancellationToken>());
        await orchestrator.Received(1).SyncNowAsync(Arg.Any<CancellationToken>());
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
        await dialogService.Received(1).ShowConfirmationAsync(
            ResourceKeys.Sync_ReplaceLocalWithCloudTitle,
            ResourceKeys.Sync_ReplaceLocalWithCloudMessage,
            ResourceKeys.Sync_ReplaceLocalWithCloudAccept,
            ResourceKeys.Btn_Abbrechen);
        await dialogService.DidNotReceive().ShowAlertAsync(
            ResourceKeys.Err_Titel,
            ResourceKeys.Sync_BlockedBothHaveData,
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task EnableCloudSync_WhenBlockedBothHaveDataAndUserCancels_DoesNotClearOrBackup()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = false });
        var enableUseCase = CreateEnableUseCaseForBothHaveData(license, metadataStore);
        var backupService = Substitute.For<IBackupService>();
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var dialogService = CreateDialogService();
        dialogService.ShowConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var sut = CreateSut(
            license,
            enableUseCase,
            metadataStore,
            dialogService,
            backupUseCase: new CreateBackupUseCase(backupService),
            clearUseCase: CreateClearUseCase(tombstoneStore, metadataStore));

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        await backupService.DidNotReceive().CreateBackupAsync(Arg.Any<string?>());
        await tombstoneStore.DidNotReceive().ClearAsync();
        Assert.False(sut.CloudSyncEnabled);
    }

    [Fact]
    public async Task EnableCloudSync_WhenBlockedBothHaveDataAndBackupFails_DoesNotClear()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(new SyncMetadata { SyncEnabled = false });
        var enableUseCase = CreateEnableUseCaseForBothHaveData(license, metadataStore);
        var backupService = Substitute.For<IBackupService>();
        backupService.CreateBackupAsync(Arg.Any<string?>())
            .Returns<BackupMetadata>(_ => throw new InvalidOperationException("disk full"));
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var dialogService = CreateDialogService();
        dialogService.ShowConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        var sut = CreateSut(
            license,
            enableUseCase,
            metadataStore,
            dialogService,
            backupUseCase: new CreateBackupUseCase(backupService),
            clearUseCase: CreateClearUseCase(tombstoneStore, metadataStore));

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        await tombstoneStore.DidNotReceive().ClearAsync();
        Assert.False(sut.CloudSyncEnabled);
        await dialogService.Received().ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task EnableCloudSync_WhenBlockedBothHaveDataAndUserAccepts_BacksUpClearsAndEnables()
    {
        var license = CreateLicenseService(canUseCloudSync: true, isImplemented: true);
        var metadata = new SyncMetadata { SyncEnabled = false };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);

        var accounts = new List<Account> { new() { Id = "acc-1", Name = "Giro" } };
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(_ => accounts.ToList());
        accountRepository
            .When(r => r.ReplaceAllAccountsAsync(Arg.Any<IEnumerable<Account>>()))
            .Do(call =>
            {
                accounts.Clear();
                accounts.AddRange(call.Arg<IEnumerable<Account>>());
            });

        var transport = Substitute.For<ICloudSyncTransport>();
        transport.IsSupported.Returns(true);
        transport.GetAccountStatusAsync(Arg.Any<CancellationToken>())
            .Returns(CloudSyncAccountStatus.Available);
        transport.IsZoneEmptyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns([]);
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>()).Returns([]);
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns([]);
        var sparZielRepository = Substitute.For<ISparZielRepository>();
        sparZielRepository.GetSparZieleAsync().Returns([]);
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns([]);
        var templateRepository = Substitute.For<ITransactionTemplateRepository>();
        templateRepository.GetTransactionTemplatesAsync().Returns([]);
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();

        var enableUseCase = new EnableCloudSyncUseCase(
            transport,
            metadataStore,
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            license);
        var clearUseCase = new ClearLocalSyncedDataUseCase(
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            budgetRepository,
            templateRepository,
            tombstoneStore,
            metadataStore);

        var backupService = Substitute.For<IBackupService>();
        backupService.CreateBackupAsync(Arg.Any<string?>()).Returns(new BackupMetadata { Id = "b1" });
        var dialogService = CreateDialogService();
        dialogService.ShowConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var appEvents = Substitute.For<IAppEvents>();

        var sut = CreateSut(
            license,
            enableUseCase,
            metadataStore,
            dialogService,
            orchestrator: orchestrator,
            backupUseCase: new CreateBackupUseCase(backupService),
            clearUseCase: clearUseCase,
            appEvents: appEvents);

        await sut.CloudSyncToggledCommand.ExecuteAsync(true);

        await backupService.Received(1).CreateBackupAsync(Arg.Any<string?>());
        await tombstoneStore.Received(1).ClearAsync();
        Assert.True(metadata.SyncEnabled);
        Assert.True(sut.CloudSyncEnabled);
        await orchestrator.Received(1).StartIfEnabledAsync(Arg.Any<CancellationToken>());
        await orchestrator.Received(1).SyncNowAsync(Arg.Any<CancellationToken>());
        appEvents.Received().NotifyDataChanged();
        await transport.Received().ResetEngineStateAsync(Arg.Any<CancellationToken>());
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
        ICloudSyncOrchestrator? orchestrator = null,
        IStoreBillingService? billing = null,
        ILicenseEntitlementStore? entitlementStore = null,
        IFeedbackService? feedback = null,
        CreateBackupUseCase? backupUseCase = null,
        ClearLocalSyncedDataUseCase? clearUseCase = null,
        IAppEvents? appEvents = null,
        IExternalBrowser? browser = null)
    {
        var metadata = metadataStore ?? Substitute.For<ISyncMetadataStore>();
        return new LicenseViewModel(
            license,
            entitlementStore ?? Substitute.For<ILicenseEntitlementStore>(),
            billing ?? Substitute.For<IStoreBillingService>(),
            localization ?? CreateLocalizationService(),
            dialogService ?? CreateDialogService(),
            feedback ?? Substitute.For<IFeedbackService>(),
            enableUseCase ?? CreateEnableUseCaseForSuccess(
                Substitute.For<ISyncMetadataStore>(),
                license),
            clearUseCase ?? CreateClearUseCase(Substitute.For<ISyncTombstoneStore>(), metadata),
            backupUseCase ?? new CreateBackupUseCase(Substitute.For<IBackupService>()),
            metadata,
            orchestrator ?? Substitute.For<ICloudSyncOrchestrator>(),
            appEvents ?? Substitute.For<IAppEvents>(),
            browser ?? Substitute.For<IExternalBrowser>());
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
        dialogService.ShowConfirmationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);
        return dialogService;
    }

    private static ClearLocalSyncedDataUseCase CreateClearUseCase(
        ISyncTombstoneStore tombstoneStore,
        ISyncMetadataStore metadataStore)
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetAccountsAsync().Returns([]);
        var categories = Substitute.For<ICategoryRepository>();
        categories.GetCategoriesAsync().Returns([]);
        var transactions = Substitute.For<ITransactionRepository>();
        var recurring = Substitute.For<IRecurringTransactionRepository>();
        var sparZiele = Substitute.For<ISparZielRepository>();
        var budgets = Substitute.For<IBudgetRepository>();
        budgets.GetBudgetsAsync().Returns([]);
        var templates = Substitute.For<ITransactionTemplateRepository>();
        templates.GetTransactionTemplatesAsync().Returns([]);

        return new ClearLocalSyncedDataUseCase(
            accounts,
            categories,
            transactions,
            recurring,
            sparZiele,
            budgets,
            templates,
            tombstoneStore,
            metadataStore);
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
