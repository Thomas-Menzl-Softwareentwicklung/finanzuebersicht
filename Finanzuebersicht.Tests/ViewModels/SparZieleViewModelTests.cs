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
    public void ToggleAddForm_OpensFormAndHidesEmptyState()
    {
        var viewModel = CreateSut(Substitute.For<ISparZielRepository>(), out _, out _);
        Assert.True(viewModel.IsEmptyStateVisible);

        viewModel.ToggleAddFormCommand.Execute(null);

        Assert.True(viewModel.ShowAddForm);
        Assert.False(viewModel.IsEmptyStateVisible);
    }

    [Fact]
    public async Task SaveNewSparZiel_WithEmptyTitel_ShowsAlertAndDoesNotSave()
    {
        var repository = Substitute.For<ISparZielRepository>();
        repository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));

        var viewModel = CreateSut(repository, out var dialogService, out _);
        viewModel.NeuerTitel = string.Empty;
        viewModel.NeuesZielBetrag = 100m;

        await viewModel.SaveNewSparZielCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await repository.DidNotReceive().SaveSparZielAsync(Arg.Any<SparZiel>());
    }

    [Fact]
    public async Task SaveNewSparZiel_WithZeroBetrag_ShowsAlertAndDoesNotSave()
    {
        var repository = Substitute.For<ISparZielRepository>();
        repository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));

        var viewModel = CreateSut(repository, out var dialogService, out _);
        viewModel.NeuerTitel = "Test";
        viewModel.NeuesZielBetrag = 0m;

        await viewModel.SaveNewSparZielCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await repository.DidNotReceive().SaveSparZielAsync(Arg.Any<SparZiel>());
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
        out ILocalizationService localizationService)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        return new SparZieleViewModel(
            new LoadSparZieleUseCase(repository, transactionRepository),
            new SaveSparZielUseCase(repository),
            new DeleteSparZielUseCase(repository),
            Substitute.For<INavigationService>(),
            dialogService,
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
