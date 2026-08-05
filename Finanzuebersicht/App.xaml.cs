using System.Globalization;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht;

public partial class App : global::Microsoft.Maui.Controls.Application
{
	// App-wide event to notify UI of data changes (e.g., after import)
	public static event Action? DataChanged;

	public static event Action? LanguageChanged;

	public static event Action? CurrencyChanged;

		public static void NotifyDataChanged()
		{
			DataChanged?.Invoke();
		}

	private readonly IRecurringGenerationService _recurringGenerationService;
	private readonly InitializationService _initService;
	private readonly ThemeService _themeService;
	private readonly ProcessQuickExpenseInboxUseCase _processQuickExpenseInboxUseCase;
	private readonly ILicenseService _licenseService;
	private readonly ILogger<App>? _logger;
	private readonly string _savedTheme;

	public App(
		InitializationService initService,
		IRecurringGenerationService recurringGenerationService,
		ISettingsService settings,
		ThemeService themeService,
		ILocalizationService localizationService,
		IDisplayCurrencyService displayCurrency,
		ProcessQuickExpenseInboxUseCase processQuickExpenseInboxUseCase,
		ILicenseService licenseService,
		ILogger<App>? logger = null)
	{
		// Sprache vor InitializeComponent setzen, damit XAML-Bindings korrekt aufgelöst werden
		localizationService.Initialize();
		localizationService.LanguageChanged += () =>
		{
			LanguageChanged?.Invoke();
			PublishWidgetSharedState();
		};
		displayCurrency.Changed += () =>
		{
			CurrencyChanged?.Invoke();
			CurrencyRefreshRegistry.RefreshAll();
		};

		InitializeComponent();
		_recurringGenerationService = recurringGenerationService;
		_initService = initService;
		_themeService = themeService;
		_processQuickExpenseInboxUseCase = processQuickExpenseInboxUseCase;
		_licenseService = licenseService;
		_logger = logger;

		// Gespeichertes Theme anwenden (MAUI-Ebene)
		_savedTheme = settings.Get(SettingsKeys.Theme, ThemeValues.System);
		_themeService.Apply(_savedTheme);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

		// UIKit-Style nach Window-Erstellung setzen
		window.Created += (_, _) => _themeService.Apply(_savedTheme);

		window.Resumed += async (_, _) =>
		{
			try
			{
				await _recurringGenerationService.GeneratePendingRecurringTransactionsAsync();
				await ProcessQuickExpenseInboxAsync();
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Dauerauftrag-Generierung / Quick-Expense-Inbox bei Resume fehlgeschlagen");
			}
		};

		return window;
	}

	protected override async void OnStart()
	{
		base.OnStart();
		try
		{
			await _initService.InitializeAsync();
			await _licenseService.RefreshAsync();
			PublishWidgetSharedState();
			await _recurringGenerationService.GeneratePendingRecurringTransactionsAsync();
			await ProcessQuickExpenseInboxAsync();
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "App-Initialisierung fehlgeschlagen");
		}
	}

	private async Task ProcessQuickExpenseInboxAsync()
	{
		PublishWidgetSharedState();
		var saved = await _processQuickExpenseInboxUseCase.ExecuteAsync();
		if (saved > 0)
			NotifyDataChanged();
	}

	private void PublishWidgetSharedState()
	{
#if IOS
		try
		{
			AppGroupQuickExpenseInboxStore.PublishHasPro(
				_licenseService.HasFeature(AppFeature.QuickExpenseCapture));
			AppGroupQuickExpenseInboxStore.PublishPreferredLanguage(
				CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
		}
		catch (Exception ex)
		{
			_logger?.LogDebug(ex, "Could not publish widget App Group state");
		}
#endif
	}
}
