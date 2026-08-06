using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SaveQuickExpenseWidgetPresetsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_NormalizesGermanAmount()
    {
        var store = Substitute.For<IQuickExpenseWidgetPresetStore>();
        var sut = new SaveQuickExpenseWidgetPresetsUseCase(store);

        var result = await sut.ExecuteAsync(
        [
            new QuickExpenseWidgetPreset(0, "Kaffee", "3,50"),
            new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
            new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
            new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
        ]);

        Assert.True(result.Success);
        await store.Received(1).SaveAsync(Arg.Any<IReadOnlyList<QuickExpenseWidgetPreset>>());
        var saved = store.ReceivedCalls()
            .Select(c => c.GetArguments()[0] as IReadOnlyList<QuickExpenseWidgetPreset>)
            .First(x => x is not null)!;
        Assert.Equal("Kaffee", saved[0].Title);
        Assert.Equal("3.5", saved[0].AmountText);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTitleWithoutAmount_ReturnsInvalidAmount()
    {
        var store = Substitute.For<IQuickExpenseWidgetPresetStore>();
        var sut = new SaveQuickExpenseWidgetPresetsUseCase(store);

        var result = await sut.ExecuteAsync(
        [
            new QuickExpenseWidgetPreset(0, "Kaffee", string.Empty),
            new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
            new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
            new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
        ]);

        Assert.False(result.Success);
        Assert.Equal(0, result.InvalidSlot);
        Assert.Equal(TransactionInputError.InvalidAmountFormat, result.ValidationError);
        await store.DidNotReceive().SaveAsync(Arg.Any<IReadOnlyList<QuickExpenseWidgetPreset>>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenProMissing_Throws()
    {
        var store = Substitute.For<IQuickExpenseWidgetPresetStore>();
        var license = Substitute.For<ILicenseService>();
        license.When(l => l.EnsureFeature(AppFeature.QuickExpenseCapture))
            .Do(_ => throw new FeatureGateException(AppFeature.QuickExpenseCapture, "Pro required"));

        var sut = new SaveQuickExpenseWidgetPresetsUseCase(store, license);

        await Assert.ThrowsAsync<FeatureGateException>(() =>
            sut.ExecuteAsync(QuickExpenseWidgetPresetDefaults.CreateSeeded()));
    }
}
