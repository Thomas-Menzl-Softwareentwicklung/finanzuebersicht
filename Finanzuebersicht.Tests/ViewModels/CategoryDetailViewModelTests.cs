using System.Globalization;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class CategoryDetailViewModelTests
{
    [Fact]
    public async Task TrySaveAsync_WhenBudgetSaveFails_SecondAttemptReusesSavedCategory()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var savedCategories = new List<Category>();
        categoryRepository.SaveCategoryAsync(Arg.Any<Category>())
            .Returns(call =>
            {
                var category = call.ArgNotNull<Category>();
                savedCategories.Add(category);
                category.Id = "cat-new";
                return Task.FromResult(category);
            });

        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(Task.FromResult(new List<CategoryBudget>()));
        budgetRepository.SaveBudgetAsync(Arg.Any<CategoryBudget>())
            .Returns(Task.FromException(new InvalidOperationException("budget failed")));

        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        var localizationService = CreateLocalizationService();
        var sut = CreateSut(
            categoryRepository,
            budgetRepository,
            dialogService,
            localizationService);

        sut.Name = "Miete";
        sut.MonthlyBudgetText = "100";

        Assert.False(await sut.TrySaveAsync());
        Assert.False(await sut.TrySaveAsync());

        Assert.Equal(2, savedCategories.Count);
        Assert.Same(savedCategories[0], savedCategories[1]);
    }

    [Fact]
    public async Task SettingCategory_LoadsDefaultBudgetIntoMonthlyBudgetText()
    {
        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns(
        [
            new CategoryBudget { Id = "b1", KategorieId = "cat-1", Betrag = 42.5m }
        ]);

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            budgetRepository,
            Substitute.For<IDialogService>(),
            CreateLocalizationService());

        sut.Category = new Category
        {
            Id = "cat-1",
            Name = "Essen",
            Icon = "🛒",
            Color = "#000",
            Typ = TransactionType.Ausgabe
        };

        var expected = 42.5m.ToString("F2", CultureInfo.CurrentCulture);
        for (var i = 0; i < 50 && sut.MonthlyBudgetText != expected; i++)
            await Task.Delay(10);

        Assert.Equal(expected, sut.MonthlyBudgetText);
    }

    [Fact]
    public async Task TrySaveAsync_PersistsBudgetViaUseCase()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.SaveCategoryAsync(Arg.Any<Category>())
            .Returns(call =>
            {
                var category = call.ArgNotNull<Category>();
                category.Id = "cat-1";
                return Task.FromResult(category);
            });

        var budgetRepository = Substitute.For<IBudgetRepository>();
        budgetRepository.GetBudgetsAsync().Returns([]);

        var feedback = Substitute.For<IFeedbackService>();
        feedback.ShowSnackbarAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var sut = CreateSut(
            categoryRepository,
            budgetRepository,
            Substitute.For<IDialogService>(),
            CreateLocalizationService(),
            feedback);

        sut.Name = "Essen";
        sut.MonthlyBudgetText = "75";

        Assert.True(await sut.TrySaveAsync());
        await budgetRepository.Received(1).SaveBudgetAsync(Arg.Is<CategoryBudget>(b =>
            b != null && b.KategorieId == "cat-1" && b.Betrag == 75m));
    }

    private static CategoryDetailViewModel CreateSut(
        ICategoryRepository categoryRepository,
        IBudgetRepository budgetRepository,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IFeedbackService? feedbackService = null) =>
        new(
            new SaveCategoryDetailUseCase(categoryRepository),
            new SaveCategoryBudgetUseCase(budgetRepository),
            new LoadCategoryBudgetUseCase(budgetRepository),
            Substitute.For<INavigationService>(),
            localizationService,
            feedbackService ?? Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>(),
            dialogService);

    private static ILocalizationService CreateLocalizationService()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));
        return localizationService;
    }
}
