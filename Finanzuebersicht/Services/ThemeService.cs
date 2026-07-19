using Finanzuebersicht.Core.Constants;

namespace Finanzuebersicht.Services;

/// <summary>
/// Verwaltet das App-Theme (Light/Dark/System) und wendet es auf MAUI und UIKit an.
/// </summary>
public class ThemeService : IThemeService
{
    public void Apply(string themeKey)
    {
        if (global::Microsoft.Maui.Controls.Application.Current is null) return;

        global::Microsoft.Maui.Controls.Application.Current.UserAppTheme = themeKey switch
        {
            ThemeValues.Light => AppTheme.Light,
            ThemeValues.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

#if MACCATALYST || IOS
        // Sync UIKit interface style so native controls (nav bar, etc.) follow the theme
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var style = themeKey switch
            {
                ThemeValues.Light => UIKit.UIUserInterfaceStyle.Light,
                ThemeValues.Dark => UIKit.UIUserInterfaceStyle.Dark,
                _ => UIKit.UIUserInterfaceStyle.Unspecified
            };

            foreach (var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is UIKit.UIWindowScene windowScene)
                {
                    foreach (var window in windowScene.Windows)
                    {
                        window.OverrideUserInterfaceStyle = style;
                    }
                }
            }
        });
#endif
    }
}
