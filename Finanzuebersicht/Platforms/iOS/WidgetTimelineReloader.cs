#if IOS
using WidgetKit;

namespace Finanzuebersicht.Platforms.iOS;

/// <summary>Asks WidgetKit to rebuild timelines after App Group state changes (language, Pro).</summary>
public static class WidgetTimelineReloader
{
    public static void ReloadAll()
    {
        try
        {
            WidgetCenter.Shared.ReloadAllTimelines();
        }
        catch
        {
            // WidgetKit may be unavailable on some builds; ignore.
        }
    }
}
#endif
