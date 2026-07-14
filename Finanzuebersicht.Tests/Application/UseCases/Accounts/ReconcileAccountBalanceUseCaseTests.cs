using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Accounts;

public class ReconcileAccountBalanceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AdjustsOpeningBalanceToMatchActualBalance()
    {
        var account = new Account
        {
            Id = "acc-1",
            Name = "Giro",
            OpeningBalance = 100m
        };

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { account });
        accountRepository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new()
                {
                    AccountId = "acc-1",
                    Betrag = 50m,
                    Typ = TransactionType.Einnahme
                }
            });

        var getBalances = new GetAccountBalancesUseCase(accountRepository, transactionRepository);
        var saveAccount = new SaveAccountDetailUseCase(accountRepository);
        var sut = new ReconcileAccountBalanceUseCase(accountRepository, getBalances, saveAccount);

        var result = await sut.ExecuteAsync("acc-1", 200m);

        Assert.Equal(150m, result.CalculatedBalance);
        Assert.Equal(200m, result.ActualBalance);
        Assert.Equal(50m, result.Delta);
        Assert.Equal(150m, result.NewOpeningBalance);
        await accountRepository.Received(1).SaveAccountAsync(Arg.Is<Account>(a =>
            a != null && a.OpeningBalance == 150m));
    }
}
