using Finanzuebersicht.Controls;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class ImportMappingPage : BaseContentPage
{
    public ImportMappingPage(ImportMappingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is ImportMappingViewModel viewModel && !TransientPageOverlay.IsActive(this))
            viewModel.HandlePageDisappearing();

        base.OnDisappearing();
    }
}
