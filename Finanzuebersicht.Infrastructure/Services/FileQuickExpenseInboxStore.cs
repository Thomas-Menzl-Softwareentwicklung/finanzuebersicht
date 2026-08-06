using System.Text.Json;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// File-based inbox under the app data directory (tests / non-iOS).
/// iOS replaces this with <c>AppGroupQuickExpenseInboxStore</c> in the MAUI host.
/// </summary>
public sealed class FileQuickExpenseInboxStore : IQuickExpenseInboxStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly ILogger<FileQuickExpenseInboxStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileQuickExpenseInboxStore(string dataDirectory, ILogger<FileQuickExpenseInboxStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, AppGroupIds.QuickExpensePendingFileName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuickExpenseInboxItem>> DrainPendingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return [];

            await using var stream = File.Open(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            List<InboxDto>? items;
            try
            {
                items = await JsonSerializer.DeserializeAsync<List<InboxDto>>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Corrupt quick-expense inbox at {Path}; clearing", _filePath);
                stream.SetLength(0);
                return [];
            }

            stream.SetLength(0);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            if (items is null || items.Count == 0)
                return [];

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.AmountText) && !string.IsNullOrWhiteSpace(i.Title))
                .Select(i => new QuickExpenseInboxItem(
                    string.IsNullOrWhiteSpace(i.Id) ? Guid.NewGuid().ToString() : i.Id!,
                    i.AmountText!.Trim(),
                    i.Title!.Trim(),
                    i.CreatedAt == default ? DateTimeOffset.UtcNow : i.CreatedAt))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Test/helper: enqueue an item (widget writes this file on iOS).</summary>
    public async Task EnqueueAsync(QuickExpenseInboxItem item, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<InboxDto> items = [];
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(json))
                    items = JsonSerializer.Deserialize<List<InboxDto>>(json, JsonOptions) ?? [];
            }

            items.Add(new InboxDto
            {
                Id = item.Id,
                AmountText = item.AmountText,
                Title = item.Title,
                CreatedAt = item.CreatedAt
            });

            await File.WriteAllTextAsync(
                _filePath,
                JsonSerializer.Serialize(items, JsonOptions),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed class InboxDto
    {
        public string? Id { get; set; }
        public string? AmountText { get; set; }
        public string? Title { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
