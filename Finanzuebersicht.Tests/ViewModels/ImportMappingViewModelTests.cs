using Finanzuebersicht.Application.UseCases.Import;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Tests.Application.UseCases.Import;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.ViewModels;

public class ImportMappingViewModelTests
{
    [Fact]
    public async Task Continue_DisabledUntilDateAmountTitle_ThenUpsertsAndNavigatesToPreview()
    {
        var sessionStore = new ImportSessionStore();
        Assert.True(CsvTableReader.TryRead(
            "Date,Amount,Merchant\n2026-03-01,1.00,Coffee\n"u8.ToArray(),
            out var table,
            out _));
        sessionStore.SetTable(table!, accountId: "acc-1");

        var profileStore = Substitute.For<ICsvImportProfileStore>();
        profileStore.GetUserProfilesAsync()
            .Returns(Task.FromResult<IReadOnlyList<CsvImportProfile>>([]));
        CsvImportProfile? upserted = null;
        profileStore.UpsertAsync(Arg.Any<CsvImportProfile>())
            .Returns(call =>
            {
                upserted = call.Arg<CsvImportProfile>();
                return Task.CompletedTask;
            });

        var navigation = Substitute.For<INavigationService>();
        navigation.GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
            .Returns(Task.CompletedTask);

        var vm = CreateSut(sessionStore, profileStore, navigation);
        await vm.LoadMappingCommand.ExecuteAsync(null);

        Assert.False(vm.CanContinue);

        vm.SelectedDate = vm.ColumnOptions.Single(o => o.Header == "Date");
        vm.SelectedAmount = vm.ColumnOptions.Single(o => o.Header == "Amount");
        vm.SelectedTitle = vm.ColumnOptions.Single(o => o.Header == "Merchant");

        Assert.True(vm.CanContinue);

        await vm.ContinueCommand.ExecuteAsync(null);

        Assert.NotNull(upserted);
        Assert.False(upserted.IsBuiltIn);
        Assert.Equal("Date", upserted.Columns.Date);
        Assert.Equal("Amount", upserted.Columns.Amount);
        Assert.Equal("Merchant", upserted.Columns.Title);
        await profileStore.Received(1).UpsertAsync(Arg.Any<CsvImportProfile>());
        await navigation.Received(1).GoToAsync(Routes.ImportPreview, Arg.Any<IDictionary<string, object>>());
        Assert.NotNull(sessionStore.GetActiveSession());
        Assert.Same(upserted, sessionStore.GetProfile());
    }

    [Fact]
    public async Task Continue_WhenOpenedFromPreview_GoesBackInsteadOfPushingPreview()
    {
        var sessionStore = new ImportSessionStore();
        Assert.True(CsvTableReader.TryRead(
            "Date,Amount,Merchant\n2026-03-01,1.00,Coffee\n"u8.ToArray(),
            out var table,
            out _));
        sessionStore.SetTable(table!, accountId: "acc-1");
        sessionStore.SetActiveSession(new ImportPreviewResult
        {
            Rows =
            [
                new ImportPreviewRow
                {
                    Id = "r1",
                    Status = ImportPreviewRowStatus.Ready,
                    Transaction = new Transaction { Id = "t1", Titel = "Old", Datum = DateTime.Today, Betrag = 1m }
                }
            ]
        });

        var profileStore = Substitute.For<ICsvImportProfileStore>();
        profileStore.UpsertAsync(Arg.Any<CsvImportProfile>()).Returns(Task.CompletedTask);

        var navigation = Substitute.For<INavigationService>();
        navigation.GoBackAsync().Returns(Task.CompletedTask);

        var vm = CreateSut(sessionStore, profileStore, navigation);
        await vm.LoadMappingCommand.ExecuteAsync(null);
        vm.SelectedDate = vm.ColumnOptions.Single(o => o.Header == "Date");
        vm.SelectedAmount = vm.ColumnOptions.Single(o => o.Header == "Amount");
        vm.SelectedTitle = vm.ColumnOptions.Single(o => o.Header == "Merchant");

        await vm.ContinueCommand.ExecuteAsync(null);

        await navigation.Received(1).GoBackAsync();
        await navigation.DidNotReceive().GoToAsync(Routes.ImportPreview, Arg.Any<IDictionary<string, object>>());
    }

    private static ImportMappingViewModel CreateSut(
        IImportSessionStore sessionStore,
        ICsvImportProfileStore profileStore,
        INavigationService navigation)
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localization.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        var dialog = Substitute.For<IDialogService>();
        dialog.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var txRepo = Substitute.For<ITransactionRepository>();
        txRepo.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));
        var catRepo = Substitute.For<ICategoryRepository>();
        catRepo.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>()));
        var orchestrator = new CsvImportOrchestrator(
            txRepo,
            Substitute.For<ILogger<CsvImportOrchestrator>>(),
            catRepo,
            uncategorizedCategoryService: new UncategorizedCategoryService(catRepo));
        var prepare = new PrepareCsvImportUseCase(new InMemoryCsvImportProfileStore(), orchestrator);
        var analyze = new AnalyzeCsvImportUseCase(orchestrator, prepare);

        return new ImportMappingViewModel(
            analyze,
            profileStore,
            sessionStore,
            navigation,
            dialog,
            localization);
    }
}
