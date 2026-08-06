using Finanzuebersicht.ViewModels;

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
    protected override void OnAppearing()
    {
        base.OnAppearing();
        App.LanguageChanged += OnLanguageChanged;
        App.CurrencyChanged += OnCurrencyChanged;
        if (BindingContext is IAutoLoadViewModel vm && vm.ShouldAutoLoad)
            vm.AutoLoadCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        App.LanguageChanged -= OnLanguageChanged;
        App.CurrencyChanged -= OnCurrencyChanged;
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
