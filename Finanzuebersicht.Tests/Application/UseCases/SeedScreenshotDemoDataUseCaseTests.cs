using Finanzuebersicht.Application.UseCases.ScreenshotDemo;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
using Finanzuebersicht.Models;
using Finanzuebersicht.Tests.TestHelpers;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SeedScreenshotDemoDataUseCaseTests
{
    private static readonly DateTime FixedToday = new(2026, 8, 12);

    [Fact]
    public async Task ExecuteAsync_WritesDeterministicAccountsAndTransactions()
    {
        var categories = Substitute.For<ICategoryRepository>();
        var accounts = Substitute.For<IAccountRepository>();
        var transactions = Substitute.For<ITransactionRepository>();
        var recurring = Substitute.For<IRecurringTransactionRepository>();
        var budgets = Substitute.For<IBudgetRepository>();
        var sparZiele = Substitute.For<ISparZielRepository>();

        IEnumerable<Account>? capturedAccounts = null;
        IEnumerable<Category>? capturedCategories = null;
        IEnumerable<Transaction>? capturedTransactions = null;
        IEnumerable<RecurringTransaction>? capturedRecurring = null;
        IEnumerable<SparZiel>? capturedSparZiele = null;

        categories.ReplaceAllCategoriesAsync(NonNullArg.Do<IEnumerable<Category>>(c => capturedCategories = c))
            .Returns(Task.CompletedTask);
        accounts.ReplaceAllAccountsAsync(NonNullArg.Do<IEnumerable<Account>>(a => capturedAccounts = a))
            .Returns(Task.CompletedTask);
        transactions.ReplaceAllTransactionsAsync(NonNullArg.Do<IEnumerable<Transaction>>(t => capturedTransactions = t))
            .Returns(Task.CompletedTask);
        recurring.ReplaceAllRecurringTransactionsAsync(NonNullArg.Do<IEnumerable<RecurringTransaction>>(r => capturedRecurring = r))
            .Returns(Task.CompletedTask);
        budgets.ReplaceAllBudgetsAsync(Arg.Any<IEnumerable<CategoryBudget>>()).Returns(Task.CompletedTask);
        sparZiele.ReplaceAllSparZieleAsync(NonNullArg.Do<IEnumerable<SparZiel>>(s => capturedSparZiele = s))
            .Returns(Task.CompletedTask);

        var clock = new FixedClock(FixedToday);
        var sut = new SeedScreenshotDemoDataUseCase(
            categories,
            accounts,
            transactions,
            recurring,
            budgets,
            sparZiele,
            clock);

        await sut.ExecuteAsync();

        await accounts.Received(1).ReplaceAllAccountsAsync(Arg.Any<IEnumerable<Account>>());
        await categories.Received(1).ReplaceAllCategoriesAsync(Arg.Any<IEnumerable<Category>>());
        await transactions.Received(1).ReplaceAllTransactionsAsync(Arg.Any<IEnumerable<Transaction>>());
        await recurring.Received(1).ReplaceAllRecurringTransactionsAsync(Arg.Any<IEnumerable<RecurringTransaction>>());
        await budgets.Received(1).ReplaceAllBudgetsAsync(Arg.Any<IEnumerable<CategoryBudget>>());
        await sparZiele.Received(1).ReplaceAllSparZieleAsync(Arg.Any<IEnumerable<SparZiel>>());

        var accountList = capturedAccounts!.ToList();
        Assert.Equal(2, accountList.Count);
        Assert.Contains(accountList, a => a.Name == "Girokonto" && a.Type == AccountType.Girokonto);
        Assert.Contains(accountList, a => a.Name == "Sparkonto" && a.Type == AccountType.Tagesgeld);

        var categoryList = capturedCategories!.ToList();
        Assert.True(categoryList.Count >= 7);
        Assert.Contains(categoryList, c => c.SystemKey == SystemCategoryKeys.Gehalt);
        Assert.Contains(categoryList, c => c.SystemKey == SystemCategoryKeys.Lebensmittel);

        var transactionList = capturedTransactions!.ToList();
        Assert.InRange(transactionList.Count, 8, 12);
        Assert.Contains(transactionList, t => t.Typ == TransactionType.Einnahme);
        Assert.Contains(transactionList, t => t.Typ == TransactionType.Ausgabe);
        Assert.Contains(transactionList, t => t.Datum.Month == FixedToday.Month && t.Datum.Year == FixedToday.Year);
        Assert.Contains(transactionList, t => t.Datum.Month == FixedToday.AddMonths(-1).Month);

        var recurringList = capturedRecurring!.ToList();
        Assert.Contains(recurringList, r => r.Aktiv && r.Titel.Length > 0);

        var sparZielList = capturedSparZiele!.ToList();
        Assert.Contains(sparZielList, s => s.ZielBetrag > 0 && s.AktuellerBetrag > 0);
    }

    [Fact]
    public void Fixture_Create_IsDeterministicForSameClock()
    {
        var clock = new FixedClock(FixedToday);
        var first = ScreenshotDemoFixture.Create(clock);
        var second = ScreenshotDemoFixture.Create(clock);

        Assert.Equal(first.Accounts.Select(a => a.Id), second.Accounts.Select(a => a.Id));
        Assert.Equal(first.Transactions.Select(t => t.Id), second.Transactions.Select(t => t.Id));
        Assert.Equal(first.Transactions.Count, second.Transactions.Count);
    }
}
