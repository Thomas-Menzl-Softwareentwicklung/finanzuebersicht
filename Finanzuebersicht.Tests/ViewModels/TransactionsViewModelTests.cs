using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.ViewModels;

public class TransactionsViewModelTests
{
    [Fact]
    public async Task ClearSearch_ResetsAllFiltersAndReloads()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var searchTransactionRepository = Substitute.For<ITransactionRepository>();
        searchTransactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync()
            .Returns(Task.FromResult(new List<Category>()));

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns(Task.FromResult(new List<Account>()));

        var searchCategoryRepository = Substitute.For<ICategoryRepository>();
        searchCategoryRepository.GetCategoriesAsync()
            .Returns(Task.FromResult(new List<Category>()));

        var searchAccountRepository = Substitute.For<IAccountRepository>();
        searchAccountRepository.GetAccountsAsync()
            .Returns(Task.FromResult(new List<Account>()));

        var viewModel = CreateSut(
            transactionRepository,
            categoryRepository,
            searchTransactionRepository,
            searchCategoryRepository,
            accountRepository,
            searchAccountRepository,
            out _,
            out _,
            out _,
            out _);

        viewModel.SearchText = "Suche";
        viewModel.SelectedKategorieId = "cat-1";
        viewModel.SelectedTypFilter = TransactionTypeFilter.Ausgabe;
        viewModel.IsDateFilterEnabled = true;
        viewModel.VonDatum = new DateTime(2026, 1, 1);
        viewModel.BisDatum = new DateTime(2026, 1, 31);
        viewModel.SearchErgebnisGruppen = new ObservableCollection<TransactionGroup>
        {
            new(new DateTime(2026, 1, 1), new[] { new Transaction { Id = "t1", Titel = "Test" } })
        };

        await viewModel.ClearSearchCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.Null(viewModel.SelectedKategorieId);
        Assert.Equal(TransactionTypeFilter.Alle, viewModel.SelectedTypFilter);
        Assert.False(viewModel.IsDateFilterEnabled);
        Assert.Null(viewModel.VonDatum);
        Assert.Null(viewModel.BisDatum);
        Assert.Empty(viewModel.SearchErgebnisGruppen);
        await transactionRepository.Received(1).GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task LoadTransaktionen_WhenAlreadyLoading_DoesNotExecuteAgain()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<List<Transaction>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(_ =>
            {
                started.TrySetResult(true);
                return finish.Task;
            });

        var searchTransactionRepository = Substitute.For<ITransactionRepository>();
        searchTransactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync()
            .Returns(Task.FromResult(new List<Category>()));

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync()
            .Returns(Task.FromResult(new List<Account>()));

        var searchCategoryRepository = Substitute.For<ICategoryRepository>();
        searchCategoryRepository.GetCategoriesAsync()
            .Returns(Task.FromResult(new List<Category>()));

        var searchAccountRepository = Substitute.For<IAccountRepository>();
        searchAccountRepository.GetAccountsAsync()
            .Returns(Task.FromResult(new List<Account>()));

        var viewModel = CreateSut(
            transactionRepository,
            categoryRepository,
            searchTransactionRepository,
            searchCategoryRepository,
            accountRepository,
            searchAccountRepository,
            out _,
            out _,
            out _,
            out _);

        var firstLoad = viewModel.LoadTransaktionenCommand.ExecuteAsync(null);
        await started.Task;

        var secondLoad = viewModel.LoadTransaktionenCommand.ExecuteAsync(null);

        finish.SetResult(new List<Transaction>());

        await Task.WhenAll(firstLoad, secondLoad);

        await transactionRepository.Received(1).GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task DeleteTransaktion_WhenConfirmed_CallsDeleteUseCase()
    {
        var deleteRepository = Substitute.For<ITransactionRepository>();
        deleteRepository.DeleteTransactionAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var viewModel = CreateSut(
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<IAccountRepository>(),
            out var dialogService,
            out _,
            out _,
            out _,
            deleteRepository);

        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var transaction = new Transaction { Id = "tx-1", Titel = "Miete" };

        await viewModel.DeleteTransaktionCommand.ExecuteAsync(transaction);

        await deleteRepository.Received(1).DeleteTransactionAsync("tx-1");
    }

    [Fact]
    public async Task DeleteTransaktion_WhenNotConfirmed_DoesNotCallDeleteUseCase()
    {
        var deleteRepository = Substitute.For<ITransactionRepository>();

        var viewModel = CreateSut(
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<IAccountRepository>(),
            out var dialogService,
            out _,
            out _,
            out _,
            deleteRepository);

        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var transaction = new Transaction { Id = "tx-1", Titel = "Miete" };

        await viewModel.DeleteTransaktionCommand.ExecuteAsync(transaction);

        await deleteRepository.DidNotReceive().DeleteTransactionAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteTransaktion_WhenTransfer_DeletesWholeGroup()
    {
        var deleteRepository = Substitute.For<ITransactionRepository>();
        deleteRepository.DeleteTransferGroupAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        deleteRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var viewModel = CreateSut(
            deleteRepository,
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<IAccountRepository>(),
            out var dialogService,
            out _,
            out _,
            out _,
            deleteRepository);

        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        var transaction = new Transaction { Id = "tx-1", Titel = "Umbuchung", IsTransfer = true, TransferGroupId = "grp-1" };

        await viewModel.DeleteTransaktionCommand.ExecuteAsync(transaction);

        await deleteRepository.Received(1).DeleteTransferGroupAsync("grp-1");
    }

    [Fact]
    public async Task ImportCsv_NavigatesToPreviewRoute()
    {
        var parser = Substitute.For<IStatementParser>();
        parser.Parse(Arg.Any<Stream>()).Returns([
            new TransactionDto { Buchungsdatum = DateTime.Today, Betrag = 10m, Zahlungsempfaenger = "Import" }
        ]);

        var importRepository = Substitute.For<ITransactionRepository>();
        importRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var importCategoryRepository = Substitute.For<ICategoryRepository>();
        importCategoryRepository.GetCategoriesAsync()
            .Returns(Task.FromResult(new List<Category>()));

        var importAccountRepository = Substitute.For<IAccountRepository>();
        importAccountRepository.GetAccountsAsync()
            .Returns(Task.FromResult(new List<Account>()));

        var importLogger = Substitute.For<ILogger<ImportService>>();
        var importService = new ImportService([parser], importRepository, importLogger, importCategoryRepository, null, importAccountRepository);

        var pickedFile = new PickFileResult("test.csv", () => Task.FromResult<Stream>(new MemoryStream()));
        var filePicker = Substitute.For<IFilePicker>();
        filePicker.PickAsync().Returns(Task.FromResult<PickFileResult?>(pickedFile));

        var importSessionStore = new ImportSessionStore();

        var viewModel = CreateSut(
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<IAccountRepository>(),
            out _,
            out _,
            out _,
            out var navigationService,
            filePicker: filePicker,
            importService: importService,
            importSessionStore: importSessionStore);

        await viewModel.ImportCsvCommand.ExecuteAsync(null);

        Assert.NotNull(importSessionStore.GetActiveSession());
        await navigationService.Received(1).GoToAsync(Routes.ImportPreview, Arg.Any<IDictionary<string, object>>());
    }

    private static TransactionsViewModel CreateSut(
        ITransactionRepository loadTransactionRepository,
        ICategoryRepository loadCategoryRepository,
        ITransactionRepository searchTransactionRepository,
        ICategoryRepository searchCategoryRepository,
        IAccountRepository loadAccountRepository,
        IAccountRepository searchAccountRepository,
        out IDialogService dialogService,
        out ILocalizationService localizationService,
        out IMainThreadDispatcher dispatcher,
        out INavigationService navigationService,
        ITransactionRepository? deleteTransactionRepository = null,
        IFilePicker? filePicker = null,
        ImportService? importService = null,
        IImportSessionStore? importSessionStore = null)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        dispatcher = Substitute.For<IMainThreadDispatcher>();
        dispatcher.InvokeAsync(Arg.Any<Func<Task>>())
            .Returns(call => call.Arg<Func<Task>>()());

        navigationService = Substitute.For<INavigationService>();
        navigationService.GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(Task.CompletedTask);
        navigationService.GoToAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        filePicker ??= Substitute.For<IFilePicker>();
        var appEvents = Substitute.For<IAppEvents>();
        var logger = Substitute.For<ILogger<TransactionsViewModel>>();
        importService ??= new ImportService(
            [],
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ILogger<ImportService>>(),
            Substitute.For<ICategoryRepository>(),
            null,
            Substitute.For<IAccountRepository>());

        deleteTransactionRepository ??= Substitute.For<ITransactionRepository>();
        deleteTransactionRepository.DeleteTransactionAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        deleteTransactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);

        var feedbackService = Substitute.For<IFeedbackService>();
        feedbackService.ShowSnackbarAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<Task>>())
            .Returns(Task.CompletedTask);

        return new TransactionsViewModel(
            new DeleteTransactionUseCase(deleteTransactionRepository),
            new RestoreTransactionUseCase(deleteTransactionRepository),
            new LoadTransactionsMonthUseCase(loadTransactionRepository, loadCategoryRepository, loadAccountRepository),
            new SearchTransactionsUseCase(searchTransactionRepository, searchCategoryRepository, searchAccountRepository),
            navigationService,
            importService,
            dialogService,
            feedbackService,
            localizationService,
            new LoadCategoriesUseCase(loadCategoryRepository),
            new LoadAccountsUseCase(loadAccountRepository),
            dispatcher,
            filePicker,
            appEvents,
            logger,
            importSessionStore);
    }
}
