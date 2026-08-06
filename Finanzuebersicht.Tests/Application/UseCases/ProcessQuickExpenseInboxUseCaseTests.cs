using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class ProcessQuickExpenseInboxUseCaseTests
{
    private static CaptureQuickExpenseUseCase CreateCapture(ITransactionRepository transactions)
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc", SystemKey = SystemAccountKeys.Default, IsArchived = false }
        ]);
        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.EnsureAsync(Arg.Any<CancellationToken>()).Returns("uncat");
        var clock = Substitute.For<IClock>();
        clock.Today.Returns(DateTime.Today);

        return new CaptureQuickExpenseUseCase(
            transactions,
            accounts,
            uncategorized,
            new TransactionValidationService(),
            UnrestrictedLicenseService.Instance,
            clock);
    }

    [Fact]
    public async Task ExecuteAsync_SavesPendingItems()
    {
        var inbox = Substitute.For<IQuickExpenseInboxStore>();
        inbox.DrainPendingAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new QuickExpenseInboxItem("1", "3.50", "Coffee", DateTimeOffset.UtcNow)
        ]);

        var transactions = Substitute.For<ITransactionRepository>();
        var sut = new ProcessQuickExpenseInboxUseCase(
            inbox,
            CreateCapture(transactions),
            Substitute.For<ILogger<ProcessQuickExpenseInboxUseCase>>());

        Assert.Equal(1, await sut.ExecuteAsync());
        await transactions.Received(1).SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ExecuteAsync_ParsesGermanDecimalComma_AsEurosNotHundreds()
    {
        var inbox = Substitute.For<IQuickExpenseInboxStore>();
        inbox.DrainPendingAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new QuickExpenseInboxItem("1", "3,50", "Kaffee", DateTimeOffset.UtcNow)
        ]);

        Transaction? saved = null;
        var transactions = Substitute.For<ITransactionRepository>();
        transactions.SaveTransactionAsync(Arg.Do<Transaction>(t => saved = t)).Returns(Task.CompletedTask);

        var sut = new ProcessQuickExpenseInboxUseCase(inbox, CreateCapture(transactions));
        Assert.Equal(1, await sut.ExecuteAsync());
        Assert.NotNull(saved);
        Assert.Equal(3.50m, saved!.Betrag);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsZero_WhenEmpty()
    {
        var inbox = Substitute.For<IQuickExpenseInboxStore>();
        inbox.DrainPendingAsync(Arg.Any<CancellationToken>()).Returns([]);
        var transactions = Substitute.For<ITransactionRepository>();

        var sut = new ProcessQuickExpenseInboxUseCase(inbox, CreateCapture(transactions));
        Assert.Equal(0, await sut.ExecuteAsync());
        await transactions.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }
}
