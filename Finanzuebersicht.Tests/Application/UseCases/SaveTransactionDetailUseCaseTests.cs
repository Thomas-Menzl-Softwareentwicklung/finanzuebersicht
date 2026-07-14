using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SaveTransactionDetailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesNewTransaction_WhenExistingIsNull()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { new() { Id = "acc-1", IsArchived = false } });
        var sut = new SaveTransactionDetailUseCase(transactionRepository, accountRepository);

        await sut.ExecuteAsync(
            existingTransaction: null,
            betrag: 120.50m,
            titel: "Einkauf",
            datum: new DateTime(2026, 3, 1),
            kategorieId: "cat-1",
            accountId: "acc-1",
            typ: TransactionType.Ausgabe,
            verwendungszweck: "Zweck A");

        await transactionRepository.Received(1).SaveTransactionAsync(
            Arg.Is<Transaction>(t =>
                t != null &&
                t.Betrag == 120.50m &&
                t.Titel == "Einkauf" &&
                t.Datum == new DateTime(2026, 3, 1) &&
                t.KategorieId == "cat-1" &&
                t.AccountId == "acc-1" &&
                t.Typ == TransactionType.Ausgabe &&
                t.Verwendungszweck == "Zweck A"));
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingTransaction_WhenProvided()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { new() { Id = "acc-2", IsArchived = false } });
        var existing = new Transaction
        {
            Id = "tx-1",
            Betrag = 10m,
            Titel = "Alt",
            KategorieId = "cat-old",
            Typ = TransactionType.Ausgabe,
            Datum = new DateTime(2026, 1, 1)
        };

        var sut = new SaveTransactionDetailUseCase(transactionRepository, accountRepository);

        await sut.ExecuteAsync(
            existingTransaction: existing,
            betrag: 200m,
            titel: "Neu",
            datum: new DateTime(2026, 3, 2),
            kategorieId: "cat-2",
            accountId: "acc-2",
            typ: TransactionType.Einnahme,
            verwendungszweck: "Gehaltszahlung");

        await transactionRepository.Received(1).SaveTransactionAsync(existing);
        Assert.Equal("tx-1", existing.Id);
        Assert.Equal(200m, existing.Betrag);
        Assert.Equal("Neu", existing.Titel);
        Assert.Equal(new DateTime(2026, 3, 2), existing.Datum);
        Assert.Equal("cat-2", existing.KategorieId);
        Assert.Equal("acc-2", existing.AccountId);
        Assert.Equal(TransactionType.Einnahme, existing.Typ);
        Assert.Equal("Gehaltszahlung", existing.Verwendungszweck);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultAccount_WhenAccountIdIsNull()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var defaultAccount = new Account { Id = "acc-default", SystemKey = SystemAccountKeys.Default };
        accountRepository.GetAccountsAsync().Returns(new List<Account> { defaultAccount });

        var sut = new SaveTransactionDetailUseCase(transactionRepository, accountRepository);

        await sut.ExecuteAsync(
            existingTransaction: null,
            betrag: 50m,
            titel: "Test",
            datum: new DateTime(2026, 3, 1),
            kategorieId: "cat-1",
            accountId: null,
            typ: TransactionType.Ausgabe,
            verwendungszweck: string.Empty);

        await transactionRepository.Received(1).SaveTransactionAsync(
            Arg.Is<Transaction>(t => t != null && t.AccountId == "acc-default"));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsForTransferTransactions()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        var existing = new Transaction
        {
            Id = "tx-transfer",
            IsTransfer = true,
            TransferGroupId = "grp-1"
        };

        var sut = new SaveTransactionDetailUseCase(transactionRepository, accountRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(
                existing,
                120m,
                "Edit",
                new DateTime(2026, 3, 2),
                "cat-2",
                "acc-2",
                TransactionType.Einnahme,
                "Zweck"));
        await transactionRepository.DidNotReceive().SaveTransactionAsync(Arg.Any<Transaction>());
    }
}