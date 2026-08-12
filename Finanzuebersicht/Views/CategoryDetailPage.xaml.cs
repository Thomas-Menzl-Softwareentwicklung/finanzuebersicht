using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class CategoryDetailPage : ContentPage, IQueryAttributable
{
    private readonly IAppEvents _appEvents;

    public CategoryDetailPage(CategoryDetailViewModel viewModel, IAppEvents appEvents)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _appEvents = appEvents;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _appEvents.LanguageChanged += OnLocalizationChanged;
        _appEvents.CurrencyChanged += OnLocalizationChanged;
    }

    protected override void OnDisappearing()
    {
        _appEvents.LanguageChanged -= OnLocalizationChanged;
        _appEvents.CurrencyChanged -= OnLocalizationChanged;
        base.OnDisappearing();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is IApplyQueryAttributes vm)
            vm.ApplyQueryAttributes(query);
    }

    private void OnLocalizationChanged()
    {
        if (BindingContext is ILocalizableViewModel vm)
            vm.RefreshLocalizedStrings();
    }
}
