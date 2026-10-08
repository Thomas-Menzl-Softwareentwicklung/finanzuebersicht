using System.Windows.Input;
using CoreDesktop = Finanzuebersicht.Core.Platform.DesktopChrome;

namespace Finanzuebersicht.Helpers;

/// <summary>
/// MAUI chrome helpers on top of <see cref="CoreDesktop"/>.
/// </summary>
public static class DesktopChrome
{
    public const double ContentMaxWidth = CoreDesktop.ContentMaxWidth;

    public static bool IsDesktop => CoreDesktop.IsDesktop;

    public static bool IsPhone => CoreDesktop.IsPhone;

    public static readonly BindableProperty SuppressSwipeOnDesktopProperty =
        BindableProperty.CreateAttached(
            "SuppressSwipeOnDesktop",
            typeof(bool),
            typeof(DesktopChrome),
            false,
            propertyChanged: OnSuppressSwipeChanged);

    public static bool GetSuppressSwipeOnDesktop(BindableObject view) =>
        (bool)view.GetValue(SuppressSwipeOnDesktopProperty);

    public static void SetSuppressSwipeOnDesktop(BindableObject view, bool value) =>
        view.SetValue(SuppressSwipeOnDesktopProperty, value);

    static void OnSuppressSwipeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not SwipeView swipe || newValue is not true || !IsDesktop)
            return;

        void Clear()
        {
            swipe.LeftItems?.Clear();
            swipe.RightItems?.Clear();
        }

        Clear();
        swipe.Loaded += (_, _) => Clear();
        swipe.HandlerChanged += (_, _) =>
        {
            if (swipe.Handler is not null)
                Clear();
        };
    }

    /// <summary>Adds toolbar items only on desktop; no-op on phone.</summary>
    public static void AddToolbarItems(Page page, params ToolbarItem[] items)
    {
        if (!IsDesktop || items.Length == 0)
            return;

        foreach (var item in items)
            page.ToolbarItems.Add(item);
    }

    public static ToolbarItem CreateToolbarItem(string text, ICommand? command, int priority = 0) =>
        new()
        {
            Text = text,
            Command = command,
            Order = ToolbarItemOrder.Primary,
            Priority = priority
        };
}
