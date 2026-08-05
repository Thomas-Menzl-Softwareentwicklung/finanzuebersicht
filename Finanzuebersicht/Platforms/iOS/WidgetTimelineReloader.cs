#if IOS
using System.Runtime.InteropServices;
using ObjCRuntime;

namespace Finanzuebersicht.Platforms.iOS;

/// <summary>
/// Asks WidgetKit to rebuild timelines after App Group state changes (language, Pro).
/// Uses ObjC messaging — the managed WidgetKit bindings are not referenced by the MAUI host.
/// </summary>
public static class WidgetTimelineReloader
{
    private const string WidgetKitLibrary = "/System/Library/Frameworks/WidgetKit.framework/WidgetKit";
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.dylib";

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    public static void ReloadAll()
    {
        try
        {
            // Ensure WidgetKit is loaded into the process.
            dlopen(WidgetKitLibrary, 1 /* RTLD_LAZY */);

            var centerClass = Class.GetHandle("WidgetCenter");
            if (centerClass == IntPtr.Zero)
                return;

            var sharedSel = Selector.GetHandle("sharedCenter");
            var reloadSel = Selector.GetHandle("reloadAllTimelines");
            if (sharedSel == IntPtr.Zero || reloadSel == IntPtr.Zero)
                return;

            var shared = IntPtr_objc_msgSend(centerClass, sharedSel);
            if (shared == IntPtr.Zero)
                return;

            void_objc_msgSend(shared, reloadSel);
        }
        catch
        {
            // WidgetKit may be unavailable; ignore.
        }
    }
}
#endif
