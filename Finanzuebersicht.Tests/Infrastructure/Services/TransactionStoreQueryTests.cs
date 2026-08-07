using Finanzuebersicht.Infrastructure.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Infrastructure.Services;

public class TransactionStoreQueryTests : IDisposable
{
    private readonly string _dataDir;
    private readonly TransactionStore _sut;

    public TransactionStoreQueryTests()
    {
        _dataDir = Path.Combine(Path.GetTempPath(), "fu-tx-query-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataDir);
        _sut = new TransactionStore(_dataDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDir))
            Directory.Delete(_dataDir, recursive: true);
    }

    [Fact]
    public async Task GetEarliestTransactionYearAsync_ReturnsMinimumYear()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { Datum = new DateTime(2024, 6, 1) },
            new Transaction { Datum = new DateTime(2022, 1, 15) },
            new Transaction { Datum = new DateTime(2026, 3, 1) }
        ]);

        var year = await _sut.GetEarliestTransactionYearAsync();

        Assert.Equal(2022, year);
    }

    [Fact]
    public async Task GetEarliestTransactionYearAsync_ReturnsNull_WhenEmpty()
    {
        Assert.Null(await _sut.GetEarliestTransactionYearAsync());
    }

    [Fact]
    public async Task HasTransactionsForCategoryAsync_DetectsMatches()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { KategorieId = "cat-a" },
            new Transaction { KategorieId = "cat-b" }
        ]);

        Assert.True(await _sut.HasTransactionsForCategoryAsync("cat-a"));
        Assert.False(await _sut.HasTransactionsForCategoryAsync("cat-missing"));
    }

    [Fact]
    public async Task HasTransactionsForAccountAsync_DetectsMatches()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { AccountId = "acc-1" },
            new Transaction { AccountId = "acc-2" }
        ]);

        Assert.True(await _sut.HasTransactionsForAccountAsync("acc-1"));
        Assert.False(await _sut.HasTransactionsForAccountAsync("acc-missing"));
    }

    [Fact]
    public async Task GetAllTransactionsAsync_ReturnsAllUnorderedByDateFilter()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { Id = "t1", Datum = new DateTime(2020, 1, 1) },
            new Transaction { Id = "t2", Datum = new DateTime(2030, 1, 1) }
        ]);

        var all = await _sut.GetAllTransactionsAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, t => t.Id == "t1");
        Assert.Contains(all, t => t.Id == "t2");
    }

    [Fact]
    public async Task RemapCategoryIdAsync_UpdatesMatchingRowsInOnePass()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { Id = "t1", KategorieId = "old" },
            new Transaction { Id = "t2", KategorieId = "keep" }
        ]);

        var changed = await _sut.RemapCategoryIdAsync("old", "fallback");

        Assert.Equal(1, changed);
        var all = await _sut.GetAllTransactionsAsync();
        Assert.Equal("fallback", all.Single(t => t.Id == "t1").KategorieId);
        Assert.Equal("keep", all.Single(t => t.Id == "t2").KategorieId);
    }

    [Fact]
    public async Task RemapAccountIdAsync_UpdatesMatchingRowsInOnePass()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { Id = "t1", AccountId = "old" },
            new Transaction { Id = "t2", AccountId = "keep" }
        ]);

        var changed = await _sut.RemapAccountIdAsync("old", "fallback");

        Assert.Equal(1, changed);
        var all = await _sut.GetAllTransactionsAsync();
        Assert.Equal("fallback", all.Single(t => t.Id == "t1").AccountId);
        Assert.Equal("keep", all.Single(t => t.Id == "t2").AccountId);
    }

    [Fact]
    public async Task AssignMissingAccountIdsAsync_FillsEmptyAccountIds()
    {
        await _sut.SaveTransactionsAsync(
        [
            new Transaction { Id = "t1", AccountId = "" },
            new Transaction { Id = "t2", AccountId = "acc-existing" },
            new Transaction { Id = "t3", AccountId = "   " }
        ]);

        var changed = await _sut.AssignMissingAccountIdsAsync("acc-default");

        Assert.Equal(2, changed);
        var all = await _sut.GetAllTransactionsAsync();
        Assert.Equal("acc-default", all.Single(t => t.Id == "t1").AccountId);
        Assert.Equal("acc-existing", all.Single(t => t.Id == "t2").AccountId);
        Assert.Equal("acc-default", all.Single(t => t.Id == "t3").AccountId);
    }
}
