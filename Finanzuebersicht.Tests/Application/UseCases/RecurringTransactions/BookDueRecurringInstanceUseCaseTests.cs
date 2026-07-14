using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.RecurringTransactions;

public class BookDueRecurringInstanceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesTransactionAndUpdatesLastExecution()
    {
        var recurring = new RecurringTransaction
        {
            Id = "rec-1",
            Titel = "Miete",
            Betrag = 800m,
            KategorieId = "cat-rent",
            AccountId = "acc-1",
            Typ = TransactionType.Ausgabe
        };

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>());
        var accountRepository = Substitute.For<IAccountRepository>();

        var sut = new BookDueRecurringInstanceUseCase(recurringRepository, transactionRepository, accountRepository);
        var instanceDate = new DateTime(2026, 3, 1);

        await sut.ExecuteAsync("rec-1", instanceDate);

        await transactionRepository.Received(1).SaveTransactionAsync(Arg.Is<Transaction>(t =>
            t != null
            && t.DauerauftragId == "rec-1"
            && t.Betrag == 800m
            && t.AccountId == "acc-1"
            && t.Datum == instanceDate));
        await recurringRepository.Received(1).SaveRecurringTransactionAsync(Arg.Is<RecurringTransaction>(r =>
            r != null && r.LetzteAusfuehrung == instanceDate));
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultAccount_WhenRecurringHasNoAccount()
    {
        var recurring = new RecurringTransaction
        {
            Id = "rec-1",
            Titel = "Abo",
            Betrag = 15m,
            KategorieId = "cat-sub",
            Typ = TransactionType.Ausgabe
        };

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>());
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-default", SystemKey = SystemAccountKeys.Default }
        });

        var sut = new BookDueRecurringInstanceUseCase(recurringRepository, transactionRepository, accountRepository);

        await sut.ExecuteAsync("rec-1", new DateTime(2026, 3, 1));

        await transactionRepository.Received(1).SaveTransactionAsync(Arg.Is<Transaction>(t =>
            t != null && t.AccountId == "acc-default"));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenInstanceAlreadyBooked()
    {
        var instanceDate = new DateTime(2026, 3, 1);
        var recurring = new RecurringTransaction
        {
            Id = "rec-1",
            Titel = "Miete",
            Betrag = 800m,
            KategorieId = "cat-rent",
            AccountId = "acc-1",
            Typ = TransactionType.Ausgabe
        };

        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringRepository.GetRecurringTransactionsAsync().Returns(new List<RecurringTransaction> { recurring });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { DauerauftragId = "rec-1", Datum = instanceDate, Betrag = 800m }
            });

        var accountRepository = Substitute.For<IAccountRepository>();
        var sut = new BookDueRecurringInstanceUseCase(recurringRepository, transactionRepository, accountRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync("rec-1", instanceDate));
        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }
}
