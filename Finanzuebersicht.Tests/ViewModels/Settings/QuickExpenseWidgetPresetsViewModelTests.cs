using System.Globalization;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using NSubstitute;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class QuickExpenseWidgetPresetsViewModelTests
{
    [Fact]
    public async Task LoadAsync_FillsSlotsFromStore_WithDisplayAmounts()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var store = Substitute.For<IQuickExpenseWidgetPresetStore>();
            store.LoadAsync(Arg.Any<CancellationToken>()).Returns(
            [
                new QuickExpenseWidgetPreset(0, "A", "1.00"),
                new QuickExpenseWidgetPreset(1, "B", "2.00"),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);

            var sut = CreateSut(store);
            await sut.LoadAsync();

            Assert.Equal("A", sut.Slots[0].Title);
            Assert.Equal("1,00", sut.Slots[0].AmountText);
            Assert.Equal("B", sut.Slots[1].Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task SaveAsync_WhenValid_ReloadsWidgetTimeline()
    {
        var store = Substitute.For<IQuickExpenseWidgetPresetStore>();
        store.LoadAsync(Arg.Any<CancellationToken>()).Returns(QuickExpenseWidgetPresetDefaults.CreateSeeded());
        var reloader = Substitute.For<IWidgetTimelineReloader>();
        var feedback = Substitute.For<IFeedbackService>();

        var sut = CreateSut(store, reloader, feedback);
        sut.Slots[0].Title = "Taxi";
        sut.Slots[0].AmountText = "10";
        sut.Slots[1].Title = string.Empty;
        sut.Slots[1].AmountText = string.Empty;
        sut.Slots[2].Title = string.Empty;
        sut.Slots[2].AmountText = string.Empty;
        sut.Slots[3].Title = string.Empty;
        sut.Slots[3].AmountText = string.Empty;

        await sut.SaveCommand.ExecuteAsync(null);

        await store.Received(1).SaveAsync(Arg.Any<IReadOnlyList<QuickExpenseWidgetPreset>>());
        reloader.Received(1).ReloadAll();
        await feedback.Received(1).ShowSnackbarAsync(Arg.Any<string>());
    }

    private static QuickExpenseWidgetPresetsViewModel CreateSut(
        IQuickExpenseWidgetPresetStore store,
        IWidgetTimelineReloader? reloader = null,
        IFeedbackService? feedback = null)
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetString(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
        loc.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(ci =>
        {
            var key = ci.ArgAt<string>(0);
            var args = ci.ArgAt<object[]>(1);
            return args.Length == 0 ? key : string.Format(key, args);
        });

        return new QuickExpenseWidgetPresetsViewModel(
            new LoadQuickExpenseWidgetPresetsUseCase(store),
            new SaveQuickExpenseWidgetPresetsUseCase(store),
            loc,
            Substitute.For<IDialogService>(),
            feedback ?? Substitute.For<IFeedbackService>(),
            reloader ?? Substitute.For<IWidgetTimelineReloader>());
    }
}
