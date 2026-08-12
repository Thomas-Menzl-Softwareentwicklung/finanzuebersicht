using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class SparZielePage : BaseContentPage
{
    public SparZielePage(SparZieleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
