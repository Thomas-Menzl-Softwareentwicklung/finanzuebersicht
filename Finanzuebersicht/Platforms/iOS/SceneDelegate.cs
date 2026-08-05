using Foundation;
using Microsoft.Maui;
using UIKit;

namespace Finanzuebersicht;

/// <summary>
/// Required on iOS 27+ when linked against the iOS 27 SDK (UIScene lifecycle mandate).
/// See https://developer.apple.com/documentation/technotes/tn3187-migrating-to-the-uikit-scene-based-life-cycle
/// and https://learn.microsoft.com/dotnet/maui/user-interface/controls/window#ipados-and-macos-configuration
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
