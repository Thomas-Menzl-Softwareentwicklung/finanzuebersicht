using Finanzuebersicht.Helpers;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.Views;

namespace Finanzuebersicht;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(Routes.TransactionDetail, typeof(TransactionDetailPage));
		Routing.RegisterRoute(Routes.TransferDetail, typeof(TransferDetailPage));
		Routing.RegisterRoute(Routes.RecurringTransactionDetail, typeof(RecurringTransactionDetailPage));
		Routing.RegisterRoute(Routes.CategoryDetail, typeof(CategoryDetailPage));
		Routing.RegisterRoute(Routes.AccountDetail, typeof(AccountDetailPage));
		Routing.RegisterRoute(Routes.RecurringInstanceShift, typeof(RecurringInstanceShiftPage));
		Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
		Routing.RegisterRoute(Routes.BackupList, typeof(BackupListPage));
		Routing.RegisterRoute(Routes.ImportPreview, typeof(ImportPreviewPage));
		Routing.RegisterRoute(Routes.ImportMapping, typeof(ImportMappingPage));
		Routing.RegisterRoute(Routes.Cashflow, typeof(CashflowPage));
		Routing.RegisterRoute(Routes.SparZielDetail, typeof(SparZielDetailPage));
		Routing.RegisterRoute(Routes.Onboarding, typeof(OnboardingPage));
		Routing.RegisterRoute(Routes.QuickExpenseCapture, typeof(QuickExpenseCapturePage));

		if (DesktopChrome.IsDesktop)
		{
			DesktopMenuBridge.OpenSettingsHandler = () => _ = OpenSettingsAsync();
			Navigated += OnShellNavigated;
		}
	}

	void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
	{
		if (CurrentPage is Page page)
			DesktopChrome.PublishPageActions(page);
	}

	/// <summary>Push the visible page's toolbar actions into the native macOS menu bar.</summary>
	public void SyncActionsMenu(string title, DesktopChrome.DesktopAction[] actions)
	{
		if (!DesktopChrome.IsDesktop)
			return;

		var items = new List<DesktopMenuBridge.Item>(actions.Length);
		for (var i = 0; i < actions.Length; i++)
		{
			var action = actions[i];
			var command = action.Command;
			items.Add(new DesktopMenuBridge.Item(
				Id: $"finanz.action.{i}",
				Title: action.Text,
				Key: action.AcceleratorKey,
				Execute: () =>
				{
					if (command?.CanExecute(null) == true)
						command.Execute(null);
				}));
		}

		DesktopMenuBridge.SetActions(items);
	}

	private async void OnSettingsClicked(object? sender, EventArgs e) =>
		await OpenSettingsAsync();

	static async Task OpenSettingsAsync()
	{
		var location = Shell.Current.CurrentState.Location.ToString();
		if (location.EndsWith(Routes.Settings))
			return;
		await Shell.Current.GoToAsync(Routes.Settings);
	}
}
