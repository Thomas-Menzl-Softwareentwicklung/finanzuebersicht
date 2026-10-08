using Finanzuebersicht.Helpers;

namespace Finanzuebersicht.Controls;

/// <summary>
/// On Mac Catalyst: full-width content with comfortable side padding (no phone-column cap).
/// On phone: passthrough (#352).
/// </summary>
public sealed class DesktopContentHost : ContentView
{
    /// <summary>Horizontal inset so lists/cards aren't flush against window edges.</summary>
    public const double DesktopSidePadding = 28;

    public DesktopContentHost()
    {
        if (!DesktopChrome.IsDesktop)
            return;

        HorizontalOptions = LayoutOptions.Fill;
        Padding = new Thickness(DesktopSidePadding, 0);
    }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);
        if (!DesktopChrome.IsDesktop || child is not View view)
            return;

        view.HorizontalOptions = LayoutOptions.Fill;
    }
}
