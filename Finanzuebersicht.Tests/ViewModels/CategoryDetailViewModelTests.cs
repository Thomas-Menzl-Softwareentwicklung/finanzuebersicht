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
                var category = call.Arg<Category>()!;
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

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));

        var sut = new CategoryDetailViewModel(
            new SaveCategoryDetailUseCase(categoryRepository),
            Substitute.For<INavigationService>(),
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>(),
            dialogService,
            new SaveCategoryBudgetUseCase(budgetRepository),
            budgetRepository);

        sut.Name = "Miete";
        sut.MonthlyBudgetText = "100";

        Assert.False(await sut.TrySaveAsync());
        Assert.False(await sut.TrySaveAsync());

        Assert.Equal(2, savedCategories.Count);
        Assert.Same(savedCategories[0], savedCategories[1]);
    }
}
