using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class RecurringTransactionsViewModelTests
{
    [Fact]
    public async Task LoadDauerauftraege_PopulatesCollection()
    {
        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.GetRecurringTransactionsAsync().Returns(Task.FromResult(new List<RecurringTransaction>
        {
            new() { Id = "rec-2", Titel = "Versicherung", Aktiv = false },
            new() { Id = "rec-1", Titel = "Miete", Aktiv = true }
        }));

        var sut = CreateSut(recurringTransactionRepository, out _, out _);

        await sut.LoadDauerauftraegeCommand.ExecuteAsync(null);

        Assert.Collection(
            sut.Dauerauftraege,
            item => Assert.Equal("rec-1", item.Id),
            item => Assert.Equal("rec-2", item.Id));
    }

    [Fact]
    public async Task DeleteDauerauftrag_WhenConfirmed_CallsDeleteUseCase()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete" };
        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.DeleteRecurringTransactionAsync("rec-1").Returns(Task.CompletedTask);

        var sut = CreateSut(recurringTransactionRepository, out var dialogService, out _);
        sut.Dauerauftraege = new ObservableCollection<RecurringTransaction> { recurringTransaction };
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await sut.DeleteDauerauftragCommand.ExecuteAsync(recurringTransaction);

        await recurringTransactionRepository.Received(1).DeleteRecurringTransactionAsync("rec-1");
        Assert.Empty(sut.Dauerauftraege);
    }

    [Fact]
    public async Task DeleteDauerauftrag_WhenNotConfirmed_DoesNotDelete()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete" };
        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        var sut = CreateSut(recurringTransactionRepository, out var dialogService, out _);

        sut.Dauerauftraege = new ObservableCollection<RecurringTransaction> { recurringTransaction };
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await sut.DeleteDauerauftragCommand.ExecuteAsync(recurringTransaction);

        await recurringTransactionRepository.DidNotReceive().DeleteRecurringTransactionAsync(Arg.Any<string>());
        Assert.Single(sut.Dauerauftraege);
    }

    [Fact]
    public async Task ToggleAktiv_CallsToggleUseCase()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete", Aktiv = true };
        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.SaveRecurringTransactionAsync(Arg.Any<RecurringTransaction>())
            .Returns(Task.CompletedTask);
        recurringTransactionRepository.GetRecurringTransactionsAsync()
            .Returns(Task.FromResult(new List<RecurringTransaction> { recurringTransaction }));

        var sut = CreateSut(recurringTransactionRepository, out _, out _);

        await sut.ToggleAktivCommand.ExecuteAsync(recurringTransaction);

        Assert.False(recurringTransaction.Aktiv);
        await recurringTransactionRepository.Received(1).SaveRecurringTransactionAsync(
            NonNullArg.Is<RecurringTransaction>(item => item.Id == "rec-1" && item.Aktiv == false));
    }

    [Fact]
    public async Task GoToDetail_WhenNoItem_ShowsCreateSheet()
    {
        var createSheet = Substitute.For<IRecurringTransactionCreateSheetService>();
        createSheet.ShowAsync(Arg.Any<RecurringTransactionDetailViewModel>()).Returns(Task.FromResult(false));

        var sut = CreateSut(
            Substitute.For<IRecurringTransactionRepository>(),
            createSheet,
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(null);

        await createSheet.Received(1).ShowAsync(Arg.Any<RecurringTransactionDetailViewModel>());
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>());
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task GoToDetail_WhenItemProvided_NavigatesToDetail()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete" };
        var sut = CreateSut(
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IRecurringTransactionCreateSheetService>(),
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(recurringTransaction);

        await navigationService.Received(1).GoToAsync(
            Routes.RecurringTransactionDetail,
            NonNullArg.Is<IDictionary<string, object>>(parameters =>
                parameters.ContainsKey("RecurringTransaction") &&
                object.ReferenceEquals(parameters["RecurringTransaction"], recurringTransaction)));
    }

    private static RecurringTransactionsViewModel CreateSut(
        IRecurringTransactionRepository recurringTransactionRepository,
        out IDialogService dialogService,
        out INavigationService navigationService)
    {
        return CreateSut(
            recurringTransactionRepository,
            Substitute.For<IRecurringTransactionCreateSheetService>(),
            out dialogService,
            out navigationService);
    }

    private static RecurringTransactionsViewModel CreateSut(
        IRecurringTransactionRepository recurringTransactionRepository,
        IRecurringTransactionCreateSheetService createSheetService,
        out IDialogService dialogService,
        out INavigationService navigationService)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));

        navigationService = Substitute.For<INavigationService>();

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>()));
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));
        var recurringGenerationService = Substitute.For<IRecurringGenerationService>();

        var createViewModel = new RecurringTransactionDetailViewModel(
            new SaveRecurringTransactionDetailUseCase(recurringTransactionRepository, recurringGenerationService, accountRepository),
            new LoadRecurringTransactionDetailDataUseCase(categoryRepository, accountRepository),
            new AddRecurringExceptionUseCase(recurringTransactionRepository),
            new RemoveRecurringExceptionUseCase(recurringTransactionRepository),
            Substitute.For<ITransactionValidationService>(),
            navigationService,
            dialogService,
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());

        return new RecurringTransactionsViewModel(
            new DeleteRecurringTransactionUseCase(recurringTransactionRepository),
            new LoadRecurringTransactionsUseCase(recurringTransactionRepository),
            new ToggleRecurringTransactionActiveUseCase(recurringTransactionRepository),
            createViewModel,
            createSheetService,
            localizationService,
            navigationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
