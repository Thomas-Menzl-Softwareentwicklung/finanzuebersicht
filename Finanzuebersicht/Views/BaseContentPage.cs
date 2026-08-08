using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Finanzuebersicht.Views;

/// <summary>
/// Base page that automatically executes <see cref="IAutoLoadViewModel.AutoLoadCommand"/>
/// on appearing. Pages with complex OnAppearing logic (e.g. DashboardPage) should
/// override directly instead of using this base class.
/// </summary>
/// <remarks>
/// <b>Requirement:</b> <see cref="ContentPage.BindingContext"/> must be set before
/// <c>OnAppearing</c> fires (i.e. via constructor injection, not XAML binding or
/// late assignment in code-behind), otherwise auto-load will silently not fire.
/// <para>
/// Pages that should <em>not</em> reload on every back-navigation can override
/// <see cref="IAutoLoadViewModel.ShouldAutoLoad"/> to return <c>false</c> based
/// on cached state.
/// </para>
/// </remarks>
public abstract class BaseContentPage : ContentPage
{
    private IAppEvents? _appEvents;

    /// <summary>Cached after first resolve; safe to use in <see cref="OnDisappearing"/>.</summary>
    protected IAppEvents? CachedAppEvents => _appEvents;

    /// <summary>
    /// Resolves the DI-backed app event bus (not static <c>App</c> events).
    /// </summary>
    protected IAppEvents AppEvents
    {
        get
        {
            if (_appEvents is not null)
                return _appEvents;

            var services = Handler?.MauiContext?.Services
                ?? Application.Current?.Handler?.MauiContext?.Services;
            _appEvents = services?.GetService<IAppEvents>()
                ?? throw new InvalidOperationException("IAppEvents is not registered in the MAUI service provider.");
            return _appEvents;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppEvents.LanguageChanged += OnLanguageChanged;
        AppEvents.CurrencyChanged += OnCurrencyChanged;
        if (BindingContext is IAutoLoadViewModel vm && vm.ShouldAutoLoad)
            vm.AutoLoadCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        if (_appEvents is not null)
        {
            _appEvents.LanguageChanged -= OnLanguageChanged;
            _appEvents.CurrencyChanged -= OnCurrencyChanged;
        }

        base.OnDisappearing();
    }

    private void OnLanguageChanged()
    {
        if (BindingContext is ILocalizableViewModel locVm)
            locVm.RefreshLocalizedStrings();

        if (BindingContext is IAutoLoadViewModel vm && vm.ShouldAutoLoad)
            vm.AutoLoadCommand.Execute(null);
    }

    private void OnCurrencyChanged()
    {
        if (BindingContext is ICurrencyRefreshViewModel)
            return;

        if (BindingContext is ILocalizableViewModel locVm)
            locVm.RefreshLocalizedStrings();
    }
}
