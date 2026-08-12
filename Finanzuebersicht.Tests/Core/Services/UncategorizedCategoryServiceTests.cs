using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Core.Services;

public class UncategorizedCategoryServiceTests
{
    [Fact]
    public async Task EnsureAsync_ReturnsExistingId()
    {
        var repo = Substitute.For<ICategoryRepository>();
        repo.GetCategoriesAsync().Returns(
        [
            new Category { Id = "c1", Name = "Unkategorisiert", SystemKey = SystemCategoryKeys.Unkategorisiert }
        ]);

        var sut = new UncategorizedCategoryService(repo);
        Assert.Equal("c1", await sut.EnsureAsync());
        await repo.DidNotReceive().SaveCategoryAsync(Arg.Any<Category>());
    }

    [Fact]
    public async Task EnsureAsync_CreatesWhenMissing()
    {
        var repo = Substitute.For<ICategoryRepository>();
        repo.GetCategoriesAsync().Returns([]);

        var sut = new UncategorizedCategoryService(repo);
        var id = await sut.EnsureAsync();

        Assert.False(string.IsNullOrWhiteSpace(id));
        await repo.Received(1).SaveCategoryAsync(
            NonNullArg.Is<Category>(c =>
                c.SystemKey == SystemCategoryKeys.Unkategorisiert &&
                c.Name == "Unkategorisiert" &&
                c.Typ == TransactionType.Ausgabe));
    }

    [Fact]
    public void IsUncategorized_MatchesSystemKeyOrName()
    {
        var sut = new UncategorizedCategoryService(Substitute.For<ICategoryRepository>());
        Assert.True(sut.IsUncategorized(new Category { SystemKey = SystemCategoryKeys.Unkategorisiert }));
        Assert.True(sut.IsUncategorized(new Category { Name = "unkategorisiert" }));
        Assert.False(sut.IsUncategorized(new Category { Name = "Sonstiges", SystemKey = SystemCategoryKeys.Sonstiges }));
    }
}
