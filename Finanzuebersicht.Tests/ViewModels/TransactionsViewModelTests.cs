using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Core.Services;
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
    public async Task EnterGesamtMode_ActivatesSearchPathAndLoadsAll()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var searchTransactionRepository = Substitute.For<ITransactionRepository>();
        searchTransactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction> { new() { Id = "t1", Titel = "Test" } }));

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
            out _,
            out _,
            out _);

        Assert.False(viewModel.IsSearchActive);

        await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsGesamtMode);
        Assert.True(viewModel.IsSearchActive);
        Assert.False(viewModel.IsMonthMode);
        await searchTransactionRepository.Received().GetTransactionsAsync(
            Arg.Is<DateTime>(d => d == DateTime.MinValue),
            Arg.Is<DateTime>(d => d == DateTime.MaxValue));
    }

    [Fact]
    public async Task ClearSearch_ResetsIsGesamtMode()
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
            out _,
            out _,
            out _);

        await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);
        await viewModel.ClearSearchCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsGesamtMode);
        Assert.True(viewModel.IsMonthMode);
    }

    [Fact]
    public async Task PreviousMonth_WhileGesamt_ExitsGesamtAndShowsMonth()
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
            out _,
            out _,
            out _);

        var before = viewModel.AktuellerMonat;
        await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);
        await viewModel.PreviousMonthCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsGesamtMode);
        Assert.Equal(before.AddMonths(-1), viewModel.AktuellerMonat);
        Assert.True(viewModel.IsMonthMode);
    }

    [Fact]
    public async Task NextMonth_WhileCategoryFilterActive_ExitsToMonthModeAndClearsFilter()
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
            out _,
            out _,
            out _);

        var before = viewModel.AktuellerMonat;
        viewModel.SelectedKategorieId = "cat-1";
        Assert.True(viewModel.IsSearchActive);

        await viewModel.NextMonthCommand.ExecuteAsync(null);

        Assert.Null(viewModel.SelectedKategorieId);
        Assert.False(viewModel.IsSearchActive);
        Assert.True(viewModel.IsMonthMode);
        Assert.Equal(before.AddMonths(1), viewModel.AktuellerMonat);
        await transactionRepository.Received().GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SelectCurrentMonthChip_ExitsGesamtAndClearsFilters()
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
            out _,
            out _,
            out _);

        await viewModel.LoadTransaktionenCommand.ExecuteAsync(null);
        viewModel.SelectedKategorieId = "cat-1";
        viewModel.SelectedKategorieFilterItem = new KategorieFilterItem("cat-1", "Test Kategorie");
        await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);
        viewModel.SearchText = "Miete";

        await viewModel.SelectCurrentMonthChipCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsGesamtMode);
        Assert.Null(viewModel.SelectedKategorieId);
        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.False(viewModel.IsSearchActive);
        Assert.True(viewModel.IsMonthMode);
    }

    [Fact]
    public async Task SelectCurrentMonthChip_WhenGesamtOnly_ShowsMonthMode()
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
            out _,
            out _,
            out _);

        await viewModel.EnterGesamtModeCommand.ExecuteAsync(null);

        await viewModel.SelectCurrentMonthChipCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsGesamtMode);
        Assert.True(viewModel.IsMonthMode);
        Assert.False(viewModel.IsSearchActive);
        await transactionRepository.Received().GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task LoadTransaktionen_WhenAlreadyLoading_QueuesSecondLoad()
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
            out _,
            out _,
            out _);

        var firstLoad = viewModel.LoadTransaktionenCommand.ExecuteAsync(null);
        await started.Task;

        var secondLoad = viewModel.LoadTransaktionenCommand.ExecuteAsync(null);

        finish.SetResult(new List<Transaction>());

        await Task.WhenAll(firstLoad, secondLoad);

        // Second call while loading is queued and runs after the first completes.
        await transactionRepository.Received(2).GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
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

        var importLogger = Substitute.For<ILogger<CsvImportOrchestrator>>();
        var analyzeUseCase = new AnalyzeCsvImportUseCase(
            new CsvImportOrchestrator(
                [parser],
                importRepository,
                importLogger,
                importCategoryRepository,
                null,
                importAccountRepository,
                new UncategorizedCategoryService(importCategoryRepository)));

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
            out _,
            out _,
            filePicker: filePicker,
            analyzeCsvImportUseCase: analyzeUseCase,
            importSessionStore: importSessionStore);

        await viewModel.ImportCsvCommand.ExecuteAsync(null);

        Assert.NotNull(importSessionStore.GetActiveSession());
        await navigationService.Received(1).GoToAsync(Routes.ImportPreview, Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task GoToDetail_WhenNoItem_OpensCreateSheet()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>()));
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));

        var viewModel = CreateSut(
            Substitute.For<ITransactionRepository>(),
            categoryRepository,
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            accountRepository,
            Substitute.For<IAccountRepository>(),
            out _,
            out _,
            out _,
            out var navigationService,
            out var transactionCreateSheet,
            out _);

        await viewModel.GoToDetailCommand.ExecuteAsync(null);

        await transactionCreateSheet.Received(1).ShowAsync(Arg.Any<TransactionDetailViewModel>());
        await navigationService.DidNotReceive().GoToAsync(Routes.TransactionDetail, Arg.Any<IDictionary<string, object>>());
        await navigationService.DidNotReceive().GoToAsync(Routes.TransactionDetail);
    }

    [Fact]
    public async Task GoToTransfer_OpensTransferCreateSheet()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "a1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default },
            new() { Id = "a2", Name = "Spar" }
        }));

        var viewModel = CreateSut(
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<ICategoryRepository>(),
            accountRepository,
            Substitute.For<IAccountRepository>(),
            out _,
            out _,
            out _,
            out var navigationService,
            out _,
            out var transferCreateSheet);

        await viewModel.GoToTransferCommand.ExecuteAsync(null);

        await transferCreateSheet.Received(1).ShowAsync(Arg.Any<TransferDetailViewModel>());
        await navigationService.DidNotReceive().GoToAsync(Routes.TransferDetail);
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
        out ITransactionCreateSheetService transactionCreateSheet,
        out ITransferCreateSheetService transferCreateSheet,
        ITransactionRepository? deleteTransactionRepository = null,
        IFilePicker? filePicker = null,
        AnalyzeCsvImportUseCase? analyzeCsvImportUseCase = null,
        IImportSessionStore? importSessionStore = null)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        dispatcher = Substitute.For<IMainThreadDispatcher>();
        dispatcher.InvokeAsync(Arg.Any<Func<Task>>())
            .Returns(call => call.ArgNotNull<Func<Task>>()());

        navigationService = Substitute.For<INavigationService>();
        navigationService.GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(Task.CompletedTask);
        navigationService.GoToAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        filePicker ??= Substitute.For<IFilePicker>();
        var appEvents = Substitute.For<IAppEvents>();
        var logger = Substitute.For<ILogger<TransactionsViewModel>>();
        analyzeCsvImportUseCase ??= new AnalyzeCsvImportUseCase(
            new CsvImportOrchestrator(
                [],
                Substitute.For<ITransactionRepository>(),
                Substitute.For<ILogger<CsvImportOrchestrator>>(),
                Substitute.For<ICategoryRepository>(),
                null,
                Substitute.For<IAccountRepository>()));

        deleteTransactionRepository ??= Substitute.For<ITransactionRepository>();
        deleteTransactionRepository.DeleteTransactionAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        deleteTransactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);
        deleteTransactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));
        deleteTransactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var feedbackService = Substitute.For<IFeedbackService>();
        feedbackService.ShowSnackbarAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<Task>>())
            .Returns(Task.CompletedTask);

        var importCoordinator = new TransactionImportCoordinator(
            analyzeCsvImportUseCase,
            filePicker,
            navigationService,
            dialogService,
            localizationService,
            importSessionStore: importSessionStore);

        var templatesCoordinator = new TransactionTemplatesCoordinator(
            navigationService,
            dialogService,
            localizationService);

        transactionCreateSheet = Substitute.For<ITransactionCreateSheetService>();
        transactionCreateSheet.ShowAsync(Arg.Any<TransactionDetailViewModel>()).Returns(false);
        transferCreateSheet = Substitute.For<ITransferCreateSheetService>();
        transferCreateSheet.ShowAsync(Arg.Any<TransferDetailViewModel>()).Returns(false);

        var sparZielRepository = Substitute.For<ISparZielRepository>();
        sparZielRepository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));

        var createTransactionVm = new TransactionDetailViewModel(
            new SaveTransactionDetailUseCase(deleteTransactionRepository, loadAccountRepository),
            new LoadTransactionDetailDataUseCase(loadCategoryRepository, loadAccountRepository),
            new GetTransactionByIdUseCase(deleteTransactionRepository),
            new LoadSparZieleUseCase(sparZielRepository, deleteTransactionRepository),
            new TransactionValidationService(),
            localizationService,
            navigationService,
            dialogService,
            feedbackService,
            appEvents);

        var createTransferVm = new TransferDetailViewModel(
            new SaveTransferUseCase(deleteTransactionRepository, loadAccountRepository),
            new LoadActiveAccountsUseCase(loadAccountRepository),
            navigationService,
            dialogService,
            localizationService,
            feedbackService,
            appEvents);

        var quickExpenseSheet = Substitute.For<IQuickExpenseCaptureSheetService>();
        quickExpenseSheet.ShowAsync(Arg.Any<QuickExpenseCaptureViewModel>()).Returns(false);

        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.EnsureAsync(Arg.Any<CancellationToken>()).Returns("cat-uncat");
        var quickExpenseVm = new QuickExpenseCaptureViewModel(
            new CaptureQuickExpenseUseCase(
                deleteTransactionRepository,
                loadAccountRepository,
                uncategorized,
                new TransactionValidationService(),
                SystemClock.Instance),
            localizationService,
            dialogService,
            navigationService,
            feedbackService,
            appEvents);

        return new TransactionsViewModel(
            new DeleteTransactionUseCase(deleteTransactionRepository),
            new RestoreTransactionUseCase(deleteTransactionRepository),
            new LoadTransactionsMonthUseCase(loadTransactionRepository, loadCategoryRepository, loadAccountRepository),
            new SearchTransactionsUseCase(searchTransactionRepository, searchCategoryRepository, searchAccountRepository),
            navigationService,
            importCoordinator,
            templatesCoordinator,
            createTransactionVm,
            createTransferVm,
            quickExpenseVm,
            transactionCreateSheet,
            transferCreateSheet,
            quickExpenseSheet,
            dialogService,
            feedbackService,
            localizationService,
            new LoadCategoriesUseCase(loadCategoryRepository),
            new LoadAccountsUseCase(loadAccountRepository),
            dispatcher,
            appEvents,
            logger);
    }
}
