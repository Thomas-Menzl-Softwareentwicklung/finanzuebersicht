using Finanzuebersicht.Helpers;

namespace Finanzuebersicht.Controls;

/// <summary>
/// Centers content and caps width on Mac Catalyst; full-bleed on phone (#352).
/// </summary>
public sealed class DesktopContentHost : ContentView
{
    public DesktopContentHost()
    {
        if (!DesktopChrome.IsDesktop)
            return;

        MaximumWidthRequest = DesktopChrome.ContentMaxWidth;
        HorizontalOptions = LayoutOptions.Center;
    }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);
        if (!DesktopChrome.IsDesktop || child is not View view)
            return;

        view.HorizontalOptions = LayoutOptions.Fill;
    }
}
