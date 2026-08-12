using System.Globalization;
using Finanzuebersicht.Application.UseCases.ScreenshotDemo;
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
	private readonly IRecurringGenerationService _recurringGenerationService;
	private readonly InitializationService _initService;
	private readonly ThemeService _themeService;
	private readonly ProcessQuickExpenseInboxUseCase _processQuickExpenseInboxUseCase;
	private readonly ILicenseService _licenseService;
	private readonly INavigationService _navigationService;
	private readonly IAppEvents _appEvents;
	private readonly IQuickExpenseWidgetPresetStore? _quickExpenseWidgetPresetStore;
	private readonly SeedScreenshotDemoDataUseCase _seedScreenshotDemoDataUseCase;
	private readonly ILogger<App>? _logger;
	private readonly string _savedTheme;
	private readonly bool _screenshotDemoMode;
	private Uri? _pendingAppLink;
	private bool _startupComplete;
	private static Uri? _pendingAppLinkBeforeApp;

	/// <summary>
	/// Entry from UIScene OpenUrl / WillConnect (custom scheme). Queues until Shell is ready.
	/// </summary>
	public static void EnqueueAppLink(Uri uri)
	{
		if (Current is App app)
			app.ReceiveAppLink(uri);
		else
			_pendingAppLinkBeforeApp = uri;
	}

	public App(
		InitializationService initService,
		IRecurringGenerationService recurringGenerationService,
		ISettingsService settings,
		ThemeService themeService,
		ILocalizationService localizationService,
		IDisplayCurrencyService displayCurrency,
		ProcessQuickExpenseInboxUseCase processQuickExpenseInboxUseCase,
		SeedScreenshotDemoDataUseCase seedScreenshotDemoDataUseCase,
		ILicenseService licenseService,
		INavigationService navigationService,
		IAppEvents appEvents,
		IQuickExpenseWidgetPresetStore? quickExpenseWidgetPresetStore = null,
		ILogger<App>? logger = null)
	{
		_appEvents = appEvents;
		_screenshotDemoMode = ScreenshotDemoBootstrap.IsActive();

		// Sprache vor InitializeComponent setzen, damit XAML-Bindings korrekt aufgelöst werden
		localizationService.Initialize();
		localizationService.LanguageChanged += () =>
		{
			_appEvents.NotifyLanguageChanged();
			if (!_screenshotDemoMode)
				PublishWidgetSharedState();
		};
		displayCurrency.Changed += () =>
		{
			_appEvents.NotifyCurrencyChanged();
			CurrencyRefreshRegistry.RefreshAll();
		};

		InitializeComponent();
		_recurringGenerationService = recurringGenerationService;
		_initService = initService;
		_themeService = themeService;
		_processQuickExpenseInboxUseCase = processQuickExpenseInboxUseCase;
		_licenseService = licenseService;
		_navigationService = navigationService;
		_quickExpenseWidgetPresetStore = quickExpenseWidgetPresetStore;
		_seedScreenshotDemoDataUseCase = seedScreenshotDemoDataUseCase;
		_logger = logger;

		// Gespeichertes Theme anwenden (MAUI-Ebene); Screenshot-Demo erzwingt Light ohne Settings-Persistenz
		_savedTheme = _screenshotDemoMode
			? ThemeValues.Light
			: settings.Get(SettingsKeys.Theme, ThemeValues.System);
		_themeService.Apply(_savedTheme);

		if (_pendingAppLinkBeforeApp is not null)
		{
			_pendingAppLink = _pendingAppLinkBeforeApp;
			_pendingAppLinkBeforeApp = null;
		}
	}

	internal void ReceiveAppLink(Uri uri)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				if (!_startupComplete || Shell.Current is null)
				{
					_pendingAppLink = uri;
					return;
				}

				await HandleAppLinkAsync(uri);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "App-Link (Scene) fehlgeschlagen: {Uri}", uri);
			}
		});
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
				await _licenseService.RefreshAsync();
				if (_screenshotDemoMode)
					return;

				PublishWidgetSharedState();
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
			if (_screenshotDemoMode)
				await ScreenshotDemoBootstrap.TrySeedAsync(_seedScreenshotDemoDataUseCase);
			await _licenseService.RefreshAsync();
			if (!_screenshotDemoMode)
			{
				await EnsureWidgetPresetsMirroredAsync();
				PublishWidgetSharedState();
				await _recurringGenerationService.GeneratePendingRecurringTransactionsAsync();
				await ProcessQuickExpenseInboxAsync();
			}
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, "App-Initialisierung fehlgeschlagen");
		}
		finally
		{
			_startupComplete = true;
			var pending = _pendingAppLink;
			_pendingAppLink = null;
			if (pending is not null)
			{
				try
				{
					await HandleAppLinkAsync(pending);
				}
				catch (Exception ex)
				{
					_logger?.LogError(ex, "Pending App-Link fehlgeschlagen: {Uri}", pending);
				}
			}
		}
	}

	protected override void OnAppLinkRequestReceived(Uri uri)
	{
		base.OnAppLinkRequestReceived(uri);
		ReceiveAppLink(uri);
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
			// Wait briefly if Shell is still wiring up after cold start.
			for (var i = 0; i < 20 && Shell.Current is null; i++)
				await Task.Delay(50);

			if (Shell.Current is null)
			{
				_pendingAppLink = uri;
				_logger?.LogWarning("Shell not ready for App-Link; queued {Uri}", uri);
				return;
			}

			var parameters = new Dictionary<string, object>
			{
				[NavigationQueryKeys.Amount] = amount,
				[NavigationQueryKeys.Title] = title
			};

			// Land on Transaktionen (Schnell lives there), then open capture with prefill.
			await _navigationService.GoToAsync(Routes.TransactionsTab);
			await _navigationService.GoToAsync(Routes.QuickExpenseCapture, parameters);
		});
	}

	private async Task ProcessQuickExpenseInboxAsync()
	{
		if (_screenshotDemoMode)
			return;

		PublishWidgetSharedState();
		var saved = await _processQuickExpenseInboxUseCase.ExecuteAsync();
		if (saved > 0)
			_appEvents.NotifyDataChanged();
	}

	/// <summary>
	/// Copies locally saved presets into the App Group if the widget file is still missing.
	/// </summary>
	private async Task EnsureWidgetPresetsMirroredAsync()
	{
		if (_quickExpenseWidgetPresetStore is null)
			return;

		try
		{
			await _quickExpenseWidgetPresetStore.LoadAsync();
		}
		catch (Exception ex)
		{
			_logger?.LogDebug(ex, "Widget preset mirror/load skipped");
		}
	}

	private void PublishWidgetSharedState()
	{
		if (_screenshotDemoMode)
			return;

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
