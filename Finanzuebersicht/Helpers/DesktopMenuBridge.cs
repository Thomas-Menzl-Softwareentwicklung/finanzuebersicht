namespace Finanzuebersicht.Helpers;

/// <summary>
/// Bridge from the native Mac Catalyst menu bar (<c>AppDelegate.BuildMenu</c>) to
/// the currently visible page's commands. MAUI <see cref="MenuBarItem"/> is unreliable
/// under Shell on Mac (duplicate Ablage/File, missing Aktionen).
/// </summary>
public static class DesktopMenuBridge
{
    public sealed record Item(string Id, string Title, string? Key, Action Execute);

    public sealed record GoToItem(string Id, string Title, string Key, string Route);

    static readonly object Gate = new();
    static Item[] _actions = [];

    public static Action? OpenSettingsHandler { get; set; }

    public static Action? FocusSearchHandler { get; set; }

    public static Func<Task>? DismissModalAsync { get; set; }

    public static Func<string, Task>? GoToRouteAsync { get; set; }

    /// <summary>Fixed "Gehe zu" destinations (⌘1–⌘5); TabBar stays visible for discoverability.</summary>
    public static IReadOnlyList<GoToItem> GoToItems { get; set; } = [];

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

    public static void FocusSearch() =>
        MainThread.BeginInvokeOnMainThread(() => FocusSearchHandler?.Invoke());

    public static void DismissModal() =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var dismiss = DismissModalAsync;
            if (dismiss is not null)
                _ = dismiss();
        });

    public static void GoTo(string route) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var go = GoToRouteAsync;
            if (go is not null)
                _ = go(route);
        });
}
