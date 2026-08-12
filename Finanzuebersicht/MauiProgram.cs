using System.Reflection;
using CommunityToolkit.Maui;
using Finanzuebersicht.Application.DependencyInjection;
using Finanzuebersicht.Infrastructure;
using Finanzuebersicht.Presentation.DependencyInjection;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Finanzuebersicht.Views;

using Microsoft.Extensions.Logging;

#if MACCATALYST || IOS
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
#endif

namespace Finanzuebersicht;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if MACCATALYST || IOS
		// FastShellRenderer removed: with UIScene (iOS 27 / Mac Catalyst SDK) custom Shell
		// renderers aborted when switching to Verwaltung. Use stock Shell handlers only.
		builder.ConfigureMauiHandlers(handlers =>
		{
			// Mac Catalyst uses the iOS picker: scrolling fires SelectedItem/Date immediately and
			// can freeze the UI when pickers live inside a ScrollView. Only commit on Done.
			Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("WhenFinishedSelection", (handler, view) =>
			{
				if (view is Microsoft.Maui.Controls.Picker picker)
					picker.On<iOS>().SetUpdateMode(UpdateMode.WhenFinished);
			});
			Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("WhenFinishedSelection", (handler, view) =>
			{
				if (view is Microsoft.Maui.Controls.DatePicker datePicker)
					Finanzuebersicht.Controls.DatePickerProperties.ApplyUpdateMode(datePicker);
			});
			Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("WhenFinishedSelection", (handler, view) =>
			{
				if (view is Microsoft.Maui.Controls.TimePicker timePicker)
					timePicker.On<iOS>().SetUpdateMode(UpdateMode.WhenFinished);
			});
		});
#endif

		// Services
		// Clock for testable current time
		builder.Services.AddSingleton<Finanzuebersicht.Core.Services.IClock, Finanzuebersicht.Core.Services.SystemClock>();
#if APP_DISTRIBUTION_STORE
		builder.Services.AddSingleton<Finanzuebersicht.Core.Licensing.IDistributionChannelProvider>(
			_ => new Finanzuebersicht.Core.Licensing.FixedDistributionChannelProvider(
				Finanzuebersicht.Core.Licensing.DistributionChannel.Store));
#if IOS || MACCATALYST
		builder.Services.AddSingleton<Finanzuebersicht.Core.Licensing.IStoreBillingService, Finanzuebersicht.Services.Billing.StoreKitBillingService>();
#else
		builder.Services.AddSingleton<Finanzuebersicht.Core.Licensing.IStoreBillingService, Finanzuebersicht.Core.Licensing.UnavailableStoreBillingService>();
#endif
#else
		// Default: Direct (GitHub / self-built Windows & Mac) — full local Pro, no Cloud Sync / StoreKit
		builder.Services.AddSingleton<Finanzuebersicht.Core.Licensing.IDistributionChannelProvider>(
			_ => new Finanzuebersicht.Core.Licensing.FixedDistributionChannelProvider(
				Finanzuebersicht.Core.Licensing.DistributionChannel.Direct));
		builder.Services.AddSingleton<Finanzuebersicht.Core.Licensing.IStoreBillingService, Finanzuebersicht.Core.Licensing.UnavailableStoreBillingService>();
#endif
		builder.Services.AddInfrastructureServices();
		// App Group inbox/presets are iPhone-only (widget). Mac Catalyst has no App Group entitlement —
		// keep File* stores from Infrastructure.
#if IOS && !MACCATALYST
		builder.Services.AddSingleton<Finanzuebersicht.Core.Services.IQuickExpenseInboxStore, AppGroupQuickExpenseInboxStore>();
		// Resolve App Group path lazily on each Load/Save (not at DI build — often null that early).
		builder.Services.AddSingleton<Finanzuebersicht.Core.Services.IQuickExpenseWidgetPresetStore>(sp =>
			new Finanzuebersicht.Infrastructure.Services.MirroredQuickExpenseWidgetPresetStore(
				Finanzuebersicht.Infrastructure.Services.DataPathResolver.ResolveDataDir(
					sp.GetRequiredService<Finanzuebersicht.Core.Services.ISettingsService>()),
				AppGroupQuickExpenseInboxStore.TryGetContainerPath,
				sp.GetService<Microsoft.Extensions.Logging.ILogger<Finanzuebersicht.Infrastructure.Services.MirroredQuickExpenseWidgetPresetStore>>(),
				sp.GetService<Microsoft.Extensions.Logging.ILogger<Finanzuebersicht.Infrastructure.Services.FileQuickExpenseWidgetPresetStore>>()));
#endif
		builder.Services.AddSingleton<IRecurringGenerationService, RecurringGenerationService>();
		builder.Services.AddSingleton<IReportingService, ReportingService>();
		builder.Services.AddSingleton<IForecastService, ForecastService>();
		builder.Services.AddSingleton<ITransactionValidationService, TransactionValidationService>();

		builder.Services.AddApplicationUseCases();

		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton<IDialogService, ShellDialogService>();
		builder.Services.AddSingleton<IFeedbackService, MauiFeedbackService>();
		builder.Services.AddSingleton<IOnboardingCoordinator, OnboardingCoordinator>();
		builder.Services.AddSingleton<IMainThreadDispatcher, MauiMainThreadDispatcher>();
		builder.Services.AddSingleton<Finanzuebersicht.Presentation.Services.IFilePicker, MauiFilePicker>();
		builder.Services.AddSingleton<IAppEvents, AppEvents>();
		builder.Services.AddSingleton<IWidgetTimelineReloader, MauiWidgetTimelineReloader>();
		builder.Services.AddSingleton<ICategoryCreateSheetService, CategoryCreateSheetService>();
		builder.Services.AddSingleton<IRecurringTransactionCreateSheetService, RecurringTransactionCreateSheetService>();
		builder.Services.AddSingleton<IQuickExpenseCaptureSheetService, QuickExpenseCaptureSheetService>();
		builder.Services.AddSingleton<IImportSessionStore, ImportSessionStore>();
		builder.Services.AddSingleton<IFolderPicker, MauiFolderPicker>();
		builder.Services.AddSingleton<IFileSaver, MauiFileSaver>();
		builder.Services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());

		builder.Services.AddPresentationViewModels(Assembly.GetExecutingAssembly());

		// Pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<TransactionsPage>();
		builder.Services.AddTransient<TransactionDetailPage>();
		builder.Services.AddTransient<TransferDetailPage>();
		builder.Services.AddTransient<RecurringTransactionsPage>();
		builder.Services.AddTransient<RecurringTransactionDetailPage>();
        builder.Services.AddTransient<RecurringInstanceShiftPage>();
		builder.Services.AddTransient<CategoriesPage>(sp =>
		{
			try
			{
				var vm = sp.GetRequiredService<CategoriesViewModel>();
				var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<CategoriesPage>>();
				return new CategoriesPage(vm, logger);
			}
			catch (Exception ex)
			{
				CrashLog.Write("CategoriesPage DI/create failed", ex);
				throw;
			}
		});
		builder.Services.AddTransient<CategoryDetailPage>();
		builder.Services.AddTransient<AccountDetailPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SparZielePage>();
		builder.Services.AddTransient<SparZielDetailPage>();
		builder.Services.AddTransient<BackupListPage>();
		builder.Services.AddTransient<ImportPreviewPage>();
		builder.Services.AddTransient<CashflowPage>();
		builder.Services.AddTransient<OnboardingPage>();
		builder.Services.AddTransient<QuickExpenseCapturePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		return app;
	}
}
