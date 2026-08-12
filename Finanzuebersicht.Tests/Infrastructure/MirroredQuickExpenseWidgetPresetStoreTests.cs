using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Services;

namespace Finanzuebersicht.Tests.Infrastructure;

public class MirroredQuickExpenseWidgetPresetStoreTests
{
    [Fact]
    public async Task SaveAsync_WritesLocalAndAppGroup()
    {
        var local = CreateTempDir();
        var group = CreateTempDir();
        try
        {
            var sut = new MirroredQuickExpenseWidgetPresetStore(local, () => group);
            var presets = new[]
            {
                new QuickExpenseWidgetPreset(0, "Taxi", "12.5"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            };

            await sut.SaveAsync(presets);

            Assert.True(File.Exists(Path.Combine(local, AppGroupIds.QuickExpensePresetsFileName)));
            Assert.True(File.Exists(Path.Combine(group, AppGroupIds.QuickExpensePresetsFileName)));

            var fromGroup = await new FileQuickExpenseWidgetPresetStore(group).LoadAsync();
            Assert.Equal("Taxi", fromGroup[0].Title);
            Assert.Equal("12.5", fromGroup[0].AmountText);
        }
        finally
        {
            Cleanup(local, group);
        }
    }

    [Fact]
    public async Task LoadAsync_MigratesLocalFileIntoAppGroupWhenGroupMissing()
    {
        var local = CreateTempDir();
        var group = CreateTempDir();
        try
        {
            await new FileQuickExpenseWidgetPresetStore(local).SaveAsync(
            [
                new QuickExpenseWidgetPreset(0, "Bus", "2.80"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);

            var sut = new MirroredQuickExpenseWidgetPresetStore(local, () => group);
            var loaded = await sut.LoadAsync();

            Assert.Equal("Bus", loaded[0].Title);
            Assert.True(File.Exists(Path.Combine(group, AppGroupIds.QuickExpensePresetsFileName)));
            var fromGroup = await new FileQuickExpenseWidgetPresetStore(group).LoadAsync();
            Assert.Equal("Bus", fromGroup[0].Title);
        }
        finally
        {
            Cleanup(local, group);
        }
    }

    [Fact]
    public async Task LoadAsync_PrefersExistingAppGroupOverLocal()
    {
        var local = CreateTempDir();
        var group = CreateTempDir();
        try
        {
            await new FileQuickExpenseWidgetPresetStore(local).SaveAsync(
            [
                new QuickExpenseWidgetPreset(0, "LocalOnly", "1.00"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);
            await new FileQuickExpenseWidgetPresetStore(group).SaveAsync(
            [
                new QuickExpenseWidgetPreset(0, "FromWidget", "9.00"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);

            var sut = new MirroredQuickExpenseWidgetPresetStore(local, () => group);
            var loaded = await sut.LoadAsync();

            Assert.Equal("FromWidget", loaded[0].Title);
            Assert.Equal("9.00", loaded[0].AmountText);
        }
        finally
        {
            Cleanup(local, group);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenAppGroupNull_StillWritesLocal()
    {
        var local = CreateTempDir();
        try
        {
            var sut = new MirroredQuickExpenseWidgetPresetStore(local, () => null);
            await sut.SaveAsync(
            [
                new QuickExpenseWidgetPreset(0, "Solo", "3.00"),
                new QuickExpenseWidgetPreset(1, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(2, string.Empty, string.Empty),
                new QuickExpenseWidgetPreset(3, string.Empty, string.Empty)
            ]);

            var loaded = await new FileQuickExpenseWidgetPresetStore(local).LoadAsync();
            Assert.Equal("Solo", loaded[0].Title);
        }
        finally
        {
            Cleanup(local);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-presets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
