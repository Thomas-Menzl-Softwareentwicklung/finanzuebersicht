using System.Globalization;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class CaptureQuickExpenseUseCaseTests
{
    private static CaptureQuickExpenseUseCase CreateSut(
        ITransactionRepository? transactionRepository = null,
        IAccountRepository? accountRepository = null,
        IUncategorizedCategoryService? uncategorized = null,
        IClock? clock = null)
    {
        transactionRepository ??= Substitute.For<ITransactionRepository>();
        accountRepository ??= Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(
        [
            new Account { Id = "acc-default", SystemKey = SystemAccountKeys.Default, IsArchived = false }
        ]);

        if (uncategorized is null)
        {
            uncategorized = Substitute.For<IUncategorizedCategoryService>();
            uncategorized.EnsureAsync(Arg.Any<CancellationToken>()).Returns("cat-uncat");
        }

        clock ??= Substitute.For<IClock>();
        clock.Today.Returns(new DateTime(2026, 8, 5));

        return new CaptureQuickExpenseUseCase(
            transactionRepository,
            accountRepository,
            uncategorized,
            new TransactionValidationService(),
            clock);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesAusgabe_WithUncategorizedAndDefaultAccount()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var sut = CreateSut(transactionRepository);

        var result = await sut.ExecuteAsync("12,50", "Kaffee", CultureInfo.GetCultureInfo("de-DE"));

        Assert.True(result.Success);
        Assert.NotNull(result.Transaction);
        await transactionRepository.Received(1).SaveTransactionAsync(
            NonNullArg.Is<Transaction>(t =>
                t.Betrag == 12.50m &&
                t.Titel == "Kaffee" &&
                t.KategorieId == "cat-uncat" &&
                t.AccountId == "acc-default" &&
                t.Typ == TransactionType.Ausgabe &&
                t.Datum == new DateTime(2026, 8, 5)));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsValidationError_WhenTitleMissing()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var sut = CreateSut(transactionRepository);

        var result = await sut.ExecuteAsync("5", "  ", CultureInfo.InvariantCulture);

        Assert.False(result.Success);
        Assert.Equal(TransactionInputError.TitleRequired, result.ValidationError);
        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesUncategorizedCategory_ViaService()
    {
        var uncategorized = Substitute.For<IUncategorizedCategoryService>();
        uncategorized.EnsureAsync(Arg.Any<CancellationToken>()).Returns("new-uncat");
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var sut = CreateSut(transactionRepository, uncategorized: uncategorized);

        await sut.ExecuteAsync("1.00", "Bus", CultureInfo.InvariantCulture);

        await uncategorized.Received(1).EnsureAsync(Arg.Any<CancellationToken>());
        await transactionRepository.Received(1).SaveTransactionAsync(
            NonNullArg.Is<Transaction>(t => t.KategorieId == "new-uncat"));
    }
}
