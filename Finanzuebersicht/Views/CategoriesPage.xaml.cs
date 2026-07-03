using System.ComponentModel;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class CategoriesPage : BaseContentPage
{
    private CategoriesViewModel? _viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (BindingContext is CategoriesViewModel viewModel)
        {
            _viewModel = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        else
        {
            _viewModel = null;
        }
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategoriesViewModel.ShowAddKontoForm) || _viewModel?.ShowAddKontoForm != true)
            return;

        await KontenScrollView.ScrollToAsync(0, 0, false);
    }
}
