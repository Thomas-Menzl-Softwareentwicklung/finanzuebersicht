using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Settings (file-based JSON persistence)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDisplayCurrencyService, DisplayCurrencyService>();

        // Backup
        services.AddSingleton<IDataMigrator, Finanzuebersicht.Core.Services.Migrations.V1ToV2Migrator>();
        services.AddSingleton<IDataMigrator, Finanzuebersicht.Core.Services.Migrations.V2ToV3Migrator>();
        services.AddSingleton<DataMigrationService>(sp =>
            new DataMigrationService(sp.GetServices<IDataMigrator>()));
        services.AddSingleton<IBackupService, BackupService>();

        // Helper to resolve data directory
        string GetDataDir(IServiceProvider sp)
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            var customPath = settings.Get(SettingsKeys.DataPath, "");
            return string.IsNullOrWhiteSpace(customPath) ? AppPaths.GetDefaultDataDir() : customPath;
        }

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
                sp.GetRequiredService<TransactionTemplateStore>(),
                sp.GetRequiredService<Finanzuebersicht.Core.Services.IClock>()));

        // Expose the LocalDataService instance via the repository interfaces it implements
        services.AddSingleton<ICategoryRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IAccountRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ITransactionRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IRecurringTransactionRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<IBudgetRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ISparZielRepository>(sp => sp.GetRequiredService<LocalDataService>());
        services.AddSingleton<ITransactionTemplateRepository>(sp => sp.GetRequiredService<LocalDataService>());

        return services;
    }
}