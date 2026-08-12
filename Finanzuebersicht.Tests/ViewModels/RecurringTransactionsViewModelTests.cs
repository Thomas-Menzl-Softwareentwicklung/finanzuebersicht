using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;
using NSubstitute;

namespace Finanzuebersicht.Tests.ViewModels;

public class RecurringTransactionsViewModelTests
{
    [Fact]
    public async Task ToggleAktiv_PersistsUpdatedFlag()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete", Aktiv = true };
        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.GetRecurringTransactionsAsync()
            .Returns(Task.FromResult(new List<RecurringTransaction> { recurringTransaction }));
        recurringTransactionRepository.SaveRecurringTransactionAsync(Arg.Any<RecurringTransaction>())
            .Returns(call => Task.FromResult(call.ArgNotNull<RecurringTransaction>()));

        var sut = CreateSut(recurringTransactionRepository, out _, out _);
        await sut.LoadDauerauftraegeCommand.ExecuteAsync(null);

        await sut.ToggleAktivCommand.ExecuteAsync(recurringTransaction);

        Assert.False(recurringTransaction.Aktiv);
        await recurringTransactionRepository.Received(1).SaveRecurringTransactionAsync(
            NonNullArg.Is<RecurringTransaction>(item => item.Id == "rec-1" && item.Aktiv == false));
    }

    [Fact]
    public async Task GoToDetail_WhenNoItem_NavigatesToCreateDetail()
    {
        var sut = CreateSut(
            Substitute.For<IRecurringTransactionRepository>(),
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(null);

        await navigationService.Received(1).GoToAsync(Routes.RecurringTransactionDetail);
    }

    [Fact]
    public async Task GoToDetail_WhenItemProvided_NavigatesToDetail()
    {
        var recurringTransaction = new RecurringTransaction { Id = "rec-1", Titel = "Miete" };
        var sut = CreateSut(
            Substitute.For<IRecurringTransactionRepository>(),
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(recurringTransaction);

        await navigationService.Received(1).GoToAsync(
            Routes.RecurringTransactionDetail,
            NonNullArg.Is<IDictionary<string, object>>(parameters =>
                parameters.ContainsKey(NavigationQueryKeys.RecurringTransaction) &&
                object.ReferenceEquals(parameters[NavigationQueryKeys.RecurringTransaction], recurringTransaction)));
    }

    private static RecurringTransactionsViewModel CreateSut(
        IRecurringTransactionRepository recurringTransactionRepository,
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

        return new RecurringTransactionsViewModel(
            new DeleteRecurringTransactionUseCase(recurringTransactionRepository),
            new LoadRecurringTransactionsUseCase(recurringTransactionRepository),
            new ToggleRecurringTransactionActiveUseCase(recurringTransactionRepository),
            localizationService,
            navigationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
