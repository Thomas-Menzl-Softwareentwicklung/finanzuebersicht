using System.Globalization;
using Finanzuebersicht.Application.UseCases.SparZiele;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class SparZielDetailViewModelTests
{
    [Fact]
    public void ApplyQueryAttributes_LoadsSparZielFields()
    {
        var viewModel = CreateSut(out _);
        var sparZiel = new SparZiel
        {
            Id = "goal-1",
            Titel = "Urlaub",
            Icon = "🏖️",
            ZielBetrag = 3000m,
            AktuellerBetrag = 500m,
            MonatlicheSparrate = 200m,
            Faelligkeitsdatum = new DateTime(2027, 6, 1)
        };

        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { ["SparZiel"] = sparZiel });

        Assert.Equal("Urlaub", viewModel.Titel);
        Assert.Equal("🏖️", viewModel.Icon);
        Assert.Equal(3000m.ToString("F2", CultureInfo.CurrentCulture), viewModel.ZielBetragText);
        Assert.Equal(500m.ToString("F2", CultureInfo.CurrentCulture), viewModel.AktuellerBetragText);
        Assert.Equal(200m.ToString("F2", CultureInfo.CurrentCulture), viewModel.MonatlicheSparrateText);
        Assert.True(viewModel.UseFaelligkeit);
    }

    [Fact]
    public async Task Save_WithValidData_PersistsAndNavigatesBack()
    {
        var repository = Substitute.For<ISparZielRepository>();
        repository.SaveSparZielAsync(Arg.Any<SparZiel>()).Returns(Task.CompletedTask);
        repository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));

        var viewModel = CreateSut(repository, out var navigationService);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object>
        {
            ["SparZiel"] = new SparZiel { Id = "goal-1", Titel = "Alt", ZielBetrag = 1000m }
        });
        viewModel.Titel = "Neu";
        viewModel.ZielBetragText = "1500";

        await viewModel.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).SaveSparZielAsync(NonNullArg.Is<SparZiel>(z => z.Titel == "Neu" && z.ZielBetrag == 1500m));
        await navigationService.Received(1).GoBackAsync();
    }

    [Fact]
    public async Task BookContribution_NavigatesToTransactionDetailWithSparZiel()
    {
        var viewModel = CreateSut(out var navigationService);
        var sparZiel = new SparZiel { Id = "goal-1", Titel = "Urlaub", MonatlicheSparrate = 100m };
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { ["SparZiel"] = sparZiel });

        await viewModel.BookContributionCommand.ExecuteAsync(null);

        await navigationService.Received(1).GoToAsync(
            Routes.TransactionDetail,
            Arg.Any<IDictionary<string, object>>());
        var call = navigationService.ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == nameof(INavigationService.GoToAsync));
        var args = (IDictionary<string, object>)call.GetArguments()[1]!;
        Assert.True(args.TryGetValue("SparZielContribution", out var value));
        Assert.IsType<SparZiel>(value);
        Assert.Equal("goal-1", ((SparZiel)value).Id);
    }

    private static SparZielDetailViewModel CreateSut(out INavigationService navigationService)
        => CreateSut(Substitute.For<ISparZielRepository>(), out navigationService);

    private static SparZielDetailViewModel CreateSut(
        ISparZielRepository repository,
        out INavigationService navigationService)
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));
        repository.GetSparZieleAsync().Returns(Task.FromResult(new List<SparZiel>()));

        navigationService = Substitute.For<INavigationService>();
        navigationService.GoBackAsync().Returns(Task.CompletedTask);
        navigationService.GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>())
            .Returns(Task.CompletedTask);

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgNotNull<string>());

        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        return new SparZielDetailViewModel(
            new SaveSparZielUseCase(repository),
            new DeleteSparZielUseCase(repository),
            new LoadSparZieleUseCase(repository, transactionRepository),
            navigationService,
            localizationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
