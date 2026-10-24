using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class DashboardDueRecurringCoordinatorTests
{
    [Fact]
    public async Task TryBookAsync_PromptsWithLastAmount_AndBooksConfirmedValue()
    {
        var item = CreateItem(isVariable: true, initialAmount: 75m, lastAmount: 88m);
        var (sut, dialog, transactionRepository) = CreateSut(item.Recurring);
        dialog.ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("92,40");

        var result = await sut.TryBookAsync(item);

        Assert.True(result);
        await dialog.Received(1).ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), "88,00");
        await transactionRepository.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Betrag == 92.40m));
    }

    [Fact]
    public async Task TryBookAsync_CancelingVariableAmountDoesNotBook()
    {
        var item = CreateItem(isVariable: true, initialAmount: 75m);
        var (sut, dialog, transactionRepository) = CreateSut(item.Recurring);
        dialog.ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);

        var result = await sut.TryBookAsync(item);

        Assert.False(result);
        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task TryBookAsync_UsesInitialAmountBeforeFirstVariableBooking()
    {
        var item = CreateItem(isVariable: true, initialAmount: 75m);
        var (sut, dialog, transactionRepository) = CreateSut(item.Recurring);
        dialog.ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("75,00");

        var result = await sut.TryBookAsync(item);

        Assert.True(result);
        await dialog.Received(1).ShowPromptAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            75m.ToString("F2", System.Globalization.CultureInfo.CurrentCulture));
        await transactionRepository.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Betrag == 75m));
    }

    [Theory]
    [InlineData("not-an-amount")]
    [InlineData("0")]
    public async Task TryBookAsync_InvalidOrNonPositiveVariableAmountDoesNotBook(string enteredAmount)
    {
        var item = CreateItem(isVariable: true, initialAmount: 75m);
        var (sut, dialog, transactionRepository) = CreateSut(item.Recurring);
        dialog.ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(enteredAmount);

        var result = await sut.TryBookAsync(item);

        Assert.False(result);
        await dialog.Received(1).ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task TryBookAsync_FixedAmountUsesConfirmation()
    {
        var item = CreateItem(isVariable: false, initialAmount: 75m, lastAmount: 88m);
        var (sut, dialog, transactionRepository) = CreateSut(item.Recurring);
        dialog.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        var result = await sut.TryBookAsync(item);

        Assert.True(result);
        await dialog.Received(1).ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await dialog.DidNotReceive().ShowPromptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await transactionRepository.Received(1).SaveTransactionAsync(NonNullArg.Is<Transaction>(t => t.Betrag == 75m));
    }

    private static (DashboardDueRecurringCoordinator Sut, IDialogService Dialog, ITransactionRepository TransactionRepository) CreateSut(RecurringTransaction recurring)
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });
        var transactions = Substitute.For<ITransactionRepository>();
        transactions.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(new List<Transaction>());
        var dialog = Substitute.For<IDialogService>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());
        return (
            new DashboardDueRecurringCoordinator(
                new GetDueRecurringWithHintsUseCase(recurringRepository),
                new BookDueRecurringInstanceUseCase(recurringRepository, transactions, Substitute.For<IAccountRepository>()),
                new SkipDueRecurringInstanceUseCase(new AddRecurringExceptionUseCase(recurringRepository)),
                dialog,
                Substitute.For<INavigationService>(),
                localization),
            dialog,
            transactions);
    }

    private static DueRecurringItem CreateItem(bool isVariable, decimal initialAmount, decimal? lastAmount = null)
        => new()
        {
            Recurring = new RecurringTransaction
            {
                Id = "rec-1",
                Titel = "Energie",
                Betrag = initialAmount,
                IsVariableAmount = isVariable,
                LastBookedAmount = lastAmount,
                KategorieId = "cat-1",
                AccountId = "acc-1"
            },
            InstanceDate = new DateTime(2026, 3, 1),
            DueDate = new DateTime(2026, 3, 1)
        };
}
