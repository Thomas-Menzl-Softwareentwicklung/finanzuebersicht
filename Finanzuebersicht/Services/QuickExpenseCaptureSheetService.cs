using Finanzuebersicht.Controls;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

/// <summary>
/// Quick-expense capture via modal page (not CommunityToolkit Popup).
/// FormSheet/Popup v2 was crashing on iOS when opening from Transaktionen → Schnell.
/// </summary>
public sealed class QuickExpenseCaptureSheetService : IQuickExpenseCaptureSheetService
{
    public async Task<bool> ShowAsync(QuickExpenseCaptureViewModel viewModel)
    {
        var navigation = Shell.Current?.Navigation;
        if (navigation is null)
            return false;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settled = 0;

        void Complete(bool saved)
        {
            if (Interlocked.Exchange(ref settled, 1) == 0)
                tcs.TrySetResult(saved);
        }

        var form = new QuickExpenseFormView { BindingContext = viewModel };

        var card = new CreateFormCard
        {
            Title = viewModel.PageTitle,
            FormContent = form,
            CancelText = LocalizationResourceManager.Current[ResourceKeys.Btn_Abbrechen],
            SaveText = LocalizationResourceManager.Current[ResourceKeys.Btn_Speichern],
            ScrollFormContent = true,
            MaxFormHeight = 320,
            CancelCommand = new Command(async () =>
            {
                Complete(false);
                await navigation.PopModalAsync();
            }),
            SaveCommand = new Command(async () =>
            {
                if (!await viewModel.TrySaveAsync())
                    return;

                Complete(true);
                await navigation.PopModalAsync();
            })
        };

        var page = new ContentPage
        {
            Title = viewModel.PageTitle,
            Content = new ScrollView
            {
                Padding = new Thickness(20),
                Content = card
            }
        };

        // Swipe-down / system dismiss without Cancel button.
        page.Disappearing += (_, _) => Complete(false);

        await navigation.PushModalAsync(new NavigationPage(page));
        return await tcs.Task;
    }
}
