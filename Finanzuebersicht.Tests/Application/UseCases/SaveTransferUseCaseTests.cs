using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SaveTransferUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_SavesTwoTransferLegsInSingleCall()
    {
        var repository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", IsArchived = false },
            new() { Id = "acc-2", IsArchived = false }
        });
        var sut = new SaveTransferUseCase(repository, accountRepository);

        await sut.ExecuteAsync("acc-1", "acc-2", 150m, new DateTime(2026, 4, 10), "Umbuchung", "Test");

        await repository.Received(1).SaveTransactionsAsync(Arg.Is<IEnumerable<Transaction>>(items =>
            items != null
            && items.Count() == 2
            && items.All(t => t.IsTransfer)
            && items.Select(t => t.TransferGroupId).Distinct().Count() == 1
            && items.Any(t => t.AccountId == "acc-1" && t.Typ == TransactionType.Ausgabe && t.Betrag == 150m)
            && items.Any(t => t.AccountId == "acc-2" && t.Typ == TransactionType.Einnahme && t.Betrag == 150m)));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenAccountsEqual()
    {
        var repository = Substitute.For<ITransactionRepository>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", IsArchived = false }
        });
        var sut = new SaveTransferUseCase(repository, accountRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync("acc-1", "acc-1", 150m, new DateTime(2026, 4, 10)));
    }
}
