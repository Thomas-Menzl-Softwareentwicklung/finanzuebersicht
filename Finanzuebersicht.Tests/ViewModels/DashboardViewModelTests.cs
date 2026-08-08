using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Dashboard;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Models;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public void HasYearData_WhenJahrGruppenEmpty_ReturnsFalse()
    {
        var viewModel = CreateSut();

        Assert.False(viewModel.HasYearData);
    }

    [Fact]
    public void HasMonthData_WhenCollectionsEmpty_ReturnsFalse()
    {
        var viewModel = CreateSut();

        Assert.False(viewModel.HasMonthData);
    }

    [Fact]
    public async Task LoadDashboard_PopulatesKontenUebersichtForActiveAccounts()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" },
            new() { Id = "acc-2", Name = "Sparkonto", IsArchived = true }
        }));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>
            {
                new() { Typ = TransactionType.Einnahme, Betrag = 1000m, AccountId = "acc-1", KategorieId = "cat-1" },
                new() { Typ = TransactionType.Einnahme, Betrag = 500m, AccountId = "acc-2", KategorieId = "cat-1" }
            }));

        var viewModel = CreateSut(accountRepository, transactionRepository);

        await viewModel.LoadDashboardCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasKontenUebersicht);
        Assert.Equal(1000m, viewModel.GesamtSaldo);
        Assert.Single(viewModel.KontenUebersicht);
        Assert.Equal("acc-1", viewModel.KontenUebersicht[0].AccountId);
    }

    [Fact]
    public async Task SelectKontoFromOverview_SetsAccountFilter()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" },
            new() { Id = "acc-2", Name = "Tagesgeld", OpeningBalance = 2500m }
        }));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var viewModel = CreateSut(accountRepository, transactionRepository);
        await viewModel.LoadDashboardCommand.ExecuteAsync(null);

        var item = viewModel.KontenUebersicht.First(i => i.AccountId == "acc-2");
        await viewModel.SelectKontoFromOverviewCommand.ExecuteAsync(item);

        Assert.Equal("acc-2", viewModel.SelectedAccountId);
        Assert.Equal(2500m, viewModel.SelectedAccountSaldo);
    }

    [Fact]
    public async Task SelectedKontoFilterItem_NotifiesHasSelectedAccountSaldo()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro", OpeningBalance = 100m }
        }));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var viewModel = CreateSut(accountRepository, transactionRepository);
        await viewModel.LoadDashboardCommand.ExecuteAsync(null);

        var notified = false;
        var notifiedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DashboardViewModel.HasSelectedAccountSaldo))
            {
                notified = true;
                notifiedTcs.TrySetResult(true);
            }
        };

        viewModel.SelectedKontoFilterItem = viewModel.AvailableKonten.First(k => k.Id == "acc-1");
        var completed = await Task.WhenAny(notifiedTcs.Task, Task.Delay(1000));
        Assert.Same(notifiedTcs.Task, completed);

        Assert.True(viewModel.HasSelectedAccountSaldo);
        Assert.True(notified);
    }

    [Fact]
    public async Task LoadDashboard_PopulatesCashflowPreview()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>
            {
                new()
                {
                    Typ = TransactionType.Einnahme,
                    Betrag = 500m,
                    Datum = new DateTime(2026, 3, 20),
                    KategorieId = "cat-1",
                    Titel = "Gehalt"
                }
            }));

        var viewModel = CreateSut(accountRepository, transactionRepository);
        await viewModel.LoadDashboardCommand.ExecuteAsync(null);

        Assert.True(viewModel.HasCashflowPreview);
        Assert.Equal(500m, viewModel.CashflowProjectedIncome);
    }

    [Fact]
    public void ToggleBudgetSection_PersistsExpandedState()
    {
        var settings = Substitute.For<ISettingsService>();
        settings.Get(SettingsKeys.DashboardBudgetExpanded, "false").Returns("true");

        var viewModel = CreateSut(settings);
        Assert.True(viewModel.IsBudgetSectionExpanded);

        viewModel.ToggleBudgetSectionCommand.Execute(null);

        Assert.False(viewModel.IsBudgetSectionExpanded);
        settings.Received(1).Set(SettingsKeys.DashboardBudgetExpanded, "false");
    }

    [Fact]
    public async Task LoadDashboard_WhenTransactionHistoryExistsButMonthEmpty_KeepsNavigationVisible()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" }
        }));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(call =>
            {
                var from = call.ArgAt<DateTime>(0);
                if (from.Year == 2026 && from.Month == 8)
                {
                    return Task.FromResult(new List<Transaction>
                    {
                        new()
                        {
                            Typ = TransactionType.Ausgabe,
                            Betrag = 12m,
                            KategorieId = "cat-1",
                            AccountId = "acc-1",
                            Datum = new DateTime(2026, 8, 5)
                        }
                    });
                }

                return Task.FromResult(new List<Transaction>());
            });

        var viewModel = CreateSut(
            accountRepository,
            transactionRepository,
            Substitute.For<ISettingsService>(),
            new FixedClock(new DateTime(2026, 8, 6)));

        await viewModel.LoadDashboardCommand.ExecuteAsync(null);
        Assert.True(viewModel.HasAnyData);
        Assert.True(viewModel.HasMonthData);

        await viewModel.PreviousMonthCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasMonthData);
        Assert.True(viewModel.HasAnyData);
        Assert.True(viewModel.ShowMonthView);
        Assert.Equal(new DateTime(2026, 7, 1), viewModel.AktuellerMonat);
    }

    private static DashboardViewModel CreateSut()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));
        return CreateSut(accountRepository, transactionRepository);
    }

    private static DashboardViewModel CreateSut(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
        => CreateSut(accountRepository, transactionRepository, Substitute.For<ISettingsService>());

    private static DashboardViewModel CreateSut(ISettingsService settingsService)
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));
        return CreateSut(accountRepository, transactionRepository, settingsService);
    }

    private static DashboardViewModel CreateSut(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ISettingsService settingsService,
        Finanzuebersicht.Core.Services.IClock? clock = null)
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>
        {
            new() { Id = "cat-1", Name = "Sonstiges", Color = "#999999", Icon = "📁" }
        }));

        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.GetRecurringTransactionsAsync()
            .Returns(Task.FromResult(new List<RecurringTransaction>()));

        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(Task.FromResult(new List<CategoryBudget>()));

        var localizationService = Substitute.For<ILocalizationService>();
        var navigationService = Substitute.For<INavigationService>();
        var dialogService = Substitute.For<IDialogService>();
        var forecastService = Substitute.For<IForecastService>();
        forecastService.GetMovingAverageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new ForecastResult()));
        clock ??= new FixedClock(new DateTime(2026, 3, 15));

        var accountsCoordinator = new DashboardAccountsCoordinator(
            new GetAccountBalancesUseCase(accountRepository, transactionRepository),
            new LoadActiveAccountsUseCase(accountRepository),
            localizationService);
        var cashflowCoordinator = new DashboardCashflowPreviewCoordinator(
            new LoadCashflowOutlookUseCase(transactionRepository, recurringTransactionRepository, clock),
            localizationService);
        var dueRecurringCoordinator = new DashboardDueRecurringCoordinator(
            new GetDueRecurringWithHintsUseCase(recurringTransactionRepository),
            new BookDueRecurringInstanceUseCase(recurringTransactionRepository, transactionRepository, accountRepository),
            new SkipDueRecurringInstanceUseCase(new AddRecurringExceptionUseCase(recurringTransactionRepository)),
            dialogService,
            navigationService,
            localizationService,
            clock);
        var expandSettings = new DashboardExpandSettingsHelper(settingsService);

        return new DashboardViewModel(
            new LoadDashboardMonthUseCase(categoryRepository, transactionRepository, recurringTransactionRepository, budgetRepository),
            new LoadDashboardYearUseCase(transactionRepository, categoryRepository),
            new LoadForecastUseCase(forecastService),
            cashflowCoordinator,
            dueRecurringCoordinator,
            accountsCoordinator,
            expandSettings,
            new GetDefaultBudgetTotalUseCase(budgetRepository),
            new GetEarliestTransactionYearUseCase(transactionRepository),
            localizationService,
            navigationService,
            settingsService,
            dialogService,
            Substitute.For<Finanzuebersicht.Core.Licensing.ILicenseService>(),
            clock);
    }
}
