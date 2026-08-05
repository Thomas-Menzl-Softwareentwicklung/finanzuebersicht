using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Services;

namespace Finanzuebersicht.Tests.Infrastructure;

public class FileQuickExpenseInboxStoreTests
{
    [Fact]
    public async Task DrainPendingAsync_ReturnsAndClearsItems()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-inbox-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileQuickExpenseInboxStore(dir);
            await store.EnqueueAsync(new QuickExpenseInboxItem("a", "1.00", "A", DateTimeOffset.UtcNow));
            await store.EnqueueAsync(new QuickExpenseInboxItem("b", "2.00", "B", DateTimeOffset.UtcNow));

            var first = await store.DrainPendingAsync();
            Assert.Equal(2, first.Count);
            Assert.Equal("A", first[0].Title);

            var second = await store.DrainPendingAsync();
            Assert.Empty(second);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}
