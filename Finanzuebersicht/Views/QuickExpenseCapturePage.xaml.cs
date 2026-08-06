using Finanzuebersicht.Navigation;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Views;

public partial class QuickExpenseCapturePage : ContentPage, IQueryAttributable
{
    public QuickExpenseCapturePage(QuickExpenseCaptureViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.Reset();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is IApplyQueryAttributes vm)
            vm.ApplyQueryAttributes(query);
    }
}
