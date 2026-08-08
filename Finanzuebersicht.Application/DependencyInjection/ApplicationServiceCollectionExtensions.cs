using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Application.UseCases.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Finanzuebersicht.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationUseCases(this IServiceCollection services)
    {
        services.AddTransient<DeleteCategoryUseCase>();
        services.AddTransient<LoadCategoriesUseCase>();
        services.AddTransient<SaveCategoryDetailUseCase>();
        services.AddTransient<SaveCategoryBudgetUseCase>();
        services.AddTransient<LoadCategoryBudgetUseCase>();
        services.AddTransient<LoadAccountsUseCase>();
        services.AddTransient<LoadActiveAccountsUseCase>();
        services.AddTransient<SaveAccountDetailUseCase>();
        services.AddTransient<ToggleAccountArchiveUseCase>();
        services.AddTransient<DeleteAccountUseCase>();
        services.AddTransient<GetAccountBalancesUseCase>();
        services.AddTransient<ReconcileAccountBalanceUseCase>();

        services.AddTransient<LoadDashboardMonthUseCase>();
        services.AddTransient<LoadDashboardYearUseCase>();
        services.AddTransient<LoadForecastUseCase>();
        services.AddTransient<LoadCashflowOutlookUseCase>();
        services.AddTransient<GetDefaultBudgetTotalUseCase>();

        services.AddTransient<DeleteRecurringTransactionUseCase>();
        services.AddTransient<LoadRecurringTransactionDetailDataUseCase>();
        services.AddTransient<LoadRecurringTransactionsUseCase>();
        services.AddTransient<ToggleRecurringTransactionActiveUseCase>();
        services.AddTransient<AddRecurringExceptionUseCase>();
        services.AddTransient<RemoveRecurringExceptionUseCase>();
        services.AddTransient<ShiftRecurringInstanceUseCase>();
        services.AddTransient<GetDueRecurringWithHintsUseCase>();
        services.AddTransient<BookDueRecurringInstanceUseCase>();
        services.AddTransient<SkipDueRecurringInstanceUseCase>();

        services.AddTransient<LoadTransactionDetailDataUseCase>();
        services.AddTransient<DeleteTransactionUseCase>();
        services.AddTransient<RestoreTransactionUseCase>();
        services.AddTransient<HasAnyTransactionsUseCase>();
        services.AddTransient<GetEarliestTransactionYearUseCase>();
        services.AddTransient<LoadTransactionsMonthUseCase>();
        services.AddTransient<LoadTransactionTemplatesUseCase>();
        services.AddTransient<SearchTransactionsUseCase>();
        services.AddTransient<SaveTransferUseCase>();
        services.AddTransient<SaveRecurringTransactionDetailUseCase>();
        services.AddTransient<SaveTransactionDetailUseCase>();
        services.AddTransient<CaptureQuickExpenseUseCase>();
        services.AddTransient<CountUncategorizedTransactionsUseCase>();
        services.AddTransient<ProcessQuickExpenseInboxUseCase>();
        services.AddTransient<LoadQuickExpenseWidgetPresetsUseCase>();
        services.AddTransient<SaveQuickExpenseWidgetPresetsUseCase>();
        services.AddTransient<SaveTransactionTemplateUseCase>();
        services.AddTransient<DeleteTransactionTemplateUseCase>();
        services.AddTransient<UseTransactionTemplateUseCase>();

        services.AddTransient<CsvImportOrchestrator>();
        services.AddTransient<AnalyzeCsvImportUseCase>();
        services.AddTransient<CommitCsvImportUseCase>();

        services.AddTransient<CreateBackupUseCase>();
        services.AddTransient<ListBackupsUseCase>();
        services.AddTransient<RestoreBackupUseCase>();
        services.AddTransient<DeleteBackupUseCase>();
        services.AddTransient<ExportCsvUseCase>();

        services.AddTransient<LoadSparZieleUseCase>();
        services.AddTransient<SaveSparZielUseCase>();
        services.AddTransient<DeleteSparZielUseCase>();

        services.AddSingleton<InitializationService>();

        return services;
    }
}
