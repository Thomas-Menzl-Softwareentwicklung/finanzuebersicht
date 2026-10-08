namespace Finanzuebersicht.Helpers;

/// <summary>
/// Bridge from the native Mac Catalyst menu bar (<c>AppDelegate.BuildMenu</c>) to
/// the currently visible page's commands. MAUI <see cref="MenuBarItem"/> is unreliable
/// under Shell on Mac (duplicate Ablage/File, missing Aktionen).
/// </summary>
public static class DesktopMenuBridge
{
    public sealed record Item(string Id, string Title, string? Key, Action Execute);

    static readonly object Gate = new();
    static Item[] _actions = [];

    public static Action? OpenSettingsHandler { get; set; }

    public static IReadOnlyList<Item> Actions
    {
        get
        {
            lock (Gate)
                return _actions;
        }
    }

    public static void SetActions(IEnumerable<Item> items)
    {
        lock (Gate)
            _actions = items.ToArray();

#if MACCATALYST
        // Defer rebuild so BuildMenu observes the updated snapshot after navigation.
        MainThread.BeginInvokeOnMainThread(Platforms.MacCatalyst.MacMenuBar.RequestRebuild);
#endif
    }

    public static void ClearActions() => SetActions([]);

    public static void Invoke(string id)
    {
        Item? match;
        lock (Gate)
            match = Array.Find(_actions, a => a.Id == id);

        MainThread.BeginInvokeOnMainThread(() => match?.Execute());
    }

    public static void InvokeByKey(string key)
    {
        var normalized = key.ToLowerInvariant();
        Item? match;
        lock (Gate)
        {
            match = Array.Find(
                _actions,
                a => string.Equals(a.Key, normalized, StringComparison.OrdinalIgnoreCase));
        }

        MainThread.BeginInvokeOnMainThread(() => match?.Execute());
    }

    public static void OpenSettings() =>
        MainThread.BeginInvokeOnMainThread(() => OpenSettingsHandler?.Invoke());
}
