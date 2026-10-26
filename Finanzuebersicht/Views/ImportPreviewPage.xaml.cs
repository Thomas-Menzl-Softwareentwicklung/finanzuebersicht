using Finanzuebersicht.Controls;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class ImportPreviewPage : BaseContentPage
{
    public ImportPreviewPage(ImportPreviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is ImportPreviewViewModel viewModel)
            viewModel.HandlePageDisappearing(retainSession: TransientPageOverlay.IsActive(this));

        base.OnDisappearing();
    }
}
