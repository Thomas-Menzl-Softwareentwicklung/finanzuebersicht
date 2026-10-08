using System.Windows.Input;
using CoreDesktop = Finanzuebersicht.Core.Platform.DesktopChrome;

namespace Finanzuebersicht.Helpers;

/// <summary>
/// MAUI chrome helpers on top of <see cref="CoreDesktop"/>.
/// </summary>
public static class DesktopChrome
{
    public const double ContentMaxWidth = CoreDesktop.ContentMaxWidth;

    /// <summary>Desktop row action glyph size — between row title (~15pt) and category tile (~36).</summary>
    public const double RowActionIconSize = 18;

    /// <summary>List rows open detail on double-click on Mac; single tap on phone.</summary>
    public static int DetailOpenTapCount => CoreDesktop.DetailOpenTapCount;

    public static bool IsDesktop => CoreDesktop.IsDesktop;

    public static bool IsPhone => CoreDesktop.IsPhone;

    public static readonly BindableProperty SuppressSwipeOnDesktopProperty =
        BindableProperty.CreateAttached(
            "SuppressSwipeOnDesktop",
            typeof(bool),
            typeof(DesktopChrome),
            false,
            propertyChanged: OnSuppressSwipeChanged);

    public static readonly BindableProperty EnableRowHoverProperty =
        BindableProperty.CreateAttached(
            "EnableRowHover",
            typeof(bool),
            typeof(DesktopChrome),
            false,
            propertyChanged: OnEnableRowHoverChanged);

    public static readonly BindableProperty RegisteredActionsProperty =
        BindableProperty.CreateAttached(
            "RegisteredActions",
            typeof(DesktopAction[]),
            typeof(DesktopChrome),
            null);

    public static readonly BindableProperty ActionsMenuTitleProperty =
        BindableProperty.CreateAttached(
            "ActionsMenuTitle",
            typeof(string),
            typeof(DesktopChrome),
            null);

    static readonly BindableProperty AttachedToolbarItemsProperty =
        BindableProperty.CreateAttached(
            "AttachedToolbarItems",
            typeof(ToolbarItem[]),
            typeof(DesktopChrome),
            null);

    static readonly BindableProperty ToolbarBuilderProperty =
        BindableProperty.CreateAttached(
            "ToolbarBuilder",
            typeof(Action),
            typeof(DesktopChrome),
            null);

    public static bool GetSuppressSwipeOnDesktop(BindableObject view) =>
        (bool)view.GetValue(SuppressSwipeOnDesktopProperty);

    public static void SetSuppressSwipeOnDesktop(BindableObject view, bool value) =>
        view.SetValue(SuppressSwipeOnDesktopProperty, value);

    public static bool GetEnableRowHover(BindableObject view) =>
        (bool)view.GetValue(EnableRowHoverProperty);

    public static void SetEnableRowHover(BindableObject view, bool value) =>
        view.SetValue(EnableRowHoverProperty, value);

    public static DesktopAction[]? GetRegisteredActions(BindableObject view) =>
        (DesktopAction[]?)view.GetValue(RegisteredActionsProperty);

    public static void SetRegisteredActions(BindableObject view, DesktopAction[]? value) =>
        view.SetValue(RegisteredActionsProperty, value);

    public static string? GetActionsMenuTitle(BindableObject view) =>
        (string?)view.GetValue(ActionsMenuTitleProperty);

    public static void SetActionsMenuTitle(BindableObject view, string? value) =>
        view.SetValue(ActionsMenuTitleProperty, value);

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

    // Distinct from PageBackground (#F2F2F7) and CardBackground / CardBackgroundDark (#FFFFFF / #2C2C2E).
    static readonly Color RowHoverLight = Color.FromArgb("#D8D8DE");
    static readonly Color RowHoverDark = Color.FromArgb("#3A3A3C");

    static void OnEnableRowHoverChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view || newValue is not true || !IsDesktop)
            return;

        // Capture even when BackgroundColor is null — otherwise exit never clears the hover tint.
        Color? original = null;
        var captured = false;
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) =>
        {
            if (!captured)
            {
                original = view.BackgroundColor;
                captured = true;
            }

            var dark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
            view.BackgroundColor = dark ? RowHoverDark : RowHoverLight;
        };
        pointer.PointerExited += (_, _) =>
        {
            if (!captured)
                return;
            view.BackgroundColor = original;
        };
        view.GestureRecognizers.Add(pointer);
    }

    /// <summary>
    /// Toolbar on the page + register actions for the Shell macOS menu
    /// (page-level MenuBarItems are unreliable under Shell on Mac Catalyst).
    /// <paramref name="build"/> is re-run on language change via <see cref="RefreshPageActions"/>.
    /// </summary>
    public static void AttachPageActions(Page page, Func<(string MenuTitle, DesktopAction[] Actions)> build)
    {
        if (!IsDesktop)
            return;

        void Apply()
        {
            var (menuTitle, actions) = build();
            if (actions.Length == 0)
                return;

            if (page.GetValue(AttachedToolbarItemsProperty) is ToolbarItem[] previous)
            {
                foreach (var item in previous)
                    page.ToolbarItems.Remove(item);
            }

            var created = new ToolbarItem[actions.Length];
            for (var i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                created[i] = CreateToolbarItem(action.Text, action.Command, priority: i);
                page.ToolbarItems.Add(created[i]);
            }

            page.SetValue(AttachedToolbarItemsProperty, created);
            SetActionsMenuTitle(page, menuTitle);
            SetRegisteredActions(page, actions);
            PublishPageActions(page);
        }

        page.SetValue(ToolbarBuilderProperty, (Action)Apply);
        Apply();
    }

    /// <summary>Re-apply toolbar/menu action titles after an in-app language change.</summary>
    public static void RefreshPageActions(Page page)
    {
        if (!IsDesktop)
            return;
        if (page.GetValue(ToolbarBuilderProperty) is Action apply)
            apply();
    }

    /// <summary>Re-publish this page's actions into the Shell menu bar (call from OnAppearing).</summary>
    public static void PublishPageActions(Page page)
    {
        if (!IsDesktop)
            return;

        var actions = GetRegisteredActions(page);
        var title = GetActionsMenuTitle(page);
        if (actions is null || actions.Length == 0 || string.IsNullOrEmpty(title))
        {
            // Settings / detail / import have no page actions — clear stale ⌘N/⌘I from the prior tab.
            DesktopMenuBridge.ClearActions();
            return;
        }

        // Always push through AppShell when available; otherwise publish directly so
        // early constructor AttachPageActions still reaches the native menu bar.
        if (Shell.Current is AppShell shell)
            shell.SyncActionsMenu(title, actions);
        else
            SyncActionsToBridge(actions);
    }

    static void SyncActionsToBridge(DesktopAction[] actions)
    {
        var items = new List<DesktopMenuBridge.Item>(actions.Length);
        for (var i = 0; i < actions.Length; i++)
        {
            var action = actions[i];
            var command = action.Command;
            items.Add(new DesktopMenuBridge.Item(
                Id: $"finanz.action.{i}",
                Title: action.Text,
                Key: action.AcceleratorKey,
                Execute: () =>
                {
                    if (command?.CanExecute(null) == true)
                        command.Execute(null);
                }));
        }

        DesktopMenuBridge.SetActions(items);
    }

    public static ToolbarItem CreateToolbarItem(string text, ICommand? command, int priority = 0) =>
        new()
        {
            Text = text,
            Command = command,
            Order = ToolbarItemOrder.Primary,
            Priority = priority
        };

    public static MenuFlyoutItem CreateMenuFlyoutItem(string text, ICommand? command, string? acceleratorKey = null)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            Command = WrapCommand(command)
        };

        if (!string.IsNullOrWhiteSpace(acceleratorKey))
        {
            item.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = acceleratorKey,
                Modifiers = KeyboardAcceleratorModifiers.Cmd
            });
        }

        return item;
    }

    /// <summary>
    /// MenuFlyoutItem on Mac can stay disabled if the source command's CanExecute
    /// is not observed; wrap so the menu item stays usable when the command allows it.
    /// </summary>
    public static ICommand? WrapCommand(ICommand? inner)
    {
        if (inner is null)
            return null;

        var wrapped = new Command(
            execute: () =>
            {
                if (inner.CanExecute(null))
                    inner.Execute(null);
            },
            canExecute: () => inner.CanExecute(null));

        inner.CanExecuteChanged += (_, _) => ((Command)wrapped).ChangeCanExecute();
        return wrapped;
    }

    public readonly record struct DesktopAction(string Text, ICommand? Command, string? AcceleratorKey = null);
}
