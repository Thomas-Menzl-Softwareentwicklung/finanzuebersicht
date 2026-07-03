using System.Reflection;
using CommunityToolkit.Maui;
using Finanzuebersicht.Application.DependencyInjection;
using Finanzuebersicht.Infrastructure;
using Finanzuebersicht.Presentation.DependencyInjection;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Services;
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
		// Faster tab switch animation via custom Shell renderer
		builder.ConfigureMauiHandlers(handlers =>
		{
			handlers.AddHandler<Shell, Finanzuebersicht.Handlers.FastShellRenderer>();

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
		builder.Services.AddInfrastructureServices();
		builder.Services.AddSingleton<IRecurringGenerationService, RecurringGenerationService>();
		builder.Services.AddSingleton<IReportingService, ReportingService>();
		// IDataService is kept for legacy compatibility; new code uses specific repository interfaces
#pragma warning disable CS0618
		builder.Services.AddSingleton<IDataService, DataServiceFacade>();
#pragma warning restore CS0618
		builder.Services.AddSingleton<IForecastService, ForecastService>();
		builder.Services.AddSingleton<ITransactionValidationService, TransactionValidationService>();
		// Import/parsers
		// register parser explicitly using DI extension to avoid ambiguous CommunityToolkit overloads
		Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IStatementParser, DkbCsvParser>(builder.Services);
		builder.Services.AddSingleton<ImportService>();
		// Categorization strategies
		Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<ICategorizationStrategy, KeywordCategorizationStrategy>(builder.Services);
		Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<ICategorizationStrategy, HistoricalCategorizationStrategy>(builder.Services);
		builder.Services.AddSingleton<CategorizationService>();

		builder.Services.AddApplicationUseCases();

		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton<IDialogService, ShellDialogService>();
		builder.Services.AddSingleton<IFeedbackService, MauiFeedbackService>();
		builder.Services.AddSingleton<IOnboardingCoordinator, OnboardingCoordinator>();
		builder.Services.AddSingleton<IMainThreadDispatcher, MauiMainThreadDispatcher>();
		builder.Services.AddSingleton<Finanzuebersicht.Presentation.Services.IFilePicker, MauiFilePicker>();
		builder.Services.AddSingleton<IAppEvents, MauiAppEvents>();
		builder.Services.AddSingleton<ICategoryCreateSheetService, CategoryCreateSheetService>();
		builder.Services.AddSingleton<IRecurringTransactionCreateSheetService, RecurringTransactionCreateSheetService>();
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
		builder.Services.AddTransient<CategoriesPage>();
		builder.Services.AddTransient<CategoryDetailPage>();
		builder.Services.AddTransient<AccountDetailPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SparZielePage>();
		builder.Services.AddTransient<SparZielDetailPage>();
		builder.Services.AddTransient<BackupListPage>();
		builder.Services.AddTransient<ImportPreviewPage>();
		builder.Services.AddTransient<CashflowPage>();
		builder.Services.AddTransient<OnboardingPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		_ = app.Services.GetRequiredService<IAppEvents>();

		return app;
	}
}
