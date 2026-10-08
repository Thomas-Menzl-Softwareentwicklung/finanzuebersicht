using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Finanzuebersicht;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	static readonly NSString ActionsMenuId = new("finanz.aktionen");
	static readonly NSString GoToMenuId = new("finanz.gehezu");

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
		builder.RemoveMenu(GoToMenuId);

		var loc = LocalizationResourceManager.Current;

		BuildGoToMenu(builder, loc[ResourceKeys.Menu_GeheZu]);
		BuildActionsMenu(builder);
		BuildSearchKeyCommand(builder);
		BuildEscapeCommand(builder);
		BuildSettingsCommand(builder);
	}

	static void BuildGoToMenu(IUIMenuBuilder builder, string title)
	{
		var goTo = DesktopMenuBridge.GoToItems;
		if (goTo.Count == 0)
			return;

		var elements = new List<UIMenuElement>(goTo.Count);
		foreach (var item in goTo)
		{
			var route = item.Route;
			elements.Add(UIKeyCommand.Create(
				item.Title,
				null,
				new Selector("onDesktopGoTo:"),
				item.Key,
				UIKeyModifierFlags.Command,
				(NSString)route));
		}

		// Arg 3 is UIMenuIdentifier on MacCatalyst 26.0 (CI) and NSString on 26.2 — same ObjC type.
		var menu = UIMenu.Create(title, null, (dynamic)GoToMenuId, default(UIMenuOptions), elements.ToArray());
		if (UIMenuIdentifier.View.GetConstant() is { } viewId)
			builder.InsertSiblingMenuAfter(menu, viewId);
		else if (UIMenuIdentifier.File.GetConstant() is { } fileId)
			builder.InsertSiblingMenuAfter(menu, fileId);
	}

	static void BuildActionsMenu(IUIMenuBuilder builder)
	{
		var loc = LocalizationResourceManager.Current;
		var snapshot = DesktopMenuBridge.Actions;
		var actionElements = new List<UIMenuElement>(snapshot.Count + 1);

		// Visible label without ⌘F — a UIKeyCommand for "f" inside this menu makes Catalyst
		// drop the whole Aktionen group (system Edit›Find owns ⌘F).
		actionElements.Add(UIAction.Create(
			loc[ResourceKeys.Menu_Suchen],
			null,
			"finanz.search",
			_ => DesktopMenuBridge.FocusSearch()));

		foreach (var item in snapshot)
		{
			var id = item.Id;
			if (!string.IsNullOrWhiteSpace(item.Key))
			{
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

		var actionsMenu = UIMenu.Create(
			loc[ResourceKeys.Menu_Aktionen],
			null,
			(dynamic)ActionsMenuId,
			default(UIMenuOptions),
			actionElements.ToArray());

		if (UIMenuIdentifier.Help.GetConstant() is { } helpId)
			builder.InsertSiblingMenuBefore(actionsMenu, helpId);
		else if (UIMenuIdentifier.Window.GetConstant() is { } windowId)
			builder.InsertSiblingMenuBefore(actionsMenu, windowId);
	}

	static void BuildSearchKeyCommand(IUIMenuBuilder builder)
	{
		// Drop system Find so ⌘F is free (leaving it in place swallows our handler).
		builder.RemoveMenu((NSString)"Find");
		builder.RemoveMenu((NSString)"com.apple.menu.find");

		var title = LocalizationResourceManager.Current[ResourceKeys.Menu_Suchen];
		var search = UIKeyCommand.Create(
			title,
			null,
			new Selector("onDesktopSearch:"),
			"f",
			UIKeyModifierFlags.Command,
			null);
		search.WantsPriorityOverSystemBehavior = true;

		var menu = UIMenu.Create(
			string.Empty,
			null,
			UIMenuIdentifier.None,
			UIMenuOptions.DisplayInline,
			[search]);
		if (UIMenuIdentifier.Edit.GetConstant() is { } editId)
			builder.InsertChildMenuAtStart(menu, editId);
	}

	static void BuildEscapeCommand(IUIMenuBuilder builder)
	{
		// Escape dismisses the create FormSheet; keep it out of the visible menu.
		// Without priority, a focused Entry/UITextField eats Esc (resign first responder → beep).
		var escape = UIKeyCommand.Create(
			(NSString)UIKeyCommand.Escape,
			default,
			new Selector("onDesktopEscape:"));
		escape.WantsPriorityOverSystemBehavior = true;
		var menu = UIMenu.Create(
			string.Empty,
			null,
			UIMenuIdentifier.None,
			UIMenuOptions.DisplayInline,
			[escape]);
		if (UIMenuIdentifier.File.GetConstant() is { } fileId)
			builder.InsertChildMenuAtStart(menu, fileId);
	}

	static void BuildSettingsCommand(IUIMenuBuilder builder)
	{
		var settingsTitle = LocalizationResourceManager.Current[ResourceKeys.Nav_Einstellungen] + "\u2026";
		var settingsCommand = UIKeyCommand.Create(
			settingsTitle,
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

	[Export("onDesktopGoTo:")]
	void OnDesktopGoTo(UIKeyCommand command)
	{
		if (command.PropertyList is NSString route && !string.IsNullOrEmpty(route))
			DesktopMenuBridge.GoTo(route);
	}

	[Export("onDesktopSearch:")]
	void OnDesktopSearch(UIKeyCommand command) => DesktopMenuBridge.FocusSearch();

	[Export("onDesktopEscape:")]
	void OnDesktopEscape(UIKeyCommand command) => DesktopMenuBridge.DismissModal();

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
