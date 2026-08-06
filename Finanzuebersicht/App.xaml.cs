using System.Globalization;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation;
using Finanzuebersicht.Presentation.Services;
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
	private readonly INavigationService _navigationService;
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
		INavigationService navigationService,
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
		_navigationService = navigationService;
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

	protected override async void OnAppLinkRequestReceived(Uri uri)
	{
		base.OnAppLinkRequestReceived(uri);
		try
		{
			await HandleAppLinkAsync(uri);
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "App-Link fehlgeschlagen: {Uri}", uri);
		}
	}

	internal async Task HandleAppLinkAsync(Uri uri)
	{
		if (!string.Equals(uri.Scheme, "finanzuebersicht", StringComparison.OrdinalIgnoreCase))
			return;

		var host = uri.Host;
		if (string.IsNullOrEmpty(host) && uri.AbsolutePath.StartsWith("/", StringComparison.Ordinal))
			host = uri.AbsolutePath.Trim('/');

		if (!string.Equals(host, "quick-expense", StringComparison.OrdinalIgnoreCase))
			return;

		var amount = string.Empty;
		var title = string.Empty;
		foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var eq = part.IndexOf('=');
			if (eq <= 0)
				continue;
			var key = Uri.UnescapeDataString(part[..eq]);
			var value = Uri.UnescapeDataString(part[(eq + 1)..]);
			if (string.Equals(key, "amount", StringComparison.OrdinalIgnoreCase))
				amount = value;
			else if (string.Equals(key, "title", StringComparison.OrdinalIgnoreCase))
				title = value;
		}

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await _navigationService.GoToAsync(
				Routes.QuickExpenseCapture,
				new Dictionary<string, object>
				{
					[NavigationQueryKeys.Amount] = amount,
					[NavigationQueryKeys.Title] = title
				});
		});
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
			Platforms.iOS.WidgetTimelineReloader.ReloadAll();
		}
		catch (Exception ex)
		{
			_logger?.LogDebug(ex, "Could not publish widget App Group state");
		}
#endif
	}
}
