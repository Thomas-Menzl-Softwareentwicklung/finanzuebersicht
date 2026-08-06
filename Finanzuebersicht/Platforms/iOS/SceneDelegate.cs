using Foundation;
using Microsoft.Maui;
using UIKit;

namespace Finanzuebersicht;

/// <summary>
/// Required on iOS 27+ when linked against the iOS 27 SDK (UIScene lifecycle mandate).
/// Custom URL schemes (widget Anpassen) arrive here — not via AppDelegate.OpenUrl.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
	public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		base.WillConnect(scene, session, connectionOptions);
		ForwardUrlContexts(connectionOptions?.UrlContexts);
	}

	public override bool OpenUrl(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
	{
		_ = base.OpenUrl(scene, urlContexts);
		ForwardUrlContexts(urlContexts);
		return true;
	}

	static void ForwardUrlContexts(NSSet? urlContexts)
	{
		if (urlContexts is null || urlContexts.Count == 0)
			return;

		foreach (var item in urlContexts)
		{
			if (item is not UIOpenUrlContext ctx || ctx.Url is null)
				continue;

			var absolute = ctx.Url.AbsoluteString;
			if (string.IsNullOrWhiteSpace(absolute))
				continue;

			if (Uri.TryCreate(absolute, UriKind.Absolute, out var uri))
				App.EnqueueAppLink(uri);
		}
	}
}
