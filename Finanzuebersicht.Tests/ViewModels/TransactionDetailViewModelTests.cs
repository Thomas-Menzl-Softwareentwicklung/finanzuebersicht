using System.Globalization;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class TransactionDetailViewModelTests
{
    [Fact]
    public void ApplyQueryAttributes_LoadsExistingTransactionFields()
    {
        var viewModel = CreateSut(out _);
        var transaction = new Transaction
        {
            Id = "tx-1",
            Titel = "Supermarkt",
            Betrag = 42.50m,
            KategorieId = "cat-1",
            AccountId = "acc-1",
            Typ = TransactionType.Ausgabe,
            Datum = new DateTime(2026, 3, 10),
            Verwendungszweck = "Wocheneinkauf"
        };

        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { ["Transaction"] = transaction });

        Assert.Equal("Supermarkt", viewModel.Titel);
        Assert.Equal(42.50m.ToString("F2", CultureInfo.CurrentCulture), viewModel.BetragText);
        Assert.Equal(TransactionType.Ausgabe, viewModel.Typ);
        Assert.Equal("Wocheneinkauf", viewModel.Verwendungszweck);
    }

    [Fact]
    public async Task Save_WithMissingTitle_ShowsValidationDialog()
    {
        var viewModel = CreateSut(out var dialogService);
        viewModel.BetragText = "10";
        viewModel.Titel = "   ";
        viewModel.SelectedKategorie = new Category { Id = "cat-1", Name = "Essen" };

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
        transactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);

        var viewModel = CreateSut(out _, out var navigationService, transactionRepository);

        viewModel.BetragText = "25,50";
        viewModel.Titel = "Test";
        viewModel.SelectedKategorie = new Category { Id = "cat-1", Name = "Essen" };
        viewModel.SelectedAccount = new Account { Id = "acc-1", Name = "Giro" };

        await viewModel.SaveCommand.ExecuteAsync(null);

        await transactionRepository.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Titel == "Test"));
        await navigationService.Received(1).GoBackAsync();
    }

    private static TransactionDetailViewModel CreateSut(
        out IDialogService dialogService,
        ITransactionRepository? transactionRepository = null)
        => CreateSut(out dialogService, out _, transactionRepository);

    private static TransactionDetailViewModel CreateSut(
        out IDialogService dialogService,
        out INavigationService navigationService,
        ITransactionRepository? transactionRepository = null)
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(
        [
            new Category { Id = "cat-1", Name = "Essen", SystemKey = Finanzuebersicht.Constants.SystemCategoryKeys.Sonstiges }
        ]);

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        ]);

        transactionRepository ??= Substitute.For<ITransactionRepository>();
        transactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);

        var sparZielRepository = Substitute.For<ISparZielRepository>();
        sparZielRepository.GetSparZieleAsync().Returns([]);

        var saveUseCase = new SaveTransactionDetailUseCase(transactionRepository, accountRepository);

        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var navigationServiceSubstitute = Substitute.For<INavigationService>();
        navigationServiceSubstitute.GoBackAsync().Returns(Task.CompletedTask);
        navigationService = navigationServiceSubstitute;

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        var feedbackService = Substitute.For<IFeedbackService>();
        feedbackService.ShowSnackbarAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var appEvents = Substitute.For<IAppEvents>();

        return new TransactionDetailViewModel(
            saveUseCase,
            new LoadTransactionDetailDataUseCase(categoryRepository, accountRepository),
            new LoadSparZieleUseCase(sparZielRepository, transactionRepository),
            new TransactionValidationService(),
            localizationService,
            navigationServiceSubstitute,
            dialogService,
            feedbackService,
            appEvents);
    }
}
