using System.ComponentModel;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class SparZielePage : BaseContentPage
{
    private SparZieleViewModel? _viewModel;

    public SparZielePage(SparZieleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        if (BindingContext is SparZieleViewModel viewModel)
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
        if (e.PropertyName != nameof(SparZieleViewModel.ShowAddForm) || _viewModel?.ShowAddForm != true)
            return;

        await SparZieleScrollView.ScrollToAsync(0, 0, false);
        AddSparZielForm.FocusForm();
    }
}
