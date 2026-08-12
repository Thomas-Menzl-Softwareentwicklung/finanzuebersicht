using System.Globalization;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using NSubstitute;

namespace Finanzuebersicht.Tests.ViewModels;

public class QuickExpenseCaptureViewModelTests
{
    [Fact]
    public void ApplyQueryAttributes_PrefillsAmountWithCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var sut = CreateSut();
            sut.Reset();

            sut.ApplyQueryAttributes(new Dictionary<string, object>
            {
                [NavigationQueryKeys.Amount] = "4.20",
                [NavigationQueryKeys.Title] = "Bahn"
            });

            Assert.Equal("4,20", sut.BetragText);
            Assert.Equal("Bahn", sut.Titel);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static QuickExpenseCaptureViewModel CreateSut()
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-default", SystemKey = SystemAccountKeys.Default, IsArchived = false }
        ]);

        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.EnsureAsync(Arg.Any<CancellationToken>()).Returns("cat-uncat");

        var clock = Substitute.For<IClock>();
        clock.Today.Returns(new DateTime(2026, 8, 5));

        var capture = new CaptureQuickExpenseUseCase(
            Substitute.For<ITransactionRepository>(),
            accounts,
            uncategorized,
            new TransactionValidationService(),
            clock);

        var loc = Substitute.For<ILocalizationService>();
        loc.GetString(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
        loc.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(ci => ci.ArgAt<string>(0));

        return new QuickExpenseCaptureViewModel(
            capture,
            loc,
            Substitute.For<IDialogService>(),
            Substitute.For<INavigationService>(),
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }
}
