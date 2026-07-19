
namespace Finanzuebersicht.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _settings;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"finanz_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settings = new SettingsService(Path.Combine(_tempDir, "settings.json"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void Get_ReturnsDefaultWhenKeyMissing()
    {
        var result = _settings.Get("NonExistentKey_" + Guid.NewGuid(), "defaultValue");
        Assert.Equal("defaultValue", result);
    }

    [Fact]
    public void SetAndGet_RoundTrip()
    {
        var key = "TestKey_" + Guid.NewGuid();
        _settings.Set(key, "TestValue");
        Assert.Equal("TestValue", _settings.Get(key));
    }

    [Fact]
    public void Set_OverwritesExistingValue()
    {
        var key = "OverwriteKey_" + Guid.NewGuid();
        _settings.Set(key, "Initial");
        _settings.Set(key, "Updated");
        Assert.Equal("Updated", _settings.Get(key));
    }

    [Fact]
    public void Constructor_LoadsExistingSettingsFile()
    {
        var path = Path.Combine(_tempDir, "existing-settings.json");
        File.WriteAllText(path, "{\"Theme\":\"Dark\",\"Currency\":\"EUR\"}");

        var settings = new SettingsService(path);

        Assert.Equal("Dark", settings.Get(SettingsKeys.Theme));
        Assert.Equal("EUR", settings.Get(SettingsKeys.Currency));
    }

    [Fact]
    public void Constructor_InvalidJson_DoesNotThrowAndUsesDefaults()
    {
        var path = Path.Combine(_tempDir, "broken-settings.json");
        File.WriteAllText(path, "{ not-valid-json }");

        var exception = Record.Exception(() => new SettingsService(path));

        Assert.Null(exception);

        var settings = new SettingsService(path);
        Assert.Equal("fallback", settings.Get(SettingsKeys.Theme, "fallback"));
    }

    [Fact]
    public void Set_CreatesMissingDirectoryAndPersistsValue()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "config", "settings.json");
        var settings = new SettingsService(nestedPath);

        settings.Set("Language", "de");

        Assert.True(File.Exists(nestedPath));

        var reloaded = new SettingsService(nestedPath);
        Assert.Equal("de", reloaded.Get("Language"));
    }

    [Fact]
    public void Set_WithInvalidPath_DoesNotThrowAndKeepsValueInMemory()
    {
        var invalidPath = "\0invalid-path";
        var settings = new SettingsService(invalidPath);

        var exception = Record.Exception(() => settings.Set("Key", "Value"));

        Assert.Null(exception);
        Assert.Equal("Value", settings.Get("Key"));
    }
}
