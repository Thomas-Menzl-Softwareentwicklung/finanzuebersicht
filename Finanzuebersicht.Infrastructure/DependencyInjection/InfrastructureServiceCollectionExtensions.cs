using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Licensing;

namespace Finanzuebersicht.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Settings (file-based JSON persistence)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDisplayCurrencyService, DisplayCurrencyService>();

        // Licensing (IDistributionChannelProvider + IStoreBillingService registered by the host)
        // Entitlement stubs only in Debug — Release/Store builds must not honor free Pro toggles.
        services.AddSingleton<ILicenseEntitlementStore>(sp =>
            new LicenseEntitlementStore(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<IStoreBillingService>(),
#if DEBUG
                allowEntitlementStubs: true));
#else
                allowEntitlementStubs: false));
#endif
        services.AddSingleton<ILicenseService, LicenseService>();

        services.AddSingleton<IUncategorizedCategoryService, UncategorizedCategoryService>();

        // CSV import parsers + categorization (Application use cases consume these)
        services.AddSingleton<IStatementParser, DkbCsvParser>();
        services.AddSingleton<ICategorizationStrategy, KeywordCategorizationStrategy>();
        services.AddSingleton<ICategorizationStrategy, HistoricalCategorizationStrategy>();
        services.AddSingleton<CategorizationService>();

        // Backup
        services.AddSingleton<IDataMigrator, Finanzuebersicht.Core.Services.Migrations.V1ToV2Migrator>();
        services.AddSingleton<IDataMigrator, Finanzuebersicht.Core.Services.Migrations.V2ToV3Migrator>();
        services.AddSingleton<DataMigrationService>(sp =>
            new DataMigrationService(sp.GetServices<IDataMigrator>()));
        services.AddSingleton<IBackupService, BackupService>();

        // Resolve once per provider call: applies pending DataPath, then returns active dir.
        // Store factories invoke this at singleton creation (typically app start).
        string GetDataDir(IServiceProvider sp) =>
            DataPathResolver.ResolveDataDir(sp.GetRequiredService<ISettingsService>());

        // Register specialized data stores as singletons with factory pattern
        // Each store receives the resolved dataDir and optional logger
        services.AddSingleton<CategoryStore>(sp =>
            new CategoryStore(
                GetDataDir(sp),
                sp.GetService<ILogger<CategoryStore>>()));

        services.AddSingleton<AccountStore>(sp =>
            new AccountStore(
                GetDataDir(sp),
                sp.GetService<ILogger<AccountStore>>()));

        services.AddSingleton<TransactionStore>(sp =>
            new TransactionStore(
                GetDataDir(sp),
                sp.GetService<ILogger<TransactionStore>>(),
                sp.GetRequiredService<CategoryStore>()));

        services.AddSingleton<RecurringStore>(sp =>
            new RecurringStore(
                GetDataDir(sp),
                sp.GetService<ILogger<RecurringStore>>()));

        services.AddSingleton<BudgetStore>(sp =>
            new BudgetStore(
                GetDataDir(sp),
                sp.GetService<ILogger<BudgetStore>>()));

        services.AddSingleton<SparZielStore>(sp =>
            new SparZielStore(
                GetDataDir(sp),
                sp.GetService<ILogger<SparZielStore>>()));

        services.AddSingleton<TransactionTemplateStore>(sp =>
            new TransactionTemplateStore(
                GetDataDir(sp),
                sp.GetService<ILogger<TransactionTemplateStore>>()));

        // Register composite LocalDataService which coordinates all stores
        // Stores are injected, not manually constructed
        services.AddSingleton<LocalDataService>(sp =>
            new LocalDataService(
                sp.GetRequiredService<CategoryStore>(),
                sp.GetRequiredService<AccountStore>(),
                sp.GetRequiredService<TransactionStore>(),
                sp.GetRequiredService<RecurringStore>(),
                sp.GetRequiredService<BudgetStore>(),
                sp.GetRequiredService<SparZielStore>(),
                sp.GetRequiredService<TransactionTemplateStore>()));

        // Expose the LocalDataService instance via the repository interfaces it implements
        services.AddSingleton<ICategoryRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IAccountRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ITransactionRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IRecurringTransactionRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IBudgetRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ISparZielRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ITransactionTemplateRepository>(sp => sp.GetRequiredService<LocalDataService>());

        // Default inbox (tests / non-iOS). MAUI host may replace with App Group store.
        services.AddSingleton<IQuickExpenseInboxStore>(sp =>
            new FileQuickExpenseInboxStore(
                GetDataDir(sp),
                sp.GetService<ILogger<FileQuickExpenseInboxStore>>()));

        services.AddSingleton<IQuickExpenseWidgetPresetStore>(sp =>
            new FileQuickExpenseWidgetPresetStore(
                GetDataDir(sp),
                sp.GetService<ILogger<FileQuickExpenseWidgetPresetStore>>()));

        return services;
    }
}