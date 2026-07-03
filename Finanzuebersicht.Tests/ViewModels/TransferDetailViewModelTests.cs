using System.Globalization;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class TransferDetailViewModelTests
{
    [Fact]
    public async Task LoadAccounts_SelectsDefaultSourceAndDifferentTarget()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default },
            new Account { Id = "acc-2", Name = "Sparkonto" }
        ]);

        var viewModel = CreateSut(accountRepository, out _, out _);

        await viewModel.LoadAccountsCommand.ExecuteAsync(null);

        Assert.Equal("acc-1", viewModel.SourceAccount?.Id);
        Assert.Equal("acc-2", viewModel.TargetAccount?.Id);
    }

    [Fact]
    public async Task Save_WithInvalidAmount_ShowsValidationDialog()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns([]);
        var viewModel = CreateSut(accountRepository, out var dialogService, out _);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);
        viewModel.AmountText = "abc";

        await viewModel.SaveCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Save_WithSameAccounts_ShowsValidationDialog()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        ]);

        var viewModel = CreateSut(accountRepository, out var dialogService, out _);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);
        viewModel.AmountText = "100";
        viewModel.TargetAccount = viewModel.SourceAccount;

        await viewModel.SaveCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Save_WithValidData_PersistsAndNavigatesBack()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.SaveTransactionsAsync(Arg.Any<IEnumerable<Transaction>>()).Returns(Task.CompletedTask);

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default },
            new Account { Id = "acc-2", Name = "Sparkonto" }
        ]);

        var viewModel = CreateSut(accountRepository, out _, out var navigationService, transactionRepository);
        await viewModel.LoadAccountsCommand.ExecuteAsync(null);
        viewModel.AmountText = 150m.ToString("F2", CultureInfo.CurrentCulture);
        viewModel.Title = "Umbuchung";

        await viewModel.SaveCommand.ExecuteAsync(null);

        await transactionRepository.Received(1).SaveTransactionsAsync(Arg.Any<IEnumerable<Transaction>>());
        await navigationService.Received(1).GoBackAsync();
    }

    private static TransferDetailViewModel CreateSut(
        IAccountRepository accountRepository,
        out IDialogService dialogService,
        out INavigationService navigationService,
        ITransactionRepository? transactionRepository = null)
    {
        transactionRepository ??= Substitute.For<ITransactionRepository>();
        transactionRepository.SaveTransactionsAsync(Arg.Any<IEnumerable<Transaction>>()).Returns(Task.CompletedTask);

        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        navigationService = Substitute.For<INavigationService>();
        navigationService.GoBackAsync().Returns(Task.CompletedTask);

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var feedbackService = Substitute.For<IFeedbackService>();
        feedbackService.ShowSnackbarAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var appEvents = Substitute.For<IAppEvents>();

        return new TransferDetailViewModel(
            new SaveTransferUseCase(transactionRepository, accountRepository),
            new LoadActiveAccountsUseCase(accountRepository),
            navigationService,
            dialogService,
            localizationService,
            feedbackService,
            appEvents);
    }
}
