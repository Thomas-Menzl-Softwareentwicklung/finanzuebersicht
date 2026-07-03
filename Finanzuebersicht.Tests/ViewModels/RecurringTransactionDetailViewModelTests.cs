using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class RecurringTransactionDetailViewModelTests
{
    [Fact]
    public async Task ResetForCreateAsync_ClearsCategoryAndAccountSelection()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>
        {
            new() { Id = "cat-1", Name = "Miete" }
        }));
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        }));

        var sut = CreateSut(categoryRepository, accountRepository);
        sut.SelectedKategorie = new Category { Id = "cat-old", Name = "Alt" };
        sut.SelectedAccount = new Account { Id = "acc-old", Name = "Alt" };

        await sut.ResetForCreateAsync();

        Assert.Null(sut.SelectedKategorie);
        Assert.NotEqual("acc-old", sut.SelectedAccount?.Id);
        Assert.Equal("acc-1", sut.SelectedAccount?.Id);
        Assert.False(sut.IsEditing);
    }

    private static RecurringTransactionDetailViewModel CreateSut(
        ICategoryRepository categoryRepository,
        IAccountRepository accountRepository)
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());

        return new RecurringTransactionDetailViewModel(
            new SaveRecurringTransactionDetailUseCase(
                Substitute.For<IRecurringTransactionRepository>(),
                Substitute.For<IRecurringGenerationService>(),
                accountRepository),
            new LoadRecurringTransactionDetailDataUseCase(categoryRepository, accountRepository),
            new AddRecurringExceptionUseCase(Substitute.For<IRecurringTransactionRepository>()),
            new RemoveRecurringExceptionUseCase(Substitute.For<IRecurringTransactionRepository>()),
            Substitute.For<ITransactionValidationService>(),
            Substitute.For<INavigationService>(),
            Substitute.For<IDialogService>(),
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>(),
            clock: new FixedClock(new DateTime(2026, 7, 3)));
    }

    private sealed class FixedClock(DateTime today) : IClock
    {
        public DateTime Now => today;
        public DateTime Today => today.Date;
        public DateTime UtcNow => today.ToUniversalTime();
    }
}
