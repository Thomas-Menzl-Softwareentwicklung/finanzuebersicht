using System.Text.Json;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// JSON presets under a data directory (app data or App Group container on iOS).
/// </summary>
public sealed class FileQuickExpenseWidgetPresetStore : IQuickExpenseWidgetPresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly ILogger<FileQuickExpenseWidgetPresetStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileQuickExpenseWidgetPresetStore(
        string dataDirectory,
        ILogger<FileQuickExpenseWidgetPresetStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, AppGroupIds.QuickExpensePresetsFileName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuickExpenseWidgetPreset>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return QuickExpenseWidgetPresetDefaults.CreateSeeded();

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return QuickExpenseWidgetPresetDefaults.CreateSeeded();

            List<PresetDto>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<PresetDto>>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Corrupt quick-expense presets at {Path}; using defaults", _filePath);
                return QuickExpenseWidgetPresetDefaults.CreateSeeded();
            }

            if (items is null || items.Count == 0)
                return QuickExpenseWidgetPresetDefaults.CreateSeeded();

            return QuickExpenseWidgetPresetDefaults.Normalize(
                items.Select(i => new QuickExpenseWidgetPreset(
                    i.Slot,
                    i.Title ?? string.Empty,
                    i.AmountText ?? string.Empty)));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        IReadOnlyList<QuickExpenseWidgetPreset> presets,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presets);

        var normalized = QuickExpenseWidgetPresetDefaults.Normalize(presets);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var dtos = normalized
                .Select(p => new PresetDto
                {
                    Slot = p.Slot,
                    Title = p.Title,
                    AmountText = p.AmountText
                })
                .ToList();

            await File.WriteAllTextAsync(
                _filePath,
                JsonSerializer.Serialize(dtos, JsonOptions),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class PresetDto
    {
        public int Slot { get; set; }
        public string? Title { get; set; }
        public string? AmountText { get; set; }
    }
}
