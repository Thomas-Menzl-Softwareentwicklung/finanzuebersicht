using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.ViewModels;

public class ImportPreviewViewModelTests
{
    private static CommitCsvImportUseCase CreateCommitUseCase(
        ICategoryRepository categoryRepository,
        ITransactionRepository? transactionRepository = null)
        => new(new CsvImportOrchestrator(
            transactionRepository ?? Substitute.For<ITransactionRepository>(),
            Substitute.For<ILogger<CsvImportOrchestrator>>(),
            categoryRepository,
            uncategorizedCategoryService: new UncategorizedCategoryService(categoryRepository)));

    private static LoadCategoriesUseCase CreateLoadCategoriesUseCase(ICategoryRepository categoryRepository)
        => new(categoryRepository);

    private static async Task<ImportPreviewViewModel> CreateLoadedViewModel(
        ImportPreviewResult preview,
        params Category[] categories)
        => await CreateLoadedViewModel(preview, null, categories);

    private static async Task<ImportPreviewViewModel> CreateLoadedViewModel(
        ImportPreviewResult preview,
        ITransactionRepository? transactionRepository,
        params Category[] categories)
    {
        var sessionStore = new ImportSessionStore();
        sessionStore.SetActiveSession(preview);
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(categories.ToList());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localization.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());
        var navigation = Substitute.For<INavigationService>();
        navigation.GoBackAsync().Returns(Task.CompletedTask);
        var feedback = Substitute.For<IFeedbackService>();
        feedback.ShowSnackbarAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var vm = new ImportPreviewViewModel(
            CreateCommitUseCase(categoryRepository, transactionRepository),
            sessionStore,
            CreateLoadCategoriesUseCase(categoryRepository),
            navigation,
            Substitute.For<IDialogService>(),
            localization,
            Substitute.For<IAppEvents>(),
            feedback,
            Substitute.For<ILogger<ImportPreviewViewModel>>());
        await vm.LoadPreviewCommand.ExecuteAsync(null);
        return vm;
    }

    [Fact]
    public async Task LoadPreview_LoadsRowsAndDefaultFilterIsAll()
    {
        var sessionStore = new ImportSessionStore();
        sessionStore.SetActiveSession(new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    Status = ImportPreviewRowStatus.Ready,
                    IsIncluded = true,
                    Transaction = new Transaction { Id = "t1", Titel = "Ready", Datum = DateTime.Today, Betrag = 10m }
                },
                new ImportPreviewRow
                {
                    Id = "r2",
                    Status = ImportPreviewRowStatus.Uncategorized,
                    IsIncluded = true,
                    Transaction = new Transaction { Id = "t2", Titel = "NoCat", Datum = DateTime.Today, Betrag = 20m }
                },
                new ImportPreviewRow
                {
                    Id = "r3",
                    Status = ImportPreviewRowStatus.Duplicate,
                    IsIncluded = false,
                    Transaction = new Transaction { Id = "t3", Titel = "Dup", Datum = DateTime.Today, Betrag = 30m }
                }
            ]
        });

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns([
            new Category { Id = "cat-1", Name = "Test", Icon = "T" }
        ]);

        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localization.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        var navigation = Substitute.For<INavigationService>();
        navigation.GoBackAsync().Returns(Task.CompletedTask);

        var dialog = Substitute.For<IDialogService>();
        dialog.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var vm = new ImportPreviewViewModel(
            CreateCommitUseCase(categoryRepository),
            sessionStore,
            CreateLoadCategoriesUseCase(categoryRepository),
            navigation,
            dialog,
            localization,
            Substitute.For<IAppEvents>(),
            Substitute.For<IFeedbackService>(),
            Substitute.For<ILogger<ImportPreviewViewModel>>());

        await vm.LoadPreviewCommand.ExecuteAsync(null);

        Assert.Equal(3, vm.Rows.Count);
        Assert.NotSame(vm.Rows[0].CategoryOptions[1], vm.Rows[1].CategoryOptions[1]);
        Assert.NotNull(vm.SelectedFilter);
        Assert.Equal(ImportPreviewFilter.All, vm.SelectedFilter.Filter);
        Assert.False(vm.CanRemap);

        vm.SelectedFilter = vm.FilterOptions.Single(option => option.Filter == ImportPreviewFilter.Ready);

        Assert.Equal(2, vm.FilteredRows.Count);
        Assert.All(vm.FilteredRows, row => Assert.True(row.Status is ImportPreviewRowStatus.Ready or ImportPreviewRowStatus.Uncategorized));
    }

    [Fact]
    public async Task LoadPreview_CanRemap_TrueWhenTableInSession()
    {
        var sessionStore = new ImportSessionStore();
        Assert.True(CsvTableReader.TryRead(
            "Date,Amount,Merchant\n2026-03-01,1.00,Coffee\n"u8.ToArray(),
            out var table,
            out _));
        sessionStore.SetTable(table!, "acc-1");
        sessionStore.SetActiveSession(new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    Status = ImportPreviewRowStatus.Ready,
                    IsIncluded = true,
                    Transaction = new Transaction { Id = "t1", Titel = "Ready", Datum = DateTime.Today, Betrag = 10m }
                }
            ]
        });

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns([]);

        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localization.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        var navigation = Substitute.For<INavigationService>();
        navigation.GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>()).Returns(Task.CompletedTask);

        var vm = new ImportPreviewViewModel(
            CreateCommitUseCase(categoryRepository),
            sessionStore,
            CreateLoadCategoriesUseCase(categoryRepository),
            navigation,
            Substitute.For<IDialogService>(),
            localization,
            Substitute.For<IAppEvents>(),
            Substitute.For<IFeedbackService>(),
            Substitute.For<ILogger<ImportPreviewViewModel>>());

        await vm.LoadPreviewCommand.ExecuteAsync(null);

        Assert.True(vm.CanRemap);

        await vm.RemapColumnsCommand.ExecuteAsync(null);
        vm.HandlePageDisappearing();

        await navigation.Received(1).GoToAsync(Routes.ImportMapping, Arg.Any<IDictionary<string, object>>());
        Assert.NotNull(sessionStore.GetTable());
        Assert.NotNull(sessionStore.GetActiveSession());
    }

    [Fact]
    public async Task SelectSamePayee_SelectsOnlyCommittableRowsWithSameNormalizedPayee()
    {
        var vm = await CreateLoadedViewModel(new ImportPreviewResult
        {
            Rows =
            [
                new() { Id = "r1", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, NormalizedPayeeKey = "cafe central", Transaction = new Transaction { Titel = "Cafe Central" } },
                new() { Id = "r2", Status = ImportPreviewRowStatus.Ready, IsIncluded = true, NormalizedPayeeKey = "cafe central", Transaction = new Transaction { Titel = "CAFE CENTRAL" } },
                new() { Id = "r3", Status = ImportPreviewRowStatus.Ready, IsIncluded = true, NormalizedPayeeKey = "other shop", Transaction = new Transaction { Titel = "Other Shop" } },
                new() { Id = "r4", Status = ImportPreviewRowStatus.Duplicate, IsIncluded = false, NormalizedPayeeKey = "cafe central", Transaction = new Transaction { Titel = "Cafe Central" } },
                new() { Id = "r5", Status = ImportPreviewRowStatus.Invalid, IsIncluded = false, NormalizedPayeeKey = "cafe central", Transaction = new Transaction { Titel = "Cafe Central" } }
            ]
        });

        vm.SelectSamePayeeCommand.Execute(vm.Rows[0]);

        Assert.True(vm.Rows[0].IsCategorizationSelected);
        Assert.True(vm.Rows[1].IsCategorizationSelected);
        Assert.False(vm.Rows[2].IsCategorizationSelected);
        Assert.False(vm.Rows[3].IsCategorizationSelected);
        Assert.False(vm.Rows[4].IsCategorizationSelected);
    }

    [Fact]
    public async Task ApplyCategoryToSelection_UpdatesSelectedRowsAndLeavesUnselectedRowsUnchanged()
    {
        var vm = await CreateLoadedViewModel(new ImportPreviewResult
        {
            Rows =
            [
                new() { Id = "r1", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, NormalizedPayeeKey = "merchant", Transaction = new Transaction { Titel = "Merchant" } },
                new() { Id = "r2", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, NormalizedPayeeKey = "merchant", Transaction = new Transaction { Titel = "Merchant" } },
                new() { Id = "r3", Status = ImportPreviewRowStatus.Duplicate, IsIncluded = false, NormalizedPayeeKey = "merchant", Transaction = new Transaction { Titel = "Duplicate", KategorieId = "old" } }
            ]
        }, new Category { Id = "food", Name = "Food", Icon = "🍎" }, new Category { Id = "old", Name = "Old", Icon = "O" });

        vm.Rows[0].IsCategorizationSelected = true;
        vm.Rows[1].IsCategorizationSelected = true;
        vm.SelectedBulkCategory = vm.Rows[0].CategoryOptions.Single(option => option.Id == "food");
        vm.ApplyCategoryToSelectionCommand.Execute(null);

        Assert.All(vm.Rows.Take(2), row =>
        {
            Assert.Equal("food", row.SelectedCategoryOption?.Id);
            Assert.Equal(ImportPreviewRowStatus.Ready, row.Status);
        });
        Assert.Equal("old", vm.Rows[2].SelectedCategoryOption?.Id);
        Assert.Equal(ImportPreviewRowStatus.Duplicate, vm.Rows[2].Status);

        vm.ClearCategorizationSelectionCommand.Execute(null);
        Assert.All(vm.Rows, row => Assert.False(row.IsCategorizationSelected));
        Assert.All(vm.Rows.Take(2), row => Assert.Equal("food", row.SelectedCategoryOption?.Id));
    }

    [Fact]
    public async Task LoadPreview_ExposesLocalizedCategoryMatchSource()
    {
        var vm = await CreateLoadedViewModel(new ImportPreviewResult
        {
            Rows =
            [
                new() { Id = "rule", Status = ImportPreviewRowStatus.Ready, IsIncluded = true, CategoryMatchSource = ImportCategoryMatchSource.KeywordRule, Transaction = new Transaction { Titel = "Rule" } },
                new() { Id = "history", Status = ImportPreviewRowStatus.Ready, IsIncluded = true, CategoryMatchSource = ImportCategoryMatchSource.History, Transaction = new Transaction { Titel = "History" } },
                new() { Id = "none", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, CategoryMatchSource = ImportCategoryMatchSource.None, Transaction = new Transaction { Titel = "None" } }
            ]
        });

        Assert.Equal(ResourceKeys.Lbl_ImportCategorySourceRule, vm.Rows[0].CategoryMatchSourceText);
        Assert.Equal(ResourceKeys.Lbl_ImportCategorySourceHistory, vm.Rows[1].CategoryMatchSourceText);
        Assert.Equal(ResourceKeys.Lbl_ImportCategorySourceNone, vm.Rows[2].CategoryMatchSourceText);
    }

    [Fact]
    public async Task ToggleCategorySelectionMode_ClearsSelectionWhenClosed()
    {
        var vm = await CreateLoadedViewModel(new ImportPreviewResult
        {
            Rows = [new() { Id = "r1", Status = ImportPreviewRowStatus.Ready, IsIncluded = true, NormalizedPayeeKey = "payee", Transaction = new Transaction { Titel = "Payee" } }]
        });

        vm.ToggleCategorySelectionModeCommand.Execute(null);
        vm.Rows[0].IsCategorizationSelected = true;
        Assert.Equal(1, vm.SelectedCategoryCount);

        vm.ToggleCategorySelectionModeCommand.Execute(null);

        Assert.False(vm.IsCategorySelectionMode);
        Assert.Equal(0, vm.SelectedCategoryCount);
        Assert.False(vm.Rows[0].IsCategorizationSelected);
    }

    [Fact]
    public async Task BulkCategoryAssignment_CommitsOnlyIncludedRowsWithAssignedCategory()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns([]);
        transactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);
        var food = new Category { Id = "food", Name = "Food", Icon = "🍎" };
        var vm = await CreateLoadedViewModel(new ImportPreviewResult
        {
            Rows =
            [
                new() { Id = "r1", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, NormalizedPayeeKey = "merchant", Transaction = new Transaction { Id = "t1", Titel = "Merchant", Datum = DateTime.Today, Betrag = 10m } },
                new() { Id = "r2", Status = ImportPreviewRowStatus.Uncategorized, IsIncluded = true, NormalizedPayeeKey = "merchant", Transaction = new Transaction { Id = "t2", Titel = "Merchant", Datum = DateTime.Today, Betrag = 20m } }
            ]
        }, transactionRepository, food);
        vm.SelectSamePayeeCommand.Execute(vm.Rows[0]);
        vm.SelectedBulkCategory = vm.BulkCategoryOptions.Single(option => option.Id == "food");
        vm.ApplyCategoryToSelectionCommand.Execute(null);
        vm.Rows[1].IsIncluded = false;
        await vm.CommitImportCommand.ExecuteAsync(null);

        await transactionRepository.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(transaction =>
            transaction.Id == "t1" && transaction.KategorieId == "food"));
        await transactionRepository.DidNotReceive().SaveTransactionAsync(NonNullArg.Is<Transaction>(transaction =>
            transaction.Id == "t2"));
    }

    [Fact]
    public void HandlePageDisappearing_ClearsSession()
    {
        var sessionStore = new ImportSessionStore();
        sessionStore.SetActiveSession(new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    Status = ImportPreviewRowStatus.Ready,
                    Transaction = new Transaction { Id = "t1", Titel = "Ready", Datum = DateTime.Today, Betrag = 1m }
                }
            ]
        });

        var categoryRepository = Substitute.For<ICategoryRepository>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        var vm = new ImportPreviewViewModel(
            CreateCommitUseCase(categoryRepository),
            sessionStore,
            CreateLoadCategoriesUseCase(categoryRepository),
            Substitute.For<INavigationService>(),
            Substitute.For<IDialogService>(),
            localization,
            Substitute.For<IAppEvents>(),
            Substitute.For<IFeedbackService>(),
            Substitute.For<ILogger<ImportPreviewViewModel>>());

        vm.HandlePageDisappearing();

        Assert.Null(sessionStore.GetActiveSession());
    }
}
