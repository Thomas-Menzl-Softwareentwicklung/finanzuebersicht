using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases.Sync;

public class ClearLocalSyncedDataUseCaseTests
{
    [Fact]
    public async Task Execute_RemovesUserSyncDataKeepsSystemRowsAndDoesNotEnableSync()
    {
        var systemAccount = new Account { Id = "sys-acc", Name = "Girokonto", SystemKey = "default" };
        var userAccount = new Account { Id = "user-acc", Name = "Sparkonto" };
        var accounts = new List<Account> { systemAccount, userAccount };

        var systemCategory = new Category { Id = "sys-cat", Name = "Sonstiges", SystemKey = "sonstiges" };
        var userCategory = new Category { Id = "user-cat", Name = "Urlaub" };
        var categories = new List<Category> { systemCategory, userCategory };

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(_ => accounts.ToList());
        accountRepository
            .When(r => r.ReplaceAllAccountsAsync(Arg.Any<IEnumerable<Account>>()))
            .Do(call =>
            {
                accounts.Clear();
                accounts.AddRange(call.Arg<IEnumerable<Account>>());
            });

        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(_ => categories.ToList());
        categoryRepository
            .When(r => r.ReplaceAllCategoriesAsync(Arg.Any<IEnumerable<Category>>()))
            .Do(call =>
            {
                categories.Clear();
                categories.AddRange(call.Arg<IEnumerable<Category>>());
            });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var sparZielRepository = Substitute.For<ISparZielRepository>();
        var budgetRepository = Substitute.For<IBudgetRepository>();
        var templateRepository = Substitute.For<ITransactionTemplateRepository>();
        var tombstoneStore = Substitute.For<ISyncTombstoneStore>();
        var metadata = new SyncMetadata
        {
            SyncEnabled = false,
            LastSyncUtc = DateTime.UtcNow.AddDays(-1),
            LastError = "old",
            SchemaVersionSeen = 1
        };
        var metadataStore = Substitute.For<ISyncMetadataStore>();
        metadataStore.GetAsync().Returns(metadata);

        budgetRepository.GetBudgetsAsync().Returns([
            new CategoryBudget { Id = "b-sys", KategorieId = systemCategory.Id, Betrag = 100 },
            new CategoryBudget { Id = "b-user", KategorieId = userCategory.Id, Betrag = 50 }
        ]);
        templateRepository.GetTransactionTemplatesAsync().Returns([
            new TransactionTemplate { Id = "tpl-1", Name = "Miete", AccountId = userAccount.Id, KategorieId = userCategory.Id }
        ]);

        var sut = new ClearLocalSyncedDataUseCase(
            accountRepository,
            categoryRepository,
            transactionRepository,
            recurringRepository,
            sparZielRepository,
            budgetRepository,
            templateRepository,
            tombstoneStore,
            metadataStore);

        await sut.ExecuteAsync();

        await transactionRepository.Received(1).ReplaceAllTransactionsAsync(
            Arg.Is<IEnumerable<Transaction>>(items => !items.Any()));
        await recurringRepository.Received(1).ReplaceAllRecurringTransactionsAsync(
            Arg.Is<IEnumerable<RecurringTransaction>>(items => !items.Any()));
        await sparZielRepository.Received(1).ReplaceAllSparZieleAsync(
            Arg.Is<IEnumerable<SparZiel>>(items => !items.Any()));
        await accountRepository.Received(1).ReplaceAllAccountsAsync(
            Arg.Is<IEnumerable<Account>>(kept => kept.Single().Id == "sys-acc"));
        await categoryRepository.Received(1).ReplaceAllCategoriesAsync(
            Arg.Is<IEnumerable<Category>>(kept => kept.Single().Id == "sys-cat"));
        await budgetRepository.Received(1).ReplaceAllBudgetsAsync(
            Arg.Is<IEnumerable<CategoryBudget>>(kept => kept.Single().Id == "b-sys"));
        await templateRepository.Received(1).ReplaceAllTransactionTemplatesAsync(
            Arg.Is<IEnumerable<TransactionTemplate>>(kept =>
                kept.Single().Id == "tpl-1" &&
                kept.Single().AccountId == "sys-acc" &&
                kept.Single().KategorieId == "sys-cat"));
        await tombstoneStore.Received(1).ClearAsync();
        Assert.False(metadata.SyncEnabled);
        Assert.Null(metadata.LastSyncUtc);
        Assert.Null(metadata.LastError);
        await metadataStore.Received(1).SaveAsync(metadata);
    }
}
