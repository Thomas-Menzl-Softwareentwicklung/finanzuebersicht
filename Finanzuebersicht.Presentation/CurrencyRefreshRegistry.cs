using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Presentation;

/// <summary>
/// Keeps weak references to view models that display monetary amounts and refreshes them when the display currency changes.
/// </summary>
public static class CurrencyRefreshRegistry
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<ICurrencyRefreshViewModel>> Registered = [];

    public static void Register(ICurrencyRefreshViewModel viewModel)
    {
        lock (Gate)
        {
            Registered.RemoveAll(r => !r.TryGetTarget(out _));
            if (Registered.Any(r => r.TryGetTarget(out var existing) && ReferenceEquals(existing, viewModel)))
                return;

            Registered.Add(new WeakReference<ICurrencyRefreshViewModel>(viewModel));
        }
    }

    public static void RefreshAll()
    {
        List<WeakReference<ICurrencyRefreshViewModel>> snapshot;
        lock (Gate)
        {
            Registered.RemoveAll(r => !r.TryGetTarget(out _));
            snapshot = Registered.ToList();
        }
        // Callbacks may register other view models; never invoke them under the registry lock.
        foreach (var reference in snapshot)
        {
            if (reference.TryGetTarget(out var viewModel))
                viewModel.RefreshCurrencyDisplay();
        }
    }
}
