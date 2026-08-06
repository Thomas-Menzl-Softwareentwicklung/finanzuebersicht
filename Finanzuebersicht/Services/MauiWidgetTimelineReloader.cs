#if IOS
using Finanzuebersicht.Platforms.iOS;
#endif
using Finanzuebersicht.Presentation.Services;

namespace Finanzuebersicht.Services;

public sealed class MauiWidgetTimelineReloader : IWidgetTimelineReloader
{
    public void ReloadAll()
    {
#if IOS
        WidgetTimelineReloader.ReloadAll();
#endif
    }
}
