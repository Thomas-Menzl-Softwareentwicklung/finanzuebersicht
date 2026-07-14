using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Accounts;

public class AccountUseCaseTests
{
    [Fact]
    public async Task SaveAccountDetailUseCase_PersistsNameAndType()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var sut = new SaveAccountDetailUseCase(repository);

        var saved = await sut.ExecuteAsync(null, "Tagesgeld", AccountType.Tagesgeld);

        await repository.Received(1).SaveAccountAsync(Arg.Is<Account>(a =>
            a != null &&
            a.Name == "Tagesgeld" &&
            a.Type == AccountType.Tagesgeld));
        Assert.Equal("Tagesgeld", saved.Name);
        Assert.Equal(AccountType.Tagesgeld, saved.Type);
    }

    [Fact]
    public async Task DeleteAccountUseCase_ReassignsTransactionsAndTemplates()
    {
        var defaultAccount = new Account { Id = "acc-default", Name = "Girokonto", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default };
        var accountToDelete = new Account { Id = "acc-old", Name = "Alt" };

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { defaultAccount, accountToDelete });
        accountRepository.DeleteAccountAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Id = "t1", AccountId = "acc-old" }
            });
        transactionRepository.SaveTransactionAsync(Arg.Any<Transaction>()).Returns(Task.CompletedTask);

        var templateRepository = Substitute.For<ITransactionTemplateRepository>();
        templateRepository.GetTransactionTemplatesAsync().Returns(new List<TransactionTemplate>
        {
            new() { Id = "tpl1", AccountId = "acc-old" }
        });
        templateRepository.SaveTransactionTemplateAsync(Arg.Any<TransactionTemplate>()).Returns(Task.CompletedTask);

        var sut = new DeleteAccountUseCase(accountRepository, transactionRepository, templateRepository);

        await sut.ExecuteAsync("acc-old");

        await transactionRepository.Received(1).SaveTransactionAsync(Arg.Is<Transaction>(t =>
            t != null && t.AccountId == "acc-default"));
        await templateRepository.Received(1).SaveTransactionTemplateAsync(Arg.Is<TransactionTemplate>(t =>
            t != null && t.AccountId == "acc-default"));
        await accountRepository.Received(1).DeleteAccountAsync("acc-old");
    }

    [Fact]
    public async Task DeleteAccountUseCase_RejectsDefaultAccount()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-default", Name = "Girokonto", SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        });

        var sut = new DeleteAccountUseCase(accountRepository, Substitute.For<ITransactionRepository>(), Substitute.For<ITransactionTemplateRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync("acc-default"));
    }

    [Fact]
    public async Task GetAccountBalancesUseCase_ComputesSaldoPerAccount()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" },
            new() { Id = "acc-2", Name = "Sparkonto" }
        });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Typ = TransactionType.Einnahme, Betrag = 1000m, AccountId = "acc-1" },
                new() { Typ = TransactionType.Ausgabe, Betrag = 200m, AccountId = "acc-1" },
                new() { Typ = TransactionType.Einnahme, Betrag = 500m, AccountId = "acc-2" }
            });

        var sut = new GetAccountBalancesUseCase(accountRepository, transactionRepository);
        var result = await sut.ExecuteAsync();

        Assert.Equal(800m, result.First(b => b.AccountId == "acc-1").Saldo);
        Assert.Equal(500m, result.First(b => b.AccountId == "acc-2").Saldo);
    }

    [Fact]
    public async Task GetAccountBalancesUseCase_TransfersAffectBothAccounts()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" },
            new() { Id = "acc-2", Name = "Tagesgeld" }
        });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Typ = TransactionType.Einnahme, Betrag = 1000m, AccountId = "acc-1" },
                new() { Typ = TransactionType.Ausgabe, Betrag = 300m, AccountId = "acc-1", IsTransfer = true },
                new() { Typ = TransactionType.Einnahme, Betrag = 300m, AccountId = "acc-2", IsTransfer = true }
            });

        var sut = new GetAccountBalancesUseCase(accountRepository, transactionRepository);
        var result = await sut.ExecuteAsync();

        Assert.Equal(700m, result.First(b => b.AccountId == "acc-1").Saldo);
        Assert.Equal(300m, result.First(b => b.AccountId == "acc-2").Saldo);
    }

    [Fact]
    public async Task GetAccountBalancesUseCase_IncludesOpeningBalance()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro", OpeningBalance = 1000m }
        });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Typ = TransactionType.Ausgabe, Betrag = 200m, AccountId = "acc-1" }
            });

        var sut = new GetAccountBalancesUseCase(accountRepository, transactionRepository);
        var result = await sut.ExecuteAsync();
        var balance = result.Single();

        Assert.Equal(1000m, balance.OpeningBalance);
        Assert.Equal(-200m, balance.TransactionBalance);
        Assert.Equal(800m, balance.Saldo);
    }

    [Fact]
    public async Task GetAccountBalancesUseCase_LegacyAccountDefaultsOpeningBalanceToZero()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" }
        });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(new List<Transaction>
            {
                new() { Typ = TransactionType.Einnahme, Betrag = 150m, AccountId = "acc-1" }
            });

        var sut = new GetAccountBalancesUseCase(accountRepository, transactionRepository);
        var result = await sut.ExecuteAsync();
        var balance = result.Single();

        Assert.Equal(0m, balance.OpeningBalance);
        Assert.Equal(150m, balance.TransactionBalance);
        Assert.Equal(150m, balance.Saldo);
    }

    [Fact]
    public async Task SaveAccountDetailUseCase_PersistsOpeningBalance()
    {
        var repository = Substitute.For<IAccountRepository>();
        repository.SaveAccountAsync(Arg.Any<Account>()).Returns(Task.CompletedTask);

        var sut = new SaveAccountDetailUseCase(repository);
        var stichtag = new DateTime(2026, 1, 1);

        var saved = await sut.ExecuteAsync(null, "Tagesgeld", AccountType.Tagesgeld, false, 2500m, stichtag);

        await repository.Received(1).SaveAccountAsync(Arg.Is<Account>(a =>
            a != null &&
            a.Name == "Tagesgeld" &&
            a.Type == AccountType.Tagesgeld &&
            a.OpeningBalance == 2500m &&
            a.OpeningBalanceDate == stichtag));
        Assert.Equal(2500m, saved.OpeningBalance);
        Assert.Equal(stichtag, saved.OpeningBalanceDate);
    }
}
