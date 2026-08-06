using Finanzuebersicht.Navigation;
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
		Routing.RegisterRoute(Routes.Cashflow, typeof(CashflowPage));
		Routing.RegisterRoute(Routes.SparZielDetail, typeof(SparZielDetailPage));
		Routing.RegisterRoute(Routes.Onboarding, typeof(OnboardingPage));
		Routing.RegisterRoute(Routes.QuickExpenseCapture, typeof(QuickExpenseCapturePage));
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		var location = Shell.Current.CurrentState.Location.ToString();
		if (location.EndsWith(Routes.Settings)) return;
		await Shell.Current.GoToAsync(Routes.Settings);
	}
}
