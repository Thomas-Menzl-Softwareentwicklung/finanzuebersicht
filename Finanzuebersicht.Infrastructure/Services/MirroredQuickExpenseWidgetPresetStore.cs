using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// Persists presets in the app data directory and mirrors them into an App Group
/// container when available (iOS WidgetKit). App Group path is resolved lazily on
/// each call — resolving at DI registration time often yields null and leaves the
/// widget on stale/seeded shortcuts while Settings still look correct.
/// </summary>
public sealed class MirroredQuickExpenseWidgetPresetStore : IQuickExpenseWidgetPresetStore
{
    private readonly string _localDataDirectory;
    private readonly Func<string?> _tryGetAppGroupDirectory;
    private readonly ILogger<MirroredQuickExpenseWidgetPresetStore>? _logger;
    private readonly ILogger<FileQuickExpenseWidgetPresetStore>? _fileLogger;

    public MirroredQuickExpenseWidgetPresetStore(
        string localDataDirectory,
        Func<string?> tryGetAppGroupDirectory,
        ILogger<MirroredQuickExpenseWidgetPresetStore>? logger = null,
        ILogger<FileQuickExpenseWidgetPresetStore>? fileLogger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localDataDirectory);
        ArgumentNullException.ThrowIfNull(tryGetAppGroupDirectory);
        _localDataDirectory = localDataDirectory;
        _tryGetAppGroupDirectory = tryGetAppGroupDirectory;
        _logger = logger;
        _fileLogger = fileLogger;
    }

    public async Task<IReadOnlyList<QuickExpenseWidgetPreset>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var local = CreateStore(_localDataDirectory);
        var groupDir = _tryGetAppGroupDirectory();

        if (!string.IsNullOrWhiteSpace(groupDir))
        {
            var groupFile = Path.Combine(groupDir, AppGroupIds.QuickExpensePresetsFileName);
            if (File.Exists(groupFile))
                return await CreateStore(groupDir).LoadAsync(cancellationToken).ConfigureAwait(false);

            var localFile = Path.Combine(_localDataDirectory, AppGroupIds.QuickExpensePresetsFileName);
            if (File.Exists(localFile))
            {
                var fromLocal = await local.LoadAsync(cancellationToken).ConfigureAwait(false);
                await CreateStore(groupDir).SaveAsync(fromLocal, cancellationToken).ConfigureAwait(false);
                _logger?.LogInformation(
                    "Migrated quick-expense presets from app data to App Group ({Path})",
                    groupFile);
                return fromLocal;
            }
        }

        return await local.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(
        IReadOnlyList<QuickExpenseWidgetPreset> presets,
        CancellationToken cancellationToken = default)
    {
        await CreateStore(_localDataDirectory).SaveAsync(presets, cancellationToken).ConfigureAwait(false);

        var groupDir = _tryGetAppGroupDirectory();
        if (string.IsNullOrWhiteSpace(groupDir))
        {
            _logger?.LogWarning(
                "App Group container unavailable; widget cannot see preset changes until App Group works");
            return;
        }

        await CreateStore(groupDir).SaveAsync(presets, cancellationToken).ConfigureAwait(false);
    }

    private FileQuickExpenseWidgetPresetStore CreateStore(string directory) =>
        new(directory, _fileLogger);
}
