using System.Collections.Generic;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class SparZieleViewModelTests
{
    [Fact]
    public async Task OpenCreateForm_NavigatesToSparZielDetail()
    {
        var viewModel = CreateSut(Substitute.For<ISparZielRepository>(), out _, out var navigationService);

        await viewModel.OpenCreateFormCommand.ExecuteAsync(null);

        await navigationService.Received(1).GoToAsync(Routes.SparZielDetail);
    }

    [Fact]
    public async Task DeleteSparZiel_ShowsConfirmationAndDeletesWhenConfirmed()
    {
        var repository = Substitute.For<ISparZielRepository>();
        repository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));
        repository.DeleteSparZielAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var viewModel = CreateSut(repository, out var dialogService, out _);
        viewModel.SparZiele.Add(new SparZielSummary
        {
            SparZiel = new SparZiel { Id = "ziel-1", Titel = "Urlaub" }
        });

        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await viewModel.DeleteSparZielCommand.ExecuteAsync("ziel-1");

        await dialogService.Received(1).ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await repository.Received(1).DeleteSparZielAsync("ziel-1");
    }

    private static SparZieleViewModel CreateSut(
        ISparZielRepository repository,
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
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        navigationService = Substitute.For<INavigationService>();

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        return new SparZieleViewModel(
            new LoadSparZieleUseCase(repository, transactionRepository),
            new SaveSparZielUseCase(repository),
            new DeleteSparZielUseCase(repository),
            navigationService,
            dialogService,
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
