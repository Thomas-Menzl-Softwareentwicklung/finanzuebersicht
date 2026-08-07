using Finanzuebersicht.Models;
using NSubstitute;
using Finanzuebersicht.Constants;

namespace Finanzuebersicht.Tests.Services;

public class InitializationServiceTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IAccountRepository _accountRepository = Substitute.For<IAccountRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();

    [Fact]
    public async Task InitializeAsync_ErstelltStandardKategorien_WennKeineVorhanden()
    {
        _categoryRepository.GetCategoriesAsync().Returns(new List<Category>());
        _accountRepository.GetAccountsAsync().Returns(new List<Account>());
        _transactionRepository.AssignMissingAccountIdsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(0);

        var service = new InitializationService(_categoryRepository, _accountRepository, _transactionRepository);
        await service.InitializeAsync();

        // 7 Standardkategorien werden erstellt
        await _categoryRepository.Received(7).SaveCategoryAsync(Arg.Any<Category>());
        await _accountRepository.Received(1).SaveAccountAsync(NonNullArg.Is<Account>(a =>
            a.SystemKey == SystemAccountKeys.Default &&
            a.Type == AccountType.Girokonto &&
            a.Name == "Girokonto"));
    }

    [Fact]
    public async Task InitializeAsync_ErstelltNichts_WennKategorienVorhanden()
    {
        _categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "1", Name = "Existiert", Icon = "📦", Color = "#007AFF", Typ = TransactionType.Ausgabe }
        });
        _accountRepository.GetAccountsAsync().Returns(new List<Account>
        {
            new() { Id = "acc-1", Name = "Girokonto", Type = AccountType.Girokonto, SystemKey = SystemAccountKeys.Default }
        });
        _transactionRepository.AssignMissingAccountIdsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(0);

        var service = new InitializationService(_categoryRepository, _accountRepository, _transactionRepository);
        await service.InitializeAsync();

        await _categoryRepository.DidNotReceive().SaveCategoryAsync(Arg.Any<Category>());
        await _accountRepository.DidNotReceive().SaveAccountAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task InitializeAsync_MigriertTransaktionen_OhneAccountAufDefaultKonto()
    {
        _categoryRepository.GetCategoriesAsync().Returns(new List<Category>
        {
            new() { Id = "1", Name = "Existiert", Icon = "📦", Color = "#007AFF", Typ = TransactionType.Ausgabe }
        });
        _accountRepository.GetAccountsAsync().Returns(new List<Account>());
        _transactionRepository.AssignMissingAccountIdsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1);

        string? defaultAccountId = null;
        _accountRepository.When(x => x.SaveAccountAsync(Arg.Any<Account>()))
            .Do(callInfo => defaultAccountId = callInfo.ArgNotNull<Account>().Id);

        var service = new InitializationService(_categoryRepository, _accountRepository, _transactionRepository);
        await service.InitializeAsync();

        Assert.NotNull(defaultAccountId);
        await _transactionRepository.Received(1).AssignMissingAccountIdsAsync(defaultAccountId!, Arg.Any<CancellationToken>());
    }
}
