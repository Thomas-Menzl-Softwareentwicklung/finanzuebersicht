using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class ProcessQuickExpenseInboxUseCaseTests
{
    private static CaptureQuickExpenseUseCase CreateCapture(
        ITransactionRepository transactions,
        ILicenseService? license = null)
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
            license ?? UnrestrictedLicenseService.Instance,
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
            UnrestrictedLicenseService.Instance,
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

        var sut = new ProcessQuickExpenseInboxUseCase(
            inbox,
            CreateCapture(transactions),
            UnrestrictedLicenseService.Instance);
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

        var sut = new ProcessQuickExpenseInboxUseCase(
            inbox,
            CreateCapture(transactions),
            UnrestrictedLicenseService.Instance);
        Assert.Equal(0, await sut.ExecuteAsync());
        await transactions.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotPro_DoesNotDrainInbox()
    {
        var license = Substitute.For<ILicenseService>();
        license.HasFeature(AppFeature.QuickExpenseCapture).Returns(false);

        var inbox = Substitute.For<IQuickExpenseInboxStore>();
        var transactions = Substitute.For<ITransactionRepository>();
        var sut = new ProcessQuickExpenseInboxUseCase(
            inbox,
            CreateCapture(transactions, license),
            license);

        Assert.Equal(0, await sut.ExecuteAsync());
        await inbox.DidNotReceive().DrainPendingAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenFeatureGateMidBatch_RestoresRemainingItems()
    {
        var license = Substitute.For<ILicenseService>();
        license.HasFeature(AppFeature.QuickExpenseCapture).Returns(true);
        license.When(l => l.EnsureFeature(AppFeature.QuickExpenseCapture))
            .Do(_ => throw new FeatureGateException(AppFeature.QuickExpenseCapture, "Pro required"));

        var item1 = new QuickExpenseInboxItem("1", "1.00", "A", DateTimeOffset.UtcNow);
        var item2 = new QuickExpenseInboxItem("2", "2.00", "B", DateTimeOffset.UtcNow);
        var inbox = Substitute.For<IQuickExpenseInboxStore>();
        inbox.DrainPendingAsync(Arg.Any<CancellationToken>()).Returns([item1, item2]);

        var sut = new ProcessQuickExpenseInboxUseCase(
            inbox,
            CreateCapture(Substitute.For<ITransactionRepository>(), license),
            license);

        Assert.Equal(0, await sut.ExecuteAsync());
        await inbox.Received(1).WritePendingAsync(
            NonNullArg.Is<IReadOnlyList<QuickExpenseInboxItem>>(list =>
                list.Count == 2 && list[0].Id == "1" && list[1].Id == "2"),
            Arg.Any<CancellationToken>());
    }
}
