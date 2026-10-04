using Finanzuebersicht.Infrastructure.Services;

namespace Finanzuebersicht.Tests.Infrastructure.Services;

public class AtomicFileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "finanz-atomic-" + Guid.NewGuid().ToString("N"));

    public AtomicFileTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task WriteAllTextAsync_CreatesFileWhenMissing()
    {
        var path = Path.Combine(_dir, "data.json");
        await AtomicFile.WriteAllTextAsync(path, "[1]");
        Assert.Equal("[1]", await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public async Task WriteAllTextAsync_ReplacesExistingContent()
    {
        var path = Path.Combine(_dir, "data.json");
        await File.WriteAllTextAsync(path, "old");
        await AtomicFile.WriteAllTextAsync(path, "new");
        Assert.Equal("new", await File.ReadAllTextAsync(path));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void WriteAllText_ReplacesExistingContent()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "{}");
        AtomicFile.WriteAllText(path, "{\"a\":\"1\"}");
        Assert.Equal("{\"a\":\"1\"}", File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
