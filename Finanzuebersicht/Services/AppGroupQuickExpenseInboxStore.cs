#if IOS || MACCATALYST
using System.Text.Json;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Services;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Services;

/// <summary>Reads pending quick expenses from the App Group container (written by WidgetKit).</summary>
public sealed class AppGroupQuickExpenseInboxStore : IQuickExpenseInboxStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<AppGroupQuickExpenseInboxStore>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppGroupQuickExpenseInboxStore(ILogger<AppGroupQuickExpenseInboxStore>? logger = null)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuickExpenseInboxItem>> DrainPendingAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var path = ResolvePendingPath();
            if (path is null || !File.Exists(path))
                return [];

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                string json;
                try
                {
                    json = File.ReadAllText(path);
                    File.WriteAllText(path, "[]");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to read/clear App Group quick-expense inbox");
                    return (IReadOnlyList<QuickExpenseInboxItem>)[];
                }

                if (string.IsNullOrWhiteSpace(json))
                    return [];

                List<InboxDto>? items;
                try
                {
                    items = JsonSerializer.Deserialize<List<InboxDto>>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger?.LogWarning(ex, "Corrupt App Group quick-expense inbox");
                    return [];
                }

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
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Publishes Pro entitlement for the widget Intent to check.</summary>
    public static void PublishHasPro(bool hasPro)
    {
        using var defaults = new NSUserDefaults(AppGroupIds.Finanzuebersicht, NSUserDefaultsType.SuiteName);
        if (defaults is null)
            return;
        defaults.SetBool(hasPro, AppGroupIds.HasProFlagKey);
        defaults.Synchronize();
    }

    /// <summary>Publishes in-app language so the widget can match (empty = system).</summary>
    public static void PublishPreferredLanguage(string? languageCode)
    {
        using var defaults = new NSUserDefaults(AppGroupIds.Finanzuebersicht, NSUserDefaultsType.SuiteName);
        if (defaults is null)
            return;

        if (string.IsNullOrWhiteSpace(languageCode))
            defaults.RemoveObject(AppGroupIds.PreferredLanguageKey);
        else
            defaults.SetString(languageCode.Trim(), AppGroupIds.PreferredLanguageKey);

        defaults.Synchronize();
    }

    private static string? ResolvePendingPath()
    {
        var url = NSFileManager.DefaultManager.GetContainerUrl(AppGroupIds.Finanzuebersicht);
        if (url?.Path is null)
            return null;
        return Path.Combine(url.Path, AppGroupIds.QuickExpensePendingFileName);
    }

    private sealed class InboxDto
    {
        public string? Id { get; set; }
        public string? AmountText { get; set; }
        public string? Title { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
#endif
