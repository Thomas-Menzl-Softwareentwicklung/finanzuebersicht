using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Services;

namespace Finanzuebersicht.Tests.Infrastructure;

public class FileQuickExpenseWidgetPresetStoreTests
{
    [Fact]
    public async Task LoadAsync_WhenMissingFile_ReturnsSeededDefaults()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-presets-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileQuickExpenseWidgetPresetStore(dir);
            var presets = await store.LoadAsync();

            Assert.Equal(4, presets.Count);
            Assert.Equal("Coffee", presets[0].Title);
            Assert.Equal("3.50", presets[0].AmountText);
            Assert.Equal("Snack", presets[1].Title);
            Assert.Equal("5.00", presets[1].AmountText);
            Assert.False(presets[2].IsFilled);
            Assert.False(presets[3].IsFilled);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-presets-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileQuickExpenseWidgetPresetStore(dir);
            await store.SaveAsync(
            [
                new QuickExpenseWidgetPreset(0, "Taxi", "12.5"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, "Lunch", "8.00"),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);

            var loaded = await store.LoadAsync();
            Assert.Equal("Taxi", loaded[0].Title);
            Assert.Equal("12.5", loaded[0].AmountText);
            Assert.False(loaded[1].IsFilled);
            Assert.Equal("Lunch", loaded[2].Title);
            Assert.Equal("8.00", loaded[2].AmountText);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
