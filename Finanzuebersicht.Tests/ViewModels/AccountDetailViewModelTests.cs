using System.Globalization;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class AccountDetailViewModelTests
{
    [Fact]
    public void ApplyQueryAttributes_LoadsAccountFields()
    {
        var viewModel = CreateSut(out _);
        var account = new Account
        {
            Id = "acc-1",
            Name = "Giro",
            Type = AccountType.Girokonto,
            OpeningBalance = 1500m,
            OpeningBalanceDate = new DateTime(2026, 1, 1)
        };

        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationQueryKeys.Account] = account });

        Assert.Equal("Giro", viewModel.Name);
        Assert.Equal(AccountType.Girokonto, viewModel.Type);
        Assert.Equal(1500m.ToString("F2", CultureInfo.CurrentCulture), viewModel.OpeningBalanceText);
        Assert.True(viewModel.UseOpeningBalanceDate);
    }

    [Fact]
    public async Task Save_WithEmptyName_DoesNotPersist()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        var viewModel = CreateSut(out _, accountRepository);
        viewModel.Name = "   ";

        await viewModel.SaveCommand.ExecuteAsync(null);

        await accountRepository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task Save_WithValidData_PersistsAndNavigatesBack()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var viewModel = CreateSut(out var navigationService, accountRepository);
        viewModel.Name = "Neues Konto";
        viewModel.OpeningBalanceText = "250";

        await viewModel.SaveCommand.ExecuteAsync(null);

        await accountRepository.Received(1).SaveAccountAsync(NonNullArg.Is<Account>(a => a.Name == "Neues Konto"));
        await navigationService.Received(1).GoBackAsync();
    }

    [Fact]
    public async Task Reconcile_WithInvalidActualBalance_ShowsValidationDialog()
    {
        var viewModel = CreateSut(out _, out var dialogService);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            [NavigationQueryKeys.Account] = new Account { Id = "acc-1", Name = "Giro", OpeningBalance = 1000m }
        });
        viewModel.ActualBalanceText = "abc";

        await viewModel.ReconcileCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    private static AccountDetailViewModel CreateSut(
        out INavigationService navigationService,
        IAccountRepository? accountRepository = null,
        ITransactionRepository? transactionRepository = null)
        => CreateSut(out navigationService, out _, accountRepository, transactionRepository);

    private static AccountDetailViewModel CreateSut(
        out INavigationService navigationService,
        out IDialogService dialogService,
        IAccountRepository? accountRepository = null,
        ITransactionRepository? transactionRepository = null)
    {
        accountRepository ??= Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns([]);
        accountRepository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        transactionRepository ??= Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        navigationService = Substitute.For<INavigationService>();
        navigationService.GoBackAsync().Returns(Task.CompletedTask);

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var feedbackService = Substitute.For<IFeedbackService>();
        feedbackService.ShowSnackbarAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var appEvents = Substitute.For<IAppEvents>();

        return new AccountDetailViewModel(
            new SaveAccountDetailUseCase(accountRepository),
            new GetAccountBalancesUseCase(accountRepository, transactionRepository),
            new ReconcileAccountBalanceUseCase(
                accountRepository,
                new GetAccountBalancesUseCase(accountRepository, transactionRepository),
                new SaveAccountDetailUseCase(accountRepository)),
            navigationService,
            localizationService,
            dialogService,
            feedbackService,
            appEvents);
    }
}
