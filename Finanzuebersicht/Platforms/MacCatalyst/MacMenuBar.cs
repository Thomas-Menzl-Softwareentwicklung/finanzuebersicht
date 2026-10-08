#if MACCATALYST
using UIKit;

namespace Finanzuebersicht.Platforms.MacCatalyst;

/// <summary>
/// Forces the system menu bar to rebuild after MAUI MenuBarItems change.
/// Without this, dynamically updated items often stay missing or disabled on Mac Catalyst.
/// </summary>
public static class MacMenuBar
{
    public static void RequestRebuild() =>
        UIMenuSystem.MainSystem.SetNeedsRebuild();
}
#endif
