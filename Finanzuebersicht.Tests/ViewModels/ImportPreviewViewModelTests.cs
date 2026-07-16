using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Tests.ViewModels;

public class ImportPreviewViewModelTests
{
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
            new ImportService([], Substitute.For<ITransactionRepository>(), Substitute.For<ILogger<ImportService>>(), categoryRepository),
            sessionStore,
            categoryRepository,
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

        vm.SelectedFilter = vm.FilterOptions.Single(option => option.Filter == ImportPreviewFilter.Ready);

        Assert.Equal(2, vm.FilteredRows.Count);
        Assert.All(vm.FilteredRows, row => Assert.True(row.Status is ImportPreviewRowStatus.Ready or ImportPreviewRowStatus.Uncategorized));
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
            new ImportService([], Substitute.For<ITransactionRepository>(), Substitute.For<ILogger<ImportService>>(), categoryRepository),
            sessionStore,
            categoryRepository,
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
