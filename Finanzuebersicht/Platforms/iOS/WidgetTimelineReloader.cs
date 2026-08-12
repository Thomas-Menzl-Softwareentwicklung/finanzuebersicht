#if IOS
using System.Runtime.InteropServices;
using Finanzuebersicht.Services;

namespace Finanzuebersicht.Platforms.iOS;

/// <summary>
/// Asks WidgetKit to rebuild timelines after App Group state changes.
/// WidgetCenter is Swift-only — calls go through <c>WidgetKitBridge.swift</c> (@_cdecl).
/// </summary>
public static class WidgetTimelineReloader
{
    [DllImport("__Internal", EntryPoint = "finanzuebersicht_reload_all_widgets")]
    private static extern void NativeReloadAllWidgets();

    public static void ReloadAll()
    {
        try
        {
            if (MainThread.IsMainThread)
                NativeReloadAllWidgets();
            else
                MainThread.BeginInvokeOnMainThread(NativeReloadAllWidgets);
        }
        catch (Exception ex)
        {
            CrashLog.Write("WidgetTimelineReloader.ReloadAll failed", ex);
        }
    }
}
#endif
