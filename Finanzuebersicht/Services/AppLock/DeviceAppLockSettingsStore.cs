using System.Text.Json;
using Finanzuebersicht.Core.AppLock;
using Microsoft.Maui.Storage;

namespace Finanzuebersicht.Services.AppLock;

public sealed class DeviceAppLockSettingsStore : IAppLockSettingsStore
{
    private const string Key = "AppLock.Options.v1";

    public Task<AppLockOptions> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var preferences = Preferences.Default;
        if (!preferences.ContainsKey(Key))
            return Task.FromResult(AppLockOptions.Default);

        var json = preferences.Get(Key, string.Empty);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(nameof(AppLockOptions.Enabled), out var enabledElement)
            || enabledElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new InvalidDataException("App lock enabled setting is missing or invalid.");

        var delay = 300;
        if (root.TryGetProperty(nameof(AppLockOptions.DelaySeconds), out var delayElement))
            delay = delayElement.TryGetInt32(out var storedDelay) && AppLockOptions.AllowedDelaysSeconds.Contains(storedDelay)
                ? storedDelay
                : 0;

        return Task.FromResult(new AppLockOptions(enabledElement.GetBoolean(), delay));
    }

    public Task SaveAsync(AppLockOptions value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        value.Validate();
        var preferences = Preferences.Default;
        var json = JsonSerializer.Serialize(value);
        preferences.Set(Key, json);
        if (!string.Equals(preferences.Get(Key, string.Empty), json, StringComparison.Ordinal))
            throw new IOException("App lock settings could not be persisted.");
        return Task.CompletedTask;
    }
}
