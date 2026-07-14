using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SaveCategoryDetailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesNewCategory_WhenExistingIsNull()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var sut = new SaveCategoryDetailUseCase(categoryRepository);

        await sut.ExecuteAsync(null, "Lebensmittel", "🛒", "#34C759", TransactionType.Ausgabe);

        await categoryRepository.Received(1).SaveCategoryAsync(
            Arg.Is<Category>(c =>
                c != null &&
                c.Name == "Lebensmittel" &&
                c.Icon == "🛒" &&
                c.Color == "#34C759" &&
                c.Typ == TransactionType.Ausgabe));
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingCategory_WhenProvided()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var existing = new Category
        {
            Id = "cat-1",
            Name = "Alt",
            Icon = "💰",
            Color = "#007AFF",
            Typ = TransactionType.Ausgabe
        };
        var sut = new SaveCategoryDetailUseCase(categoryRepository);

        await sut.ExecuteAsync(existing, "Neu", "🏠", "#FF3B30", TransactionType.Einnahme);

        await categoryRepository.Received(1).SaveCategoryAsync(existing);
        Assert.Equal("cat-1", existing.Id);
        Assert.Equal("Neu", existing.Name);
        Assert.Equal("🏠", existing.Icon);
        Assert.Equal("#FF3B30", existing.Color);
        Assert.Equal(TransactionType.Einnahme, existing.Typ);
    }
}