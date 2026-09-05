using Finanzuebersicht.Charts;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;
using System.ComponentModel;

namespace Finanzuebersicht.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly IOnboardingCoordinator _onboardingCoordinator;
    private readonly INavigationService _navigationService;
    private readonly IAppEvents _appEvents;
    private readonly DonutChartDrawable _monthDonut = new();
    private readonly BarChartDrawable _yearBar = new();
    private readonly DonutChartDrawable _yearDonut = new();
    private bool _onboardingChecked;

    public DashboardPage(
        DashboardViewModel viewModel,
        IOnboardingCoordinator onboardingCoordinator,
        INavigationService navigationService,
        IAppEvents appEvents)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _vm = viewModel;
        _onboardingCoordinator = onboardingCoordinator;
        _navigationService = navigationService;
        _appEvents = appEvents;

        MonthDonutChart.Drawable = _monthDonut;
        YearBarChart.Drawable = _yearBar;
        YearDonutChart.Drawable = _yearDonut;

        _monthDonut.Items = _vm.KategorieAusgaben;
        _yearBar.Months = _vm.JahrMonate;
        _yearBar.CurrentMonth = _vm.IsYearView ? 0 : _vm.AktuellerMonat.Month;
        _yearBar.MonthlyBudgetTotal = _vm.JahrBudgetTotal;
        _yearBar.ForecastMonth = _vm.ForecastBarMonth;
        _yearBar.ForecastValue = _vm.ForecastBarValue;
        _yearDonut.Items = _vm.JahrKategorien;

        _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DashboardViewModel vm)
            vm.LoadDashboardCommand.Execute(null);

        if (!_onboardingChecked)
        {
            _onboardingChecked = true;
            if (await _onboardingCoordinator.ShouldShowOnboardingAsync())
                await _navigationService.GoToAsync(Routes.Onboarding);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            MonthDonutChart.Invalidate();
            YearDonutChart.Invalidate();
            YearBarChart.Invalidate();
        });

        _appEvents.DataChanged += OnAppDataChanged;
        _appEvents.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _appEvents.DataChanged -= OnAppDataChanged;
        _appEvents.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        _vm.LoadDashboardCommand.Execute(null);
        if (BindingContext is ILocalizableViewModel locVm)
            locVm.RefreshLocalizedStrings();
    }

    private void OnAppDataChanged()
    {
        if (BindingContext is DashboardViewModel vm)
            _ = vm.LoadDashboardCommand.ExecuteAsync(null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DashboardViewModel.KategorieAusgaben):
                _monthDonut.Items = _vm.KategorieAusgaben;
                MainThread.BeginInvokeOnMainThread(() => MonthDonutChart.Invalidate());
                break;
            case nameof(DashboardViewModel.JahrMonate):
                _yearBar.Months = _vm.JahrMonate;
                _yearBar.CurrentMonth = _vm.IsYearView ? 0 : _vm.AktuellerMonat.Month;
                MainThread.BeginInvokeOnMainThread(() => YearBarChart.Invalidate());
                break;
            case nameof(DashboardViewModel.JahrKategorien):
                _yearDonut.Items = _vm.JahrKategorien;
                MainThread.BeginInvokeOnMainThread(() => YearDonutChart.Invalidate());
                break;
            case nameof(DashboardViewModel.JahrBudgetTotal):
                _yearBar.MonthlyBudgetTotal = _vm.JahrBudgetTotal;
                MainThread.BeginInvokeOnMainThread(() => YearBarChart.Invalidate());
                break;
            case nameof(DashboardViewModel.ForecastBarMonth):
            case nameof(DashboardViewModel.ForecastBarValue):
                _yearBar.ForecastMonth = _vm.ForecastBarMonth;
                _yearBar.ForecastValue = _vm.ForecastBarValue;
                MainThread.BeginInvokeOnMainThread(() => YearBarChart.Invalidate());
                break;
        }
    }
}
