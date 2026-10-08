using Finanzuebersicht.Helpers;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Finanzuebersicht;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	static readonly NSString ActionsMenuId = new("finanz.aktionen");

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	// Mac Catalyst: Fenster schließen beendet die App nicht (sonst wirkt sie „einfach weg“)
	[Export("applicationShouldTerminateAfterLastWindowClosed:")]
	public bool ApplicationShouldTerminateAfterLastWindowClosed(UIApplication application) => false;

	/// <summary>
	/// Native system menu bar. MAUI MenuBarItems under Shell duplicate Ablage/File and
	/// often never show "Aktionen"; we build the real menu here instead.
	/// </summary>
	public override void BuildMenu(IUIMenuBuilder builder)
	{
		base.BuildMenu(builder);

		// Drop unused / confusing Catalyst document chrome (grayed Duplicate/Move/Rename…).
		TryRemove(builder, UIMenuIdentifier.Format);
		TryRemove(builder, UIMenuIdentifier.Font);
		RemoveDocumentMenu(builder);
		builder.RemoveMenu(ActionsMenuId);

		var snapshot = DesktopMenuBridge.Actions;
		var actionElements = new List<UIMenuElement>(snapshot.Count);
		foreach (var item in snapshot)
		{
			var id = item.Id;
			if (!string.IsNullOrWhiteSpace(item.Key))
			{
				// Title+input overload → visible label AND ⌘shortcut in the menu bar.
				// (discoverabilityTitle-only Create left titles empty and Catalyst dropped the menu.)
				actionElements.Add(UIKeyCommand.Create(
					item.Title,
					null,
					new Selector("onDesktopMenuKey:"),
					item.Key.ToLowerInvariant(),
					UIKeyModifierFlags.Command,
					(NSString)id));
			}
			else
			{
				actionElements.Add(UIAction.Create(item.Title, null, id, _ => DesktopMenuBridge.Invoke(id)));
			}
		}

		if (actionElements.Count > 0)
		{
			var actionsMenu = UIMenu.Create(
				"Aktionen",
				null,
				ActionsMenuId,
				default,
				actionElements.ToArray());

			// Insert before Help — after View / UIMenuIdentifier.None often no-ops on Catalyst.
			if (UIMenuIdentifier.Help.GetConstant() is { } helpId)
				builder.InsertSiblingMenuBefore(actionsMenu, helpId);
			else if (UIMenuIdentifier.Window.GetConstant() is { } windowId)
				builder.InsertSiblingMenuBefore(actionsMenu, windowId);
		}

		var settingsCommand = UIKeyCommand.Create(
			"Einstellungen…",
			null,
			new Selector("onDesktopSettings:"),
			",",
			UIKeyModifierFlags.Command,
			null);
		var settingsMenu = UIMenu.Create(
			string.Empty,
			null,
			UIMenuIdentifier.None,
			UIMenuOptions.DisplayInline,
			[settingsCommand]);
		if (UIMenuIdentifier.About.GetConstant() is { } aboutId)
			builder.InsertSiblingMenuAfter(settingsMenu, aboutId);
	}

	[Export("onDesktopMenuKey:")]
	void OnDesktopMenuKey(UIKeyCommand command)
	{
		if (command.PropertyList is NSString id && !string.IsNullOrEmpty(id))
		{
			DesktopMenuBridge.Invoke(id);
			return;
		}

		var key = command.Input;
		if (!string.IsNullOrEmpty(key))
			DesktopMenuBridge.InvokeByKey(key);
	}

	[Export("onDesktopSettings:")]
	void OnDesktopSettings(UIKeyCommand command) => DesktopMenuBridge.OpenSettings();

	static void TryRemove(IUIMenuBuilder builder, UIMenuIdentifier identifier)
	{
		if (identifier.GetConstant() is { } constant)
			builder.RemoveMenu(constant);
	}

	static void RemoveDocumentMenu(IUIMenuBuilder builder)
	{
#pragma warning disable CA1416 // Document identifier is Mac Catalyst 16+; app targets 15 but ships on 16+ Macs
		TryRemove(builder, UIMenuIdentifier.Document);
#pragma warning restore CA1416
		builder.RemoveMenu((NSString)"Document");
	}
}
